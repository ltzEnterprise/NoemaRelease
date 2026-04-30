using UnityEngine;
using System.Collections;

public class MagicalPhoto : MonoBehaviour
{
    [Header("--- HORÁRIO NECESSÁRIO ---")]
    public DayNightCycle.TimeState horarioNecessario = DayNightCycle.TimeState.InitialDay;

    [Header("--- PHOTO ANIMATION (HAND) ---")]
    public Vector3 restingPosition;
    public Vector3 restingRotation;
    public Vector3 restingScale = Vector3.one;
    
    public Vector3 aimingPosition;
    public Vector3 aimingRotation;
    public Vector3 aimingScale = Vector3.one;
    
    public float animationSpeed = 10f;

    [Header("--- ALIGNMENT SETTINGS ---")]
    public Transform idealPoint;
    public float maxDistance = 2.5f;
    public float maxAngle = 18.0f;

    [Header("--- THE MAGIC ---")]
    public GameObject objectToReveal;
    public bool hideInsteadOfReveal = false; 
    public AudioSource audioSource;
    public AudioClip revealSound;
    public AudioClip erroHorarioSound;

    [Header("--- CONSUMIR ITEM DO INVENTÁRIO ---")]
    public bool consumirFotoAoUsar = true;
    public int idDaFotoNoInventario = 0;

    [Header("--- UI ---")]
    public GameObject textoDicaMagica;

    private bool isAiming = false;
    private bool alreadyUsed = false;
    private bool inicializado = false;

    void Start()
    {
        if (textoDicaMagica) textoDicaMagica.SetActive(false);
        StartCoroutine(CarregarEstadoSeguro());
    }

    IEnumerator CarregarEstadoSeguro()
    {
        if (PersistenciaManager.Instance != null)
            yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);

        bool jaResolvido = false;

        if (PersistenciaManager.Instance != null && objectToReveal != null)
            jaResolvido = PersistenciaManager.Instance.ObterEstado(objectToReveal.name, false);

        if (jaResolvido)
        {
            if (objectToReveal)
                objectToReveal.SetActive(hideInsteadOfReveal ? false : true);

            alreadyUsed = true;

            if (consumirFotoAoUsar && InventoryManager.Instance != null)
                InventoryManager.Instance.ConsumirItem(idDaFotoNoInventario);

            gameObject.SetActive(false);
            yield break;
        }
        else
        {
            if (objectToReveal)
                objectToReveal.SetActive(hideInsteadOfReveal ? true : false);
        }

        inicializado = true;
    }

    void Update()
    {
        if (!inicializado) return;
        if (alreadyUsed) return;

        if (Input.GetMouseButtonDown(1)) isAiming = true;
        if (Input.GetMouseButtonUp(1)) isAiming = false;

        Vector3 targetPos = isAiming ? aimingPosition : restingPosition;
        Quaternion targetRot = Quaternion.Euler(isAiming ? aimingRotation : restingRotation);
        Vector3 targetScale = isAiming ? aimingScale : restingScale;

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * animationSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * animationSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);

        bool noPontoCerto = false;
        bool horarioCerto = DayNightCycle.Instance != null && DayNightCycle.Instance.currentState == horarioNecessario;
        
        if (isAiming && idealPoint != null && Camera.main != null && horarioCerto)
        {
            float dist = Vector3.Distance(Camera.main.transform.position, idealPoint.position);
            float angle = Quaternion.Angle(Camera.main.transform.rotation, idealPoint.rotation);
            noPontoCerto = dist <= maxDistance && angle <= maxAngle;
        }

        if (textoDicaMagica)
            textoDicaMagica.SetActive(noPontoCerto);

        if (isAiming && Input.GetMouseButtonDown(0))
            TryRevealObject();
    }

    void TryRevealObject()
    {
        bool horarioCerto = DayNightCycle.Instance != null && DayNightCycle.Instance.currentState == horarioNecessario;

        if (!horarioCerto) 
        {
            if (audioSource && erroHorarioSound)
                audioSource.PlayOneShot(erroHorarioSound);

            return;
        }

        if (idealPoint == null || Camera.main == null) return;

        float dist = Vector3.Distance(Camera.main.transform.position, idealPoint.position);
        float angle = Quaternion.Angle(Camera.main.transform.rotation, idealPoint.rotation);

        if (dist <= maxDistance && angle <= maxAngle) 
        {
            if (textoDicaMagica)
                textoDicaMagica.SetActive(false); 

            StartCoroutine(SequenciaVitoria());
        }
    }

    IEnumerator SequenciaVitoria()
    {
        alreadyUsed = true;
        
        if (objectToReveal)
            objectToReveal.SetActive(hideInsteadOfReveal ? false : true);

        if (audioSource && revealSound)
            audioSource.PlayOneShot(revealSound);

        if (PersistenciaManager.Instance != null && objectToReveal != null)
            PersistenciaManager.Instance.RegistrarEstado(objectToReveal.name, true);

        if (consumirFotoAoUsar && InventoryManager.Instance != null)
            InventoryManager.Instance.ConsumirItem(idDaFotoNoInventario);

        float t = 0;
        Vector3 currentPos = transform.localPosition;
        Vector3 currentScale = transform.localScale;
        
        while (t < 1f)
        {
            t += Time.deltaTime * 6f; 
            transform.localPosition = Vector3.Lerp(currentPos, currentPos + (Vector3.down * 2f), t);
            transform.localScale = Vector3.Lerp(currentScale, Vector3.zero, t); 
            yield return null;
        }

        SalvarProgressoSeguro();
        gameObject.SetActive(false);
    }

    private void SalvarProgressoSeguro()
    {
        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(true);
    }
}
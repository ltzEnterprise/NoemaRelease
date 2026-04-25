using UnityEngine;
using System.Collections;

public class MagicalPhoto : MonoBehaviour
{
    [Header("--- HORÁRIO NECESSÁRIO ---")]
    [Tooltip("Em qual momento do dia essa foto específica tem poder?")]
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
    [Tooltip("Se marcado, a mágica faz o objeto SUMIR ao invés de aparecer.")]
    public bool hideInsteadOfReveal = false; 
    public AudioSource audioSource;
    public AudioClip revealSound;
    [Tooltip("Som que vai tocar se o jogador clicar na hora errada do dia.")]
    public AudioClip erroHorarioSound; // 🔥 VARIÁVEL NOVA AQUI

    [Header("--- UI ---")]
    [Tooltip("Coloque aqui o texto que avisa o jogador para clicar quando estiver no ângulo certo.")]
    public GameObject textoDicaMagica;

    private bool isAiming = false;
    private bool alreadyUsed = false;

    void Start() {
        if (textoDicaMagica) textoDicaMagica.SetActive(false);

        bool jaResolvido = false;
        if (PersistenciaManager.Instance != null && objectToReveal != null) {
            jaResolvido = PersistenciaManager.Instance.ObterEstado(objectToReveal.name);
        }

        if (jaResolvido) {
            if (objectToReveal) objectToReveal.SetActive(hideInsteadOfReveal ? false : true);
            gameObject.SetActive(false); 
        } else {
            if (objectToReveal) objectToReveal.SetActive(hideInsteadOfReveal ? true : false);
        }
    }

    void Update() {
        if (alreadyUsed) return;

        if (Input.GetMouseButtonDown(1)) isAiming = true;
        if (Input.GetMouseButtonUp(1)) isAiming = false;

        Vector3 targetPos = isAiming ? aimingPosition : restingPosition;
        Quaternion targetRot = Quaternion.Euler(isAiming ? aimingRotation : restingRotation);
        Vector3 targetScale = isAiming ? aimingScale : restingScale;

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * animationSpeed);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, Time.deltaTime * animationSpeed);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * animationSpeed);

        // --- SISTEMA DA DICA NA TELA ---
        bool noPontoCerto = false;
        
        // 🔥 VERIFICA SE ESTÁ NA HORA CERTA DO DIA
        bool horarioCerto = (DayNightCycle.Instance != null && DayNightCycle.Instance.currentState == horarioNecessario);
        
        if (isAiming && idealPoint != null && Camera.main != null && horarioCerto)
        {
            float dist = Vector3.Distance(Camera.main.transform.position, idealPoint.position);
            float angle = Quaternion.Angle(Camera.main.transform.rotation, idealPoint.rotation);
            
            if (dist <= maxDistance && angle <= maxAngle)
            {
                noPontoCerto = true;
            }
        }

        if (textoDicaMagica) textoDicaMagica.SetActive(noPontoCerto);

        if (isAiming && Input.GetMouseButtonDown(0)) TryRevealObject();
    }

    void TryRevealObject() {
        // 🔥 TRAVA DE SEGURANÇA COM ÁUDIO DE ERRO
        bool horarioCerto = (DayNightCycle.Instance != null && DayNightCycle.Instance.currentState == horarioNecessario);
        if (!horarioCerto) 
        {
            // Se tentou clicar na hora errada, toca o som e cancela a função
            if (audioSource && erroHorarioSound) audioSource.PlayOneShot(erroHorarioSound);
            return;
        }

        if (idealPoint == null || Camera.main == null) return;
        float dist = Vector3.Distance(Camera.main.transform.position, idealPoint.position);
        float angle = Quaternion.Angle(Camera.main.transform.rotation, idealPoint.rotation);

        if (dist <= maxDistance && angle <= maxAngle) 
        {
            if (textoDicaMagica) textoDicaMagica.SetActive(false); 
            StartCoroutine(SequenciaVitoria());
        }
        else 
        {
            Debug.Log($"Fora de foco. Dist: {dist:F1} | Ang: {angle:F1}");
        }
    }

    IEnumerator SequenciaVitoria() {
        alreadyUsed = true;
        
        if (objectToReveal) objectToReveal.SetActive(hideInsteadOfReveal ? false : true);
        if (audioSource && revealSound) audioSource.PlayOneShot(revealSound);

        if (PersistenciaManager.Instance != null && objectToReveal != null) {
            PersistenciaManager.Instance.RegistrarEstado(objectToReveal.name, true);
        }

        float t = 0;
        Vector3 currentPos = transform.localPosition;
        Vector3 currentScale = transform.localScale;
        
        while (t < 1f) {
            t += Time.deltaTime * 6f; 
            transform.localPosition = Vector3.Lerp(currentPos, currentPos + (Vector3.down * 2f), t);
            transform.localScale = Vector3.Lerp(currentScale, Vector3.zero, t); 
            yield return null;
        }

        if (PersistenciaManager.Instance != null) PersistenciaManager.Instance.SalvarTudo();
        
        gameObject.SetActive(false);
    }
}
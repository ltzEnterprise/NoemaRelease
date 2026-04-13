using UnityEngine;
using System.Collections;

public class MagicalPhoto : MonoBehaviour
{
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

    private bool isAiming = false;
    private bool alreadyUsed = false;

    void Start() {
        // Verifica no save se a mágica já foi feita
        bool jaResolvido = false;
        if (PersistenciaManager.Instance != null && objectToReveal != null) {
            jaResolvido = PersistenciaManager.Instance.ObterEstado(objectToReveal.name);
        }

        if (jaResolvido) {
            // Já usou no passado! Aplica o resultado final e desativa a foto da mão para sempre.
            if (objectToReveal) objectToReveal.SetActive(hideInsteadOfReveal ? false : true);
            gameObject.SetActive(false); 
        } else {
            // Ainda não usou. Prepara o objeto no estado inicial e deixa a foto pronta para uso.
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

        if (isAiming && Input.GetMouseButtonDown(0)) TryRevealObject();
    }

    void TryRevealObject() {
        if (idealPoint == null || Camera.main == null) return;
        float dist = Vector3.Distance(Camera.main.transform.position, idealPoint.position);
        float angle = Quaternion.Angle(Camera.main.transform.rotation, idealPoint.rotation);

        if (dist <= maxDistance && angle <= maxAngle) StartCoroutine(SequenciaVitoria());
        else Debug.Log($"Fora de foco. Dist: {dist:F1} | Ang: {angle:F1}");
    }

    IEnumerator SequenciaVitoria() {
        alreadyUsed = true;
        
        // Aplica a mágica invertendo o estado dependendo da caixinha
        if (objectToReveal) objectToReveal.SetActive(hideInsteadOfReveal ? false : true);
        if (audioSource && revealSound) audioSource.PlayOneShot(revealSound);

        // REGISTRA NO LINK DO MANAGER (Gravamos 'true' para dizer que a foto já foi ativada neste objeto)
        if (PersistenciaManager.Instance != null && objectToReveal != null) {
            PersistenciaManager.Instance.RegistrarEstado(objectToReveal.name, true);
        }

        // Animação da foto saindo da tela
        float t = 0;
        Vector3 currentPos = transform.localPosition;
        while (t < 1f) {
            t += Time.deltaTime * 4f;
            transform.localPosition = Vector3.Lerp(currentPos, currentPos + Vector3.down * 3f, t);
            yield return null;
        }

        // SALVA DE VERDADE SÓ DEPOIS DA CENA
        if (PersistenciaManager.Instance != null) PersistenciaManager.Instance.SalvarTudo();
        
        // FOTO SOME DA MÃO DE VEZ
        gameObject.SetActive(false);
    }
}
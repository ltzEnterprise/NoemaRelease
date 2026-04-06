using UnityEngine;

public class SimpleDoor : MonoBehaviour
{
    [Header("Configuração de Ângulo")]
    [Tooltip("Quantos graus ela vai girar a partir de onde está? (Ex: 90 ou -90)")]
    public float anguloDeAbertura = 90f;
    [Tooltip("Velocidade suave de abertura (Recomendado: 5)")]
    public float velocidade = 5f; 

    [Header("Physics")]
    public Collider physicsCollider; 

    [Header("Locking")]
    public GameObject blockerObject; 
    
    [Header("UI")]
    public GameObject interactText; 

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound; 

    private bool isOpen = false;
    private bool isMoving = false; // Guarda se a porta tá girando
    private bool taOlhando = false; // Guarda se o player tá com a mira nela

    private Quaternion rotacaoFechada;
    private Quaternion rotacaoAberta;
    private float tempoUltimoClique = 0f;

    void Start()
    {
        rotacaoFechada = transform.localRotation;
        rotacaoAberta = rotacaoFechada * Quaternion.Euler(0, anguloDeAbertura, 0);
        
        if (interactText) interactText.SetActive(false);
    }

    public void AoOlhar()
    {
        taOlhando = true;
        bool isBlocked = (blockerObject != null && blockerObject.activeSelf);
        
        // Só acende se não tiver bloqueada E não estiver se mexendo
        if (interactText && !isBlocked && !isMoving) interactText.SetActive(true);
    }

    public void AoSair()
    {
        taOlhando = false;
        if (interactText) interactText.SetActive(false);
    }

    public void Interagir()
    {
        if (Time.time < tempoUltimoClique + 0.5f) return;
        tempoUltimoClique = Time.time;

        bool isBlocked = (blockerObject != null && blockerObject.activeSelf);

        if (isBlocked)
        {
            if (audioSource && lockedSound) audioSource.PlayOneShot(lockedSound);
            return;
        }

        if (interactText) interactText.SetActive(false);

        isOpen = !isOpen;

        if (isOpen && audioSource && openSound) audioSource.PlayOneShot(openSound);
        else if (!isOpen && audioSource && closeSound) audioSource.PlayOneShot(closeSound);
    }

    void Update()
    {
        Quaternion alvo = isOpen ? rotacaoAberta : rotacaoFechada;
        
        transform.localRotation = Quaternion.Slerp(transform.localRotation, alvo, velocidade * Time.deltaTime);

        if (physicsCollider != null)
        {
            // Se faltar mais de 1 grau, ela ainda tá mexendo
            isMoving = Quaternion.Angle(transform.localRotation, alvo) > 1.0f;
            
            physicsCollider.isTrigger = isMoving;

            // Se ela terminou de se mexer e o player AINDA tá olhando pra ela, acende o texto
            if (!isMoving && taOlhando && interactText && !interactText.activeSelf)
            {
                bool isBlocked = (blockerObject != null && blockerObject.activeSelf);
                if (!isBlocked) interactText.SetActive(true);
            }
        }
    }
}
using UnityEngine;

public class SimpleDoor : MonoBehaviour
{
    [Header("Configuração de Ângulo")]
    [Tooltip("Quantos graus ela vai girar a partir de onde está? (Ex: 90 ou -90)")]
    public float anguloDeAbertura = 90f;
    [Tooltip("Velocidade suave de abertura (Recomendado: 5)")]
    public float velocidade = 5f; 

    [Header("Physics")]
    public Collider physicsCollider; // O colisor que vai virar fantasma

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
    private Quaternion rotacaoFechada;
    private Quaternion rotacaoAberta;
    
    // Trava para impedir o bug do "clique metralhadora" do Raycast
    private float tempoUltimoClique = 0f;

    void Start()
    {
        // Salva a posição inicial exata do mapa
        rotacaoFechada = transform.localRotation;
        
        // Calcula a posição aberta
        rotacaoAberta = rotacaoFechada * Quaternion.Euler(0, anguloDeAbertura, 0);
        
        if (interactText) interactText.SetActive(false);
    }

    public void AoOlhar()
    {
        bool isBlocked = (blockerObject != null && blockerObject.activeSelf);
        if (interactText && !isBlocked) interactText.SetActive(true);
    }

    public void AoSair()
    {
        if (interactText) interactText.SetActive(false);
    }

    public void Interagir()
    {
        // Trava de segurança: ignora se clicou muito rápido (0.5s)
        if (Time.time < tempoUltimoClique + 0.5f) return;
        tempoUltimoClique = Time.time;

        bool isBlocked = (blockerObject != null && blockerObject.activeSelf);

        if (isBlocked)
        {
            if (audioSource && lockedSound) audioSource.PlayOneShot(lockedSound);
            return;
        }

        // Limpa a tela imediatamente ao interagir
        if (interactText) interactText.SetActive(false);

        isOpen = !isOpen;

        if (isOpen && audioSource && openSound) audioSource.PlayOneShot(openSound);
        else if (!isOpen && audioSource && closeSound) audioSource.PlayOneShot(closeSound);
    }

    void Update()
    {
        Quaternion alvo = isOpen ? rotacaoAberta : rotacaoFechada;
        
        // Movimento Slerp (suave e desacelera no final)
        transform.localRotation = Quaternion.Slerp(transform.localRotation, alvo, velocidade * Time.deltaTime);

        // --- SISTEMA DE FANTASMA ---
        if (physicsCollider != null)
        {
            // Se faltar mais de 1 grau pro alvo, ela ainda tá mexendo
            bool isMoving = Quaternion.Angle(transform.localRotation, alvo) > 1.0f;
            
            // Fica intangível pro jogador, mas o Raycast continua vendo
            physicsCollider.isTrigger = isMoving;
        }
    }
}
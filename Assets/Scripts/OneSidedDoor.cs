using UnityEngine;
using System.Collections;

public class OneSidedDoor : MonoBehaviour
{
    [Header("Movement")]
    public float closedAngle = 0f; 
    [Tooltip("Quantos graus ela vai girar para abrir? (Ex: 90 ou -90)")]
    public float openAngle = 90f; 
    public float speed = 5f;

    [Header("Physics")]
    public Collider physicsCollider;

    [Header("Configuration")]
    [Tooltip("If player is on Z Positive side (Front), is it locked?")]
    public bool lockedOnPositiveSide = true; 

    [Header("UI")]
    public GameObject interactText; 
    public GameObject lockedMessage;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound; 

    private bool isOpen = false;
    private bool isUnlocked = false; 
    
    private Quaternion rotacaoFechada;
    private Quaternion rotacaoAberta;
    
    // Trava do Raycast e UI
    private float tempoUltimoClique = 0f;
    private bool estaOlhando = false;
    private bool mostrandoErro = false;

    void Start()
    {
        rotacaoFechada = transform.localRotation;
        rotacaoAberta = rotacaoFechada * Quaternion.Euler(0, openAngle, 0);

        if (lockedMessage) lockedMessage.SetActive(false);
        if (interactText) interactText.SetActive(false);
    }

    public void AoOlhar()
    {
        estaOlhando = true;
        // Só acende o botão de interação se não tiver uma mensagem de erro na cara do jogador
        if (!mostrandoErro && interactText) interactText.SetActive(true);
    }

    public void AoSair()
    {
        estaOlhando = false;
        if (interactText) interactText.SetActive(false);
        
        // Limpa a tela caso o jogador vire de costas rápido durante o erro
        if (lockedMessage) lockedMessage.SetActive(false);
    }

    public void Interagir()
    {
        if (Time.time < tempoUltimoClique + 0.5f) return;
        
        // TRAVA ANTI-SPAM: Impede o cara de ficar apertando E e sobrepondo áudio/coroutine de erro
        if (mostrandoErro) return; 

        tempoUltimoClique = Time.time;

        if (interactText) interactText.SetActive(false);

        CheckSideAndInteract();
    }

    void Update()
    {
        Quaternion alvo = isOpen ? rotacaoAberta : rotacaoFechada;
        
        // Gira a porta suavemente
        transform.localRotation = Quaternion.Slerp(transform.localRotation, alvo, speed * Time.deltaTime);

        // --- SISTEMA DE FANTASMA ---
        if (physicsCollider != null)
        {
            // Se a diferença entre o alvo e a porta atual for maior que 1 grau, ela está em movimento
            bool isMoving = Quaternion.Angle(transform.localRotation, alvo) > 1.0f;
            
            // Transforma num fantasma enquanto mexe (o jogador passa direto, mas o raycast ainda pega)
            physicsCollider.isTrigger = isMoving;
        }
    }

    void CheckSideAndInteract()
    {
        if (isUnlocked)
        {
            ToggleDoor();
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Vector3 localPos = transform.InverseTransformPoint(player.transform.position);
        bool isOnFront = localPos.z > 0;

        if (lockedOnPositiveSide == isOnFront)
        {
            StartCoroutine(ShowLockedMessage());
        }
        else
        {
            isUnlocked = true;
            ToggleDoor();
        }
    }

    void ToggleDoor()
    {
        isOpen = !isOpen;
        if (audioSource) 
            audioSource.PlayOneShot(isOpen ? openSound : closeSound);
    }

    IEnumerator ShowLockedMessage()
    {
        mostrandoErro = true;

        if (audioSource && lockedSound) audioSource.PlayOneShot(lockedSound);
        if (lockedMessage) lockedMessage.SetActive(true);
        
        yield return new WaitForSeconds(2.0f);
        
        if (lockedMessage) lockedMessage.SetActive(false);
        
        mostrandoErro = false;

        // A MÁGICA AQUI: Devolve o botão de interagir se o cara AINDA tiver olhando pra porta
        if (estaOlhando && interactText) interactText.SetActive(true);
    }
}
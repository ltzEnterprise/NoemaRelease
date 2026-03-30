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
    
    // Trava do Raycast
    private float tempoUltimoClique = 0f;

    void Start()
    {
        rotacaoFechada = transform.localRotation;
        rotacaoAberta = rotacaoFechada * Quaternion.Euler(0, openAngle, 0);

        if (lockedMessage) lockedMessage.SetActive(false);
        if (interactText) interactText.SetActive(false);
    }

    public void AoOlhar()
    {
        if (interactText) interactText.SetActive(true);
    }

    public void AoSair()
    {
        if (interactText) interactText.SetActive(false);
    }

    public void Interagir()
    {
        if (Time.time < tempoUltimoClique + 0.5f) return;
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
        if (audioSource && lockedSound) audioSource.PlayOneShot(lockedSound);
        if (lockedMessage) lockedMessage.SetActive(true);
        yield return new WaitForSeconds(2.0f);
        if (lockedMessage) lockedMessage.SetActive(false);
    }
}
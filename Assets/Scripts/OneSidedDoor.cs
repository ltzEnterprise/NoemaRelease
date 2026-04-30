using UnityEngine;
using System.Collections;

public class OneSidedDoor : MonoBehaviour
{
    [Header("--- SAVE SYSTEM ---")]
    public string uniqueID; 

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
    
    private float tempoUltimoClique = 0f;
    private bool estaOlhando = false;
    private bool mostrandoErro = false;
    private bool inicializado = false;

    void Start()
    {
        if (string.IsNullOrEmpty(uniqueID)) 
            Debug.LogWarning($"[Aviso] Porta de um lado '{gameObject.name}' sem Unique ID! Ela não vai salvar se foi destrancada.");

        rotacaoFechada = transform.localRotation;
        rotacaoAberta = rotacaoFechada * Quaternion.Euler(0, openAngle, 0);

        if (lockedMessage) lockedMessage.SetActive(false);
        if (interactText) interactText.SetActive(false);

        StartCoroutine(CarregarEstadoSalvoSeguro());
    }

    IEnumerator CarregarEstadoSalvoSeguro()
    {
        if (PersistenciaManager.Instance != null)
            yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);

        CarregarEstadoSalvo();
        inicializado = true;
    }

    void CarregarEstadoSalvo()
    {
        if (PersistenciaManager.Instance == null || string.IsNullOrEmpty(uniqueID)) return;

        isUnlocked = PersistenciaManager.Instance.ObterEstado(uniqueID + "_unlocked", false);
        isOpen = PersistenciaManager.Instance.ObterEstado(uniqueID + "_open", false);

        if (isOpen)
            transform.localRotation = rotacaoAberta;
        else
            transform.localRotation = rotacaoFechada;
    }

    public void AoOlhar()
    {
        if (!inicializado) return;

        estaOlhando = true;

        if (!mostrandoErro && interactText)
            interactText.SetActive(true);
    }

    public void AoSair()
    {
        estaOlhando = false;

        if (interactText)
            interactText.SetActive(false);

        if (lockedMessage)
            lockedMessage.SetActive(false);
    }

    public void Interagir()
    {
        if (!inicializado) return;
        if (Time.time < tempoUltimoClique + 0.5f) return;
        if (mostrandoErro) return; 

        tempoUltimoClique = Time.time;

        if (interactText)
            interactText.SetActive(false);

        CheckSideAndInteract();
    }

    void Update()
    {
        Quaternion alvo = isOpen ? rotacaoAberta : rotacaoFechada;
        
        transform.localRotation = Quaternion.Slerp(transform.localRotation, alvo, speed * Time.deltaTime);

        if (physicsCollider != null)
        {
            bool isMoving = Quaternion.Angle(transform.localRotation, alvo) > 1.0f;
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
            
            if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
                PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_unlocked", true);

            ToggleDoor();
        }
    }

    void ToggleDoor()
    {
        isOpen = !isOpen;

        if (audioSource)
            audioSource.PlayOneShot(isOpen ? openSound : closeSound);

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_open", isOpen);
            SalvarProgressoSeguro();
        }
    }

    IEnumerator ShowLockedMessage()
    {
        mostrandoErro = true;

        if (audioSource && lockedSound)
            audioSource.PlayOneShot(lockedSound);

        if (lockedMessage)
            lockedMessage.SetActive(true);
        
        yield return new WaitForSeconds(2.0f);
        
        if (lockedMessage)
            lockedMessage.SetActive(false);
        
        mostrandoErro = false;

        if (estaOlhando && interactText)
            interactText.SetActive(true);
    }

    private void SalvarProgressoSeguro()
    {
        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(false);
    }
}
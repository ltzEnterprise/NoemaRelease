using UnityEngine;
using System.Collections;

public class SimpleDoor : MonoBehaviour
{
    [Header("--- SAVE SYSTEM ---")]
    [Tooltip("ID único da porta. Se vazio, usa o nome do objeto, mas o ideal é preencher manualmente.")]
    public string uniqueID = "";
    public bool salvarEstadoAberta = true;

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
    private bool isMoving = false;
    private bool taOlhando = false;

    private Quaternion rotacaoFechada;
    private Quaternion rotacaoAberta;
    private float tempoUltimoClique = 0f;
    private bool inicializado = false;

    void Start()
    {
        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = gameObject.name;

        rotacaoFechada = transform.localRotation;
        rotacaoAberta = rotacaoFechada * Quaternion.Euler(0, anguloDeAbertura, 0);

        if (interactText) interactText.SetActive(false);

        StartCoroutine(CarregarEstadoSeguro());
    }

    IEnumerator CarregarEstadoSeguro()
    {
        if (PersistenciaManager.Instance != null)
        {
            yield return new WaitUntil(() =>
                PersistenciaManager.Instance.DadosProntosParaUso &&
                !PersistenciaManager.Instance.EstaCarregando
            );

            if (salvarEstadoAberta && !string.IsNullOrEmpty(uniqueID))
            {
                isOpen = PersistenciaManager.Instance.ObterEstado(uniqueID + "_open", false);
                transform.localRotation = isOpen ? rotacaoAberta : rotacaoFechada;
            }
        }

        inicializado = true;
    }

    public void AoOlhar()
    {
        if (!inicializado) return;

        taOlhando = true;
        bool isBlocked = (blockerObject != null && blockerObject.activeSelf);

        if (interactText && !isBlocked && !isMoving)
            interactText.SetActive(true);
    }

    public void AoSair()
    {
        taOlhando = false;
        if (interactText) interactText.SetActive(false);
    }

    public void Interagir()
    {
        if (!inicializado) return;
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

        SalvarEstado();
    }

    void Update()
    {
        if (!inicializado) return;

        Quaternion alvo = isOpen ? rotacaoAberta : rotacaoFechada;

        transform.localRotation = Quaternion.Slerp(transform.localRotation, alvo, velocidade * Time.deltaTime);

        if (physicsCollider != null)
        {
            isMoving = Quaternion.Angle(transform.localRotation, alvo) > 1.0f;
            physicsCollider.isTrigger = isMoving;

            if (!isMoving && taOlhando && interactText && !interactText.activeSelf)
            {
                bool isBlocked = (blockerObject != null && blockerObject.activeSelf);
                if (!isBlocked) interactText.SetActive(true);
            }
        }
    }

    private void SalvarEstado()
    {
        if (!salvarEstadoAberta) return;
        if (PersistenciaManager.Instance == null) return;
        if (string.IsNullOrEmpty(uniqueID)) return;

        PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_open", isOpen);

        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        PersistenciaManager.Instance.SalvarTudo(false);
    }
}
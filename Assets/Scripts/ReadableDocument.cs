using UnityEngine;

public class ReadableDocument : MonoBehaviour
{
    [Header("--- SAVE SYSTEM (OPCIONAL) ---")]
    [Tooltip("Deixe em branco para documentos normais. Preencha se o documento começar bloqueado e precisar salvar que foi liberado.")]
    public string uniqueID; 

    [Header("--- UI CONFIG ---")]
    public GameObject uiInteractionPrompt; 
    public GameObject uiContentPanel;      

    [Header("--- AUDIO ---")]
    public AudioClip soundEffect;          

    [Header("--- BLOQUEIO DE CENÁRIO ---")]
    public bool comecaBloqueado = false;
    
    // Estado Interno
    private bool isReading = false;
    private bool isBlocked = false;
    private bool estaSendoOlhado = false; 
    
    private AudioSource audioSource;
    private float tempoBloqueio = 0f; 

    void Awake()
    {
        isBlocked = comecaBloqueado; 
    }

    void Start()
    {
        if (uiInteractionPrompt) uiInteractionPrompt.SetActive(false);
        if (uiContentPanel) uiContentPanel.SetActive(false);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; 
        audioSource.ignoreListenerPause = true; 

        // Confere se já foi liberado em algum save anterior
        if (!string.IsNullOrEmpty(uniqueID) && PersistenciaManager.Instance != null)
        {
            if (PersistenciaManager.Instance.ObterEstado(uniqueID + "_unlocked"))
            {
                isBlocked = false;
            }
        }
    }

    public void LiberarDocumento()
    {
        isBlocked = false;

        // Se tem ID, avisa o save que esse documento tá livre pra sempre
        if (!string.IsNullOrEmpty(uniqueID) && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_unlocked", true);
            // Não chamo o SalvarTudo() aqui porque quem libera o documento (ex: Vitrola) já deve estar salvando o jogo.
        }
    }

    void Update()
    {
        if (uiInteractionPrompt != null)
        {
            bool deveAparecer = (estaSendoOlhado && !isBlocked && !isReading);

            if (uiInteractionPrompt.activeSelf != deveAparecer)
            {
                uiInteractionPrompt.SetActive(deveAparecer);
            }
        }

        if (isReading && Time.unscaledTime > tempoBloqueio)
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            {
                tempoBloqueio = Time.unscaledTime + 0.2f; 
                ToggleReading(false);
            }
        }
    }

    public void AoOlhar()
    {
        estaSendoOlhado = true; 
    }

    public void AoSair()
    {
        estaSendoOlhado = false; 
    }

    public void Interagir()
    {
        if (isBlocked) return; 

        if (!isReading && Time.unscaledTime > tempoBloqueio)
        {
            tempoBloqueio = Time.unscaledTime + 0.2f;
            ToggleReading(true);
        }
    }

    void ToggleReading(bool state)
    {
        isReading = state;

        if (isReading)
        {
            if (soundEffect && audioSource) audioSource.PlayOneShot(soundEffect);
            if (uiContentPanel) uiContentPanel.SetActive(true);
            if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(true, true);
            
            Time.timeScale = 0f; 
        }
        else
        {
            if (uiContentPanel) uiContentPanel.SetActive(false);
            if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(false, false);
            
            Time.timeScale = 1f; 
        }
    }
}
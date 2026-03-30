using UnityEngine;

public class ReadableDocument : MonoBehaviour
{
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
        // TEM QUE SER NO AWAKE! Se deixar no Start(), ele re-bloqueia o papel sozinho ao carregar o save.
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
    }

    public void LiberarDocumento()
    {
        isBlocked = false;
    }

    void Update()
    {
        // 1. O SISTEMA À PROVA DE BALAS DO TEXTO:
        // O Update força a interface a obedecer a matemática atualizada no exato frame.
        if (uiInteractionPrompt != null)
        {
            // A regra: Só acende se estiver sendo olhado, NÃO estiver bloqueado e NÃO estiver aberto.
            bool deveAparecer = (estaSendoOlhado && !isBlocked && !isReading);

            // Só mexe no SetActive se precisar (pra não fritar a CPU)
            if (uiInteractionPrompt.activeSelf != deveAparecer)
            {
                uiInteractionPrompt.SetActive(deveAparecer);
            }
        }

        // 2. Lógica de Fechar o papel
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
        // Agora ele só avisa que a mira encostou. O Update cuida do resto.
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
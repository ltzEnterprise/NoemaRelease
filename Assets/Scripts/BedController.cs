using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.SceneManagement;
using System.Collections; 
using QuantumTek.SimpleMenu; 

public class BedController : MonoBehaviour
{
    [Header("Configurações Base da Cama")]
    public string dreamSceneName;
    
    [Header("--- FINAL DO JOGO (OPCIONAL) ---")]
    [Tooltip("Marque isso APENAS na última cama do jogo. Vai salvar tudo antes de ir pro mapa infinito.")]
    public bool isFinalSave = false;
    public Transform playerTransform; 
    
    [Header("Interface")]
    public GameObject warningText; 

    [Header("--- INTERAÇÃO (NOVO) ---")]
    public float distanciaInteracao = 3f;
    public KeyCode botaoInteragir = KeyCode.E;

    [Header("--- EFEITO DE DORMIR ---")]
    public Image blackScreen; 
    public AudioSource sleepSound; 
    public float blackFadeTime = 2f;

    [Header("--- TELA DE LOADING ---")]
    public GameObject loadingPanel;          
    public SM_Bar progressBar; 

    [Header("--- ÁUDIO AMBIENTE (OPCIONAL) ---")]
    public AudioSource ambientMusic;
    public float musicFadeTime = 1.5f;

    private bool isGoingToSleep = false; 
    private bool isLookingAtBed = false; // Controla pra não ficar piscando a UI atoa
    private Camera mainCam;

    void Start()
    {
        if (warningText != null) warningText.SetActive(false);
        mainCam = Camera.main; // Puxa a câmera principal do jogador automaticamente
    }

    void Update()
    {
        // Se já tiver dormindo ou não achar a câmera, nem tenta rodar a mira
        if (isGoingToSleep || mainCam == null) return;

        // O "Laser" que sai do exato centro da tela pra frente
        Ray ray = new Ray(mainCam.transform.position, mainCam.transform.forward);
        RaycastHit hit;

        bool isCurrentlyLooking = false;

        // Atira o laser. Se bater em algo dentro da distância...
        if (Physics.Raycast(ray, out hit, distanciaInteracao))
        {
            // Verifica se o que o laser bateu é EXATAMENTE o collider dessa cama
            if (hit.collider.gameObject == this.gameObject)
            {
                isCurrentlyLooking = true;

                // Se apertar a tecla configurada (E), dorme
                if (Input.GetKeyDown(botaoInteragir))
                {
                    Interagir();
                }
            }
        }

        // Liga e desliga a UI do texto com base no laser
        if (isCurrentlyLooking && !isLookingAtBed)
        {
            isLookingAtBed = true;
            AoOlhar();
        }
        else if (!isCurrentlyLooking && isLookingAtBed)
        {
            isLookingAtBed = false;
            AoSair();
        }
    }

    public void AoOlhar()
    {
        if (isGoingToSleep) return;
        if (warningText != null) warningText.SetActive(true);
    }

    public void AoSair()
    {
        if (warningText != null) warningText.SetActive(false);
    }

    public void Interagir()
    {
        if (isGoingToSleep) return;
        SleepRoutine();
    }

    void SleepRoutine()
    {
        isGoingToSleep = true; 
        
        if (warningText != null) warningText.SetActive(false); 

        if (isFinalSave)
        {
            if (playerTransform != null)
            {
                PlayerPrefs.SetFloat("Player3D_PosX", playerTransform.position.x);
                PlayerPrefs.SetFloat("Player3D_PosY", playerTransform.position.y);
                PlayerPrefs.SetFloat("Player3D_PosZ", playerTransform.position.z);
                PlayerPrefs.SetFloat("Player3D_RotY", playerTransform.eulerAngles.y);
                PlayerPrefs.SetInt("Player3D_HasSave", 1);
            }
            
            if (PersistenciaManager.Instance != null)
            {
                PersistenciaManager.Instance.SalvarTudo();
            }
            Debug.Log("Cama Final: Progresso salvo no HD antes de carregar a última cena.");
        }

        if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(true, false);

        StartCoroutine(FadeAndLoadRoutine(dreamSceneName));
    }

    public void TogglePanel(GameObject panel, bool state)
    {
        if (panel == null) return;
        
        panel.SetActive(state);
        
        SM_Window window = panel.GetComponent<SM_Window>();
        if (window != null) window.Toggle(state);

        if (state == true)
        {
            foreach (Transform child in panel.transform)
            {
                child.gameObject.SetActive(true);
            }
        }
    }

    IEnumerator FadeAndLoadRoutine(string sceneName)
    {
        if (blackScreen != null)
        {
            blackScreen.gameObject.SetActive(true);
            Color cor = blackScreen.color;
            cor.a = 0f;
            blackScreen.color = cor;
        }

        if (sleepSound != null) sleepSound.Play();

        float elapsedTime = 0f;
        while (elapsedTime < blackFadeTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            if (blackScreen != null)
            {
                Color cor = blackScreen.color;
                cor.a = Mathf.Lerp(0f, 1f, elapsedTime / blackFadeTime);
                blackScreen.color = cor;
            }
            yield return null;
        }

        if (blackScreen != null)
        {
            Color finalColor = blackScreen.color;
            finalColor.a = 1f;
            blackScreen.color = finalColor;
        }

        StartCoroutine(LoadingPercentageRoutine(sceneName));
    }

    IEnumerator FadeOutMusic()
    {
        if (ambientMusic == null) yield break;
        
        float initialVolume = ambientMusic.volume;
        float elapsedTime = 0f;

        while (elapsedTime < musicFadeTime)
        {
            elapsedTime += Time.unscaledDeltaTime;
            ambientMusic.volume = Mathf.Lerp(initialVolume, 0f, elapsedTime / musicFadeTime);
            yield return null;
        }
        ambientMusic.volume = 0f;
    }

    IEnumerator LoadingPercentageRoutine(string sceneName)
    {
        StartCoroutine(FadeOutMusic()); 
        
        TogglePanel(loadingPanel, true);

        if (progressBar) progressBar.SetFill(0f); 

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false; 

        float visualProgress = 0f;

        while (visualProgress < 0.8f)
        {
            float realProgress = Mathf.Clamp01(operation.progress / 0.9f) * 0.8f;
            visualProgress = Mathf.MoveTowards(visualProgress, realProgress, Time.unscaledDeltaTime * 0.8f); 

            if (progressBar) progressBar.SetFill(visualProgress);

            if (operation.progress >= 0.9f && visualProgress >= 0.79f)
            {
                visualProgress = 0.8f;
                if (progressBar) progressBar.SetFill(visualProgress);
                break;
            }

            yield return null;
        }

        float extraTime = 0f;
        while (extraTime < 2f)
        {
            extraTime += Time.unscaledDeltaTime;
            visualProgress = Mathf.Lerp(0.8f, 1f, extraTime / 2f); 
            
            if (progressBar) progressBar.SetFill(visualProgress);
            
            yield return null;
        }

        operation.allowSceneActivation = true;
    }
}
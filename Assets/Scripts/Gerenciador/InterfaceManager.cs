using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using QuantumTek.SimpleMenu;

public class InterfaceManager : MonoBehaviour
{
    [Header("--- DEV MODE ---")]
    public bool modoDesenvolvedor = false; 

    [Header("--- REFERÊNCIAS ---")]
    public GameObject contentOpcoes; 
    public GameObject painelConfirmacaoReset;
    public GameObject painelConfirmacaoBackup; 

    [Header("--- PAINÉIS ---")]
    public GameObject painelMenuPrincipal;
    public GameObject painelSlots;
    public GameObject painelOpcoes;
    public GameObject painelPause;

    [Header("--- TELA DE LOADING ---")]
    public GameObject painelLoading;          
    public SM_Bar barraDeProgresso; 
    public string nomeDaCenaDoJogo = "CenaPrincipal3D"; 

    [Header("--- LOADING POLIMENTO ---")]
    [Tooltip("Tempo mínimo que a barra fica preenchendo de 80% até 100%.")]
    public float tempoExtraLoading = 2f;

    [Tooltip("Depois que a cena ativa, espera esse tempo antes de remover o loading. Ajuda post-processing/câmera estabilizarem.")]
    public float esperaDepoisDaCenaPronta = 0.25f;

    [Tooltip("Fade suave para tirar a tela de loading depois que a cena já está pronta.")]
    public float fadeOutLoadingFinal = 0.35f;

    [Tooltip("Se true, espera GameManager.CenaPronta antes de esconder o loading em cenas de jogo.")]
    public bool esperarGameManagerCenaPronta = true;

    [Header("--- UI DOS SLOTS ---")]
    public TextMeshProUGUI[] textosDosSlots;    
    public TextMeshProUGUI[] textosBotaoApagar; 

    [Header("--- TRADUÇÕES DO SCRIPT ---")]
    public string textoNovoJogo_PT = "NOVO JOGO";
    public string textoNovoJogo_EN = "NEW GAME";
    
    public string textoApagar_PT = "APAGAR";
    public string textoApagar_EN = "DELETE";
    
    public string textoConfirmar_PT = "CONFIRMAR";
    public string textoConfirmar_EN = "CONFIRM";
    
    public string textoApagarSave_PT = "APAGAR SAVE?";
    public string textoApagarSave_EN = "DELETE SAVE?";
    
    public string textoSlot_PT = "SLOT";
    public string textoSlot_EN = "SLOT";

    [Header("--- PAUSE ---")]
    public RawImage imagemCongelada; 

    [Header("--- JOGO ---")]
    public FPS_Master playerMaster; 
    public bool isCenaDeJogo = false; 
    public bool mouseLivreNoJogo = false; 

    [Header("--- ÁUDIO ---")]
    public AudioSource musicaDoMenu;
    public AudioClip clipeMusicaMenu; 
    [Range(0f, 1f)] public float volumeMaximoMusica = 1f; 
    public float tempoDeFade = 1.5f;

    [HideInInspector] public bool jogoPausado = false;
    
    [HideInInspector] public int slotConfirmacao = -1;
    [HideInInspector] public int slotParaRestaurar = -1; 

    private Vector3 escalaOpcoes = Vector3.one;
    private bool escalaSalva = false;
    private bool isProcessandoPause = false; 
    private bool estaSaindoParaMenu = false; 

    private enum EstadoInterface { Menu, Slots, Opcoes, Jogando, Pausado, Loading }
    private EstadoInterface estadoAtual;

    private Coroutine loadingAtual;
    private Coroutine fadeAtual;

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (contentOpcoes != null)
        {
            escalaOpcoes = contentOpcoes.transform.localScale;
            escalaSalva = true;
        }

        if (SaveSlotManager.Instance != null)
            SaveSlotManager.Instance.ForcarAutoSizeCentral();

        PrepararPainelLoadingInicial();
        LigarDesligarPainel(painelLoading, false);

        if (painelConfirmacaoBackup != null)
            LigarDesligarPainel(painelConfirmacaoBackup, false);
        
        if (imagemCongelada != null) 
        {
            imagemCongelada.gameObject.SetActive(false);
            ClearFreezeTexture(); 
            ForcarTelaCheia(imagemCongelada.rectTransform);

            Canvas canvasFundo = imagemCongelada.gameObject.GetComponent<Canvas>();

            if (canvasFundo == null)
                canvasFundo = imagemCongelada.gameObject.AddComponent<Canvas>();

            canvasFundo.overrideSorting = true;
            canvasFundo.sortingOrder = 9998; 
        }

        if (isCenaDeJogo)
        {
            estadoAtual = EstadoInterface.Jogando;

            Cursor.lockState = mouseLivreNoJogo ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = mouseLivreNoJogo;

            if (FPS_Master.Instance != null)
                FPS_Master.travadoInteracao = false;      

            LigarDesligarPainel(painelPause, false);
            LigarDesligarPainel(painelOpcoes, false);
            LigarDesligarPainel(painelMenuPrincipal, false);
            LigarDesligarPainel(painelSlots, false);

            if (painelOpcoes != null)
            {
                SettingsManager settings = painelOpcoes.GetComponentInChildren<SettingsManager>(true);
                if (settings != null) settings.LoadAndApplyAllSettings();
            }

            StartCoroutine(SequenciaInicializacaoJogo());
        }
        else
        {
            estadoAtual = EstadoInterface.Menu;

            Cursor.lockState = CursorLockMode.None;   
            Cursor.visible = true;                    

            LigarDesligarPainel(painelMenuPrincipal, true);
            LigarDesligarPainel(painelSlots, false);
            LigarDesligarPainel(painelOpcoes, false);
            LigarDesligarPainel(painelPause, false);
            
            StartCoroutine(AtualizarTextosMenuSeguro());

            if (musicaDoMenu != null && clipeMusicaMenu != null)
            {
                musicaDoMenu.clip = clipeMusicaMenu;
                musicaDoMenu.volume = volumeMaximoMusica; 
                musicaDoMenu.loop = true;

                if (!musicaDoMenu.isPlaying)
                    musicaDoMenu.Play();
            }
        }
    }

    IEnumerator AtualizarTextosMenuSeguro()
    {
        yield return new WaitForSecondsRealtime(0.1f);

        if (SaveSlotManager.Instance != null)
            SaveSlotManager.Instance.AtualizarTextosSlots();

        yield return new WaitForSecondsRealtime(0.4f);

        if (SaveSlotManager.Instance != null)
            SaveSlotManager.Instance.AtualizarTextosSlots();
    }

    public void IniciarLoadingParaCena(string cenaAlvo)
    {
        if (string.IsNullOrEmpty(cenaAlvo))
        {
            Debug.LogError("[InterfaceManager] Tentando carregar cena vazia.");
            return;
        }

        if (loadingAtual != null)
            StopCoroutine(loadingAtual);

        loadingAtual = StartCoroutine(RotinaLoadingPorcentagem(cenaAlvo));
    }

    public void LigarDesligarPainel(GameObject painel, bool estado)
    {
        if (painel == null) return;

        Animator animPai = painel.GetComponent<Animator>();
        if (animPai != null) animPai.enabled = false;

        painel.SetActive(estado);

        CanvasGroup cgPainel = painel.GetComponent<CanvasGroup>();
        if (cgPainel != null && painel == painelLoading && estado)
        {
            cgPainel.alpha = 1f;
            cgPainel.interactable = true;
            cgPainel.blocksRaycasts = true;
        }

        SM_Window janela = painel.GetComponent<SM_Window>();

        if (janela != null)
        {
            if (janela.content != null)
            {
                Animator animContent = janela.content.GetComponent<Animator>();
                if (animContent != null) animContent.enabled = false;

                janela.content.gameObject.SetActive(estado);
            }

            janela.Toggle(estado);
        }

        if (painel == painelOpcoes && estado)
        {
            if (contentOpcoes != null)
            {
                Animator animOpcoes = contentOpcoes.GetComponent<Animator>();
                if (animOpcoes != null) animOpcoes.enabled = false;

                contentOpcoes.SetActive(true);

                CanvasGroup cg = contentOpcoes.GetComponent<CanvasGroup>();

                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.interactable = true;
                    cg.blocksRaycasts = true;
                }
                
                if (escalaSalva)
                    contentOpcoes.transform.localScale = escalaOpcoes;
            }

            SettingsManager settings = painel.GetComponentInChildren<SettingsManager>(true);

            if (settings != null)
            {
                settings.enabled = true;
                settings.LoadAndApplyAllSettings();

                if (settings.controladorDeAbas != null && settings.janelaGameplay != null)
                {
                    Animator animTabGroup = settings.controladorDeAbas.GetComponent<Animator>();

                    if (animTabGroup != null)
                        animTabGroup.enabled = false;

                    settings.controladorDeAbas.ChangeTab(settings.janelaGameplay);

                    Transform tabContent = settings.janelaGameplay.content;

                    if (tabContent != null)
                    {
                        Animator animTab = tabContent.GetComponent<Animator>();

                        if (animTab != null)
                            animTab.enabled = false;

                        tabContent.gameObject.SetActive(true);
                        
                        CanvasGroup cgTab = tabContent.GetComponent<CanvasGroup>();

                        if (cgTab != null)
                        {
                            cgTab.alpha = 1f;
                            cgTab.interactable = true;
                            cgTab.blocksRaycasts = true;
                        }
                        
                        tabContent.localScale = Vector3.one; 
                    }
                }
            }
        }
    }

    void ForcarTelaCheia(RectTransform rt)
    {
        if (rt == null) return;

        rt.anchorMin = Vector2.zero; 
        rt.anchorMax = Vector2.one;  
        rt.pivot = new Vector2(0.5f, 0.5f); 
        rt.sizeDelta = Vector2.zero; 
        rt.anchoredPosition = Vector2.zero; 
    }

    public void BotaoJogar_AbreSlots()
    {
        estadoAtual = EstadoInterface.Slots;
        slotConfirmacao = -1;

        LigarDesligarPainel(painelMenuPrincipal, false);
        LigarDesligarPainel(painelOpcoes, false);
        LigarDesligarPainel(painelPause, false);
        LigarDesligarPainel(painelSlots, true);

        if (SaveSlotManager.Instance != null)
            SaveSlotManager.Instance.AtualizarTextosSlots();
    }

    public void AbrirOpcoes()
    {
        estadoAtual = EstadoInterface.Opcoes;

        LigarDesligarPainel(painelMenuPrincipal, false);
        LigarDesligarPainel(painelPause, false);
        LigarDesligarPainel(painelSlots, false);
        LigarDesligarPainel(painelOpcoes, true);
    }

    public void BotaoSair() 
    { 
        if (isCenaDeJogo && playerMaster != null && SistemaGlobal.Instance != null)
        {
            SistemaGlobal.Instance.SalvarJogo(playerMaster.transform.position, SceneManager.GetActiveScene().name);
        }
        else if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarTudo(false);
        }

        Application.Quit(); 
    }

    public void BotaoVoltarGenerico()
    {
        if (estadoAtual == EstadoInterface.Opcoes)
        {
            FecharOpcoesVoltar();
        }
        else if (estadoAtual == EstadoInterface.Slots)
        {
            estadoAtual = EstadoInterface.Menu;
            slotConfirmacao = -1;

            LigarDesligarPainel(painelSlots, false);
            LigarDesligarPainel(painelMenuPrincipal, true);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (painelConfirmacaoBackup != null && painelConfirmacaoBackup.activeSelf)
            {
                if (SaveSlotManager.Instance != null)
                    SaveSlotManager.Instance.BOTAO_CANCELAR_BACKUP_TELA_PRETA();

                return;
            }

            if (painelConfirmacaoReset != null && painelConfirmacaoReset.activeSelf)
            {
                FecharJanelaConfirmacao();
                return; 
            }

            HandleEscape();
        }

        if (Input.GetKeyDown(KeyCode.R) && estadoAtual == EstadoInterface.Opcoes)
        {
            if (painelConfirmacaoReset != null && !painelConfirmacaoReset.activeSelf)
                LigarDesligarPainel(painelConfirmacaoReset, true);
        }
    }

    private void HandleEscape()
    {
        if (estadoAtual == EstadoInterface.Loading) return;

        switch (estadoAtual)
        {
            case EstadoInterface.Opcoes:
                FecharOpcoesVoltar();
                break;

            case EstadoInterface.Jogando:
                if (!isProcessandoPause)
                    StartCoroutine(PausarComPrint());
                break;

            case EstadoInterface.Pausado:
                if (!isProcessandoPause)
                    ResumeJogo();
                break;

            case EstadoInterface.Slots:
                BotaoVoltarGenerico();
                break;
        }
    }
    
    public void FecharJanelaConfirmacao()
    {
        LigarDesligarPainel(painelConfirmacaoReset, false);
    }

    public void BotaoConfirmarReset()
    {
        PlayerPrefs.DeleteKey("MouseSensitivity");
        PlayerPrefs.DeleteKey("PlayerFOV");
        PlayerPrefs.Save();

        if (FPS_Master.Instance != null)
            FPS_Master.Instance.CarregarConfiguracoes();

        if (painelOpcoes != null)
        {
            SettingsManager settings = painelOpcoes.GetComponentInChildren<SettingsManager>(true);

            if (settings != null)
                settings.LoadAndApplyAllSettings();
        }

        FecharJanelaConfirmacao();
    }

    IEnumerator PausarComPrint()
    {
        isProcessandoPause = true; 
        
        if (imagemCongelada != null)
        {
            yield return new WaitForEndOfFrame();
            CapturarTela();
        }
        
        PausarJogoLogica();

        isProcessandoPause = false; 
    }

    private void CapturarTela()
    {
        if (imagemCongelada == null) return;

        ClearFreezeTexture();

        int width = Screen.width;
        int height = Screen.height;

        RenderTexture rt = RenderTexture.GetTemporary(width, height, 0);
        ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);

        RenderTexture.active = rt;

        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        imagemCongelada.texture = tex;
        
        imagemCongelada.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        imagemCongelada.rectTransform.localScale = new Vector3(1f, -1f, 1f);
        
        imagemCongelada.gameObject.SetActive(true);
    }

    public void PausarJogoLogica()
    {
        estadoAtual = EstadoInterface.Pausado;
        jogoPausado = true; 

        LigarDesligarPainel(painelPause, true);

        Time.timeScale = 0f;
        AudioListener.pause = true; 

        if (FPS_Master.Instance != null)
            FPS_Master.travadoInteracao = true;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeJogo()
    {
        estadoAtual = EstadoInterface.Jogando;
        isProcessandoPause = false; 
        jogoPausado = false; 
        
        if (imagemCongelada != null) 
        {
            imagemCongelada.gameObject.SetActive(false);
            ClearFreezeTexture();
        }
        
        LigarDesligarPainel(painelOpcoes, false);
        LigarDesligarPainel(painelPause, false);

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (FPS_Master.Instance != null)
            FPS_Master.travadoInteracao = false;

        Cursor.lockState = mouseLivreNoJogo ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = mouseLivreNoJogo;
    }

    public void SairParaMenuPrincipal()
    {
        if (estaSaindoParaMenu) return;
        StartCoroutine(SairParaMenuSeguro());
    }

    IEnumerator SairParaMenuSeguro()
    {
        estaSaindoParaMenu = true;
        estadoAtual = EstadoInterface.Loading;

        Time.timeScale = 1f; 
        AudioListener.pause = false; 

        if (FPS_Master.Instance != null)
            FPS_Master.travadoInteracao = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (painelPause != null) LigarDesligarPainel(painelPause, false);
        if (painelOpcoes != null) LigarDesligarPainel(painelOpcoes, false);

        yield return StartCoroutine(FadePretoAntesDoMenu(0.25f));

        if (isCenaDeJogo && playerMaster != null && SistemaGlobal.Instance != null)
        {
            SistemaGlobal.Instance.SalvarJogo(playerMaster.transform.position, SceneManager.GetActiveScene().name);
        }
        else if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarTudo(false);
        }

        yield return null;

        SceneManager.LoadScene("MenuPrincipal");
    }

    IEnumerator FadePretoAntesDoMenu(float duracao)
    {
        GameObject obj = new GameObject("FadePretoSairMenu");
        Canvas canvas = obj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32767;

        CanvasGroup cg = obj.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = true;

        GameObject imgObj = new GameObject("ImagemPreta");
        imgObj.transform.SetParent(obj.transform, false);

        Image img = imgObj.AddComponent<Image>();
        img.color = Color.black;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        float t = 0f;

        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(t / Mathf.Max(0.01f, duracao));
            yield return null;
        }

        cg.alpha = 1f;
    }

    public void FecharOpcoesVoltar()
    {
        LigarDesligarPainel(painelOpcoes, false);

        if (isCenaDeJogo) 
        {
            estadoAtual = EstadoInterface.Pausado;
            LigarDesligarPainel(painelPause, true);
        }
        else 
        {
            estadoAtual = EstadoInterface.Menu;
            LigarDesligarPainel(painelMenuPrincipal, true);
        }
    }

    IEnumerator FadeOutMusica()
    {
        if (musicaDoMenu == null)
            yield break;
        
        float volumeInicial = musicaDoMenu.volume;
        float tempoPassado = 0f;

        while (tempoPassado < tempoDeFade)
        {
            tempoPassado += Time.unscaledDeltaTime;
            musicaDoMenu.volume = Mathf.Lerp(volumeInicial, 0f, tempoPassado / tempoDeFade);
            yield return null;
        }

        musicaDoMenu.volume = 0f;
    }

    IEnumerator RotinaLoadingPorcentagem(string nomeCena)
    {
        estadoAtual = EstadoInterface.Loading;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        
        if (fadeAtual != null)
            StopCoroutine(fadeAtual);

        fadeAtual = StartCoroutine(FadeOutMusica());
        
        PrepararPainelLoadingInicial();
        LigarDesligarPainel(painelLoading, true);
        LigarDesligarPainel(painelSlots, false);
        LigarDesligarPainel(painelMenuPrincipal, false);
        LigarDesligarPainel(painelOpcoes, false);
        LigarDesligarPainel(painelPause, false);

        if (barraDeProgresso != null)
            barraDeProgresso.SetFill(0f); 

        AsyncOperation operacao = SceneManager.LoadSceneAsync(nomeCena);

        if (operacao == null)
        {
            Debug.LogError("[InterfaceManager] Falha ao iniciar carregamento da cena: " + nomeCena);
            yield break;
        }

        operacao.allowSceneActivation = false; 

        float progressoVisual = 0f;

        while (progressoVisual < 0.8f)
        {
            float progressoReal = Mathf.Clamp01(operacao.progress / 0.9f) * 0.8f;
            progressoVisual = Mathf.MoveTowards(progressoVisual, progressoReal, Time.unscaledDeltaTime * 0.8f); 

            if (barraDeProgresso != null)
                barraDeProgresso.SetFill(progressoVisual);

            if (operacao.progress >= 0.9f && progressoVisual >= 0.79f)
                break;

            yield return null;
        }

        float tempoExtra = 0f;

        while (tempoExtra < tempoExtraLoading)
        {
            tempoExtra += Time.unscaledDeltaTime;
            progressoVisual = Mathf.Lerp(0.8f, 1f, tempoExtra / Mathf.Max(0.01f, tempoExtraLoading)); 

            if (barraDeProgresso != null)
                barraDeProgresso.SetFill(progressoVisual);

            yield return null;
        }

        if (barraDeProgresso != null)
            barraDeProgresso.SetFill(1f);

        operacao.allowSceneActivation = true;

        yield return new WaitUntil(() => operacao.isDone);

        yield return null;
        yield return new WaitForEndOfFrame();

        bool carregouMenu = SceneManager.GetActiveScene().name == "MenuPrincipal";

        if (!carregouMenu && esperarGameManagerCenaPronta && GameManager.Instance != null)
        {
            float timeout = 5f;
            while (!GameManager.CenaPronta && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }
        }

        if (esperaDepoisDaCenaPronta > 0f)
            yield return new WaitForSecondsRealtime(esperaDepoisDaCenaPronta);

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (fadeOutLoadingFinal > 0f)
            yield return StartCoroutine(FadeOutPainelLoadingFinal());
        else
            LigarDesligarPainel(painelLoading, false);

        loadingAtual = null;
    }

    private void PrepararPainelLoadingInicial()
    {
        if (painelLoading == null) return;

        Canvas canvas = painelLoading.GetComponent<Canvas>();
        if (canvas == null) canvas = painelLoading.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = 10000;

        CanvasGroup cg = painelLoading.GetComponent<CanvasGroup>();
        if (cg == null) cg = painelLoading.AddComponent<CanvasGroup>();

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    IEnumerator FadeOutPainelLoadingFinal()
    {
        if (painelLoading == null)
            yield break;

        CanvasGroup cg = painelLoading.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            LigarDesligarPainel(painelLoading, false);
            yield break;
        }

        cg.interactable = false;
        cg.blocksRaycasts = true;

        float t = 0f;
        float alphaInicial = cg.alpha;

        while (t < fadeOutLoadingFinal)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(alphaInicial, 0f, t / fadeOutLoadingFinal);
            yield return null;
        }

        cg.alpha = 0f;
        cg.blocksRaycasts = false;

        LigarDesligarPainel(painelLoading, false);

        cg.alpha = 1f;
        cg.interactable = true;
        cg.blocksRaycasts = true;
    }

    IEnumerator SequenciaInicializacaoJogo()
    {
        yield return new WaitUntil(() => PersistenciaManager.Instance != null);
        yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);
        yield return new WaitForEndOfFrame();
        
        if (SistemaGlobal.Instance != null &&
            playerMaster != null &&
            SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar)
        {
            EstadoGlobal.CarregarDoSlot(SistemaGlobal.Instance.slotAtual);

            string p = "Slot_" + SistemaGlobal.Instance.slotAtual;
            
            if (PersistenciaManager.Instance.TemFloat(p + "_PosX"))
            {
                float x = PersistenciaManager.Instance.ObterFloat(p + "_PosX");
                float y = PersistenciaManager.Instance.ObterFloat(p + "_PosY");
                float z = PersistenciaManager.Instance.ObterFloat(p + "_PosZ");

                playerMaster.Teleportar(new Vector3(x, y + 0.1f, z));
                Physics.SyncTransforms();
            }
            
            SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = false; 
        }
    }

    private void ClearFreezeTexture()
    {
        if (imagemCongelada != null && imagemCongelada.texture != null)
        {
            Texture texturaAntiga = imagemCongelada.texture;
            imagemCongelada.texture = null;
            Destroy(texturaAntiga); 
        }
    }

    private void OnDisable()
    {
        if (!estaSaindoParaMenu && estadoAtual != EstadoInterface.Loading)
            ClearFreezeTexture();
    }

    public void ClicarNoSlot(int slot)
    {
        if (SaveSlotManager.Instance != null)
            SaveSlotManager.Instance.BOTAO_JOGAR_SLOT(slot);
    }

    public void BotaoApagarSlot(int slot)
    {
        if (SaveSlotManager.Instance != null)
            SaveSlotManager.Instance.BOTAO_APAGAR_SLOT(slot);
    }

    public void CancelarConfirmacaoDelete()
    {
        if (SaveSlotManager.Instance != null)
            SaveSlotManager.Instance.BOTAO_CANCELAR_APAGAR();
    }

    public void BotaoPedirRestauracao(int slot)
    {
        if (SaveSlotManager.Instance != null)
            SaveSlotManager.Instance.BOTAO_PREPARAR_BACKUP_SLOT(slot);
    }

    public void ConfirmarRestauracaoBackup()
    {
        if (SaveSlotManager.Instance != null)
            SaveSlotManager.Instance.BOTAO_CONFIRMAR_BACKUP_TELA_PRETA();
    }

    public void CancelarRestauracaoBackup()
    {
        if (SaveSlotManager.Instance != null)
            SaveSlotManager.Instance.BOTAO_CANCELAR_BACKUP_TELA_PRETA();
    }
}
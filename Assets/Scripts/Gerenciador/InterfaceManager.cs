using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;
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
    
    private int slotConfirmacao = -1;
    private int slotParaRestaurar = -1; 

    private Vector3 escalaOpcoes = Vector3.one;
    private bool escalaSalva = false;
    private bool isProcessandoPause = false; 

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

        ForcarAutoSizeCentral();

        LigarDesligarPainel(painelLoading, false);
        if (painelConfirmacaoBackup) LigarDesligarPainel(painelConfirmacaoBackup, false);
        
        if (imagemCongelada) 
        {
            imagemCongelada.gameObject.SetActive(false);
            ClearFreezeTexture(); 
            ForcarTelaCheia(imagemCongelada.rectTransform);

            Canvas canvasFundo = imagemCongelada.gameObject.GetComponent<Canvas>();
            if (canvasFundo == null) canvasFundo = imagemCongelada.gameObject.AddComponent<Canvas>();
            canvasFundo.overrideSorting = true;
            canvasFundo.sortingOrder = -100; 
        }

        if (isCenaDeJogo)
        {
            estadoAtual = EstadoInterface.Jogando;
            Cursor.lockState = mouseLivreNoJogo ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = mouseLivreNoJogo;

            if (FPS_Master.Instance != null) FPS_Master.travadoInteracao = false;      

            LigarDesligarPainel(painelPause, false);
            LigarDesligarPainel(painelOpcoes, false);
            LigarDesligarPainel(painelMenuPrincipal, false);

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
            
            AtualizarTextosSlots();

            if (musicaDoMenu != null && clipeMusicaMenu != null)
            {
                musicaDoMenu.clip = clipeMusicaMenu;
                musicaDoMenu.volume = volumeMaximoMusica; 
                musicaDoMenu.loop = true;
                if (!musicaDoMenu.isPlaying) musicaDoMenu.Play();
            }
        }
    }

    public void LigarDesligarPainel(GameObject painel, bool estado)
    {
        if (painel == null) return;

        Animator animPai = painel.GetComponent<Animator>();
        if (animPai != null) animPai.enabled = false;

        painel.SetActive(estado);

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
                if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
                
                if (escalaSalva) contentOpcoes.transform.localScale = escalaOpcoes;
            }

            SettingsManager settings = painel.GetComponentInChildren<SettingsManager>(true);
            if (settings != null)
            {
                settings.enabled = true;
                settings.LoadAndApplyAllSettings();

                if (settings.controladorDeAbas != null && settings.janelaGameplay != null)
                {
                    Animator animTabGroup = settings.controladorDeAbas.GetComponent<Animator>();
                    if (animTabGroup != null) animTabGroup.enabled = false;

                    settings.controladorDeAbas.ChangeTab(settings.janelaGameplay);

                    Transform tabContent = settings.janelaGameplay.content;
                    if (tabContent != null)
                    {
                        Animator animTab = tabContent.GetComponent<Animator>();
                        if (animTab != null) animTab.enabled = false;

                        tabContent.gameObject.SetActive(true);
                        
                        CanvasGroup cgTab = tabContent.GetComponent<CanvasGroup>();
                        if (cgTab != null) { cgTab.alpha = 1f; cgTab.interactable = true; cgTab.blocksRaycasts = true; }
                        
                        tabContent.localScale = Vector3.one; 
                    }
                }
            }
        }
    }

    public void ClicarNoSlot(int slot)
    {
        if (slotConfirmacao != -1)
        {
            slotConfirmacao = -1;
            AtualizarTextosSlots();
            return;
        }

        if (modoDesenvolvedor)
        {
            if (PlayerPrefs.GetInt($"Slot_{slot}_SaveExistente", 0) == 0)
            {
                string dataAgora = System.DateTime.Now.ToString("dd/MM HH:mm");
                PlayerPrefs.SetInt($"Slot_{slot}_SaveExistente", 1);
                PlayerPrefs.SetString($"Slot_{slot}_Data", dataAgora);
                PlayerPrefs.Save();
            }
            
            AtualizarTextosSlots();
            if (loadingAtual != null) StopCoroutine(loadingAtual);
            loadingAtual = StartCoroutine(RotinaLoadingDevMode());
            return; 
        }

        string cenaAlvo = nomeDaCenaDoJogo; 

        if (SistemaGlobal.Instance != null) 
        {
            SistemaGlobal.Instance.slotAtual = slot; 

            if (SistemaGlobal.Instance.ExisteSave(slot))
            {
                SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = true;
                
                string cenaSalva = "";
                if (PersistenciaManager.Instance != null)
                {
                    // Garante que o Persistencia vai ler o slot que acabamos de selecionar
                    PersistenciaManager.Instance.LimparDicionario();
                    cenaSalva = PersistenciaManager.Instance.ObterString($"Slot_{slot}_Cena");
                }
                
                if (!string.IsNullOrEmpty(cenaSalva))
                {
                    cenaAlvo = cenaSalva;
                }
                else
                {
                    Debug.LogWarning("[SAVE] Cena não encontrada no Save. Usando cena padrão.");
                }
            }
            else
            {
                SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = false; 
                // Se é save novo, limpa qualquer lixo da RAM
                if (PersistenciaManager.Instance != null) PersistenciaManager.Instance.LimparDicionario();
            }
        }
        
        if (loadingAtual != null) StopCoroutine(loadingAtual);
        loadingAtual = StartCoroutine(RotinaLoadingPorcentagem(cenaAlvo));
    }

    public void BotaoApagarSlot(int slot)
    {
        if (modoDesenvolvedor)
        {
            if (PlayerPrefs.GetInt($"Slot_{slot}_SaveExistente", 0) == 0) return; 

            if (slotConfirmacao == slot)
            {
                PlayerPrefs.DeleteKey($"Slot_{slot}_SaveExistente");
                PlayerPrefs.DeleteKey($"Slot_{slot}_Data");
                PlayerPrefs.Save();
                slotConfirmacao = -1;
            }
            else slotConfirmacao = slot;
            
            AtualizarTextosSlots();
            return;
        }

        if (SistemaGlobal.Instance == null || !SistemaGlobal.Instance.ExisteSave(slot)) return;

        if (slotConfirmacao == slot)
        {
            SistemaGlobal.Instance.ApagarSave(slot);
            
            // 🔥 CORREÇÃO: Limpa a RAM se estiver apagando o slot que estava carregado.
            if (PersistenciaManager.Instance != null)
            {
                 PersistenciaManager.Instance.LimparDicionario();
            }

            slotConfirmacao = -1;
        }
        else slotConfirmacao = slot;
        
        AtualizarTextosSlots();
    }

    public void CancelarConfirmacaoDelete()
    {
        if (slotConfirmacao != -1)
        {
            slotConfirmacao = -1;
            AtualizarTextosSlots();
        }
    }

    public void BotaoPedirRestauracao(int slot)
    {
        slotParaRestaurar = slot; 
        LigarDesligarPainel(painelConfirmacaoBackup, true); 
    }

    public void ConfirmarRestauracaoBackup()
    {
        if (slotParaRestaurar != -1 && !modoDesenvolvedor && SistemaGlobal.Instance != null && PersistenciaManager.Instance != null)
        {
            // 🔥 CORREÇÃO: Força o slot atual temporariamente para o SaveTudo ir para o arquivo correto
            int slotAntigo = SistemaGlobal.Instance.slotAtual;
            SistemaGlobal.Instance.slotAtual = slotParaRestaurar;

            PersistenciaManager.Instance.RestaurarBackup(slotParaRestaurar);
            PersistenciaManager.Instance.SalvarTudo();

            // Restaura
            SistemaGlobal.Instance.slotAtual = slotAntigo;

            AtualizarTextosSlots(); 
        }
        
        slotParaRestaurar = -1; 
        LigarDesligarPainel(painelConfirmacaoBackup, false); 
    }

    public void CancelarRestauracaoBackup()
    {
        slotParaRestaurar = -1; 
        LigarDesligarPainel(painelConfirmacaoBackup, false); 
    }

    void AtualizarTextosSlots()
    {
        int lang = 0;
        if (LanguageManager.Instance != null) lang = LanguageManager.Instance.currentLanguage;
        else lang = PlayerPrefs.GetInt("Idioma", 0);

        string txtNovo = (lang == 0) ? textoNovoJogo_PT : textoNovoJogo_EN;
        string txtApagarBtn = (lang == 0) ? textoApagar_PT : textoApagar_EN;
        string txtConfirmarBtn = (lang == 0) ? textoConfirmar_PT : textoConfirmar_EN;
        string txtApagarSave = (lang == 0) ? textoApagarSave_PT : textoApagarSave_EN;
        string txtSlot = (lang == 0) ? textoSlot_PT : textoSlot_EN;

        for (int i = 0; i < textosDosSlots.Length; i++)
        {
            if (textosDosSlots[i] == null) continue;
            int slotNum = i + 1;

            bool temSave = false;
            if (modoDesenvolvedor) temSave = (PlayerPrefs.GetInt($"Slot_{slotNum}_SaveExistente", 0) == 1);
            else if (SistemaGlobal.Instance != null) temSave = SistemaGlobal.Instance.ExisteSave(slotNum);

            if (textosBotaoApagar != null && i < textosBotaoApagar.Length && textosBotaoApagar[i] != null)
            {
                textosBotaoApagar[i].text = (slotConfirmacao == slotNum) ? txtConfirmarBtn : txtApagarBtn;
            }

            string cabecalho = $"<size=40%>{txtSlot} {slotNum}</size>\n";

            if (slotConfirmacao == slotNum)
            {
                textosDosSlots[i].text = cabecalho + $"<color=red>{txtApagarSave}</color>";
            }
            else if (temSave)
            {
                string data = modoDesenvolvedor ? PlayerPrefs.GetString($"Slot_{slotNum}_Data", "") : (SistemaGlobal.Instance != null ? SistemaGlobal.Instance.GetDataSave(slotNum) : "");
                if (string.IsNullOrEmpty(data)) data = System.DateTime.Now.ToString("dd/MM HH:mm");
                
                textosDosSlots[i].text = cabecalho + $"<size=50%>{data}</size>";
            }
            else 
            {
                textosDosSlots[i].text = cabecalho + txtNovo;
            }
        }
    }

    void ForcarAutoSizeCentral()
    {
        if (textosDosSlots == null) return;
        foreach (var t in textosDosSlots) 
        {
            if (t == null) continue;
            t.enableAutoSizing = true;
            t.fontSizeMin = 10;
            t.fontSizeMax = 60; 
            t.alignment = TextAlignmentOptions.Center;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Truncate;
            t.margin = new Vector4(5, 5, 5, 5);
            t.rectTransform.localScale = Vector3.one;
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
        LigarDesligarPainel(painelSlots, true);
        AtualizarTextosSlots();
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
        Application.Quit(); 
    }

    public void BotaoVoltarGenerico()
    {
        if (estadoAtual == EstadoInterface.Opcoes) FecharOpcoesVoltar();
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
        if (slotConfirmacao != -1 && Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                CancelarConfirmacaoDelete();
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (painelConfirmacaoBackup != null && painelConfirmacaoBackup.activeSelf)
            {
                CancelarRestauracaoBackup();
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
                if (!isProcessandoPause) StartCoroutine(PausarComPrint());
                break;
            case EstadoInterface.Pausado:
                if (!isProcessandoPause) ResumeJogo();
                break;
            case EstadoInterface.Slots:
                BotaoVoltarGenerico();
                break;
        }
    }
    
    public void FecharJanelaConfirmacao() { LigarDesligarPainel(painelConfirmacaoReset, false); }

    public void BotaoConfirmarReset()
    {
        PlayerPrefs.DeleteKey("MouseSensitivity");
        PlayerPrefs.DeleteKey("PlayerFOV");
        PlayerPrefs.Save();

        if (FPS_Master.Instance != null) FPS_Master.Instance.CarregarConfiguracoes();

        SettingsManager settings = painelOpcoes.GetComponentInChildren<SettingsManager>(true);
        if (settings != null) settings.LoadAndApplyAllSettings(); 

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
        if (FPS_Master.Instance != null) FPS_Master.travadoInteracao = true;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeJogo()
    {
        estadoAtual = EstadoInterface.Jogando;
        isProcessandoPause = false; 
        jogoPausado = false; 
        
        if (imagemCongelada) 
        {
            imagemCongelada.gameObject.SetActive(false);
            ClearFreezeTexture();
        }
        
        LigarDesligarPainel(painelOpcoes, false);
        LigarDesligarPainel(painelPause, false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (FPS_Master.Instance != null) FPS_Master.travadoInteracao = false;

        Cursor.lockState = mouseLivreNoJogo ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = mouseLivreNoJogo;
    }

    public void SairParaMenuPrincipal()
    {
        Time.timeScale = 1f; 
        AudioListener.pause = false; 

        ClearFreezeTexture();

        if (playerMaster != null && SistemaGlobal.Instance != null)
        {
            SistemaGlobal.Instance.SalvarJogo(playerMaster.transform.position, SceneManager.GetActiveScene().name);
            
            if (PersistenciaManager.Instance != null)
            {
                PersistenciaManager.Instance.SalvarString($"Slot_{SistemaGlobal.Instance.slotAtual}_Cena", SceneManager.GetActiveScene().name);
                
                if (!PersistenciaManager.Instance.ModoSemSave())
                {
                    PersistenciaManager.Instance.SalvarTudo();
                }
            }
            PlayerPrefs.Save();
        }
        SceneManager.LoadScene("MenuPrincipal"); 
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
        if (musicaDoMenu == null) yield break;
        
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
        
        if (fadeAtual != null) StopCoroutine(fadeAtual);
        fadeAtual = StartCoroutine(FadeOutMusica());
        
        LigarDesligarPainel(painelLoading, true);
        LigarDesligarPainel(painelSlots, false);
        LigarDesligarPainel(painelMenuPrincipal, false);

        if (barraDeProgresso) barraDeProgresso.SetFill(0f); 

        AsyncOperation operacao = SceneManager.LoadSceneAsync(nomeCena);
        operacao.allowSceneActivation = false; 

        float progressoVisual = 0f;

        while (progressoVisual < 0.8f)
        {
            float progressoReal = Mathf.Clamp01(operacao.progress / 0.9f) * 0.8f;
            progressoVisual = Mathf.MoveTowards(progressoVisual, progressoReal, Time.unscaledDeltaTime * 0.8f); 

            if (barraDeProgresso) barraDeProgresso.SetFill(progressoVisual);

            if (operacao.progress >= 0.9f && progressoVisual >= 0.79f) break;
            yield return null;
        }

        float tempoExtra = 0f;
        while (tempoExtra < 2f)
        {
            tempoExtra += Time.unscaledDeltaTime;
            progressoVisual = Mathf.Lerp(0.8f, 1f, tempoExtra / 2f); 
            if (barraDeProgresso) barraDeProgresso.SetFill(progressoVisual);
            yield return null;
        }

        operacao.allowSceneActivation = true;
    }

    IEnumerator RotinaLoadingDevMode()
    {
        estadoAtual = EstadoInterface.Loading;
        
        if (fadeAtual != null) StopCoroutine(fadeAtual);
        fadeAtual = StartCoroutine(FadeOutMusica());
        
        LigarDesligarPainel(painelLoading, true);
        LigarDesligarPainel(painelSlots, false);
        LigarDesligarPainel(painelMenuPrincipal, false);

        if (barraDeProgresso) barraDeProgresso.SetFill(0.8f); 

        float progressoVisual = 0.8f;
        float tempoExtra = 0f;
        
        while (tempoExtra < 2f) 
        {
            tempoExtra += Time.unscaledDeltaTime;
            progressoVisual = Mathf.Lerp(0.8f, 1f, tempoExtra / 2f); 
            if (barraDeProgresso) barraDeProgresso.SetFill(progressoVisual);
            yield return null;
        }

        LigarDesligarPainel(painelLoading, false);
        LigarDesligarPainel(painelSlots, true);
        AtualizarTextosSlots(); 
        estadoAtual = EstadoInterface.Slots;
    }

    IEnumerator SequenciaInicializacaoJogo()
    {
        yield return new WaitForEndOfFrame();
        
        if (SistemaGlobal.Instance != null && playerMaster != null && SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar)
        {
            SistemaGlobal.Instance.CarregarJogo(SistemaGlobal.Instance.slotAtual);
            
            string p = "Slot_" + SistemaGlobal.Instance.slotAtual;
            float x = 0, y = 0, z = 0;
            bool achouPos = false;
            
            if (PersistenciaManager.Instance != null && PersistenciaManager.Instance.TemFloat(p + "_PosX"))
            {
                x = PersistenciaManager.Instance.ObterFloat(p + "_PosX");
                y = PersistenciaManager.Instance.ObterFloat(p + "_PosY");
                z = PersistenciaManager.Instance.ObterFloat(p + "_PosZ");
                achouPos = true;
            }
            else if (PlayerPrefs.HasKey(p + "_PosX"))
            {
                x = PlayerPrefs.GetFloat(p + "_PosX");
                y = PlayerPrefs.GetFloat(p + "_PosY");
                z = PlayerPrefs.GetFloat(p + "_PosZ");
                achouPos = true;
            }
            
            if (achouPos)
            {
                playerMaster.Teleportar(new Vector3(x, y + 0.1f, z));
            }
            
            SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = false; 
        }
    }

    private void ClearFreezeTexture()
    {
        if (imagemCongelada != null && imagemCongelada.texture != null)
        {
            Texture texCorrompida = imagemCongelada.texture;
            imagemCongelada.texture = null;
            Destroy(texCorrompida); 
        }
    }

    private void OnDisable() => ClearFreezeTexture();
}
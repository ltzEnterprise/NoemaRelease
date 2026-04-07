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

    private bool jogoPausado = false;
    private int slotConfirmacao = -1;
    private int slotParaRestaurar = -1; 

    private Vector3 escalaOpcoes = Vector3.one;
    private bool escalaSalva = false;
    private bool isProcessandoPause = false; 

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
            StartCoroutine(RotinaLoadingDevMode());
            return; 
        }

        // 🔥 LÓGICA DE CENA MÁGICA: Decide aqui pra qual cena o jogo vai carregar 🔥
        string cenaAlvo = nomeDaCenaDoJogo; 

        if (SistemaGlobal.Instance != null) 
        {
            SistemaGlobal.Instance.slotAtual = slot; 

            if (SistemaGlobal.Instance.ExisteSave(slot))
            {
                SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = true;
                
                // Se existe save, pega a cena que ele parou. Se bugar e não achar, usa a padrão.
                string cenaSalva = PlayerPrefs.GetString($"Slot_{slot}_Cena", "");
                if (!string.IsNullOrEmpty(cenaSalva))
                {
                    cenaAlvo = cenaSalva;
                }
            }
            else
            {
                // Novo jogo
                SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = false; 
            }
        }
        
        StartCoroutine(RotinaLoadingPorcentagem(cenaAlvo));
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
            PersistenciaManager.Instance.RestaurarBackup(slotParaRestaurar);
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
        // 1. Descobre qual idioma tá rodando agora
        int lang = 0;
        if (LanguageManager.Instance != null) lang = LanguageManager.Instance.currentLanguage;
        else lang = PlayerPrefs.GetInt("Idioma", 0);

        // 2. Puxa as palavras certas
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

            // Traduz o botão de Apagar/Confirmar
            if (textosBotaoApagar != null && i < textosBotaoApagar.Length && textosBotaoApagar[i] != null)
            {
                textosBotaoApagar[i].text = (slotConfirmacao == slotNum) ? txtConfirmarBtn : txtApagarBtn;
            }

            // Traduz a palavra SLOT
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
                // Traduz o Novo Jogo
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
        rt.sizeDelta = Vector2.zero; 
        rt.anchoredPosition = Vector2.zero; 
    }

    public void BotaoJogar_AbreSlots()
    {
        slotConfirmacao = -1;
        LigarDesligarPainel(painelMenuPrincipal, false);
        LigarDesligarPainel(painelOpcoes, false);
        LigarDesligarPainel(painelSlots, true);
        AtualizarTextosSlots();
    }

    public void AbrirOpcoes()
    {
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
        if (painelOpcoes != null && painelOpcoes.activeSelf) FecharOpcoesVoltar();
        else if (painelSlots != null && painelSlots.activeSelf)
        {
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
                LigarDesligarPainel(painelConfirmacaoReset, false);
                return; 
            }

            if (painelOpcoes != null && painelOpcoes.activeSelf)
            {
                FecharOpcoesVoltar();
            }
            else if (isCenaDeJogo)
            {
                if (isProcessandoPause) return;
                if (jogoPausado) ResumeJogo(); 
                else StartCoroutine(PausarComPrint());
            }
            else if (painelSlots != null && painelSlots.activeSelf) 
            {
                BotaoVoltarGenerico();
            }
        }

        if (Input.GetKeyDown(KeyCode.R) && painelOpcoes != null && painelOpcoes.activeSelf)
        {
            if (painelConfirmacaoReset != null && !painelConfirmacaoReset.activeSelf)
                LigarDesligarPainel(painelConfirmacaoReset, true);
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

        LigarDesligarPainel(painelConfirmacaoReset, false);
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

    IEnumerator PausarComPrint()
    {
        isProcessandoPause = true; 
        yield return new WaitForEndOfFrame();

        if (imagemCongelada != null)
        {
            ClearFreezeTexture();

            int width = Screen.width;
            int height = Screen.height;

            RenderTexture rtTelaCheia = RenderTexture.GetTemporary(width, height, 0);
            ScreenCapture.CaptureScreenshotIntoRenderTexture(rtTelaCheia);
            
            int w1 = width / 2; int h1 = height / 2;
            RenderTexture rt1 = RenderTexture.GetTemporary(w1, h1, 0);
            rt1.filterMode = FilterMode.Bilinear;
            Graphics.Blit(rtTelaCheia, rt1);

            int w2 = w1 / 2; int h2 = h1 / 2;
            RenderTexture rt2 = RenderTexture.GetTemporary(w2, h2, 0);
            rt2.filterMode = FilterMode.Bilinear;
            Graphics.Blit(rt1, rt2);

            int w3 = w2 / 2; int h3 = h2 / 2;
            RenderTexture rt3 = RenderTexture.GetTemporary(w3, h3, 0);
            rt3.filterMode = FilterMode.Bilinear;
            Graphics.Blit(rt2, rt3);
            
            RenderTexture.active = rt3;
            Texture2D texture = new Texture2D(w3, h3, TextureFormat.RGB24, false);
            texture.filterMode = FilterMode.Bilinear; 
            texture.ReadPixels(new Rect(0, 0, w3, h3), 0, 0);
            texture.Apply();
            
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rtTelaCheia);
            RenderTexture.ReleaseTemporary(rt1);
            RenderTexture.ReleaseTemporary(rt2);
            RenderTexture.ReleaseTemporary(rt3);

            imagemCongelada.texture = texture;
            imagemCongelada.color = Color.white;
            imagemCongelada.rectTransform.localScale = new Vector3(1f, -1f, 1f);
            
            ForcarTelaCheia(imagemCongelada.rectTransform);
            imagemCongelada.gameObject.SetActive(true);
        }
        PausarJogoLogica();
        
        isProcessandoPause = false; 
    }

    public void PausarJogoLogica()
    {
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
            
            // 🔥 GARANTIA ABSOLUTA DE SALVAR A CENA QUE O CARA PAROU 🔥
            PlayerPrefs.SetString($"Slot_{SistemaGlobal.Instance.slotAtual}_Cena", SceneManager.GetActiveScene().name);
            
            if (PersistenciaManager.Instance != null) PersistenciaManager.Instance.SalvarTudo();
            PlayerPrefs.Save();
        }
        SceneManager.LoadScene("MenuPrincipal"); 
    }

    public void FecharOpcoesVoltar()
    {
        LigarDesligarPainel(painelOpcoes, false);
        if (isCenaDeJogo) LigarDesligarPainel(painelPause, true);
        else LigarDesligarPainel(painelMenuPrincipal, true);
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
        StartCoroutine(FadeOutMusica());
        
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
        StartCoroutine(FadeOutMusica());
        
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
    }

    IEnumerator SequenciaInicializacaoJogo()
    {
        yield return new WaitForEndOfFrame();
        
        if (SistemaGlobal.Instance != null && playerMaster != null && SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar)
        {
            SistemaGlobal.Instance.CarregarJogo(SistemaGlobal.Instance.slotAtual);
            
            string p = "Slot_" + SistemaGlobal.Instance.slotAtual;
            
            if (PlayerPrefs.HasKey(p + "_PosX"))
            {
                float x = PlayerPrefs.GetFloat(p + "_PosX");
                float y = PlayerPrefs.GetFloat(p + "_PosY");
                float z = PlayerPrefs.GetFloat(p + "_PosZ");
                playerMaster.Teleportar(new Vector3(x, y + 0.1f, z));
            }
            
            SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = false; 
        }
    }

    private void OnDestroy() { ClearFreezeTexture(); }
}
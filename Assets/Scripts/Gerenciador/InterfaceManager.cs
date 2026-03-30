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
    [Tooltip("Se ligado, ignora o SistemaGlobal, cria saves falsos e simula a tela de loading de 80% a 100%.")]
    public bool modoDesenvolvedor = false; 

    [Header("--- JANELA DE CONFIRMAÇÃO ---")]
    public GameObject painelConfirmacaoReset;

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

    [Header("--- PAUSE ---")]
    public RawImage imagemCongelada; 

    [Header("--- JOGO ---")]
    public FPS_Master playerMaster; 
    public bool isCenaDeJogo = false; 
    
    [Tooltip("Marque isso nas fases 2D. Deixa o mouse livre e visível mesmo sendo cena de jogo.")]
    public bool mouseLivreNoJogo = false; 

    [Header("--- ÁUDIO DO MENU ---")]
    public AudioSource musicaDoMenu;
    [Tooltip("Arraste o arquivo da música do menu aqui")]
    public AudioClip clipeMusicaMenu; // <--- NOVO SLOT PARA O ÁUDIO AQUI
    [Range(0f, 1f)] public float volumeMaximoMusica = 1f; // <--- CONTROLE DE VOLUME
    public float tempoDeFade = 1.5f;

    private bool jogoPausado = false;
    private int slotConfirmacao = -1;

    public void LigarDesligarPainel(GameObject painel, bool estado)
    {
        if (painel == null) return;
        
        painel.SetActive(estado);
        
        SM_Window janela = painel.GetComponent<SM_Window>();
        if (janela != null) janela.Toggle(estado);

        if (estado == true)
        {
            foreach (Transform filho in painel.transform)
            {
                filho.gameObject.SetActive(true);
            }
        }
    }

    void Start()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        ForcarAutoSizeCentral();

        LigarDesligarPainel(painelLoading, false);
        
        if (imagemCongelada) 
        {
            imagemCongelada.gameObject.SetActive(false);
            ForcarTelaCheia(imagemCongelada.rectTransform);
        }

        if (isCenaDeJogo)
        {
            if (mouseLivreNoJogo)
            {
                Cursor.lockState = CursorLockMode.None; 
                Cursor.visible = true; 
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked; 
                Cursor.visible = false; 
            }

            if (FPS_Master.Instance != null) FPS_Master.travadoInteracao = false;      

            LigarDesligarPainel(painelPause, false);
            LigarDesligarPainel(painelOpcoes, false);
            LigarDesligarPainel(painelMenuPrincipal, false);

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

            // --- INICIA A MÚSICA DO MENU AQUI ---
            if (musicaDoMenu != null && clipeMusicaMenu != null)
            {
                musicaDoMenu.clip = clipeMusicaMenu;
                musicaDoMenu.volume = volumeMaximoMusica; // Garante que volta no volume certo
                musicaDoMenu.loop = true;
                if (!musicaDoMenu.isPlaying) musicaDoMenu.Play();
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
            Debug.Log($"[DEV] Testando Loading no Slot {slot}.");
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

        if (SistemaGlobal.Instance == null) 
        {
            Debug.LogError("SistemaGlobal não encontrado na cena!");
            return;
        }

        if (SistemaGlobal.Instance.ExisteSave(slot)) 
        {
            SistemaGlobal.Instance.CarregarJogo(slot);
        }
        
        StartCoroutine(RotinaLoadingPorcentagem(nomeDaCenaDoJogo));
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
            else
            {
                slotConfirmacao = slot;
            }
            AtualizarTextosSlots();
            return;
        }

        if (SistemaGlobal.Instance == null || !SistemaGlobal.Instance.ExisteSave(slot)) return;

        if (slotConfirmacao == slot)
        {
            SistemaGlobal.Instance.ApagarSave(slot);
            slotConfirmacao = -1;
        }
        else
        {
            slotConfirmacao = slot;
        }
        AtualizarTextosSlots();
    }

    void AtualizarTextosSlots()
    {
        for (int i = 0; i < textosDosSlots.Length; i++)
        {
            if (textosDosSlots[i] == null) continue;
            int slotNum = i + 1;

            bool temSave = false;
            if (modoDesenvolvedor) temSave = (PlayerPrefs.GetInt($"Slot_{slotNum}_SaveExistente", 0) == 1);
            else if (SistemaGlobal.Instance != null) temSave = SistemaGlobal.Instance.ExisteSave(slotNum);

            if (textosBotaoApagar != null && i < textosBotaoApagar.Length && textosBotaoApagar[i] != null)
            {
                textosBotaoApagar[i].text = (slotConfirmacao == slotNum) ? "<size=70%>CONFIRM</size>" : "DELETE";
            }

            string cabecalho = $"<size=40%>SLOT {slotNum}</size>\n";

            if (slotConfirmacao == slotNum)
            {
                textosDosSlots[i].text = cabecalho + "<color=red>DELETE SAVE?</color>";
            }
            else if (temSave)
            {
                string data = "";
                if (modoDesenvolvedor) data = PlayerPrefs.GetString($"Slot_{slotNum}_Data", "");
                else if (SistemaGlobal.Instance != null) data = SistemaGlobal.Instance.GetDataSave(slotNum);

                if (string.IsNullOrEmpty(data)) data = System.DateTime.Now.ToString("dd/MM HH:mm");
                
                textosDosSlots[i].text = cabecalho + $"<size=50%>{data}</size>";
            }
            else
            {
                textosDosSlots[i].text = cabecalho + "NEW GAME";
            }
        }
    }

    void ForcarAutoSizeCentral()
    {
        if (textosDosSlots != null) 
        {
            foreach (var t in textosDosSlots) 
            {
                if (t != null)
                {
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

    public void BotaoSair() { Application.Quit(); }

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
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (painelConfirmacaoReset != null && painelConfirmacaoReset.activeSelf)
            {
                LigarDesligarPainel(painelConfirmacaoReset, false);
                return; 
            }

            if (isCenaDeJogo)
            {
                if (painelOpcoes != null && painelOpcoes.activeSelf) 
                    FecharOpcoesVoltar();
                else 
                { 
                    if (jogoPausado) ResumeJogo(); 
                    else StartCoroutine(PausarComPrint());
                }
            }
            else
            {
                if (painelOpcoes != null && painelOpcoes.activeSelf) 
                    FecharOpcoesVoltar();
                else if (painelSlots != null && painelSlots.activeSelf) 
                    BotaoVoltarGenerico();
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (painelOpcoes != null && painelOpcoes.activeSelf)
            {
                if (painelConfirmacaoReset != null && !painelConfirmacaoReset.activeSelf)
                {
                    LigarDesligarPainel(painelConfirmacaoReset, true);
                }
            }
        }
    }
    
    public void FecharJanelaConfirmacao()
    {
        LigarDesligarPainel(painelConfirmacaoReset, false);
    }

    IEnumerator PausarComPrint()
    {
        yield return new WaitForEndOfFrame();
        if (imagemCongelada != null)
        {
            if (imagemCongelada.texture != null)
            {
                Destroy(imagemCongelada.texture);
            }

            Texture2D texture = ScreenCapture.CaptureScreenshotAsTexture();
            imagemCongelada.texture = texture;
            imagemCongelada.color = Color.white;
            ForcarTelaCheia(imagemCongelada.rectTransform);
            imagemCongelada.gameObject.SetActive(true);
        }
        PausarJogoLogica();
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
        jogoPausado = false;
        if (imagemCongelada) imagemCongelada.gameObject.SetActive(false);
        LigarDesligarPainel(painelOpcoes, false);
        LigarDesligarPainel(painelPause, false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        if (FPS_Master.Instance != null) FPS_Master.travadoInteracao = false;

        if (mouseLivreNoJogo)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void SairParaMenuPrincipal()
    {
        Time.timeScale = 1f; 
        AudioListener.pause = false; 

        if (playerMaster != null && SistemaGlobal.Instance != null)
        {
            SistemaGlobal.Instance.SalvarJogo(playerMaster.transform.position, SceneManager.GetActiveScene().name);
            PlayerPrefs.Save();
        }
        SceneManager.LoadScene("MenuPrincipal"); 
    }

    public void FecharOpcoesVoltar()
    {
        LigarDesligarPainel(painelOpcoes, false);
        
        if (isCenaDeJogo) 
        { 
            LigarDesligarPainel(painelPause, true);
        }
        else
        {
            LigarDesligarPainel(painelMenuPrincipal, true);
        }
    }

    // --- FADE OUT DA MÚSICA AQUI ---
    IEnumerator FadeOutMusica()
    {
        if (musicaDoMenu == null) yield break;
        
        float volumeInicial = musicaDoMenu.volume;
        float tempoPassado = 0f;

        // Vai descendo o volume devagarinho durante o loading
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
        // 1. Inicia o fade out na mesma hora que clica no slot
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

            if (operacao.progress >= 0.9f && progressoVisual >= 0.79f)
            {
                progressoVisual = 0.8f;
                if (barraDeProgresso) barraDeProgresso.SetFill(progressoVisual);
                break;
            }

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
        // Também faz fade out se carregar pelo modo desenvolvedor
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
            int slot = SistemaGlobal.Instance.slotAtual;
            string p = "Slot_" + slot;
            if (PlayerPrefs.HasKey(p + "_PosX"))
            {
                float x = PlayerPrefs.GetFloat(p + "_PosX");
                float y = PlayerPrefs.GetFloat(p + "_PosY");
                float z = PlayerPrefs.GetFloat(p + "_PosZ");
                if (x != 0 || y != 0 || z != 0) playerMaster.Teleportar(new Vector3(x, y + 0.1f, z));
            }
        }
    }
}
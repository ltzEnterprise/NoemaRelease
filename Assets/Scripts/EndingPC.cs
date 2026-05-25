using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System.Collections;
using System.IO;
using TMPro;

public class EndingPC : MonoBehaviour
{
    [Header("--- SAVE ---")]
    public string uniqueID = "EndingPC_Delta_Final";

    [Header("--- INPUT ---")]
    public bool permitirTeclaEInterna = true;
    public bool bloquearDuploInputNoMesmoFrame = true;

    [Header("--- PLAYER / CÂMERA ---")]
    [Tooltip("Arraste aqui o objeto principal do Player.")]
    public Transform player;

    [Tooltip("Arraste aqui a câmera do Player.")]
    public Transform cameraPlayer;

    [Header("--- DELTA / DESKTOP ---")]
    public GameObject painelSistemaDelta;
    public CanvasGroup canvasGroupSistemaDelta;

    [Header("--- WALLPAPER ---")]
    public GameObject wallpaperInicialPainel;
    public GameObject wallpaperUsuarioPainel;
    public bool usarWallpaperRealDoWindows = true;

    [Header("--- BARRA DE TAREFAS E ÍCONES ---")]
    public GameObject barraTarefasEIconesInicial;
    public GameObject barraTarefasEIconesComPasta;

    [Header("--- POPUP SENHA ---")]
    public GameObject painelPopupSenha;
    public CanvasGroup canvasGroupPopupSenha;

    [Tooltip("A JANELA pequena do popup. NÃO coloque o painel full-screen aqui.")]
    public RectTransform popupSenhaRect;

    [Tooltip("Texto TextMeshProUGUI onde aparecem os asteriscos e a barrinha piscando.")]
    public TextMeshProUGUI textoSenhaTMP;

    public string senhaCorreta = "WHERE IS LOST";
    public int limiteCaracteresSenha = 26;
    public char caractereSenha = '*';
    public float tempoAnimacaoPopup = 0.18f;

    [Header("--- BARRINHA PISCANDO ---")]
    public bool mostrarBarraPiscando = true;
    public string caractereBarra = "|";
    public float intervaloPiscarBarra = 0.45f;

    [Header("--- CURSOR CUSTOMIZADO ---")]
    public bool usarCursorCustomizadoDelta = false;

    public Texture2D cursorNormal;
    public Texture2D cursorClique;

    [Range(0.1f, 5f)]
    public float escalaCursorNormal = 1f;

    [Range(0.1f, 5f)]
    public float escalaCursorClique = 1f;

    public Vector2 hotspotCursorNormal = Vector2.zero;
    public Vector2 hotspotCursorClique = Vector2.zero;

    [Header("--- TELA PRETA DELTA ---")]
    public Image telaPretaDelta;
    public float tempoTelaPretaDelta = 1f;

    [Header("--- BOTÕES DO DELTA ---")]
    public Button botaoArquivoSenha;
    public Button botaoArquivoFinal;
    public Button botaoFecharPopupSenha;
    public Button[] botoesExtrasDelta;

    [Header("--- SOM ---")]
    public AudioSource audioSource;
    public AudioClip somSenhaErrada;

    [Header("--- ARG NO PC REAL ---")]
    public TextAsset arquivoHTML;
    public string nomePastaDesktopReal = "NOEMA";
    public string nomeArquivoHTML = "ARG.html";
    public bool criarArquivoHTMLNoDesktopReal = true;

    [Header("--- FADE AO CLICAR NO FIM ---")]
    public Image fadeFimImage;
    public float tempoFadeFim = 1.2f;

    [Header("--- CRÉDITOS / FINAL ---")]
    public Image blackBackground;
    public CanvasGroup creditNameCanvas;
    public RectTransform creditosRect;

    public float creditosYInicial = -700f;
    public float creditosYFinal = 900f;
    public float blackScreenFadeTime = 2f;
    public float creditosScrollTime = 8f;
    public float tempoDepoisDosCreditos = 1f;
    public string menuSceneName = "MenuPrincipal";

    [Header("--- OPCIONAL ---")]
    public GameObject interactText;
    public GameObject volumeDessaCena;
    public GameObject volumeGlobalPraDesativar;

    private bool deltaAberto = false;
    private bool popupAberto = false;
    private bool senhaResolvida = false;
    private bool processandoSenha = false;
    private bool encerrando = false;

    private bool volumeGlobalEstavaAtivo = false;

    private string senhaDigitada = "";

    private Coroutine rotinaPopup;
    private Coroutine rotinaSenha;
    private Coroutine rotinaBarra;

    private Image wallpaperInicialImagem;
    private Image wallpaperUsuarioImagem;

    private Texture2D texturaWallpaperCarregada;
    private Sprite spriteWallpaperCarregado;

    private CursorLockMode cursorLockOriginal;
    private bool cursorVisibleOriginal;

    private Texture2D cursorNormalProcessado;
    private Texture2D cursorCliqueProcessado;

    private Vector3 escalaOriginalPopup = Vector3.one;
    private bool escalaOriginalPopupCapturada = false;

    private int frameBloqueadoInput = -1000;

    private bool barraVisivel = true;

    private Vector3 playerPosicaoTravada;
    private Quaternion playerRotacaoTravada;
    private Vector3 cameraPosicaoTravada;
    private Quaternion cameraRotacaoTravada;

    private bool playerFoiTravado = false;
    private bool cameraFoiTravada = false;

    private string ChaveSenhaResolvida => uniqueID + "_SenhaResolvida";
    private string ChaveArquivoCriado => uniqueID + "_ArquivoHTMLCriado";

    void Start()
    {
        if (volumeGlobalPraDesativar != null)
        {
            volumeGlobalEstavaAtivo = volumeGlobalPraDesativar.activeSelf;
            volumeGlobalPraDesativar.SetActive(false);
        }

        if (volumeDessaCena != null)
            volumeDessaCena.SetActive(true);

        if (interactText != null)
            interactText.SetActive(false);

        PrepararDelta();
        PrepararPopup();
        PrepararFadeFim();
        PrepararCreditos();
        RegistrarBotoes();
        RegistrarCursoresDosBotoes();

        StartCoroutine(CarregarSaveSeguro());
    }

    void Update()
    {
        if (encerrando)
            return;

        if (deltaAberto)
            ForcarEstadoDeltaAberto();

        if (popupAberto)
            CapturarSenhaDigitada();

        if (!permitirTeclaEInterna)
            return;

        if (bloquearDuploInputNoMesmoFrame && Time.frameCount == frameBloqueadoInput)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!deltaAberto)
            {
                AbrirDelta();
                return;
            }

            if (deltaAberto && !popupAberto)
            {
                FecharDelta();
                return;
            }
        }

        if (deltaAberto && popupAberto && Input.GetKeyDown(KeyCode.Escape))
            FecharPopupSenha();
    }

    void LateUpdate()
    {
        if (!encerrando && deltaAberto)
            ForcarEstadoDeltaAberto();
    }

    private IEnumerator CarregarSaveSeguro()
    {
        if (PersistenciaManager.Instance != null)
        {
            yield return new WaitUntil(() =>
                PersistenciaManager.Instance.DadosProntosParaUso &&
                !PersistenciaManager.Instance.EstaCarregando
            );

            senhaResolvida = PersistenciaManager.Instance.ObterEstado(ChaveSenhaResolvida, false);
        }

        if (senhaResolvida)
            AplicarEstadoSenhaResolvida(false);
    }

    private void OnApplicationFocus(bool focus)
    {
        if (focus && deltaAberto)
            ForcarEstadoDeltaAberto();
    }

    private void OnDisable()
    {
        if (deltaAberto)
        {
            deltaAberto = false;
            RestaurarCursor();
            DestravarJogador();
        }
    }

    private void OnDestroy()
    {
        if (volumeGlobalPraDesativar != null && volumeGlobalEstavaAtivo)
            volumeGlobalPraDesativar.SetActive(true);

        RestaurarCursor();
        DestravarJogador();

        if (spriteWallpaperCarregado != null)
            Destroy(spriteWallpaperCarregado);

        if (texturaWallpaperCarregada != null)
            Destroy(texturaWallpaperCarregada);

        if (cursorNormalProcessado != null)
            Destroy(cursorNormalProcessado);

        if (cursorCliqueProcessado != null)
            Destroy(cursorCliqueProcessado);
    }

    public void Interagir()
    {
        if (encerrando)
            return;

        frameBloqueadoInput = Time.frameCount;

        if (deltaAberto)
            FecharDelta();
        else
            AbrirDelta();
    }

    public void AbrirDelta()
    {
        if (encerrando)
            return;

        if (painelSistemaDelta == null)
        {
            Debug.LogError("[EndingPC] Painel Sistema Delta não foi atribuído.");
            return;
        }

        if (deltaAberto)
        {
            ForcarEstadoDeltaAberto();
            return;
        }

        deltaAberto = true;

        cursorLockOriginal = Cursor.lockState;
        cursorVisibleOriginal = Cursor.visible;

        CapturarPosicaoTravada();

        if (interactText != null)
            interactText.SetActive(false);

        AtivarObjetoComPais(painelSistemaDelta);
        painelSistemaDelta.SetActive(true);

        if (canvasGroupSistemaDelta == null)
            canvasGroupSistemaDelta = painelSistemaDelta.GetComponent<CanvasGroup>();

        if (canvasGroupSistemaDelta == null)
            canvasGroupSistemaDelta = painelSistemaDelta.AddComponent<CanvasGroup>();

        canvasGroupSistemaDelta.alpha = 1f;
        canvasGroupSistemaDelta.interactable = true;
        canvasGroupSistemaDelta.blocksRaycasts = true;

        GarantirSistemaUI();
        CorrigirRaycastsBasicos();

        TravarJogador();
        ForcarEstadoDeltaAberto();
        AplicarCursorNormal();

        Debug.Log("[EndingPC] Delta aberto.");
    }

    public void FecharDelta()
    {
        if (!deltaAberto)
            return;

        if (popupAberto)
        {
            FecharPopupSenha();
            return;
        }

        deltaAberto = false;

        if (painelSistemaDelta != null)
            painelSistemaDelta.SetActive(false);

        if (canvasGroupSistemaDelta != null)
        {
            canvasGroupSistemaDelta.alpha = 0f;
            canvasGroupSistemaDelta.interactable = false;
            canvasGroupSistemaDelta.blocksRaycasts = false;
        }

        RestaurarCursor();
        DestravarJogador();

        Debug.Log("[EndingPC] Delta fechado.");
    }

    public void AbrirPopupSenha()
    {
        if (encerrando || senhaResolvida || processandoSenha)
            return;

        if (painelPopupSenha == null)
        {
            Debug.LogError("[EndingPC] Painel Popup Senha não foi atribuído.");
            return;
        }

        if (popupSenhaRect == null)
        {
            Debug.LogError("[EndingPC] Popup Senha Rect não foi atribuído. Arraste a janela pequena do popup.");
            return;
        }

        if (!deltaAberto)
            AbrirDelta();

        if (rotinaPopup != null)
            StopCoroutine(rotinaPopup);

        popupAberto = true;
        senhaDigitada = "";
        barraVisivel = true;
        AtualizarTextoSenha();

        rotinaPopup = StartCoroutine(AnimarPopup(true));
    }

    public void FecharPopupSenha()
    {
        if (!popupAberto)
            return;

        if (rotinaPopup != null)
            StopCoroutine(rotinaPopup);

        rotinaPopup = StartCoroutine(AnimarPopup(false));
    }

    public void AbrirFinal()
    {
        if (encerrando)
            return;

        StartCoroutine(RotinaFinalComFade());
    }

    private void PrepararDelta()
    {
        if (painelSistemaDelta != null)
        {
            painelSistemaDelta.SetActive(false);

            if (canvasGroupSistemaDelta == null)
                canvasGroupSistemaDelta = painelSistemaDelta.GetComponent<CanvasGroup>();

            if (canvasGroupSistemaDelta == null)
                canvasGroupSistemaDelta = painelSistemaDelta.AddComponent<CanvasGroup>();

            canvasGroupSistemaDelta.alpha = 0f;
            canvasGroupSistemaDelta.interactable = false;
            canvasGroupSistemaDelta.blocksRaycasts = false;
        }

        if (wallpaperInicialPainel != null)
            wallpaperInicialImagem = wallpaperInicialPainel.GetComponent<Image>();

        if (wallpaperUsuarioPainel != null)
            wallpaperUsuarioImagem = wallpaperUsuarioPainel.GetComponent<Image>();

        if (wallpaperInicialPainel != null)
            wallpaperInicialPainel.SetActive(true);

        if (wallpaperUsuarioPainel != null)
            wallpaperUsuarioPainel.SetActive(false);

        if (wallpaperInicialImagem != null)
            wallpaperInicialImagem.raycastTarget = false;

        if (wallpaperUsuarioImagem != null)
            wallpaperUsuarioImagem.raycastTarget = false;

        if (barraTarefasEIconesInicial != null)
            barraTarefasEIconesInicial.SetActive(true);

        if (barraTarefasEIconesComPasta != null)
            barraTarefasEIconesComPasta.SetActive(false);

        if (telaPretaDelta != null)
        {
            Color c = telaPretaDelta.color;
            c.a = 0f;
            telaPretaDelta.color = c;
            telaPretaDelta.gameObject.SetActive(false);
            telaPretaDelta.raycastTarget = true;
        }
    }

    private void PrepararPopup()
    {
        if (painelPopupSenha != null)
        {
            painelPopupSenha.SetActive(false);

            if (canvasGroupPopupSenha == null)
                canvasGroupPopupSenha = painelPopupSenha.GetComponent<CanvasGroup>();

            if (canvasGroupPopupSenha == null)
                canvasGroupPopupSenha = painelPopupSenha.AddComponent<CanvasGroup>();

            canvasGroupPopupSenha.alpha = 0f;
            canvasGroupPopupSenha.interactable = false;
            canvasGroupPopupSenha.blocksRaycasts = false;
        }

        if (popupSenhaRect != null)
        {
            escalaOriginalPopup = popupSenhaRect.localScale;
            escalaOriginalPopupCapturada = true;
        }

        senhaDigitada = "";
        AtualizarTextoSenha();
    }

    private void PrepararFadeFim()
    {
        if (fadeFimImage != null)
        {
            Color c = fadeFimImage.color;
            c.a = 0f;
            fadeFimImage.color = c;
            fadeFimImage.gameObject.SetActive(false);
            fadeFimImage.raycastTarget = true;
        }
    }

    private void PrepararCreditos()
    {
        if (blackBackground != null)
        {
            Color c = blackBackground.color;
            c.a = 0f;
            blackBackground.color = c;
            blackBackground.gameObject.SetActive(false);
        }

        if (creditNameCanvas != null)
        {
            creditNameCanvas.alpha = 0f;
            creditNameCanvas.gameObject.SetActive(false);
        }

        if (creditosRect != null)
        {
            Vector2 pos = creditosRect.anchoredPosition;
            pos.y = creditosYInicial;
            creditosRect.anchoredPosition = pos;
        }
    }

    private void RegistrarBotoes()
    {
        if (botaoArquivoSenha != null)
        {
            botaoArquivoSenha.onClick.RemoveListener(AbrirPopupSenha);
            botaoArquivoSenha.onClick.AddListener(AbrirPopupSenha);
        }

        if (botaoArquivoFinal != null)
        {
            botaoArquivoFinal.onClick.RemoveListener(AbrirFinal);
            botaoArquivoFinal.onClick.AddListener(AbrirFinal);
        }

        if (botaoFecharPopupSenha != null)
        {
            botaoFecharPopupSenha.onClick.RemoveListener(FecharPopupSenha);
            botaoFecharPopupSenha.onClick.AddListener(FecharPopupSenha);
        }
    }

    private void RegistrarCursoresDosBotoes()
    {
        RegistrarCursorBotao(botaoArquivoSenha);
        RegistrarCursorBotao(botaoArquivoFinal);
        RegistrarCursorBotao(botaoFecharPopupSenha);

        if (botoesExtrasDelta != null)
        {
            for (int i = 0; i < botoesExtrasDelta.Length; i++)
                RegistrarCursorBotao(botoesExtrasDelta[i]);
        }
    }

    private void RegistrarCursorBotao(Button botao)
    {
        if (botao == null)
            return;

        EventTrigger trigger = botao.GetComponent<EventTrigger>();

        if (trigger == null)
            trigger = botao.gameObject.AddComponent<EventTrigger>();

        trigger.triggers.Clear();

        EventTrigger.Entry enter = new EventTrigger.Entry();
        enter.eventID = EventTriggerType.PointerEnter;
        enter.callback.AddListener((data) =>
        {
            if (deltaAberto)
                AplicarCursorClique();
        });

        EventTrigger.Entry exit = new EventTrigger.Entry();
        exit.eventID = EventTriggerType.PointerExit;
        exit.callback.AddListener((data) =>
        {
            if (deltaAberto)
                AplicarCursorNormal();
        });

        trigger.triggers.Add(enter);
        trigger.triggers.Add(exit);
    }

    private void CapturarSenhaDigitada()
    {
        if (processandoSenha || senhaResolvida)
            return;

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (senhaDigitada.Length > 0)
            {
                senhaDigitada = senhaDigitada.Substring(0, senhaDigitada.Length - 1);
                AtualizarTextoSenha();
            }

            return;
        }

        string entrada = Input.inputString;

        if (string.IsNullOrEmpty(entrada))
            return;

        for (int i = 0; i < entrada.Length; i++)
        {
            char c = entrada[i];

            if (c == '\b' || c == '\n' || c == '\r')
                continue;

            if (senhaDigitada.Length >= Mathf.Max(1, limiteCaracteresSenha))
                continue;

            senhaDigitada += c;
        }

        AtualizarTextoSenha();
        VerificarSenhaAutomatica();
    }

    private void AtualizarTextoSenha()
    {
        if (textoSenhaTMP == null)
            return;

        string textoSenha = string.IsNullOrEmpty(senhaDigitada)
            ? ""
            : new string(caractereSenha, senhaDigitada.Length);

        if (mostrarBarraPiscando && popupAberto && barraVisivel)
            textoSenha += caractereBarra;

        textoSenhaTMP.text = textoSenha;
    }

    private IEnumerator RotinaPiscarBarra()
    {
        while (popupAberto)
        {
            barraVisivel = !barraVisivel;
            AtualizarTextoSenha();
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, intervaloPiscarBarra));
        }

        barraVisivel = false;
        AtualizarTextoSenha();
        rotinaBarra = null;
    }

    private void VerificarSenhaAutomatica()
    {
        string digitado = NormalizarSenha(senhaDigitada);
        string senha = NormalizarSenha(senhaCorreta);

        if (string.IsNullOrEmpty(senha))
            return;

        if (digitado.Length < senha.Length)
            return;

        if (digitado == senha)
        {
            if (rotinaSenha == null)
                rotinaSenha = StartCoroutine(RotinaSenhaCorreta());
        }
        else
        {
            SenhaErrada();
        }
    }

    private void SenhaErrada()
    {
        if (processandoSenha)
            return;

        if (audioSource != null && somSenhaErrada != null)
            audioSource.PlayOneShot(somSenhaErrada);

        senhaDigitada = "";
        AtualizarTextoSenha();

        FecharPopupSenha();
    }

    private string NormalizarSenha(string s)
    {
        if (string.IsNullOrEmpty(s))
            return "";

        return s.Trim().ToUpperInvariant();
    }

    private IEnumerator AnimarPopup(bool abrir)
    {
        if (painelPopupSenha == null || popupSenhaRect == null)
            yield break;

        painelPopupSenha.SetActive(true);

        if (canvasGroupPopupSenha == null)
            canvasGroupPopupSenha = painelPopupSenha.GetComponent<CanvasGroup>();

        if (canvasGroupPopupSenha == null)
            canvasGroupPopupSenha = painelPopupSenha.AddComponent<CanvasGroup>();

        if (!escalaOriginalPopupCapturada)
        {
            escalaOriginalPopup = popupSenhaRect.localScale;
            escalaOriginalPopupCapturada = true;
        }

        float t = 0f;

        float alphaInicio = abrir ? 0f : 1f;
        float alphaFim = abrir ? 1f : 0f;

        Vector3 escalaInicio = abrir ? escalaOriginalPopup * 0.92f : escalaOriginalPopup;
        Vector3 escalaFim = abrir ? escalaOriginalPopup : escalaOriginalPopup * 0.92f;

        canvasGroupPopupSenha.interactable = false;
        canvasGroupPopupSenha.blocksRaycasts = false;

        popupSenhaRect.localScale = escalaInicio;

        while (t < tempoAnimacaoPopup)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / Mathf.Max(0.001f, tempoAnimacaoPopup));
            float suave = Mathf.SmoothStep(0f, 1f, p);

            canvasGroupPopupSenha.alpha = Mathf.Lerp(alphaInicio, alphaFim, suave);
            popupSenhaRect.localScale = Vector3.Lerp(escalaInicio, escalaFim, suave);

            yield return null;
        }

        canvasGroupPopupSenha.alpha = alphaFim;
        popupSenhaRect.localScale = escalaFim;

        if (abrir)
        {
            popupAberto = true;
            canvasGroupPopupSenha.interactable = true;
            canvasGroupPopupSenha.blocksRaycasts = true;

            barraVisivel = true;
            AtualizarTextoSenha();

            if (rotinaBarra != null)
                StopCoroutine(rotinaBarra);

            rotinaBarra = StartCoroutine(RotinaPiscarBarra());

            ForcarEstadoDeltaAberto();
        }
        else
        {
            popupAberto = false;

            if (rotinaBarra != null)
            {
                StopCoroutine(rotinaBarra);
                rotinaBarra = null;
            }

            canvasGroupPopupSenha.interactable = false;
            canvasGroupPopupSenha.blocksRaycasts = false;
            painelPopupSenha.SetActive(false);

            senhaDigitada = "";
            barraVisivel = false;
            AtualizarTextoSenha();
        }

        rotinaPopup = null;
    }

    private IEnumerator RotinaSenhaCorreta()
    {
        processandoSenha = true;

        if (rotinaPopup != null)
            StopCoroutine(rotinaPopup);

        if (popupAberto)
            yield return StartCoroutine(AnimarPopup(false));

        yield return StartCoroutine(PiscarTrocarWallpaper());

        CriarPastaNOEMAComHTML();

        senhaResolvida = true;
        processandoSenha = false;
        rotinaSenha = null;

        SalvarEstadoSenhaResolvida();
    }

    private IEnumerator PiscarTrocarWallpaper()
    {
        if (telaPretaDelta == null)
        {
            AplicarEstadoSenhaResolvida(false);
            yield break;
        }

        telaPretaDelta.gameObject.SetActive(true);

        float tempoTotal = Mathf.Max(0.05f, tempoTelaPretaDelta);
        float fade = Mathf.Min(0.2f, tempoTotal * 0.35f);
        float hold = Mathf.Max(0f, tempoTotal - fade * 2f);

        yield return StartCoroutine(FadeImagem(telaPretaDelta, 0f, 1f, fade));

        AplicarEstadoSenhaResolvida(false);

        if (hold > 0f)
            yield return new WaitForSecondsRealtime(hold);

        yield return StartCoroutine(FadeImagem(telaPretaDelta, 1f, 0f, fade));

        telaPretaDelta.gameObject.SetActive(false);
    }

    private IEnumerator RotinaFinalComFade()
    {
        encerrando = true;

        if (popupAberto)
            FecharPopupSenha();

        if (fadeFimImage != null)
        {
            fadeFimImage.gameObject.SetActive(true);
            yield return StartCoroutine(FadeImagem(fadeFimImage, 0f, 1f, tempoFadeFim));
        }

        if (painelSistemaDelta != null)
            painelSistemaDelta.SetActive(false);

        yield return StartCoroutine(RotinaCreditosFinal());
    }

    private IEnumerator FadeImagem(Image img, float inicio, float fim, float duracao)
    {
        if (img == null)
            yield break;

        float t = 0f;
        duracao = Mathf.Max(0.001f, duracao);

        Color c = img.color;
        c.a = inicio;
        img.color = c;

        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duracao);

            c = img.color;
            c.a = Mathf.Lerp(inicio, fim, p);
            img.color = c;

            yield return null;
        }

        Color final = img.color;
        final.a = fim;
        img.color = final;
    }

    private void AplicarEstadoSenhaResolvida(bool criarArquivoSePreciso)
    {
        senhaResolvida = true;

        AplicarWallpaperRealOuFallback();

        if (wallpaperInicialPainel != null)
            wallpaperInicialPainel.SetActive(false);

        if (wallpaperUsuarioPainel != null)
            wallpaperUsuarioPainel.SetActive(true);

        if (barraTarefasEIconesInicial != null)
            barraTarefasEIconesInicial.SetActive(false);

        if (barraTarefasEIconesComPasta != null)
            barraTarefasEIconesComPasta.SetActive(true);

        if (botaoArquivoSenha != null)
            botaoArquivoSenha.interactable = false;

        if (criarArquivoSePreciso)
            CriarPastaNOEMAComHTML();
    }

    private void AplicarWallpaperRealOuFallback()
    {
        if (wallpaperUsuarioImagem == null)
            return;

        Sprite sprite = null;

        if (usarWallpaperRealDoWindows)
        {
            string caminho = ObterCaminhoWallpaperWindows();

            if (!string.IsNullOrEmpty(caminho))
                sprite = CarregarSpriteDeImagem(caminho);
        }

        if (sprite == null && wallpaperInicialImagem != null)
            sprite = wallpaperInicialImagem.sprite;

        if (sprite == null)
            return;

        wallpaperUsuarioImagem.sprite = sprite;
        wallpaperUsuarioImagem.enabled = true;
        wallpaperUsuarioImagem.raycastTarget = false;
        wallpaperUsuarioImagem.preserveAspect = false;
    }

    private string ObterCaminhoWallpaperWindows()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try
        {
            string appData = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);

            if (string.IsNullOrEmpty(appData))
                return "";

            string pastaThemes = Path.Combine(appData, "Microsoft", "Windows", "Themes");
            string transcoded = Path.Combine(pastaThemes, "TranscodedWallpaper");

            if (File.Exists(transcoded))
                return transcoded;

            string cachedFiles = Path.Combine(pastaThemes, "CachedFiles");

            if (Directory.Exists(cachedFiles))
            {
                string[] arquivos = Directory.GetFiles(cachedFiles);

                if (arquivos != null && arquivos.Length > 0)
                    return arquivos[0];
            }
        }
        catch { }

        return "";
#else
        return "";
#endif
    }

    private Sprite CarregarSpriteDeImagem(string caminho)
    {
        try
        {
            if (!File.Exists(caminho))
                return null;

            byte[] bytes = File.ReadAllBytes(caminho);

            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            if (!tex.LoadImage(bytes))
            {
                Destroy(tex);
                return null;
            }

            if (spriteWallpaperCarregado != null)
                Destroy(spriteWallpaperCarregado);

            if (texturaWallpaperCarregada != null)
                Destroy(texturaWallpaperCarregada);

            texturaWallpaperCarregada = tex;

            spriteWallpaperCarregado = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            return spriteWallpaperCarregado;
        }
        catch
        {
            return null;
        }
    }

    private void CriarPastaNOEMAComHTML()
    {
        if (!criarArquivoHTMLNoDesktopReal)
            return;

        if (arquivoHTML == null)
        {
            Debug.LogError("[EndingPC] Arquivo HTML não atribuído.");
            return;
        }

        try
        {
            string desktop = System.Environment.GetFolderPath(System.Environment.SpecialFolder.DesktopDirectory);

            if (string.IsNullOrEmpty(desktop))
                desktop = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);

            string pasta = Path.Combine(desktop, nomePastaDesktopReal);
            Directory.CreateDirectory(pasta);

            string nomeFinal = nomeArquivoHTML;

            if (string.IsNullOrWhiteSpace(nomeFinal))
                nomeFinal = "ARG.html";

            if (!nomeFinal.ToLowerInvariant().EndsWith(".html") &&
                !nomeFinal.ToLowerInvariant().EndsWith(".htm"))
                nomeFinal += ".html";

            string caminho = Path.Combine(pasta, nomeFinal);

            File.WriteAllText(caminho, arquivoHTML.text);

            if (PersistenciaManager.Instance != null)
                PersistenciaManager.Instance.RegistrarEstado(ChaveArquivoCriado, true);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[EndingPC] Erro ao criar pasta/HTML: " + e.Message);
        }
    }

    private void SalvarEstadoSenhaResolvida()
    {
        if (PersistenciaManager.Instance == null)
            return;

        PersistenciaManager.Instance.RegistrarEstado(ChaveSenhaResolvida, true);
        PersistenciaManager.Instance.SalvarTudo(true);
    }

    private void GarantirSistemaUI()
    {
        Canvas canvas = painelSistemaDelta != null
            ? painelSistemaDelta.GetComponentInParent<Canvas>(true)
            : null;

        if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            canvas.gameObject.AddComponent<GraphicRaycaster>();

        if (EventSystem.current == null)
        {
            GameObject obj = new GameObject("EventSystem");
            obj.AddComponent<EventSystem>();
            obj.AddComponent<StandaloneInputModule>();
        }
        else
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void CorrigirRaycastsBasicos()
    {
        if (wallpaperInicialImagem != null)
            wallpaperInicialImagem.raycastTarget = false;

        if (wallpaperUsuarioImagem != null)
            wallpaperUsuarioImagem.raycastTarget = false;

        PrepararBotao(botaoArquivoSenha);
        PrepararBotao(botaoArquivoFinal);
        PrepararBotao(botaoFecharPopupSenha);

        if (botoesExtrasDelta != null)
        {
            for (int i = 0; i < botoesExtrasDelta.Length; i++)
                PrepararBotao(botoesExtrasDelta[i]);
        }
    }

    private void PrepararBotao(Button b)
    {
        if (b == null)
            return;

        b.interactable = true;

        Graphic g = b.targetGraphic;

        if (g == null)
            g = b.GetComponent<Graphic>();

        if (g != null)
        {
            g.raycastTarget = true;
            b.targetGraphic = g;
        }
    }

    private void CapturarPosicaoTravada()
    {
        if (player != null)
        {
            playerFoiTravado = true;
            playerPosicaoTravada = player.position;
            playerRotacaoTravada = player.rotation;
        }

        if (cameraPlayer != null)
        {
            cameraFoiTravada = true;
            cameraPosicaoTravada = cameraPlayer.position;
            cameraRotacaoTravada = cameraPlayer.rotation;
        }
    }

    private void TravarJogador()
    {
        FPS_Master.travadoInteracao = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void DestravarJogador()
    {
        FPS_Master.travadoInteracao = false;

        playerFoiTravado = false;
        cameraFoiTravada = false;
    }

    private void ForcarEstadoDeltaAberto()
    {
        FPS_Master.travadoInteracao = true;

        if (playerFoiTravado && player != null)
        {
            player.position = playerPosicaoTravada;
            player.rotation = playerRotacaoTravada;
        }

        if (cameraFoiTravada && cameraPlayer != null)
        {
            cameraPlayer.position = cameraPosicaoTravada;
            cameraPlayer.rotation = cameraRotacaoTravada;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (painelSistemaDelta != null && !painelSistemaDelta.activeInHierarchy)
            AtivarObjetoComPais(painelSistemaDelta);

        if (canvasGroupSistemaDelta != null)
        {
            canvasGroupSistemaDelta.alpha = 1f;
            canvasGroupSistemaDelta.interactable = true;
            canvasGroupSistemaDelta.blocksRaycasts = true;
        }
    }

    private void AplicarCursorNormal()
    {
        if (!usarCursorCustomizadoDelta || cursorNormal == null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        if (cursorNormalProcessado != null)
            Destroy(cursorNormalProcessado);

        cursorNormalProcessado = CriarCursorEscalado(cursorNormal, escalaCursorNormal);

        if (cursorNormalProcessado == null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        Cursor.SetCursor(cursorNormalProcessado, hotspotCursorNormal * escalaCursorNormal, CursorMode.Auto);
    }

    private void AplicarCursorClique()
    {
        if (!usarCursorCustomizadoDelta || cursorClique == null)
        {
            AplicarCursorNormal();
            return;
        }

        if (cursorCliqueProcessado != null)
            Destroy(cursorCliqueProcessado);

        cursorCliqueProcessado = CriarCursorEscalado(cursorClique, escalaCursorClique);

        if (cursorCliqueProcessado == null)
        {
            AplicarCursorNormal();
            return;
        }

        Cursor.SetCursor(cursorCliqueProcessado, hotspotCursorClique * escalaCursorClique, CursorMode.Auto);
    }

    private Texture2D CriarCursorEscalado(Texture2D original, float escala)
    {
        if (original == null)
            return null;

        escala = Mathf.Clamp(escala, 0.1f, 5f);

        int largura = Mathf.Max(1, Mathf.RoundToInt(original.width * escala));
        int altura = Mathf.Max(1, Mathf.RoundToInt(original.height * escala));

        RenderTexture rt = RenderTexture.GetTemporary(largura, altura, 0, RenderTextureFormat.ARGB32);
        RenderTexture anterior = RenderTexture.active;

        try
        {
            Graphics.Blit(original, rt);
            RenderTexture.active = rt;

            Texture2D nova = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
            nova.ReadPixels(new Rect(0, 0, largura, altura), 0, 0);
            nova.Apply(false, false);

            return nova;
        }
        catch
        {
            return null;
        }
        finally
        {
            RenderTexture.active = anterior;
            RenderTexture.ReleaseTemporary(rt);
        }
    }

    private void RestaurarCursor()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        Cursor.lockState = cursorLockOriginal;
        Cursor.visible = cursorVisibleOriginal;
    }

    private void AtivarObjetoComPais(GameObject obj)
    {
        if (obj == null)
            return;

        Transform atual = obj.transform;

        while (atual != null)
        {
            if (!atual.gameObject.activeSelf)
                atual.gameObject.SetActive(true);

            atual = atual.parent;
        }
    }

    private IEnumerator RotinaCreditosFinal()
    {
        if (blackBackground != null)
        {
            blackBackground.gameObject.SetActive(true);

            float t = 0f;
            blackScreenFadeTime = Mathf.Max(0.001f, blackScreenFadeTime);

            while (t < blackScreenFadeTime)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / blackScreenFadeTime);

                Color c = blackBackground.color;
                c.a = Mathf.Lerp(0f, 1f, p);
                blackBackground.color = c;

                yield return null;
            }
        }

        if (creditNameCanvas != null)
        {
            creditNameCanvas.gameObject.SetActive(true);
            creditNameCanvas.alpha = 1f;
        }

        if (creditosRect != null)
        {
            Vector2 pos = creditosRect.anchoredPosition;
            pos.y = creditosYInicial;
            creditosRect.anchoredPosition = pos;

            float t = 0f;
            creditosScrollTime = Mathf.Max(0.001f, creditosScrollTime);

            while (t < creditosScrollTime)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / creditosScrollTime);

                pos = creditosRect.anchoredPosition;
                pos.y = Mathf.Lerp(creditosYInicial, creditosYFinal, p);
                creditosRect.anchoredPosition = pos;

                yield return null;
            }
        }
        else
        {
            yield return new WaitForSecondsRealtime(creditosScrollTime);
        }

        yield return new WaitForSecondsRealtime(tempoDepoisDosCreditos);

        RestaurarCursor();
        DestravarJogador();

        SceneManager.LoadScene(menuSceneName);
    }
}
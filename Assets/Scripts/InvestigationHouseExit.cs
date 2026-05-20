using UnityEngine; 
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using QuantumTek.SimpleMenu;

public class InvestigationHouseExit : MonoBehaviour
{
    [Header("--- CONFIGURAÇÃO ---")]
    public string nomeDaCenaPrincipal = "DreamSceane";

    [Header("--- RETORNO EXATO ---")]
    [Tooltip("Opcional. Se preencher, tenta usar as chaves específicas da porta. Se deixar vazio, usa o retorno global da casa.")]
    public string uniqueIDDaPortaDeEntrada = "";

    [Tooltip("Se ativado, aplica a rotação Y salva no ponto de retorno.")]
    public bool aplicarRotacaoYSalva = true;

    [Header("--- LOADING INDIVIDUAL ---")]
    public GameObject painelLoading;
    public SM_Bar barraDeProgresso;
    public float tempoExtraLoading = 1.5f;
    public float esperaDepoisDaCenaPronta = 0.25f;
    public float fadeOutLoadingFinal = 0.35f;
    public bool esperarGameManagerCenaPronta = true;

    [Header("--- UI E FEEDBACK ---")]
    public GameObject textoPortaTrancada; 
    public AudioSource fonteAudio;
    public AudioClip somTrancado;    
    public AudioClip somSairDaCasa;  

    private bool jaEstaSaindo = false;

    void Start()
    {
        if (textoPortaTrancada)
            textoPortaTrancada.SetActive(false);

        if (painelLoading)
            painelLoading.SetActive(false);
    }

    public void Interagir()
    {
        if (jaEstaSaindo) return;

        StartCoroutine(SairDaCasaComSom());
    }

    IEnumerator SairDaCasaComSom()
    {
        jaEstaSaindo = true;
        
        if (textoPortaTrancada)
            textoPortaTrancada.SetActive(false);

        if (FPS_Master.Instance != null)
            FPS_Master.travadoInteracao = true;

        PrepararRetornoExatoParaCenaPrincipal();

        if (somSairDaCasa && fonteAudio) 
        {
            fonteAudio.PlayOneShot(somSairDaCasa);
            yield return new WaitForSeconds(Mathf.Min(somSairDaCasa.length, 1f)); 
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        StartCoroutine(CarregarCenaComLoadingIndividual(nomeDaCenaPrincipal));
    }

    IEnumerator CarregarCenaComLoadingIndividual(string nomeCena)
    {
        PrepararLoadingParaTrocaDeCena();

        if (barraDeProgresso)
            barraDeProgresso.SetFill(0f);

        AsyncOperation operacao = SceneManager.LoadSceneAsync(nomeCena);

        if (operacao == null)
        {
            Debug.LogError("[InvestigationHouseExit] Falha ao carregar cena: " + nomeCena);
            yield break;
        }

        operacao.allowSceneActivation = false;

        float progressoVisual = 0f;

        while (progressoVisual < 0.8f)
        {
            float progressoReal = Mathf.Clamp01(operacao.progress / 0.9f) * 0.8f;
            progressoVisual = Mathf.MoveTowards(progressoVisual, progressoReal, Time.unscaledDeltaTime * 0.8f);

            if (barraDeProgresso)
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

            if (barraDeProgresso)
                barraDeProgresso.SetFill(progressoVisual);

            yield return null;
        }

        if (barraDeProgresso)
            barraDeProgresso.SetFill(1f);

        operacao.allowSceneActivation = true;

        yield return new WaitUntil(() => operacao.isDone);

        yield return null;
        yield return new WaitForEndOfFrame();

        if (esperarGameManagerCenaPronta && GameManager.Instance != null)
        {
            yield return new WaitUntil(() => GameManager.CenaPronta);
        }

        if (esperaDepoisDaCenaPronta > 0f)
            yield return new WaitForSecondsRealtime(esperaDepoisDaCenaPronta);

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (fadeOutLoadingFinal > 0f)
            yield return StartCoroutine(FadeOutLoadingIndividual());
        else if (painelLoading)
            painelLoading.SetActive(false);

        if (painelLoading)
            Destroy(painelLoading);

        Destroy(gameObject);
    }

    void PrepararLoadingParaTrocaDeCena()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (transform.parent != null)
            transform.SetParent(null, true);

        DontDestroyOnLoad(gameObject);

        if (painelLoading)
        {
            painelLoading.transform.SetParent(null, true);
            DontDestroyOnLoad(painelLoading);

            Canvas canvas = painelLoading.GetComponent<Canvas>();
            if (canvas == null) canvas = painelLoading.AddComponent<Canvas>();

            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;

            CanvasGroup cg = painelLoading.GetComponent<CanvasGroup>();
            if (cg == null) cg = painelLoading.AddComponent<CanvasGroup>();

            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;

            painelLoading.SetActive(true);
        }
    }

    IEnumerator FadeOutLoadingIndividual()
    {
        if (!painelLoading)
            yield break;

        CanvasGroup cg = painelLoading.GetComponent<CanvasGroup>();

        if (cg == null)
        {
            painelLoading.SetActive(false);
            yield break;
        }

        float t = 0f;
        float alphaInicial = cg.alpha;

        while (t < fadeOutLoadingFinal)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(alphaInicial, 0f, t / Mathf.Max(0.01f, fadeOutLoadingFinal));
            yield return null;
        }

        cg.alpha = 0f;
        painelLoading.SetActive(false);
    }

    private void PrepararRetornoExatoParaCenaPrincipal()
    {
        if (PersistenciaManager.Instance == null)
            return;

        string prefixoEspecifico = uniqueIDDaPortaDeEntrada;
        bool temRetornoEspecifico = false;

        if (!string.IsNullOrEmpty(prefixoEspecifico))
            temRetornoEspecifico = PersistenciaManager.Instance.ObterEstado(prefixoEspecifico + "_HasReturnPoint", false);

        bool temRetornoGlobal = PersistenciaManager.Instance.ObterEstado("InvestigationHouse_HasReturnPoint", false);

        string prefixoFinal = "";

        if (temRetornoEspecifico)
            prefixoFinal = prefixoEspecifico + "_Return";
        else if (temRetornoGlobal)
            prefixoFinal = "InvestigationHouse_Return";
        else
        {
            if (SistemaGlobal.Instance != null)
                SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = true;

            PersistenciaManager.Instance.SalvarTudo(true);
            return;
        }

        float x = PersistenciaManager.Instance.ObterFloat(prefixoFinal + "_PosX");
        float y = PersistenciaManager.Instance.ObterFloat(prefixoFinal + "_PosY");
        float z = PersistenciaManager.Instance.ObterFloat(prefixoFinal + "_PosZ");
        float rotY = PersistenciaManager.Instance.ObterFloat(prefixoFinal + "_RotY");

        if (SistemaGlobal.Instance != null && SistemaGlobal.Instance.slotFoiDefinido)
        {
            string prefixoSlot = "Slot_" + SistemaGlobal.Instance.slotAtual;

            PersistenciaManager.Instance.SalvarString(prefixoSlot + "_Cena", nomeDaCenaPrincipal);
            PersistenciaManager.Instance.SalvarFloat(prefixoSlot + "_PosX", x);
            PersistenciaManager.Instance.SalvarFloat(prefixoSlot + "_PosY", y);
            PersistenciaManager.Instance.SalvarFloat(prefixoSlot + "_PosZ", z);

            if (aplicarRotacaoYSalva)
                PersistenciaManager.Instance.SalvarFloat(prefixoSlot + "_RotY", rotY);

            SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = true;
        }

        PersistenciaManager.Instance.SalvarTudo(true);

        Debug.Log("[InvestigationHouseExit] Retorno preparado para: " + new Vector3(x, y, z));
    }

    IEnumerator AvisoTrancado()
    {
        if (somTrancado && fonteAudio)
            fonteAudio.PlayOneShot(somTrancado);
        
        if (textoPortaTrancada)
            textoPortaTrancada.SetActive(true);

        yield return new WaitForSeconds(2.5f);

        if (textoPortaTrancada)
            textoPortaTrancada.SetActive(false);
    }
}
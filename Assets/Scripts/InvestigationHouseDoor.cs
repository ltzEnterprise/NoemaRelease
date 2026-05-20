using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using QuantumTek.SimpleMenu;

public class InvestigationHouseDoor : MonoBehaviour
{
    [Header("--- SAVE SYSTEM ---")]
    [Tooltip("EXTREMAMENTE IMPORTANTE! Ex: Porta_Investigacao_Floresta")]
    public string uniqueID; 

    [Header("--- CONFIGURAÇÃO DE CENA ---")]
    public string nomeDaCenaParaCarregar = "InvestigacaoCena";
    
    [Header("--- CONFIGURAÇÃO DE SPAWN (Ao voltar) ---")]
    public Transform pontoDeRetorno; 

    [Tooltip("Altura extra aplicada ao ponto de retorno para evitar nascer dentro do chão.")]
    public float offsetVerticalRetorno = 0.15f;

    [Tooltip("Se ativado, ao voltar da casa o player usa a rotação Y do pontoDeRetorno.")]
    public bool usarRotacaoYDoPontoDeRetorno = true;

    [Header("--- LOADING INDIVIDUAL ---")]
    public GameObject painelLoading;
    public SM_Bar barraDeProgresso;
    public float tempoExtraLoading = 1.5f;
    public float esperaDepoisDaCenaPronta = 0.25f;
    public float fadeOutLoadingFinal = 0.35f;
    public bool esperarGameManagerCenaPronta = true;

    [Header("--- REQUISITOS (CHAVE E BARRICADA) ---")]
    public string idChaveNecessaria = "Chave_Casa_Noite";
    public GameObject hudIconeChaveParaApagar; 
    public List<GameObject> madeirasBloqueio; 

    [Header("--- UI E EFEITOS ---")]
    public GameObject textoInteragirUI; 
    public GameObject textoSemChaveUI;  
    
    public AudioSource audioSource;
    public AudioClip somTrancada;
    public AudioClip somDestrancar;
    public AudioClip somAbrirPorta;

    private bool interagindo = false;
    private bool estaDestrancadaPraSempre = false; 
    private bool inicializado = false;
    private Coroutine rotinaMensagemSemChave;

    void Start()
    {
        if (textoSemChaveUI)
            textoSemChaveUI.SetActive(false);

        if (textoInteragirUI)
            textoInteragirUI.SetActive(false);

        if (painelLoading)
            painelLoading.SetActive(false);

        if (!audioSource)
            audioSource = gameObject.AddComponent<AudioSource>();

        StartCoroutine(CarregarEstadoSeguro());
    }

    IEnumerator CarregarEstadoSeguro()
    {
        if (PersistenciaManager.Instance != null)
            yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            if (PersistenciaManager.Instance.ObterEstado(uniqueID, false))
                estaDestrancadaPraSempre = true;
        }

        inicializado = true;
    }

    public void AoOlhar()
    {
        if (!inicializado) return;
        if (interagindo) return;

        if (TemMadeiraBloqueando()) 
        {
            if (textoInteragirUI)
                textoInteragirUI.SetActive(false);

            return;
        }

        if (textoInteragirUI)
            textoInteragirUI.SetActive(true);
    }

    public void AoSair()
    {
        if (textoInteragirUI)
            textoInteragirUI.SetActive(false);
    }

    public void Interagir()
    {
        if (!inicializado) return;
        if (interagindo) return;
        if (TemMadeiraBloqueando()) return; 

        if (estaDestrancadaPraSempre)
        {
            StartCoroutine(EntrarNaCasa(false)); 
            return;
        }

        if (KeySystem.TemChave(idChaveNecessaria))
        {
            StartCoroutine(EntrarNaCasa(true)); 
        }
        else
        {
            if (rotinaMensagemSemChave != null)
                StopCoroutine(rotinaMensagemSemChave);

            rotinaMensagemSemChave = StartCoroutine(MostrarMensagemSemChave());
        }
    }

    bool TemMadeiraBloqueando()
    {
        if (madeirasBloqueio == null)
            return false;

        foreach (GameObject m in madeirasBloqueio)
        {
            if (m != null && m.activeInHierarchy)
                return true;
        }

        return false;
    }

    IEnumerator EntrarNaCasa(bool primeiraVezComChave)
    {
        interagindo = true;

        if (textoInteragirUI)
            textoInteragirUI.SetActive(false);

        if (textoSemChaveUI)
            textoSemChaveUI.SetActive(false);

        if (FPS_Master.Instance != null)
            FPS_Master.travadoInteracao = true;

        if (primeiraVezComChave)
        {
            KeySystem.GastarChave(idChaveNecessaria);

            if (hudIconeChaveParaApagar)
                hudIconeChaveParaApagar.SetActive(false);

            if (audioSource && somDestrancar)
                audioSource.PlayOneShot(somDestrancar);

            yield return new WaitForSeconds(0.5f);

            estaDestrancadaPraSempre = true;

            if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
                PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);
        }

        SalvarPontoExatoDeRetornoAntesDeEntrar();

        if (audioSource && somAbrirPorta)
            audioSource.PlayOneShot(somAbrirPorta);

        yield return new WaitForSeconds(1f); 

        StartCoroutine(CarregarCenaComLoadingIndividual(nomeDaCenaParaCarregar));
    }

    IEnumerator CarregarCenaComLoadingIndividual(string nomeCena)
    {
        PrepararLoadingParaTrocaDeCena();

        if (barraDeProgresso)
            barraDeProgresso.SetFill(0f);

        AsyncOperation operacao = SceneManager.LoadSceneAsync(nomeCena);

        if (operacao == null)
        {
            Debug.LogError("[InvestigationHouseDoor] Falha ao carregar cena: " + nomeCena);
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

    private void SalvarPontoExatoDeRetornoAntesDeEntrar()
    {
        Vector3 posicaoParaVoltar;

        if (pontoDeRetorno != null)
            posicaoParaVoltar = pontoDeRetorno.position + Vector3.up * offsetVerticalRetorno;
        else
            posicaoParaVoltar = transform.position + Vector3.up * offsetVerticalRetorno;

        float rotY = pontoDeRetorno != null ? pontoDeRetorno.eulerAngles.y : transform.eulerAngles.y;

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarFloat("InvestigationHouse_Return_PosX", posicaoParaVoltar.x);
            PersistenciaManager.Instance.SalvarFloat("InvestigationHouse_Return_PosY", posicaoParaVoltar.y);
            PersistenciaManager.Instance.SalvarFloat("InvestigationHouse_Return_PosZ", posicaoParaVoltar.z);
            PersistenciaManager.Instance.SalvarFloat("InvestigationHouse_Return_RotY", rotY);
            PersistenciaManager.Instance.RegistrarEstado("InvestigationHouse_HasReturnPoint", true);

            if (!string.IsNullOrEmpty(uniqueID))
            {
                PersistenciaManager.Instance.SalvarFloat(uniqueID + "_Return_PosX", posicaoParaVoltar.x);
                PersistenciaManager.Instance.SalvarFloat(uniqueID + "_Return_PosY", posicaoParaVoltar.y);
                PersistenciaManager.Instance.SalvarFloat(uniqueID + "_Return_PosZ", posicaoParaVoltar.z);
                PersistenciaManager.Instance.SalvarFloat(uniqueID + "_Return_RotY", rotY);
                PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_HasReturnPoint", true);
            }
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(true);

        Debug.Log("[InvestigationHouseDoor] Ponto exato de retorno salvo: " + posicaoParaVoltar);
    }

    IEnumerator MostrarMensagemSemChave()
    {
        if (textoInteragirUI)
            textoInteragirUI.SetActive(false);

        if (textoSemChaveUI)
            textoSemChaveUI.SetActive(true);

        if (audioSource && somTrancada)
            audioSource.PlayOneShot(somTrancada);

        yield return new WaitForSeconds(2.5f);

        if (textoSemChaveUI)
            textoSemChaveUI.SetActive(false);

        if (!interagindo && !TemMadeiraBloqueando() && textoInteragirUI)
            textoInteragirUI.SetActive(true);

        rotinaMensagemSemChave = null;
    }
}
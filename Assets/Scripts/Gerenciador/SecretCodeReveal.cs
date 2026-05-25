using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

public class SecretCodeReveal : MonoBehaviour
{
    [Header("--- SAVE SYSTEM ---")]
    [Tooltip("ID único desse segredo. Ex: Segredo_Codigo_Setas_Castelo")]
    public string uniqueID = "Segredo_Codigo_Setas";

    [Tooltip("Se ativado, depois de revelar uma vez, a sequência não faz mais nada.")]
    public bool ativarApenasUmaVez = true;

    [Header("--- CONFIGURAÇÃO ---")]
    [Tooltip("A sequência de teclas para ativar o segredo.")]
    public List<KeyCode> sequenciaSecreta;

    [Header("--- O QUE ACONTECE ---")]
    [Tooltip("O objeto (ou grupo) que vai aparecer no mapa.")]
    public GameObject objetoParaAparecer;

    [Tooltip("O som que toca ao acertar.")]
    public AudioClip somSucesso;

    [Tooltip("Volume do som (0 a 1).")]
    [Range(0f, 1f)] public float volumeSom = 1f;

    [Header("--- TROCA DE TEMPO OPCIONAL ---")]
    [Tooltip("Se ligado, ao completar o código troca o tempo: Noite -> Manhã, Manhã -> Tarde, Tarde -> Noite.")]
    public bool trocarTempoAoCompletarCodigo = false;

    [Tooltip("Se ligado, a troca de tempo só acontece uma vez no save deste segredo.")]
    public bool trocarTempoApenasUmaVez = false;

    [Header("--- RELÂMPAGO NO CÉU ---")]
    [Tooltip("Ativa um flash rápido de exposição no skybox ao revelar o segredo.")]
    public bool usarRelampagoNoCeu = true;

    [Tooltip("Exposição máxima durante o clarão.")]
    public float exposicaoRelampago = 2.4f;

    [Tooltip("Tempo para subir a exposição.")]
    public float tempoSubidaRelampago = 0.04f;

    [Tooltip("Tempo segurando o pico do clarão.")]
    public float tempoPicoRelampago = 0.05f;

    [Tooltip("Tempo para voltar à exposição original.")]
    public float tempoRetornoRelampago = 0.18f;

    [Tooltip("Quantidade de piscadas. 1 = relâmpago simples, 2 = relâmpago duplo.")]
    public int quantidadePiscadas = 2;

    [Tooltip("Intervalo entre piscadas.")]
    public float intervaloEntrePiscadas = 0.08f;

    [Header("--- EVENTOS OPCIONAIS ---")]
    public UnityEvent aoRevelarSegredo;

    private int indexAtual = 0;
    private AudioSource audioSource;
    private bool segredoRevelado = false;
    private bool tempoJaTrocado = false;
    private bool inicializado = false;
    private bool relampagoRodando = false;

    private Material skyboxOriginal;
    private Material skyboxInstanciado;
    private float exposicaoOriginal = 1f;

    private string ChaveRevelado
    {
        get { return uniqueID + "_Revelado"; }
    }

    private string ChaveObjetoApareceu
    {
        get { return uniqueID + "_ObjetoApareceu"; }
    }

    private string ChaveTempoTrocado
    {
        get { return uniqueID + "_TempoTrocado"; }
    }

    void Start()
    {
        if (objetoParaAparecer != null)
            objetoParaAparecer.SetActive(false);

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;

        if (sequenciaSecreta == null || sequenciaSecreta.Count == 0)
        {
            sequenciaSecreta = new List<KeyCode>
            {
                KeyCode.UpArrow,
                KeyCode.LeftArrow,
                KeyCode.UpArrow,
                KeyCode.RightArrow,
                KeyCode.DownArrow,
                KeyCode.RightArrow
            };
        }

        StartCoroutine(InicializarSeguro());
    }

    IEnumerator InicializarSeguro()
    {
        if (PersistenciaManager.Instance != null)
        {
            yield return new WaitUntil(() =>
                PersistenciaManager.Instance.DadosProntosParaUso &&
                !PersistenciaManager.Instance.EstaCarregando
            );

            if (!string.IsNullOrEmpty(uniqueID))
            {
                segredoRevelado = PersistenciaManager.Instance.ObterEstado(ChaveRevelado, false);
                tempoJaTrocado = PersistenciaManager.Instance.ObterEstado(ChaveTempoTrocado, false);
            }
        }

        if (segredoRevelado)
            AplicarEstadoRevelado(false);

        inicializado = true;
    }

    void Update()
    {
        if (!inicializado) return;
        if (sequenciaSecreta == null || sequenciaSecreta.Count == 0) return;

        if (ativarApenasUmaVez && segredoRevelado) return;

        if (Input.anyKeyDown)
        {
            if (Input.GetKeyDown(sequenciaSecreta[indexAtual]))
            {
                indexAtual++;

                if (indexAtual >= sequenciaSecreta.Count)
                {
                    AtivarSegredo();
                    indexAtual = 0;
                }
            }
            else
            {
                if (Input.GetKeyDown(sequenciaSecreta[0]))
                    indexAtual = 1;
                else
                    indexAtual = 0;
            }
        }
    }

    void AtivarSegredo()
    {
        if (ativarApenasUmaVez && segredoRevelado)
            return;

        Debug.Log("SENHA SECRETA ATIVADA!");

        segredoRevelado = true;

        AplicarEstadoRevelado(true);
        TrocarTempoOpcional();

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            PersistenciaManager.Instance.RegistrarEstado(ChaveRevelado, true);

            if (objetoParaAparecer != null)
                PersistenciaManager.Instance.RegistrarEstado(ChaveObjetoApareceu, true);

            PersistenciaManager.Instance.RegistrarEstado(ChaveTempoTrocado, tempoJaTrocado);

            PersistenciaManager.Instance.SalvarTudo(true);
        }
    }

    private void TrocarTempoOpcional()
    {
        if (!trocarTempoAoCompletarCodigo)
            return;

        if (trocarTempoApenasUmaVez && tempoJaTrocado)
            return;

        if (DayNightCycle.Instance == null)
        {
            Debug.LogError("[SecretCodeReveal] trocarTempoAoCompletarCodigo está ligado, mas DayNightCycle.Instance está nulo.");
            return;
        }

        DayNightCycle.TimeState estadoAtual = DayNightCycle.Instance.currentState;
        DayNightCycle.TimeState novoEstado;

        switch (estadoAtual)
        {
            case DayNightCycle.TimeState.Night:
                novoEstado = DayNightCycle.TimeState.InitialDay;
                break;

            case DayNightCycle.TimeState.InitialDay:
                novoEstado = DayNightCycle.TimeState.DramaticDay;
                break;

            case DayNightCycle.TimeState.DramaticDay:
                novoEstado = DayNightCycle.TimeState.Night;
                break;

            default:
                novoEstado = DayNightCycle.TimeState.InitialDay;
                break;
        }

        DayNightCycle.Instance.ChangeTo(novoEstado);

        tempoJaTrocado = true;

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            PersistenciaManager.Instance.RegistrarEstado(ChaveTempoTrocado, true);
            PersistenciaManager.Instance.SalvarTudo(true);
        }

        Debug.Log("[SecretCodeReveal] Tempo alterado pelo código secreto: " + estadoAtual + " -> " + novoEstado);
    }

    private void AplicarEstadoRevelado(bool tocarEfeitos)
    {
        if (objetoParaAparecer != null)
            objetoParaAparecer.SetActive(true);

        if (tocarEfeitos)
        {
            if (somSucesso != null && audioSource != null)
                audioSource.PlayOneShot(somSucesso, volumeSom);

            if (usarRelampagoNoCeu && !relampagoRodando)
                StartCoroutine(EfeitoRelampagoNoCeu());

            if (aoRevelarSegredo != null)
                aoRevelarSegredo.Invoke();
        }
    }

    IEnumerator EfeitoRelampagoNoCeu()
    {
        relampagoRodando = true;

        skyboxOriginal = RenderSettings.skybox;

        if (skyboxOriginal == null)
        {
            relampagoRodando = false;
            yield break;
        }

        skyboxInstanciado = new Material(skyboxOriginal);
        RenderSettings.skybox = skyboxInstanciado;

        if (skyboxInstanciado.HasProperty("_Exposure"))
            exposicaoOriginal = skyboxInstanciado.GetFloat("_Exposure");
        else
        {
            relampagoRodando = false;
            yield break;
        }

        int piscadas = Mathf.Max(1, quantidadePiscadas);

        for (int i = 0; i < piscadas; i++)
        {
            yield return StartCoroutine(AnimarExposicao(exposicaoOriginal, exposicaoRelampago, tempoSubidaRelampago));

            yield return new WaitForSeconds(tempoPicoRelampago);

            yield return StartCoroutine(AnimarExposicao(exposicaoRelampago, exposicaoOriginal, tempoRetornoRelampago));

            if (i < piscadas - 1)
                yield return new WaitForSeconds(intervaloEntrePiscadas);
        }

        if (skyboxInstanciado != null && skyboxInstanciado.HasProperty("_Exposure"))
            skyboxInstanciado.SetFloat("_Exposure", exposicaoOriginal);

        DynamicGI.UpdateEnvironment();

        if (skyboxOriginal != null)
            RenderSettings.skybox = skyboxOriginal;

        if (skyboxInstanciado != null)
            Destroy(skyboxInstanciado);

        skyboxInstanciado = null;
        relampagoRodando = false;
    }

    IEnumerator AnimarExposicao(float inicio, float fim, float duracao)
    {
        if (skyboxInstanciado == null || !skyboxInstanciado.HasProperty("_Exposure"))
            yield break;

        if (duracao <= 0f)
        {
            skyboxInstanciado.SetFloat("_Exposure", fim);
            DynamicGI.UpdateEnvironment();
            yield break;
        }

        float t = 0f;

        while (t < duracao)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duracao);

            float valor = Mathf.Lerp(inicio, fim, p);
            skyboxInstanciado.SetFloat("_Exposure", valor);
            DynamicGI.UpdateEnvironment();

            yield return null;
        }

        skyboxInstanciado.SetFloat("_Exposure", fim);
        DynamicGI.UpdateEnvironment();
    }

    void OnDestroy()
    {
        if (skyboxOriginal != null && RenderSettings.skybox == skyboxInstanciado)
        {
            RenderSettings.skybox = skyboxOriginal;
            DynamicGI.UpdateEnvironment();
        }

        if (skyboxInstanciado != null)
            Destroy(skyboxInstanciado);
    }
}
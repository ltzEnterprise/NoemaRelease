using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.SceneManagement; 
using System.Collections;

[System.Serializable]
public class SlotConfigRuna
{
    public string nomeIdentificador;
    public RunaData data;
    public UnityEvent eventoParaColetar; 
}

public class InventarioRunas : MonoBehaviour
{
    public static InventarioRunas Instance;

    [Header("Configuração das Runas")]
    public List<SlotConfigRuna> configuracaoRunas = new List<SlotConfigRuna>();

    [Header("--- CENAS PERMITIDAS ---")]
    [Tooltip("Coloque aqui EXATAMENTE os nomes das cenas onde o inventário de runas pode existir e aparecer.")]
    public List<string> cenasPermitidasParaRunas = new List<string>();

    [Header("--- CLIMA POR RUNAS ---")]
    [Tooltip("Quantidade de runas necessárias para mudar o clima para tarde/DramaticDay.")]
    public int quantidadeRunasParaMudarParaTarde = 3;

    [Header("Runas Atualmente Coletadas")]
    public List<RunaData> runasNaMao = new List<RunaData>();

    [HideInInspector] public Vector3 ultimaPosicaoSalva;
    [HideInInspector] public bool deveCarregarPosicao = false;

    private Coroutine rotinaCarregar;
    private Coroutine rotinaRedesenhar;
    private Coroutine rotinaVerificarClima;
    private bool registradoNoSceneLoaded = false;
    private bool cenaPermitidaAtual = false;

    private void Awake() 
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (transform.parent != null)
            transform.SetParent(null, true);

        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded -= AoCarregarCena;
        SceneManager.sceneLoaded += AoCarregarCena;
        registradoNoSceneLoaded = true;

        cenaPermitidaAtual = CenaAtualEstaPermitida();
    }

    private void Start()
    {
        if (Instance != this) return;

        rotinaCarregar = StartCoroutine(CarregarRunasDoSaveSeguro());
    }

    private void OnDestroy()
    {
        if (registradoNoSceneLoaded)
            SceneManager.sceneLoaded -= AoCarregarCena;

        if (Instance == this)
            Instance = null;
    }

    private void AoCarregarCena(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this) return;

        cenaPermitidaAtual = CenaPermitida(scene.name);

        if (rotinaRedesenhar != null)
            StopCoroutine(rotinaRedesenhar);

        rotinaRedesenhar = StartCoroutine(RedesenharRunasNaTelaSeguro());
    }

    private bool CenaAtualEstaPermitida()
    {
        string cenaAtual = SceneManager.GetActiveScene().name;
        return CenaPermitida(cenaAtual);
    }

    private bool CenaPermitida(string nomeCena)
    {
        if (cenasPermitidasParaRunas == null || cenasPermitidasParaRunas.Count == 0)
        {
            Debug.LogError("[InventarioRunas] Nenhuma cena permitida foi configurada. Adicione os nomes das cenas no Inspector.");
            return false;
        }

        return cenasPermitidasParaRunas.Contains(nomeCena);
    }

    public void RecarregarDoSave()
    {
        if (Instance != this) return;

        runasNaMao.Clear();

        if (rotinaCarregar != null)
            StopCoroutine(rotinaCarregar);

        rotinaCarregar = StartCoroutine(CarregarRunasDoSaveSeguro());
    }

    IEnumerator RedesenharRunasNaTelaSeguro()
    {
        yield return null;

        float timeout = 2f;

        while (AreaDasRunas.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (AreaDasRunas.Instance != null)
            AreaDasRunas.Instance.LimparTodasAsRunasDaTela();

        if (!cenaPermitidaAtual)
        {
            rotinaRedesenhar = null;
            yield break;
        }

        foreach (var runa in runasNaMao)
        {
            if (runa != null)
                AtualizarUI(runa.icone);
        }

        rotinaRedesenhar = null;
    }

    System.Collections.IEnumerator RedesenharRunasNaTela()
    {
        yield return RedesenharRunasNaTelaSeguro();
    }

    private System.Collections.IEnumerator CarregarRunasDoSaveSeguro()
    {
        yield return new WaitUntil(() =>
            PersistenciaManager.Instance != null &&
            PersistenciaManager.Instance.DadosProntosParaUso &&
            !PersistenciaManager.Instance.EstaCarregando
        );

        CarregarRunasDoSave();
        rotinaCarregar = null;

        VerificarMudancaClimaPorRunasSeguro();
    }

    private void CarregarRunasDoSave()
    {
        if (PersistenciaManager.Instance == null) return;

        runasNaMao.Clear();

        if (configuracaoRunas == null)
            configuracaoRunas = new List<SlotConfigRuna>();

        foreach (var slot in configuracaoRunas)
        {
            if (slot == null || slot.data == null || string.IsNullOrEmpty(slot.nomeIdentificador))
                continue;

            bool temRuna = PersistenciaManager.Instance.ObterEstado("Runa_" + slot.nomeIdentificador, false);

            if (temRuna && !runasNaMao.Contains(slot.data))
                runasNaMao.Add(slot.data);
        }

        if (rotinaRedesenhar != null)
            StopCoroutine(rotinaRedesenhar);

        rotinaRedesenhar = StartCoroutine(RedesenharRunasNaTelaSeguro());
    }

    public void SalvarPosicaoAtual(Vector3 posicao)
    {
        ultimaPosicaoSalva = posicao;
        deveCarregarPosicao = true;
    }

    public void ColetarRunaPeloNome(string nome)
    {
        ColetarRunaInterno(nome, true);
    }

    public void ColetarRunaSemForcarSaveHD(string nome)
    {
        ColetarRunaInterno(nome, false);
    }

    private void ColetarRunaInterno(string nome, bool salvarNoDisco)
    {
        if (string.IsNullOrEmpty(nome)) return;

        if (!cenaPermitidaAtual)
        {
            Debug.LogWarning("[InventarioRunas] Tentativa de coletar runa em cena não permitida: " + SceneManager.GetActiveScene().name);
            return;
        }

        if (configuracaoRunas == null)
            configuracaoRunas = new List<SlotConfigRuna>();

        SlotConfigRuna slot = configuracaoRunas.Find(x => x != null && x.nomeIdentificador == nome);

        if (slot != null && slot.data != null)
        {
            if (!runasNaMao.Contains(slot.data))
            {
                runasNaMao.Add(slot.data);

                if (slot.eventoParaColetar != null)
                    slot.eventoParaColetar.Invoke();

                AtualizarUI(slot.data.icone);

                VerificarMudancaClimaPorRunasSeguro();
            }

            if (PersistenciaManager.Instance != null)
            {
                PersistenciaManager.Instance.RegistrarEstado("Runa_" + nome, true);

                if (salvarNoDisco)
                    SalvarProgressoSeguro();
            }
        }
        else
        {
            Debug.LogWarning("[InventarioRunas] Tentou coletar uma runa que não existe na configuracaoRunas: " + nome);
        }
    }

    private void VerificarMudancaClimaPorRunasSeguro()
    {
        if (rotinaVerificarClima != null)
            StopCoroutine(rotinaVerificarClima);

        rotinaVerificarClima = StartCoroutine(VerificarMudancaClimaPorRunasCoroutine());
    }

    private IEnumerator VerificarMudancaClimaPorRunasCoroutine()
    {
        if (runasNaMao == null)
            yield break;

        if (runasNaMao.Count < quantidadeRunasParaMudarParaTarde)
            yield break;

        float timeout = 3f;

        while ((DayNightCycle.Instance == null || !DayNightCycle.Instance.Inicializado) && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (DayNightCycle.Instance == null)
            yield break;

        if (DayNightCycle.Instance.currentState == DayNightCycle.TimeState.InitialDay)
        {
            DayNightCycle.Instance.ChangeTo(DayNightCycle.TimeState.DramaticDay);
        }

        rotinaVerificarClima = null;
    }

    public bool TemRuna(string nome)
    {
        if (string.IsNullOrEmpty(nome)) return false;

        if (configuracaoRunas == null)
            configuracaoRunas = new List<SlotConfigRuna>();

        SlotConfigRuna slot = configuracaoRunas.Find(x => x != null && x.nomeIdentificador == nome);

        if (slot == null || slot.data == null)
            return false;

        return runasNaMao.Contains(slot.data);
    }

    public bool TemARunaPeloNome(string nome)
    {
        return TemRuna(nome);
    }

    public void RemoverRuna(string nome)
    {
        if (string.IsNullOrEmpty(nome)) return;

        if (configuracaoRunas == null)
            configuracaoRunas = new List<SlotConfigRuna>();

        SlotConfigRuna slot = configuracaoRunas.Find(x => x != null && x.nomeIdentificador == nome);

        if (slot != null && slot.data != null && runasNaMao.Contains(slot.data))
        {
            runasNaMao.Remove(slot.data);

            if (PersistenciaManager.Instance != null)
            {
                PersistenciaManager.Instance.RegistrarEstado("Runa_" + nome, false);
                SalvarProgressoSeguro();
            }

            if (rotinaRedesenhar != null)
                StopCoroutine(rotinaRedesenhar);

            rotinaRedesenhar = StartCoroutine(RedesenharRunasNaTelaSeguro());
        }
    }

    public void UsarTodasAsRunasNoAltar()
    {
        if (configuracaoRunas == null)
            configuracaoRunas = new List<SlotConfigRuna>();

        foreach (var slot in configuracaoRunas)
        {
            if (slot == null || string.IsNullOrEmpty(slot.nomeIdentificador)) continue;

            if (PersistenciaManager.Instance != null)
                PersistenciaManager.Instance.RegistrarEstado("Runa_" + slot.nomeIdentificador, false);
        }

        runasNaMao.Clear();

        if (rotinaRedesenhar != null)
            StopCoroutine(rotinaRedesenhar);

        rotinaRedesenhar = StartCoroutine(RedesenharRunasNaTelaSeguro());

        SalvarProgressoSeguro();
    }

    private void AtualizarUI(Sprite icone)
    {
        if (!cenaPermitidaAtual) return;

        if (AreaDasRunas.Instance != null)
            AreaDasRunas.Instance.AdicionarRunaNaTela(icone);
    }

    private void SalvarProgressoSeguro()
    {
        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(true);
    }
}
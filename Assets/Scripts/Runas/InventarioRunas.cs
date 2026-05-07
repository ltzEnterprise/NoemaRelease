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
    public List<string> cenasPermitidasParaRunas = new List<string>();

    [Header("--- CLIMA POR RUNAS ---")]
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
    private bool saveCarregadoPeloMenosUmaVez = false;

    private HashSet<string> idsRunasColetadasNormalizados = new HashSet<string>();

    private const string SEPARADOR_RUNAS = "|";

    private void Awake() 
    {
        if (Instance != null && Instance != this)
        {
            CopiarConfiguracaoSeNecessario(Instance);
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

        RecarregarDoSave();
    }

    private void OnDestroy()
    {
        if (registradoNoSceneLoaded)
            SceneManager.sceneLoaded -= AoCarregarCena;

        if (Instance == this)
            Instance = null;
    }

    private void CopiarConfiguracaoSeNecessario(InventarioRunas alvo)
    {
        if (alvo == null) return;

        bool mudouAlgo = false;

        if (configuracaoRunas != null && configuracaoRunas.Count > 0)
        {
            if (alvo.configuracaoRunas == null)
                alvo.configuracaoRunas = new List<SlotConfigRuna>();

            foreach (SlotConfigRuna nova in configuracaoRunas)
            {
                if (nova == null || string.IsNullOrEmpty(nova.nomeIdentificador))
                    continue;

                string novaNorm = NormalizarIDRuna(nova.nomeIdentificador);

                bool jaExiste = alvo.configuracaoRunas.Exists(x =>
                    x != null &&
                    NormalizarIDRuna(x.nomeIdentificador) == novaNorm
                );

                if (!jaExiste)
                {
                    alvo.configuracaoRunas.Add(nova);
                    mudouAlgo = true;
                }
            }
        }

        if (cenasPermitidasParaRunas != null && cenasPermitidasParaRunas.Count > 0)
        {
            if (alvo.cenasPermitidasParaRunas == null)
                alvo.cenasPermitidasParaRunas = new List<string>();

            foreach (string cena in cenasPermitidasParaRunas)
            {
                if (string.IsNullOrEmpty(cena))
                    continue;

                if (!alvo.cenasPermitidasParaRunas.Contains(cena))
                {
                    alvo.cenasPermitidasParaRunas.Add(cena);
                    mudouAlgo = true;
                }
            }
        }

        if (mudouAlgo)
            alvo.RecarregarDoSave();
    }

    private void AoCarregarCena(Scene scene, LoadSceneMode mode)
    {
        if (Instance != this) return;

        cenaPermitidaAtual = CenaPermitida(scene.name);

        if (!saveCarregadoPeloMenosUmaVez)
            RecarregarDoSave();
        else
            RedesenharAgora();
    }

    private bool CenaAtualEstaPermitida()
    {
        return CenaPermitida(SceneManager.GetActiveScene().name);
    }

    private bool CenaPermitida(string nomeCena)
    {
        if (cenasPermitidasParaRunas == null || cenasPermitidasParaRunas.Count == 0)
            return true;

        return cenasPermitidasParaRunas.Contains(nomeCena);
    }

    public void RecarregarDoSave()
    {
        if (Instance != this) return;

        if (rotinaCarregar != null)
            StopCoroutine(rotinaCarregar);

        rotinaCarregar = StartCoroutine(CarregarRunasDoSaveSeguro());
    }

    private IEnumerator CarregarRunasDoSaveSeguro()
    {
        yield return new WaitUntil(() =>
            PersistenciaManager.Instance != null &&
            PersistenciaManager.Instance.DadosProntosParaUso &&
            !PersistenciaManager.Instance.EstaCarregando
        );

        CarregarRunasDoSave();

        saveCarregadoPeloMenosUmaVez = true;
        rotinaCarregar = null;

        VerificarMudancaClimaPorRunasSeguro();
    }

    private void CarregarRunasDoSave()
    {
        if (PersistenciaManager.Instance == null)
            return;

        idsRunasColetadasNormalizados.Clear();

        CarregarManifestoDeRunas();

        if (configuracaoRunas != null)
        {
            foreach (SlotConfigRuna slot in configuracaoRunas)
            {
                if (slot == null || string.IsNullOrEmpty(slot.nomeIdentificador))
                    continue;

                string nomeOriginal = slot.nomeIdentificador;
                string nomeNorm = NormalizarIDRuna(nomeOriginal);

                if (ObterEstadoRunaComVariantes(nomeOriginal, nomeNorm))
                    idsRunasColetadasNormalizados.Add(nomeNorm);
            }
        }

        ReconstruirListaVisualAPartirDosIDs();
        SalvarManifestoDeRunas();
        RedesenharAgora();
    }

    private bool ObterEstadoRunaComVariantes(string nomeOriginal, string nomeNorm)
    {
        if (PersistenciaManager.Instance == null)
            return false;

        string prefixo = PrefixoSlotAtual();

        if (PersistenciaManager.Instance.ObterEstado(prefixo + "Runa_" + nomeNorm, false))
            return true;

        if (PersistenciaManager.Instance.ObterEstado(prefixo + "Runa_" + nomeOriginal, false))
            return true;

        if (PersistenciaManager.Instance.ObterEstado(prefixo + "Runa_Runa_" + nomeNorm, false))
            return true;

        if (PersistenciaManager.Instance.ObterEstado(prefixo + "Runa_Runa_" + nomeOriginal, false))
            return true;

        if (PersistenciaManager.Instance.ObterEstado("Runa_" + nomeNorm, false))
            return true;

        if (PersistenciaManager.Instance.ObterEstado("Runa_" + nomeOriginal, false))
            return true;

        if (PersistenciaManager.Instance.ObterEstado("Runa_Runa_" + nomeNorm, false))
            return true;

        if (PersistenciaManager.Instance.ObterEstado("Runa_Runa_" + nomeOriginal, false))
            return true;

        return false;
    }

    private void CarregarManifestoDeRunas()
    {
        if (PersistenciaManager.Instance == null)
            return;

        string manifestoSlot = PersistenciaManager.Instance.ObterString(ChaveManifestoSlot(), "");
        string manifestoLegado = PersistenciaManager.Instance.ObterString("Runas_Coletadas", "");

        LerManifesto(manifestoSlot);
        LerManifesto(manifestoLegado);
    }

    private void LerManifesto(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return;

        string[] partes = texto.Split(new string[] { SEPARADOR_RUNAS }, System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string parte in partes)
        {
            string id = NormalizarIDRuna(parte.Trim());

            if (!string.IsNullOrEmpty(id))
                idsRunasColetadasNormalizados.Add(id);
        }
    }

    private void SalvarManifestoDeRunas()
    {
        if (PersistenciaManager.Instance == null)
            return;

        string manifesto = CriarManifestoAtual();

        PersistenciaManager.Instance.SalvarString(ChaveManifestoSlot(), manifesto);
        PersistenciaManager.Instance.SalvarString("Runas_Coletadas", manifesto);
    }

    private string CriarManifestoAtual()
    {
        List<string> lista = new List<string>(idsRunasColetadasNormalizados);
        lista.Sort();

        return string.Join(SEPARADOR_RUNAS, lista);
    }

    private void ReconstruirListaVisualAPartirDosIDs()
    {
        runasNaMao.Clear();

        if (configuracaoRunas == null || configuracaoRunas.Count == 0)
        {
            Debug.LogError("[InventarioRunas] configuracaoRunas está vazia. Sem RunaData, não tem como desenhar ícone.");
            return;
        }

        foreach (SlotConfigRuna slot in configuracaoRunas)
        {
            if (slot == null || slot.data == null || string.IsNullOrEmpty(slot.nomeIdentificador))
                continue;

            string slotNorm = NormalizarIDRuna(slot.nomeIdentificador);

            if (idsRunasColetadasNormalizados.Contains(slotNorm))
            {
                if (!runasNaMao.Contains(slot.data))
                    runasNaMao.Add(slot.data);
            }
        }

        Debug.Log("[InventarioRunas] Reconstruído. IDs: " + CriarManifestoAtual() + " | runasNaMao: " + runasNaMao.Count);
    }

    private void RedesenharAgora()
    {
        if (rotinaRedesenhar != null)
            StopCoroutine(rotinaRedesenhar);

        rotinaRedesenhar = StartCoroutine(RedesenharRunasNaTelaSeguro());
    }

    public void ForcarRedesenhoDasRunas()
    {
        if (rotinaRedesenhar != null)
            StopCoroutine(rotinaRedesenhar);

        rotinaRedesenhar = StartCoroutine(RedesenharRunasNaTelaSeguro());
    }

    public void SolicitarRedesenhoDaUI()
    {
        if (!saveCarregadoPeloMenosUmaVez)
        {
            RecarregarDoSave();
            return;
        }

        ForcarRedesenhoDasRunas();
    }

    private IEnumerator RedesenharRunasNaTelaSeguro()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        AreaDasRunas area = AreaDasRunas.Instance;

        if (area == null)
        {
            AreaDasRunas[] areas = Object.FindObjectsByType<AreaDasRunas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            if (areas != null && areas.Length > 0)
            {
                area = areas[0];

                if (area != null)
                {
                    Canvas canvasPai = area.GetComponentInParent<Canvas>(true);

                    if (canvasPai != null)
                        canvasPai.gameObject.SetActive(true);

                    area.gameObject.SetActive(true);
                    AreaDasRunas.Instance = area;
                }
            }
        }

        float timeout = 8f;

        while (area == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            area = AreaDasRunas.Instance;
            yield return null;
        }

        if (area == null)
        {
            Debug.LogError("[InventarioRunas] Não existe AreaDasRunas na cena. runasNaMao.Count = " + runasNaMao.Count);
            rotinaRedesenhar = null;
            yield break;
        }

        area.LimparTodasAsRunasDaTela();

        if (!cenaPermitidaAtual)
        {
            Debug.LogWarning("[InventarioRunas] Cena não está em cenasPermitidasParaRunas: " + SceneManager.GetActiveScene().name + ". Mesmo assim vou desenhar porque AreaDasRunas existe.");
        }

        int desenhadas = 0;

        foreach (RunaData runa in runasNaMao)
        {
            if (runa == null)
            {
                Debug.LogWarning("[InventarioRunas] Existe uma runa nula em runasNaMao.");
                continue;
            }

            if (runa.icone == null)
            {
                Debug.LogWarning("[InventarioRunas] Runa sem ícone: " + runa.name);
                continue;
            }

            area.AdicionarRunaNaTela(runa.icone);
            desenhadas++;
        }

        Debug.Log("[InventarioRunas] Redesenho finalizado. runasNaMao.Count = " + runasNaMao.Count + " | desenhadas = " + desenhadas);

        rotinaRedesenhar = null;
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
        if (string.IsNullOrEmpty(nome))
            return;

        string nomeNorm = NormalizarIDRuna(nome);

        if (string.IsNullOrEmpty(nomeNorm))
            return;

        if (configuracaoRunas == null)
            configuracaoRunas = new List<SlotConfigRuna>();

        idsRunasColetadasNormalizados.Add(nomeNorm);

        SlotConfigRuna slot = EncontrarSlotPorNome(nome);

        if (slot != null && slot.data != null)
        {
            if (!runasNaMao.Contains(slot.data))
            {
                runasNaMao.Add(slot.data);

                if (slot.eventoParaColetar != null)
                    slot.eventoParaColetar.Invoke();

                VerificarMudancaClimaPorRunasSeguro();
            }
        }
        else
        {
            Debug.LogError("[InventarioRunas] Runa salva, mas não existe SlotConfigRuna compatível com: " + nome + " | normalizado: " + nomeNorm);
        }

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(ChaveRunaSlot(nomeNorm), true);
            PersistenciaManager.Instance.RegistrarEstado("Runa_" + nomeNorm, true);
            PersistenciaManager.Instance.RegistrarEstado("Runa_" + nome, true);

            SalvarManifestoDeRunas();

            if (salvarNoDisco)
                SalvarProgressoSeguro();
        }

        Debug.Log("[InventarioRunas] Runa registrada/coletada: " + nome + " | normalizada: " + nomeNorm + " | runasNaMao.Count = " + runasNaMao.Count);

        RedesenharAgora();
    }

    private SlotConfigRuna EncontrarSlotPorNome(string nome)
    {
        if (configuracaoRunas == null)
            return null;

        string nomeNorm = NormalizarIDRuna(nome);

        foreach (SlotConfigRuna slot in configuracaoRunas)
        {
            if (slot == null || string.IsNullOrEmpty(slot.nomeIdentificador))
                continue;

            if (NormalizarIDRuna(slot.nomeIdentificador) == nomeNorm)
                return slot;
        }

        return null;
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
            DayNightCycle.Instance.ChangeTo(DayNightCycle.TimeState.DramaticDay);

        rotinaVerificarClima = null;
    }

    public bool TemRuna(string nome)
    {
        if (string.IsNullOrEmpty(nome))
            return false;

        string nomeNorm = NormalizarIDRuna(nome);

        if (idsRunasColetadasNormalizados.Contains(nomeNorm))
            return true;

        if (PersistenciaManager.Instance != null)
        {
            if (PersistenciaManager.Instance.ObterEstado(ChaveRunaSlot(nomeNorm), false))
                return true;

            if (PersistenciaManager.Instance.ObterEstado("Runa_" + nomeNorm, false))
                return true;

            if (PersistenciaManager.Instance.ObterEstado("Runa_" + nome, false))
                return true;
        }

        return false;
    }

    public bool TemARunaPeloNome(string nome)
    {
        return TemRuna(nome);
    }

    public void RemoverRuna(string nome)
    {
        if (string.IsNullOrEmpty(nome))
            return;

        string nomeNorm = NormalizarIDRuna(nome);

        idsRunasColetadasNormalizados.Remove(nomeNorm);

        SlotConfigRuna slot = EncontrarSlotPorNome(nome);

        if (slot != null && slot.data != null && runasNaMao.Contains(slot.data))
            runasNaMao.Remove(slot.data);

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(ChaveRunaSlot(nomeNorm), false);
            PersistenciaManager.Instance.RegistrarEstado("Runa_" + nomeNorm, false);
            PersistenciaManager.Instance.RegistrarEstado("Runa_" + nome, false);

            SalvarManifestoDeRunas();
            SalvarProgressoSeguro();
        }

        RedesenharAgora();
    }

    public void UsarTodasAsRunasNoAltar()
    {
        List<string> idsParaRemover = new List<string>(idsRunasColetadasNormalizados);

        foreach (string id in idsParaRemover)
        {
            if (PersistenciaManager.Instance != null)
            {
                PersistenciaManager.Instance.RegistrarEstado(ChaveRunaSlot(id), false);
                PersistenciaManager.Instance.RegistrarEstado("Runa_" + id, false);
            }
        }

        idsRunasColetadasNormalizados.Clear();
        runasNaMao.Clear();

        SalvarManifestoDeRunas();

        if (PersistenciaManager.Instance != null)
            SalvarProgressoSeguro();

        RedesenharAgora();
    }

    private void AtualizarUI(Sprite icone)
    {
        if (icone == null)
        {
            Debug.LogWarning("[InventarioRunas] AtualizarUI recebeu ícone nulo.");
            return;
        }

        if (AreaDasRunas.Instance != null)
        {
            AreaDasRunas.Instance.AdicionarRunaNaTela(icone);
        }
        else
        {
            Debug.LogWarning("[InventarioRunas] AreaDasRunas.Instance nula. Redesenho será tentado depois.");
            RedesenharAgora();
        }
    }

    private string PrefixoSlotAtual()
    {
        if (SistemaGlobal.Instance != null &&
            SistemaGlobal.Instance.slotFoiDefinido &&
            SistemaGlobal.Instance.slotAtual > 0)
        {
            return "Slot_" + SistemaGlobal.Instance.slotAtual + "_";
        }

        return "";
    }

    private string ChaveRunaSlot(string nomeNormalizado)
    {
        return PrefixoSlotAtual() + "Runa_" + NormalizarIDRuna(nomeNormalizado);
    }

    private string ChaveManifestoSlot()
    {
        return PrefixoSlotAtual() + "Runas_Coletadas";
    }

    private string NormalizarIDRuna(string nome)
    {
        if (string.IsNullOrEmpty(nome))
            return "";

        string n = nome.Trim();

        while (n.StartsWith("Runa_"))
            n = n.Substring(5);

        while (n.StartsWith("runa_"))
            n = n.Substring(5);

        return n.Trim().ToLowerInvariant();
    }

    private void SalvarProgressoSeguro()
    {
        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(true);
    }

    [ContextMenu("DEBUG - Mostrar Runas Salvas")]
    private void DebugMostrarRunasSalvas()
    {
        Debug.Log("[InventarioRunas] Slot prefix: " + PrefixoSlotAtual());
        Debug.Log("[InventarioRunas] IDs normalizados coletados: " + CriarManifestoAtual());
        Debug.Log("[InventarioRunas] runasNaMao.Count: " + (runasNaMao != null ? runasNaMao.Count : -1));
        Debug.Log("[InventarioRunas] cenaPermitidaAtual: " + cenaPermitidaAtual + " | cena: " + SceneManager.GetActiveScene().name);
    }
}
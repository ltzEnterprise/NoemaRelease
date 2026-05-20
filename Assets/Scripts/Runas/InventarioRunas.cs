using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text;
using System.Globalization;

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

    [Header("--- HACK / DEBUG ---")]
    [Tooltip("DEV ONLY: se ligado no Play Mode, mostra todas as runas na UI sem salvar no HD.")]
    public bool comecarComTodasAsRunas = false;

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
    private bool estadoAplicado = false;

    private bool hackTodasAsRunasAplicadoNestaSessao = false;

    private HashSet<string> idsRunasColetadasNormalizados = new HashSet<string>();

    private const string SEPARADOR_RUNAS = "|";
    private const string CHAVE_MANIFESTO_RUNAS = "Runas_Coletadas";

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
    }

    private void Start()
    {
        if (Instance != this) return;

        StartCoroutine(AplicarEstadoSeguro());
    }

    private void Update()
    {
        if (Instance != this) return;

        if (comecarComTodasAsRunas && !hackTodasAsRunasAplicadoNestaSessao)
        {
            AplicarHackTodasAsRunasSomenteDev();
        }

        if (!comecarComTodasAsRunas && hackTodasAsRunasAplicadoNestaSessao)
        {
            hackTodasAsRunasAplicadoNestaSessao = false;
            RecarregarDoSave();
        }
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
                if (nova == null)
                    continue;

                string nomeNovo = ObterNomeComparavelDoSlot(nova);

                if (string.IsNullOrEmpty(nomeNovo))
                    continue;

                string novaNorm = NormalizarIDRuna(nomeNovo);

                bool jaExiste = alvo.configuracaoRunas.Exists(x =>
                    x != null &&
                    NormalizarIDRuna(ObterNomeComparavelDoSlot(x)) == novaNorm
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
                if (string.IsNullOrWhiteSpace(cena))
                    continue;

                bool jaExiste = false;

                foreach (string cenaAlvo in alvo.cenasPermitidasParaRunas)
                {
                    if (CompararNomeCena(cenaAlvo, cena))
                    {
                        jaExiste = true;
                        break;
                    }
                }

                if (!jaExiste)
                {
                    alvo.cenasPermitidasParaRunas.Add(cena.Trim());
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

        if (!CenaPermitida(scene.name))
        {
            EsconderAreaSeExistir();
            return;
        }

        RecarregarDoSave();
    }

    private IEnumerator AplicarEstadoSeguro()
    {
        if (estadoAplicado) yield break;

        yield return new WaitUntil(() => PersistenciaManager.Instance != null);
        yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);
        yield return new WaitUntil(() => !PersistenciaManager.Instance.EstaCarregando);
        yield return null;

        CarregarRunasDoSave();

        if (comecarComTodasAsRunas)
            AplicarHackTodasAsRunasSomenteDev();

        estadoAplicado = true;
    }

    public void RecarregarDoSave()
    {
        if (Instance != this) return;

        if (rotinaCarregar != null)
            StopCoroutine(rotinaCarregar);

        rotinaCarregar = StartCoroutine(RecarregarDoSaveSeguro());
    }

    private IEnumerator RecarregarDoSaveSeguro()
    {
        yield return new WaitUntil(() => PersistenciaManager.Instance != null);
        yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);
        yield return new WaitUntil(() => !PersistenciaManager.Instance.EstaCarregando);
        yield return null;

        CarregarRunasDoSave();

        if (comecarComTodasAsRunas)
            AplicarHackTodasAsRunasSomenteDev();

        rotinaCarregar = null;
    }

    private void AplicarHackTodasAsRunasSomenteDev()
    {
        if (configuracaoRunas == null || configuracaoRunas.Count == 0)
        {
            Debug.LogWarning("[InventarioRunas] DEV HACK ligado, mas configuracaoRunas está vazia.");
            RedesenharAgora();
            return;
        }

        idsRunasColetadasNormalizados.Clear();
        runasNaMao.Clear();

        int adicionadas = 0;

        foreach (SlotConfigRuna slot in configuracaoRunas)
        {
            if (slot == null)
                continue;

            string nomeBase = ObterNomeComparavelDoSlot(slot);

            if (string.IsNullOrEmpty(nomeBase))
                continue;

            string nomeNorm = NormalizarIDRuna(nomeBase);

            if (string.IsNullOrEmpty(nomeNorm))
                continue;

            idsRunasColetadasNormalizados.Add(nomeNorm);

            if (slot.data != null && !runasNaMao.Contains(slot.data))
            {
                runasNaMao.Add(slot.data);
                adicionadas++;
            }
        }

        hackTodasAsRunasAplicadoNestaSessao = true;

        VerificarMudancaClimaPorRunasSeguro();
        RedesenharAgora();

        Debug.Log("[InventarioRunas] DEV HACK aplicado SEM salvar no HD. Runas na RAM/UI: " + adicionadas);
    }

    private void CarregarRunasDoSave()
    {
        if (PersistenciaManager.Instance == null)
        {
            RedesenharAgora();
            return;
        }

        idsRunasColetadasNormalizados.Clear();
        runasNaMao.Clear();

        string manifesto = PersistenciaManager.Instance.ObterString(CHAVE_MANIFESTO_RUNAS, "");

        LerManifesto(manifesto);
        CarregarEstadosIndividuaisDasRunas();
        ReconstruirListaVisualAPartirDosIDs();

        Debug.Log("[InventarioRunas] Runas carregadas do PersistenciaManager. Manifesto=" +
                  CriarManifestoAtual() +
                  " | runasNaMao=" + runasNaMao.Count);

        RedesenharAgora();
    }

    private void CarregarEstadosIndividuaisDasRunas()
    {
        if (PersistenciaManager.Instance == null) return;
        if (configuracaoRunas == null) return;

        foreach (SlotConfigRuna slot in configuracaoRunas)
        {
            if (slot == null) continue;

            string nomeBase = ObterNomeComparavelDoSlot(slot);

            if (string.IsNullOrEmpty(nomeBase))
                continue;

            string nomeNorm = NormalizarIDRuna(nomeBase);

            if (PersistenciaManager.Instance.ObterEstado("Runa_" + nomeNorm, false))
                idsRunasColetadasNormalizados.Add(nomeNorm);
        }
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
            if (slot == null)
                continue;

            if (slot.data == null)
            {
                Debug.LogWarning("[InventarioRunas] SlotConfigRuna sem RunaData. Identificador: " + slot.nomeIdentificador);
                continue;
            }

            string nomeBase = ObterNomeComparavelDoSlot(slot);
            string slotNorm = NormalizarIDRuna(nomeBase);

            if (idsRunasColetadasNormalizados.Contains(slotNorm))
            {
                if (!runasNaMao.Contains(slot.data))
                    runasNaMao.Add(slot.data);
            }
        }
    }

    private string ObterNomeComparavelDoSlot(SlotConfigRuna slot)
    {
        if (slot == null)
            return "";

        if (!string.IsNullOrEmpty(slot.nomeIdentificador))
            return slot.nomeIdentificador;

        if (slot.data != null)
            return slot.data.name;

        return "";
    }

    private bool CenaPermitida(string nomeCena)
    {
        if (cenasPermitidasParaRunas == null || cenasPermitidasParaRunas.Count == 0)
            return true;

        foreach (string cenaPermitida in cenasPermitidasParaRunas)
        {
            if (CompararNomeCena(cenaPermitida, nomeCena))
                return true;
        }

        return false;
    }

    private static bool CompararNomeCena(string a, string b)
    {
        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return false;

        return string.Equals(a.Trim(), b.Trim(), System.StringComparison.OrdinalIgnoreCase);
    }

    public bool CenaAtualPermiteMostrarRunas()
    {
        return CenaPermitida(SceneManager.GetActiveScene().name);
    }

    private void RedesenharAgora()
    {
        if (rotinaRedesenhar != null)
            StopCoroutine(rotinaRedesenhar);

        rotinaRedesenhar = StartCoroutine(RedesenharRunasNaTelaSeguro());
    }

    public void ForcarRedesenhoDasRunas()
    {
        RedesenharAgora();
    }

    public void SolicitarRedesenhoDaUI()
    {
        RedesenharAgora();
    }

    private IEnumerator RedesenharRunasNaTelaSeguro()
    {
        if (!CenaAtualPermiteMostrarRunas())
        {
            EsconderAreaSeExistir();
            rotinaRedesenhar = null;
            yield break;
        }

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

        if (area == null)
        {
            Debug.LogError("[InventarioRunas] Não existe AreaDasRunas na cena permitida. runasNaMao.Count = " + runasNaMao.Count);
            rotinaRedesenhar = null;
            yield break;
        }

        MostrarArea(area);

        area.LimparTodasAsRunasDaTela();

        int desenhadas = 0;

        foreach (RunaData runa in runasNaMao)
        {
            if (runa == null)
                continue;

            if (runa.icone == null)
            {
                Debug.LogWarning("[InventarioRunas] Runa sem ícone: " + runa.name);
                continue;
            }

            area.AdicionarRunaNaTela(runa.icone);
            desenhadas++;
        }

        Debug.Log("[InventarioRunas] Redesenho finalizado. runasNaMao.Count=" +
                  runasNaMao.Count +
                  " | desenhadas=" + desenhadas +
                  " | Manifesto=" + CriarManifestoAtual());

        rotinaRedesenhar = null;
    }

    private void MostrarArea(AreaDasRunas area)
    {
        if (area == null) return;

        Canvas canvasPai = area.GetComponentInParent<Canvas>(true);

        if (canvasPai != null)
            canvasPai.gameObject.SetActive(true);

        area.gameObject.SetActive(true);

        CanvasGroup cg = area.GetComponent<CanvasGroup>();

        if (cg == null)
            cg = area.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 1f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
    }

    private void EsconderAreaSeExistir()
    {
        AreaDasRunas area = AreaDasRunas.Instance;

        if (area == null)
            return;

        CanvasGroup cg = area.GetComponent<CanvasGroup>();

        if (cg == null)
            cg = area.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;

        area.LimparTodasAsRunasDaTela();
    }

    public void SalvarPosicaoAtual(Vector3 posicao)
    {
        ultimaPosicaoSalva = posicao;
        deveCarregarPosicao = true;
    }

    public void ColetarRunaPeloNome(string nome)
    {
        ColetarRunaInterno(nome);
    }

    private void ColetarRunaInterno(string nome)
    {
        if (string.IsNullOrEmpty(nome))
            return;

        string nomeNorm = NormalizarIDRuna(nome);

        if (string.IsNullOrEmpty(nomeNorm))
            return;

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
            Debug.LogError("[InventarioRunas] Runa coletada, mas não existe SlotConfigRuna compatível com: " +
                           nome + " | normalizado: " + nomeNorm + ". Sem RunaData, não tem como desenhar ícone.");
        }

        SalvarRunasNoPersistencia();

        RedesenharAgora();

        Debug.Log("[InventarioRunas] Runa coletada e salva. Nome=" +
                  nome +
                  " | Normalizada=" + nomeNorm +
                  " | Manifesto=" + CriarManifestoAtual());
    }

    private void SalvarRunasNoPersistencia()
    {
        if (PersistenciaManager.Instance == null)
        {
            Debug.LogError("[InventarioRunas] PersistenciaManager.Instance está nulo. Não salvou runas.");
            return;
        }

        foreach (string id in idsRunasColetadasNormalizados)
        {
            if (!string.IsNullOrEmpty(id))
                PersistenciaManager.Instance.RegistrarEstado("Runa_" + id, true);
        }

        PersistenciaManager.Instance.SalvarString(CHAVE_MANIFESTO_RUNAS, CriarManifestoAtual());
        PersistenciaManager.Instance.SalvarTudo(true);

        Debug.Log("[InventarioRunas] Salvou no PersistenciaManager. Manifesto=" + CriarManifestoAtual());
    }

    private SlotConfigRuna EncontrarSlotPorNome(string nome)
    {
        if (configuracaoRunas == null)
            return null;

        string nomeNorm = NormalizarIDRuna(nome);

        foreach (SlotConfigRuna slot in configuracaoRunas)
        {
            if (slot == null)
                continue;

            string nomeBase = ObterNomeComparavelDoSlot(slot);

            if (string.IsNullOrEmpty(nomeBase))
                continue;

            if (NormalizarIDRuna(nomeBase) == nomeNorm)
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
            if (PersistenciaManager.Instance.ObterEstado("Runa_" + nomeNorm, false))
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
            PersistenciaManager.Instance.RegistrarEstado("Runa_" + nomeNorm, false);
            PersistenciaManager.Instance.SalvarString(CHAVE_MANIFESTO_RUNAS, CriarManifestoAtual());
            PersistenciaManager.Instance.SalvarTudo(true);
        }

        RedesenharAgora();
    }

    public void UsarTodasAsRunasNoAltar()
    {
        List<string> idsParaRemover = new List<string>(idsRunasColetadasNormalizados);

        foreach (string id in idsParaRemover)
        {
            if (PersistenciaManager.Instance != null)
                PersistenciaManager.Instance.RegistrarEstado("Runa_" + id, false);
        }

        idsRunasColetadasNormalizados.Clear();
        runasNaMao.Clear();

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarString(CHAVE_MANIFESTO_RUNAS, "");
            PersistenciaManager.Instance.SalvarTudo(true);
        }

        RedesenharAgora();
    }

    private string NormalizarIDRuna(string nome)
    {
        if (string.IsNullOrEmpty(nome))
            return "";

        string n = nome.Trim().ToLowerInvariant();
        n = RemoverAcentos(n);

        n = n.Replace("runa_", " ");
        n = n.Replace("runa-", " ");
        n = n.Replace("runa ", " ");
        n = n.Replace("runa", " ");

        n = n.Replace(" da ", " ");
        n = n.Replace(" de ", " ");
        n = n.Replace(" do ", " ");
        n = n.Replace(" das ", " ");
        n = n.Replace(" dos ", " ");

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < n.Length; i++)
        {
            char c = n[i];

            if (char.IsLetterOrDigit(c))
                sb.Append(c);
        }

        return sb.ToString();
    }

    private string RemoverAcentos(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return "";

        string normalizado = texto.Normalize(NormalizationForm.FormD);
        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < normalizado.Length; i++)
        {
            UnicodeCategory categoria = CharUnicodeInfo.GetUnicodeCategory(normalizado[i]);

            if (categoria != UnicodeCategory.NonSpacingMark)
                sb.Append(normalizado[i]);
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    [ContextMenu("DEBUG - Mostrar Runas Salvas")]
    private void DebugMostrarRunasSalvas()
    {
        Debug.Log("[InventarioRunas] IDs coletados: " + CriarManifestoAtual());
        Debug.Log("[InventarioRunas] runasNaMao.Count: " + (runasNaMao != null ? runasNaMao.Count : -1));
        Debug.Log("[InventarioRunas] cena atual: " + SceneManager.GetActiveScene().name);
        Debug.Log("[InventarioRunas] CenaAtualPermiteMostrarRunas: " + CenaAtualPermiteMostrarRunas());

        if (PersistenciaManager.Instance != null)
            Debug.Log("[InventarioRunas] Manifesto no PersistenciaManager: " +
                      PersistenciaManager.Instance.ObterString(CHAVE_MANIFESTO_RUNAS, "VAZIO"));

        if (configuracaoRunas != null)
        {
            for (int i = 0; i < configuracaoRunas.Count; i++)
            {
                SlotConfigRuna slot = configuracaoRunas[i];

                if (slot == null)
                {
                    Debug.Log("[InventarioRunas] Config " + i + ": NULL");
                    continue;
                }

                Debug.Log("[InventarioRunas] Config " + i +
                          " nome=[" + slot.nomeIdentificador + "]" +
                          " norm=[" + NormalizarIDRuna(ObterNomeComparavelDoSlot(slot)) + "]" +
                          " data=[" + (slot.data != null ? slot.data.name : "NULL") + "]" +
                          " icone=[" + (slot.data != null && slot.data.icone != null ? slot.data.icone.name : "NULL") + "]");
            }
        }
    }
}
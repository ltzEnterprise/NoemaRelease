using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class AreaDasRunas : MonoBehaviour
{
    public static AreaDasRunas Instance;

    [Header("--- CONFIG VISUAL ---")]
    public Vector2 tamanhoIcone = new Vector2(100, 100);

    [Tooltip("Se tiver Layout Group no objeto pai, deixe ligado. Se não tiver, os ícones podem ficar sobrepostos.")]
    public bool usarLayoutDoPai = true;

    [Header("--- COMPORTAMENTO ---")]
    [Tooltip("Se ligado, a área só aparece nas cenas permitidas pelo InventarioRunas.")]
    public bool respeitarListaDeCenas = true;

    [Header("--- DEBUG ---")]
    public bool mostrarLogsDebug = true;

    private Coroutine rotinaInicializar;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        Instance = this;
        PrepararCanvasGroup();
    }

    private void OnEnable()
    {
        Instance = this;
        PrepararCanvasGroup();

        if (rotinaInicializar != null)
            StopCoroutine(rotinaInicializar);

        rotinaInicializar = StartCoroutine(InicializarSeguro());
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private IEnumerator InicializarSeguro()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        float timeout = 8f;

        while (InventarioRunas.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (InventarioRunas.Instance == null)
        {
            LimparTodasAsRunasDaTela();
            MostrarArea();

            Debug.LogError("[AreaDasRunas] InventarioRunas.Instance não apareceu. Não foi possível desenhar runas.");

            rotinaInicializar = null;
            yield break;
        }

        if (!CenaAtualPodeMostrarRunas())
        {
            LimparTodasAsRunasDaTela();
            EsconderAreaSemDesativar();

            if (mostrarLogsDebug)
                Debug.LogWarning("[AreaDasRunas] Cena bloqueada para runas: " + SceneManager.GetActiveScene().name + " | Permitidas: " + ListarCenasPermitidas());

            rotinaInicializar = null;
            yield break;
        }

        MostrarArea();

        if (mostrarLogsDebug)
            Debug.Log("[AreaDasRunas] Cena permitida. Pedindo redesenho das runas em: " + SceneManager.GetActiveScene().name);

        InventarioRunas.Instance.ForcarRedesenhoDasRunas();

        rotinaInicializar = null;
    }

    public void AdicionarRunaNaTela(Sprite iconeRuna)
    {
        if (!CenaAtualPodeMostrarRunas())
        {
            LimparTodasAsRunasDaTela();
            EsconderAreaSemDesativar();

            if (mostrarLogsDebug)
                Debug.LogWarning("[AreaDasRunas] Bloqueou desenho de runa fora da lista. Cena atual: " + SceneManager.GetActiveScene().name + " | Permitidas: " + ListarCenasPermitidas());

            return;
        }

        if (iconeRuna == null)
        {
            Debug.LogWarning("[AreaDasRunas] Tentou desenhar uma runa com ícone nulo.");
            return;
        }

        MostrarArea();
        DesenharRunaExistente(iconeRuna);
    }

    private void DesenharRunaExistente(Sprite iconeRuna)
    {
        GameObject novoIcone = new GameObject("IconeRuna");
        novoIcone.transform.SetParent(transform, false);

        int uiLayer = LayerMask.NameToLayer("UI");

        if (uiLayer >= 0)
            novoIcone.layer = uiLayer;

        Image img = novoIcone.AddComponent<Image>();
        img.sprite = iconeRuna;
        img.color = Color.white;
        img.raycastTarget = false;

        RectTransform rect = novoIcone.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition3D = Vector3.zero;
            rect.sizeDelta = tamanhoIcone;
        }

        if (usarLayoutDoPai)
        {
            LayoutElement layout = novoIcone.AddComponent<LayoutElement>();
            layout.preferredWidth = tamanhoIcone.x;
            layout.preferredHeight = tamanhoIcone.y;
            layout.minWidth = tamanhoIcone.x;
            layout.minHeight = tamanhoIcone.y;
        }

        if (mostrarLogsDebug)
            Debug.Log("[AreaDasRunas] Ícone de runa desenhado: " + iconeRuna.name);
    }

    public void LimparTodasAsRunasDaTela()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);
    }

    private bool CenaAtualPodeMostrarRunas()
    {
        if (!respeitarListaDeCenas)
            return true;

        if (InventarioRunas.Instance == null)
            return true;

        if (InventarioRunas.Instance.cenasPermitidasParaRunas == null ||
            InventarioRunas.Instance.cenasPermitidasParaRunas.Count == 0)
        {
            return true;
        }

        string cenaAtual = NormalizarNomeCena(SceneManager.GetActiveScene().name);

        foreach (string cenaPermitida in InventarioRunas.Instance.cenasPermitidasParaRunas)
        {
            if (NormalizarNomeCena(cenaPermitida) == cenaAtual)
                return true;
        }

        return false;
    }

    private string NormalizarNomeCena(string nome)
    {
        if (string.IsNullOrEmpty(nome))
            return "";

        return nome.Trim().ToLowerInvariant();
    }

    private string ListarCenasPermitidas()
    {
        if (InventarioRunas.Instance == null ||
            InventarioRunas.Instance.cenasPermitidasParaRunas == null ||
            InventarioRunas.Instance.cenasPermitidasParaRunas.Count == 0)
        {
            return "LISTA VAZIA";
        }

        string resultado = "";

        for (int i = 0; i < InventarioRunas.Instance.cenasPermitidasParaRunas.Count; i++)
        {
            resultado += "[" + InventarioRunas.Instance.cenasPermitidasParaRunas[i] + "]";

            if (i < InventarioRunas.Instance.cenasPermitidasParaRunas.Count - 1)
                resultado += ", ";
        }

        return resultado;
    }

    private void PrepararCanvasGroup()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void MostrarArea()
    {
        PrepararCanvasGroup();

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void EsconderAreaSemDesativar()
    {
        PrepararCanvasGroup();

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
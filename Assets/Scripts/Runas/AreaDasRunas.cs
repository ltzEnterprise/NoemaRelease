using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AreaDasRunas : MonoBehaviour
{
    public static AreaDasRunas Instance;

    [Header("--- CONFIG VISUAL ---")]
    public Vector2 tamanhoIcone = new Vector2(100, 100);

    [Tooltip("Se tiver Layout Group no objeto pai, deixe ligado. Se não tiver, os ícones podem ficar sobrepostos.")]
    public bool usarLayoutDoPai = true;

    private Coroutine rotinaInicializar;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        Instance = this;

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

        if (InventarioRunas.Instance != null)
        {
            InventarioRunas.Instance.ForcarRedesenhoDasRunas();
        }
        else
        {
            LimparTodasAsRunasDaTela();
            Debug.LogError("[AreaDasRunas] InventarioRunas.Instance não apareceu. Não foi possível desenhar runas.");
        }

        rotinaInicializar = null;
    }

    public void AdicionarRunaNaTela(Sprite iconeRuna)
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        Canvas canvasPai = GetComponentInParent<Canvas>(true);

        if (canvasPai != null && !canvasPai.gameObject.activeSelf)
            canvasPai.gameObject.SetActive(true);

        DesenharRunaExistente(iconeRuna);
    }

    private void DesenharRunaExistente(Sprite iconeRuna)
    {
        if (iconeRuna == null)
        {
            Debug.LogWarning("[AreaDasRunas] Tentou desenhar uma runa com ícone nulo.");
            return;
        }

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

        Debug.Log("[AreaDasRunas] Ícone de runa desenhado: " + iconeRuna.name);
    }

    public void LimparTodasAsRunasDaTela()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
}
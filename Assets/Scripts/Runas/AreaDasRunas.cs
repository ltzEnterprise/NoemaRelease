using UnityEngine;
using UnityEngine.UI;

public class AreaDasRunas : MonoBehaviour
{
    public static AreaDasRunas Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (InventarioRunas.Instance != null)
        {
            LimparTodasAsRunasDaTela();
            foreach (var runa in InventarioRunas.Instance.runasNaMao)
            {
                DesenharRunaExistente(runa.icone);
            }
        }
    }

    public void AdicionarRunaNaTela(Sprite iconeRuna)
    {
        // --- CORREÇÃO 1: LIGA O OBJETO NA MARRA ---
        // Se por algum motivo o objeto estiver desativado, isso liga ele de volta
        if (!this.gameObject.activeSelf) 
        {
            this.gameObject.SetActive(true);
            Debug.Log("AreaDasRunas estava desativada! Forcei a ativação.");
        }

        DesenharRunaExistente(iconeRuna);
    }

    private void DesenharRunaExistente(Sprite iconeRuna)
    {
        if (iconeRuna == null) return;

        GameObject novoIcone = new GameObject("IconeRuna");
        novoIcone.transform.SetParent(this.transform, false);

        // Força Layer UI
        novoIcone.layer = LayerMask.NameToLayer("UI"); 

        Image img = novoIcone.AddComponent<Image>();
        img.sprite = iconeRuna;
        img.color = Color.white; 

        // Força Tamanho e Escala
        RectTransform rect = novoIcone.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one; 
            rect.localPosition = Vector3.zero;
            rect.sizeDelta = new Vector2(100, 100); 
        }
    }

    public void LimparTodasAsRunasDaTela()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }
}
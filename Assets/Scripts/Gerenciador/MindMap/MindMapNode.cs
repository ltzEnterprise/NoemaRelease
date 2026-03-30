using UnityEngine;

public class MindMapNode : MonoBehaviour
{
    [Header("Identificação para o Save")]
    [Tooltip("Ex: node_cabana, node_codigoCofre")]
    public string nodeID;

    [Header("O que deve aparecer?")]
    [Tooltip("O objeto pai que contém as setas, textos e imagens desta pista")]
    public GameObject visualGroup;

    void Start()
    {
        // Ao iniciar, ele checa o HD. Se tiver 1, ele aparece. Se for 0, fica invisível.
        if (PlayerPrefs.GetInt("MindMap_" + nodeID, 0) == 1)
        {
            visualGroup.SetActive(true);
        }
        else
        {
            visualGroup.SetActive(false);
        }
    }

    public void UnlockNode()
    {
        // Salva que essa pista foi descoberta e liga o visual
        PlayerPrefs.SetInt("MindMap_" + nodeID, 1);
        PlayerPrefs.Save();
        visualGroup.SetActive(true);
    }
}
using UnityEngine;
using System.Collections.Generic;

public class AmbientSkyObject : MonoBehaviour
{
    public static List<AmbientSkyObject> todosObjetos = new List<AmbientSkyObject>();

    public enum TipoFoto { Decoracao, ChaveFinal, LinkPilar }

    [Header("--- TIPO DE FOTO ---")]
    [Tooltip("Escolha 'ChaveFinal' para a chave gigante")]
    public TipoFoto tipoDeInteracao = TipoFoto.Decoracao;

    [Header("Apenas se for Pilar")]
    public int idLinkPilar = 0;

    [Header("--- ITEM OPCIONAL AO FOTOGRAFAR ---")]
    [Tooltip("Se ligado, quando este objeto sumir após a foto, entrega um item ao inventário.")]
    public bool darItemAoFotografar = false;

    [Tooltip("ID do item no InventoryManager.itensRegistrados.")]
    public int itemID = 0;

    [Tooltip("Apenas para organização no Inspector. O item real é definido pelo Item ID.")]
    public string nomeDoItem = "Item";

    [Header("Efeitos")]
    public GameObject efeitoAoSumir;
    public bool sumirAposFoto = true;

    private bool itemJaEntregue = false;

    void OnEnable()
    {
        if (!todosObjetos.Contains(this))
            todosObjetos.Add(this);
    }

    void OnDisable()
    {
        if (todosObjetos.Contains(this))
            todosObjetos.Remove(this);
    }

    public void Sumir()
    {
        DarItemOpcional();

        if (efeitoAoSumir != null)
            Instantiate(efeitoAoSumir, transform.position, Quaternion.identity);

        if (sumirAposFoto)
            gameObject.SetActive(false);
    }

    private void DarItemOpcional()
    {
        if (!darItemAoFotografar)
            return;

        if (itemJaEntregue)
            return;

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[AmbientSkyObject] darItemAoFotografar está ligado, mas InventoryManager.Instance está nulo. Objeto: " + gameObject.name);
            return;
        }

        InventoryManager.Instance.ReceberItem(itemID);
        itemJaEntregue = true;

        Debug.Log("[AmbientSkyObject] Item entregue ao fotografar objeto. ID=" + itemID + " | Nome=" + nomeDoItem);
    }
}
using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class ItemPickup : MonoBehaviour
{
    [Header("--- SAVE SYSTEM (NÃO DEIXE VAZIO) ---")]
    [Tooltip("Dê um nome único pra essa arma não dar respawn. Ex: Glock_Mesa_Sala")]
    public string uniqueID; 

    [Header("Configuração do Item")]
    public int itemID = 0; 
    public string nomeDoItem = "Item";

    [Header("Interação")]
    public GameObject objetoDeTexto;

    [Header("Eventos Especiais")]
    [Tooltip("Arraste aqui o que acontece ao pegar (Ex: Salvar jogo, Tocar som)")]
    public UnityEvent onPickup; 

    [Header("Configuração de Save (Opcional)")]
    public bool salvarAoPegar = false;

    private bool jaPegou = false;
    private bool inicializado = false;

    void Awake()
    {
        if (GetComponent<SaveableItem>() != null)
        {
            Debug.LogError($"<color=red>[ERRO FATAL]</color> O objeto '{gameObject.name}' tem 'ItemPickup' E 'SaveableItem'. Remova o SaveableItem deste objeto.");
        }

        if (string.IsNullOrEmpty(uniqueID))
        {
            Debug.LogError($"[ERRO DE LÓGICA] A arma/item '{gameObject.name}' está sem UniqueID. Ela não vai salvar corretamente.");
        }
    }

    void Start() 
    { 
        if (objetoDeTexto) objetoDeTexto.SetActive(false); 
        StartCoroutine(InicializarSeguro());
    }

    IEnumerator InicializarSeguro()
    {
        if (PersistenciaManager.Instance != null)
            yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            if (PersistenciaManager.Instance.ObterEstado(uniqueID, false))
            {
                Destroy(gameObject);
                yield break;
            }
        }

        inicializado = true;
    }

    public void AoOlhar()
    {
        if (!inicializado) return;
        if (jaPegou) return;

        if (objetoDeTexto)
            objetoDeTexto.SetActive(true);
    }

    public void AoSair()
    {
        if (objetoDeTexto)
            objetoDeTexto.SetActive(false);
    }

    public void Interagir()
    {
        if (!inicializado) return;
        if (jaPegou) return;

        PegarItem();
    }

    void PegarItem()
    {
        jaPegou = true; 
        
        if (objetoDeTexto)
            objetoDeTexto.SetActive(false);

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.enabled = false;

        if (!string.IsNullOrEmpty(uniqueID) && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ReceberItem(itemID);
        }
        else
        {
            Debug.LogError("[ItemPickup] InventoryManager não encontrado na cena!");
        }

        if (onPickup != null)
            onPickup.Invoke();

        if (salvarAoPegar)
            SalvarProgressoSeguro();

        Destroy(gameObject, 0.1f);
    }

    private void SalvarProgressoSeguro()
    {
        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(false);
    }
}
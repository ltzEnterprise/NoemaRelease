using UnityEngine;
using UnityEngine.Events;

public class ItemPickup : MonoBehaviour
{
    [Header("Configuração do Item")]
    public int itemID = 0; 
    public string nomeDoItem = "Item"; // Para UI futura

    [Header("Interação")]
    public GameObject objetoDeTexto; // Texto "Pressione E para pegar"

    [Header("Eventos Especiais")]
    [Tooltip("Arraste aqui o que acontece ao pegar (Ex: Salvar jogo, Tocar som)")]
    public UnityEvent onPickup; 

    [Header("Configuração de Save (Opcional)")]
    public bool salvarAoPegar = false;

    void Start() 
    { 
        if(objetoDeTexto) objetoDeTexto.SetActive(false); 
    }

    // --- SISTEMA RAYCAST ---
    public void AoOlhar()
    {
        if (objetoDeTexto) objetoDeTexto.SetActive(true);
    }

    public void AoSair()
    {
        if (objetoDeTexto) objetoDeTexto.SetActive(false);
    }

    public void Interagir()
    {
        PegarItem();
    }
    // -----------------------

    void PegarItem()
    {
        if (objetoDeTexto) objetoDeTexto.SetActive(false);

        // 1. Adiciona ao Inventário
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ReceberItem(itemID);
        }
        else
        {
            Debug.LogError("[ItemPickup] InventoryManager não encontrado!");
        }

        // 2. Executa eventos extras (Ex: Lógica do Disco, Tocar Som)
        onPickup.Invoke();

        // 3. Salvar (Se marcado)
        if (salvarAoPegar && SistemaGlobal.Instance != null)
        {
            // Salva no slot atual usando a posição do player (Singleton)
            if (FPS_Master.Instance != null)
                SistemaGlobal.Instance.SalvarJogo(FPS_Master.Instance.transform.position, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        // 4. Destruir
        Destroy(gameObject);
    }
}
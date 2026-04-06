using UnityEngine;
using UnityEngine.Events;

public class ItemPickup : MonoBehaviour
{
    [Header("--- SAVE SYSTEM (NÃO DEIXE VAZIO) ---")]
    [Tooltip("Dê um nome único pra essa arma não dar respawn. Ex: Glock_Mesa_Sala")]
    public string uniqueID; 

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

    private bool jaPegou = false; // Trava contra duplo clique e bug de Raycast

    void Start() 
    { 
        // Se o save diz que você já pegou essa arma, ela se destrói antes de você ver.
        if (!string.IsNullOrEmpty(uniqueID) && PlayerPrefs.GetInt(uniqueID + "_Pego", 0) == 1)
        {
            Destroy(gameObject);
            return;
        }

        if(objetoDeTexto) objetoDeTexto.SetActive(false); 
    }

    // --- MÉTODOS DO SEU SISTEMA DE RAYCAST CENTRAL ---
    
    public void AoOlhar()
    {
        if (jaPegou) return;
        if (objetoDeTexto) objetoDeTexto.SetActive(true);
    }

    public void AoSair()
    {
        if (objetoDeTexto) objetoDeTexto.SetActive(false);
    }

    public void Interagir()
    {
        if (jaPegou) return;
        PegarItem();
    }
    
    // -------------------------------------------------

    void PegarItem()
    {
        jaPegou = true; // Trava imediata pra não receber mais comandos do Raycast
        
        if (objetoDeTexto) objetoDeTexto.SetActive(false);

        // Desliga o colisor AGORA. Assim o seu Raycast Central entende que não tem mais nada ali
        // e não dá erro quando a Unity destruir o objeto no fim do frame.
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // 1. Grava no cérebro do jogo que esse item sumiu pra sempre
        if (!string.IsNullOrEmpty(uniqueID))
        {
            PlayerPrefs.SetInt(uniqueID + "_Pego", 1);
            PlayerPrefs.Save();
        }

        // 2. Adiciona ao Inventário
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ReceberItem(itemID);
        }
        else
        {
            Debug.LogError("[ItemPickup] InventoryManager não encontrado!");
        }

        // 3. Executa eventos extras (Tocar Som, rodar cutscene, etc)
        if (onPickup != null) onPickup.Invoke();

        // 4. Salvar (Se marcado)
        if (salvarAoPegar && SistemaGlobal.Instance != null && FPS_Master.Instance != null)
        {
            SistemaGlobal.Instance.SalvarJogo(FPS_Master.Instance.transform.position, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        // 5. Some com a arma
        Destroy(gameObject);
    }
}
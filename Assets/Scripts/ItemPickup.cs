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

    void Awake()
    {
        // 🔥 VERIFICAÇÃO DE EXCELÊNCIA 1: Impede o desenvolvedor de causar um paradoxo no JSON
        if (GetComponent<SaveableItem>() != null)
        {
            Debug.LogError($"<color=red>[ERRO FATAL]</color> O objeto '{gameObject.name}' tem 'ItemPickup' E 'SaveableItem'. Eles usam lógicas opostas de Save! Remova o 'SaveableItem' deste objeto imediatamente.");
        }

        // 🔥 VERIFICAÇÃO DE EXCELÊNCIA 2: Impede o respawn infinito por falta de ID
        if (string.IsNullOrEmpty(uniqueID))
        {
            Debug.LogError($"[ERRO DE LÓGICA] A arma/item '{gameObject.name}' está sem UniqueID. Ela não vai salvar e dará respawn toda vez que a cena carregar.");
        }
    }

    void Start() 
    { 
        // 🔥 VERIFICAÇÃO DE EXCELÊNCIA 3: Removido o bloqueio do Editor. O save TEM que funcionar na engine.
        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            // Neste script, o estado TRUE significa "Item já foi pego". O valor padrão de um jogo novo é FALSE.
            if (PersistenciaManager.Instance.ObterEstado(uniqueID, false))
            {
                Destroy(gameObject);
                return;
            }
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
        jaPegou = true; 
        
        if (objetoDeTexto) objetoDeTexto.SetActive(false);

        // 🔥 VERIFICAÇÃO DE EXCELÊNCIA 4: Desliga colisores e malhas ANTES de destruir pra não cortar eventos e áudios.
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach(var r in renderers) r.enabled = false;

        // 1. Grava no cérebro do jogo que esse item sumiu (Registra TRUE para "Morto")
        if (!string.IsNullOrEmpty(uniqueID) && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);
        }

        // 2. Adiciona ao Inventário
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ReceberItem(itemID);
        }
        else
        {
            Debug.LogError("[ItemPickup] InventoryManager não encontrado na cena!");
        }

        // 3. Executa eventos extras com segurança (Agora eles não serão decepados pela Unity)
        if (onPickup != null) onPickup.Invoke();

        // 4. Salvar Fisicamente no HD
        if (salvarAoPegar && SistemaGlobal.Instance != null && FPS_Master.Instance != null)
        {
            SistemaGlobal.Instance.SalvarJogo(FPS_Master.Instance.transform.position, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
        else if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarTudo();
        }

        // 5. Some com a arma da memória com 0.1s de delay para garantir que a fila de eventos (como partículas) foi processada
        Destroy(gameObject, 0.1f);
    }
}
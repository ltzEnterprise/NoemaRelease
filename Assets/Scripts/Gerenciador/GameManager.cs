using UnityEngine;
using UnityEngine.SceneManagement; // Adicionado para ler a cena direito

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Configurações de Save")]
    public string nomeCenaPadrao = "DreamSceane";
    public bool autoSaveAoPegarRuna = true;

    [Header("Referências Globais (Opcional)")]
    public GameObject player;
    public Camera cameraPrincipal;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        if (cameraPrincipal == null) cameraPrincipal = Camera.main;
    }

    public void SalvarProgresso()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        
        // 1. Salva a posição e a cena no sistema base
        if (player != null && SistemaGlobal.Instance != null)
        {
            SistemaGlobal.Instance.SalvarJogo(player.transform.position, SceneManager.GetActiveScene().name);
        }

        // 2. 🔥 A MÁGICA NOVA 🔥: Manda o PersistenciaManager gravar todas as portas, chaves e runas no HD!
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarTudo();
        }

        // 3. Salva os dados legados do EstadoGlobal no slot atual, pra não perder as armas/casas
        if (SistemaGlobal.Instance != null)
        {
            EstadoGlobal.SalvarNoSlot(SistemaGlobal.Instance.slotAtual);
        }

        Debug.Log("<color=green>[GameManager] Progresso salvo 100% no disco (Posição, Cena, Inventário e Mapa)!</color>");
    }
}
using UnityEngine;
using UnityEngine.SceneManagement; 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Configurações de Save")]
    public string nomeCenaPadrao = "DreamSceane";
    public bool autoSaveAoPegarRuna = true;

    [Header("Referências Globais")]
    public GameObject player;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    void Start()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        Invoke("SalvarProgresso", 1f);
    }

    public void SalvarProgresso()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");
        
        string cenaAtual = SceneManager.GetActiveScene().name;
        Vector3 posSegura = player != null ? player.transform.position : Vector3.zero;

        if (SistemaGlobal.Instance != null)
        {
            SistemaGlobal.Instance.SalvarJogo(posSegura, cenaAtual);
        }
        
        // 🔥 CRAVA A CENA NO HD SEM DESCULPA 🔥
        if (PersistenciaManager.Instance != null)
        {
            int slotCerto = SistemaGlobal.Instance != null ? SistemaGlobal.Instance.slotAtual : 1;
            
            PersistenciaManager.Instance.SalvarString("Slot_" + slotCerto + "_Cena", cenaAtual);
            PersistenciaManager.Instance.SalvarTudo();
        }
    }
}
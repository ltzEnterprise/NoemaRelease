using UnityEngine;

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
        
        if (player != null && SistemaGlobal.Instance != null)
        {
            SistemaGlobal.Instance.SalvarJogo(player.transform.position, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            Debug.Log("Progresso salvo automaticamente!");
        }
    }
}

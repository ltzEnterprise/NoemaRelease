using UnityEngine;
using UnityEngine.SceneManagement; 

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
        
        // Agora o SistemaGlobal faz TUDO em um lugar só.
        if (player != null && SistemaGlobal.Instance != null)
        {
            SistemaGlobal.Instance.SalvarJogo(player.transform.position, SceneManager.GetActiveScene().name);
        }
        else if (PersistenciaManager.Instance != null)
        {
            // Segurança caso você chame o botão no menu ou sem player na cena
            PersistenciaManager.Instance.SalvarTudo();
        }

        Debug.Log("<color=green>[GameManager] Progresso salvo em bloco fechado!</color>");
    }
}
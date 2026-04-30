using UnityEngine;
using UnityEngine.SceneManagement; 
using System.Collections; 

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Configurações de Save")]
    public string nomeCenaPadrao = "DreamSceane";
    public string nomeCenaMenu = "MenuPrincipal";
    public bool autoSaveAoPegarRuna = true;

    public GameObject player;
    public static bool CenaPronta = false;

    private Coroutine rotinaAtual;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[GameManager] Cópia local/duplicada destruída. Mantendo a instância global.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CenaPronta = false;

        if (SistemaGlobal.Instance != null)
            SistemaGlobal.Instance.sistemaPronto = false;

        if (rotinaAtual != null)
        {
            StopCoroutine(rotinaAtual);
            rotinaAtual = null;
        }

        if (scene.name == nomeCenaMenu)
        {
            player = null;
            return;
        }

        rotinaAtual = StartCoroutine(PrepararCenaERegistrarSave(scene.name));
    }

    IEnumerator PrepararCenaERegistrarSave(string nomeCena)
    {
        yield return new WaitUntil(() =>
            PersistenciaManager.Instance != null &&
            PersistenciaManager.Instance.DadosProntosParaUso &&
            !PersistenciaManager.Instance.EstaCarregando
        );

        yield return new WaitUntil(() => GameObject.FindGameObjectWithTag("Player") != null);

        player = GameObject.FindGameObjectWithTag("Player");

        if (SistemaGlobal.Instance != null && SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar)
        {
            string prefixo = "Slot_" + SistemaGlobal.Instance.slotAtual;

            if (PersistenciaManager.Instance != null &&
                PersistenciaManager.Instance.TemFloat(prefixo + "_PosX"))
            {
                float x = PersistenciaManager.Instance.ObterFloat(prefixo + "_PosX");
                float y = PersistenciaManager.Instance.ObterFloat(prefixo + "_PosY");
                float z = PersistenciaManager.Instance.ObterFloat(prefixo + "_PosZ");

                player.transform.position = new Vector3(x, y + 0.1f, z);
                Physics.SyncTransforms();
            }

            SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = false;
        }

        yield return new WaitForSeconds(0.35f);

        CenaPronta = true;

        if (SistemaGlobal.Instance != null)
        {
            SistemaGlobal.Instance.sistemaPronto = true;

            if (SistemaGlobal.Instance.acabouDeCarregar)
            {
                SistemaGlobal.Instance.acabouDeCarregar = false;
                rotinaAtual = null;
                yield break;
            }
        }

        SalvarProgresso();

        rotinaAtual = null;
    }

    public void SalvarProgresso()
    {
        if (!CenaPronta) return;

        if (SistemaGlobal.Instance == null) return;
        if (!SistemaGlobal.Instance.slotFoiDefinido || SistemaGlobal.Instance.slotAtual <= 0) return;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        string cenaAtual = SceneManager.GetActiveScene().name;
        Vector3 posSegura = player != null ? player.transform.position : Vector3.zero;

        SistemaGlobal.Instance.SalvarJogo(posSegura, cenaAtual);
    }
}

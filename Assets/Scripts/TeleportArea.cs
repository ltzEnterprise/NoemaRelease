using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Text;

[RequireComponent(typeof(Collider))]
public class TeleportArea : MonoBehaviour
{
    [Header("--- SAVE AUTOMÁTICO ---")]
    [SerializeField]
    private string chavePersistente;

    [Header("--- DESTINATION ---")]
    public Transform destinationPoint;

    [Header("--- SETTINGS ---")]
    public KeyCode teleportKey = KeyCode.T;
    public float offsetVerticalTeleporte = 0.15f;
    public bool usarRotacaoDoDestino = false;

    [Header("--- UI & EFFECTS ---")]
    public GameObject interactionTextUI;
    public AudioSource audioSource;
    public AudioClip teleportSound;

    private bool playerInArea = false;
    private bool usadoNestaSessao = false;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(chavePersistente))
            chavePersistente = System.Guid.NewGuid().ToString("N");
    }

    private void Reset()
    {
        if (string.IsNullOrWhiteSpace(chavePersistente))
            chavePersistente = System.Guid.NewGuid().ToString("N");
    }
#endif

    private void Awake()
    {
        GarantirChavePersistente();
    }

    private void Start()
    {
        if (interactionTextUI)
            interactionTextUI.SetActive(false);

        Collider col = GetComponent<Collider>();

        if (col != null)
            col.isTrigger = true;

        StartCoroutine(CarregarEstadoSeguro());
    }

    private IEnumerator CarregarEstadoSeguro()
    {
        yield return new WaitUntil(() => PersistenciaManager.Instance != null);
        yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);
        yield return new WaitUntil(() => !PersistenciaManager.Instance.EstaCarregando);
        yield return null;

        usadoNestaSessao = PersistenciaManager.Instance.ObterEstado(ChaveUsado(), false);

        Debug.Log("[TeleportArea] Estado carregado. TP=" + gameObject.name +
                  " | chave=" + ChaveUsado() +
                  " | usado=" + usadoNestaSessao);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<FPS_Master>() != null || other.GetComponentInParent<FPS_Master>() != null)
        {
            playerInArea = true;

            if (interactionTextUI)
                interactionTextUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<FPS_Master>() != null || other.GetComponentInParent<FPS_Master>() != null)
        {
            playerInArea = false;

            if (interactionTextUI)
                interactionTextUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerInArea && Input.GetKeyDown(teleportKey))
            ExecuteTeleport();
    }

    private void ExecuteTeleport()
    {
        if (destinationPoint == null)
        {
            Debug.LogError("[TeleportArea] destinationPoint não foi atribuído em: " + gameObject.name);
            return;
        }

        if (FPS_Master.Instance == null)
        {
            Debug.LogError("[TeleportArea] FPS_Master.Instance está nulo.");
            return;
        }

        RegistrarComoUsadoESalvar();

        if (audioSource && teleportSound)
            audioSource.PlayOneShot(teleportSound);

        if (interactionTextUI)
            interactionTextUI.SetActive(false);

        playerInArea = false;

        Vector3 destinoSeguro = destinationPoint.position + Vector3.up * offsetVerticalTeleporte;

        FPS_Master.Instance.Teleportar(destinoSeguro);

        if (usarRotacaoDoDestino)
        {
            Vector3 rotacaoAtual = FPS_Master.Instance.transform.eulerAngles;

            FPS_Master.Instance.transform.rotation = Quaternion.Euler(
                rotacaoAtual.x,
                destinationPoint.eulerAngles.y,
                rotacaoAtual.z
            );

            if (FPS_Master.Instance.cameraJogador != null)
                FPS_Master.Instance.cameraJogador.transform.localRotation = Quaternion.identity;
        }

        Physics.SyncTransforms();
    }

    public bool FoiUsado()
    {
        if (usadoNestaSessao)
            return true;

        if (PersistenciaManager.Instance == null)
            return false;

        if (!PersistenciaManager.Instance.DadosProntosParaUso || PersistenciaManager.Instance.EstaCarregando)
            return usadoNestaSessao;

        usadoNestaSessao = PersistenciaManager.Instance.ObterEstado(ChaveUsado(), false);

        return usadoNestaSessao;
    }

    public void RegistrarComoUsadoESalvar()
    {
        usadoNestaSessao = true;

        if (PersistenciaManager.Instance == null)
        {
            Debug.LogError("[TeleportArea] PersistenciaManager.Instance está nulo. TP marcado só em sessão.");
            return;
        }

        PersistenciaManager.Instance.RegistrarEstado(ChaveUsado(), true);
        PersistenciaManager.Instance.SalvarTudo(true);

        Debug.Log("[TeleportArea] TP usado e salvo. TP=" + gameObject.name + " | chave=" + ChaveUsado());
    }

    public string ChaveUsado()
    {
        GarantirChavePersistente();
        return "TP_USED_" + chavePersistente;
    }

    private void GarantirChavePersistente()
    {
        if (!string.IsNullOrWhiteSpace(chavePersistente))
            return;

        chavePersistente = GerarChaveFallbackDeterministica();
    }

    private string GerarChaveFallbackDeterministica()
    {
        string cena = SceneManager.GetActiveScene().name;
        string caminho = CaminhoNaHierarquia(transform);

        return NormalizarChave(cena + "_" + caminho);
    }

    private string CaminhoNaHierarquia(Transform alvo)
    {
        if (alvo == null)
            return "NULL";

        string caminho = alvo.name;
        Transform atual = alvo.parent;

        while (atual != null)
        {
            caminho = atual.name + "_" + caminho;
            atual = atual.parent;
        }

        return caminho;
    }

    private string NormalizarChave(string texto)
    {
        if (string.IsNullOrEmpty(texto))
            return "VAZIO";

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < texto.Length; i++)
        {
            char c = texto[i];

            if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else
                sb.Append("_");
        }

        return sb.ToString();
    }

    [ContextMenu("Gerar nova chave persistente")]
    private void GerarNovaChavePersistente()
    {
        chavePersistente = System.Guid.NewGuid().ToString("N");
        Debug.Log("[TeleportArea] Nova chave persistente gerada: " + chavePersistente);
    }

    [ContextMenu("Mostrar chave de save")]
    private void MostrarChaveDeSave()
    {
        Debug.Log("[TeleportArea] Chave de save: " + ChaveUsado());
    }
}
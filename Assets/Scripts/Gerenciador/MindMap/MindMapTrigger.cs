using UnityEngine;

public class MindMapTrigger : MonoBehaviour
{
    [Header("1. Mensagem na Tela Normal")]
    [TextArea(2, 4)]
    public string hintMessage = "Nova pista no Mapa Mental!";
    public float displayTime = 4f;

    [Header("2. O que destrava no Mapa Mental?")]
    [Tooltip("Arraste os objetos lá do Canvas que têm o script MindMapNode")]
    public MindMapNode[] nodesToUnlock; 

    [Header("3. Configurações")]
    public bool triggerOnStep = true;
    public bool destroyAfterUse = true;

    void Start()
    {
        // Se o primeiro nó dessa lista já estiver salvo no HD como descoberto, o gatilho se mata ao carregar a cena
        if (nodesToUnlock.Length > 0 && PlayerPrefs.GetInt("MindMap_" + nodesToUnlock[0].nodeID, 0) == 1)
        {
            if (destroyAfterUse) Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (triggerOnStep && other.CompareTag("Player"))
        {
            UnlockEverything();
        }
    }

    // Você pode chamar essa função pelo seu script de Interação/Click
    public void UnlockEverything()
    {
        // 1. Manda a dica normal pra tela (independente da dificuldade)
        if (HintManager.Instance != null && !string.IsNullOrEmpty(hintMessage))
        {
            HintManager.Instance.MostrarDica(hintMessage, displayTime, false);
        }

        // 2. Manda os nós do mapa mental se desbloquearem e salvarem
        foreach (MindMapNode node in nodesToUnlock)
        {
            if (node != null) node.UnlockNode();
        }

        // 3. Se mata
        if (destroyAfterUse) Destroy(gameObject);
    }
}
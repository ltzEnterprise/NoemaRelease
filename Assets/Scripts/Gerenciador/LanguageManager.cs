using UnityEngine;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance;

    [HideInInspector] public int currentLanguage = 0; // 0 = PT, 1 = EN

    // 🔥 A MÁGICA DA EXCELÊNCIA: Roda ANTES da cena carregar, em qualquer cena que você der Play!
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        // Se já existe um na cena (caso você tenha esquecido de deletar algum), ele ignora para não clonar.
        if (Instance != null) return;

        // Pesca o Prefab direto da pasta Resources
        GameObject prefab = Resources.Load<GameObject>("LanguageManager");

        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab);
            instance.name = "LanguageManager (Auto)"; // Muda o nome pra você saber que foi gerado sozinho
        }
        else
        {
            // Se der merda e ele não achar a pasta/prefab, ele cria um vazio pra não quebrar o jogo
            GameObject go = new GameObject("LanguageManager (Fallback)");
            go.AddComponent<LanguageManager>();
            Debug.LogWarning("<color=yellow>[LanguageManager]</color> Prefab não encontrado em 'Resources/LanguageManager'. Criando um vazio por segurança.");
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // O escudo principal: Ele nunca morre quando você passa pelas portas
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        LoadLanguage();
    }

    public void LoadLanguage()
    {
        // Aqui o PlayerPrefs tá CERTÍSSIMO. Idioma é config do PC do cara, não do slot de save.
        currentLanguage = PlayerPrefs.GetInt("Idioma", 0);
    }

    public void ChangeLanguage(int newLanguage)
    {
        currentLanguage = newLanguage;
        PlayerPrefs.SetInt("Idioma", currentLanguage);
        PlayerPrefs.Save();
        
        AtualizarTodosOsTextosNaCena();
    }

    public void AtualizarTodosOsTextosNaCena()
    {
        // Varre a cena inteira atualizando tudo que tiver o script LocalizedText (mesmo os desativados)
        LocalizedText[] textsInScene = FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var txt in textsInScene)
        {
            if (txt != null) txt.UpdateText();
        }
    }
}
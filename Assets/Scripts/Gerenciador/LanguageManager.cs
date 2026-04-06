using UnityEngine;

public class LanguageManager : MonoBehaviour
{
    public static LanguageManager Instance;

    [HideInInspector] public int currentLanguage = 0; // 0 = PT, 1 = EN

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
            return; 
        }
        
        LoadLanguage();
    }

    public void LoadLanguage()
    {
        currentLanguage = PlayerPrefs.GetInt("Idioma", 0);
    }

    public void ChangeLanguage(int newLanguage)
    {
        currentLanguage = newLanguage;
        PlayerPrefs.SetInt("Idioma", currentLanguage);
        PlayerPrefs.Save();
        
        // Grita pra todo mundo da cena que tem o script atualizar na hora
        LocalizedText[] textsInScene = FindObjectsByType<LocalizedText>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var txt in textsInScene)
        {
            txt.UpdateText();
        }
    }
}
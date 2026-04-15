using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class KeyHUDConfig
{
    [Tooltip("Ex: Chave_Porao")]
    public string keyID;
    public GameObject hudIcon;
}

public class KeyManager : MonoBehaviour
{
    public static KeyManager Instance;
    public List<KeyHUDConfig> keyDatabase = new List<KeyHUDConfig>();
    private List<string> chavesNoBolso = new List<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        SincronizarChavesComSave();
    }

    public void SincronizarChavesComSave()
    {
        chavesNoBolso.Clear();
        if (PersistenciaManager.Instance == null) return;

        foreach (var key in keyDatabase)
        {
            bool temNoSave = PersistenciaManager.Instance.ObterEstado("Key_" + key.keyID);
            if (temNoSave)
            {
                chavesNoBolso.Add(key.keyID);
                if (key.hudIcon != null) key.hudIcon.SetActive(true);
            }
            else
            {
                if (key.hudIcon != null) key.hudIcon.SetActive(false);
            }
        }
    }

    public bool CheckIfHasKey(string searchedID)
    {
        return chavesNoBolso.Contains(searchedID);
    }

    public void TurnOnKeyIcon(string searchedID)
    {
        if (!chavesNoBolso.Contains(searchedID)) chavesNoBolso.Add(searchedID);
        
        foreach (var key in keyDatabase)
            if (key.keyID == searchedID && key.hudIcon != null) key.hudIcon.SetActive(true);

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado("Key_" + searchedID, true);
            PersistenciaManager.Instance.SalvarTudo();
        }
    }

    public void TurnOffKeyIcon(string searchedID)
    {
        if (chavesNoBolso.Contains(searchedID)) chavesNoBolso.Remove(searchedID);

        foreach (var key in keyDatabase)
            if (key.keyID == searchedID && key.hudIcon != null) key.hudIcon.SetActive(false);

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado("Key_" + searchedID, false);
            PersistenciaManager.Instance.SalvarTudo();
        }
    }
}
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

    [Header("--- KEY INVENTORY ---")]
    [Tooltip("Add all keys here and link their Canvas UI images")]
    public List<KeyHUDConfig> keyDatabase = new List<KeyHUDConfig>();

    private List<string> chavesNoBolso = new List<string>();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        chavesNoBolso.Clear();

        foreach (var key in keyDatabase)
        {
            if (key.hudIcon != null)
            {
                key.hudIcon.SetActive(false);
            }

            if (!Application.isEditor && PersistenciaManager.Instance != null)
            {
                bool hasKeyInSave = PersistenciaManager.Instance.ObterEstado("Key_" + key.keyID);
                if (hasKeyInSave)
                {
                    chavesNoBolso.Add(key.keyID); 
                    if (key.hudIcon != null) key.hudIcon.SetActive(true); 
                }
            }
        }
    }

    public bool CheckIfHasKey(string searchedID)
    {
        bool isRegistered = false;
        foreach (var key in keyDatabase)
        {
            if (key.keyID == searchedID) isRegistered = true;
        }

        if (!isRegistered)
        {
            Debug.LogError($"<color=red>[PROGRESSION BUG]</color> A chave '{searchedID}' foi exigida, mas NÃO ESTÁ CADASTRADA no KeyManager!");
        }

        if (chavesNoBolso.Contains(searchedID))
        {
            return true;
        }

        if (!Application.isEditor && PersistenciaManager.Instance != null)
        {
            return PersistenciaManager.Instance.ObterEstado("Key_" + searchedID);
        }

        return false;
    }

    public void TurnOnKeyIcon(string searchedID)
    {
        if (!chavesNoBolso.Contains(searchedID))
        {
            chavesNoBolso.Add(searchedID);
        }

        foreach (var key in keyDatabase)
        {
            if (key.keyID == searchedID && key.hudIcon != null)
                key.hudIcon.SetActive(true);
        }

        // --- SALVA A CHAVE IMEDIATAMENTE NO DISCO ---
        if (!Application.isEditor && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado("Key_" + searchedID, true);
            PersistenciaManager.Instance.SalvarTudo();
        }
    }

    public void TurnOffKeyIcon(string searchedID)
    {
        if (chavesNoBolso.Contains(searchedID))
        {
            chavesNoBolso.Remove(searchedID);
        }

        foreach (var key in keyDatabase)
        {
            if (key.keyID == searchedID && key.hudIcon != null)
                key.hudIcon.SetActive(false);
        }

        // --- REMOVE A CHAVE DO DISCO ---
        if (!Application.isEditor && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado("Key_" + searchedID, false);
            PersistenciaManager.Instance.SalvarTudo();
        }
    }
}
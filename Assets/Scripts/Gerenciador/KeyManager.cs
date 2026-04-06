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

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // --- CORREÇÃO DO ERRO BÁSICO ---
        // 1. DESLIGA TUDO PRIMEIRO. Isso roda sempre, até no Editor.
        // Limpa a cagada de deixar a UI acesa na hora de editar a cena.
        foreach (var key in keyDatabase)
        {
            if (key.hudIcon != null)
            {
                key.hudIcon.SetActive(false);
            }
        }

        // 2. LÊ O SAVE DEPOIS. Isso tem a trava do Editor pra não ler fantasma.
        if (!Application.isEditor && PersistenciaManager.Instance != null)
        {
            foreach (var key in keyDatabase)
            {
                if (key.hudIcon != null)
                {
                    bool hasKeyInSave = PersistenciaManager.Instance.ObterEstado("Key_" + key.keyID);
                    key.hudIcon.SetActive(hasKeyInSave);
                }
            }
        }
    }

    public bool CheckIfHasKey(string searchedID)
    {
        foreach (var key in keyDatabase)
        {
            if (key.keyID == searchedID)
            {
                return key.hudIcon != null && key.hudIcon.activeSelf;
            }
        }

        Debug.LogError($"<color=red>[PROGRESSION BUG]</color> A chave '{searchedID}' foi exigida, mas NÃO ESTÁ CADASTRADA no KeyManager!");

        if (!Application.isEditor && PersistenciaManager.Instance != null)
        {
            return PersistenciaManager.Instance.ObterEstado("Key_" + searchedID);
        }

        return false;
    }

    public void TurnOnKeyIcon(string searchedID)
    {
        foreach (var key in keyDatabase)
        {
            if (key.keyID == searchedID && key.hudIcon != null)
                key.hudIcon.SetActive(true);
        }
    }

    public void TurnOffKeyIcon(string searchedID)
    {
        foreach (var key in keyDatabase)
        {
            if (key.keyID == searchedID && key.hudIcon != null)
                key.hudIcon.SetActive(false);
        }
    }
}
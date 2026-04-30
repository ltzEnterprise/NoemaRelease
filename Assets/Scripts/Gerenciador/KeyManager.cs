using UnityEngine;
using System.Collections;
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

    private bool saveCarregado = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        StartCoroutine(SincronizarChavesComSaveSeguro());
    }

    IEnumerator SincronizarChavesComSaveSeguro()
    {
        yield return new WaitUntil(() => PersistenciaManager.Instance != null);
        yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);

        SincronizarChavesComSave();

        saveCarregado = true;
    }

    public void SincronizarChavesComSave()
    {
        chavesNoBolso.Clear();

        if (PersistenciaManager.Instance == null)
            return;

        foreach (var key in keyDatabase)
        {
            if (key == null || string.IsNullOrEmpty(key.keyID))
                continue;

            bool temNoSave = PersistenciaManager.Instance.ObterEstado("Key_" + key.keyID);

            if (temNoSave)
            {
                if (!chavesNoBolso.Contains(key.keyID))
                    chavesNoBolso.Add(key.keyID);

                if (key.hudIcon != null)
                    key.hudIcon.SetActive(true);
            }
            else
            {
                if (key.hudIcon != null)
                    key.hudIcon.SetActive(false);
            }
        }
    }

    public bool CheckIfHasKey(string searchedID)
    {
        if (string.IsNullOrEmpty(searchedID))
            return false;

        return chavesNoBolso.Contains(searchedID);
    }

    public void TurnOnKeyIcon(string searchedID)
    {
        if (string.IsNullOrEmpty(searchedID))
            return;

        if (!chavesNoBolso.Contains(searchedID))
            chavesNoBolso.Add(searchedID);
        
        foreach (var key in keyDatabase)
        {
            if (key == null) continue;

            if (key.keyID == searchedID && key.hudIcon != null)
                key.hudIcon.SetActive(true);
        }

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado("Key_" + searchedID, true);
            SalvarProgressoSeguro();
        }
    }

    public void TurnOffKeyIcon(string searchedID)
    {
        if (string.IsNullOrEmpty(searchedID))
            return;

        if (chavesNoBolso.Contains(searchedID))
            chavesNoBolso.Remove(searchedID);

        foreach (var key in keyDatabase)
        {
            if (key == null) continue;

            if (key.keyID == searchedID && key.hudIcon != null)
                key.hudIcon.SetActive(false);
        }

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado("Key_" + searchedID, false);
            SalvarProgressoSeguro();
        }
    }

    private void SalvarProgressoSeguro()
    {
        if (!saveCarregado)
            return;

        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(false);
    }
}
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class DevToolsEditor
{
    // 1. RESET GERAL DE SAVES E DADOS
    [MenuItem("Hacks/Reset All Saves")]
    public static void ResetarSaveGeral()
    {
        // Apaga configurações e slots
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // Apaga o estado global da memória rodando
        EstadoGlobal.ResetarTudo();

        Debug.LogWarning(" TODOS OS SAVES E DADOS FORAM APAGADOS DO SISTEMA ");
    }

    // 2. LIMPACÃO DE MISSING SCRIPTS
    [MenuItem("Hacks/Remove Missing Scripts")]
    public static void RemoveMissingScripts()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        int removedCount = 0;

        foreach (GameObject go in rootObjects)
        {
            removedCount += CleanMissingScriptsRecursively(go);
        }

        if (removedCount > 0)
        {
            Debug.Log($"[DEV TOOLS] Sucesso! {removedCount} missing scripts foram deletados.");
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        else
        {
            Debug.Log("[DEV TOOLS] A cena está limpa. Nenhum missing script encontrado.");
        }
    }

    private static int CleanMissingScriptsRecursively(GameObject obj)
    {
        int count = 0;
        count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);

        foreach (Transform child in obj.transform)
        {
            count += CleanMissingScriptsRecursively(child.gameObject);
        }

        return count;
    }
}
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO; // <--- CERTIFIQUE-SE DE TER ESSA LINHA NO TOPO

public class DevToolsEditor
{
    // 1. RESET GERAL DE SAVES E DADOS
    [MenuItem("Hacks/Reset All Saves")]
    public static void ResetarSaveGeral()
    {
        // A) Apaga PlayerPrefs (Configurações básicas)
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();

        // B) Apaga os arquivos JSON do PersistenciaManager
        // Usamos o mesmo caminho que o seu Manager usa
        string diretorioSaves = Path.Combine(Application.persistentDataPath, "Saves");

        if (Directory.Exists(diretorioSaves))
        {
            // Deleta a pasta e tudo dentro dela
            Directory.Delete(diretorioSaves, true);
            // Cria de novo vazia para o sistema não dar erro de "pasta não encontrada"
            Directory.CreateDirectory(diretorioSaves);
            
            Debug.Log("<color=cyan>[DEV TOOLS]</color> Pasta de arquivos JSON deletada.");
        }

        // C) Se você estiver com o jogo aberto no Editor, limpa a memória RAM agora
        if (Application.isPlaying && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.LimparDicionario();
            Debug.Log("<color=cyan>[DEV TOOLS]</color> Cache da memória RAM limpo.");
        }

        Debug.LogWarning(" !!! TODOS OS SAVES (PLAYERPREFS E JSON) FORAM APAGADOS DO SISTEMA !!! ");
    }

    // 2. LIMPACÃO SUPREMA DE MISSING SCRIPTS (CENA + PREFABS)
    [MenuItem("Hacks/Remove Missing Scripts (Cena e Prefabs)")]
    public static void RemoveMissingScripts()
    {
        int cenaCount = LimparCena();
        int prefabCount = LimparPrefabsNoProjeto();

        if (cenaCount > 0 || prefabCount > 0)
        {
            Debug.Log($"<color=green><b>[DEV TOOLS] EXORCISMO CONCLUÍDO!</b></color>\nLixos removidos da Cena: {cenaCount}\nLixos removidos direto dos Prefabs: {prefabCount}");
        }
        else
        {
            Debug.Log("[DEV TOOLS] Tudo limpo. Nenhum missing script encontrado em lugar nenhum.");
        }
    }

    private static int LimparCena()
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        int removedCount = 0;

        foreach (GameObject go in rootObjects)
        {
            removedCount += CleanMissingScriptsRecursively(go);
        }

        if (removedCount > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        return removedCount;
    }

    private static int LimparPrefabsNoProjeto()
    {
        int removedCount = 0;
        
        // Acha TODOS os prefabs do projeto inteiro
        string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in allPrefabs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                // Limpa o prefab e os filhos dele
                int count = CleanMissingScriptsRecursively(prefab);
                if (count > 0)
                {
                    removedCount += count;
                    EditorUtility.SetDirty(prefab); // Avisa a Unity que o arquivo foi modificado
                }
            }
        }

        if (removedCount > 0)
        {
            AssetDatabase.SaveAssets(); // Salva a limpeza no HD pra não voltar mais
        }
        return removedCount;
    }

    private static int CleanMissingScriptsRecursively(GameObject obj)
    {
        int count = 0;
        // O comando matador da Unity
        count += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);

        foreach (Transform child in obj.transform)
        {
            count += CleanMissingScriptsRecursively(child.gameObject);
        }

        return count;
    }
}
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO;

public class DeepAssetCleaner : EditorWindow
{
    public string targetFolder = "Assets/";
    
    [Tooltip("Drag the scenes you want to verify here.")]
    public List<SceneAsset> scenesToVerify = new List<SceneAsset>();
    
    [Tooltip("Drag specific folders or files that should NEVER be deleted.")]
    public List<Object> exceptions = new List<Object>();

    private SerializedObject so;
    private SerializedProperty scenesProp;
    private SerializedProperty exceptionsProp;

    [MenuItem("Tools/Noema/Deep Asset Cleaner (Brute Force)")]
    public static void ShowWindow()
    {
        GetWindow<DeepAssetCleaner>("Deep Cleaner");
    }

    private void OnEnable()
    {
        so = new SerializedObject(this);
        scenesProp = so.FindProperty("scenesToVerify");
        exceptionsProp = so.FindProperty("exceptions");
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("ULTIMATE CLEANER: Deep Text Scan Enabled", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        targetFolder = EditorGUILayout.TextField("Target Folder:", targetFolder);
        GUILayout.Label("Ex: Assets/Plugins/OldStuff", EditorStyles.miniLabel);
        
        GUILayout.Space(15);
        
        so.Update();
        EditorGUILayout.PropertyField(scenesProp, new GUIContent("Scenes to Verify"), true);
        GUILayout.Space(5);
        EditorGUILayout.PropertyField(exceptionsProp, new GUIContent("Exceptions (Safe List)"), true);
        so.ApplyModifiedProperties();

        GUILayout.Space(20);

        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("SCAN & DESTROY (DEEP CODE ANALYSIS)", GUILayout.Height(40)))
        {
            ExecuteDeepClean();
        }
        GUI.backgroundColor = Color.white;
    }

    void ExecuteDeepClean()
    {
        if (!AssetDatabase.IsValidFolder(targetFolder))
        {
            EditorUtility.DisplayDialog("Error", $"Target folder '{targetFolder}' does not exist.", "OK");
            return;
        }

        if (scenesToVerify.Count == 0)
        {
            EditorUtility.DisplayDialog("Warning", "You must add at least one scene to verify!", "OK");
            return;
        }

        try
        {
            // 1. STANDARD DEPENDENCIES (O jeito normal da Unity)
            EditorUtility.DisplayProgressBar("Deep Scan", "Getting standard dependencies...", 0.1f);
            List<string> scenePaths = scenesToVerify.Where(s => s != null).Select(AssetDatabase.GetAssetPath).ToList();
            HashSet<string> usedAssets = new HashSet<string>(AssetDatabase.GetDependencies(scenePaths.ToArray(), true));

            // 2. EXCEPTION LIST
            List<string> ignoredPaths = exceptions.Where(e => e != null).Select(AssetDatabase.GetAssetPath).ToList();

            // 3. GET ALL TARGET FILES
            EditorUtility.DisplayProgressBar("Deep Scan", "Gathering target files...", 0.2f);
            string[] guidsInFolder = AssetDatabase.FindAssets("", new[] { targetFolder });
            List<string> potentialGarbage = new List<string>();

            foreach (string guid in guidsInFolder)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                
                if (AssetDatabase.IsValidFolder(path)) continue; 
                if (path == AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this))) continue; 

                // Intocáveis da Unity
                if (path.Contains("/Resources/") || path.Contains("/Editor/")) continue;
                
                // Ignora núcleo do motor
                string ext = Path.GetExtension(path).ToLower();
                if (ext == ".dll" || ext == ".asmdef" || ext == ".asset") continue;

                // Checa exceções
                if (ignoredPaths.Any(ign => path.StartsWith(ign))) continue;

                // Se não tá na lista de uso padrão, vira suspeito
                if (!usedAssets.Contains(path))
                {
                    potentialGarbage.Add(path);
                }
            }

            // ====================================================================
            // 4. DEEP CODE SCAN (A PARTE DA FORÇA BRUTA QUE VOCÊ PEDIU)
            // ====================================================================
            EditorUtility.DisplayProgressBar("Deep Scan", "Reading all scripts and shaders in the project...", 0.4f);
            
            // Pega TODOS os scripts, shaders, cginc do projeto inteiro
            string[] allCodeGUIDs = AssetDatabase.FindAssets("t:MonoScript t:Shader");
            List<string> allCodeTexts = new List<string>();

            for (int i = 0; i < allCodeGUIDs.Length; i++)
            {
                string codePath = AssetDatabase.GUIDToAssetPath(allCodeGUIDs[i]);
                if (File.Exists(codePath))
                {
                    allCodeTexts.Add(File.ReadAllText(codePath));
                }
            }

            List<string> confirmedGarbage = new List<string>();

            // Testa arquivo por arquivo suspeito contra TODOS os códigos do jogo
            for (int i = 0; i < potentialGarbage.Count; i++)
            {
                string suspectPath = potentialGarbage[i];
                string suspectName = Path.GetFileNameWithoutExtension(suspectPath);
                string suspectGUID = AssetDatabase.AssetPathToGUID(suspectPath);
                
                EditorUtility.DisplayProgressBar("Deep Scan", $"Analyzing {suspectName}...", 0.4f + (0.5f * ((float)i / potentialGarbage.Count)));

                bool isReferencedInCode = false;

                // Analisa o texto puro de todos os códigos
                foreach (string codeText in allCodeTexts)
                {
                    // Se o nome do arquivo ou o GUID dele estiver escrito em algum script, ELE TÁ SALVO!
                    if (codeText.Contains(suspectName) || codeText.Contains(suspectGUID))
                    {
                        isReferencedInCode = true;
                        break;
                    }
                }

                if (!isReferencedInCode)
                {
                    confirmedGarbage.Add(suspectPath); // Se ninguém fala dele, é LIXO
                }
            }
            // ====================================================================

            EditorUtility.ClearProgressBar();

            // 5. EXECUÇÃO
            if (confirmedGarbage.Count > 0)
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "MASSIVE DESTRUCTION ALERT", 
                    $"Found {confirmedGarbage.Count} completely unused files in '{targetFolder}'.\n\nNot even your scripts or shaders mention them.\nThey will be moved to the Windows Recycle Bin.\n\nAre you absolutely sure?", 
                    "DESTROY", 
                    "Cancel"
                );

                if (confirm)
                {
                    int deletedCount = 0;
                    foreach (string p in confirmedGarbage)
                    {
                        if (AssetDatabase.MoveAssetToTrash(p)) deletedCount++;
                    }
                    Debug.Log($"[DeepAssetCleaner] Cleanup complete! {deletedCount} files moved to the Recycle Bin.");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Success", "Target folder is completely clean! All assets inside it are being used.", "OK");
            }

            // 6. LIMPA AS PASTAS VAZIAS
            DeleteEmptyFoldersRecursively(targetFolder);
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"[DeepAssetCleaner] Error during scan: {e.Message}");
        }
        finally
        {
            EditorUtility.ClearProgressBar(); // Garante que a barra de carregamento suma se der merda
        }
    }

    void DeleteEmptyFoldersRecursively(string folder)
    {
        string fullPath = Path.GetFullPath(folder);
        if (!Directory.Exists(fullPath)) return;

        string[] subfolders = Directory.GetDirectories(fullPath);

        foreach (string subfolder in subfolders)
        {
            string relativeSubfolder = "Assets" + subfolder.Replace(Application.dataPath, "").Replace("\\", "/");
            DeleteEmptyFoldersRecursively(relativeSubfolder);
        }

        var filesInFolder = Directory.EnumerateFiles(fullPath).Where(f => !f.EndsWith(".meta"));
        var remainingSubfolders = Directory.GetDirectories(fullPath);

        if (!filesInFolder.Any() && remainingSubfolders.Length == 0)
        {
            AssetDatabase.MoveAssetToTrash(folder);
            Debug.Log($"[DeepAssetCleaner] Empty folder obliterated: {folder}");
        }
    }
}
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class EditorMemoryCleaner : EditorWindow
{
    [MenuItem("Tools/Clear Eternal Cache")]
    public static void CleanMemory()
    {
        // Força a Unity a soltar todas as texturas e assets que não estão sendo usados
        Resources.UnloadUnusedAssets();
        
        // Força o coletor de lixo do C# a limpar a RAM
        System.GC.Collect();
        
        Debug.Log("<color=cyan><b>[Memory Tools]</b> Eternal Cache cleared successfully!</color>");
    }
}
#endif
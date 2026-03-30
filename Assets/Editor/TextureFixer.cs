using UnityEngine;
using UnityEditor;

public class TextureFixer : EditorWindow 
{
    [MenuItem("Tools/Resgatar Texturas (HDRP para URP)")]
    public static void FixTextures()
    {
        // Pega todos os materiais que você selecionou
        foreach (Material mat in Selection.GetFiltered<Material>(SelectionMode.DeepAssets))
        {
            Undo.RecordObject(mat, "Fix Texture"); // Permite dar Ctrl+Z se der merda

            // 1. Tenta achar a textura escondida nos nomes antigos do HDRP
            Texture texture = mat.GetTexture("_BaseColorMap"); 
            
            // Se não achou, tenta o nome antigo do Standard
            if (texture == null) texture = mat.GetTexture("_MainTex");

            // 2. Se achou alguma coisa, joga no nome novo do URP (_BaseMap)
            if (texture != null)
            {
                mat.SetTexture("_BaseMap", texture);
                mat.SetColor("_BaseColor", Color.white); // Garante que a cor base seja branca (senão fica escuro)
                Debug.Log($"Resgatada textura para: {mat.name}");
            }
        }
        Debug.Log("Processo finalizado!");
    }
}
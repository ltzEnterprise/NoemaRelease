#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class GrassBrightnessTool : EditorWindow
{
    public Texture2D originalTexture;
    public float brightnessMultiplier = 1.5f; // 1 is normal brightness, higher values make it brighter
    public Color colorTint = Color.white;

    // Creates a new button in the Unity top menu
    [MenuItem("Tools/Grass Brightness Adjuster")]
    public static void ShowWindow()
    {
        GetWindow<GrassBrightnessTool>("Grass Adjustment");
    }

    void OnGUI()
    {
        GUILayout.Label("Grass Mini-Photoshop", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        originalTexture = (Texture2D)EditorGUILayout.ObjectField("Original Texture", originalTexture, typeof(Texture2D), false);
        
        EditorGUILayout.Space();
        brightnessMultiplier = EditorGUILayout.Slider("Brightness Multiplier", brightnessMultiplier, 0.5f, 5.0f);
        colorTint = EditorGUILayout.ColorField("Color Tint", colorTint);
        
        EditorGUILayout.Space();

        if (GUILayout.Button("Create Brightened Texture", GUILayout.Height(40)))
        {
            if (originalTexture != null)
            {
                CreateTexture();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Drag the original grass texture first, goddammit!", "OK");
            }
        }
    }

    void CreateTexture()
    {
        string originalPath = AssetDatabase.GetAssetPath(originalTexture);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(originalPath);
        
        // Unlock the image so Unity can read its pixels
        bool wasReadable = importer.isReadable;
        if (!wasReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        Color[] pixels = originalTexture.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            // Multiply brightness and color, but leave Alpha (transparency) intact
            pixels[i].r = Mathf.Clamp01(pixels[i].r * brightnessMultiplier * colorTint.r);
            pixels[i].g = Mathf.Clamp01(pixels[i].g * brightnessMultiplier * colorTint.g);
            pixels[i].b = Mathf.Clamp01(pixels[i].b * brightnessMultiplier * colorTint.b);
        }

        // Create the new image with the transparent background intact
        Texture2D newTexture = new Texture2D(originalTexture.width, originalTexture.height, TextureFormat.RGBA32, false);
        newTexture.SetPixels(pixels);
        newTexture.Apply();

        byte[] pngData = newTexture.EncodeToPNG();
        
        // Save the file in the exact same folder as the original
        string folder = Path.GetDirectoryName(originalPath);
        string fileName = Path.GetFileNameWithoutExtension(originalPath) + "_Brightened.png";
        string newPath = Path.Combine(folder, fileName);

        File.WriteAllBytes(newPath, pngData);
        AssetDatabase.Refresh();

        // Lock the original image again so it doesn't impact game performance
        if (!wasReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        EditorUtility.DisplayDialog("Success", "New texture created!\nLook for: " + fileName + "\nJust drag it into your Material's Base Map.", "Perfect");
    }
}
#endif
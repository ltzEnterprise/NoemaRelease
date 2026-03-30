using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class LimpadorDivino : EditorWindow
{
    public string pastaAlvo = "Assets/";
    
    [Tooltip("Arraste as cenas que você quer verificar aqui.")]
    public List<SceneAsset> cenasParaVerificar = new List<SceneAsset>();
    
    [Tooltip("Arraste pastas ou arquivos específicos que o script NUNCA deve apagar.")]
    public List<Object> excecoes = new List<Object>();

    // Variaveis de serialização para fazer a lista bonitinha aparecer na janela
    private SerializedObject so;
    private SerializedProperty cenasProp;
    private SerializedProperty excecoesProp;

    [MenuItem("Tools/Noema/Limpador Divino Supremo")]
    public static void MostrarJanela()
    {
        GetWindow<LimpadorDivino>("Limpador Divino Supremo");
    }

    private void OnEnable()
    {
        so = new SerializedObject(this);
        cenasProp = so.FindProperty("cenasParaVerificar");
        excecoesProp = so.FindProperty("excecoes");
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("CUIDADO: O Julgamento Final dos Assets", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        pastaAlvo = EditorGUILayout.TextField("Pasta Alvo (Onde apagar):", pastaAlvo);
        GUILayout.Label("Ex: Assets/Models/Cenario", EditorStyles.miniLabel);
        
        GUILayout.Space(15);
        
        // Desenha as listas do Inspector dentro da nossa janelinha
        so.Update();
        EditorGUILayout.PropertyField(cenasProp, new GUIContent("Cenas para Verificar"), true);
        GUILayout.Space(5);
        EditorGUILayout.PropertyField(excecoesProp, new GUIContent("Exceções (Ignorar esses)"), true);
        so.ApplyModifiedProperties();

        GUILayout.Space(20);

        if (GUILayout.Button("Varrer e Apagar Lixo", GUILayout.Height(40)))
        {
            ExecutarLimpeza();
        }
    }

    void ExecutarLimpeza()
    {
        if (!AssetDatabase.IsValidFolder(pastaAlvo))
        {
            EditorUtility.DisplayDialog("Erro", $"A pasta alvo '{pastaAlvo}' não existe.", "OK");
            return;
        }

        if (cenasParaVerificar.Count == 0)
        {
            EditorUtility.DisplayDialog("Aviso", "Você precisa arrastar pelo menos uma cena na lista para verificar!", "OK");
            return;
        }

        // 1. Pega os caminhos de todas as cenas que você arrastou
        List<string> caminhosCenas = new List<string>();
        foreach (var cena in cenasParaVerificar)
        {
            if (cena != null) caminhosCenas.Add(AssetDatabase.GetAssetPath(cena));
        }

        // 2. A MÁGICA: Pega TODAS as dependências de todas as cenas da lista (Texturas, Mats, Prefabs)
        // O "true" faz ser recursivo (Se a cena tem um prefab, e o prefab tem uma textura, ele acha a textura).
        string[] dependenciasGlobais = AssetDatabase.GetDependencies(caminhosCenas.ToArray(), true);
        HashSet<string> usados = new HashSet<string>(dependenciasGlobais);

        // 3. Monta a lista de exceções
        List<string> caminhosIgnorados = new List<string>();
        foreach (var exc in excecoes)
        {
            if (exc != null) caminhosIgnorados.Add(AssetDatabase.GetAssetPath(exc));
        }

        // 4. Procura TUDO que tem dentro da pasta alvo
        string[] guidsNaPasta = AssetDatabase.FindAssets("", new[] { pastaAlvo });
        List<string> caminhosParaApagar = new List<string>();

        foreach (string guid in guidsNaPasta)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            if (AssetDatabase.IsValidFolder(path)) continue; // Não apaga pastas, só arquivos
            if (path == AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this))) continue; // Impede que o script cometa suicídio

            // Verifica se está na lista de exceções
            bool deveIgnorar = false;
            foreach (string ignorado in caminhosIgnorados)
            {
                // Se o caminho do arquivo começar com o nome da pasta ignorada, ele pula fora
                if (path.StartsWith(ignorado)) 
                {
                    deveIgnorar = true;
                    break;
                }
            }
            if (deveIgnorar) continue;

            // Se NÃO está nas dependências que as cenas usam, vai pra guilhotina
            if (!usados.Contains(path))
            {
                caminhosParaApagar.Add(path);
            }
        }

        // 5. O Julgamento Final
        if (caminhosParaApagar.Count == 0)
        {
            EditorUtility.DisplayDialog("Sucesso", "Essa pasta está limpa! Tudo nela está sendo usado pelas cenas que você escolheu ou foi ignorado.", "OK");
            return;
        }

        bool confirma = EditorUtility.DisplayDialog(
            "ALERTA DE DESTRUIÇÃO", 
            $"Encontrei {caminhosParaApagar.Count} arquivos inúteis na pasta '{pastaAlvo}'.\n\nEles irão para a Lixeira do Windows. Tem certeza?", 
            "APAGAR TUDO", 
            "Cancelar"
        );

        if (confirma)
        {
            int apagados = 0;
            foreach (string p in caminhosParaApagar)
            {
                if (AssetDatabase.MoveAssetToTrash(p)) 
                {
                    apagados++;
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"[Limpador Divino Supremo] Limpeza concluída! {apagados} arquivos foram pra lixeira.");
        }
    }
}
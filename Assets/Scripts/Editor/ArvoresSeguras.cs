using UnityEngine;
using UnityEditor;

public class ArvoresSeguras : EditorWindow
{
    [MenuItem("Tools/Arvores Seguras/1. Extrair (SILENCIOSO E COM COLLIDER CERTO)")]
    public static void Extrair()
    {
        Terrain[] terrenos = Terrain.activeTerrains;
        if (terrenos.Length == 0) return;

        GameObject master = GameObject.Find("Arvores_Extraidas_Seguras");
        if (master != null) return; // Se já extraiu, simplesmente não faz nada. Sem floodar o log.

        master = new GameObject("Arvores_Extraidas_Seguras");

        foreach (Terrain t in terrenos)
        {
            TerrainData data = t.terrainData;
            if (data == null || data.treeInstances.Length == 0) continue;

            GameObject terrainParent = new GameObject("Arvores_" + t.name);
            terrainParent.transform.parent = master.transform;

            foreach (TreeInstance arvore in data.treeInstances)
            {
                GameObject prefab = data.treePrototypes[arvore.prototypeIndex].prefab;
                if (prefab == null) continue;

                Vector3 pos = Vector3.Scale(arvore.position, data.size) + t.transform.position;
                GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                
                obj.transform.position = pos;
                obj.transform.rotation = Quaternion.AngleAxis(arvore.rotation * Mathf.Rad2Deg, Vector3.up);
                obj.transform.localScale = new Vector3(arvore.widthScale, arvore.heightScale, arvore.widthScale);
                obj.transform.parent = terrainParent.transform;
                
                obj.isStatic = true;

                LODGroup lod = obj.GetComponent<LODGroup>();
                if (lod != null) DestroyImmediate(lod);

                // --- FILTRO DE COLLIDER ---
                // Se o nome for de mato/arbusto pequeno, arranca a física. Se for árvore, mantém.
                string nomeBaixo = prefab.name.ToLower();
                if (nomeBaixo.Contains("grass") || nomeBaixo.Contains("mato") || 
                    nomeBaixo.Contains("bush") || nomeBaixo.Contains("fern") || 
                    nomeBaixo.Contains("plant") || nomeBaixo.Contains("flower") || 
                    nomeBaixo.Contains("weed") || nomeBaixo.Contains("shrub"))
                {
                    Collider[] colliders = obj.GetComponentsInChildren<Collider>();
                    foreach (Collider col in colliders)
                    {
                        DestroyImmediate(col);
                    }
                }
            }

            // Esconde as originais do terreno
            t.treeDistance = 0f; 
        }
    }

    [MenuItem("Tools/Arvores Seguras/2. Reverter (SILENCIOSO)")]
    public static void Reverter()
    {
        GameObject master = GameObject.Find("Arvores_Extraidas_Seguras");
        if (master != null)
        {
            DestroyImmediate(master);
        }

        Terrain[] terrenos = Terrain.activeTerrains;
        foreach (Terrain t in terrenos)
        {
            t.treeDistance = 2000f; // Volta a mostrar as originais
        }
    }
}
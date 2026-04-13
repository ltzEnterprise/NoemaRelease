using UnityEngine;
using TMPro;

[ExecuteAlways]
public class HandwrittenEffect : MonoBehaviour
{
    [Header("Distortion Settings")]
    public float angleStrength = 2.5f;
    public float posStrength = 1.0f;

    [Header("Ink Simulation")]
    [Range(0, 255)] public float inkWearAmount = 100f;

    private TMP_Text textComponent;

    void OnEnable()
    {
        textComponent = GetComponent<TMP_Text>();
        ApplyVisuals();
    }

    void OnValidate()
    {
        if (textComponent == null) textComponent = GetComponent<TMP_Text>();
        
        // Evita rodar no exato frame em que o objeto é desligado/destruído, o que causa crash
        if (!gameObject.activeInHierarchy) return;
        
        ApplyVisuals();
    }

    public void ApplyVisuals()
    {
        if (textComponent == null) return;

        textComponent.ForceMeshUpdate();
        var textInfo = textComponent.textInfo;

        if (textInfo == null || textInfo.meshInfo == null || textInfo.characterCount == 0) return;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            // Trava de segurança 1: Material inválido
            if (materialIndex < 0 || materialIndex >= textInfo.meshInfo.Length) continue;

            var meshInfo = textInfo.meshInfo[materialIndex];
            var vertices = meshInfo.vertices;
            var colors = meshInfo.colors32;

            // Trava de segurança 2: TMP não montou os arrays direito
            if (vertices == null || colors == null) continue;

            Vector3 center = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) / 2f;
            Random.InitState(i * 33); 
            
            float rndAngle = Random.Range(-angleStrength, angleStrength);
            Quaternion rotation = Quaternion.Euler(0, 0, rndAngle);
            Vector3 offset = new Vector3(0, Random.Range(-posStrength, posStrength), 0);
            byte alphaReduction = (byte)Random.Range(0, inkWearAmount);

            for (int j = 0; j < 4; j++)
            {
                int vIdx = vertexIndex + j;
                
                // Trava de Segurança ABSOLUTA: Testa o índice ANTES de aplicar qualquer matemática
                // Se o TMP bugou e a cor ou o vértice não existe, ele pula o loop na hora e não dá erro
                if (vIdx < 0 || vIdx >= vertices.Length || vIdx >= colors.Length) break;

                // Aplica distorção
                vertices[vIdx] = rotation * (vertices[vIdx] - center) + center + offset;

                // Aplica cor (tinta falhada)
                byte currentAlpha = colors[vIdx].a;
                colors[vIdx].a = currentAlpha > alphaReduction ? (byte)(currentAlpha - alphaReduction) : (byte)0;
            }
        }

        // Aplica as mudanças na malha
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            if (textInfo.meshInfo[i].mesh == null) continue;

            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textInfo.meshInfo[i].mesh.colors32 = textInfo.meshInfo[i].colors32;
            
            textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
using UnityEngine;
using TMPro;

[ExecuteAlways]
public class HandwrittenEffect : MonoBehaviour
{
    [Header("Distortion Settings")]
    [Tooltip("Maximum random rotation angle for each character")]
    public float angleStrength = 2.5f;
    [Tooltip("Maximum vertical offset for each character")]
    public float posStrength = 1.0f;

    [Header("Ink Simulation")]
    [Tooltip("How much the ink fades randomly (0 to 255). Higher = more worn out.")]
    [Range(0, 255)] public float inkWearAmount = 100f;

    private TMP_Text textComponent;

    void OnEnable()
    {
        textComponent = GetComponent<TMP_Text>();
        ApplyVisuals();
    }

    void OnValidate()
    {
        // Garante que pegamos o componente antes de tentar usar
        if (textComponent == null) textComponent = GetComponent<TMP_Text>();
        ApplyVisuals();
    }

    public void ApplyVisuals()
    {
        // 1. Verificações de Segurança Básica
        if (textComponent == null) return;

        // Força o TMP a atualizar a geometria antes de tentarmos mexer nela
        textComponent.ForceMeshUpdate();

        var textInfo = textComponent.textInfo;

        // 2. Verifica se existem dados de malha válidos
        if (textInfo == null || textInfo.meshInfo == null || textInfo.characterCount == 0) return;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            var charInfo = textInfo.characterInfo[i];

            // Pula caracteres invisíveis ou sem dados
            if (!charInfo.isVisible) continue;

            int vertexIndex = charInfo.vertexIndex;
            int materialIndex = charInfo.materialReferenceIndex;
            
            // 3. Verificação de Segurança de Array
            if (materialIndex >= textInfo.meshInfo.Length) continue;

            var vertices = textInfo.meshInfo[materialIndex].vertices;
            var colors = textInfo.meshInfo[materialIndex].colors32;

            // Se os arrays estiverem vazios por algum motivo, pula
            if (vertices == null || colors == null) continue;
            // Se o índice for maior que o tamanho do array, pula (evita crash)
            if (vertexIndex + 3 >= vertices.Length) continue;

            // --- Jitter Logic ---
            Vector3 center = (vertices[vertexIndex + 0] + vertices[vertexIndex + 2]) / 2;
            
            Random.InitState(i * 33); // Seed fixa para não tremer

            float rndAngle = Random.Range(-angleStrength, angleStrength);
            Quaternion rotation = Quaternion.Euler(0, 0, rndAngle);
            
            float rndOffsetY = Random.Range(-posStrength, posStrength);
            Vector3 offset = new Vector3(0, rndOffsetY, 0);

            // --- Ink Wear Logic ---
            byte alphaReduction = (byte)Random.Range(0, inkWearAmount);

            for (int j = 0; j < 4; j++)
            {
                // Aplica distorção
                Vector3 original = vertices[vertexIndex + j];
                vertices[vertexIndex + j] = rotation * (original - center) + center + offset;

                // Aplica cor (tinta falhada)
                byte currentAlpha = colors[vertexIndex + j].a;
                
                if (currentAlpha > alphaReduction)
                    colors[vertexIndex + j].a = (byte)(currentAlpha - alphaReduction);
                else
                    colors[vertexIndex + j].a = 0;
            }
        }

        // Aplica as mudanças na malha
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            var meshInfo = textInfo.meshInfo[i];
            if (meshInfo.mesh == null) continue;

            meshInfo.mesh.vertices = meshInfo.vertices;
            meshInfo.mesh.colors32 = meshInfo.colors32;
            
            textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
        }
    }
}
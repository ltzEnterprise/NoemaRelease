using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(MeshCollider))]
public class TowerGlitchTrigger : MonoBehaviour
{
    [Header("Shader e Renderer")]
    public Material glitchMaterial;
    public UniversalRendererData rendererData;
    public string featureName = "FullScreenPassRendererFeature";

    [Header("Configurações")]
    public float maxIntensity = 1.0f;
    [Tooltip("Raio do trigger (ajuste conforme o tamanho da torre)")]
    public float raioDaTorre = 10.0f;
    
    private ScriptableRendererFeature glitchFeature;
    private MeshCollider triggerCollider;
    private bool jogadorDentroFrameAtual = false;

    void Start()
    {
        triggerCollider = GetComponent<MeshCollider>();
        
        if (rendererData != null)
        {
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature.name == featureName)
                {
                    glitchFeature = feature;
                    break;
                }
            }
        }
        DesligarGlitch();
    }

    void Update()
    {
        if (!jogadorDentroFrameAtual)
        {
            DesligarGlitch();
        }
        jogadorDentroFrameAtual = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Pega as posições ignorando a altura (Y) pra não bugar com o pé do boneco
        Vector3 playerPos = other.transform.position;
        Vector3 centroTorre = triggerCollider.bounds.center;
        playerPos.y = 0;
        centroTorre.y = 0;

        // Calcula a distância real do jogador até o meio exato do cômodo
        float distanciaProCentro = Vector3.Distance(playerPos, centroTorre);

        // Se o jogador estiver a menos da metade do raio da torre (50% adentro), LIGA!
        if (distanciaProCentro < (raioDaTorre * 0.5f))
        {
            jogadorDentroFrameAtual = true;
            LigarGlitch();
        }
    }

    private void LigarGlitch()
    {
        if (glitchFeature != null && !glitchFeature.isActive) glitchFeature.SetActive(true);
        if (glitchMaterial != null && glitchMaterial.GetFloat("_Intensity") != maxIntensity)
             glitchMaterial.SetFloat("_Intensity", maxIntensity);
    }

    private void DesligarGlitch()
    {
        if (glitchMaterial != null && glitchMaterial.GetFloat("_Intensity") != 0f) 
            glitchMaterial.SetFloat("_Intensity", 0f);
            
        if (glitchFeature != null && glitchFeature.isActive) glitchFeature.SetActive(false);
    }

    void OnDisable()
    {
        DesligarGlitch();
    }
}
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class UVLightReceiver : MonoBehaviour
{
    [Header("--- GLOW SETTINGS ---")]
    [Tooltip("Color of the glow when hit by UV light")]
    [ColorUsage(true, true)] // Enables HDR
    public Color uvGlowColor = new Color(0.5f, 0f, 1f, 1f); 
    
    [Tooltip("Base intensity of the glow")]
    public float glowIntensity = 2f;

    [Header("--- UV FLASHLIGHT FINDER ---")]
    [Tooltip("The exact Tag of your UV Flashlight object")]
    public string flashlightTag = "UVLight"; 
    
    [Tooltip("Cone angle of the flashlight (e.g., 0.8)")]
    public float lightAngle = 0.8f;
    
    [Tooltip("Maximum range of the flashlight")]
    public float lightRange = 10f;

    // Internal variables
    private Transform uvFlashlight;
    private Renderer myRenderer;
    private MaterialPropertyBlock propertyBlock;
    private int emissionColorID;
    private float searchCooldown = 0f; // Pra não fritar a CPU procurando a lanterna

    void Awake()
    {
        // Puxa tudo no Awake pra ter certeza absoluta que tá pronto antes do Update
        myRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        emissionColorID = Shader.PropertyToID("_EmissionColor");
    }

    void Start()
    {
        // Usa sharedMaterial para NÃO CLONAR o material e evitar outro Out of Memory
        if (myRenderer != null && myRenderer.sharedMaterial != null)
        {
            if (myRenderer.sharedMaterial.HasProperty("_EmissionColor"))
            {
                myRenderer.sharedMaterial.EnableKeyword("_EMISSION");
            }
        }

        FindFlashlight();
    }

    void FindFlashlight()
    {
        GameObject lightObj = GameObject.FindGameObjectWithTag(flashlightTag);
        if (lightObj != null)
        {
            uvFlashlight = lightObj.transform;
        }
    }

    void Update()
    {
        if (myRenderer == null) return;

        // Se perdeu a lanterna (ou começou sem ela)
        if (uvFlashlight == null)
        {
            searchCooldown -= Time.deltaTime;
            if (searchCooldown <= 0f)
            {
                FindFlashlight();
                searchCooldown = 1f; // Tenta achar só 1x por segundo pra não matar o FPS
            }

            // Garante que o objeto fique apagado enquanto não achar a lanterna
            ApplyGlow(Color.black);
            return; 
        }

        // Checa se o objeto da lanterna (ou o pai dele) está ativo
        bool isFlashlightOn = uvFlashlight.gameObject.activeInHierarchy; 

        if (!isFlashlightOn)
        {
            ApplyGlow(Color.black);
            return;
        }

        Vector3 myPosition = myRenderer.bounds.center;
        Vector3 lightPosition = uvFlashlight.position;
        Vector3 lightDirection = uvFlashlight.forward.normalized;
        Vector3 dirToTarget = (myPosition - lightPosition);

        float distance = dirToTarget.magnitude;
        Vector3 dirToTargetNorm = dirToTarget.normalized;

        float dotProduct = Vector3.Dot(lightDirection, dirToTargetNorm);

        bool insideCone = dotProduct >= lightAngle;
        bool insideRange = distance <= lightRange;

        if (insideCone && insideRange)
        {
            float focusStrength = Mathf.SmoothStep(lightAngle, 1.0f, dotProduct);
            float distanceStrength = 1.0f - Mathf.SmoothStep(lightRange * 0.5f, lightRange, distance);
            
            float finalIntensity = focusStrength * distanceStrength * glowIntensity;

            ApplyGlow(uvGlowColor * finalIntensity);
        }
        else
        {
            ApplyGlow(Color.black);
        }
    }

    void ApplyGlow(Color color)
    {
        // O SALVA-VIDAS DEFINITIVO CONTRA O ERRO DE "dest cannot be null"
        if (propertyBlock == null) 
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        if (myRenderer != null)
        {
            myRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(emissionColorID, color);
            myRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
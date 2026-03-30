using UnityEngine;
using TMPro; // Necessário para o TextMeshPro

public class UVReveal : MonoBehaviour
{
    [Header("--- CONTROLE TOTAL ---")]
    [Tooltip("Só roda a lógica se isso estiver marcado. Pode ligar/desligar via código.")]
    public bool ativarEfeito = true; 

    [Header("Configuração UV")]
    public string tagDaLanterna = "LuzUV";
    [Tooltip("Distância máxima que a luz revela o texto. Igual ao _LightRange do Shader.")]
    public float lightRange = 10f;
    [Tooltip("Ângulo do cone de luz. 0.8 = mais fechado, 0 = 180 graus. Igual ao _LightAngle do Shader.")]
    [Range(0f, 1f)] public float lightAngle = 0.8f;

    [Header("Visual")]
    [ColorUsage(true, true)] 
    public Color corDaLuz = new Color(0.5f, 0f, 1f, 1f); 
    [Range(1f, 50f)] public float suavidade = 20f; 

    // Variáveis internas
    private GameObject lanternaUV;
    private float estadoAtual = 0f;
    
    // Suporte 3D Genérico
    private Renderer rend;
    private MaterialPropertyBlock propBlock;
    
    // Suporte específico para TextMeshPro
    private TMP_Text meuTexto;
    private Material tmpMaterial;
    private Color corOriginalTexto;
    
    // IDs do Shader (Para objetos normais)
    private static readonly int LightPosID = Shader.PropertyToID("_LightPosition");
    private static readonly int LightDirID = Shader.PropertyToID("_LightDirection");
    private static readonly int LightStateID = Shader.PropertyToID("_LightState");
    private static readonly int GlowColorID = Shader.PropertyToID("_GlowColor");
    private static readonly int LightAngleID = Shader.PropertyToID("_LightAngle");
    private static readonly int LightRangeID = Shader.PropertyToID("_LightRange");

    void Awake() 
    {
        // 1. CHECA PRIMEIRO SE É TEXTMESHPRO
        meuTexto = GetComponent<TMP_Text>();
        if (meuTexto != null)
        {
            tmpMaterial = meuTexto.fontMaterial; 
            corOriginalTexto = meuTexto.color; 
            
            // Força o Alpha inicial a 0 (Invisível)
            meuTexto.color = new Color(corOriginalTexto.r, corOriginalTexto.g, corOriginalTexto.b, 0f);
            return; 
        }

        // 2. SE NÃO FOR TEXTO, É UM RENDERER NORMAL USANDO O SHADER 'HiddenReveal'
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            propBlock = new MaterialPropertyBlock();
        }
    }

    void LateUpdate()
    {
        if (!ativarEfeito)
        {
            if (estadoAtual > 0.001f)
            {
                estadoAtual = 0f;
                AplicarDesligado();
            }
            return; 
        }

        if (lanternaUV == null)
        {
            lanternaUV = GameObject.FindGameObjectWithTag(tagDaLanterna);
            if (lanternaUV == null) return;
        }

        // O estado alvo principal é se a lanterna está ativada na mão
        bool estaLigada = lanternaUV.activeInHierarchy;
        float alvoGlobal = estaLigada ? 1f : 0f;
        
        estadoAtual = Mathf.Lerp(estadoAtual, alvoGlobal, Time.deltaTime * suavidade);

        if (estadoAtual < 0.001f)
        {
            if (estadoAtual > 0) AplicarDesligado();
            return;
        }

        Vector3 lightPos = lanternaUV.transform.position;
        Vector3 lightDir = lanternaUV.transform.forward;

        // SE FOR TEXTO, NÓS CALCULAMOS O SHADER NA MÃO (CPU)
        if (meuTexto != null)
        {
            float alphaCalculado = CalcularConeDeLuzMatematico(lightPos, lightDir);
            
            // Multiplica o resultado do cone pelo fade suave de ligar/desligar
            float alphaFinal = alphaCalculado * estadoAtual;
            
            meuTexto.color = new Color(corOriginalTexto.r, corOriginalTexto.g, corOriginalTexto.b, alphaFinal);
        }
        // SE FOR SHADER 'HiddenReveal', MANDA OS DADOS E DEIXA A GPU FAZER
        else if (rend != null)
        {
            rend.GetPropertyBlock(propBlock);
            propBlock.SetVector(LightPosID, lightPos);
            propBlock.SetVector(LightDirID, lightDir);
            propBlock.SetFloat(LightStateID, estadoAtual);
            propBlock.SetColor(GlowColorID, corDaLuz);
            
            // Garante que o script controle o range e angle pra tudo ficar sincronizado
            propBlock.SetFloat(LightAngleID, lightAngle); 
            propBlock.SetFloat(LightRangeID, lightRange); 
            
            rend.SetPropertyBlock(propBlock);
        }
    }

    void AplicarDesligado()
    {
        if (meuTexto != null)
        {
            meuTexto.color = new Color(corOriginalTexto.r, corOriginalTexto.g, corOriginalTexto.b, 0f);
        }
        else if (rend != null)
        {
            rend.GetPropertyBlock(propBlock);
            propBlock.SetFloat(LightStateID, 0f);
            rend.SetPropertyBlock(propBlock);
        }
    }

    // --- REPLICAÇÃO EXATA DA MATEMÁTICA DO SEU SHADER HIDDENREVEAL ---
    float CalcularConeDeLuzMatematico(Vector3 lightPos, Vector3 lightDir)
    {
        Vector3 textPos = transform.position;
        Vector3 dirToText = textPos - lightPos;
        
        float dist = dirToText.magnitude;
        
        // Se estiver fora do Range, retorna 0 (Invisível)
        if (dist > lightRange) return 0f;

        Vector3 dirToTextNorm = dirToText.normalized;
        Vector3 lightDirNorm = lightDir.normalized;

        // Produto Escalar (Dot Product)
        float dotProd = Vector3.Dot(lightDirNorm, dirToTextNorm);

        // Angle Mask (Smoothstep simplificado)
        // O Shader usa smoothstep(angle, angle + 0.1, dotProd)
        float angleMask = Mathf.Clamp01((dotProd - lightAngle) / 0.1f);

        // Dist Mask (Smoothstep simplificado)
        // O Shader usa 1.0 - smoothstep(range * 0.7, range, dist)
        float distEdge = lightRange * 0.7f;
        float distMask = 1.0f - Mathf.Clamp01((dist - distEdge) / (lightRange - distEdge));

        return angleMask * distMask;
    }
}
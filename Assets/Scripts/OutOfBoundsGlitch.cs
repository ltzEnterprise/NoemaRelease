using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Reflection;
using System.Collections;

public class OutOfBoundsGlitch : MonoBehaviour
{
    [Header("--- QUEM É O ALVO? ---")]
    [Tooltip("O nome do GameObject do jogador ou do Renderer que vai encostar aqui")]
    public string nomeDoJogador = "Player";

    [Header("--- OS 2 COLLIDERS (AIRLOCK) ---")]
    [Tooltip("O Collider do lado DENTRO do mapa (Seguro).")]
    public Collider zonaSegura;  
    [Tooltip("O Collider do lado FORA do mapa (Início do Glitch).")]
    public Collider zonaGlitch;  

    [Header("--- DISTÂNCIA E PUNIÇÃO ---")]
    [Tooltip("De qual objeto ele mede a distância? (Pode arrastar o próprio Collider da zona Glitch aqui)")]
    public Transform pontoBaseDaDistancia;
    [Tooltip("Quantos metros o jogador pode andar no vazio antes do jogo resetar ele?")]
    public float distanciaMaxima = 20f;
    [Tooltip("Pra onde ele é jogado quando a tela apagar?")]
    public Transform pontoDeTeleporte;

    [Header("--- SHADER E RENDERER ---")]
    public string featureName = "FullScreenPassRendererFeature";
    public Material materialGlitch;
    [Tooltip("O nome exato do parâmetro de intensidade dentro do seu Shader Graph (Ex: _Intensity, _Amount)")]
    public string parametroDoShader = "_Intensity";
    public float intensidadeMaximaShader = 5f;

    [Header("--- ÁUDIO E UI ---")]
    public AudioSource somGlitch;
    public float volumeMaximoAudio = 1f;
    
    [Tooltip("Um Canvas com imagem preta e um CanvasGroup pra fazer o fade")]
    public CanvasGroup telaPreta;
    public float tempoDeFade = 1f;

    // Estado de Controle Interno
    private float intensidadeOriginal;
    private bool jogadorTaNoGlitch = false;
    private Transform playerTransform;
    private bool estaTeleportando = false;
    
    // Referência da Feature encontrada automaticamente
    private ScriptableRendererFeature meuGlitchFeature;

    // Contadores da lógica genial do Airlock
    private int countSeguro = 0;
    private int countGlitch = 0;

    void Start()
    {
        // Salva a configuração original do seu Shader pra devolver depois
        if (materialGlitch != null && materialGlitch.HasProperty(parametroDoShader))
            intensidadeOriginal = materialGlitch.GetFloat(parametroDoShader);

        if (telaPreta != null) telaPreta.alpha = 0f;

        // BRUXARIA: Caça a feature só pelo nome, sem precisar arrastar o Renderer Data no Inspector
        UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset != null)
        {
            FieldInfo fieldInfo = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo != null)
            {
                ScriptableRendererData[] rendererDatas = (ScriptableRendererData[])fieldInfo.GetValue(urpAsset);
                
                // Pega o primeiro Renderer (O principal) e acha a feature
                if (rendererDatas != null && rendererDatas.Length > 0 && rendererDatas[0] != null)
                {
                    foreach (var feature in rendererDatas[0].rendererFeatures)
                    {
                        if (feature.name == featureName)
                        {
                            meuGlitchFeature = feature;
                            break;
                        }
                    }
                }
            }
        }

        if (meuGlitchFeature == null) 
            Debug.LogError($"[OutOfBoundsGlitch] Não achei a feature '{featureName}'! A Unity tá de sacanagem ou o nome tá errado.");
        
        DesativarEfeitoForte();

        // MÁGICA: Passa o nome que você digitou no Inspector direto pros ajudantes
        if (zonaSegura != null)
            zonaSegura.gameObject.AddComponent<GlitchTriggerHelper>().Setup(this, false, nomeDoJogador);
        if (zonaGlitch != null)
            zonaGlitch.gameObject.AddComponent<GlitchTriggerHelper>().Setup(this, true, nomeDoJogador);
    }

    // Essa função é chamada pelos colliders quando o alvo entra/sai deles
    public void AtualizarPresencaDoPlayer(bool ladoGlitch, bool entrando, Transform player)
    {
        if (estaTeleportando) return;
        playerTransform = player;

        if (ladoGlitch)
        {
            if (entrando) countGlitch++; else countGlitch--;
        }
        else
        {
            if (entrando) countSeguro++; else countSeguro--;
        }

        countGlitch = Mathf.Max(0, countGlitch);
        countSeguro = Mathf.Max(0, countSeguro);
        
        if (countGlitch > 0 && countSeguro == 0)
        {
            AtivarEfeito();
        }
        else if (countSeguro > 0 && countGlitch == 0)
        {
            DesativarEfeitoForte();
        }
    }

    void AtivarEfeito()
    {
        jogadorTaNoGlitch = true;
        if (meuGlitchFeature != null) meuGlitchFeature.SetActive(true);
        if (somGlitch != null && !somGlitch.isPlaying) somGlitch.Play();
    }

    void DesativarEfeitoForte()
    {
        jogadorTaNoGlitch = false;
        
        if (meuGlitchFeature != null) meuGlitchFeature.SetActive(false);
        
        if (materialGlitch != null && materialGlitch.HasProperty(parametroDoShader))
            materialGlitch.SetFloat(parametroDoShader, intensidadeOriginal);
            
        if (somGlitch != null) 
        { 
            somGlitch.volume = 0f; 
            somGlitch.Stop(); 
        }
    }

    void Update()
    {
        if (!jogadorTaNoGlitch || playerTransform == null || estaTeleportando || pontoBaseDaDistancia == null) return;

        float distanciaAtual = Vector3.Distance(playerTransform.position, pontoBaseDaDistancia.position);
        float porcentagem = Mathf.Clamp01(distanciaAtual / distanciaMaxima);

        if (materialGlitch != null && materialGlitch.HasProperty(parametroDoShader))
            materialGlitch.SetFloat(parametroDoShader, Mathf.Lerp(intensidadeOriginal, intensidadeMaximaShader, porcentagem));

        if (somGlitch != null)
            somGlitch.volume = Mathf.Lerp(0f, volumeMaximoAudio, porcentagem);

        if (porcentagem >= 1f)
        {
            StartCoroutine(PunicaoPorSairDoMapa());
        }
    }

    IEnumerator PunicaoPorSairDoMapa()
    {
        estaTeleportando = true;

        if (telaPreta != null)
        {
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime / (tempoDeFade / 2f);
                telaPreta.alpha = t;
                yield return null;
            }
        }

        if (FPS_Master.Instance != null && pontoDeTeleporte != null)
        {
            FPS_Master.Instance.Teleportar(pontoDeTeleporte.position);
        }

        countGlitch = 0;
        countSeguro = 0;
        DesativarEfeitoForte();

        yield return new WaitForSeconds(1f);

        if (telaPreta != null)
        {
            float t = 1f;
            while (t > 0f)
            {
                t -= Time.deltaTime / (tempoDeFade / 2f);
                telaPreta.alpha = t;
                yield return null;
            }
        }

        estaTeleportando = false;
    }
}

// =========================================================================
// SCRIPT AJUDANTE INVISÍVEL
// =========================================================================
public class GlitchTriggerHelper : MonoBehaviour
{
    private OutOfBoundsGlitch mestre;
    private bool souLadoGlitch;
    private string nomeAlvo;

    public void Setup(OutOfBoundsGlitch m, bool ladoGlitch, string nome)
    {
        mestre = m;
        souLadoGlitch = ladoGlitch;
        nomeAlvo = nome;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.name.Contains(nomeAlvo))
            mestre.AtualizarPresencaDoPlayer(souLadoGlitch, true, other.transform);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.name.Contains(nomeAlvo))
            mestre.AtualizarPresencaDoPlayer(souLadoGlitch, false, other.transform);
    }
}
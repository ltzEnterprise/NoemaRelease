using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class OutOfBoundsGlitch : MonoBehaviour
{
    [Header("--- TARGET ---")]
    public string targetPlayerName = "Player";

    [Header("--- AIRLOCK COLLIDERS ---")]
    public Collider safeZoneCollider;  
    public Collider glitchZoneCollider;  

    [Header("--- DISTANCE & PUNISHMENT ---")]
    public float maxDistance = 20f;
    public Transform teleportPoint;

    [Header("--- SHADER & RENDERER ---")]
    public Material glitchMaterial;
    public UniversalRendererData rendererData;
    public string featureName = "FullScreenPassRendererFeature";
    public string shaderParameter = "_Intensity";
    public float maxShaderIntensity = 5f;

    [Header("--- AUDIO & UI ---")]
    public AudioSource glitchSound;
    public float maxAudioVolume = 1f;
    public CanvasGroup blackScreenCanvas;
    public float fadeTime = 2.0f;

    private bool isPlayerInGlitch = false;
    private Transform playerTransform;
    private bool isTeleporting = false;
    
    private ScriptableRendererFeature myGlitchFeature;
    private int safeCount = 0;
    private int glitchCount = 0;

    private Vector3 entryPoint; 

    void Start()
    {
        if (blackScreenCanvas != null) 
        {
            blackScreenCanvas.alpha = 0f;
            blackScreenCanvas.gameObject.SetActive(false); 
        }

        if (glitchSound != null) glitchSound.loop = true; 

        if (rendererData != null)
        {
            foreach (var feature in rendererData.rendererFeatures)
            {
                if (feature.name == featureName)
                {
                    myGlitchFeature = feature;
                    break;
                }
            }
        }

        ForcarDesligamentoTotal();

        if (safeZoneCollider != null)
            safeZoneCollider.gameObject.AddComponent<GlitchTriggerHelper>().Setup(this, false, targetPlayerName);
        if (glitchZoneCollider != null)
            glitchZoneCollider.gameObject.AddComponent<GlitchTriggerHelper>().Setup(this, true, targetPlayerName);
    }

    public void UpdatePlayerPresence(bool isGlitchSide, bool isEntering, Transform player)
    {
        if (isTeleporting) return;
        playerTransform = player;

        if (isGlitchSide)
        {
            if (isEntering) glitchCount++; else glitchCount--;
        }
        else
        {
            if (isEntering) safeCount++; else safeCount--;
        }

        glitchCount = Mathf.Max(0, glitchCount);
        safeCount = Mathf.Max(0, safeCount);
        
        if (glitchCount > 0 && safeCount == 0)
        {
            if (!isPlayerInGlitch) 
            {
                // SALVA EXATAMENTE DE ONDE COMEÇOU PRA SER 100% GRADUAL
                entryPoint = playerTransform.position;
            }
            EnableEffect();
        }
        else if (safeCount > 0 && glitchCount == 0)
        {
            DisableHeavyEffect();
        }
    }

    void EnableEffect()
    {
        isPlayerInGlitch = true;
        
        if (myGlitchFeature != null && !myGlitchFeature.isActive) 
            myGlitchFeature.SetActive(true);
        
        if (glitchSound != null && !glitchSound.isPlaying) 
        {
            glitchSound.volume = 0f;
            glitchSound.Play();
        }
    }

    void DisableHeavyEffect()
    {
        isPlayerInGlitch = false;
        ForcarDesligamentoTotal();
    }

    void LateUpdate()
    {
        if (!isPlayerInGlitch || playerTransform == null || isTeleporting) return;

        // Bate de frente com a Torre pra forçar a Feature ativada
        if (myGlitchFeature != null && !myGlitchFeature.isActive)
        {
            myGlitchFeature.SetActive(true);
        }

        float currentDistance = Vector3.Distance(playerTransform.position, entryPoint);
        float percentage = Mathf.Clamp01(currentDistance / maxDistance);

        float currentIntensity = Mathf.Lerp(0f, maxShaderIntensity, percentage);

        if (glitchMaterial != null)
            glitchMaterial.SetFloat(shaderParameter, currentIntensity);

        if (glitchSound != null)
            glitchSound.volume = Mathf.Lerp(0f, maxAudioVolume, percentage);

        if (percentage >= 1f)
        {
            StartCoroutine(PunishPlayerRoutine());
        }
    }

    IEnumerator PunishPlayerRoutine()
    {
        isTeleporting = true;

        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(true, false);

        if (blackScreenCanvas != null)
        {
            blackScreenCanvas.gameObject.SetActive(true);
            float t = 0;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                blackScreenCanvas.alpha = Mathf.Lerp(0f, 1f, t / fadeTime);
                yield return null;
            }
            blackScreenCanvas.alpha = 1f;
        }

        if (FPS_Master.Instance != null && teleportPoint != null)
        {
            FPS_Master.Instance.Teleportar(teleportPoint.position);
        }

        glitchCount = 0;
        safeCount = 0;
        DisableHeavyEffect();

        yield return new WaitForSeconds(1.0f);

        if (blackScreenCanvas != null)
        {
            float t = 0;
            while (t < fadeTime)
            {
                t += Time.deltaTime;
                blackScreenCanvas.alpha = Mathf.Lerp(1f, 0f, t / fadeTime);
                yield return null;
            }
            blackScreenCanvas.alpha = 0f;
            blackScreenCanvas.gameObject.SetActive(false);
        }

        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(false, false);

        isTeleporting = false;
    }

    private void ForcarDesligamentoTotal()
    {
        if (glitchMaterial != null)
            glitchMaterial.SetFloat(shaderParameter, 0f);
            
        if (myGlitchFeature != null && myGlitchFeature.isActive) 
            myGlitchFeature.SetActive(false);

        if (glitchSound != null) 
        { 
            glitchSound.volume = 0f; 
            glitchSound.Stop(); 
        }
    }

    void OnDisable() { ForcarDesligamentoTotal(); }
    void OnDestroy() { ForcarDesligamentoTotal(); }
    void OnApplicationQuit() { ForcarDesligamentoTotal(); }
}

public class GlitchTriggerHelper : MonoBehaviour
{
    private OutOfBoundsGlitch masterScript;
    private bool amIGlitchSide;
    private string targetName;

    public void Setup(OutOfBoundsGlitch m, bool isGlitchSide, string name)
    {
        masterScript = m;
        amIGlitchSide = isGlitchSide;
        targetName = name;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.name.Contains(targetName))
            masterScript.UpdatePlayerPresence(amIGlitchSide, true, other.transform);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.name.Contains(targetName))
            masterScript.UpdatePlayerPresence(amIGlitchSide, false, other.transform);
    }
}
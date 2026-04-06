using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class AltarActivationCutscene : MonoBehaviour
{
    [Header("--- GENERAL REFERENCES ---")]
    public WeaponAltar altarScript; 
    public Camera playerMainCamera; 

    [Header("--- CUTSCENE CAMERAS ---")]
    public Camera cam1Recoil;
    public Camera cam2Sky;
    public Camera cam3CloseAltar;

    [Header("--- AUDIO ---")]
    public AudioSource cutsceneMusicSource; 
    public AudioSource sfxSkySource;       
    public AudioSource sfxCloseWeaponSource; 

    [Header("--- TIMING (Shot Durations) ---")]
    public float shot1Duration = 4f;
    public float shot2Duration = 5f; 
    public float shot3Duration = 3f;
    
    [Header("--- SKY CONFIG ---")]
    public float skyTransitionDuration = 2.5f; 
    public float targetExposure = 1.27f; 

    [Header("--- MUSIC FADE CONFIG ---")]
    public float fadeOutDuration = 2.5f; 

    private Material originalSkybox; // Guarda o arquivo original intacto
    private Material instancedSkybox; // O clone que a gente vai modificar
    private float initialExposure;
    private bool isCam1Recoiling = false;
    private float originalMusicVolume; 

    void Start()
    {
        if (cam1Recoil) cam1Recoil.gameObject.SetActive(false);
        if (cam2Sky) cam2Sky.gameObject.SetActive(false);
        if (cam3CloseAltar) cam3CloseAltar.gameObject.SetActive(false);

        if (cutsceneMusicSource) originalMusicVolume = cutsceneMusicSource.volume;
    }

    void Update()
    {
        if (isCam1Recoiling && cam1Recoil != null)
        {
            cam1Recoil.transform.Translate(Vector3.back * 0.5f * Time.deltaTime, Space.Self);
        }
    }

    public void IniciarCutscene() 
    {
        StartCoroutine(CutsceneSequence());
    }

    IEnumerator CutsceneSequence()
    {
        // === PREPARATION ===
        if (cutsceneMusicSource) 
        {
            cutsceneMusicSource.volume = originalMusicVolume; 
            cutsceneMusicSource.Play();
        }

        // TRAVA O PLAYER E DEIXA ELE INVISÍVEL
        if (FPS_Master.Instance != null) 
        {
            FPS_Master.Instance.AlterarEstadoJogador(true, false);
            FPS_Master.Instance.FicarInvisivelMasFisico(true); 
        }

        if (playerMainCamera) playerMainCamera.gameObject.SetActive(false);

        if (altarScript != null && altarScript.visualWeapon != null)
            altarScript.visualWeapon.SetActive(true);
        
        // A MÁGICA PRA NÃO ESTRAGAR SEU ARQUIVO DO CÉU:
        originalSkybox = RenderSettings.skybox;
        if (originalSkybox != null)
        {
            // Cria uma cópia temporária do material
            instancedSkybox = new Material(originalSkybox);
            RenderSettings.skybox = instancedSkybox; // Bota a cópia no céu
            
            initialExposure = (instancedSkybox.HasProperty("_Exposure")) ? instancedSkybox.GetFloat("_Exposure") : 1f;
        }

        // === SHOT 1: Recoil ===
        if (cam1Recoil) cam1Recoil.gameObject.SetActive(true);
        isCam1Recoiling = true;
        yield return new WaitForSeconds(shot1Duration);
        isCam1Recoiling = false;
        if (cam1Recoil) cam1Recoil.gameObject.SetActive(false);

        // === SHOT 2: Sky ===
        if (cam2Sky) cam2Sky.gameObject.SetActive(true);
        if (sfxSkySource) sfxSkySource.Play(); 

        float skyTimer = 0f;
        
        while (skyTimer < shot2Duration)
        {
            skyTimer += Time.deltaTime;
            
            float lightProgress = Mathf.Clamp01(skyTimer / skyTransitionDuration);
            float newExposure = Mathf.Lerp(initialExposure, targetExposure, lightProgress);
            
            if (instancedSkybox && instancedSkybox.HasProperty("_Exposure"))
            {
                instancedSkybox.SetFloat("_Exposure", newExposure);
                DynamicGI.UpdateEnvironment();
            }
            yield return null; 
        }
        
        if (instancedSkybox && instancedSkybox.HasProperty("_Exposure")) 
            instancedSkybox.SetFloat("_Exposure", targetExposure);

        if (cam2Sky) cam2Sky.gameObject.SetActive(false);

        // === SHOT 3: Close Up ===
        if (cam3CloseAltar) cam3CloseAltar.gameObject.SetActive(true);
        if (sfxCloseWeaponSource) sfxCloseWeaponSource.Play(); 

        StartCoroutine(MusicFadeOut());

        yield return new WaitForSeconds(shot3Duration);

        if (cam3CloseAltar) cam3CloseAltar.gameObject.SetActive(false);

        // === FINISH ===
        if (playerMainCamera) playerMainCamera.gameObject.SetActive(true);
        
        if (FPS_Master.Instance != null) 
        {
            FPS_Master.Instance.FicarInvisivelMasFisico(false); 
            FPS_Master.Instance.AlterarEstadoJogador(false, false);
        }

        if (altarScript != null)
            altarScript.FinalizeActivation(); 
    }

    IEnumerator MusicFadeOut()
    {
        if (cutsceneMusicSource == null) yield break;

        float timeElapsed = 0f;
        float startVol = cutsceneMusicSource.volume;

        while (timeElapsed < fadeOutDuration)
        {
            timeElapsed += Time.deltaTime;
            cutsceneMusicSource.volume = Mathf.Lerp(startVol, 0f, timeElapsed / fadeOutDuration);
            yield return null; 
        }

        cutsceneMusicSource.volume = 0f;
        cutsceneMusicSource.Stop();
        cutsceneMusicSource.volume = originalMusicVolume;
    }

    // ISSO AQUI PROTEGE O SEU JOGO! Se você der Stop ou a cena recarregar, ele reseta o céu.
    void OnDestroy()
    {
        if (originalSkybox != null)
        {
            RenderSettings.skybox = originalSkybox;
            DynamicGI.UpdateEnvironment();
        }
        
        if (instancedSkybox != null)
        {
            Destroy(instancedSkybox); // Joga a cópia fora pra não vazar memória
        }
    }
}
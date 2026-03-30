using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class AltarActivationCutscene : MonoBehaviour
{
    [Header("--- GENERAL REFERENCES ---")]
    public WeaponAltar altarScript; // Mudou de AltarDaArma para WeaponAltar
    public GameObject playerObjectRyo; 
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
    public float shot2Duration = 5f; // Sky shot duration
    public float shot3Duration = 3f;
    
    [Header("--- SKY CONFIG ---")]
    [Tooltip("How long for the sky color to transition?")]
    public float skyTransitionDuration = 2.5f; 
    public float targetExposure = 1.27f; 

    [Header("--- MUSIC FADE CONFIG ---")]
    public float fadeOutDuration = 2.5f; 

    // Internal Variables
    private Material skyboxMaterial;
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

    public void IniciarCutscene() // Mantive o nome público caso o WeaponAltar chame assim
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

        if (playerObjectRyo) playerObjectRyo.SetActive(false);
        if (playerMainCamera) playerMainCamera.gameObject.SetActive(false);

        // Atualizado para usar as referências em INGLÊS do WeaponAltar
        if (altarScript != null && altarScript.visualWeapon != null)
            altarScript.visualWeapon.SetActive(true);
        
        skyboxMaterial = RenderSettings.skybox;
        initialExposure = (skyboxMaterial.HasProperty("_Exposure")) ? skyboxMaterial.GetFloat("_Exposure") : 1f;

        // === SHOT 1: Recoil ===
        cam1Recoil.gameObject.SetActive(true);
        isCam1Recoiling = true;
        yield return new WaitForSeconds(shot1Duration);
        isCam1Recoiling = false;
        cam1Recoil.gameObject.SetActive(false);

        // === SHOT 2: Sky ===
        cam2Sky.gameObject.SetActive(true);
        if (sfxSkySource) sfxSkySource.Play(); 

        float skyTimer = 0f;
        
        // Loop based on SCENE DURATION
        while (skyTimer < shot2Duration)
        {
            skyTimer += Time.deltaTime;
            
            // Calc light based on TRANSITION DURATION
            float lightProgress = Mathf.Clamp01(skyTimer / skyTransitionDuration);
            
            float newExposure = Mathf.Lerp(initialExposure, targetExposure, lightProgress);
            
            if (skyboxMaterial)
            {
                skyboxMaterial.SetFloat("_Exposure", newExposure);
                DynamicGI.UpdateEnvironment();
            }
            yield return null; 
        }
        
        if (skyboxMaterial) skyboxMaterial.SetFloat("_Exposure", targetExposure);

        cam2Sky.gameObject.SetActive(false);

        // === SHOT 3: Close Up ===
        cam3CloseAltar.gameObject.SetActive(true);
        if (sfxCloseWeaponSource) sfxCloseWeaponSource.Play(); 

        StartCoroutine(MusicFadeOut());

        yield return new WaitForSeconds(shot3Duration);

        cam3CloseAltar.gameObject.SetActive(false);

        // === FINISH ===
        if (playerObjectRyo) playerObjectRyo.SetActive(true);
        if (playerMainCamera) playerMainCamera.gameObject.SetActive(true);

        if (altarScript != null)
            altarScript.FinalizeActivation(); // Método atualizado para Inglês
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
}
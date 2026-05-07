using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class AltarActivationCutscene : MonoBehaviour
{
    [Header("--- SAVE SYSTEM ---")]
    [Tooltip("ID único desta cutscene. Ex: Cutscene_AltarArma_01")]
    public string uniqueID = "Cutscene_AltarArma_01";
    public bool executarApenasUmaVez = true;

    [Header("--- GENERAL REFERENCES ---")]
    public WeaponAltar altarScript; 
    public Camera playerMainCamera; 

    [Header("--- UI ---")]
    [Tooltip("Arraste aqui a imagem da retícula/crosshair para ela sumir durante a cutscene.")]
    public GameObject reticula;

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

    private Material originalSkybox;
    private Material instancedSkybox;
    private float initialExposure;
    private bool isCam1Recoiling = false;
    private float originalMusicVolume; 
    private bool cutsceneJaExecutada = false;
    private bool emCutscene = false;
    private bool reticulaEstadoAnterior = true;

    void Start()
    {
        if (cam1Recoil) cam1Recoil.gameObject.SetActive(false);
        if (cam2Sky) cam2Sky.gameObject.SetActive(false);
        if (cam3CloseAltar) cam3CloseAltar.gameObject.SetActive(false);

        if (cutsceneMusicSource) originalMusicVolume = cutsceneMusicSource.volume;

        StartCoroutine(CarregarEstadoSeguro());
    }

    IEnumerator CarregarEstadoSeguro()
    {
        if (PersistenciaManager.Instance != null)
        {
            yield return new WaitUntil(() =>
                PersistenciaManager.Instance.DadosProntosParaUso &&
                !PersistenciaManager.Instance.EstaCarregando
            );

            if (!string.IsNullOrEmpty(uniqueID))
                cutsceneJaExecutada = PersistenciaManager.Instance.ObterEstado(uniqueID + "_Visto", false);
        }
    }

    void Update()
    {
        if (isCam1Recoiling && cam1Recoil != null)
            cam1Recoil.transform.Translate(Vector3.back * 0.5f * Time.deltaTime, Space.Self);
    }

    public void IniciarCutscene() 
    {
        if (emCutscene) return;

        if (executarApenasUmaVez && cutsceneJaExecutada)
        {
            Debug.Log($"[AltarActivationCutscene] Cutscene '{uniqueID}' já foi vista. Finalizando altar sem repetir câmera.");

            if (altarScript != null)
                altarScript.FinalizeActivation();

            return;
        }

        SalvarCutsceneIniciada();
        StartCoroutine(CutsceneSequence());
    }

    private void SalvarCutsceneIniciada()
    {
        if (PersistenciaManager.Instance == null) return;

        if (!string.IsNullOrEmpty(uniqueID))
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_Iniciada", true);

        PersistenciaManager.Instance.SalvarTudo(true);
    }

    IEnumerator CutsceneSequence()
    {
        emCutscene = true;

        EsconderReticula();

        if (cutsceneMusicSource) 
        {
            cutsceneMusicSource.volume = originalMusicVolume; 
            cutsceneMusicSource.Play();
        }

        if (FPS_Master.Instance != null) 
        {
            FPS_Master.Instance.AlterarEstadoJogador(true, false);
            FPS_Master.Instance.FicarInvisivelMasFisico(true); 
        }

        if (playerMainCamera)
            playerMainCamera.gameObject.SetActive(false);

        if (altarScript != null && altarScript.visualWeapon != null)
            altarScript.visualWeapon.SetActive(true);

        originalSkybox = RenderSettings.skybox;

        if (originalSkybox != null)
        {
            instancedSkybox = new Material(originalSkybox);
            RenderSettings.skybox = instancedSkybox;
            initialExposure = instancedSkybox.HasProperty("_Exposure") ? instancedSkybox.GetFloat("_Exposure") : 1f;
        }

        if (cam1Recoil)
            cam1Recoil.gameObject.SetActive(true);

        isCam1Recoiling = true;
        yield return new WaitForSeconds(shot1Duration);
        isCam1Recoiling = false;

        if (cam1Recoil)
            cam1Recoil.gameObject.SetActive(false);

        if (cam2Sky)
            cam2Sky.gameObject.SetActive(true);

        if (sfxSkySource)
            sfxSkySource.Play(); 

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

        if (cam2Sky)
            cam2Sky.gameObject.SetActive(false);

        if (cam3CloseAltar)
            cam3CloseAltar.gameObject.SetActive(true);

        if (sfxCloseWeaponSource)
            sfxCloseWeaponSource.Play(); 

        StartCoroutine(MusicFadeOut());

        yield return new WaitForSeconds(shot3Duration);

        if (cam3CloseAltar)
            cam3CloseAltar.gameObject.SetActive(false);

        if (playerMainCamera)
            playerMainCamera.gameObject.SetActive(true);

        if (FPS_Master.Instance != null) 
        {
            FPS_Master.Instance.FicarInvisivelMasFisico(false); 
            FPS_Master.Instance.AlterarEstadoJogador(false, false);
        }

        RestaurarReticula();

        SalvarCutsceneVista();

        emCutscene = false;

        if (altarScript != null)
            altarScript.FinalizeActivation(); 
    }

    IEnumerator MusicFadeOut()
    {
        if (cutsceneMusicSource == null)
            yield break;

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

    private void EsconderReticula()
    {
        if (reticula == null) return;

        reticulaEstadoAnterior = reticula.activeSelf;
        reticula.SetActive(false);
    }

    private void RestaurarReticula()
    {
        if (reticula == null) return;

        reticula.SetActive(reticulaEstadoAnterior);
    }

    private void SalvarCutsceneVista()
    {
        cutsceneJaExecutada = true;

        if (!string.IsNullOrEmpty(uniqueID) && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_Iniciada", true);
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_Visto", true);
            PersistenciaManager.Instance.SalvarTudo(true);
        }
    }

    void OnDestroy()
    {
        if (originalSkybox != null)
        {
            RenderSettings.skybox = originalSkybox;
            DynamicGI.UpdateEnvironment();
        }

        if (instancedSkybox != null)
            Destroy(instancedSkybox);

        if (emCutscene)
        {
            if (cam1Recoil)
                cam1Recoil.gameObject.SetActive(false);

            if (cam2Sky)
                cam2Sky.gameObject.SetActive(false);

            if (cam3CloseAltar)
                cam3CloseAltar.gameObject.SetActive(false);

            if (playerMainCamera)
                playerMainCamera.gameObject.SetActive(true);

            if (FPS_Master.Instance != null) 
            {
                FPS_Master.Instance.FicarInvisivelMasFisico(false); 
                FPS_Master.Instance.AlterarEstadoJogador(false, false);
            }

            RestaurarReticula();
        }
    }
}
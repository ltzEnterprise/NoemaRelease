using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

public class AltarActivationCutscene : MonoBehaviour
{
    [Header("--- SAVE SYSTEM ---")]
    public string uniqueID = "Cutscene_AltarArma_01";
    public bool executarApenasUmaVez = true;

    [Header("--- GENERAL REFERENCES ---")]
    public WeaponAltar altarScript;
    public Camera playerMainCamera;

    [Header("--- UI ---")]
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
    private bool ceuFinalAplicado = false;
    private bool emCutscene = false;
    private bool reticulaEstadoAnterior = true;

    private string ChaveCutsceneVista { get { return uniqueID + "_Visto"; } }
    private string ChaveCutsceneIniciada { get { return uniqueID + "_Iniciada"; } }
    private string ChaveCeuFinalAplicado { get { return uniqueID + "_CeuFinalAplicado"; } }

    void Start()
    {
        if (cam1Recoil) cam1Recoil.gameObject.SetActive(false);
        if (cam2Sky) cam2Sky.gameObject.SetActive(false);
        if (cam3CloseAltar) cam3CloseAltar.gameObject.SetActive(false);

        if (cutsceneMusicSource)
            originalMusicVolume = cutsceneMusicSource.volume;

        StartCoroutine(CarregarEstadoSeguro());
    }

    IEnumerator CarregarEstadoSeguro()
    {
        yield return new WaitUntil(() =>
            PersistenciaManager.Instance != null &&
            PersistenciaManager.Instance.DadosProntosParaUso &&
            !PersistenciaManager.Instance.EstaCarregando
        );

        if (!string.IsNullOrEmpty(uniqueID))
        {
            cutsceneJaExecutada = PersistenciaManager.Instance.ObterEstado(ChaveCutsceneVista, false);
            ceuFinalAplicado = PersistenciaManager.Instance.ObterEstado(ChaveCeuFinalAplicado, false);

            if (cutsceneJaExecutada || ceuFinalAplicado)
            {
                AplicarCeuFinalDaCutscene();
                SalvarCeuFinalAplicado();
            }
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
            AplicarCeuFinalDaCutscene();
            SalvarCeuFinalAplicado();

            if (altarScript != null)
                altarScript.FinalizeActivation();

            StartCoroutine(ReaplicarCeuFinalDepoisDoAltar());

            return;
        }

        SalvarCutsceneIniciada();
        StartCoroutine(CutsceneSequence());
    }

    private void SalvarCutsceneIniciada()
    {
        if (PersistenciaManager.Instance == null) return;

        if (!string.IsNullOrEmpty(uniqueID))
            PersistenciaManager.Instance.RegistrarEstado(ChaveCutsceneIniciada, true);

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

        PrepararSkyboxInstanciado();

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

        AplicarCeuFinalDaCutscene();
        SalvarCeuFinalAplicado();

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

        StartCoroutine(ReaplicarCeuFinalDepoisDoAltar());
    }

    private IEnumerator ReaplicarCeuFinalDepoisDoAltar()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        AplicarCeuFinalDaCutscene();
        SalvarCeuFinalAplicado();

        yield return new WaitForSeconds(0.1f);

        AplicarCeuFinalDaCutscene();
    }

    private void PrepararSkyboxInstanciado()
    {
        if (instancedSkybox != null)
        {
            RenderSettings.skybox = instancedSkybox;
            initialExposure = instancedSkybox.HasProperty("_Exposure") ? instancedSkybox.GetFloat("_Exposure") : 1f;
            return;
        }

        originalSkybox = RenderSettings.skybox;

        if (originalSkybox != null)
        {
            instancedSkybox = new Material(originalSkybox);
            RenderSettings.skybox = instancedSkybox;
            initialExposure = instancedSkybox.HasProperty("_Exposure") ? instancedSkybox.GetFloat("_Exposure") : 1f;
        }
    }

    public void AplicarCeuFinalDaCutscene()
    {
        PrepararSkyboxInstanciado();

        if (instancedSkybox && instancedSkybox.HasProperty("_Exposure"))
        {
            instancedSkybox.SetFloat("_Exposure", targetExposure);
            RenderSettings.skybox = instancedSkybox;
            DynamicGI.UpdateEnvironment();
        }

        ceuFinalAplicado = true;
    }

    private void SalvarCeuFinalAplicado()
    {
        ceuFinalAplicado = true;

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            PersistenciaManager.Instance.RegistrarEstado(ChaveCeuFinalAplicado, true);
            PersistenciaManager.Instance.SalvarTudo(true);
        }
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
        ceuFinalAplicado = true;

        if (!string.IsNullOrEmpty(uniqueID) && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(ChaveCutsceneIniciada, true);
            PersistenciaManager.Instance.RegistrarEstado(ChaveCutsceneVista, true);
            PersistenciaManager.Instance.RegistrarEstado(ChaveCeuFinalAplicado, true);
            PersistenciaManager.Instance.SalvarTudo(true);
        }
    }

    void OnDestroy()
    {
        if (!ceuFinalAplicado && originalSkybox != null)
        {
            RenderSettings.skybox = originalSkybox;
            DynamicGI.UpdateEnvironment();
        }

        if (instancedSkybox != null && !ceuFinalAplicado)
            Destroy(instancedSkybox);

        if (emCutscene)
        {
            if (cam1Recoil) cam1Recoil.gameObject.SetActive(false);
            if (cam2Sky) cam2Sky.gameObject.SetActive(false);
            if (cam3CloseAltar) cam3CloseAltar.gameObject.SetActive(false);

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
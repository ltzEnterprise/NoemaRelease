using UnityEngine;
using UnityEngine.Rendering;

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;

    public enum TimeState { InitialDay, DramaticDay, Night }

    [Header("--- DEV MODE ---")]
    public TimeState startWith = TimeState.InitialDay;

    [Header("--- ROTAÇÃO (Sun/Moon) ---")]
    public Vector3 sunAngle = new Vector3(50f, -30f, 0f);
    public Vector3 sunAngleInitial = new Vector3(20f, -30f, 0f); 
    public Vector3 moonAngle = new Vector3(20f, -100f, 0f);

    [Header("--- GLOBAL REFERENCES ---")]
    public Light mainLight;  
    public Volume globalVolume; 
    
    [Header("--- LENS FLARES ---")]
    public LensFlareDataSRP flareInitial;
    public LensFlareDataSRP flareDramatic;
    public LensFlareDataSRP flareNight;
    private LensFlareComponentSRP flareComponent;

    [System.Serializable]
    public class LightingProfile 
    {
        public Material skybox;
        public VolumeProfile volumeProfile;
        public Color lightColor = Color.white;
        public float lightIntensity = 1.0f;
        public Color fogColor = Color.grey;
        public float fogDensity = 0.005f;
        public GameObject objectsGroup;
    }

    [Header("--- PERFIS DE ILUMINAÇÃO ---")]
    public LightingProfile initialDayProfile;
    public LightingProfile dramaticDayProfile;
    public LightingProfile nightProfile;

    [Header("--- GAMEPLAY ---")]
    public bool isNight = false; 
    
    private TimeState currentState;

    void Awake() 
    { 
        if (Instance == null) Instance = this; 
        else Destroy(gameObject); 
    }

    void Start()
    {
        if (mainLight != null)
            flareComponent = mainLight.GetComponent<LensFlareComponentSRP>();

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;

        ChangeTo(startWith);
    }

    public void ChangeTo(TimeState newState)
    {
        currentState = newState;
        ApplyProfile(newState);
    }

    void Update()
    {
        UpdateRotation();

#if UNITY_EDITOR
        ApplyColorsRealtime();
#endif
    }

    void UpdateRotation()
    {
        if (mainLight == null) return;

        Quaternion targetRot = Quaternion.identity;

        switch (currentState)
        {
            case TimeState.InitialDay: targetRot = Quaternion.Euler(sunAngleInitial); break;
            case TimeState.DramaticDay: targetRot = Quaternion.Euler(sunAngle); break;
            case TimeState.Night: targetRot = Quaternion.Euler(moonAngle); break;
        }

        mainLight.transform.rotation = targetRot;
    }

    void ApplyProfile(TimeState state)
    {
        isNight = (state == TimeState.Night);
        LightingProfile p = null;

        switch (state)
        {
            case TimeState.InitialDay: p = initialDayProfile; break;
            case TimeState.DramaticDay: p = dramaticDayProfile; break;
            case TimeState.Night: p = nightProfile; break;
        }

        if (p == null) return;

        ToggleGroups(p.objectsGroup);

        if (p.skybox != null) RenderSettings.skybox = p.skybox;
        if (globalVolume != null && p.volumeProfile != null) globalVolume.profile = p.volumeProfile;

        if (mainLight != null)
        {
            mainLight.shadows = LightShadows.Soft;
            if (flareComponent != null) 
            {
                if (state == TimeState.InitialDay) flareComponent.lensFlareData = flareInitial;
                else if (state == TimeState.DramaticDay) flareComponent.lensFlareData = flareDramatic;
                else flareComponent.lensFlareData = flareNight;
            }
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = isNight ? new Color(0.25f, 0.25f, 0.35f) : Color.gray;
        
        RenderSettings.fogColor = p.fogColor;
        RenderSettings.fogDensity = p.fogDensity;
        if (mainLight) 
        {
            mainLight.color = p.lightColor;
            mainLight.intensity = p.lightIntensity;
        }

        DynamicGI.UpdateEnvironment();
    }

    void ApplyColorsRealtime()
    {
        LightingProfile p = null;
        switch (currentState)
        {
            case TimeState.InitialDay: p = initialDayProfile; break;
            case TimeState.DramaticDay: p = dramaticDayProfile; break;
            case TimeState.Night: p = nightProfile; break;
        }

        if (p != null)
        {
            RenderSettings.fogColor = p.fogColor;
            RenderSettings.fogDensity = p.fogDensity;
            if (mainLight) 
            {
                mainLight.color = p.lightColor;
                mainLight.intensity = p.lightIntensity;
            }
        }
    }

    void ToggleGroups(GameObject activeGroup)
    {
        if (initialDayProfile.objectsGroup) initialDayProfile.objectsGroup.SetActive(initialDayProfile.objectsGroup == activeGroup);
        if (dramaticDayProfile.objectsGroup) dramaticDayProfile.objectsGroup.SetActive(dramaticDayProfile.objectsGroup == activeGroup);
        if (nightProfile.objectsGroup) nightProfile.objectsGroup.SetActive(nightProfile.objectsGroup == activeGroup);
    }

    [ContextMenu("Apply Initial Day")] public void TestInitial() => ChangeTo(TimeState.InitialDay);
    [ContextMenu("Apply Dramatic Day")] public void TestDramatic() => ChangeTo(TimeState.DramaticDay);
    [ContextMenu("Apply Night")] public void TestNight() => ChangeTo(TimeState.Night);
}
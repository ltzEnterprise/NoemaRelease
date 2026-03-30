using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI; 
using System.Collections.Generic;
using TMPro;
using QuantumTek.SimpleMenu;

public class SettingsManager : MonoBehaviour
{
    [Header("--- REFERÊNCIAS ---")]
    public AudioMixer mixerPrincipal; 
    public Camera cameraDoJogador; 
    
    [Header("--- CONTROLE DE ABAS ---")]
    public SM_TabGroup controladorDeAbas; 
    public SM_TabWindow janelaGameplay; 

    [Header("--- UI DROPDOWNS ---")]
    public TMP_Dropdown dropdownResolucao;
    public TMP_Dropdown dropdownQualidade;
    public TMP_Dropdown dropdownIdioma; 

    [Header("--- UI SLIDERS (FORÇA POSIÇÃO) ---")]
    public Slider sliderMaster;
    public Slider sliderMusic;
    public Slider sliderSFX;
    public Slider sliderFOV;
    public Slider sliderSensibilidade;

    [Header("--- UI TEXTOS DOS NÚMEROS ---")]
    public TextMeshProUGUI textMaster;
    public TextMeshProUGUI textMusic;
    public TextMeshProUGUI textSFX;
    public TextMeshProUGUI textFOV;
    public TextMeshProUGUI textSensibilidade;

    private struct ResData {
        public int w, h, hz;
        public ResData(int width, int height, int refreshRate) { w = width; h = height; hz = refreshRate; }
    }
    private List<ResData> listaManual = new List<ResData>();

    private void OnEnable()
    {
        if (controladorDeAbas != null && janelaGameplay != null) 
            controladorDeAbas.ChangeTab(janelaGameplay);
    }

    private void Start()
    {
        if (!PlayerPrefs.HasKey("ConfiguracoesCriadas")) SetDefaultSettings();

        ConfigurarListaDeResolucoesManual();
        ConfigurarListaDeQualidade();
        ConfigurarListaDeIdioma();
        
        LoadAndApplyAllSettings();
    }

    private void SetDefaultSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", 1f);
        PlayerPrefs.SetFloat("MusicVolume", 1f);
        PlayerPrefs.SetFloat("SFXVolume", 1f);
        PlayerPrefs.SetFloat("PlayerFOV", 70f);
        PlayerPrefs.SetFloat("MouseSensitivity", 2.06f);
        PlayerPrefs.SetInt("QualityLevel", 2); 
        PlayerPrefs.SetInt("VSync", 0); 
        PlayerPrefs.SetInt("Idioma", 1); 
        PlayerPrefs.SetInt("DificuldadeJogo", 0); 
        PlayerPrefs.SetInt("Resolution", 9); 
        PlayerPrefs.SetInt("ConfiguracoesCriadas", 1); 
        PlayerPrefs.Save();
    }
    public void RestaurarPadroes()
    {
        SetDefaultSettings();
        LoadAndApplyAllSettings(); 
        Debug.Log("Configurações repostas para o padrão!");
    }

    private void ConfigurarListaDeResolucoesManual()
    {
        if (dropdownResolucao == null) return;
        dropdownResolucao.ClearOptions();
        listaManual.Clear();
        int[] widths = { 1280, 1366, 1600, 1920, 2560, 3840 };
        int[] heights = { 720, 768, 900, 1080, 1440, 2160 };
        int[] rates = { 60, 120, 144 }; 
        List<string> opcoesDeTexto = new List<string>();
        for (int i = 0; i < widths.Length; i++) {
            foreach (int r in rates) {
                string resStr = widths[i] + "x" + heights[i] + " (" + r + "Hz)";
                opcoesDeTexto.Add(resStr);
                listaManual.Add(new ResData(widths[i], heights[i], r));
            }
        }
        dropdownResolucao.AddOptions(opcoesDeTexto);
        int savedRes = PlayerPrefs.GetInt("Resolution", 9); 
        savedRes = Mathf.Clamp(savedRes, 0, listaManual.Count - 1);
        dropdownResolucao.value = savedRes;
        dropdownResolucao.RefreshShownValue();
    }

    private void ConfigurarListaDeQualidade()
    {
        if (dropdownQualidade == null) return;
        dropdownQualidade.value = PlayerPrefs.GetInt("QualityLevel", 2);
        dropdownQualidade.RefreshShownValue();
    }

    private void ConfigurarListaDeIdioma()
    {
        if (dropdownIdioma == null) return;
        dropdownIdioma.ClearOptions();
        dropdownIdioma.AddOptions(new List<string> { "Português", "English" });
        dropdownIdioma.value = PlayerPrefs.GetInt("Idioma", 0);
        dropdownIdioma.RefreshShownValue();
    }

    public void LoadAndApplyAllSettings()
    {
        float vMaster = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float vMusic = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float vSFX = PlayerPrefs.GetFloat("SFXVolume", 1f);
        float vFOV = PlayerPrefs.GetFloat("PlayerFOV", 60f);
        float vSens = PlayerPrefs.GetFloat("MouseSensitivity", 2f);

        if (sliderMaster) sliderMaster.value = vMaster;
        if (sliderMusic) sliderMusic.value = vMusic;
        if (sliderSFX) sliderSFX.value = vSFX;
        if (sliderFOV) sliderFOV.value = vFOV;
        if (sliderSensibilidade) sliderSensibilidade.value = vSens;

        ApplyMasterVolume(vMaster);
        ApplyMusicVolume(vMusic);
        ApplySFXVolume(vSFX);
        ApplyFOV(vFOV);
        ApplySensitivity(vSens);

        ApplyQuality(PlayerPrefs.GetInt("QualityLevel", 2));
        ApplyVSync(PlayerPrefs.GetInt("VSync", 0) == 1);
        ApplyIdioma(PlayerPrefs.GetInt("Idioma", 0));

        int resIndex = PlayerPrefs.GetInt("Resolution", -1);
        if (resIndex != -1) ApplyResolution(resIndex);
    }

    // --- MÉTODOS DE APLICAÇÃO (SEM O %) ---

    public void ApplySensitivity(float value)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();
        if (textSensibilidade) 
        {
            int display = Mathf.RoundToInt(Mathf.InverseLerp(0.1f, 5.0f, value) * 100f);
            textSensibilidade.text = display.ToString();
        }
        if (FPS_Master.Instance != null) FPS_Master.Instance.CarregarConfiguracoes();
    }

    public void ApplyFOV(float value)
    {
        PlayerPrefs.SetFloat("PlayerFOV", value);
        PlayerPrefs.Save();
        if (textFOV) textFOV.text = Mathf.RoundToInt(value).ToString();
        if (cameraDoJogador != null) cameraDoJogador.fieldOfView = value;
        if (FPS_Master.Instance != null) FPS_Master.Instance.CarregarConfiguracoes();
    }

    public void ApplyMasterVolume(float value) 
    { 
        if(mixerPrincipal) mixerPrincipal.SetFloat("VolGeral", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f); 
        PlayerPrefs.SetFloat("MasterVolume", value); 
        PlayerPrefs.Save(); 
        if (textMaster) textMaster.text = Mathf.RoundToInt(value * 100f).ToString();
    }

    public void ApplyMusicVolume(float value) 
    { 
        if(mixerPrincipal) mixerPrincipal.SetFloat("VolMusica", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f); 
        PlayerPrefs.SetFloat("MusicVolume", value); 
        PlayerPrefs.Save(); 
        if (textMusic) textMusic.text = Mathf.RoundToInt(value * 100f).ToString();
    }

    public void ApplySFXVolume(float value) 
    { 
        if(mixerPrincipal) mixerPrincipal.SetFloat("VolEfeitos", Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f); 
        PlayerPrefs.SetFloat("SFXVolume", value); 
        PlayerPrefs.Save(); 
        if (textSFX) textSFX.text = Mathf.RoundToInt(value * 100f).ToString();
    }

    public void ApplyResolution(int index)
    {
        if (index >= 0 && index < listaManual.Count)
        {
            ResData res = listaManual[index];
            Screen.SetResolution(res.w, res.h, FullScreenMode.FullScreenWindow, new RefreshRate() { numerator = (uint)res.hz, denominator = 1 });
            Application.targetFrameRate = res.hz; 
            PlayerPrefs.SetInt("Resolution", index);
            PlayerPrefs.Save();
        }
    }

    public void ApplyIdioma(int index) { PlayerPrefs.SetInt("Idioma", index); PlayerPrefs.Save(); }
    public void ApplyQuality(int qualityIndex) { QualitySettings.SetQualityLevel(qualityIndex); PlayerPrefs.SetInt("QualityLevel", qualityIndex); PlayerPrefs.Save(); }
    public void ApplyDifficulty(int difficultyIndex)
    {
        PlayerPrefs.SetInt("DificuldadeJogo", difficultyIndex);
        PlayerPrefs.Save();
        Debug.Log("Dificuldade alterada para: " + (difficultyIndex == 0 ? "Easily" : "Normal"));
    }
    public void ApplyVSync(bool isVsyncOn) { QualitySettings.vSyncCount = isVsyncOn ? 1 : 0; PlayerPrefs.SetInt("VSync", isVsyncOn ? 1 : 0); PlayerPrefs.Save(); }
}
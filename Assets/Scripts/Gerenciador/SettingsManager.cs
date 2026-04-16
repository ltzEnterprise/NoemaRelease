using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI; 
using System.Collections.Generic;
using TMPro;
using QuantumTek.SimpleMenu;
using System.Reflection; 

public class SettingsManager : MonoBehaviour
{
    [Header("--- REFERÊNCIAS ---")]
    public AudioMixer mixerPrincipal; 
    public Camera cameraDoJogador; 
    public SM_TabGroup controladorDeAbas; 
    public SM_TabWindow janelaGameplay; 

    [Header("--- VELOCIDADE DO JOGADOR ---")]
    public FPS_Master playerNaCena; 
    public SM_OptionList listaVelocidade; 
    public float andarLento = 3f;
    public float correrLento = 6f;
    public float andarNormal = 5f;
    public float correrNormal = 10f;
    public float andarRapido = 7f;
    public float correrRapido = 14f;

    [Header("--- UI DROPDOWNS E OPTION LIST ---")]
    public TMP_Dropdown dropdownResolucao;
    public TMP_Dropdown dropdownQualidade;
    public TMP_Dropdown dropdownIdioma;
    public SM_OptionList listaDificuldade; 

    [Header("--- UI SLIDERS ---")]
    public Slider sliderMaster;
    public Slider sliderMusic;
    public Slider sliderSFX;
    public Slider sliderFOV;
    public Slider sliderSensibilidade;

    [Header("--- UI TEXTOS ---")]
    public TextMeshProUGUI textMaster;
    public TextMeshProUGUI textMusic;
    public TextMeshProUGUI textSFX;
    public TextMeshProUGUI textFOV;
    public TextMeshProUGUI textSensibilidade;

    [Header("--- TRADUÇÕES DOS MENUS GERADOS ---")]
    public List<string> qualidades_PT = new List<string> { "Baixo", "Médio", "Alto" };
    public List<string> qualidades_EN = new List<string> { "Low", "Medium", "High" };

    public List<string> dificuldades_PT = new List<string> { "Fácil", "Normal" };
    public List<string> dificuldades_EN = new List<string> { "Easy", "Normal" };

    public List<string> velocidades_PT = new List<string> { "Lento", "Normal", "Rápido" };
    public List<string> velocidades_EN = new List<string> { "Slow", "Normal", "Fast" };

    public List<string> idiomas_PT = new List<string> { "Português", "Inglês" };
    public List<string> idiomas_EN = new List<string> { "Portuguese", "English" };

    private struct ResData {
        public int w, h, hz;
        public ResData(int width, int height, int refreshRate) { w = width; h = height; hz = refreshRate; }
    }
    private List<ResData> listaManual = new List<ResData>();

    private bool isUpdatingUI = false;

    private void Start()
    {
        if (!PlayerPrefs.HasKey("ConfiguracoesCriadas")) SetDefaultSettings();
        ConfigurarListaDeResolucoesManual();
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
        PlayerPrefs.SetInt("Idioma", 0); 
        PlayerPrefs.SetInt("DificuldadeJogo", 1); 
        PlayerPrefs.SetInt("PlayerSpeed", 1);
        PlayerPrefs.SetInt("Resolution", 9); 
        PlayerPrefs.SetInt("ConfiguracoesCriadas", 1); 
        PlayerPrefs.Save();
    }

    public void ResetarConfiguracoes()
    {
        SetDefaultSettings(); 
        LoadAndApplyAllSettings();
    }

    public void LoadAndApplyAllSettings()
    {
        float vMaster = PlayerPrefs.GetFloat("MasterVolume", 1f);
        float vMusic = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float vSFX = PlayerPrefs.GetFloat("SFXVolume", 1f);
        float vFOV = PlayerPrefs.GetFloat("PlayerFOV", 70f);
        float vSens = PlayerPrefs.GetFloat("MouseSensitivity", 2.06f);
        
        int vQualidade = PlayerPrefs.GetInt("QualityLevel", 2);
        int vIdioma = PlayerPrefs.GetInt("Idioma", 0);
        int vDificuldade = PlayerPrefs.GetInt("DificuldadeJogo", 1);
        int vVelocidade = PlayerPrefs.GetInt("PlayerSpeed", 1);
        int vResolucao = PlayerPrefs.GetInt("Resolution", 9);

        if (sliderMaster) sliderMaster.SetValueWithoutNotify(vMaster);
        if (sliderMusic) sliderMusic.SetValueWithoutNotify(vMusic);
        if (sliderSFX) sliderSFX.SetValueWithoutNotify(vSFX);
        if (sliderFOV) sliderFOV.SetValueWithoutNotify(vFOV);
        if (sliderSensibilidade) sliderSensibilidade.SetValueWithoutNotify(vSens);

        if (dropdownQualidade) dropdownQualidade.SetValueWithoutNotify(vQualidade);
        if (dropdownIdioma) dropdownIdioma.SetValueWithoutNotify(vIdioma);
        if (dropdownResolucao) dropdownResolucao.SetValueWithoutNotify(Mathf.Clamp(vResolucao, 0, listaManual.Count > 0 ? listaManual.Count - 1 : 0));

        ApplyMasterVolume(vMaster);
        ApplyMusicVolume(vMusic);
        ApplySFXVolume(vSFX);
        ApplyFOV(vFOV);
        ApplySensitivity(vSens);
        ApplyQuality(vQualidade);
        ApplyVSync(PlayerPrefs.GetInt("VSync", 0) == 1);
        ApplyIdioma(vIdioma); 
        ApplyVelocidadeInterna(vVelocidade);

        isUpdatingUI = true;
        if (listaDificuldade) { listaDificuldade.current = vDificuldade; listaDificuldade.SetOption(vDificuldade); }
        if (listaVelocidade) { listaVelocidade.current = vVelocidade; listaVelocidade.SetOption(vVelocidade); }
        isUpdatingUI = false;

        if (vResolucao >= 0 && vResolucao < listaManual.Count) ApplyResolution(vResolucao);
    }

    public void AtualizarTextosDinamicos(int lang)
    {
        if (dropdownIdioma != null)
        {
            int val = dropdownIdioma.value;
            dropdownIdioma.ClearOptions();
            dropdownIdioma.AddOptions(lang == 0 ? idiomas_PT : idiomas_EN);
            dropdownIdioma.SetValueWithoutNotify(val);
            dropdownIdioma.RefreshShownValue();
        }

        if (dropdownQualidade != null)
        {
            int val = dropdownQualidade.value;
            dropdownQualidade.ClearOptions();
            dropdownQualidade.AddOptions(lang == 0 ? qualidades_PT : qualidades_EN);
            dropdownQualidade.SetValueWithoutNotify(val);
            dropdownQualidade.RefreshShownValue();
        }

        ActualizarOpcoesLista(listaDificuldade, lang == 0 ? dificuldades_PT : dificuldades_EN);
        ActualizarOpcoesLista(listaVelocidade, lang == 0 ? velocidades_PT : velocidades_EN);
    }

    private void ActualizarOpcoesLista(SM_OptionList list, List<string> novasOpcoes)
    {
        if (list == null) return;
        
        FieldInfo field = typeof(SM_OptionList).GetField("options", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
        {
            List<string> listaInterna = (List<string>)field.GetValue(list);
            listaInterna.Clear();
            listaInterna.AddRange(novasOpcoes);
            list.SetOption(list.current); 
        }
    }

    public void ApplyIdioma(int index) 
    { 
        if (isUpdatingUI) return;

        PlayerPrefs.SetInt("Idioma", index); 
        
        if (LanguageManager.Instance != null)
        {
            LanguageManager.Instance.ChangeLanguage(index);
        }

        isUpdatingUI = true;
        AtualizarTextosDinamicos(index);
        isUpdatingUI = false;
    }

    public void ApplySensitivity(float value)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        if (textSensibilidade) 
        {
            float display = Mathf.InverseLerp(0.1f, 5.0f, value) * 100f;
            textSensibilidade.text = Mathf.RoundToInt(display).ToString();
        }
        if (FPS_Master.Instance != null) FPS_Master.Instance.CarregarConfiguracoes();
    }

    public void ApplyFOV(float value)
    {
        PlayerPrefs.SetFloat("PlayerFOV", value);
        if (textFOV) textFOV.text = Mathf.RoundToInt(value).ToString();
        if (cameraDoJogador != null) cameraDoJogador.fieldOfView = value;
        if (FPS_Master.Instance != null) FPS_Master.Instance.CarregarConfiguracoes();
    }

    public void ApplyMasterVolume(float value) 
    { 
        float db = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f;
        if(mixerPrincipal) mixerPrincipal.SetFloat("VolGeral", db); 
        PlayerPrefs.SetFloat("MasterVolume", value); 
        if (textMaster) textMaster.text = Mathf.RoundToInt(value * 100f).ToString();
    }

    public void ApplyMusicVolume(float value) 
    { 
        float db = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f;
        if(mixerPrincipal) mixerPrincipal.SetFloat("VolMusica", db); 
        PlayerPrefs.SetFloat("MusicVolume", value); 
        if (textMusic) textMusic.text = Mathf.RoundToInt(value * 100f).ToString();
    }

    public void ApplySFXVolume(float value) 
    { 
        float db = Mathf.Log10(Mathf.Max(0.0001f, value)) * 20f;
        if(mixerPrincipal) mixerPrincipal.SetFloat("VolEfeitos", db); 
        PlayerPrefs.SetFloat("SFXVolume", value); 
        if (textSFX) textSFX.text = Mathf.RoundToInt(value * 100f).ToString();
    }

    public void SaveSettings() { PlayerPrefs.Save(); }
    private void OnDisable() { PlayerPrefs.Save(); }

    private void ConfigurarListaDeResolucoesManual()
    {
        if (dropdownResolucao == null) return;
        dropdownResolucao.ClearOptions();
        listaManual.Clear();
        
        int[] widths = { 1280, 1366, 1600, 1920, 2560, 3840 };
        int[] heights = { 720, 768, 900, 1080, 1440, 2160 };
        int[] rates = { 60, 120, 144 }; 
        
        List<string> opcoesDeTexto = new List<string>();
        for (int i = 0; i < widths.Length; i++) 
        {
            foreach (int r in rates) 
            {
                opcoesDeTexto.Add($"{widths[i]}x{heights[i]} ({r}Hz)");
                listaManual.Add(new ResData(widths[i], heights[i], r));
            }
        }
        dropdownResolucao.AddOptions(opcoesDeTexto);
        dropdownResolucao.RefreshShownValue();
    }

    public void ApplyResolution(int index)
    {
        if (index >= 0 && index < listaManual.Count)
        {
            ResData res = listaManual[index];
            
            // 🔥 A MÁGICA ANTI-GLITCH AQUI:
            // A Unity só aplica a resolução se ela DE FATO for diferente da do monitor atual.
            // Isso impede que a tela pisque preto toda vez que você entra por uma porta!
            if (Screen.width != res.w || Screen.height != res.h || Screen.currentResolution.refreshRateRatio.value != res.hz)
            {
                Screen.SetResolution(res.w, res.h, FullScreenMode.FullScreenWindow, new RefreshRate() { numerator = (uint)res.hz, denominator = 1 });
            }
            
            Application.targetFrameRate = res.hz; 
            PlayerPrefs.SetInt("Resolution", index);
        }
    }
    
    public void ApplyQuality(int qualityIndex) 
    { 
        int indexInvertido = Mathf.Max(0, (QualitySettings.names.Length - 1) - qualityIndex);
        QualitySettings.SetQualityLevel(indexInvertido); 
        PlayerPrefs.SetInt("QualityLevel", qualityIndex); 
    }

    public void ApplyVSync(bool isVsyncOn) 
    { 
        QualitySettings.vSyncCount = isVsyncOn ? 1 : 0; 
        PlayerPrefs.SetInt("VSync", isVsyncOn ? 1 : 0); 
    }
    
    public void ApplyDificuldade() 
    { 
        if (isUpdatingUI) return;

        if (listaDificuldade != null)
        {
            PlayerPrefs.SetInt("DificuldadeJogo", listaDificuldade.current); 
        }
    }

    public void ApplyVelocidadeDoBotao() 
    { 
        if (isUpdatingUI) return;

        if (listaVelocidade != null)
        {
            int speedIndex = listaVelocidade.current;
            PlayerPrefs.SetInt("PlayerSpeed", speedIndex); 
            ApplyVelocidadeInterna(speedIndex);
        }
    }

    private void ApplyVelocidadeInterna(int speedIndex)
    {
        FPS_Master player = playerNaCena;
        if (player == null) player = FPS_Master.Instance;
        if (player == null) player = FindAnyObjectByType<FPS_Master>();

        if (player != null)
        {
            if (speedIndex == 0) 
            {
                player.velocidadeAndar = andarLento;
                player.velocidadeCorrer = correrLento;
            }
            else if (speedIndex == 1) 
            {
                player.velocidadeAndar = andarNormal;
                player.velocidadeCorrer = correrNormal;
            }
            else if (speedIndex == 2) 
            {
                player.velocidadeAndar = andarRapido;
                player.velocidadeCorrer = correrRapido;
            }
        }
    }
}
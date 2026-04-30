using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class ManagerMundo2D : MonoBehaviour
{
    public static ManagerMundo2D Instance;

    [Header("Configuração")]
    public bool usarSpawnDeTeste = false;
    public int spawnInicialTeste = 1;
    public int indiceUltimaFase = 9;
    public float tempoTotalFaseImpossivel = 60f;
    public string nomeCenaPrincipal = "CenaPrincipal3D";

    [Header("Referências")]
    public Transform player2D;
    public Transform[] spawnPoints;
    public GameObject telaAzulBSOD;
    public GameObject hudChaveUI;
    public FlashlightSystem2D sistemaLanterna;

    [Header("Visual")]
    public Image imagemIrisUI; 

    [Header("--- TEMPOS SEPARADOS DA IRIS ---")]
    [Tooltip("Tempo da animação quando aperta R para resetar a fase atual.")]
    public float tempoTransicaoResetR = 0.35f;

    [Tooltip("Tempo da animação quando passa para a próxima fase.")]
    public float tempoTransicaoTrocaFase = 0.6f;

    private Material materialIrisInstancia;

    [Header("Áudio Sources")]
    public AudioSource musicSource; 
    public AudioSource sfxSource;

    [Header("Músicas e Volumes")]
    public AudioClip musicaFases0a4;
    [Range(0f, 1f)] public float volumeFases0a4 = 0.5f; 
    
    public AudioClip musicaFases5a8;
    [Range(0f, 1f)] public float volumeFases5a8 = 0.5f; 
    
    public AudioClip musicaFase9;
    [Range(0f, 1f)] public float volumeFase9 = 0.5f;    

    [Header("Sons e Efeitos")]
    public AudioClip somVitoria;
    public AudioClip somTelaAzul; 
    public AudioClip somResetAgua; 
    
    [Header("Configuração do Fade")]
    public float tempoFadeMusica = 1.5f;

    public int currentLevel = 0; 
    private bool isTransitioning = false;
    private bool jogoCrashou = false;
    private bool pegouAChaveFinal = false;
    private float cronometroAtual;
    private bool save2DInicializado = false;

    private static GameObject musicaPersistenteObj;
    private static AudioSource musicaPersistente;
    private static Coroutine fadeMusicaAtual;

    private const string KEY_ABRIR_IRIS = "AbrirIrisNoStart";
    private const string KEY_TEMPO_IRIS = "TempoAbrirIris2D";
    private const string KEY_NIVEL_PENDENTE = "NivelAtual2D_Pendente";

    void Awake()
    {
        Instance = this;

        PrepararMusicaPersistente();

        if (musicSource != null)
            musicSource.loop = true;
    }

    private void PrepararMusicaPersistente()
    {
        AudioSource sourceDaCena = musicSource;

        if (musicaPersistente == null)
        {
            musicaPersistenteObj = new GameObject("Mundo2D_MusicaPersistente");
            DontDestroyOnLoad(musicaPersistenteObj);

            musicaPersistente = musicaPersistenteObj.AddComponent<AudioSource>();
            musicaPersistente.loop = true;
            musicaPersistente.playOnAwake = false;
            musicaPersistente.spatialBlend = 0f;

            if (sourceDaCena != null)
            {
                musicaPersistente.outputAudioMixerGroup = sourceDaCena.outputAudioMixerGroup;
            }
        }
        else
        {
            if (sourceDaCena != null &&
                sourceDaCena.outputAudioMixerGroup != null &&
                musicaPersistente.outputAudioMixerGroup == null)
            {
                musicaPersistente.outputAudioMixerGroup = sourceDaCena.outputAudioMixerGroup;
            }
        }

        if (sourceDaCena != null && sourceDaCena != musicaPersistente)
            sourceDaCena.Stop();

        musicSource = musicaPersistente;
    }

    IEnumerator Start()
    {
        if (imagemIrisUI != null)
        {
            materialIrisInstancia = Instantiate(imagemIrisUI.material);
            imagemIrisUI.material = materialIrisInstancia;
            materialIrisInstancia.SetFloat("_Radius", 1.5f); 
            imagemIrisUI.gameObject.SetActive(false); 
        }

        if (telaAzulBSOD) telaAzulBSOD.SetActive(false);
        if (hudChaveUI) hudChaveUI.SetActive(false);

        if (PersistenciaManager.Instance != null)
        {
            yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso && !PersistenciaManager.Instance.EstaCarregando);
        }

        if (PlayerPrefs.HasKey(KEY_NIVEL_PENDENTE))
        {
            currentLevel = PlayerPrefs.GetInt(KEY_NIVEL_PENDENTE, 0);
            PlayerPrefs.DeleteKey(KEY_NIVEL_PENDENTE);
            PlayerPrefs.Save();
        }
        else if (usarSpawnDeTeste) 
        {
            currentLevel = spawnInicialTeste;

            if (PersistenciaManager.Instance != null)
            {
                PersistenciaManager.Instance.RegistrarEstado("Fase9_PegouChave", false);
                PersistenciaManager.Instance.SalvarFloat("TempoRestante_Fase9", tempoTotalFaseImpossivel);
            }
        }
        else 
        {
            if (PersistenciaManager.Instance != null)
                currentLevel = PersistenciaManager.Instance.ObterInt("NivelAtual_2D", 0);
            else
                currentLevel = 0;
        }

        PosicionarPlayer();
        VerificarMusicaDoNivel();

        if (sistemaLanterna != null) sistemaLanterna.AtivarSistema(currentLevel > 0);

        if (currentLevel >= indiceUltimaFase)
        {
            if (!usarSpawnDeTeste && PersistenciaManager.Instance != null && PersistenciaManager.Instance.ObterEstado("Fase9_PegouChave"))
            {
                pegouAChaveFinal = true;
                if (hudChaveUI) hudChaveUI.SetActive(true);
                
                float tempoSalvo = PersistenciaManager.Instance.ObterFloat("TempoRestante_Fase9", tempoTotalFaseImpossivel);
                StartCoroutine(TimerTelaAzulDaMorte(tempoSalvo));
            }
        }

        if (PlayerPrefs.GetInt(KEY_ABRIR_IRIS, 0) == 1)
        {
            float tempoAbrir = PlayerPrefs.GetFloat(KEY_TEMPO_IRIS, tempoTransicaoTrocaFase);

            PlayerPrefs.SetInt(KEY_ABRIR_IRIS, 0);
            PlayerPrefs.DeleteKey(KEY_TEMPO_IRIS);
            PlayerPrefs.Save();
            
            if (imagemIrisUI) 
            {
                imagemIrisUI.gameObject.SetActive(true);
                if (materialIrisInstancia != null) materialIrisInstancia.SetFloat("_Radius", 0f); 
            }

            StartCoroutine(RotinaChegadaNaFaseNova(tempoAbrir));
        }

        save2DInicializado = true;
    }

    public void TocarSomResetAgua()
    {
        if (somResetAgua != null && sfxSource != null) sfxSource.PlayOneShot(somResetAgua);
    }

    IEnumerator RotinaChegadaNaFaseNova(float duracao)
    {
        isTransitioning = false; 
        yield return StartCoroutine(AnimarIris(true, duracao)); 
        if (imagemIrisUI) imagemIrisUI.gameObject.SetActive(false);

        if (player2D != null)
        {
            PlayerMovement2D pm = player2D.GetComponent<PlayerMovement2D>();
            if (pm != null) pm.podeAndar = true;
        }
    }

    void VerificarMusicaDoNivel()
    {
        if (musicSource == null) return;

        AudioClip clipAlvo = null;
        float volumeAlvo = 0.5f;

        if (currentLevel <= 4) { clipAlvo = musicaFases0a4; volumeAlvo = volumeFases0a4; }
        else if (currentLevel <= 8) { clipAlvo = musicaFases5a8; volumeAlvo = volumeFases5a8; }
        else { clipAlvo = musicaFase9; volumeAlvo = volumeFase9; }

        if (clipAlvo == null) return;

        if (musicSource.clip == clipAlvo && musicSource.isPlaying)
        {
            musicSource.volume = volumeAlvo;
            musicSource.loop = true;
            return;
        }

        if (!musicSource.isPlaying || musicSource.clip == null)
        {
            musicSource.volume = volumeAlvo;
            musicSource.clip = clipAlvo;
            musicSource.loop = true;
            musicSource.Play();
        }
        else
        {
            if (fadeMusicaAtual != null)
                StopCoroutine(fadeMusicaAtual);

            fadeMusicaAtual = StartCoroutine(CrossfadeMusica(clipAlvo, volumeAlvo));
        }
    }

    IEnumerator CrossfadeMusica(AudioClip novoClip, float volumeAlvo)
    {
        if (musicSource == null || novoClip == null) yield break;

        float startVol = musicSource.volume;

        for (float t = 0; t < tempoFadeMusica; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / tempoFadeMusica);
            yield return null;
        }
        
        musicSource.volume = 0f;
        musicSource.clip = novoClip;
        musicSource.time = 0f;
        musicSource.loop = true;
        musicSource.Play();
        
        for (float t = 0; t < tempoFadeMusica; t += Time.unscaledDeltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, volumeAlvo, t / tempoFadeMusica);
            yield return null;
        }

        musicSource.volume = volumeAlvo;
        fadeMusicaAtual = null;
    }

    void PosicionarPlayer()
    {
        if (spawnPoints != null && currentLevel < spawnPoints.Length)
        {
            Rigidbody2D rb = player2D.GetComponent<Rigidbody2D>();
            if (rb) rb.simulated = false;

            player2D.position = spawnPoints[currentLevel].position;
            
            Physics2D.gravity = new Vector2(0, -9.81f);
            player2D.rotation = Quaternion.identity;

            Physics2D.SyncTransforms();

            if (rb)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = true;
            }
        }
    }

    public void AvancarFase()
    {
        if (currentLevel >= indiceUltimaFase || isTransitioning) return;

        if (somVitoria != null && sfxSource != null) sfxSource.PlayOneShot(somVitoria);
        StartCoroutine(RotinaTrocaDeFase());
    }

    IEnumerator RotinaTrocaDeFase()
    {
        isTransitioning = true;
        
        if (player2D != null)
        {
            PlayerMovement2D pm = player2D.GetComponent<PlayerMovement2D>();
            if (pm != null) pm.podeAndar = false; 
        }

        currentLevel++;

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarInt("NivelAtual_2D", currentLevel);
            SalvarProgresso2DSeguro(true);
        }

        PlayerPrefs.SetInt(KEY_NIVEL_PENDENTE, currentLevel);
        PlayerPrefs.SetInt(KEY_ABRIR_IRIS, 1);
        PlayerPrefs.SetFloat(KEY_TEMPO_IRIS, tempoTransicaoTrocaFase);
        PlayerPrefs.Save();

        if (imagemIrisUI) imagemIrisUI.gameObject.SetActive(true);
        yield return StartCoroutine(AnimarIris(false, tempoTransicaoTrocaFase));
        
        Physics2D.gravity = new Vector2(0, -9.81f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator AnimarIris(bool abrindo, float duracao)
    {
        if (materialIrisInstancia == null) yield break;

        float t = 0f;
        
        float start = abrindo ? 0f : 1.1f;
        float end = abrindo ? 1.5f : 0f;
        
        while (t < duracao)
        {
            t += Time.unscaledDeltaTime;
            float p = t / Mathf.Max(0.01f, duracao);
            
            if (abrindo) p = p * p * (3f - 2f * p); 
            else p = Mathf.Pow(p, 0.5f); 
            
            materialIrisInstancia.SetFloat("_Radius", Mathf.Lerp(start, end, p));
            yield return null;
        }

        materialIrisInstancia.SetFloat("_Radius", end);
    }

    public void PegarChaveFinal()
    {
        if (pegouAChaveFinal) return;

        pegouAChaveFinal = true;
        if (hudChaveUI) hudChaveUI.SetActive(true);
        
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado("Fase9_PegouChave", true);
            SalvarProgresso2DSeguro(true);
        }

        StartCoroutine(TimerTelaAzulDaMorte(tempoTotalFaseImpossivel));
    }

    IEnumerator TimerTelaAzulDaMorte(float tempoInicial)
    {
        cronometroAtual = tempoInicial;

        while (cronometroAtual > 0)
        {
            if (jogoCrashou) yield break;
            cronometroAtual -= Time.deltaTime;
            yield return null;
        }

        yield return StartCoroutine(ExecutarCrashFake());
    }

    IEnumerator ExecutarCrashFake()
    {
        jogoCrashou = true;
        isTransitioning = true;
        
        PararEDestruirMusicaPersistente();

        if (telaAzulBSOD) telaAzulBSOD.SetActive(true);
        if (somTelaAzul != null && sfxSource != null) sfxSource.PlayOneShot(somTelaAzul);
        
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarFloat("TempoRestante_Fase9", tempoTotalFaseImpossivel);
            PersistenciaManager.Instance.RegistrarEstado("Fase9_PegouChave", false);
            PersistenciaManager.Instance.SalvarInt("NivelAtual_2D", 0);
            PersistenciaManager.Instance.RegistrarEstado("PC_Crash_Event", true); 
            
            SalvarProgresso2DSeguro(true);
        }

        yield return new WaitForSeconds(4.0f);
        
        if (sfxSource != null)
            sfxSource.Stop();

        SceneManager.LoadScene(nomeCenaPrincipal);
    }

    void Update()
    {
        if (jogoCrashou) return;

        if (Input.GetKeyDown(KeyCode.R))
            ReiniciarFaseAtual();

        if (musicSource != null && musicSource.clip != null && !musicSource.isPlaying && !isTransitioning)
        {
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    public void ReiniciarFaseAtual()
    {
        if (isTransitioning || jogoCrashou) return;
        StartCoroutine(RotinaResetDeFase());
    }

    IEnumerator RotinaResetDeFase()
    {
        isTransitioning = true;

        if (player2D != null)
        {
            PlayerMovement2D pm = player2D.GetComponent<PlayerMovement2D>();
            if (pm != null) pm.podeAndar = false; 
        }

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarInt("NivelAtual_2D", currentLevel);

            if (currentLevel >= indiceUltimaFase && pegouAChaveFinal)
            {
                PersistenciaManager.Instance.SalvarFloat("TempoRestante_Fase9", cronometroAtual);
            }

            SalvarProgresso2DSeguro(true);
        }

        PlayerPrefs.SetInt(KEY_NIVEL_PENDENTE, currentLevel);
        PlayerPrefs.SetInt(KEY_ABRIR_IRIS, 1);
        PlayerPrefs.SetFloat(KEY_TEMPO_IRIS, tempoTransicaoResetR);
        PlayerPrefs.Save();

        if (imagemIrisUI) imagemIrisUI.gameObject.SetActive(true);
        yield return StartCoroutine(AnimarIris(false, tempoTransicaoResetR));
        
        Physics2D.gravity = new Vector2(0, -9.81f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void PararEDestruirMusicaPersistente()
    {
        if (musicaPersistente != null)
        {
            musicaPersistente.Stop();
            musicaPersistente.clip = null;
        }

        if (musicaPersistenteObj != null)
        {
            Destroy(musicaPersistenteObj);
            musicaPersistenteObj = null;
            musicaPersistente = null;
        }

        fadeMusicaAtual = null;
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (sfxSource != null)
            sfxSource.Stop();

        if (materialIrisInstancia != null)
            Destroy(materialIrisInstancia);
    }

    private void SalvarProgresso2DSeguro(bool forcarSaveNoDisco)
    {
        if (PersistenciaManager.Instance == null) return;
        if (!save2DInicializado && !forcarSaveNoDisco) return;

        PersistenciaManager.Instance.SalvarTudo(forcarSaveNoDisco);
    }
}
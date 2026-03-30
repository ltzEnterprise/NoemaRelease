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
    public float tempoTransicaoVisual = 0.6f; 
    private Material materialIrisInstancia;

    [Header("Áudio Sources (ATENÇÃO: Crie um objeto VAZIO para eles)")]
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

    // Estado
    public int currentLevel = 0; 
    private bool isTransitioning = false;
    private bool jogoCrashou = false;
    private bool pegouAChaveFinal = false;
    private float cronometroAtual;

    // --- VARIÁVEIS PARA O ÁUDIO IMORTAL ---
    private static AudioSource activeMusicSource;
    private static AudioSource activeSfxSource;

    void Awake()
    {
        Instance = this;

        if (musicSource == null || sfxSource == null) return;

        // --- A MARRETADA DO LOOP TÁ AQUI ---
        // Força a Unity a entender que a música de fundo é pra repetir infinitamente
        musicSource.loop = true;

        if (activeMusicSource == null)
        {
            musicSource.transform.parent = null; 
            DontDestroyOnLoad(musicSource.gameObject);
            activeMusicSource = musicSource;

            if (sfxSource.gameObject != musicSource.gameObject)
            {
                sfxSource.transform.parent = null;
                DontDestroyOnLoad(sfxSource.gameObject);
            }
            activeSfxSource = sfxSource;
        }
        else
        {
            if (musicSource != activeMusicSource && musicSource.gameObject != this.gameObject) Destroy(musicSource.gameObject);
            if (sfxSource != activeSfxSource && sfxSource.gameObject != this.gameObject && sfxSource.gameObject != musicSource.gameObject) Destroy(sfxSource.gameObject);

            musicSource = activeMusicSource;
            sfxSource = activeSfxSource;
            
            // Garante que o loop continue ativo mesmo se voltar pro mundo 2D
            musicSource.loop = true; 
        }
    }

    void Start()
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

        if (usarSpawnDeTeste) 
        {
            currentLevel = spawnInicialTeste;
            PlayerPrefs.DeleteKey("Fase9_PegouChave");
            PlayerPrefs.DeleteKey("TempoRestante_Fase9");
            PlayerPrefs.Save();
        }
        else 
        {
            currentLevel = PlayerPrefs.GetInt("NivelAtual_2D", 0);
        }

        PosicionarPlayer();
        VerificarMusicaDoNivel();

        if (sistemaLanterna != null) sistemaLanterna.AtivarSistema(currentLevel > 0);

        if (currentLevel >= indiceUltimaFase)
        {
            if (!usarSpawnDeTeste && PlayerPrefs.GetInt("Fase9_PegouChave", 0) == 1)
            {
                pegouAChaveFinal = true;
                if (hudChaveUI) hudChaveUI.SetActive(true);
                float tempoSalvo = PlayerPrefs.GetFloat("TempoRestante_Fase9", tempoTotalFaseImpossivel);
                StartCoroutine(TimerTelaAzulDaMorte(tempoSalvo));
            }
        }

        if (PlayerPrefs.GetInt("AbrirIrisNoStart", 0) == 1)
        {
            PlayerPrefs.SetInt("AbrirIrisNoStart", 0);
            PlayerPrefs.Save();
            
            if (imagemIrisUI) 
            {
                imagemIrisUI.gameObject.SetActive(true);
                if (materialIrisInstancia != null) materialIrisInstancia.SetFloat("_Radius", 0f); 
            }

            StartCoroutine(RotinaChegadaNaFaseNova());
        }
    }

    public void TocarSomResetAgua()
    {
        if (somResetAgua != null && sfxSource != null) sfxSource.PlayOneShot(somResetAgua);
    }

    IEnumerator RotinaChegadaNaFaseNova()
    {
        isTransitioning = false; 
        
        yield return StartCoroutine(AnimarIris(true)); 
        if (imagemIrisUI) imagemIrisUI.gameObject.SetActive(false);
    }

    void VerificarMusicaDoNivel()
    {
        AudioClip clipAlvo = null;
        float volumeAlvo = 0.5f;

        if (currentLevel <= 4) { clipAlvo = musicaFases0a4; volumeAlvo = volumeFases0a4; }
        else if (currentLevel <= 8) { clipAlvo = musicaFases5a8; volumeAlvo = volumeFases5a8; }
        else { clipAlvo = musicaFase9; volumeAlvo = volumeFase9; }

        if (musicSource.clip == clipAlvo && musicSource.isPlaying)
        {
            musicSource.volume = volumeAlvo;
            return;
        }

        if (!musicSource.isPlaying)
        {
            musicSource.volume = volumeAlvo;
            musicSource.clip = clipAlvo;
            musicSource.Play();
        }
        else StartCoroutine(CrossfadeMusica(clipAlvo, volumeAlvo));
    }

    IEnumerator CrossfadeMusica(AudioClip novoClip, float volumeAlvo)
    {
        float startVol = musicSource.volume;
        for (float t = 0; t < tempoFadeMusica; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(startVol, 0f, t / tempoFadeMusica);
            yield return null;
        }
        
        musicSource.volume = 0f;
        musicSource.clip = novoClip;
        musicSource.Play();
        
        for (float t = 0; t < tempoFadeMusica; t += Time.deltaTime)
        {
            musicSource.volume = Mathf.Lerp(0f, volumeAlvo, t / tempoFadeMusica);
            yield return null;
        }
        musicSource.volume = volumeAlvo;
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
            if (rb) { rb.linearVelocity = Vector2.zero; rb.simulated = true; }
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

        if (imagemIrisUI) imagemIrisUI.gameObject.SetActive(true);
        yield return StartCoroutine(AnimarIris(false));

        currentLevel++;
        PlayerPrefs.SetInt("NivelAtual_2D", currentLevel);
        PlayerPrefs.SetInt("AbrirIrisNoStart", 1);
        PlayerPrefs.Save();

        Physics2D.gravity = new Vector2(0, -9.81f);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    IEnumerator AnimarIris(bool abrindo)
    {
        if (materialIrisInstancia == null) yield break;
        float t = 0f;
        
        float start = abrindo ? 0f : 1.1f;
        float end = abrindo ? 1.5f : 0f;
        
        while (t < tempoTransicaoVisual)
        {
            t += Time.deltaTime;
            float p = t / tempoTransicaoVisual;
            
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
        
        PlayerPrefs.SetInt("Fase9_PegouChave", 1);
        PlayerPrefs.Save();
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
        
        if (musicSource != null) musicSource.Stop();
        if (telaAzulBSOD) telaAzulBSOD.SetActive(true);
        if (somTelaAzul != null && sfxSource != null) sfxSource.PlayOneShot(somTelaAzul);
        
        PlayerPrefs.DeleteKey("TempoRestante_Fase9");
        PlayerPrefs.DeleteKey("Fase9_PegouChave");
        PlayerPrefs.SetInt("PC_Crash_Event", 1); 
        PlayerPrefs.Save();

        yield return new WaitForSeconds(4.0f);
        
        if (activeMusicSource != null) Destroy(activeMusicSource.gameObject);
        if (activeSfxSource != null && activeSfxSource.gameObject != activeMusicSource.gameObject) Destroy(activeSfxSource.gameObject);
        
        SceneManager.LoadScene(nomeCenaPrincipal);
    }

    void Update()
    {
        if (jogoCrashou) return;
        if (Input.GetKeyDown(KeyCode.R)) ReiniciarFaseAtual();

        // --- CÃO DE GUARDA DA MÚSICA ---
        // Se a música calar a boca no meio da fase, a gente força ela a voltar.
        if (musicSource != null && musicSource.clip != null && !musicSource.isPlaying && !isTransitioning)
        {
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

        if (imagemIrisUI) imagemIrisUI.gameObject.SetActive(true);
        yield return StartCoroutine(AnimarIris(false));

        PlayerPrefs.SetInt("NivelAtual_2D", currentLevel);
        if (currentLevel >= indiceUltimaFase && pegouAChaveFinal)
            PlayerPrefs.SetFloat("TempoRestante_Fase9", cronometroAtual);
            
        PlayerPrefs.SetInt("AbrirIrisNoStart", 1);
        PlayerPrefs.Save();
        
        Physics2D.gravity = new Vector2(0, -9.81f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
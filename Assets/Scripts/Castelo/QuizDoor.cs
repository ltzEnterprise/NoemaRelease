using UnityEngine;
using UnityEngine.UI; 
using UnityEngine.EventSystems;
using TMPro; 
using System.Collections;
using System.Collections.Generic;

public class QuizDoor : MonoBehaviour
{
    [System.Serializable]
    public class LinhaDeDialogo
    {
        [Tooltip("Texto em Português (Original)")]
        [TextArea] public string texto;

        [Tooltip("Texto em Inglês")]
        [TextArea] public string textoEN;

        public float tamanhoDaFonte = 36f;
        public float velocidadeDigitar = 0.04f;
        public float tempoDeEsperaApos = 1.5f;
    }

    [System.Serializable]
    public class PerguntaQuiz
    {
        [Tooltip("Enunciado em Português (Original)")]
        [TextArea] public string enunciado; 

        [Tooltip("Enunciado em Inglês")]
        [TextArea] public string enunciadoEN; 

        public float tamanhoDaFonte = 36f; 

        [Tooltip("Alternativas em Português (Original)")]
        public string[] alternativas = new string[4]; 

        [Tooltip("Alternativas em Inglês")]
        public string[] alternativasEN = new string[4]; 

        [Range(0, 3)] public int indiceCorreta; 
    }

    [Header("Save System")]
    public string uniqueID; 

    [Header("Identificação")]
    public int idDessaCasa = 2; 
    public int idItemPremio = 4; 

    [Header("--- DADOS ---")]
    public List<PerguntaQuiz> bancoDePerguntas; 
    public List<LinhaDeDialogo> introducao; 

    [Header("UI")]
    public GameObject painelTelaPreta;
    public TextMeshProUGUI textoDialogoIntro; 
    public GameObject painelQuiz; 
    public TextMeshProUGUI textoEnunciado; 
    public TextMeshProUGUI[] textosDosBotoes; 
    public TextMeshProUGUI textoFeedback; 
    public GameObject textoInteragir;
    public GameObject painelRecompensa; 

    [Header("Sons do Quiz")]
    public AudioSource audioSourceSFX;
    public AudioSource audioSourceVoz;
    public AudioSource audioSourceMusica;
    public AudioClip somBatida;
    public AudioClip somPortaAbrindo;
    public AudioClip somDigitando;
    public AudioClip somAcerto;
    public AudioClip somErro;
    public AudioClip somVitoriaFinal;
    public AudioClip musicaQuiz;

    [Header("--- SOUNDTRACK GLOBAL ---")]
    public SoundtrackManager soundtrackManager;

    [Tooltip("Nome da música/área que deve voltar depois do quiz. Tem que existir na lista do SoundtrackManager.")]
    public string nomeMusicaRestaurarAposQuiz = "";

    [Tooltip("Se true, o quiz manda o SoundtrackManager parar a trilha padrão enquanto o quiz está aberto.")]
    public bool desligarSoundtrackDuranteQuiz = true;

    [Header("--- UI FORÇADA ---")]
    public int sortingOrderQuiz = 10000;

    private bool emCena = false;
    private bool jaViuIntro = false;
    private bool casaResolvida = false;
    private bool saveCarregado = false;

    private List<PerguntaQuiz> perguntasDaRodada; 
    private PerguntaQuiz perguntaAtual;
    private int acertosConsecutivos = 0;
    private bool aguardandoResposta = false;

    private Vector3 posicaoInicialInteracao;
    private bool aguardandoSoltarE = false;

    void Start()
    {
        if (string.IsNullOrEmpty(uniqueID))
            Debug.LogError($"[ERRO] QuizDoor '{gameObject.name}' sem UniqueID!");

        if (painelTelaPreta) painelTelaPreta.SetActive(false);
        if (painelQuiz) painelQuiz.SetActive(false);
        if (painelRecompensa) painelRecompensa.SetActive(false);
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoFeedback) textoFeedback.text = "";

        if (textoDialogoIntro) textoDialogoIntro.gameObject.SetActive(false);

        PrepararCanvasAlto(painelTelaPreta);
        PrepararCanvasAlto(painelQuiz);
        PrepararCanvasAlto(painelRecompensa);

        if (soundtrackManager == null)
            soundtrackManager = Object.FindFirstObjectByType<SoundtrackManager>();

        perguntasDaRodada = new List<PerguntaQuiz>();

        StartCoroutine(CarregarEstadoSalvoSeguro());
    }

    IEnumerator CarregarEstadoSalvoSeguro()
    {
        if (PersistenciaManager.Instance != null)
            yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);

        CarregarEstadoSalvo();
        saveCarregado = true;
    }

    void CarregarEstadoSalvo()
    {
        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            if (PersistenciaManager.Instance.ObterEstado(uniqueID))
                casaResolvida = true;
        }

        if (EstadoGlobal.casasResolvidas != null && idDessaCasa < EstadoGlobal.casasResolvidas.Length)
        {
            if (EstadoGlobal.casasResolvidas[idDessaCasa])
                casaResolvida = true;
        }
    }

    public void AoOlhar()
    {
        if (!saveCarregado || casaResolvida || emCena) return;
        if (textoInteragir) textoInteragir.SetActive(true);
    }

    public void AoSair()
    {
        if (textoInteragir) textoInteragir.SetActive(false);
    }

    public void Interagir()
    {
        if (!saveCarregado || casaResolvida || emCena) return;

        if (FPS_Master.Instance != null)
            posicaoInicialInteracao = FPS_Master.Instance.transform.position;

        aguardandoSoltarE = true;

        if (!jaViuIntro)
            StartCoroutine(SequenciaIntro());
        else
            AbrirQuiz();
    }

    void Update()
    {
        if (!saveCarregado) return;

        if (emCena)
        {
            if (aguardandoSoltarE)
            {
                if (!Input.GetKey(KeyCode.E))
                    aguardandoSoltarE = false;

                return;
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                LimparSelecaoUI();
                SairDoQuiz(false); 
                return;
            }
        }

        if (casaResolvida) return;
    }

    void PrepararCanvasAlto(GameObject painel)
    {
        if (painel == null) return;

        Canvas canvas = painel.GetComponent<Canvas>();
        if (canvas == null) canvas = painel.AddComponent<Canvas>();

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrderQuiz;

        GraphicRaycaster raycaster = painel.GetComponent<GraphicRaycaster>();
        if (raycaster == null) painel.AddComponent<GraphicRaycaster>();
    }

    void LimparSelecaoUI()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    void ConfigurarMouseQuiz(bool ativo)
    {
        if (ativo)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void TravarPlayer(bool travar)
    {
        if (FPS_Master.Instance != null)
            FPS_Master.travadoInteracao = travar;
    }

    void RetornarPosicaoSegura()
    {
        if (FPS_Master.Instance != null)
        {
            FPS_Master.Instance.Teleportar(posicaoInicialInteracao);
            Physics.SyncTransforms();
        }
    }

    void DesligarSoundtrackGlobal()
    {
        if (!desligarSoundtrackDuranteQuiz) return;

        if (soundtrackManager == null)
            soundtrackManager = Object.FindFirstObjectByType<SoundtrackManager>();

        if (soundtrackManager != null)
            soundtrackManager.SwitchSoundtrack("");
    }

    void RestaurarSoundtrackGlobal()
    {
        if (!desligarSoundtrackDuranteQuiz) return;
        if (string.IsNullOrEmpty(nomeMusicaRestaurarAposQuiz)) return;

        if (soundtrackManager == null)
            soundtrackManager = Object.FindFirstObjectByType<SoundtrackManager>();

        if (soundtrackManager != null)
            soundtrackManager.SwitchSoundtrack(nomeMusicaRestaurarAposQuiz);
    }

    void LigarMusicaQuiz()
    {
        if (audioSourceMusica == null) return;

        audioSourceMusica.Stop();
        audioSourceMusica.clip = musicaQuiz;
        audioSourceMusica.loop = true;
        audioSourceMusica.volume = 1.0f;

        if (musicaQuiz != null)
            audioSourceMusica.Play();
    }

    void PararMusicaQuiz()
    {
        if (audioSourceMusica == null) return;

        audioSourceMusica.Stop();
        audioSourceMusica.clip = null;
        audioSourceMusica.volume = 1.0f;
    }

    IEnumerator SequenciaIntro()
    {
        emCena = true;
        jaViuIntro = true;

        TravarPlayer(true);
        ConfigurarMouseQuiz(true);
        LimparSelecaoUI();
        DesligarSoundtrackGlobal();

        if (painelTelaPreta) painelTelaPreta.SetActive(true);
        if (textoInteragir) textoInteragir.SetActive(false);

        if (textoDialogoIntro)
        {
            textoDialogoIntro.gameObject.SetActive(true);
            textoDialogoIntro.text = ""; 
        }

        if (audioSourceSFX) audioSourceSFX.PlayOneShot(somBatida);
        yield return new WaitForSeconds(1f);

        if (audioSourceSFX) audioSourceSFX.PlayOneShot(somBatida);
        yield return new WaitForSeconds(1f);

        if (audioSourceSFX) audioSourceSFX.PlayOneShot(somPortaAbrindo);
        yield return new WaitForSeconds(1f);

        int lang = (LanguageManager.Instance != null) ? LanguageManager.Instance.currentLanguage : 0;

        foreach (LinhaDeDialogo linha in introducao)
        {
            if (textoDialogoIntro) textoDialogoIntro.fontSize = linha.tamanhoDaFonte; 

            string textoParaExibir = (lang == 0) ? linha.texto : linha.textoEN;
            if (lang == 1 && string.IsNullOrEmpty(textoParaExibir)) textoParaExibir = linha.texto;

            yield return StartCoroutine(EfeitoDigitar(textoParaExibir, linha.velocidadeDigitar, textoDialogoIntro));
            yield return new WaitForSeconds(linha.tempoDeEsperaApos);
        }

        if (painelTelaPreta) painelTelaPreta.SetActive(false);

        AbrirQuiz(); 
    }

    void AbrirQuiz()
    {
        emCena = true;

        TravarPlayer(true);
        ConfigurarMouseQuiz(true);
        LimparSelecaoUI();
        DesligarSoundtrackGlobal();

        PrepararCanvasAlto(painelQuiz);
        PrepararCanvasAlto(painelTelaPreta);
        PrepararCanvasAlto(painelRecompensa);

        LigarMusicaQuiz();

        if (textoDialogoIntro) textoDialogoIntro.gameObject.SetActive(false);
        if (painelTelaPreta) painelTelaPreta.SetActive(false);
        if (painelQuiz) painelQuiz.SetActive(true);
        if (textoInteragir) textoInteragir.SetActive(false);

        LimparSelecaoUI();

        if (perguntasDaRodada.Count == 0)
            ResetarRodada();

        CarregarNovaPergunta();
    }

    void ResetarRodada()
    {
        perguntasDaRodada.Clear();
        acertosConsecutivos = 0;
        perguntasDaRodada.AddRange(bancoDePerguntas);
    }

    void CarregarNovaPergunta()
    {
        if (textoFeedback) textoFeedback.text = "";

        aguardandoResposta = true;
        LimparSelecaoUI();

        if (perguntasDaRodada.Count == 0) 
        {
            StartCoroutine(Vitoria()); 
            return;
        }

        int sorteio = Random.Range(0, perguntasDaRodada.Count);
        perguntaAtual = perguntasDaRodada[sorteio];

        int lang = (LanguageManager.Instance != null) ? LanguageManager.Instance.currentLanguage : 0;

        if (textoEnunciado) 
        {
            string enunciadoExibir = (lang == 0) ? perguntaAtual.enunciado : perguntaAtual.enunciadoEN;
            if (lang == 1 && string.IsNullOrEmpty(enunciadoExibir)) enunciadoExibir = perguntaAtual.enunciado;

            textoEnunciado.text = enunciadoExibir;
            textoEnunciado.fontSize = perguntaAtual.tamanhoDaFonte; 
        }

        for (int i = 0; i < 4; i++)
        {
            if (i < textosDosBotoes.Length && textosDosBotoes[i] != null) 
            {
                string altExibir = (lang == 0) ? perguntaAtual.alternativas[i] : perguntaAtual.alternativasEN[i];
                if (lang == 1 && string.IsNullOrEmpty(altExibir)) altExibir = perguntaAtual.alternativas[i];

                textosDosBotoes[i].text = altExibir;
            }
        }

        LimparSelecaoUI();
    }

    public void Responder(int indiceResposta)
    {
        if (!aguardandoResposta) return; 
        StartCoroutine(ProcessarResposta(indiceResposta));
    }

    IEnumerator ProcessarResposta(int indice)
    {
        aguardandoResposta = false;
        LimparSelecaoUI();

        int lang = (LanguageManager.Instance != null) ? LanguageManager.Instance.currentLanguage : 0;

        if (indice == perguntaAtual.indiceCorreta)
        {
            if (textoFeedback)
            { 
                textoFeedback.text = (lang == 0) ? "CORRETO" : "CORRECT"; 
                textoFeedback.color = Color.green; 
            }

            if (audioSourceSFX) audioSourceSFX.PlayOneShot(somAcerto);

            acertosConsecutivos++;
            perguntasDaRodada.Remove(perguntaAtual); 

            yield return new WaitForSeconds(1.0f);

            if (acertosConsecutivos >= 10 || perguntasDaRodada.Count == 0)
                StartCoroutine(Vitoria());
            else
                CarregarNovaPergunta(); 
        }
        else
        {
            if (textoFeedback)
            { 
                textoFeedback.text = (lang == 0) ? "VOCÊ FALHOU..." : "YOU FAILED..."; 
                textoFeedback.color = Color.red; 
            }

            if (audioSourceSFX) audioSourceSFX.PlayOneShot(somErro);

            acertosConsecutivos = 0;
            ResetarRodada(); 

            yield return new WaitForSeconds(2.0f);

            SairDoQuiz(true); 
        }
    }

    void SairDoQuiz(bool porErro)
    {
        StopAllCoroutines();

        emCena = false;
        aguardandoResposta = false;
        aguardandoSoltarE = false;

        LimparSelecaoUI();

        if (painelQuiz) painelQuiz.SetActive(false);
        if (painelTelaPreta) painelTelaPreta.SetActive(false);
        if (painelRecompensa) painelRecompensa.SetActive(false);

        if (textoDialogoIntro)
        {
            textoDialogoIntro.text = "";
            textoDialogoIntro.gameObject.SetActive(false);
        }

        if (textoFeedback) textoFeedback.text = "";
        if (audioSourceVoz) audioSourceVoz.Stop();

        PararMusicaQuiz();
        RestaurarSoundtrackGlobal();

        RetornarPosicaoSegura();
        TravarPlayer(false);
        ConfigurarMouseQuiz(false);
    }

    IEnumerator Vitoria()
    {
        if (painelQuiz) painelQuiz.SetActive(false);

        LimparSelecaoUI();

        if (textoDialogoIntro)
        {
            int lang = (LanguageManager.Instance != null) ? LanguageManager.Instance.currentLanguage : 0;
            string textoVitoria = (lang == 0) ? "Você acertou todas as perguntas." : "You answered all questions correctly.";

            if (painelTelaPreta) painelTelaPreta.SetActive(true);

            textoDialogoIntro.gameObject.SetActive(true);
            textoDialogoIntro.text = "";

            yield return StartCoroutine(EfeitoDigitar(textoVitoria, 0.05f, textoDialogoIntro));
            yield return new WaitForSeconds(2.0f);

            textoDialogoIntro.gameObject.SetActive(false);

            if (painelTelaPreta) painelTelaPreta.SetActive(false);
        }

        if (painelRecompensa) painelRecompensa.SetActive(true);
        if (audioSourceSFX) audioSourceSFX.PlayOneShot(somVitoriaFinal);

        FinalizarMissao();

        yield return new WaitForSeconds(5.0f);

        if (painelRecompensa) painelRecompensa.SetActive(false);

        emCena = false;

        PararMusicaQuiz();
        RestaurarSoundtrackGlobal();

        RetornarPosicaoSegura();
        TravarPlayer(false);
        ConfigurarMouseQuiz(false);
    }

    void FinalizarMissao()
    {
        casaResolvida = true;

        if (EstadoGlobal.armasDesbloqueadas != null && idItemPremio < EstadoGlobal.armasDesbloqueadas.Length) 
            EstadoGlobal.armasDesbloqueadas[idItemPremio] = true;

        if (EstadoGlobal.casasResolvidas != null && idDessaCasa < EstadoGlobal.casasResolvidas.Length) 
            EstadoGlobal.casasResolvidas[idDessaCasa] = true;

        if (InventoryManager.Instance != null) 
            InventoryManager.Instance.ReceberItem(idItemPremio);

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);

        SalvarProgressoSeguro();
    }

    void SalvarProgressoSeguro()
    {
        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(false);
    }

    IEnumerator EfeitoDigitar(string frase, float velocidade, TextMeshProUGUI alvo)
    {
        if (alvo == null) yield break;

        alvo.text = "";

        if (audioSourceVoz && somDigitando)
        {
            audioSourceVoz.clip = somDigitando;
            audioSourceVoz.loop = false;
            audioSourceVoz.Play();
        }

        foreach (char letra in frase.ToCharArray())
        {
            alvo.text += letra;
            yield return new WaitForSeconds(velocidade);
        }

        if (audioSourceVoz) audioSourceVoz.Stop();
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateID()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }
}
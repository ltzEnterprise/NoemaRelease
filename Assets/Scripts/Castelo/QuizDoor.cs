using UnityEngine;
using UnityEngine.UI; 
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

    [Header("Sons")]
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
    [Tooltip("Marque se o áudio acima for MÚSICA (pausa o ambiente). Desmarque se for EFEITO (toca junto).")]
    public bool silenciarMusicaBackground = true;

    private bool emCena = false;
    private bool jaViuIntro = false;
    private bool casaResolvida = false;

    private List<PerguntaQuiz> perguntasDaRodada; 
    private PerguntaQuiz perguntaAtual;
    private int acertosConsecutivos = 0;
    private bool aguardandoResposta = false;
    private Coroutine currentFade;
    
    private Vector3 posicaoInicialInteracao;
    private AudioSource musicaAmbientePausada;

    void Start()
    {
        if (string.IsNullOrEmpty(uniqueID)) Debug.LogError($"[ERRO] QuizDoor '{gameObject.name}' sem UniqueID!");

        if (painelTelaPreta) painelTelaPreta.SetActive(false);
        if (painelQuiz) painelQuiz.SetActive(false);
        if (painelRecompensa) painelRecompensa.SetActive(false);
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoFeedback) textoFeedback.text = "";
        
        if (textoDialogoIntro) textoDialogoIntro.gameObject.SetActive(false);

        CarregarEstadoSalvo();

        perguntasDaRodada = new List<PerguntaQuiz>();
    }

    void CarregarEstadoSalvo()
    {
        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            if (PersistenciaManager.Instance.ObterEstado(uniqueID)) casaResolvida = true;
        }
        if (EstadoGlobal.casasResolvidas != null && idDessaCasa < EstadoGlobal.casasResolvidas.Length)
        {
             if (EstadoGlobal.casasResolvidas[idDessaCasa]) casaResolvida = true;
        }
    }

    public void AoOlhar()
    {
        if (casaResolvida || emCena) return;
        if (textoInteragir) textoInteragir.SetActive(true);
    }

    public void AoSair()
    {
        if (textoInteragir) textoInteragir.SetActive(false);
    }

    public void Interagir()
    {
        if (casaResolvida || emCena) return;

        if (FPS_Master.Instance != null)
        {
            posicaoInicialInteracao = FPS_Master.Instance.transform.position;
        }

        if (!jaViuIntro) StartCoroutine(SequenciaIntro());
        else AbrirQuiz();
    }

    void Update()
    {
        if (casaResolvida) return;

        if (emCena && painelQuiz.activeSelf && Input.GetKeyDown(KeyCode.Escape))
        {
            SairDoQuiz(false); 
        }
    }

    void TravarPlayer(bool travar)
    {
        if (FPS_Master.Instance != null)
        {
            FPS_Master.Instance.AlterarEstadoJogador(travar, travar); 
        }
    }

    void RetornarPosicaoSegura()
    {
        if (FPS_Master.Instance != null)
        {
            FPS_Master.Instance.Teleportar(posicaoInicialInteracao);
        }
    }

    IEnumerator SequenciaIntro()
    {
        emCena = true;
        jaViuIntro = true;
        TravarPlayer(true);
        
        painelTelaPreta.SetActive(true);
        if (textoInteragir) textoInteragir.SetActive(false);

        if (textoDialogoIntro)
        {
            textoDialogoIntro.gameObject.SetActive(true);
            textoDialogoIntro.text = ""; 
        }

        if(audioSourceSFX) audioSourceSFX.PlayOneShot(somBatida);
        yield return new WaitForSeconds(1f);
        if(audioSourceSFX) audioSourceSFX.PlayOneShot(somBatida);
        yield return new WaitForSeconds(1f);
        if(audioSourceSFX) audioSourceSFX.PlayOneShot(somPortaAbrindo);
        yield return new WaitForSeconds(1f);

        // Verifica a língua atual
        int lang = (LanguageManager.Instance != null) ? LanguageManager.Instance.currentLanguage : 0;

        foreach (LinhaDeDialogo linha in introducao)
        {
            if (textoDialogoIntro) textoDialogoIntro.fontSize = linha.tamanhoDaFonte; 
            
            // Pega o texto correto com fallback de segurança
            string textoParaExibir = (lang == 0) ? linha.texto : linha.textoEN;
            if (lang == 1 && string.IsNullOrEmpty(textoParaExibir)) textoParaExibir = linha.texto;

            yield return StartCoroutine(EfeitoDigitar(textoParaExibir, linha.velocidadeDigitar, textoDialogoIntro));
            yield return new WaitForSeconds(linha.tempoDeEsperaApos);
        }

        painelTelaPreta.SetActive(false);
        AbrirQuiz(); 
    }

    void AbrirQuiz()
    {
        emCena = true;
        TravarPlayer(true);
        
        if (silenciarMusicaBackground)
        {
            GameObject bgmObj = GameObject.Find("Soundtrack");
            if (bgmObj != null)
            {
                AudioSource bgmSource = bgmObj.GetComponent<AudioSource>();
                if (bgmSource != null && bgmSource.isPlaying)
                {
                    bgmSource.Pause(); 
                    musicaAmbientePausada = bgmSource;
                }
            }
        }

        if (audioSourceMusica) 
        { 
            if (currentFade != null) StopCoroutine(currentFade);
            audioSourceMusica.volume = 1.0f; 
            audioSourceMusica.clip = musicaQuiz; 
            audioSourceMusica.loop = true; 
            audioSourceMusica.Play(); 
        }

        if (textoDialogoIntro) textoDialogoIntro.gameObject.SetActive(false);
        painelTelaPreta.SetActive(false);
        painelQuiz.SetActive(true);
        if (textoInteragir) textoInteragir.SetActive(false);

        if (perguntasDaRodada.Count == 0) ResetarRodada();
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
            // Configura o enunciado traduzido
            string enunciadoExibir = (lang == 0) ? perguntaAtual.enunciado : perguntaAtual.enunciadoEN;
            if (lang == 1 && string.IsNullOrEmpty(enunciadoExibir)) enunciadoExibir = perguntaAtual.enunciado;

            textoEnunciado.text = enunciadoExibir;
            textoEnunciado.fontSize = perguntaAtual.tamanhoDaFonte; 
        }

        for (int i = 0; i < 4; i++)
        {
            if (i < textosDosBotoes.Length && textosDosBotoes[i] != null) 
            {
                // Configura as alternativas traduzidas
                string altExibir = (lang == 0) ? perguntaAtual.alternativas[i] : perguntaAtual.alternativasEN[i];
                if (lang == 1 && string.IsNullOrEmpty(altExibir)) altExibir = perguntaAtual.alternativas[i];
                
                textosDosBotoes[i].text = altExibir;
            }
        }
    }

    public void Responder(int indiceResposta)
    {
        if (!aguardandoResposta) return; 
        StartCoroutine(ProcessarResposta(indiceResposta));
    }

    IEnumerator ProcessarResposta(int indice)
    {
        aguardandoResposta = false;
        int lang = (LanguageManager.Instance != null) ? LanguageManager.Instance.currentLanguage : 0;

        if (indice == perguntaAtual.indiceCorreta)
        {
            if(textoFeedback) { 
                textoFeedback.text = (lang == 0) ? "CORRETO" : "CORRECT"; 
                textoFeedback.color = Color.green; 
            }
            if(audioSourceSFX) audioSourceSFX.PlayOneShot(somAcerto);
            
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
            if(textoFeedback) { 
                textoFeedback.text = (lang == 0) ? "VOCÊ FALHOU..." : "YOU FAILED..."; 
                textoFeedback.color = Color.red; 
            }
            if(audioSourceSFX) audioSourceSFX.PlayOneShot(somErro);

            acertosConsecutivos = 0;
            ResetarRodada(); 
            yield return new WaitForSeconds(2.0f);
            SairDoQuiz(true); 
        }
    }

    void SairDoQuiz(bool porErro)
    {
        emCena = false;
        painelQuiz.SetActive(false);
        
        if (audioSourceMusica && audioSourceMusica.isPlaying) 
        {
            if (currentFade != null) StopCoroutine(currentFade);
            currentFade = StartCoroutine(FadeOutMusica(1.5f)); 
        }

        if (musicaAmbientePausada != null)
        {
            musicaAmbientePausada.UnPause();
            musicaAmbientePausada = null;
        }

        RetornarPosicaoSegura();
        TravarPlayer(false);
    }

    IEnumerator Vitoria()
    {
        painelQuiz.SetActive(false);
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeOutMusica(2.0f));

        if (musicaAmbientePausada != null)
        {
            musicaAmbientePausada.UnPause();
            musicaAmbientePausada = null;
        }

        if (textoDialogoIntro)
        {
            int lang = (LanguageManager.Instance != null) ? LanguageManager.Instance.currentLanguage : 0;
            string textoVitoria = (lang == 0) ? "Você acertou todas as perguntas." : "You answered all questions correctly.";

            painelTelaPreta.SetActive(true);
            textoDialogoIntro.gameObject.SetActive(true);
            textoDialogoIntro.text = "";
            yield return StartCoroutine(EfeitoDigitar(textoVitoria, 0.05f, textoDialogoIntro));
            yield return new WaitForSeconds(2.0f);
            textoDialogoIntro.gameObject.SetActive(false);
            painelTelaPreta.SetActive(false);
        }

        if (painelRecompensa) painelRecompensa.SetActive(true);
        if(audioSourceSFX) audioSourceSFX.PlayOneShot(somVitoriaFinal);

        FinalizarMissao();

        yield return new WaitForSeconds(5.0f);
        
        if (painelRecompensa) painelRecompensa.SetActive(false);

        emCena = false;
        
        RetornarPosicaoSegura();
        TravarPlayer(false);
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
    }

    IEnumerator FadeOutMusica(float duracao)
    {
        if(!audioSourceMusica) yield break;
        float startVolume = audioSourceMusica.volume;
        float rate = 1.0f / duracao;
        float progress = 0.0f;
        while (progress < 1.0f) {
            audioSourceMusica.volume = Mathf.Lerp(startVolume, 0, progress);
            progress += rate * Time.deltaTime;
            yield return null;
        }
        audioSourceMusica.Stop();
    }

    IEnumerator EfeitoDigitar(string frase, float velocidade, TextMeshProUGUI alvo)
    {
        alvo.text = "";
        if (audioSourceVoz && somDigitando) { audioSourceVoz.clip = somDigitando; audioSourceVoz.loop = false; audioSourceVoz.Play(); }
        foreach (char letra in frase.ToCharArray()) {
            alvo.text += letra;
            yield return new WaitForSeconds(velocidade);
        }
        if (audioSourceVoz) audioSourceVoz.Stop();
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateID() { uniqueID = System.Guid.NewGuid().ToString(); }
}
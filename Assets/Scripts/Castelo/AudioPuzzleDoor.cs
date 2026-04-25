using UnityEngine;
using TMPro; 
using System.Collections;
using System.Collections.Generic;

public class AudioPuzzleDoor : MonoBehaviour
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

    [Header("--- SAVE SYSTEM ---")]
    public string uniqueID; 
    public int idDessaCasa = 0; 

    [Header("--- CONFIGURAÇÃO DO PUZZLE ---")]
    public string senhaCorreta = "1234";
    public int idDoDiscoParaDar = 2;   
    
    [Tooltip("Dica em Português (Original)")]
    [TextArea] public string dicaApertarR = "[R] Repetir vozes";
    
    [Tooltip("Dica em Inglês")]
    [TextArea] public string dicaApertarREN = "[R] Repeat voices";

    [Header("--- ROTEIRO ---")]
    public List<LinhaDeDialogo> falasDaPorta; 

    [Header("--- UI ---")]
    public GameObject painelTelaPreta; 
    public TextMeshProUGUI textoDialogoCutscene; 
    public GameObject textoInteragir;  
    
    [Header("--- UI DA SENHA ---")]
    public GameObject painelSenha; 
    public TextMeshProUGUI textoVisorSenha; 
    public TextMeshProUGUI textoDicaR; 

    [Header("--- SONS ---")]
    public AudioSource audioSourceSFX;     
    public AudioSource audioSourceVoz;    
    public AudioSource audioSourceMusicaPorta;  
    public AudioClip somBatida; 
    public AudioClip somDigitandoTexto; 
    public AudioClip somTeclaSenha;
    public AudioClip somErroSenha;
    public AudioClip somSucesso;

    // Estado interno
    private bool portaResolvida = false;
    private bool jogadorNaPorta = false;
    private bool jaFezCutsceneInicial = false; 
    private bool dialogoTerminou = false; 
    private bool digitandoSenha = false;
    private string inputAtual = "";
    private Vector3 posicaoInicialInteracao; 
    private bool ignorarProximoInputE = false; // Trava de proteção pro E

    // Controle de Áudio Global
    private AudioSource musicaGlobal;
    private Coroutine transicaoAudioAtual;
    private float volumeGlobalOriginal = 0.5f;

    void Start()
    {
        if (string.IsNullOrEmpty(uniqueID)) 
            Debug.LogError($"[ERRO GRAVE] A porta '{gameObject.name}' não tem Unique ID! O Save não vai funcionar.");

        if (painelTelaPreta) painelTelaPreta.SetActive(false);
        if (textoInteragir) textoInteragir.SetActive(false);
        if (painelSenha) painelSenha.SetActive(false);
        
        if (textoDicaR) textoDicaR.gameObject.SetActive(false);

        GameObject stObj = GameObject.Find("Soundtrack");
        if (stObj != null) 
        {
            musicaGlobal = stObj.GetComponent<AudioSource>();
            if (musicaGlobal != null) volumeGlobalOriginal = musicaGlobal.volume;
        }

        CarregarEstadoSalvo();
    }

    void CarregarEstadoSalvo()
    {
        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            if (PersistenciaManager.Instance.ObterEstado(uniqueID)) 
                portaResolvida = true;
        }

        if (EstadoGlobal.casasResolvidas != null && idDessaCasa < EstadoGlobal.casasResolvidas.Length)
        {
             if (EstadoGlobal.casasResolvidas[idDessaCasa]) 
                 portaResolvida = true;
        }
    }

    public void AoOlhar()
    {
        if (portaResolvida || jogadorNaPorta) return;
        if (textoInteragir) textoInteragir.SetActive(true);
    }

    public void AoSair()
    {
        if (textoInteragir) textoInteragir.SetActive(false);
    }

    public void Interagir()
    {
        if (portaResolvida || jogadorNaPorta) return;

        if (FPS_Master.Instance != null)
            posicaoInicialInteracao = FPS_Master.Instance.transform.position;

        jogadorNaPorta = true;
        
        if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(true, false);
        
        if (textoInteragir) textoInteragir.SetActive(false);
        
        TrocarParaMusicaDaPorta();

        if (!dialogoTerminou) 
        {
            StartCoroutine(SequenciaCutscene(!jaFezCutsceneInicial)); 
        }
        else 
        {
            AbrirPainelSenha(true); 
        }
    }

    void Update()
    {
        if (!jogadorNaPorta || portaResolvida) return;

        if (ignorarProximoInputE)
        {
            ignorarProximoInputE = false;
            return;
        }

        // 🔥 Removido o Input.GetKeyDown(KeyCode.Escape) daqui
        if (Input.GetKeyDown(KeyCode.E)) 
        {
            SairDoPuzzle();
            return;
        }

        if (painelSenha.activeSelf && textoDicaR.gameObject.activeSelf && Input.GetKeyDown(KeyCode.R))
        {
            AbrirPainelSenha(false);
            StartCoroutine(SequenciaCutscene(false));
            return;
        }

        if (digitandoSenha)
        {
            CapturarTecladoNumerico();
        }
    }

    void CapturarTecladoNumerico()
    {
        string teclaPressionada = "";

        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0)) teclaPressionada = "0";
        else if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) teclaPressionada = "1";
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) teclaPressionada = "2";
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) teclaPressionada = "3";
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) teclaPressionada = "4";
        else if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5)) teclaPressionada = "5";
        else if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6)) teclaPressionada = "6";
        else if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7)) teclaPressionada = "7";
        else if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8)) teclaPressionada = "8";
        else if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9)) teclaPressionada = "9";
        else if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (inputAtual.Length > 0)
            {
                inputAtual = inputAtual.Substring(0, inputAtual.Length - 1);
                AtualizarVisorSenha();
            }
            return;
        }

        if (teclaPressionada != "" && inputAtual.Length < 4)
        {
            inputAtual += teclaPressionada;
            if (audioSourceSFX && somTeclaSenha) audioSourceSFX.PlayOneShot(somTeclaSenha);
            AtualizarVisorSenha();

            if (inputAtual.Length == 4)
            {
                StartCoroutine(VerificarSenhaDelay());
            }
        }
    }

    void AtualizarVisorSenha()
    {
        if (textoVisorSenha == null) return;

        string display = "";
        for (int i = 0; i < 4; i++)
        {
            if (i < inputAtual.Length) display += inputAtual[i] + " ";
            else display += "_ ";
        }
        textoVisorSenha.text = display.Trim();
    }

    IEnumerator VerificarSenhaDelay()
    {
        digitandoSenha = false; 
        yield return new WaitForSeconds(0.3f); 

        if (inputAtual == senhaCorreta)
        {
            ResolverPuzzle();
        }
        else
        {
            if (audioSourceSFX && somErroSenha) audioSourceSFX.PlayOneShot(somErroSenha);
            textoVisorSenha.color = Color.red;
            yield return new WaitForSeconds(0.6f);
            
            inputAtual = "";
            textoVisorSenha.color = Color.white;
            AtualizarVisorSenha();
            SairDoPuzzle(); 
        }
    }

    IEnumerator SequenciaCutscene(bool comBatida)
    {
        if (textoDialogoCutscene) textoDialogoCutscene.text = ""; 
        
        dialogoTerminou = false;
        jaFezCutsceneInicial = true; 
        
        painelSenha.SetActive(false);
        painelTelaPreta.SetActive(true);

        if (comBatida && audioSourceSFX && somBatida) 
        {
            audioSourceSFX.PlayOneShot(somBatida);
            yield return new WaitForSeconds(1.5f);
        }

        if (falasDaPorta != null) yield return StartCoroutine(TocarLista(falasDaPorta));

        if (textoDialogoCutscene) textoDialogoCutscene.text = ""; 
        painelTelaPreta.SetActive(false);

        dialogoTerminou = true;
        AbrirPainelSenha(true); 
    }

    void AbrirPainelSenha(bool mostrarDicaR)
    {
        inputAtual = "";
        AtualizarVisorSenha();
        painelSenha.SetActive(true);
        textoVisorSenha.color = Color.white;
        
        if (textoDicaR)
        {
            int lang = (LanguageManager.Instance != null) ? LanguageManager.Instance.currentLanguage : 0;
            string dicaParaExibir = (lang == 0) ? dicaApertarR : dicaApertarREN;
            
            if (lang == 1 && string.IsNullOrEmpty(dicaParaExibir)) dicaParaExibir = dicaApertarR;

            textoDicaR.text = dicaParaExibir;
            textoDicaR.gameObject.SetActive(mostrarDicaR);
        }

        digitandoSenha = true;
    }

    void ResolverPuzzle()
    {
        portaResolvida = true;
        if (audioSourceSFX && somSucesso) audioSourceSFX.PlayOneShot(somSucesso);
        textoVisorSenha.color = Color.green;

        if (InventoryManager.Instance != null) 
            InventoryManager.Instance.ReceberItem(idDoDiscoParaDar);
        else if (EstadoGlobal.armasDesbloqueadas != null && idDoDiscoParaDar < EstadoGlobal.armasDesbloqueadas.Length)
            EstadoGlobal.armasDesbloqueadas[idDoDiscoParaDar] = true;

        if (EstadoGlobal.casasResolvidas != null && idDessaCasa < EstadoGlobal.casasResolvidas.Length) 
            EstadoGlobal.casasResolvidas[idDessaCasa] = true;

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID)) 
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);

        if (SistemaGlobal.Instance != null)
        {
            EstadoGlobal.SalvarNoSlot(SistemaGlobal.Instance.slotAtual);
            if (PersistenciaManager.Instance) PersistenciaManager.Instance.SalvarTudo();
        }

        StartCoroutine(SairDoPuzzleDelay());
    }

    IEnumerator SairDoPuzzleDelay()
    {
        yield return new WaitForSeconds(1.5f);
        SairDoPuzzle();
    }

    void SairDoPuzzle()
    {
        StopAllCoroutines(); 
        
        painelTelaPreta.SetActive(false);
        painelSenha.SetActive(false);
        if (textoDialogoCutscene) textoDialogoCutscene.text = "";
        
        jogadorNaPorta = false;
        digitandoSenha = false;
        
        TrocarParaMusicaGlobal();
        
        if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(false, false);
        
        ignorarProximoInputE = true; 
        
        if (FPS_Master.Instance != null) FPS_Master.Instance.LimparVisual();
    }

    void TrocarParaMusicaDaPorta()
    {
        if (transicaoAudioAtual != null) StopCoroutine(transicaoAudioAtual);
        transicaoAudioAtual = StartCoroutine(FadeAudio(musicaGlobal, audioSourceMusicaPorta));
    }

    void TrocarParaMusicaGlobal()
    {
        if (transicaoAudioAtual != null) StopCoroutine(transicaoAudioAtual);
        transicaoAudioAtual = StartCoroutine(FadeAudio(audioSourceMusicaPorta, musicaGlobal));
    }

    IEnumerator FadeAudio(AudioSource musicaSaindo, AudioSource musicaEntrando)
    {
        float fadeTime = 1.0f;
        float timer = 0f;

        if (musicaSaindo != null && musicaSaindo.isPlaying)
        {
            float startVolume = musicaSaindo.volume;
            while (timer < fadeTime)
            {
                musicaSaindo.volume = Mathf.Lerp(startVolume, 0f, timer / fadeTime);
                timer += Time.deltaTime;
                yield return null;
            }
            musicaSaindo.volume = 0f;
            musicaSaindo.Pause(); 
        }

        timer = 0f;

        if (musicaEntrando != null)
        {
            float targetVolume = (musicaEntrando == musicaGlobal) ? volumeGlobalOriginal : 1f; 
            
            musicaEntrando.volume = 0f;
            if (!musicaEntrando.isPlaying) musicaEntrando.UnPause(); 
            if (!musicaEntrando.isPlaying) musicaEntrando.Play(); 
            
            while (timer < fadeTime)
            {
                musicaEntrando.volume = Mathf.Lerp(0f, targetVolume, timer / fadeTime);
                timer += Time.deltaTime;
                yield return null;
            }
            musicaEntrando.volume = targetVolume;
        }
    }

    IEnumerator EfeitoDigitar(string frase, float velocidade)
    {
        if(textoDialogoCutscene) textoDialogoCutscene.text = "";
        
        // 🔥 CORREÇÃO: loop = true para tocar sem parar até a frase acabar
        if (audioSourceVoz && somDigitandoTexto) 
        { 
            audioSourceVoz.clip = somDigitandoTexto; 
            audioSourceVoz.loop = true; 
            audioSourceVoz.Play(); 
        }
        
        foreach (char letra in frase.ToCharArray()) {
            if(textoDialogoCutscene) textoDialogoCutscene.text += letra;
            yield return new WaitForSeconds(velocidade); 
        }
        
        if (audioSourceVoz) audioSourceVoz.Stop();
    }

    IEnumerator TocarLista(List<LinhaDeDialogo> lista)
    {
        int lang = (LanguageManager.Instance != null) ? LanguageManager.Instance.currentLanguage : 0;

        foreach (LinhaDeDialogo linha in lista)
        {
            if (textoDialogoCutscene) textoDialogoCutscene.fontSize = linha.tamanhoDaFonte;

            string textoParaExibir = (lang == 0) ? linha.texto : linha.textoEN;
            if (lang == 1 && string.IsNullOrEmpty(textoParaExibir)) textoParaExibir = linha.texto;

            yield return StartCoroutine(EfeitoDigitar(textoParaExibir, linha.velocidadeDigitar));
            yield return new WaitForSeconds(linha.tempoDeEsperaApos);
        }
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateID() { uniqueID = System.Guid.NewGuid().ToString(); }
}
using UnityEngine;
using TMPro; 
using System.Collections;
using System.Collections.Generic;

public class ChildPuzzleDoor : MonoBehaviour
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

    [Header("Save System")]
    public string uniqueID; 

    [Header("Config")]
    public int idDessaCasa = 1; 
    public string senhaCorreta = "23195"; 
    
    [Header("--- RECOMPENSA (HUD) ---")]
    public bool darChaveAoResolver = true;
    public string idChaveRecompensa = "Chave_Porao";
    public GameObject imagemChaveNoInventario;

    [Header("Roteiro")]
    public List<LinhaDeDialogo> introducao; 
    public List<LinhaDeDialogo> poemaDoFantasma; 
    public List<LinhaDeDialogo> complemento; 
    
    [Header("--- PAINEIS FEEDBACK ---")]
    [Tooltip("Objeto que aparece quando ACERTA a senha")]
    public GameObject objetoAcertou; 
    
    [Tooltip("Objeto que aparece quando ERRA a senha")]
    public GameObject objetoErrou; 

    [Header("UI GERAL")]
    public GameObject painelTelaPreta; 
    public TextMeshProUGUI textoDialogoCutscene; 
    public GameObject textoInteragir; 
    public GameObject painelSenha; 
    public TextMeshProUGUI textoDisplaySenha; 
    public GameObject textoAvisoBotaoR; 

    [Header("Sons")]
    public AudioSource audioSourceSFX;     
    public AudioSource audioSourceVoz;     
    public AudioSource audioSourceMusica;  
    public AudioClip somBatida; 
    public AudioClip somPortaAbrindo;
    public AudioClip somDigitando; 
    public AudioClip somTeclaSenha; 
    public AudioClip somErro; 
    public AudioClip somSucesso; 
    public AudioClip musicaTensa;

    private bool emCena = false; 
    private bool jaViuIntroducao = false; 
    private bool casaResolvida = false;
    private string senhaAtual = "";
    private bool digitandoSenha = false;
    
    private Vector3 posicaoInicialInteracao;

    void Start()
    {
        if (string.IsNullOrEmpty(uniqueID)) Debug.LogError($"[ERRO] Porta {gameObject.name} sem Unique ID!");

        if (painelTelaPreta) painelTelaPreta.SetActive(false);
        if (painelSenha) painelSenha.SetActive(false);
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoDialogoCutscene) textoDialogoCutscene.text = "";
        if (textoDisplaySenha) textoDisplaySenha.text = "";
        
        if (objetoAcertou) objetoAcertou.SetActive(false);
        if (objetoErrou) objetoErrou.SetActive(false);

        CarregarEstadoSalvo();

        if (imagemChaveNoInventario)
        {
            imagemChaveNoInventario.SetActive(casaResolvida);
        }
    }

    void CarregarEstadoSalvo()
    {
        if (Application.isEditor) return; 

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID)) {
             if(PersistenciaManager.Instance.ObterEstado(uniqueID)) casaResolvida = true;
        }
        if (EstadoGlobal.casasResolvidas != null && idDessaCasa < EstadoGlobal.casasResolvidas.Length)
        {
             if (EstadoGlobal.casasResolvidas[idDessaCasa]) casaResolvida = true;
        }
    }

    public void Interagir()
    {
        if (casaResolvida || emCena) return;

        if (textoInteragir) textoInteragir.SetActive(false);

        if (FPS_Master.Instance != null)
        {
            posicaoInicialInteracao = FPS_Master.Instance.transform.position;
        }

        if (!jaViuIntroducao) 
        {
            StartCoroutine(SequenciaPrimeiraVez());
        }
        else 
        {
            AtivarModoSenha(true);
        }
    }

    void Update()
    {
        if (casaResolvida) return;
        if (digitandoSenha) ProcessarEntradaSenha();
    }

    void TravarPlayer(bool travar)
    {
        if (FPS_Master.Instance != null)
        {
            FPS_Master.Instance.AlterarEstadoJogador(travar, false);
            if (travar)
            {
                CharacterController cc = FPS_Master.Instance.GetComponent<CharacterController>();
                if (cc != null) cc.Move(Vector3.zero); 
            }
        }
    }

    void RetornarPosicaoSegura()
    {
        if (FPS_Master.Instance != null)
        {
            FPS_Master.Instance.Teleportar(posicaoInicialInteracao);
            Physics.SyncTransforms();
        }
    }

    IEnumerator SequenciaPrimeiraVez()
    {
        emCena = true;
        jaViuIntroducao = true; 
        TravarPlayer(true); 

        if (textoInteragir) textoInteragir.SetActive(false);
        if (painelTelaPreta) painelTelaPreta.SetActive(true);
        if (textoDialogoCutscene) { textoDialogoCutscene.text = ""; textoDialogoCutscene.color = Color.white; }
        if (painelSenha) painelSenha.SetActive(false);
        if (textoAvisoBotaoR) textoAvisoBotaoR.SetActive(false);

        if (audioSourceMusica) { audioSourceMusica.clip = musicaTensa; audioSourceMusica.Play(); }
        
        if(audioSourceSFX) audioSourceSFX.PlayOneShot(somBatida);
        yield return new WaitForSeconds(1f);
        if(audioSourceSFX) audioSourceSFX.PlayOneShot(somBatida);
        yield return new WaitForSeconds(1f);
        if(audioSourceSFX) audioSourceSFX.PlayOneShot(somPortaAbrindo);
        yield return new WaitForSeconds(1f);
        
        yield return StartCoroutine(TocarLista(introducao));
        yield return StartCoroutine(TocarLista(poemaDoFantasma));
        yield return StartCoroutine(TocarLista(complemento));

        if (textoDialogoCutscene) textoDialogoCutscene.text = ""; 
        
        AtivarModoSenha(false); 
    }

    IEnumerator ReverPoema()
    {
        painelSenha.SetActive(false);
        if (textoAvisoBotaoR) textoAvisoBotaoR.SetActive(false);
        if (textoDialogoCutscene) { textoDialogoCutscene.text = ""; textoDialogoCutscene.color = Color.white; }
        
        yield return StartCoroutine(TocarLista(poemaDoFantasma));
        
        if (textoDialogoCutscene) textoDialogoCutscene.text = "";
        
        AtivarModoSenha(true);
    }

    void AtivarModoSenha(bool mostrarAvisoR)
    {
        emCena = true;
        TravarPlayer(true); 
        if (textoInteragir) textoInteragir.SetActive(false);

        if (painelTelaPreta) painelTelaPreta.SetActive(true); 
        if (textoDialogoCutscene) textoDialogoCutscene.text = ""; 

        if (painelSenha) painelSenha.SetActive(true);
        if (textoAvisoBotaoR) textoAvisoBotaoR.SetActive(mostrarAvisoR);
        
        if (objetoAcertou) objetoAcertou.SetActive(false);
        if (objetoErrou) objetoErrou.SetActive(false);
        
        senhaAtual = "";
        digitandoSenha = true; 
        
        AtualizarTextoDisplay(); 
        if (textoDisplaySenha) textoDisplaySenha.ForceMeshUpdate(); 
    }

    void ProcessarEntradaSenha()
    {
        if (Input.GetKeyDown(KeyCode.R)) { digitandoSenha = false; StartCoroutine(ReverPoema()); return; }
        if (Input.GetKeyDown(KeyCode.Escape)) { SairDaInteracao(); return; }

        string num = "";
        for (int i = 0; i <= 9; i++) {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i)) num = i.ToString();
        }

        if (num != "" && senhaAtual.Length < 5)
        {
            senhaAtual += num;
            if(audioSourceSFX) audioSourceSFX.PlayOneShot(somTeclaSenha);
            AtualizarTextoDisplay();
            if (senhaAtual.Length == 5) VerificarSenha();
        }
        
        if (Input.GetKeyDown(KeyCode.Backspace) && senhaAtual.Length > 0)
        {
            senhaAtual = senhaAtual.Substring(0, senhaAtual.Length - 1);
            AtualizarTextoDisplay();
        }
    }

    void AtualizarTextoDisplay()
    {
        if(textoDisplaySenha == null) return;
        string display = "";
        for (int i = 0; i < 5; i++) {
            if (i < senhaAtual.Length) display += senhaAtual[i]; else display += "_"; 
            if (i < 4) display += "  "; 
        }
        textoDisplaySenha.text = display;
    }

    void VerificarSenha()
    {
        if (senhaAtual == senhaCorreta) StartCoroutine(SequenciaVitoria());
        else StartCoroutine(SequenciaErro());
    }

    IEnumerator SequenciaErro()
    {
        digitandoSenha = false;

        if (objetoErrou) objetoErrou.SetActive(true);
        if(audioSourceSFX) audioSourceSFX.PlayOneShot(somErro);
        
        senhaAtual = "";
        AtualizarTextoDisplay(); 
        
        yield return new WaitForSeconds(2.0f);
        SairDaInteracao(); 
    }

    IEnumerator SequenciaVitoria()
    {
        digitandoSenha = false;

        if (objetoAcertou) objetoAcertou.SetActive(true);
        if(audioSourceSFX) audioSourceSFX.PlayOneShot(somSucesso);

        if (darChaveAoResolver)
        {
            KeySystem.AdicionarChave(idChaveRecompensa);
            if (imagemChaveNoInventario != null) imagemChaveNoInventario.SetActive(true);
        }

        casaResolvida = true;

        if (!Application.isEditor)
        {
            if (EstadoGlobal.casasResolvidas != null && idDessaCasa < EstadoGlobal.casasResolvidas.Length) 
                EstadoGlobal.casasResolvidas[idDessaCasa] = true;

            if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
                PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);

            // 🔥 TRAVA DE SAVE DE AÇO
            if (SistemaGlobal.Instance != null)
            {
                EstadoGlobal.SalvarNoSlot(SistemaGlobal.Instance.slotAtual);
                if (PersistenciaManager.Instance) PersistenciaManager.Instance.SalvarTudo();
            }
        }

        yield return new WaitForSeconds(3.0f);
        SairDaInteracao();
    }

    void SairDaInteracao()
    {
        digitandoSenha = false;
        emCena = false;
        if (painelSenha) painelSenha.SetActive(false);
        if (painelTelaPreta) painelTelaPreta.SetActive(false);
        if (textoDialogoCutscene) textoDialogoCutscene.text = ""; 
        if (audioSourceMusica) audioSourceMusica.Stop(); 
        if (textoAvisoBotaoR) textoAvisoBotaoR.SetActive(false);
        
        if (objetoAcertou) objetoAcertou.SetActive(false);
        if (objetoErrou) objetoErrou.SetActive(false);
        
        RetornarPosicaoSegura();
        TravarPlayer(false); 
    }
    
    IEnumerator TocarLista(List<LinhaDeDialogo> lista)
    {
        int lang = (LanguageManager.Instance != null) ? LanguageManager.Instance.currentLanguage : 0;

        foreach (LinhaDeDialogo linha in lista) {
            if (textoDialogoCutscene) textoDialogoCutscene.fontSize = linha.tamanhoDaFonte;

            string textoParaExibir = (lang == 0) ? linha.texto : linha.textoEN;
            if (lang == 1 && string.IsNullOrEmpty(textoParaExibir)) textoParaExibir = linha.texto;

            yield return StartCoroutine(EfeitoDigitar(textoParaExibir, linha.velocidadeDigitar));
            yield return new WaitForSeconds(linha.tempoDeEsperaApos);
        }
    }

    IEnumerator EfeitoDigitar(string frase, float velocidade)
    {
        if (textoDialogoCutscene) { textoDialogoCutscene.text = ""; textoDialogoCutscene.color = Color.white; }
        
        if (audioSourceVoz && somDigitando) { audioSourceVoz.clip = somDigitando; audioSourceVoz.Play(); }
        
        foreach (char letra in frase.ToCharArray()) {
            if (textoDialogoCutscene) textoDialogoCutscene.text += letra;
            yield return new WaitForSeconds(velocidade);
        }
        if (audioSourceVoz) audioSourceVoz.Stop();
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateID() { uniqueID = System.Guid.NewGuid().ToString(); }
    public void AoOlhar() { if (!casaResolvida && !emCena && textoInteragir) textoInteragir.SetActive(true); }
    public void AoSair() { if (textoInteragir) textoInteragir.SetActive(false); }
}
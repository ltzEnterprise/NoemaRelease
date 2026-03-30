using UnityEngine;
using TMPro; 
using System.Collections;
using System.Collections.Generic;

public class TroubledSoulDoor : MonoBehaviour
{
    [System.Serializable]
    public class LinhaDeDialogo
    {
        [TextArea] public string texto;
        public float tamanhoDaFonte = 36f; 
        public float velocidadeDigitar = 0.04f;
        public float tempoDeEsperaApos = 1.5f;
    }

    [Header("Save System")]
    public string uniqueID; 

    [Header("Identificação")]
    public int idDessaCasa = 0; 
    public int idDaPaNoInventario = 1; 
    public int idDoDiscoParaDar = 2;   

    [Header("Roteiro")]
    public List<LinhaDeDialogo> falasLoucura; 
    public List<LinhaDeDialogo> falasComPa; 
    [TextArea] public string mensagemEntregaPa = "Você entregou a pá para a alma.";
    [TextArea] public string mensagemDeuses = "Os deuses te presentearam com um disco musical.";

    [Header("UI")]
    public GameObject painelTelaPreta; 
    public TextMeshProUGUI textoDialogoCutscene; 
    public GameObject textoInteragir;  
    public GameObject objetoDicaSemPa; 

    [Header("Sons")]
    public AudioSource audioSourceSFX;     
    public AudioSource audioSourceVoz;     
    public AudioSource audioSourceMusica;  
    public AudioClip somBatidaUnico; 
    public AudioClip somDigitando; 
    public AudioClip somPortaAbrindo;
    public AudioClip somPortaFechando;
    public AudioClip somImpactoDeuses; 
    public AudioClip musicaTensa;

    // Estado interno
    private bool emCena = false;
    private bool casaJaResolvida = false;
    private bool jaFezCutscene = false;
    private Vector3 posicaoInicialInteracao; 

    void Start()
    {
        // 1. Validar ID
        if (string.IsNullOrEmpty(uniqueID)) 
        {
            Debug.LogError($"[ERRO GRAVE] A porta '{gameObject.name}' não tem Unique ID! O Save não vai funcionar.");
        }

        // 2. Esconder UI inicial
        if (painelTelaPreta) painelTelaPreta.SetActive(false);
        if (textoInteragir) textoInteragir.SetActive(false);
        if (objetoDicaSemPa) objetoDicaSemPa.SetActive(false);

        // 3. Carregar Save (PRIORIDADE TOTAL)
        CarregarEstadoSalvo();
    }

    void CarregarEstadoSalvo()
    {
        // Verifica no PersistenciaManager (Pelo ID único da porta)
        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            if (PersistenciaManager.Instance.ObterEstado(uniqueID)) 
            {
                casaJaResolvida = true;
                Debug.Log($"[Save] Porta {uniqueID} carregada como RESOLVIDA.");
            }
        }

        // Verifica no EstadoGlobal (Pelo Index do Array - Método Antigo/Backup)
        if (EstadoGlobal.casasResolvidas != null && idDessaCasa < EstadoGlobal.casasResolvidas.Length)
        {
             if (EstadoGlobal.casasResolvidas[idDessaCasa]) 
             {
                 casaJaResolvida = true;
             }
        }
    }

    // --- INTERAÇÃO ---

    public void AoOlhar()
    {
        if (casaJaResolvida || emCena) return;
        if (textoInteragir) textoInteragir.SetActive(true);
    }

    public void AoSair()
    {
        if (textoInteragir) textoInteragir.SetActive(false);
    }

    public void Interagir()
    {
        if (casaJaResolvida || emCena) return;

        // Salva posição para retorno seguro
        if (FPS_Master.Instance != null)
        {
            posicaoInicialInteracao = FPS_Master.Instance.transform.position;
        }

        // Verifica Inventário
        bool temPa = false;
        if (EstadoGlobal.armasDesbloqueadas != null && idDaPaNoInventario < EstadoGlobal.armasDesbloqueadas.Length)
        {
            temPa = EstadoGlobal.armasDesbloqueadas[idDaPaNoInventario];
        }

        if (temPa) StartCoroutine(SequenciaEntregaImediata()); 
        else
        {
            if (!jaFezCutscene) StartCoroutine(SequenciaSustoSemPa()); 
            else StartCoroutine(MostrarDicaRapida()); 
        }
    }

    // --- COROUTINES (Cutscenes) ---

    IEnumerator SequenciaEntregaImediata()
    {
        emCena = true;
        TravarPlayer(true);
        if (textoInteragir) textoInteragir.SetActive(false);
        painelTelaPreta.SetActive(true);
        if (audioSourceMusica) audioSourceMusica.Stop();

        if (falasComPa != null) yield return StartCoroutine(TocarLista(falasComPa));

        // Remove item e entrega recompensa
        if (InventoryManager.Instance != null) 
        InventoryManager.Instance.ConsumirItem(idDaPaNoInventario);
        
        if(textoDialogoCutscene) textoDialogoCutscene.text = mensagemEntregaPa;
        yield return new WaitForSeconds(3.0f);
        if(textoDialogoCutscene) textoDialogoCutscene.text = "";
        
        yield return new WaitForSeconds(1.0f); 
        if(audioSourceSFX && somImpactoDeuses) audioSourceSFX.PlayOneShot(somImpactoDeuses);
        if(textoDialogoCutscene) textoDialogoCutscene.text = mensagemDeuses;
        
        // SALVA O JOGO AQUI
        FinalizarMissao();

        yield return new WaitForSeconds(4.0f);
        painelTelaPreta.SetActive(false);
        emCena = false;
        
        RetornarPosicaoSegura();
        TravarPlayer(false);
    }

    IEnumerator SequenciaSustoSemPa()
    {
        emCena = true;
        jaFezCutscene = true; 
        TravarPlayer(true);
        if (textoInteragir) textoInteragir.SetActive(false);
        painelTelaPreta.SetActive(true);

        if (audioSourceMusica) { audioSourceMusica.clip = musicaTensa; audioSourceMusica.Play(); }

        // Sequencia de batidas
        string[] sustos = { "VOCÊ", "BATEU", "NA PORTA" };
        foreach(string s in sustos) {
            if(textoDialogoCutscene) textoDialogoCutscene.text = s;
            if(audioSourceSFX) audioSourceSFX.PlayOneShot(somBatidaUnico);
            yield return new WaitForSeconds(0.8f);
        }
        yield return new WaitForSeconds(0.7f);

        if(textoDialogoCutscene) textoDialogoCutscene.text = "";
        if(audioSourceSFX) audioSourceSFX.PlayOneShot(somPortaAbrindo);
        yield return new WaitForSeconds(1.0f);

        if (falasLoucura != null) yield return StartCoroutine(TocarLista(falasLoucura));

        if (audioSourceMusica) audioSourceMusica.Stop();
        if(audioSourceSFX) audioSourceSFX.PlayOneShot(somPortaFechando);
        if(textoDialogoCutscene) textoDialogoCutscene.text = ""; 
        yield return new WaitForSeconds(1.0f);

        painelTelaPreta.SetActive(false);
        emCena = false;
        RetornarPosicaoSegura();
        TravarPlayer(false);
    }

    IEnumerator MostrarDicaRapida()
    {
        emCena = true;
        TravarPlayer(true);
        if (textoInteragir) textoInteragir.SetActive(false);
        if (objetoDicaSemPa) objetoDicaSemPa.SetActive(true);
        yield return new WaitForSeconds(3.0f);
        if (objetoDicaSemPa) objetoDicaSemPa.SetActive(false);
        emCena = false;
        RetornarPosicaoSegura();
        TravarPlayer(false);
    }

    // --- UTILITÁRIOS ---

    void FinalizarMissao()
    {
        casaJaResolvida = true;

        // 1. Atualiza Array Global (Backup)
        if (EstadoGlobal.casasResolvidas != null && idDessaCasa < EstadoGlobal.casasResolvidas.Length) 
            EstadoGlobal.casasResolvidas[idDessaCasa] = true;
        
        // 2. Dá o item
        if (InventoryManager.Instance != null) 
        InventoryManager.Instance.ConsumirItem(idDaPaNoInventario);

        // 3. SALVA NO DISCO (Principal)
        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID)) 
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);
            // O PersistenciaManager deve ter um método Save() ou salvar ao registrar. 
            // Se não tiver, chame PersistenciaManager.Instance.Save() aqui se existir.
        }
    }

    void TravarPlayer(bool travar)
    {
        if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(travar, false);
    }

    void RetornarPosicaoSegura()
    {
        if (FPS_Master.Instance != null) FPS_Master.Instance.Teleportar(posicaoInicialInteracao);
    }

    IEnumerator EfeitoDigitar(string frase, float velocidade)
    {
        if(textoDialogoCutscene) textoDialogoCutscene.text = "";
        if (audioSourceVoz && somDigitando) { audioSourceVoz.clip = somDigitando; audioSourceVoz.loop = false; audioSourceVoz.Play(); }
        foreach (char letra in frase.ToCharArray()) {
            if(textoDialogoCutscene) textoDialogoCutscene.text += letra;
            yield return new WaitForSeconds(velocidade); 
        }
        if (audioSourceVoz) audioSourceVoz.Stop();
    }

    IEnumerator TocarLista(List<LinhaDeDialogo> lista)
    {
        foreach (LinhaDeDialogo linha in lista)
        {
            if (textoDialogoCutscene) textoDialogoCutscene.fontSize = linha.tamanhoDaFonte;
            yield return StartCoroutine(EfeitoDigitar(linha.texto, linha.velocidadeDigitar));
            yield return new WaitForSeconds(linha.tempoDeEsperaApos);
        }
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateID() { uniqueID = System.Guid.NewGuid().ToString(); }
}
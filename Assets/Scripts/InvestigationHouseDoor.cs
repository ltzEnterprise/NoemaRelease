using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class InvestigationHouseDoor : MonoBehaviour
{
    [Header("--- CONFIGURAÇÃO DE CENA ---")]
    public string nomeDaCenaParaCarregar = "InvestigacaoCena";
    
    [Header("--- CONFIGURAÇÃO DE SPAWN (Ao voltar) ---")]
    [Tooltip("Arraste um Empty Object que fica NA FRENTE desta porta.")]
    public Transform pontoDeRetorno; 

    [Header("--- REQUISITOS (CHAVE E BARRICADA) ---")]
    public string idChaveNecessaria = "Chave_Casa_Noite";
    public GameObject hudIconeChaveParaApagar; 
    public List<GameObject> madeirasBloqueio; 

    [Header("--- UI E EFEITOS ---")]
    [Tooltip("O texto 'Pressione E para Entrar'")]
    public GameObject textoInteragirUI; // <--- O NOVO TEXTO AQUI
    public GameObject textoSemChaveUI;  // "Preciso de uma chave..."
    
    public AudioSource audioSource;
    public AudioClip somTrancada;
    public AudioClip somDestrancar;
    public AudioClip somAbrirPorta;

    // Estado interno
    private bool interagindo = false;

    void Start()
    {
        if (textoSemChaveUI) textoSemChaveUI.SetActive(false);
        if (textoInteragirUI) textoInteragirUI.SetActive(false);
        
        // Garante que o AudioSource exista
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
    }

    // --- SISTEMA DE MIRA (SÓ MOSTRA TEXTO SE NÃO TIVER MADEIRA) ---
    public void AoOlhar()
    {
        if (interagindo) return;

        // Se tiver madeira na frente, NÃO mostra o texto de "Entrar"
        // Isso força o jogador a entender que tem que quebrar a madeira primeiro
        if (TemMadeiraBloqueando()) 
        {
            if (textoInteragirUI) textoInteragirUI.SetActive(false);
            return;
        }

        // Se está livre, mostra o texto
        if (textoInteragirUI) textoInteragirUI.SetActive(true);
    }

    public void AoSair()
    {
        if (textoInteragirUI) textoInteragirUI.SetActive(false);
    }
    // -------------------------------------------------------------

    public void Interagir()
    {
        if (interagindo) return;

        // 1. VERIFICA BARRICADA
        if (TemMadeiraBloqueando()) return; 

        // 2. VERIFICA CHAVE
        if (KeySystem.TemChave(idChaveNecessaria))
        {
            StartCoroutine(EntrarNaCasa());
        }
        else
        {
            StartCoroutine(MostrarMensagemSemChave());
        }
    }

    bool TemMadeiraBloqueando()
    {
        foreach (GameObject madeira in madeirasBloqueio)
        {
            if (madeira != null && madeira.activeInHierarchy) return true;
        }
        return false;
    }

    IEnumerator EntrarNaCasa()
    {
        interagindo = true;
        
        // Esconde o texto de interagir imediatamente
        if (textoInteragirUI) textoInteragirUI.SetActive(false);

        FPS_Master.travadoInteracao = true;

        // 1. CONSOME A CHAVE E ATUALIZA HUD
        KeySystem.GastarChave(idChaveNecessaria);
        if (hudIconeChaveParaApagar) hudIconeChaveParaApagar.SetActive(false);

        // 2. SOM DE DESTRANCAR
        if (audioSource && somDestrancar) audioSource.PlayOneShot(somDestrancar);
        yield return new WaitForSeconds(0.5f);

        // 3. SOM DE ABRIR
        if (audioSource && somAbrirPorta) audioSource.PlayOneShot(somAbrirPorta);

        // 4. SALVA TUDO
        if (SistemaGlobal.Instance != null)
        {
            EstadoGlobal.SalvarNoSlot(SistemaGlobal.Instance.slotAtual);
            
            if(pontoDeRetorno != null)
                SistemaGlobal.Instance.SalvarJogo(pontoDeRetorno.position, SceneManager.GetActiveScene().name);
        }

        yield return new WaitForSeconds(1f); 

        // 5. CARREGA A CENA
        SceneManager.LoadScene(nomeDaCenaParaCarregar);
    }

    IEnumerator MostrarMensagemSemChave()
    {
        // Garante que o texto de interagir suma pra mostrar o de erro
        if (textoInteragirUI) textoInteragirUI.SetActive(false);

        if (textoSemChaveUI) textoSemChaveUI.SetActive(true);
        if (audioSource && somTrancada) audioSource.PlayOneShot(somTrancada);
        
        yield return new WaitForSeconds(2.5f);
        
        if (textoSemChaveUI) textoSemChaveUI.SetActive(false);
    }
}
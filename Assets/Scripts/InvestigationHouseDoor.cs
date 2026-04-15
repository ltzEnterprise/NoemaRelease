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
    public GameObject textoInteragirUI; 
    public GameObject textoSemChaveUI;  
    
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
        
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void AoOlhar()
    {
        if (interagindo) return;

        if (TemMadeiraBloqueando()) 
        {
            if (textoInteragirUI) textoInteragirUI.SetActive(false);
            return;
        }

        if (textoInteragirUI) textoInteragirUI.SetActive(true);
    }

    public void AoSair()
    {
        if (textoInteragirUI) textoInteragirUI.SetActive(false);
    }

    public void Interagir()
    {
        if (interagindo) return;

        if (TemMadeiraBloqueando()) return; 

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
        
        if (textoInteragirUI) textoInteragirUI.SetActive(false);

        if (FPS_Master.Instance != null) FPS_Master.travadoInteracao = true;

        // 1. CONSOME A CHAVE E ATUALIZA HUD (O KeySystem já salva isso no JSON pra gente)
        KeySystem.GastarChave(idChaveNecessaria);
        if (hudIconeChaveParaApagar) hudIconeChaveParaApagar.SetActive(false);

        // 2. SOM DE DESTRANCAR
        if (audioSource && somDestrancar) audioSource.PlayOneShot(somDestrancar);
        yield return new WaitForSeconds(0.5f);

        // 3. SOM DE ABRIR
        if (audioSource && somAbrirPorta) audioSource.PlayOneShot(somAbrirPorta);

        // 4. SALVA TUDO
        if (SistemaGlobal.Instance != null && pontoDeRetorno != null)
        {
            // O seu novo SistemaGlobal já salva TUDO (Player, Inventario, EstadoGlobal) numa tacada só.
            SistemaGlobal.Instance.SalvarJogo(pontoDeRetorno.position, SceneManager.GetActiveScene().name);
        }

        yield return new WaitForSeconds(1f); 

        // 5. CARREGA A CENA
        SceneManager.LoadScene(nomeDaCenaParaCarregar);
    }

    IEnumerator MostrarMensagemSemChave()
    {
        if (textoInteragirUI) textoInteragirUI.SetActive(false);

        if (textoSemChaveUI) textoSemChaveUI.SetActive(true);
        if (audioSource && somTrancada) audioSource.PlayOneShot(somTrancada);
        
        yield return new WaitForSeconds(2.5f);
        
        if (textoSemChaveUI) textoSemChaveUI.SetActive(false);
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class InvestigationHouseDoor : MonoBehaviour
{
    [Header("--- SAVE SYSTEM ---")]
    [Tooltip("EXTREMAMENTE IMPORTANTE! Ex: Porta_Investigacao_Floresta")]
    public string uniqueID; 

    [Header("--- CONFIGURAÇÃO DE CENA ---")]
    public string nomeDaCenaParaCarregar = "InvestigacaoCena";
    
    [Header("--- CONFIGURAÇÃO DE SPAWN (Ao voltar) ---")]
    public Transform pontoDeRetorno; 

    [Header("--- REQUISITOS (CHAVE E BARRICADA) ---")]
    public string idChaveNecessaria = "Chave_Casa_Noite";
    public GameObject hudIconeChaveParaApagar; 
    public List<GameObject> madeirasBloqueio; 

    [Header("--- UI E EFEITOS ---")]
    public GameObject textoInteragirUI; 
    public GameObject textoSemChaveUI;  
    
    public AudioSource audioSource;
    public AudioClip somTrancada;
    public AudioClip somDestrancar;
    public AudioClip somAbrirPorta;

    private bool interagindo = false;
    private bool estaDestrancadaPraSempre = false; 
    private bool inicializado = false;

    void Start()
    {
        if (textoSemChaveUI) textoSemChaveUI.SetActive(false);
        if (textoInteragirUI) textoInteragirUI.SetActive(false);
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();

        StartCoroutine(CarregarEstadoSeguro());
    }

    IEnumerator CarregarEstadoSeguro()
    {
        if (PersistenciaManager.Instance != null)
            yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            if (PersistenciaManager.Instance.ObterEstado(uniqueID, false))
                estaDestrancadaPraSempre = true;
        }

        inicializado = true;
    }

    public void AoOlhar()
    {
        if (!inicializado) return;
        if (interagindo) return;

        if (TemMadeiraBloqueando()) 
        {
            if (textoInteragirUI)
                textoInteragirUI.SetActive(false);

            return;
        }

        if (textoInteragirUI)
            textoInteragirUI.SetActive(true);
    }

    public void AoSair()
    {
        if (textoInteragirUI)
            textoInteragirUI.SetActive(false);
    }

    public void Interagir()
    {
        if (!inicializado) return;
        if (interagindo || TemMadeiraBloqueando()) return; 

        if (estaDestrancadaPraSempre)
        {
            StartCoroutine(EntrarNaCasa(false)); 
            return;
        }

        if (KeySystem.TemChave(idChaveNecessaria))
        {
            StartCoroutine(EntrarNaCasa(true)); 
        }
        else
        {
            StartCoroutine(MostrarMensagemSemChave());
        }
    }

    bool TemMadeiraBloqueando()
    {
        if (madeirasBloqueio == null) return false;

        foreach (GameObject m in madeirasBloqueio)
        {
            if (m != null && m.activeInHierarchy)
                return true;
        }

        return false;
    }

    IEnumerator EntrarNaCasa(bool primeiraVezComChave)
    {
        interagindo = true;

        if (textoInteragirUI)
            textoInteragirUI.SetActive(false);

        if (textoSemChaveUI)
            textoSemChaveUI.SetActive(false);

        if (FPS_Master.Instance != null)
            FPS_Master.travadoInteracao = true;

        if (primeiraVezComChave)
        {
            KeySystem.GastarChave(idChaveNecessaria);

            if (hudIconeChaveParaApagar)
                hudIconeChaveParaApagar.SetActive(false);

            if (audioSource && somDestrancar)
                audioSource.PlayOneShot(somDestrancar);

            yield return new WaitForSeconds(0.5f);

            estaDestrancadaPraSempre = true;

            if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
                PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);

            SalvarEntradaDaCasa();
        }

        if (audioSource && somAbrirPorta)
            audioSource.PlayOneShot(somAbrirPorta);

        yield return new WaitForSeconds(1f); 

        SceneManager.LoadScene(nomeDaCenaParaCarregar);
    }

    private void SalvarEntradaDaCasa()
    {
        if (SistemaGlobal.Instance != null)
        {
            Vector3 posDeSeguranca = pontoDeRetorno != null ? pontoDeRetorno.position : transform.position;
            SistemaGlobal.Instance.SalvarJogo(posDeSeguranca, SceneManager.GetActiveScene().name);
            return;
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(true);
    }

    IEnumerator MostrarMensagemSemChave()
    {
        if (textoInteragirUI)
            textoInteragirUI.SetActive(false);

        if (textoSemChaveUI)
            textoSemChaveUI.SetActive(true);

        if (audioSource && somTrancada)
            audioSource.PlayOneShot(somTrancada);

        yield return new WaitForSeconds(2.5f);

        if (textoSemChaveUI)
            textoSemChaveUI.SetActive(false);

        if (!interagindo && textoInteragirUI)
            textoInteragirUI.SetActive(true);
    }
}
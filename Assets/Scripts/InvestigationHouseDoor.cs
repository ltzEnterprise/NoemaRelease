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

    void Start()
    {
        if (textoSemChaveUI) textoSemChaveUI.SetActive(false);
        if (textoInteragirUI) textoInteragirUI.SetActive(false);
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            if (PersistenciaManager.Instance.ObterEstado(uniqueID))
            {
                estaDestrancadaPraSempre = true;
            }
        }
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

    public void AoSair() { if (textoInteragirUI) textoInteragirUI.SetActive(false); }

    public void Interagir()
    {
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
        foreach (GameObject m in madeirasBloqueio) if (m != null && m.activeInHierarchy) return true;
        return false;
    }

    IEnumerator EntrarNaCasa(bool gastarChave)
    {
        interagindo = true;
        if (textoInteragirUI) textoInteragirUI.SetActive(false);
        if (FPS_Master.Instance != null) FPS_Master.travadoInteracao = true;

        if (gastarChave)
        {
            KeySystem.GastarChave(idChaveNecessaria);
            if (hudIconeChaveParaApagar) hudIconeChaveParaApagar.SetActive(false);
            if (audioSource && somDestrancar) audioSource.PlayOneShot(somDestrancar);
            yield return new WaitForSeconds(0.5f);
        }

        if (audioSource && somAbrirPorta) audioSource.PlayOneShot(somAbrirPorta);

        estaDestrancadaPraSempre = true;
        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);

        // 🔥 REGRA 8X APLICADA AQUI: 
        // Se você esqueceu de arrastar a referência do EmptyObject na Unity, ele não pula o save mais! Ele pega a coordenada da própria porta e segue a vida.
        if (SistemaGlobal.Instance != null)
        {
            Vector3 posDeSeguranca = pontoDeRetorno != null ? pontoDeRetorno.position : transform.position;
            SistemaGlobal.Instance.SalvarJogo(posDeSeguranca, SceneManager.GetActiveScene().name);
        }

        yield return new WaitForSeconds(1f); 
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
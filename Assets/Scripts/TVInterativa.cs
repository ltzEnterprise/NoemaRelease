using UnityEngine;
using System.Collections;

public class TVInterativa : MonoBehaviour
{
    [Header("Sistema de Save")]
    public string idUnico = "TV_Sala_01"; 

    [Header("Configuração da Runa")]
    public string nomeDaRuna = "Runa da Música"; 
    
    [Header("Visuais")]
    public GameObject imagemGlitch; 
    public GameObject imagemRunaNaTela; 
    public GameObject textoInteragir;
    public GameObject painelFeedback;
    
    [Header("Áudio")]
    public AudioClip somGlitch;     
    public AudioClip somRunaAparecendo; 
    public AudioSource audioSource;     

    private bool tvLigada = false;
    private bool jaPegou = false;
    private bool animacaoRodando = false;
    private bool inicializado = false;

    void Start()
    {
        if (imagemGlitch) imagemGlitch.SetActive(false);
        if (imagemRunaNaTela) imagemRunaNaTela.SetActive(false);
        if (textoInteragir) textoInteragir.SetActive(false);
        if (painelFeedback) painelFeedback.SetActive(false);

        StartCoroutine(VerificarSaveSeguro());
    }

    IEnumerator VerificarSaveSeguro()
    {
        if (PersistenciaManager.Instance != null)
            yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);

        VerificarSave();
        inicializado = true;
    }

    void VerificarSave()
    {
        if (PersistenciaManager.Instance == null) return;

        bool runaDisponivel = PersistenciaManager.Instance.CarregarEstadoObjeto(idUnico, true);

        if (!runaDisponivel)
        {
            jaPegou = true;
            tvLigada = false;
            
            if (imagemRunaNaTela) imagemRunaNaTela.SetActive(false);
            if (textoInteragir) textoInteragir.SetActive(false);
        }
    }

    public void AoOlhar()
    {
        if (!inicializado) return;

        if (tvLigada && !jaPegou && !animacaoRodando)
        {
            if (textoInteragir) textoInteragir.SetActive(true);
        }
    }

    public void AoSair()
    {
        if (textoInteragir) textoInteragir.SetActive(false);
    }

    public void Interagir()
    {
        if (!inicializado) return;

        if (tvLigada && !jaPegou && !animacaoRodando)
            PegarRunaAgora();
    }

    public void ReceberSinalDoDisco()
    {
        if (!inicializado) return;
        if (jaPegou || animacaoRodando) return;

        StartCoroutine(SequenciaLigarTV());
    }

    void PegarRunaAgora()
    {
        jaPegou = true;
        tvLigada = false; 

        if (imagemRunaNaTela) imagemRunaNaTela.SetActive(false);
        if (textoInteragir) textoInteragir.SetActive(false);
        
        if (painelFeedback) 
        {
            painelFeedback.SetActive(true);
            StartCoroutine(EsconderPainelDepois());
        }

        if (InventarioRunas.Instance != null)
            InventarioRunas.Instance.ColetarRunaSemForcarSaveHD(nomeDaRuna);

        SalvarQueJaPegou();
    }

    void SalvarQueJaPegou()
    {
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(idUnico, false);
            SalvarProgressoSeguro();
        }
    }

    IEnumerator SequenciaLigarTV()
    {
        animacaoRodando = true;

        if (imagemGlitch) imagemGlitch.SetActive(true);
        if (audioSource && somGlitch) audioSource.PlayOneShot(somGlitch);
        
        yield return new WaitForSeconds(0.5f);

        if (imagemGlitch) imagemGlitch.SetActive(false);
        if (imagemRunaNaTela) imagemRunaNaTela.SetActive(true);
        if (audioSource && somRunaAparecendo) audioSource.PlayOneShot(somRunaAparecendo);

        tvLigada = true; 
        animacaoRodando = false;
    }

    IEnumerator EsconderPainelDepois()
    {
        yield return new WaitForSeconds(3.0f);

        if (painelFeedback)
            painelFeedback.SetActive(false);
    }

    private void SalvarProgressoSeguro()
    {
        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(false);
    }
}
using UnityEngine;
using System.Collections;

public class TVInterativa : MonoBehaviour
{
    // --- VARIÁVEIS ---
    [Header("Sistema de Save")]
    [Tooltip("Esse ID deve ser ÚNICO no jogo. Se copiar a TV, mude esse ID.")]
    public string idUnico = "TV_Sala_01"; 

    [Header("Configuração da Runa")]
    public string nomeDaRuna = "Runa da Música"; 
    
    [Header("Visuais")]
    public GameObject imagemGlitch; 
    public GameObject imagemRunaNaTela; 
    public GameObject textoInteragir;   // O Texto "Aperte E"
    public GameObject painelFeedback;   // O Painel "Você pegou a Runa"
    
    [Header("Áudio")]
    public AudioClip somGlitch;     
    public AudioClip somRunaAparecendo; 
    public AudioSource audioSource;     

    private bool tvLigada = false;
    private bool jaPegou = false;
    private bool animacaoRodando = false;

    void Start()
    {
        // Garante estado inicial visual limpo
        if (imagemGlitch) imagemGlitch.SetActive(false);
        if (imagemRunaNaTela) imagemRunaNaTela.SetActive(false);
        if (textoInteragir) textoInteragir.SetActive(false);
        if (painelFeedback) painelFeedback.SetActive(false);

        // --- SISTEMA DE SAVE (CARREGAR) ---
        VerificarSave();
    }

    void VerificarSave()
    {
        if (Application.isEditor) return;

        if (PersistenciaManager.Instance != null)
        {
            // O padrão é TRUE (Runa Disponível).
            // Se o save retornar FALSE, significa que já pegamos a runa antes.
            bool runaDisponivel = PersistenciaManager.Instance.CarregarEstadoObjeto(idUnico, true);

            if (!runaDisponivel)
            {
                jaPegou = true;
                tvLigada = false;
                
                if (imagemRunaNaTela) imagemRunaNaTela.SetActive(false);
                if (textoInteragir) textoInteragir.SetActive(false);
            }
        }
    }

    public void AoOlhar()
    {
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
        if (tvLigada && !jaPegou && !animacaoRodando)
        {
            PegarRunaAgora();
        }
    }

    public void ReceberSinalDoDisco()
    {
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
        {
            InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRuna);
        }

        // --- SISTEMA DE SAVE (SALVAR) ---
        SalvarQueJaPegou();
    }

    void SalvarQueJaPegou()
    {
        if (Application.isEditor) return;

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(idUnico, false);
            
            // 🔥 REMOVIDO o PlayerPrefs e colocado o nosso JSON oficial
            PersistenciaManager.Instance.SalvarTudo(); 
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
        if (painelFeedback) painelFeedback.SetActive(false);
    }
}
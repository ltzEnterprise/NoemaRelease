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
        // Se estiver no Editor e você quiser testar o save, comente a linha abaixo.
        // Mantive igual ao seu SaveableItem para consistência.
        if (Application.isEditor) return;

        if (PersistenciaManager.Instance != null)
        {
            // Tenta carregar o estado. O padrão é TRUE (Runa Disponível).
            // Se o save retornar FALSE, significa que já pegamos a runa antes.
            bool runaDisponivel = PersistenciaManager.Instance.CarregarEstadoObjeto(idUnico, true);

            if (!runaDisponivel)
            {
                // Já pegamos a runa no save!
                jaPegou = true;
                tvLigada = false;
                
                // Garante que visualmente não aparece nada
                if (imagemRunaNaTela) imagemRunaNaTela.SetActive(false);
                if (textoInteragir) textoInteragir.SetActive(false);
            }
        }
    }

    // --- PROTOCOLO FPS_MASTER ---

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

    // --- LÓGICA DA TV ---

    public void ReceberSinalDoDisco()
    {
        if (jaPegou || animacaoRodando) return;
        StartCoroutine(SequenciaLigarTV());
    }

    void PegarRunaAgora()
    {
        jaPegou = true;
        tvLigada = false; 

        // 1. Feedback Visual Imediato
        if (imagemRunaNaTela) imagemRunaNaTela.SetActive(false);
        if (textoInteragir) textoInteragir.SetActive(false);
        
        if (painelFeedback) 
        {
            painelFeedback.SetActive(true);
            StartCoroutine(EsconderPainelDepois());
        }

        // 2. Envia para o Inventário de Runas
        if (InventarioRunas.Instance != null)
        {
            InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRuna);
        }

        // --- SISTEMA DE SAVE (SALVAR) ---
        SalvarQueJaPegou();
    }

    void SalvarQueJaPegou()
    {
        // Mesma trava do Editor que existe no seu SaveableItem
        if (Application.isEditor) return;

        if (PersistenciaManager.Instance != null)
        {
            // Registra FALSE no ID da TV.
            // FALSE significa: "O objeto interativo (runa) NÃO está mais ativo/disponível".
            PersistenciaManager.Instance.RegistrarEstado(idUnico, false);
            
            // Força o save no disco (opcional, mas seguro para itens importantes)
            PlayerPrefs.Save(); 
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
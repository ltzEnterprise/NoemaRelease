using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;

    [Header("--- INVENTÁRIO (Chave) ---")]
    public GameObject iconeChave; // A imagem da chave no canto (RawImage ou Image)

    [Header("--- SISTEMA DE RUNAS ---")]
    public GameObject painelRunasPai; // O Objeto pai "_HUD_Runas" (Começa desligado)
    public Image[] slotsDasRunas;     // Arraste as 8 imagens aqui (na ordem visual)
    public Sprite spriteRunaAtiva;    // A imagem colorida/brilhando
    public Sprite spriteRunaVazia;    // A imagem cinza/vazia (opcional)

    [Header("--- TEXTOS DE AVISO (UI) ---")]
    public GameObject textoPegarChave;       // Texto "Aperte E para Pegar"
    public GameObject textoAbrirBau;         // Texto "Aperte E para Abrir"
    public GameObject textoInspecionarAltar; // Texto "Aperte E para Inspecionar"

    private int quantidadeAtualRunas = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 1. Esconde tudo no começo
        if (iconeChave) iconeChave.SetActive(false);
        if (painelRunasPai) painelRunasPai.SetActive(false);
        
        // Esconde textos
        DesativarTextoInteracao();

        // Zera o visual das runas (deixa tudo cinza ou transparente)
        foreach (var slot in slotsDasRunas)
        {
            if (slot) 
            {
                if (spriteRunaVazia) slot.sprite = spriteRunaVazia;
                else slot.color = new Color(1, 1, 1, 0.2f); // Se não tiver sprite vazio, deixa transparente
            }
        }
    }

    // --- FUNÇÃO DE LIMPEZA (ADICIONADA) ---
    // Esta função desativa todos os textos de interação de uma vez
    public void DesativarTextoInteracao()
    {
        if (textoPegarChave) textoPegarChave.SetActive(false);
        if (textoAbrirBau) textoAbrirBau.SetActive(false);
        if (textoInspecionarAltar) textoInspecionarAltar.SetActive(false);
    }

    // --- FUNÇÕES DA CHAVE ---
    public void PegouChave()
    {
        if (iconeChave) iconeChave.SetActive(true);
    }

    public void UsouChave()
    {
        if (iconeChave) iconeChave.SetActive(false);
    }

    // --- FUNÇÕES DAS RUNAS ---
    public void LiberarPainelRunas()
    {
        // Chamado pelo Altar: Mostra o painel (com as runas que vc já tiver)
        if (painelRunasPai) painelRunasPai.SetActive(true);
    }

    public void AdicionarRunaSequencial()
    {
        // Preenche o próximo slot vazio
        if (quantidadeAtualRunas < slotsDasRunas.Length)
        {
            Image slotAtual = slotsDasRunas[quantidadeAtualRunas];
            if (slotAtual)
            {
                slotAtual.sprite = spriteRunaAtiva;
                slotAtual.color = Color.white;
            }
            quantidadeAtualRunas++;
        }
    }
}

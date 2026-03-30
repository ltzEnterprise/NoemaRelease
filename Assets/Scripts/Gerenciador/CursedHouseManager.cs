using UnityEngine;
using TMPro;

public class CursedHouseManager : MonoBehaviour
{
    [Header("--- CONFIGURAÇÃO ---")]
    public string nomeDaRuna = "Runa_Investigacao"; // Tem que ser igual ao ID no InventarioRunas

    [Header("--- AUDIO ---")]
    public AudioSource fonteAudio;
    public AudioClip somPortaTrancando;     // Toca ao iniciar (ambiente tenso)
    public AudioClip somPortaDestrancando;  // Toca ao resolver o puzzle

    [Header("--- UI ---")]
    public GameObject painelRecompensa;
    public TextMeshProUGUI textoRecompensa;

    private bool puzzleResolvido = false;

    void Start()
    {
        // 1. Toca som de "Trancou você aqui dentro"
        if(fonteAudio && somPortaTrancando) 
            fonteAudio.PlayOneShot(somPortaTrancando);
        
        if(painelRecompensa) painelRecompensa.SetActive(false);
    }

    // --- CONECTE ISSO NO EVENTO "ON SUCCESS" DO SEU KEYPAD/PUZZLE ---
    public void OnPuzzleSolved()
    {
        if (puzzleResolvido) return;
        puzzleResolvido = true;

        // 2. Entrega a Runa para o Inventário Global (que persiste entre cenas)
        if(InventarioRunas.Instance != null)
        {
            InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRuna);
            Debug.Log("Runa adicionada ao inventário global!");
        }
        else
        {
            Debug.LogError("ERRO CRÍTICO: InventarioRunas não encontrado! A runa não será salva.");
        }

        // 3. Som de Vitória/Destrancar a porta
        if(fonteAudio && somPortaDestrancando) 
            fonteAudio.PlayOneShot(somPortaDestrancando);

        // 4. Mostra UI
        if(painelRecompensa)
        {
            painelRecompensa.SetActive(true);
            if(textoRecompensa) textoRecompensa.text = "Você encontrou a " + nomeDaRuna + "!";
            Invoke("EsconderPainel", 4f);
        }
    }

    void EsconderPainel() 
    { 
        if(painelRecompensa) painelRecompensa.SetActive(false); 
    }
}
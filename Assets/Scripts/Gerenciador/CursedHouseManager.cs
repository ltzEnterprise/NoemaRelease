using UnityEngine;
using TMPro;

public class CursedHouseManager : MonoBehaviour
{
    [Header("--- CONFIGURAÇÃO ---")]
    public string nomeDaRuna = "Runa_Investigacao"; 

    [Header("--- SAVE SYSTEM (TRAVA) ---")]
    [Tooltip("Se ativado, salva o jogo no HD assim que pegar a runa. DEIXE DESMARCADO dentro da casa para evitar softlock!")]
    public bool forcarSaveNoHD = false; // 🔥 CAIXINHA NOVA AQUI

    [Header("--- AUDIO ---")]
    public AudioSource fonteAudio;
    public AudioClip somPortaTrancando;     
    public AudioClip somPortaDestrancando;  

    [Header("--- UI ---")]
    public GameObject painelRecompensa;
    public TextMeshProUGUI textoRecompensa;

    private bool puzzleResolvido = false;

    void Start()
    {
        if(fonteAudio && somPortaTrancando) 
            fonteAudio.PlayOneShot(somPortaTrancando);
        
        if(painelRecompensa) painelRecompensa.SetActive(false);
    }

    public void OnPuzzleSolved()
    {
        if (puzzleResolvido) return;
        puzzleResolvido = true;

        if(InventarioRunas.Instance != null)
        {
            // 🔥 LÓGICA CONDICIONAL 🔥
            if (forcarSaveNoHD)
                InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRuna);
            else
                InventarioRunas.Instance.ColetarRunaSemForcarSaveHD(nomeDaRuna);
                
            Debug.Log("Runa adicionada ao inventário global (RAM)!");
        }
        else
        {
            Debug.LogError("ERRO CRÍTICO: InventarioRunas não encontrado! A runa não será salva.");
        }

        if(fonteAudio && somPortaDestrancando) 
            fonteAudio.PlayOneShot(somPortaDestrancando);

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
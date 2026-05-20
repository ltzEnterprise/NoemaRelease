using UnityEngine;
using TMPro;
using System.Collections;

public class CursedHouseManager : MonoBehaviour
{
    [Header("--- CONFIGURAÇÃO ---")]
    public string nomeDaRuna = "Runa_Investigacao"; 

    [Header("--- SAVE SYSTEM ---")]
    [Tooltip("ID único desse puzzle dentro da casa. Ex: Puzzle_Casa_Investigacao")]
    public string uniqueID = "Puzzle_Casa_Investigacao";

    [Header("--- AUDIO ---")]
    public AudioSource fonteAudio;
    public AudioClip somPortaTrancando;     
    public AudioClip somPortaDestrancando;  

    [Header("--- UI ---")]
    public GameObject painelRecompensa;
    public TextMeshProUGUI textoRecompensa;

    private bool puzzleResolvido = false;
    private bool inicializado = false;

    void Start()
    {
        if (painelRecompensa)
            painelRecompensa.SetActive(false);

        StartCoroutine(InicializarSeguro());
    }

    IEnumerator InicializarSeguro()
    {
        if (PersistenciaManager.Instance != null)
        {
            yield return new WaitUntil(() =>
                PersistenciaManager.Instance.DadosProntosParaUso &&
                !PersistenciaManager.Instance.EstaCarregando
            );
        }

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            puzzleResolvido = PersistenciaManager.Instance.ObterEstado(uniqueID + "_Resolvido", false);
        }

        inicializado = true;

        if (!puzzleResolvido)
        {
            if (fonteAudio && somPortaTrancando) 
                fonteAudio.PlayOneShot(somPortaTrancando);
        }
    }

    public void OnPuzzleSolved()
    {
        if (!inicializado) return;
        if (puzzleResolvido) return;

        puzzleResolvido = true;

        if (InventarioRunas.Instance != null)
        {
            InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRuna);
            Debug.Log("[CursedHouseManager] Runa coletada e salva pelo InventarioRunas: " + nomeDaRuna);
        }
        else
        {
            Debug.LogError("[CursedHouseManager] InventarioRunas.Instance está nulo. A runa NÃO foi entregue: " + nomeDaRuna);
        }

        if (PersistenciaManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(uniqueID))
                PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_Resolvido", true);

            PersistenciaManager.Instance.SalvarTudo(true);
        }

        if (fonteAudio && somPortaDestrancando) 
            fonteAudio.PlayOneShot(somPortaDestrancando);

        if (painelRecompensa)
        {
            painelRecompensa.SetActive(true);

            if (textoRecompensa)
                textoRecompensa.text = "Você encontrou a " + nomeDaRuna + "!";

            Invoke(nameof(EsconderPainel), 4f);
        }
    }

    void EsconderPainel() 
    { 
        if (painelRecompensa)
            painelRecompensa.SetActive(false); 
    }
}
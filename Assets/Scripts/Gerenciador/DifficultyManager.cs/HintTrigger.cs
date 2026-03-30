using UnityEngine;

public class HintTrigger : MonoBehaviour
{
    [Header("Configurações da Dica")]
    [TextArea(3, 5)]
    public string mensagemDaDica = "O texto do puzzle entra aqui...";
    
    [Tooltip("A dica fica na tela até outra aparecer ou o puzzle mandar sumir?")]
    public bool dicaInfinita = false;
    
    [Tooltip("Tempo na tela (Ignorado se for infinita)")]
    public float tempoDeExibicao = 4f;

    [Header("Como a dica ativa?")]
    [Tooltip("Se marcado, a dica aparece ao pisar num gatilho invisível (Trigger).")]
    public bool ativarAoPisar = true;
    
    [Tooltip("Se marcado, o gatilho se destrói para não repetir a mesma dica 2 vezes.")]
    public bool destruirAposUso = true;

    // MÉTODO 1: PISAR NO CHÃO
    void OnTriggerEnter(Collider other)
    {
        // Só ativa se a caixinha tiver marcada e quem pisar for o Player
        if (ativarAoPisar && other.CompareTag("Player"))
        {
            DispararDica();
        }
    }

    // MÉTODO 2: ATIVAR POR CÓDIGO (Portas, Puzzles resolvidos, etc)
    public void DispararDica()
    {
        // 0 = Easily, 1 = Normal
        int dificuldade = PlayerPrefs.GetInt("DificuldadeJogo", 1);
        
        // SÓ MOSTRA SE FOR EASILY (0)
        if (dificuldade == 0) 
        {
            if (HintManager.Instance != null)
            {
                HintManager.Instance.MostrarDica(mensagemDaDica, tempoDeExibicao, dicaInfinita);
            }
            
            // A MÁGICA: O gatilho só comete suicídio se ele realmente entregou a dica!
            if (destruirAposUso) Destroy(gameObject); 
        }
        // Se a dificuldade for 1 (Normal), ele passa direto pelo if e continua vivo no mapa.
    }

    // Usado pros seus outros scripts apagarem uma dica infinita da tela
    public void ApagarDicaManual()
    {
         if (HintManager.Instance != null) HintManager.Instance.EsconderDica();
    }
}
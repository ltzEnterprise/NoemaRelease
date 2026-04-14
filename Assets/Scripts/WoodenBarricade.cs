using UnityEngine;
using System.Collections;

public class WoodenBarricade : MonoBehaviour
{
    [Header("--- BLOQUEADOR ---")]
    [Tooltip("Coloque o objeto que bloqueia (ex: plasma). Se ficar vazio, funciona normal.")]
    public GameObject bloqueador;

    [Header("Configuração")]
    [Tooltip("ID do Pé de Cabra no Gerenciador (Geralmente 1)")]
    public int idDoPeDeCabra = 1; 
    public float tempoTelaPreta = 1.5f;

    [Header("Efeitos")]
    public GameObject painelTelaPreta; 
    public AudioSource audioSource;
    public AudioClip somQuebrarMadeira;
    public GameObject textoSemFerramenta;
    
    // UI de Interação (Raycast)
    public GameObject textoInteragir; // "Usar Pé de Cabra"

    [Header("Conexão")]
    [Tooltip("Se tiver uma porta atrás, arraste ela aqui para ativá-la quando quebrar.")]
    public GameObject portaBloqueada;

    private bool emProcesso = false;
    private bool estaOlhando = false;
    private bool mostrandoErro = false;

    // Função que checa em tempo real se a parada tá bloqueada
    private bool TaBloqueado()
    {
        return bloqueador != null && bloqueador.activeInHierarchy;
    }

    void Start()
    {
        if (painelTelaPreta) painelTelaPreta.SetActive(false);
        if (textoSemFerramenta) textoSemFerramenta.SetActive(false);
        if (textoInteragir) textoInteragir.SetActive(false);
    }

    void OnDisable()
    {
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoSemFerramenta) textoSemFerramenta.SetActive(false);
    }

    // --- MÉTODOS RAYCAST ---
    public void AoOlhar()
    {
        if (TaBloqueado() || emProcesso) return; 
        
        estaOlhando = true;

        // Só mostra o texto de [E] se NÃO estiver mostrando o erro
        if (!mostrandoErro && textoInteragir)
        {
            textoInteragir.SetActive(true);
        }
    }

    public void AoSair()
    {
        estaOlhando = false;
        
        // 🔥 AQUI TAVA O BUG! 🔥
        // Agora APAGA SÓ O TEXTO NORMAL. O texto de erro fica intacto pra não bugar.
        if (textoInteragir) textoInteragir.SetActive(false);
    }

    public void Interagir()
    {
        if (TaBloqueado() || emProcesso || mostrandoErro) return; 

        if (InventoryManager.Instance != null && InventoryManager.Instance.itemSelecionado == idDoPeDeCabra)
        {
            StartCoroutine(QuebrarBarricada());
        }
        else
        {
            StartCoroutine(MostrarAvisoDeErro());
        }
    }
    // -----------------------

    IEnumerator MostrarAvisoDeErro()
    {
        mostrandoErro = true;

        // Desliga o texto normal e liga o erro
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoSemFerramenta) textoSemFerramenta.SetActive(true);
        
        // Fica na tela por 2 segundos independente da mira
        yield return new WaitForSeconds(2f);
        
        // Acabou o tempo, apaga o erro
        if (textoSemFerramenta) textoSemFerramenta.SetActive(false);
        mostrandoErro = false;

        // Se o jogador ainda estiver olhando pra madeira, devolve o texto normal
        if (estaOlhando && !emProcesso && !TaBloqueado()) 
        {
            if (textoInteragir) textoInteragir.SetActive(true);
        }
    }

    IEnumerator QuebrarBarricada()
    {
        emProcesso = true;

        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoSemFerramenta) textoSemFerramenta.SetActive(false);

        // Trava o jogador (WASD 0, Gravidade ON)
        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(true, false);

        if (painelTelaPreta) painelTelaPreta.SetActive(true);
        if (audioSource && somQuebrarMadeira) audioSource.PlayOneShot(somQuebrarMadeira);

        yield return new WaitForSeconds(tempoTelaPreta);

        if (painelTelaPreta) painelTelaPreta.SetActive(false);
        
        // Ativa a porta atrás
        if (portaBloqueada) portaBloqueada.SetActive(true);

        // Destrava
        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(false, false);
            
        gameObject.SetActive(false); 
    }
}
using UnityEngine;
using System.Collections;

public class WoodenBarricade : MonoBehaviour
{
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
    private bool estaOlhando = false; // <--- A variável que salva a lógica

    void Start()
    {
        if (painelTelaPreta) painelTelaPreta.SetActive(false);
        if (textoSemFerramenta) textoSemFerramenta.SetActive(false);
        if (textoInteragir) textoInteragir.SetActive(false);
    }

    // --- MÉTODOS RAYCAST ---
    public void AoOlhar()
    {
        if (emProcesso) return;
        
        estaOlhando = true;

        // Se a mensagem de erro já estiver na tela, não sobrepõe ela com o texto de interagir
        if (textoSemFerramenta && textoSemFerramenta.activeSelf) return;

        if (textoInteragir) textoInteragir.SetActive(true);
    }

    public void AoSair()
    {
        estaOlhando = false;
        
        // Desliga os dois textos imediatamente quando o jogador virar as costas
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoSemFerramenta) textoSemFerramenta.SetActive(false);
    }

    public void Interagir()
    {
        if (emProcesso) return;

        if (VerificarSeTemPeDeCabra())
        {
            StartCoroutine(QuebrarBarricada());
        }
        else
        {
            // Troquei o StopAllCoroutines por este para não bugar a quebra da madeira sem querer
            StopCoroutine("MostrarAvisoDeErro"); 
            StartCoroutine("MostrarAvisoDeErro");
        }
    }
    // -----------------------

    bool VerificarSeTemPeDeCabra()
    {
        if (EstadoGlobal.armasDesbloqueadas != null && idDoPeDeCabra < EstadoGlobal.armasDesbloqueadas.Length)
        {
            return EstadoGlobal.armasDesbloqueadas[idDoPeDeCabra];
        }
        return false;
    }

    IEnumerator MostrarAvisoDeErro()
    {
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoSemFerramenta) textoSemFerramenta.SetActive(true);
        
        yield return new WaitForSeconds(2f);
        
        if (textoSemFerramenta) textoSemFerramenta.SetActive(false);
        
        // O SEGREDO AQUI: Só reativa o texto se o jogador AINDA estiver olhando
        if (!emProcesso && estaOlhando && textoInteragir) 
        {
            textoInteragir.SetActive(true);
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
            
        gameObject.SetActive(false); // Some com a barricada
    }
}
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

    void Update()
    {
        // Monitoramento constante: Se ativar o bloqueador, apaga as UIs na mesma hora
        if (TaBloqueado())
        {
            if (textoInteragir && textoInteragir.activeSelf) textoInteragir.SetActive(false);
            if (textoSemFerramenta && textoSemFerramenta.activeSelf) textoSemFerramenta.SetActive(false);
        }
    }

    // --- MÉTODOS RAYCAST ---
    public void AoOlhar()
    {
        if (TaBloqueado()) return; // Morre aqui se tiver bloqueado

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
        if (TaBloqueado()) return; // Morre aqui se tiver bloqueado
        if (emProcesso) return;

        if (VerificarSeTemPeDeCabra())
        {
            StartCoroutine(QuebrarBarricada());
        }
        else
        {
            StopCoroutine("MostrarAvisoDeErro"); 
            StartCoroutine("MostrarAvisoDeErro");
        }
    }
    // -----------------------

    bool VerificarSeTemPeDeCabra()
    {
        // VERIFICA SE O ITEM TÁ NA MÃO E SELECIONADO AGORA
        if (InventoryManager.Instance != null)
        {
            return InventoryManager.Instance.itemSelecionado == idDoPeDeCabra;
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
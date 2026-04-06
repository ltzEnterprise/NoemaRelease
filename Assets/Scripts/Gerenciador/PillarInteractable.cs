using UnityEngine;
using System.Collections;

public class PillarInteractable : MonoBehaviour
{
    [Header("Configuração Deste Pilar")]
    public int numeroDestePilar;
    public PillarManager managerDoPuzzle;

    [Header("Animação do Botão (Eixo Y Local)")]
    public float alturaPadrao = 0f;
    public float alturaBaixa = -0.15f;
    public float velocidadeAnimacao = 10f;

    [Header("Áudio e Feedback")]
    public AudioSource audioSourcePilar;
    public AudioClip somApertarBotao;
    
    [Header("UI de Interação")]
    public GameObject textoInteragir; // Arraste o texto "Apertar Botão" aqui

    private bool emAnimacao = false;
    private Collider colisorDoBotao; // Adicionado para o radar anti-parede

    void Start()
    {
        // Pega o colisor pra poder mirar no centro exato dele
        colisorDoBotao = GetComponent<Collider>();

        // Garante que o texto comece desligado
        if (textoInteragir) textoInteragir.SetActive(false);

        if (alturaPadrao == 0f && transform.localPosition.y != 0f)
        {
            alturaPadrao = transform.localPosition.y;
        }
    }

    // --- O RADAR ANTI-PAREDE ---
    bool ChecarVisaoLimpa()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        // Pega o centro exato do botão pra evitar que o raio bata no nada ou no chão
        Vector3 centroDoBotao = colisorDoBotao != null ? colisorDoBotao.bounds.center : transform.position;
        Vector3 direcao = centroDoBotao - cam.transform.position;
        float distancia = direcao.magnitude;

        RaycastHit hit;
        // Dispara o raio ignorando triggers invisíveis
        if (Physics.Raycast(cam.transform.position, direcao, out hit, distancia, ~0, QueryTriggerInteraction.Ignore))
        {
            // Se o raio bateu numa parede antes de chegar no botão/pilar, bloqueia
            if (hit.transform != transform && !hit.transform.IsChildOf(transform))
            {
                return false; 
            }
        }
        return true; 
    }

    // --- MÉTODOS DO RAYCAST (MIRA DO JOGADOR) ---
    
    public void AoOlhar()
    {
        if (emAnimacao) return; // Não mostra texto se o botão já estiver se mexendo
        
        // Só acende o texto se não tiver parede na frente
        if (ChecarVisaoLimpa())
        {
            if (textoInteragir && !textoInteragir.activeSelf) textoInteragir.SetActive(true);
        }
        else
        {
            if (textoInteragir && textoInteragir.activeSelf) textoInteragir.SetActive(false);
        }
    }

    public void AoSair()
    {
        // Esconde o texto quando o jogador vira as costas
        if (textoInteragir) textoInteragir.SetActive(false);
    }

    // --------------------------------------------

    public void Interagir()
    {
        if (emAnimacao) return;

        // Se o cara apertar E através da parede, barra a ação na hora
        if (!ChecarVisaoLimpa()) return;

        // Desliga o texto NA HORA do clique
        if (textoInteragir) textoInteragir.SetActive(false);

        // Deixa a animação rolar solta (dia ou noite)
        StartCoroutine(AnimarEAtivar());
    }

    private IEnumerator AnimarEAtivar()
    {
        emAnimacao = true;

        if (audioSourcePilar && somApertarBotao)
        {
            audioSourcePilar.PlayOneShot(somApertarBotao);
        }

        // --- A GRANDE MUDANÇA ESTÁ AQUI ---
        // Ele SÓ manda o sinal pro Puzzle se for de noite.
        // Se for de dia, ele ignora esse bloco e vai direto pra animação de descer.
        bool ehNoite = (DayNightCycle.Instance != null && DayNightCycle.Instance.isNight);
        
        if (ehNoite && managerDoPuzzle != null)
        {
            managerDoPuzzle.ReceberInteracaoPilar(numeroDestePilar);
        }
        // -----------------------------------

        // ANIMAÇÃO PARA BAIXO
        Vector3 posAlvo = transform.localPosition;
        posAlvo.y = alturaBaixa;

        while (Mathf.Abs(transform.localPosition.y - alturaBaixa) > 0.001f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, posAlvo, Time.deltaTime * velocidadeAnimacao);
            yield return null;
        }
        transform.localPosition = posAlvo;

        // Tempo que o botão fica afundado
        yield return new WaitForSeconds(0.15f);

        // ANIMAÇÃO PARA CIMA
        posAlvo.y = alturaPadrao;

        while (Mathf.Abs(transform.localPosition.y - alturaPadrao) > 0.001f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, posAlvo, Time.deltaTime * velocidadeAnimacao);
            yield return null;
        }
        transform.localPosition = posAlvo;

        emAnimacao = false;
    }
}
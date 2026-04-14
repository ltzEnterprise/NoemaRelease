using UnityEngine;
using System.Collections;

public class PuzzleResetButton : MonoBehaviour
{
    [Header("Conexão com o Puzzle")]
    [Tooltip("Arraste o objeto que tem o SlidingPuzzleManager aqui")]
    public SlidingPuzzleManager managerDoPuzzle;

    [Header("Animação do Botão (Eixo Y Local)")]
    public float alturaPadrao = 0f;
    public float alturaBaixa = -0.15f;
    public float velocidadeAnimacao = 10f;

    [Header("Áudio e Feedback")]
    public AudioSource audioSourceBotao;
    public AudioClip somApertarBotao;
    
    [Header("UI de Interação")]
    public GameObject textoInteragir; // Arraste o texto "Resetar" aqui

    private bool emAnimacao = false;
    private Collider colisorDoBotao; 

    void Start()
    {
        colisorDoBotao = GetComponent<Collider>();

        if (textoInteragir) textoInteragir.SetActive(false);

        if (alturaPadrao == 0f && transform.localPosition.y != 0f)
        {
            alturaPadrao = transform.localPosition.y;
        }
    }

    bool ChecarVisaoLimpa()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        Vector3 centroDoBotao = colisorDoBotao != null ? colisorDoBotao.bounds.center : transform.position;
        Vector3 direcao = centroDoBotao - cam.transform.position;
        float distancia = direcao.magnitude;

        RaycastHit hit;
        if (Physics.Raycast(cam.transform.position, direcao, out hit, distancia, ~0, QueryTriggerInteraction.Ignore))
        {
            if (hit.transform != transform && !hit.transform.IsChildOf(transform))
            {
                return false; 
            }
        }
        return true; 
    }

    public void AoOlhar()
    {
        if (emAnimacao) return; 
        
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
        if (textoInteragir) textoInteragir.SetActive(false);
    }

    public void Interagir()
    {
        if (emAnimacao) return;
        if (!ChecarVisaoLimpa()) return;

        if (textoInteragir) textoInteragir.SetActive(false);

        StartCoroutine(AnimarEAtivar());
    }

    private IEnumerator AnimarEAtivar()
    {
        emAnimacao = true;

        if (audioSourceBotao && somApertarBotao)
        {
            audioSourceBotao.PlayOneShot(somApertarBotao);
        }

        // CHAMA O RESET NO PUZZLE!
        if (managerDoPuzzle != null)
        {
            managerDoPuzzle.ResetarPuzzle();
        }

        // ANIMAÇÃO PARA BAIXO
        Vector3 posAlvo = transform.localPosition;
        posAlvo.y = alturaBaixa;

        while (Mathf.Abs(transform.localPosition.y - alturaBaixa) > 0.001f)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, posAlvo, Time.deltaTime * velocidadeAnimacao);
            yield return null;
        }
        transform.localPosition = posAlvo;

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
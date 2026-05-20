using UnityEngine;
using System.Collections;

public class ReceiverDoor : MonoBehaviour
{
    [Header("--- SAVE NATIVO ---")]
    public string doorID = "";

    [Tooltip("Se ligado, salva se a porta ficou aberta/fechada.")]
    public bool salvarEstadoDaPorta = true;

    [Header("--- VISUAL ---")]
    public Transform doorVisual;

    public enum EixoDaPorta
    {
        Horizontal_X,
        Vertical_Y
    }

    public enum BordaFixa
    {
        Esquerda,
        Direita,
        Cima,
        Baixo
    }

    [Header("--- MOVIMENTO ---")]
    [Tooltip("Horizontal_X para porta abrindo de lado. Vertical_Y para porta subindo/descendo.")]
    public EixoDaPorta eixoDaPorta = EixoDaPorta.Horizontal_X;

    [Tooltip("Essa borda fica parada. A outra borda se move. Para efeito da direita para a esquerda, teste Direita ou Esquerda conforme o lado do modelo.")]
    public BordaFixa bordaFixa = BordaFixa.Direita;

    [Tooltip("Escala mínima quando aberta. 0 deixa sumir completamente. 0.02 evita escala zero absoluta.")]
    public float escalaAbertaMinima = 0.02f;

    [Header("Animação")]
    public float speed = 5f;

    [Header("Áudio")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;

    [Header("Debug")]
    public bool debugLogs = false;

    private bool shouldBeOpen = false;
    private bool inicializado = false;

    private Vector3 posicaoFechada;
    private Vector3 escalaFechada;

    private Vector3 posicaoAberta;
    private Vector3 escalaAberta;

    private bool configurado = false;

    void Start()
    {
        if (string.IsNullOrEmpty(doorID))
            doorID = gameObject.name;

        if (doorVisual == null)
            doorVisual = transform;

        ConfigurarAnimacaoPorBorda();

        StartCoroutine(CarregarSeguro());
    }

    private void ConfigurarAnimacaoPorBorda()
    {
        if (doorVisual == null)
        {
            Debug.LogError("[ReceiverDoor] doorVisual está vazio em: " + gameObject.name);
            return;
        }

        posicaoFechada = doorVisual.localPosition;
        escalaFechada = doorVisual.localScale;

        escalaAberta = escalaFechada;
        posicaoAberta = posicaoFechada;

        float escalaMin = Mathf.Max(0f, escalaAbertaMinima);

        if (eixoDaPorta == EixoDaPorta.Horizontal_X)
        {
            float escalaOriginalX = escalaFechada.x;
            float escalaFinalX = escalaMin;

            escalaAberta.x = escalaFinalX;

            float diferencaEscala = escalaOriginalX - escalaFinalX;
            float deslocamento = diferencaEscala * 0.5f;

            if (bordaFixa == BordaFixa.Direita)
            {
                posicaoAberta.x = posicaoFechada.x + deslocamento;
            }
            else
            {
                posicaoAberta.x = posicaoFechada.x - deslocamento;
            }
        }
        else
        {
            float escalaOriginalY = escalaFechada.y;
            float escalaFinalY = escalaMin;

            escalaAberta.y = escalaFinalY;

            float diferencaEscala = escalaOriginalY - escalaFinalY;
            float deslocamento = diferencaEscala * 0.5f;

            if (bordaFixa == BordaFixa.Cima)
            {
                posicaoAberta.y = posicaoFechada.y + deslocamento;
            }
            else
            {
                posicaoAberta.y = posicaoFechada.y - deslocamento;
            }
        }

        configurado = true;

        if (debugLogs)
        {
            Debug.Log("[ReceiverDoor] Configurada: " + gameObject.name +
                      " | Fechada Pos=" + posicaoFechada +
                      " Escala=" + escalaFechada +
                      " | Aberta Pos=" + posicaoAberta +
                      " Escala=" + escalaAberta);
        }
    }

    IEnumerator CarregarSeguro()
    {
        if (PersistenciaManager.Instance != null)
        {
            yield return new WaitUntil(() =>
                PersistenciaManager.Instance.DadosProntosParaUso &&
                !PersistenciaManager.Instance.EstaCarregando
            );

            if (salvarEstadoDaPorta && !string.IsNullOrEmpty(doorID))
                shouldBeOpen = PersistenciaManager.Instance.ObterEstado(doorID, false);
        }

        AplicarEstadoInstantaneo();

        inicializado = true;
    }

    void Update()
    {
        if (!inicializado || !configurado || doorVisual == null)
            return;

        Vector3 alvoEscala = shouldBeOpen ? escalaAberta : escalaFechada;
        Vector3 alvoPosicao = shouldBeOpen ? posicaoAberta : posicaoFechada;

        doorVisual.localScale = Vector3.Lerp(
            doorVisual.localScale,
            alvoEscala,
            Time.deltaTime * speed
        );

        doorVisual.localPosition = Vector3.Lerp(
            doorVisual.localPosition,
            alvoPosicao,
            Time.deltaTime * speed
        );
    }

    public void SetState(bool shouldOpen)
    {
        if (!configurado)
            ConfigurarAnimacaoPorBorda();

        if (shouldBeOpen == shouldOpen)
            return;

        shouldBeOpen = shouldOpen;

        PlaySound();

        if (debugLogs)
            Debug.Log("[ReceiverDoor] SetState em " + gameObject.name + " = " + shouldBeOpen);

        if (salvarEstadoDaPorta &&
            PersistenciaManager.Instance != null &&
            !string.IsNullOrEmpty(doorID))
        {
            PersistenciaManager.Instance.RegistrarEstado(doorID, shouldBeOpen);

            if (GameManager.Instance != null && GameManager.CenaPronta)
                GameManager.Instance.SalvarProgresso();
            else
                PersistenciaManager.Instance.SalvarTudo(true);
        }
    }

    public void Abrir()
    {
        SetState(true);
    }

    public void Fechar()
    {
        SetState(false);
    }

    public void Alternar()
    {
        SetState(!shouldBeOpen);
    }

    private void AplicarEstadoInstantaneo()
    {
        if (!configurado || doorVisual == null)
            return;

        doorVisual.localScale = shouldBeOpen ? escalaAberta : escalaFechada;
        doorVisual.localPosition = shouldBeOpen ? posicaoAberta : posicaoFechada;
    }

    void PlaySound()
    {
        if (audioSource == null) return;

        if (shouldBeOpen && openSound)
            audioSource.PlayOneShot(openSound);
        else if (!shouldBeOpen && closeSound)
            audioSource.PlayOneShot(closeSound);
    }

    [ContextMenu("DEBUG - Abrir Porta")]
    private void DebugAbrir()
    {
        SetState(true);
    }

    [ContextMenu("DEBUG - Fechar Porta")]
    private void DebugFechar()
    {
        SetState(false);
    }

    [ContextMenu("DEBUG - Reconfigurar Porta")]
    private void DebugReconfigurar()
    {
        ConfigurarAnimacaoPorBorda();
        AplicarEstadoInstantaneo();
    }
}
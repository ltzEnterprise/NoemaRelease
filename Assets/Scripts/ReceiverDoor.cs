using UnityEngine;
using System.Collections;

public class ReceiverDoor : MonoBehaviour
{
    [Header("--- SAVE NATIVO ---")]
    public string doorID = "";

    [Header("--- VISUAL ---")]
    public Transform doorVisual; 

    public enum LadoSumir { Esquerda, Direita, Cima, Baixo }
    public LadoSumir direcaoParaSumir = LadoSumir.Esquerda;

    [Header("Animação")]
    public float speed = 5f;

    [Header("Áudio")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;

    private bool shouldBeOpen = false;
    private Transform pivotAutomatico;
    private Vector3 escalaFechada;
    private Vector3 escalaAberta;

    private bool inicializado = false;

    void Start()
    {
        if (string.IsNullOrEmpty(doorID)) doorID = gameObject.name;

        if (doorVisual != null)
        {
            Vector3 tamanhoLocal = doorVisual.localScale;
            MeshFilter mf = doorVisual.GetComponentInChildren<MeshFilter>();

            if (mf != null)
            {
                tamanhoLocal = mf.sharedMesh.bounds.size;
                tamanhoLocal.Scale(doorVisual.localScale);
            }

            pivotAutomatico = new GameObject(doorVisual.name + "_PivotSumico").transform;
            pivotAutomatico.SetParent(doorVisual.parent);

            Vector3 posicaoDaBorda = doorVisual.localPosition;

            if (direcaoParaSumir == LadoSumir.Esquerda) posicaoDaBorda.x -= tamanhoLocal.x / 2f;
            else if (direcaoParaSumir == LadoSumir.Direita) posicaoDaBorda.x += tamanhoLocal.x / 2f;
            else if (direcaoParaSumir == LadoSumir.Cima) posicaoDaBorda.y += tamanhoLocal.y / 2f;
            else if (direcaoParaSumir == LadoSumir.Baixo) posicaoDaBorda.y -= tamanhoLocal.y / 2f;

            pivotAutomatico.localPosition = posicaoDaBorda;
            pivotAutomatico.localRotation = doorVisual.localRotation;

            doorVisual.SetParent(pivotAutomatico);

            escalaFechada = pivotAutomatico.localScale;
            escalaAberta = escalaFechada;

            if (direcaoParaSumir == LadoSumir.Esquerda || direcaoParaSumir == LadoSumir.Direita)
                escalaAberta.x = 0f;
            else
                escalaAberta.y = 0f;
        }

        StartCoroutine(CarregarSeguro());
    }

    IEnumerator CarregarSeguro()
    {
        yield return new WaitUntil(() =>
            PersistenciaManager.Instance != null &&
            PersistenciaManager.Instance.DadosProntosParaUso &&
            !PersistenciaManager.Instance.EstaCarregando
        );

        if (!string.IsNullOrEmpty(doorID))
        {
            shouldBeOpen = PersistenciaManager.Instance.ObterEstado(doorID);
        }

        if (pivotAutomatico != null)
        {
            pivotAutomatico.localScale = shouldBeOpen ? escalaAberta : escalaFechada;
        }

        inicializado = true;
    }

    void Update()
    {
        if (!inicializado || pivotAutomatico == null) return;

        Vector3 alvo = shouldBeOpen ? escalaAberta : escalaFechada;
        pivotAutomatico.localScale = Vector3.Lerp(pivotAutomatico.localScale, alvo, Time.deltaTime * speed);
    }

    public void SetState(bool shouldOpen)
    {
        if (shouldBeOpen == shouldOpen) return;

        shouldBeOpen = shouldOpen;
        PlaySound();

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(doorID))
        {
            PersistenciaManager.Instance.RegistrarEstado(doorID, shouldBeOpen);

            // 🔥 SAVE CORRETO (via GameManager)
            if (GameManager.Instance != null && GameManager.CenaPronta)
                GameManager.Instance.SalvarProgresso();
        }
    }

    void PlaySound()
    {
        if (audioSource != null)
        {
            if (shouldBeOpen && openSound) audioSource.PlayOneShot(openSound);
            else if (!shouldBeOpen && closeSound) audioSource.PlayOneShot(closeSound);
        }
    }
}
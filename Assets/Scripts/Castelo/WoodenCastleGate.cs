using UnityEngine;
using System.Collections;

public class WoodenCastleGate : MonoBehaviour
{
    [Header("Configurações")]
    public float alturaParaSubir = 4f; 
    public float velocidade = 2f;

    [Header("Áudio")]
    public AudioSource audioSource;
    public AudioClip somPortaMovendo;

    private bool estaAberta = false;
    private Vector3 posicaoInicial;
    private Vector3 posicaoFinal;

    void Start()
    {
        posicaoInicial = transform.position;
        posicaoFinal = transform.position + new Vector3(0, alturaParaSubir, 0);
    }

    // CHAMADO PELO KEYPAD
    public void AbrirPeloKeypad()
    {
        if (!estaAberta)
        {
            Debug.Log("Portão do Castelo abrindo...");
            StartCoroutine(AbrirPortaAnimacao());
        }
    }

    // CHAMADO PELO QUIZ DOOR (Demo Mode)
    public void FecharPortaAbruptamente()
    {
        if (!estaAberta) return;

        estaAberta = false;
        StopAllCoroutines();
        transform.position = posicaoInicial;
        Debug.Log("Portão fechado abruptamente.");
    }

    IEnumerator AbrirPortaAnimacao()
    {
        estaAberta = true;
        if (audioSource && somPortaMovendo) audioSource.PlayOneShot(somPortaMovendo);

        float tempo = 0;
        Vector3 startPos = transform.position;

        while (tempo < 1f)
        {
            tempo += Time.deltaTime * velocidade;
            transform.position = Vector3.Lerp(startPos, posicaoFinal, tempo);
            yield return null;
        }
        transform.position = posicaoFinal;
    }
}
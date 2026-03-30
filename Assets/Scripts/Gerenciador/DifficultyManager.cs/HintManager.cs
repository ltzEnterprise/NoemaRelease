using UnityEngine;
using TMPro;
using System.Collections;

public class HintManager : MonoBehaviour
{
    public static HintManager Instance;

    [Header("UI da Dica")]
    public TextMeshProUGUI textoDica;

    [Header("Configurações Base")]
    public float tempoFade = 1f;

    private Coroutine fadeCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Garante que o texto começa invisível no ecrã
        if (textoDica != null)
        {
            textoDica.color = new Color(textoDica.color.r, textoDica.color.g, textoDica.color.b, 0f);
        }
    }

    // O Cérebro recebe a ordem e processa
    public void MostrarDica(string mensagem, float tempoExibicao, bool infinito = false)
    {
        if (textoDica == null) return;
        
        // Se já houver uma dica, corta-a e começa a nova
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(RotinaFade(mensagem, tempoExibicao, infinito));
    }

    // Usado para forçar uma dica infinita a desaparecer (ex: o puzzle foi resolvido)
    public void EsconderDica()
    {
        if (textoDica == null || textoDica.color.a <= 0.01f) return;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(RotinaFadeOut());
    }

    IEnumerator RotinaFade(string mensagem, float tempo, bool infinito)
    {
        textoDica.text = mensagem;
        Color cor = textoDica.color;

        // FADE IN (Aparecer)
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / tempoFade;
            textoDica.color = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(0f, 1f, t));
            yield return null;
        }

        // Se for infinita, a corrotina pára aqui e a dica fica no ecrã para sempre
        if (infinito) yield break; 

        // ESPERA
        yield return new WaitForSeconds(tempo);

        // FADE OUT (Desaparecer)
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / tempoFade;
            textoDica.color = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }
    }

    IEnumerator RotinaFadeOut()
    {
        Color cor = textoDica.color;
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime / tempoFade;
            textoDica.color = new Color(cor.r, cor.g, cor.b, Mathf.Lerp(cor.a, 0f, t));
            yield return null;
        }
    }
}
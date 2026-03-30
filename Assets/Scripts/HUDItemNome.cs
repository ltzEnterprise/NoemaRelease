using UnityEngine;
using TMPro; 
using System.Collections;

public class HUDItemNome : MonoBehaviour
{
    public static HUDItemNome Instance;
    
    [Header("UI")]
    public TextMeshProUGUI textoDoItem; 
    
    [Header("Configuração")]
    public float tempoVisivel = 2.0f; 
    public float velocidadeFade = 2.0f; 

    private Coroutine coroutineAtual;
    private bool estavaTravado = false; // Radar de interação

    void Awake()
    {
        Instance = this;
        if (textoDoItem) 
        {
            textoDoItem.alpha = 0; 
            textoDoItem.text = "";
        }
    }

    void Update()
    {
        // Se o jogador ACABOU de abrir um painel (passou de livre para travado)
        if (FPS_Master.travadoInteracao && !estavaTravado)
        {
            ForcarSumir();
        }
        
        estavaTravado = FPS_Master.travadoInteracao;
    }

    // Função marreta pra limpar a tela na hora
    public void ForcarSumir()
    {
        if (coroutineAtual != null) StopCoroutine(coroutineAtual);
        if (textoDoItem) textoDoItem.alpha = 0;
    }

    // Usado quando você TROCA de arma (Scroll do mouse)
    public void MostrarNome(string nome)
    {
        if (!textoDoItem) return;
        if (coroutineAtual != null) StopCoroutine(coroutineAtual);
        
        textoDoItem.text = nome;
        textoDoItem.alpha = 1; 
        
        coroutineAtual = StartCoroutine(ProcessoDeFadeOut());
    }

    // Usado quando você PEGA um item novo do chão/cofre
    public void MostrarFadeDeColeta(string nome)
    {
        if (!textoDoItem) return;
        if (coroutineAtual != null) StopCoroutine(coroutineAtual);
        
        textoDoItem.text = "Item Adquirido:\n" + nome;
        coroutineAtual = StartCoroutine(ProcessoDeFadeCompleto());
    }

    IEnumerator ProcessoDeFadeOut()
    {
        yield return new WaitForSeconds(tempoVisivel);
        while (textoDoItem.alpha > 0)
        {
            textoDoItem.alpha -= Time.deltaTime * velocidadeFade;
            yield return null;
        }
        textoDoItem.alpha = 0;
    }

    IEnumerator ProcessoDeFadeCompleto()
    {
        // Fade IN
        while (textoDoItem.alpha < 1)
        {
            textoDoItem.alpha += Time.deltaTime * velocidadeFade;
            yield return null;
        }
        textoDoItem.alpha = 1;

        // Espera
        yield return new WaitForSeconds(tempoVisivel);

        // Fade OUT
        while (textoDoItem.alpha > 0)
        {
            textoDoItem.alpha -= Time.deltaTime * velocidadeFade;
            yield return null;
        }
        textoDoItem.alpha = 0;
    }
}
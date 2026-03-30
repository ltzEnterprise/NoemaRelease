using UnityEngine;
using UnityEngine.UI; // <-- Precisa disso pra mexer na Imagem da tela preta
using UnityEngine.SceneManagement;
using System.Collections; 
using QuantumTek.SimpleMenu; 

public class Cama : MonoBehaviour
{
    [Header("Configurações")]
    public string nomeDaCenaSonho;
    public string teclaParaDormir = "e";
    
    [Header("Interface")]
    public GameObject textoAviso; 

    [Header("--- EFEITO DE DORMIR ---")]
    public Image telaPreta; // Arraste uma imagem preta gigante do Canvas aqui
    public AudioSource somDeDormir; // O som de lençol, bocejo, etc.
    public float tempoFadePreto = 2f;

    [Header("--- TELA DE LOADING ---")]
    public GameObject painelLoading;          
    public SM_Bar barraDeProgresso; 

    [Header("--- ÁUDIO AMBIENTE (OPCIONAL) ---")]
    public AudioSource musicaAmbiente;
    public float tempoDeFadeMusica = 1.5f;

    private bool playerPerto = false;
    private bool indoDormir = false; 

    void Update()
    {
        if (indoDormir) return;

        if (playerPerto && Input.GetKeyDown(teclaParaDormir))
        {
            Dormir();
        }
    }

    void Dormir()
    {
        indoDormir = true; 
        Debug.Log("Indo para o mundo dos sonhos...");
        
        if (textoAviso != null) textoAviso.SetActive(false); 

        // Chama a rotina do fade preto PRIMEIRO
        StartCoroutine(RotinaFadePretoELoading(nomeDaCenaSonho));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (indoDormir) return;

        if (other.CompareTag("Player"))
        {
            playerPerto = true;
            if (textoAviso != null) textoAviso.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerPerto = false;
            if (textoAviso != null) textoAviso.SetActive(false);
        }
    }

    public void LigarDesligarPainel(GameObject painel, bool estado)
    {
        if (painel == null) return;
        
        painel.SetActive(estado);
        
        SM_Window janela = painel.GetComponent<SM_Window>();
        if (janela != null) janela.Toggle(estado);

        if (estado == true)
        {
            foreach (Transform filho in painel.transform)
            {
                filho.gameObject.SetActive(true);
            }
        }
    }

    // --- CORROTINA: FADE PRETO ANTES DE TUDO ---
    IEnumerator RotinaFadePretoELoading(string nomeCena)
    {
        // 1. Liga a imagem e garante que tá transparente
        if (telaPreta != null)
        {
            telaPreta.gameObject.SetActive(true);
            Color cor = telaPreta.color;
            cor.a = 0f;
            telaPreta.color = cor;
        }

        // 2. Toca o som (se você tiver arrastado algum)
        if (somDeDormir != null)
        {
            somDeDormir.Play();
        }

        // 3. Vai escurecendo a tela aos poucos
        float tempoPassado = 0f;
        while (tempoPassado < tempoFadePreto)
        {
            tempoPassado += Time.unscaledDeltaTime;
            if (telaPreta != null)
            {
                Color cor = telaPreta.color;
                cor.a = Mathf.Lerp(0f, 1f, tempoPassado / tempoFadePreto);
                telaPreta.color = cor;
            }
            yield return null;
        }

        // Garante que ficou 100% preto
        if (telaPreta != null)
        {
            Color corFinal = telaPreta.color;
            corFinal.a = 1f;
            telaPreta.color = corFinal;
        }

        // 4. AGORA SIM, com tudo escuro, ele chama a tela de loading e corta a música
        StartCoroutine(RotinaLoadingPorcentagem(nomeCena));
    }

    IEnumerator FadeOutMusica()
    {
        if (musicaAmbiente == null) yield break;
        
        float volumeInicial = musicaAmbiente.volume;
        float tempoPassado = 0f;

        while (tempoPassado < tempoDeFadeMusica)
        {
            tempoPassado += Time.unscaledDeltaTime;
            musicaAmbiente.volume = Mathf.Lerp(volumeInicial, 0f, tempoPassado / tempoDeFadeMusica);
            yield return null;
        }
        musicaAmbiente.volume = 0f;
    }

    IEnumerator RotinaLoadingPorcentagem(string nomeCena)
    {
        StartCoroutine(FadeOutMusica()); 
        
        LigarDesligarPainel(painelLoading, true);

        if (barraDeProgresso) barraDeProgresso.SetFill(0f); 

        AsyncOperation operacao = SceneManager.LoadSceneAsync(nomeCena);
        operacao.allowSceneActivation = false; 

        float progressoVisual = 0f;

        while (progressoVisual < 0.8f)
        {
            float progressoReal = Mathf.Clamp01(operacao.progress / 0.9f) * 0.8f;
            progressoVisual = Mathf.MoveTowards(progressoVisual, progressoReal, Time.unscaledDeltaTime * 0.8f); 

            if (barraDeProgresso) barraDeProgresso.SetFill(progressoVisual);

            if (operacao.progress >= 0.9f && progressoVisual >= 0.79f)
            {
                progressoVisual = 0.8f;
                if (barraDeProgresso) barraDeProgresso.SetFill(progressoVisual);
                break;
            }

            yield return null;
        }

        float tempoExtra = 0f;
        while (tempoExtra < 2f)
        {
            tempoExtra += Time.unscaledDeltaTime;
            progressoVisual = Mathf.Lerp(0.8f, 1f, tempoExtra / 2f); 
            
            if (barraDeProgresso) barraDeProgresso.SetFill(progressoVisual);
            
            yield return null;
        }

        operacao.allowSceneActivation = true;
    }
}
using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;

public class PillarManager : MonoBehaviour
{
    [Header("Save do Puzzle (MUDE ISSO EM CADA PUZZLE NOVO)")]
    [Tooltip("Dê um nome único, ex: Puzzle_Casa ou Puzzle_Mundo_1")]
    public string idDoPuzzle = "Puzzle_Pilares_1";
    public Transform playerTransform; 

    [Header("Configuração da Senha")]
    [Tooltip("Coloque apenas o TOTAL de pilares. A senha gerada será sempre 1, 2, 3... até esse número.")]
    public int totalDePilares = 6; 
    
    [Header("--- SISTEMA DE TEXTO (PROGRESSO) ---")]
    [Tooltip("Ordem desse puzzle no mapa (Ex: 1 para o primeiro, 2 para o segundo)")]
    public int ordemDoPuzzle = 1; 
    public TextMeshProUGUI textoProgresso;

    [Tooltip("Coloque um Canvas Group no objeto do texto e arraste aqui para o Fade funcionar")]
    public CanvasGroup canvasGroupDoTexto; 

    private static int puzzleAtualNaTela = 1; 
    public static List<PillarManager> todosOsPuzzles = new List<PillarManager>();

    private List<int> inputsDoJogador = new List<int>();
    private bool puzzleConcluido = false;

    [Header("Recompensas (Plasma Branco)")]
    public GameObject pastaDePlasmas;

    [Header("Áudio Geral")]
    public AudioSource audioSourceFeedback;
    public AudioClip somPlasmaAbrindo; 
    public AudioClip somDespixalizacao; 

    [Header("Configurações do Fim do Glitch (Exclusivo do Mapa)")]
    public bool desativaMundoPixelado = false;
    public ScriptableRendererFeature efeitoGlitchURP;
    public CanvasGroup painelPretoUI;
    public float tempoTelaPreta = 1.5f;

    void Awake()
    {
        if (!todosOsPuzzles.Contains(this))
            todosOsPuzzles.Add(this);
    }

    void Start()
    {
        StartCoroutine(InicializarSeguro());
    }

    private IEnumerator InicializarSeguro()
    {
        if (totalDePilares <= 0)
        {
            Debug.LogError($"[ERRO] O Puzzle {idDoPuzzle} tá com 0 pilares configurados. Arruma isso no Inspector!");
        }

        if (painelPretoUI != null)
        {
            painelPretoUI.alpha = 0f;
            painelPretoUI.gameObject.SetActive(false);
        }

        yield return new WaitUntil(() =>
            PersistenciaManager.Instance != null &&
            PersistenciaManager.Instance.DadosProntosParaUso
        );

        CarregarSave();

        yield return StartCoroutine(SetupInicialDoTexto());
    }

    private void CarregarSave()
    {
        if (PersistenciaManager.Instance == null) return;

        puzzleConcluido = PersistenciaManager.Instance.ObterEstado(idDoPuzzle + "_Concluido", false);

        if (puzzleConcluido)
        {
            if (pastaDePlasmas != null)
                pastaDePlasmas.SetActive(false);

            if (desativaMundoPixelado && efeitoGlitchURP != null)
            {
                efeitoGlitchURP.SetActive(false);
                PersistenciaManager.Instance.RegistrarEstado("World_Is_Pixelated", false);
            }
        }
    }

    private IEnumerator SetupInicialDoTexto()
    {
        yield return new WaitForEndOfFrame();
        
        int menorOrdemIncompleta = 999;

        foreach (var p in todosOsPuzzles) 
        {
            if (p != null && !p.puzzleConcluido && p.ordemDoPuzzle < menorOrdemIncompleta) 
            {
                menorOrdemIncompleta = p.ordemDoPuzzle;
            }
        }
        
        puzzleAtualNaTela = menorOrdemIncompleta;

        if (ordemDoPuzzle == puzzleAtualNaTela && !puzzleConcluido)
        {
            IniciarTextoDessePuzzle();
        }
    }

    public void IniciarTextoDessePuzzle()
    {
        if (textoProgresso != null)
        {
            textoProgresso.gameObject.SetActive(true);
            textoProgresso.text = $"0/{totalDePilares}";

            if (canvasGroupDoTexto != null)
                canvasGroupDoTexto.alpha = 1f;
        }
    }

    void Update()
    {
        if (!puzzleConcluido && pastaDePlasmas != null && DayNightCycle.Instance != null)
        {
            if (DayNightCycle.Instance.isNight && !pastaDePlasmas.activeSelf)
            {
                pastaDePlasmas.SetActive(true); 
            }
            else if (!DayNightCycle.Instance.isNight && pastaDePlasmas.activeSelf)
            {
                pastaDePlasmas.SetActive(false); 
            }
        }
    }

    public void ReceberInteracaoPilar(int numeroDoPilar)
    {
        if (puzzleConcluido) return; 

        int proximoEsperado = inputsDoJogador.Count + 1;

        if (numeroDoPilar == proximoEsperado)
        {
            inputsDoJogador.Add(numeroDoPilar);
        }
        else
        {
            inputsDoJogador.Clear();

            if (numeroDoPilar == 1)
                inputsDoJogador.Add(1);
        }

        if (ordemDoPuzzle == puzzleAtualNaTela && textoProgresso != null)
        {
            textoProgresso.text = $"{inputsDoJogador.Count}/{totalDePilares}";
        }

        if (inputsDoJogador.Count == totalDePilares)
        {
            if (ordemDoPuzzle == puzzleAtualNaTela)
            {
                StartCoroutine(FadeOutTextoEChamarProximo());
            }

            StartCoroutine(SequenciaCompletada());
        }
    }

    private IEnumerator FadeOutTextoEChamarProximo()
    {
        yield return new WaitForSeconds(3f);

        if (canvasGroupDoTexto != null)
        {
            float t = 0f;

            while (t < 1f) 
            {
                t += Time.deltaTime;
                canvasGroupDoTexto.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            canvasGroupDoTexto.alpha = 0f;
        }

        yield return new WaitForSeconds(2f);

        puzzleAtualNaTela++;
        
        foreach (var p in todosOsPuzzles)
        {
            if (p != null && p.ordemDoPuzzle == puzzleAtualNaTela && !p.puzzleConcluido)
            {
                p.IniciarTextoDessePuzzle();
                break;
            }
        }
    }

    private IEnumerator SequenciaCompletada()
    {
        puzzleConcluido = true;

        if (desativaMundoPixelado)
        {
            if (painelPretoUI != null)
            {
                painelPretoUI.gameObject.SetActive(true);

                float t = 0f;
                float metadeDoTempo = tempoTelaPreta / 2f;
                
                while (t < metadeDoTempo)
                {
                    t += Time.deltaTime;
                    painelPretoUI.alpha = Mathf.Lerp(0f, 1f, t / metadeDoTempo);
                    yield return null;
                }

                painelPretoUI.alpha = 1f;
            }

            if (audioSourceFeedback && somDespixalizacao)
                audioSourceFeedback.PlayOneShot(somDespixalizacao);

            if (efeitoGlitchURP != null)
                efeitoGlitchURP.SetActive(false);

            if (PersistenciaManager.Instance != null)
                PersistenciaManager.Instance.RegistrarEstado("World_Is_Pixelated", false);

            yield return new WaitForSeconds(0.5f);

            if (audioSourceFeedback && somPlasmaAbrindo)
                audioSourceFeedback.PlayOneShot(somPlasmaAbrindo);
                
            if (pastaDePlasmas != null)
                pastaDePlasmas.SetActive(false); 

            if (painelPretoUI != null)
            {
                float t = 0f;
                float metadeDoTempo = tempoTelaPreta / 2f;
                
                while (t < metadeDoTempo)
                {
                    t += Time.deltaTime;
                    painelPretoUI.alpha = Mathf.Lerp(1f, 0f, t / metadeDoTempo);
                    yield return null;
                }

                painelPretoUI.alpha = 0f;
                painelPretoUI.gameObject.SetActive(false);
            }
        }
        else
        {
            if (audioSourceFeedback && somPlasmaAbrindo)
                audioSourceFeedback.PlayOneShot(somPlasmaAbrindo);
                
            if (pastaDePlasmas != null)
                pastaDePlasmas.SetActive(false); 
        }

        SalvarProgressoEPlayer();
    }

    private void SalvarProgressoEPlayer()
    {
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(idDoPuzzle + "_Concluido", true);

            if (desativaMundoPixelado)
                PersistenciaManager.Instance.RegistrarEstado("World_Is_Pixelated", false);
        }

        if (SistemaGlobal.Instance != null && playerTransform != null)
        {
            SistemaGlobal.Instance.SalvarJogo(playerTransform.position, UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
        else if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarTudo(false);
        }
    }

    void OnDisable()
    {
        if (efeitoGlitchURP != null)
            efeitoGlitchURP.SetActive(false);
    }

    void OnDestroy()
    {
        if (efeitoGlitchURP != null)
            efeitoGlitchURP.SetActive(false);

        if (todosOsPuzzles.Contains(this))
            todosOsPuzzles.Remove(this);
    }
}
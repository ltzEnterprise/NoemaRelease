using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro;
using UnityEngine.SceneManagement;

public class PillarManager : MonoBehaviour
{
    [Header("Save do Puzzle (MUDE ISSO EM CADA PUZZLE NOVO)")]
    [Tooltip("Dê um nome único, ex: Puzzle_Casa ou Puzzle_Mundo_1")]
    public string idDoPuzzle = "Puzzle_Pilares_1";
    public Transform playerTransform; 

    [Header("Configuração da Senha")]
    [Tooltip("Coloque apenas o TOTAL de pilares. Se ordemCorretaPilares estiver vazia, a senha gerada será sempre 1, 2, 3... até esse número.")]
    public int totalDePilares = 6; 

    [Tooltip("Opcional. Se preencher, essa será a ordem correta dos pilares. Ex: 2, 5, 1, 4, 3, 6. Se deixar vazio, usa 1,2,3...")]
    public List<int> ordemCorretaPilares = new List<int>();

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
    private bool saveCarregado = false;
    private bool completando = false;

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
        RegistrarEstePuzzle();
    }

    void OnEnable()
    {
        RegistrarEstePuzzle();
    }

    void Start()
    {
        StartCoroutine(InicializarSeguro());
    }

    private void RegistrarEstePuzzle()
    {
        todosOsPuzzles.RemoveAll(p => p == null);

        if (!todosOsPuzzles.Contains(this))
            todosOsPuzzles.Add(this);
    }

    private IEnumerator InicializarSeguro()
    {
        RegistrarEstePuzzle();

        if (totalDePilares <= 0)
        {
            Debug.LogError($"[ERRO] O Puzzle {idDoPuzzle} tá com 0 pilares configurados. Arruma isso no Inspector!");
        }

        if (painelPretoUI != null)
        {
            painelPretoUI.alpha = 0f;
            painelPretoUI.gameObject.SetActive(false);
        }

        if (textoProgresso != null)
            textoProgresso.gameObject.SetActive(false);

        if (canvasGroupDoTexto != null)
            canvasGroupDoTexto.alpha = 0f;

        yield return new WaitUntil(() =>
            PersistenciaManager.Instance != null &&
            PersistenciaManager.Instance.DadosProntosParaUso &&
            !PersistenciaManager.Instance.EstaCarregando
        );

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                playerTransform = player.transform;
        }

        CarregarSave();
        saveCarregado = true;

        AplicarEstadoVisualDoPuzzle();

        yield return null;
        yield return new WaitForEndOfFrame();

        ForcarAtualizacaoGlobalDosPilares();
    }

    private void CarregarSave()
    {
        if (PersistenciaManager.Instance == null) return;

        puzzleConcluido = PersistenciaManager.Instance.ObterEstado(idDoPuzzle + "_Concluido", false);

        if (desativaMundoPixelado && PixeladoFoiDesativadoPermanentemente())
        {
            puzzleConcluido = true;
        }

        if (puzzleConcluido)
        {
            inputsDoJogador.Clear();

            if (pastaDePlasmas != null)
                pastaDePlasmas.SetActive(false);

            if (desativaMundoPixelado)
                AplicarPixeladoDesativadoPermanente();
        }
    }

    private List<int> ObterOrdemCorreta()
    {
        if (ordemCorretaPilares != null && ordemCorretaPilares.Count > 0)
            return ordemCorretaPilares;

        List<int> ordemPadrao = new List<int>();

        for (int i = 1; i <= totalDePilares; i++)
            ordemPadrao.Add(i);

        return ordemPadrao;
    }

    private int ObterTotalSequencia()
    {
        List<int> ordem = ObterOrdemCorreta();
        return ordem != null && ordem.Count > 0 ? ordem.Count : totalDePilares;
    }

    private static void ForcarAtualizacaoGlobalDosPilares()
    {
        RecalcularPuzzleAtualNaTela();
        AtualizarTextoDeTodosOsPuzzles();
    }

    private static void RecalcularPuzzleAtualNaTela()
    {
        todosOsPuzzles.RemoveAll(p => p == null);

        int menorOrdemIncompleta = int.MaxValue;

        foreach (var p in todosOsPuzzles)
        {
            if (p == null) continue;
            if (!p.saveCarregado) continue;
            if (p.puzzleConcluido) continue;

            if (p.ordemDoPuzzle < menorOrdemIncompleta)
                menorOrdemIncompleta = p.ordemDoPuzzle;
        }

        puzzleAtualNaTela = menorOrdemIncompleta == int.MaxValue ? -1 : menorOrdemIncompleta;
    }

    private static void AtualizarTextoDeTodosOsPuzzles()
    {
        todosOsPuzzles.RemoveAll(p => p == null);

        foreach (var p in todosOsPuzzles)
        {
            if (p == null) continue;

            if (p.saveCarregado && !p.puzzleConcluido && p.ordemDoPuzzle == puzzleAtualNaTela)
                p.MostrarTextoProgresso();
            else
                p.EsconderTextoProgresso();
        }
    }

    private void MostrarTextoProgresso()
    {
        if (textoProgresso != null)
        {
            Canvas canvasPai = textoProgresso.GetComponentInParent<Canvas>(true);

            if (canvasPai != null)
                canvasPai.gameObject.SetActive(true);

            textoProgresso.gameObject.SetActive(true);
            textoProgresso.text = $"{inputsDoJogador.Count}/{ObterTotalSequencia()}";
        }

        if (canvasGroupDoTexto != null)
        {
            canvasGroupDoTexto.gameObject.SetActive(true);
            canvasGroupDoTexto.alpha = 1f;
            canvasGroupDoTexto.blocksRaycasts = false;
            canvasGroupDoTexto.interactable = false;
        }
    }

    private void EsconderTextoProgresso()
    {
        if (textoProgresso != null)
            textoProgresso.gameObject.SetActive(false);

        if (canvasGroupDoTexto != null)
            canvasGroupDoTexto.alpha = 0f;
    }

    void Update()
    {
        if (desativaMundoPixelado && PixeladoFoiDesativadoPermanentemente())
        {
            AplicarPixeladoDesativadoPermanente();
        }

        if (puzzleConcluido) return;

        if (pastaDePlasmas != null && DayNightCycle.Instance != null)
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
        if (!saveCarregado)
        {
            CarregarSave();
            saveCarregado = true;
            ForcarAtualizacaoGlobalDosPilares();
        }

        if (puzzleConcluido) return; 
        if (completando) return;

        List<int> ordem = ObterOrdemCorreta();

        if (ordem == null || ordem.Count == 0)
            return;

        int indiceEsperado = inputsDoJogador.Count;

        if (indiceEsperado >= ordem.Count)
        {
            inputsDoJogador.Clear();
            indiceEsperado = 0;
        }

        int proximoEsperado = ordem[indiceEsperado];

        if (numeroDoPilar == proximoEsperado)
        {
            inputsDoJogador.Add(numeroDoPilar);
        }
        else
        {
            inputsDoJogador.Clear();

            if (numeroDoPilar == ordem[0])
                inputsDoJogador.Add(numeroDoPilar);
        }

        if (ordemDoPuzzle == puzzleAtualNaTela && textoProgresso != null)
        {
            MostrarTextoProgresso();
            textoProgresso.text = $"{inputsDoJogador.Count}/{ordem.Count}";
        }

        if (inputsDoJogador.Count == ordem.Count)
        {
            StartCoroutine(SequenciaCompletada());
        }
    }

    private IEnumerator SequenciaCompletada()
    {
        if (completando) yield break;

        completando = true;
        puzzleConcluido = true;
        inputsDoJogador.Clear();

        SalvarEstadoConcluidoImediatamente();

        yield return null;

        ForcarLiberacaoDoProximoPuzzle();

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

            AplicarPixeladoDesativadoPermanente();

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

        completando = false;

        ForcarLiberacaoDoProximoPuzzle();
    }

    private void ForcarLiberacaoDoProximoPuzzle()
    {
        todosOsPuzzles.RemoveAll(p => p == null);

        foreach (var p in todosOsPuzzles)
        {
            if (p == null) continue;

            if (!p.saveCarregado)
            {
                p.CarregarSave();
                p.saveCarregado = true;
            }

            if (!p.puzzleConcluido)
                p.inputsDoJogador.Clear();
        }

        RecalcularPuzzleAtualNaTela();
        AtualizarTextoDeTodosOsPuzzles();

        Debug.Log("[PillarManager] Puzzle concluído: " + idDoPuzzle + ". Próximo puzzle atual: " + puzzleAtualNaTela);
    }

    private void AplicarEstadoVisualDoPuzzle()
    {
        if (!puzzleConcluido) return;

        if (pastaDePlasmas != null)
            pastaDePlasmas.SetActive(false);

        if (desativaMundoPixelado)
            AplicarPixeladoDesativadoPermanente();
    }

    private void SalvarEstadoConcluidoImediatamente()
    {
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(idDoPuzzle + "_Concluido", true);

            if (desativaMundoPixelado)
            {
                PersistenciaManager.Instance.RegistrarEstado(ChavePixeladoDesativadoDoPuzzle(), true);
                PersistenciaManager.Instance.RegistrarEstado("World_Is_Pixelated", false);
                PersistenciaManager.Instance.RegistrarEstado("World_Pixelated_Disabled", true);
                PersistenciaManager.Instance.RegistrarEstado("PixelatedWorld_Disabled", true);
            }

            PersistenciaManager.Instance.SalvarTudo(true);
        }
    }

    private bool PixeladoFoiDesativadoPermanentemente()
    {
        if (PersistenciaManager.Instance == null)
            return false;

        return PersistenciaManager.Instance.ObterEstado(ChavePixeladoDesativadoDoPuzzle(), false) ||
               PersistenciaManager.Instance.ObterEstado("World_Pixelated_Disabled", false) ||
               PersistenciaManager.Instance.ObterEstado("PixelatedWorld_Disabled", false) ||
               PersistenciaManager.Instance.ObterEstado("World_Is_Pixelated", true) == false;
    }

    private void AplicarPixeladoDesativadoPermanente()
    {
        if (efeitoGlitchURP != null)
            efeitoGlitchURP.SetActive(false);

        if (pastaDePlasmas != null)
            pastaDePlasmas.SetActive(false);

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(ChavePixeladoDesativadoDoPuzzle(), true);
            PersistenciaManager.Instance.RegistrarEstado("World_Is_Pixelated", false);
            PersistenciaManager.Instance.RegistrarEstado("World_Pixelated_Disabled", true);
            PersistenciaManager.Instance.RegistrarEstado("PixelatedWorld_Disabled", true);
        }
    }

    private string ChavePixeladoDesativadoDoPuzzle()
    {
        return idDoPuzzle + "_PixeladoDesativadoPermanente";
    }

    void OnDisable()
    {
        if (efeitoGlitchURP != null && desativaMundoPixelado && PixeladoFoiDesativadoPermanentemente())
            efeitoGlitchURP.SetActive(false);
    }

    void OnDestroy()
    {
        if (efeitoGlitchURP != null && desativaMundoPixelado && PixeladoFoiDesativadoPermanentemente())
            efeitoGlitchURP.SetActive(false);

        if (todosOsPuzzles.Contains(this))
            todosOsPuzzles.Remove(this);

        ForcarAtualizacaoGlobalDosPilares();
    }
}
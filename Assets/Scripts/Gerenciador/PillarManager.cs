using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using UnityEngine.Rendering.Universal;
using TMPro; // Necessário pro TextMeshPro

public class PillarManager : MonoBehaviour
{
    [Header("Save do Puzzle (MUDE ISSO EM CADA PUZZLE NOVO)")]
    [Tooltip("Dê um nome único, ex: Puzzle_Casa ou Puzzle_Mundo_1")]
    public string idDoPuzzle = "Puzzle_Pilares_1";
    public Transform playerTransform; 

    [Header("Configuração da Senha")]
    [Tooltip("Coloque apenas o TOTAL de pilares. A senha gerada será sempre 1, 2, 3... até esse número.")]
    public int totalDePilares = 6; 
    
    // --- FEATURE NOVA AQUI: TEXTO E FILA ---
    [Header("--- SISTEMA DE TEXTO (PROGRESSO) ---")]
    [Tooltip("Ordem desse puzzle no mapa (Ex: 1 para o primeiro, 2 para o segundo)")]
    public int ordemDoPuzzle = 1; 
    public TextMeshProUGUI textoProgresso;
    [Tooltip("Coloque um Canvas Group no objeto do texto e arraste aqui para o Fade funcionar")]
    public CanvasGroup canvasGroupDoTexto; 

    // Variáveis estáticas para os scripts conversarem entre si sem bugar
    private static int puzzleAtualNaTela = 1; 
    public static List<PillarManager> todosOsPuzzles = new List<PillarManager>();
    // ----------------------------------------

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
        // Se registra na lista global pra gente saber quem é quem
        if (!todosOsPuzzles.Contains(this)) todosOsPuzzles.Add(this);
    }

    void Start()
    {
        if (totalDePilares <= 0)
        {
            Debug.LogError($"[ERRO] O Puzzle {idDoPuzzle} tá com 0 pilares configurados. Arruma isso no Inspector!");
        }

        if (painelPretoUI != null)
        {
            painelPretoUI.alpha = 0f;
            painelPretoUI.gameObject.SetActive(false); // Garante que começa invisível e não come performance
        }

        if (PlayerPrefs.GetInt(idDoPuzzle + "_Concluido", 0) == 1)
        {
            puzzleConcluido = true;
            
            if (pastaDePlasmas != null) pastaDePlasmas.SetActive(false);

            if (desativaMundoPixelado && efeitoGlitchURP != null)
            {
                efeitoGlitchURP.SetActive(false);
                PlayerPrefs.SetInt("World_Is_Pixelated", 0);
            }
        }

        // Inicia a lógica de interface no fim do frame pra garantir que todos os puzzles carregaram o save
        StartCoroutine(SetupInicialDoTexto());
    }

    private IEnumerator SetupInicialDoTexto()
    {
        yield return new WaitForEndOfFrame();
        
        // Procura qual é o primeiro puzzle que AINDA NÃO FOI FEITO pelo Save
        int menorOrdemIncompleta = 999;
        foreach(var p in todosOsPuzzles) 
        {
            if (!p.puzzleConcluido && p.ordemDoPuzzle < menorOrdemIncompleta) 
            {
                menorOrdemIncompleta = p.ordemDoPuzzle;
            }
        }
        
        puzzleAtualNaTela = menorOrdemIncompleta;

        // Se ESSE script for o puzzle da vez, ele liga o texto dele.
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
            if (canvasGroupDoTexto != null) canvasGroupDoTexto.alpha = 1f;
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

        // LÓGICA NOVA: ACERTOU CONTINUA, ERROU RESETA NA HORA
        int proximoEsperado = inputsDoJogador.Count + 1;

        if (numeroDoPilar == proximoEsperado)
        {
            // Acertou o pilar certo da ordem
            inputsDoJogador.Add(numeroDoPilar);
        }
        else
        {
            // Errou! Reseta a senha.
            inputsDoJogador.Clear();
            
            // Colher de chá: se ele apertou o pilar 1, a gente já conta como o primeiro acerto da nova tentativa
            if (numeroDoPilar == 1) inputsDoJogador.Add(1);
        }

        // Atualiza a UI se for o puzzle que tá ativo agora
        if (ordemDoPuzzle == puzzleAtualNaTela && textoProgresso != null)
        {
            textoProgresso.text = $"{inputsDoJogador.Count}/{totalDePilares}";
        }

        // Como a gente reseta quando erra, se a lista encheu, é porque ele acertou 100% da ordem
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
        // 1. O texto do sucesso (ex: 2/2) fica na tela por 3 segundos
        yield return new WaitForSeconds(3f);

        // 2. Fazer o Fade Out do texto
        if (canvasGroupDoTexto != null)
        {
            float t = 0;
            while(t < 1f) 
            {
                t += Time.deltaTime; // Fade de 1 segundo
                canvasGroupDoTexto.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            canvasGroupDoTexto.alpha = 0f;
        }

        // 3. Espera mais 2 segundos no vazio
        yield return new WaitForSeconds(2f);

        // 4. Manda a bola pro próximo puzzle da fila aparecer na tela
        puzzleAtualNaTela++;
        
        foreach(var p in todosOsPuzzles)
        {
            if (p.ordemDoPuzzle == puzzleAtualNaTela && !p.puzzleConcluido)
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
            // --- FADE IN PARA O PRETO ---
            if (painelPretoUI != null)
            {
                painelPretoUI.gameObject.SetActive(true);
                float t = 0;
                float metadeDoTempo = tempoTelaPreta / 2f;
                
                while (t < metadeDoTempo)
                {
                    t += Time.deltaTime;
                    painelPretoUI.alpha = Mathf.Lerp(0f, 1f, t / metadeDoTempo);
                    yield return null;
                }
                painelPretoUI.alpha = 1f;
            }

            // Toca o som do glitch no escuro
            if (audioSourceFeedback && somDespixalizacao)
                audioSourceFeedback.PlayOneShot(somDespixalizacao);

            // Troca o shader pra normalizar o mundo enquanto a tela tá preta (esconde o corte feio)
            if (efeitoGlitchURP != null)
            {
                efeitoGlitchURP.SetActive(false);
                PlayerPrefs.SetInt("World_Is_Pixelated", 0);
                PlayerPrefs.Save();
            }

            // Espera meio segundo no breu pra dar um suspense
            yield return new WaitForSeconds(0.5f);

            // Abre a porta de plasma
            if (audioSourceFeedback && somPlasmaAbrindo)
                audioSourceFeedback.PlayOneShot(somPlasmaAbrindo);
                
            if (pastaDePlasmas != null) pastaDePlasmas.SetActive(false); 

            // --- FADE OUT REVELANDO O MUNDO NORMAL ---
            if (painelPretoUI != null)
            {
                float t = 0;
                float metadeDoTempo = tempoTelaPreta / 2f;
                
                while (t < metadeDoTempo)
                {
                    t += Time.deltaTime;
                    painelPretoUI.alpha = Mathf.Lerp(1f, 0f, t / metadeDoTempo);
                    yield return null;
                }
                painelPretoUI.alpha = 0f;
                painelPretoUI.gameObject.SetActive(false); // Desliga a UI pra não atrapalhar o clique do mouse no jogo
            }
        }
        else
        {
            // Se não desativa o mundo, só abre o plasma normal
            if (audioSourceFeedback && somPlasmaAbrindo)
                audioSourceFeedback.PlayOneShot(somPlasmaAbrindo);
                
            if (pastaDePlasmas != null) pastaDePlasmas.SetActive(false); 
        }

        SalvarProgressoEPlayer();
    }

    private void SalvarProgressoEPlayer()
    {
        PlayerPrefs.SetInt(idDoPuzzle + "_Concluido", 1);

        if (playerTransform != null)
        {
            PlayerPrefs.SetFloat("Player3D_PosX", playerTransform.position.x);
            PlayerPrefs.SetFloat("Player3D_PosY", playerTransform.position.y);
            PlayerPrefs.SetFloat("Player3D_PosZ", playerTransform.position.z);
            PlayerPrefs.SetFloat("Player3D_RotY", playerTransform.eulerAngles.y);
            PlayerPrefs.SetInt("Player3D_HasSave", 1);
        }

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarTudo();
        }
    }

    void OnDisable()
    {
        if (efeitoGlitchURP != null) efeitoGlitchURP.SetActive(false);
    }

    void OnDestroy()
    {
        if (efeitoGlitchURP != null) efeitoGlitchURP.SetActive(false);
        if (todosOsPuzzles.Contains(this)) todosOsPuzzles.Remove(this);
    }
}
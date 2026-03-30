using System.Collections;
using System.Collections.Generic; 
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PillarManager : MonoBehaviour
{
    [Header("Save do Puzzle (MUDE ISSO EM CADA PUZZLE NOVO)")]
    [Tooltip("Dê um nome único, ex: Puzzle_Casa ou Puzzle_Mundo_1")]
    public string idDoPuzzle = "Puzzle_Pilares_1";
    public Transform playerTransform; 

    [Header("Configuração da Senha")]
    [Tooltip("Coloque apenas o TOTAL de pilares. A senha gerada será sempre 1, 2, 3... até esse número.")]
    public int totalDePilares = 6; // <--- SÓ ISSO AQUI AGORA
    
    // A memória contínua do jogador
    private List<int> inputsDoJogador = new List<int>();
    
    private bool puzzleConcluido = false;

    [Header("Recompensas (Plasma Branco)")]
    [Tooltip("Arraste aqui a Pasta que contém TODAS as barreiras de plasma DESTE puzzle.")]
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

    void Start()
    {
        if (totalDePilares <= 0)
        {
            Debug.LogError($"[ERRO] O Puzzle {idDoPuzzle} tá com 0 pilares configurados. Arruma isso no Inspector!");
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

        // 1. Grava o botão que o jogador acabou de apertar
        inputsDoJogador.Add(numeroDoPilar);

        // 2. Se a lista ficou maior que o total de pilares, apaga o mais antigo
        if (inputsDoJogador.Count > totalDePilares)
        {
            inputsDoJogador.RemoveAt(0);
        }

        // 3. Checa se a sequência apertada forma exatamente 1, 2, 3... até o total
        if (inputsDoJogador.Count == totalDePilares)
        {
            bool acertouTudo = true;
            
            for (int i = 0; i < totalDePilares; i++)
            {
                // Como o índice (i) começa em 0, o número do pilar esperado é sempre (i + 1)
                if (inputsDoJogador[i] != (i + 1))
                {
                    acertouTudo = false;
                    break;
                }
            }

            // 4. Se passou no teste, BINGO.
            if (acertouTudo)
            {
                StartCoroutine(SequenciaCompletada());
            }
        }
    }

    private IEnumerator SequenciaCompletada()
    {
        puzzleConcluido = true;

        if (desativaMundoPixelado)
        {
            if (painelPretoUI != null) painelPretoUI.alpha = 1f;

            if (audioSourceFeedback && somDespixalizacao)
                audioSourceFeedback.PlayOneShot(somDespixalizacao);

            yield return new WaitForSeconds(tempoTelaPreta / 2f);

            if (efeitoGlitchURP != null)
            {
                efeitoGlitchURP.SetActive(false);
                PlayerPrefs.SetInt("World_Is_Pixelated", 0);
                PlayerPrefs.Save();
            }

            yield return new WaitForSeconds(tempoTelaPreta / 2f);

            if (audioSourceFeedback && somPlasmaAbrindo)
                audioSourceFeedback.PlayOneShot(somPlasmaAbrindo);

            if (painelPretoUI != null) painelPretoUI.alpha = 0f;
        }
        else
        {
            if (audioSourceFeedback && somPlasmaAbrindo)
                audioSourceFeedback.PlayOneShot(somPlasmaAbrindo);
        }

        if (pastaDePlasmas != null) pastaDePlasmas.SetActive(false); 

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
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public enum PirateSymbol 
{ 
    Boat, Anchor, Compass, Chest, Key, Rum 
}

public class CaptainLeverPuzzle : MonoBehaviour
{
    [Header("--- SAVE SYSTEM ---")]
    public string uniqueID; 

    [Header("Configuração Base")]
    [Tooltip("Ordem dos símbolos de cima para baixo (1 a 6)")]
    public List<PirateSymbol> symbolOrder; 

    [Header("Referências")]
    public List<PirateLever> leverList; 

    [Header("Recompensa 1 - Runa")]
    public GameObject painelRecompensa; 
    public string nomeDaRuna = "Runa_Pirata"; 
    
    [Tooltip("Objeto do mapa que vai aparecer quando resolver o puzzle principal")]
    public GameObject itemRecompensaAparecer; 

    [Tooltip("Objeto do mapa que vai SUMIR quando resolver o puzzle principal")]
    public GameObject itemEsconderNaRuna; 
    
    public UnityEvent onPuzzleSolved; 

    [Header("--- COMBINAÇÃO PRINCIPAL ---")]
    [Tooltip("Marque as caixas correspondentes às alavancas que devem estar ativadas para resolver o puzzle principal.")]
    public List<bool> mainCombination; 

    [Header("Recompensa 2 - Segredo")]
    [Tooltip("Marque as caixas correspondentes às alavancas que devem ser ativadas.")]
    public List<bool> secretCombination; 

    public GameObject quadSegredo; 
    
    [Tooltip("Objeto do mapa que vai aparecer quando resolver o segredo")]
    public GameObject itemRecompensaAparecerSegredo;

    [Tooltip("Objeto do mapa que vai SUMIR quando resolver o segredo")]
    public GameObject itemEsconderNoSegredo;
    
    public AudioClip somSegredo;

    [Header("--- TEMPO DOS PAINÉIS ---")]
    public float tempoPainelRecompensa = 2f;
    
    private AudioSource audioSource;
    private bool isSolved = false;
    private bool isSecretSolved = false;
    private bool resetando = false;
    private bool inicializado = false;

    private Coroutine rotinaPainelPrincipal;
    private Coroutine rotinaPainelSegredo;

    void Start()
    {
        StartCoroutine(InicializarSeguro());
    }

    IEnumerator InicializarSeguro()
    {
        if (string.IsNullOrEmpty(uniqueID)) 
            Debug.LogError($"[ERRO] Puzzle do Capitão '{gameObject.name}' sem Unique ID. O Save não vai funcionar.");

        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (quadSegredo)
            quadSegredo.SetActive(false);

        if (painelRecompensa)
            painelRecompensa.SetActive(false);
        
        if (itemRecompensaAparecer)
            itemRecompensaAparecer.SetActive(false);

        if (itemRecompensaAparecerSegredo)
            itemRecompensaAparecerSegredo.SetActive(false);

        yield return new WaitUntil(() =>
            PersistenciaManager.Instance != null &&
            PersistenciaManager.Instance.DadosProntosParaUso
        );

        CarregarSave();

        inicializado = true;
    }

    void CarregarSave()
    {
        if (PersistenciaManager.Instance == null || string.IsNullOrEmpty(uniqueID))
            return;

        isSolved = PersistenciaManager.Instance.ObterEstado(uniqueID + "_resolvido", false);
        isSecretSolved = PersistenciaManager.Instance.ObterEstado(uniqueID + "_segredo", false);

        if (isSolved)
            AplicarEstadoRecompensaPrincipal(false);

        if (isSecretSolved)
            AplicarEstadoRecompensaSegredo(false);
    }

    public void CheckRules()
    {
        if (!inicializado) return;
        if (resetando) return;

        if (!isSolved)
        {
            CheckMainPuzzleRules();
        }
        else if (!isSecretSolved)
        {
            CheckSecretRules();
        }
    }

    void CheckMainPuzzleRules()
    {
        if (leverList == null || mainCombination == null) return;
        if (mainCombination.Count != 6 || leverList.Count != 6) return;

        for (int i = 0; i < 6; i++)
        {
            if (leverList[i] == null) return;
            if (leverList[i].isOn != mainCombination[i]) return;
        }

        SolvePuzzle();
    }

    void CheckSecretRules()
    {
        if (leverList == null || secretCombination == null) return;
        if (secretCombination.Count != 6 || leverList.Count != 6) return;

        for (int i = 0; i < 6; i++)
        {
            if (leverList[i] == null) return;
            if (leverList[i].isOn != secretCombination[i]) return;
        }

        SolveSecret();
    }

    void AplicarEstadoRecompensaPrincipal(bool mostrarPainelESom)
    {
        if (mostrarPainelESom)
        {
            if (painelRecompensa)
            {
                if (rotinaPainelPrincipal != null)
                    StopCoroutine(rotinaPainelPrincipal);

                rotinaPainelPrincipal = StartCoroutine(MostrarPainelTemporario(painelRecompensa, true));
            }

            if (audioSource && somSegredo)
                audioSource.PlayOneShot(somSegredo);
        }
        else
        {
            if (painelRecompensa)
                painelRecompensa.SetActive(false);
        }

        if (itemRecompensaAparecer)
            itemRecompensaAparecer.SetActive(true);

        if (itemEsconderNaRuna)
            itemEsconderNaRuna.SetActive(false);
    }

    void AplicarEstadoRecompensaSegredo(bool mostrarPainelESom)
    {
        if (mostrarPainelESom)
        {
            GameObject painelDoSegredo = quadSegredo != null ? quadSegredo : painelRecompensa;

            if (painelDoSegredo)
            {
                if (rotinaPainelSegredo != null)
                    StopCoroutine(rotinaPainelSegredo);

                rotinaPainelSegredo = StartCoroutine(MostrarPainelTemporario(painelDoSegredo, false));
            }

            if (audioSource && somSegredo)
                audioSource.PlayOneShot(somSegredo);
        }
        else
        {
            if (quadSegredo)
                quadSegredo.SetActive(false);
        }

        if (itemRecompensaAparecerSegredo)
            itemRecompensaAparecerSegredo.SetActive(true);

        if (itemEsconderNoSegredo)
            itemEsconderNoSegredo.SetActive(false);
    }

    IEnumerator MostrarPainelTemporario(GameObject painel, bool principal)
    {
        if (painel == null)
            yield break;

        painel.SetActive(true);

        yield return new WaitForSeconds(tempoPainelRecompensa);

        if (painel != null)
            painel.SetActive(false);

        if (principal)
            rotinaPainelPrincipal = null;
        else
            rotinaPainelSegredo = null;
    }

    void SolvePuzzle()
    {
        if (isSolved) return;

        isSolved = true;

        AplicarEstadoRecompensaPrincipal(true);

        if (InventarioRunas.Instance != null)
            InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRuna);
        else if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado("Runa_" + nomeDaRuna, true);

        if (onPuzzleSolved != null)
            onPuzzleSolved.Invoke();

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_resolvido", true);

            if (itemRecompensaAparecer)
                PersistenciaManager.Instance.RegistrarEstado(itemRecompensaAparecer.name, true);

            if (itemEsconderNaRuna)
                PersistenciaManager.Instance.RegistrarEstado(itemEsconderNaRuna.name, false);
            
            SalvarProgressoSeguro();
        }

        resetando = true;
        Invoke("ResetarAlavancas", 1.5f);
    }

    void ResetarAlavancas()
    {
        foreach (var lever in leverList)
        {
            if (lever != null)
                lever.ForceReset(); 
        }
        
        resetando = false;
    }

    void SolveSecret()
    {
        if (isSecretSolved) return;

        isSecretSolved = true;

        AplicarEstadoRecompensaSegredo(true);

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_segredo", true);
            
            if (itemRecompensaAparecerSegredo)
                PersistenciaManager.Instance.RegistrarEstado(itemRecompensaAparecerSegredo.name, true);

            if (itemEsconderNoSegredo)
                PersistenciaManager.Instance.RegistrarEstado(itemEsconderNoSegredo.name, false);
            
            SalvarProgressoSeguro();
        }
    }

    private void SalvarProgressoSeguro()
    {
        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(true);
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateID()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }
}
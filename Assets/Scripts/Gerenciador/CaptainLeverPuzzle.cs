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
    
    private AudioSource audioSource;
    private bool isSolved = false;
    private bool isSecretSolved = false;
    private bool resetando = false;
    private bool inicializado = false;

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

        isSolved = PersistenciaManager.Instance.ObterEstado(uniqueID + "_resolvido");
        isSecretSolved = PersistenciaManager.Instance.ObterEstado(uniqueID + "_segredo");

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
                painelRecompensa.SetActive(true);

            if (audioSource && somSegredo)
                audioSource.PlayOneShot(somSegredo);
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
            if (quadSegredo)
                quadSegredo.SetActive(true);

            if (audioSource && somSegredo)
                audioSource.PlayOneShot(somSegredo);
        }
        else
        {
            if (quadSegredo)
                quadSegredo.SetActive(true);
        }

        if (itemRecompensaAparecerSegredo)
            itemRecompensaAparecerSegredo.SetActive(true);

        if (itemEsconderNoSegredo)
            itemEsconderNoSegredo.SetActive(false);
    }

    void SolvePuzzle()
    {
        if (isSolved) return;

        isSolved = true;

        AplicarEstadoRecompensaPrincipal(true);

        if (InventarioRunas.Instance != null)
            InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRuna);

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
        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(false);
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateID()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }
}
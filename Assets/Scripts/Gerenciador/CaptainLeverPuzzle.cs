using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Linq;

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

    void Start()
    {
        if (string.IsNullOrEmpty(uniqueID)) 
            Debug.LogError($"[ERRO] Puzzle do Capitão '{gameObject.name}' sem Unique ID! O Save não vai funcionar.");

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (quadSegredo) quadSegredo.SetActive(false);
        if (painelRecompensa) painelRecompensa.SetActive(false);
        
        // Garante que os itens comecem no estado padrão antes de ler o save
        if (itemRecompensaAparecer) itemRecompensaAparecer.SetActive(false);
        if (itemRecompensaAparecerSegredo) itemRecompensaAparecerSegredo.SetActive(false);

        CarregarSave();
    }

    void CarregarSave()
    {
        if (PersistenciaManager.Instance == null || string.IsNullOrEmpty(uniqueID)) return;

        isSolved = PersistenciaManager.Instance.ObterEstado(uniqueID + "_resolvido");
        isSecretSolved = PersistenciaManager.Instance.ObterEstado(uniqueID + "_segredo");

        if (isSolved)
        {
            if (itemRecompensaAparecer) itemRecompensaAparecer.SetActive(true);
            if (itemEsconderNaRuna) itemEsconderNaRuna.SetActive(false);
        }

        if (isSecretSolved)
        {
            if (quadSegredo) quadSegredo.SetActive(true);
            if (itemRecompensaAparecerSegredo) itemRecompensaAparecerSegredo.SetActive(true);
            if (itemEsconderNoSegredo) itemEsconderNoSegredo.SetActive(false);
        }
    }

    public void CheckRules()
    {
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
        if (leverList.Count != 6 || symbolOrder.Count != 6) return;

        if (leverList.Count(l => l.isOn) != 3) return;

        int GetIndex(PirateSymbol s) => symbolOrder.IndexOf(s);
        bool IsOn(PirateSymbol s) => leverList[GetIndex(s)].isOn;

        int idxChest = GetIndex(PirateSymbol.Chest);
        int idxKey = GetIndex(PirateSymbol.Key);
        int idxRum = GetIndex(PirateSymbol.Rum);
        int idxBoat = GetIndex(PirateSymbol.Boat);

        bool chestNeighborActive = (idxChest > 0 && leverList[idxChest - 1].isOn) || (idxChest < 5 && leverList[idxChest + 1].isOn);
        if (!chestNeighborActive) return;

        if (IsOn(PirateSymbol.Key) && !IsOn(PirateSymbol.Chest)) return;

        if (Mathf.Abs(idxRum - idxBoat) == 1 && IsOn(PirateSymbol.Rum)) return;

        if (IsOn(PirateSymbol.Anchor) && IsOn(PirateSymbol.Compass)) return;
        
        if (Mathf.Abs(idxKey - idxRum) == 2)
        {
            if (leverList[(idxKey + idxRum) / 2].isOn) return;
        }

        SolvePuzzle();
    }

    void CheckSecretRules()
    {
        if (secretCombination.Count != 6) return;

        for (int i = 0; i < 6; i++)
        {
            if (leverList[i].isOn != secretCombination[i]) return;
        }

        SolveSecret();
    }

    void SolvePuzzle()
    {
        isSolved = true;
        
        if (painelRecompensa) painelRecompensa.SetActive(true);
        if (InventarioRunas.Instance != null) InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRuna);
        if (onPuzzleSolved != null) onPuzzleSolved.Invoke();

        if (itemRecompensaAparecer) itemRecompensaAparecer.SetActive(true);
        if (itemEsconderNaRuna) itemEsconderNaRuna.SetActive(false);

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_resolvido", true);
            
            // Grava o estado extra dos objetos, caso precisem ser lidos em outro lugar
            if (itemRecompensaAparecer) PersistenciaManager.Instance.RegistrarEstado(itemRecompensaAparecer.name, true);
            if (itemEsconderNaRuna) PersistenciaManager.Instance.RegistrarEstado(itemEsconderNaRuna.name, false);
            
            PersistenciaManager.Instance.SalvarTudo();
        }

        resetando = true;
        Invoke("ResetarAlavancas", 1.5f);
    }

    void ResetarAlavancas()
    {
        foreach(var lever in leverList)
        {
            if (lever != null) lever.ForceReset(); 
        }
        
        if (audioSource && somSegredo) audioSource.PlayOneShot(somSegredo, 0.5f);
        
        resetando = false;
    }

    void SolveSecret()
    {
        isSecretSolved = true;

        if (somSegredo && audioSource) audioSource.PlayOneShot(somSegredo);
        if (quadSegredo) quadSegredo.SetActive(true);

        if (itemRecompensaAparecerSegredo) itemRecompensaAparecerSegredo.SetActive(true);
        if (itemEsconderNoSegredo) itemEsconderNoSegredo.SetActive(false);

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_segredo", true);
            
            // Grava o estado extra dos objetos, caso precisem ser lidos em outro lugar
            if (itemRecompensaAparecerSegredo) PersistenciaManager.Instance.RegistrarEstado(itemRecompensaAparecerSegredo.name, true);
            if (itemEsconderNoSegredo) PersistenciaManager.Instance.RegistrarEstado(itemEsconderNoSegredo.name, false);
            
            PersistenciaManager.Instance.SalvarTudo();
        }
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateID() { uniqueID = System.Guid.NewGuid().ToString(); }
}
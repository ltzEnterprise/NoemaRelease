using UnityEngine;
using System.Collections.Generic;

public class RotationPuzzleManager : MonoBehaviour
{
    [Header("--- SAVE SYSTEM ---")]
    public string uniqueID = "PaintingPuzzle_01";

    [Header("--- PUZZLE PIECES ---")]
    public List<Transform> puzzlePieces;
    public Vector3 rotationAxis = new Vector3(0, 0, 1);

    [Header("--- MANUAL STARTING TURNS ---")]
    [Tooltip("Ordem de giros (1, 2 ou 3). Ex p/ 9 peças: 1,3,2,2,1,3,3,2,1")]
    public int[] manualStartingTurns;

    [Header("--- REWARD (THE 10th QUAD) ---")]
    public GameObject finalQuadToPhotograph;
    public int newPhotoID = 7;

    [Header("--- AUDIO ---")]
    public AudioSource audioSource;
    public AudioClip pieceTurnSound;
    public AudioClip puzzleSolvedSound;

    private bool isPuzzleSolved = false;
    private bool isPhotoTaken = false;
    private Dictionary<Transform, Quaternion> rotacoesCorretas = new Dictionary<Transform, Quaternion>();

    private bool verificarVitoriaPendente = false;

    void Awake()
    {
        if (string.IsNullOrEmpty(uniqueID)) 
            Debug.LogError($"[Puzzle] O puzzle {gameObject.name} tá sem UniqueID!");

        if (finalQuadToPhotograph) finalQuadToPhotograph.SetActive(false);

        foreach (Transform piece in puzzlePieces)
        {
            if (piece != null)
            {
                rotacoesCorretas[piece] = piece.localRotation;
                
                RotatingPiece script = piece.gameObject.GetComponent<RotatingPiece>();
                if (script == null) script = piece.gameObject.AddComponent<RotatingPiece>();
                script.Setup(this);
            }
        }
    }

    void Start()
    {
        LoadSaveData();
    }

    void LoadSaveData()
    {
        if (PersistenciaManager.Instance != null)
        {
            isPuzzleSolved = PersistenciaManager.Instance.ObterEstado(uniqueID + "_Solved");
            isPhotoTaken = PersistenciaManager.Instance.ObterEstado(uniqueID + "_PhotoTaken");
        }

        if (isPhotoTaken)
        {
            HideAllPieces();
        }
        else if (isPuzzleSolved)
        {
            HideAllPieces();
            if (finalQuadToPhotograph) finalQuadToPhotograph.SetActive(true);
        }
        else
        {
            // Se não ganhou ainda, aplica as rotações manuais que você escolheu
            ApplyManualTurns();
        }
    }

    void ApplyManualTurns()
    {
        for (int i = 0; i < puzzlePieces.Count; i++)
        {
            if (puzzlePieces[i] == null) continue;
            int turnos = 0;
            if (manualStartingTurns != null && i < manualStartingTurns.Length)
                turnos = manualStartingTurns[i];
            
            puzzlePieces[i].Rotate(rotationAxis * (90f * turnos), Space.Self);
        }
    }

    void Update()
    {
        // Só roda a checagem SEGURO aqui, pra não deletar as peças durante o giro delas
        if (verificarVitoriaPendente)
        {
            verificarVitoriaPendente = false;
            ProcessarVitoriaSegura();
        }

        // Fica vigiando o quad da foto
        if (isPuzzleSolved && !isPhotoTaken && finalQuadToPhotograph != null && !finalQuadToPhotograph.activeInHierarchy)
        {
            RewardNewPhoto();
        }
    }

    public void AvisarQueGiroTerminou()
    {
        verificarVitoriaPendente = true;
    }

    public void PlayTurnSound()
    {
        if (audioSource && pieceTurnSound) audioSource.PlayOneShot(pieceTurnSound);
    }

    private void ProcessarVitoriaSegura()
    {
        if (isPuzzleSolved) return;

        bool allCorrect = true;
        foreach (Transform piece in puzzlePieces)
        {
            if (Quaternion.Angle(piece.localRotation, rotacoesCorretas[piece]) > 1f)
            {
                allCorrect = false;
                break;
            }
        }

        if (allCorrect) WinPuzzle();
    }

    void WinPuzzle()
    {
        isPuzzleSolved = true;
        if (audioSource && puzzleSolvedSound) audioSource.PlayOneShot(puzzleSolvedSound);
        
        HideAllPieces(); 
        if (finalQuadToPhotograph) finalQuadToPhotograph.SetActive(true);

        // MANDA O LINK PRO SEU SCRIPT DE SAVE
        if (PersistenciaManager.Instance != null && !Application.isEditor)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_Solved", true);
            PersistenciaManager.Instance.SalvarTudo();
        }
    }

    void RewardNewPhoto()
    {
        isPhotoTaken = true;
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ReceberItem(newPhotoID);
            InventoryManager.Instance.TentarEquipar(newPhotoID);
        }
        HideAllPieces();

        // MANDA O LINK PRO SEU SCRIPT DE SAVE
        if (PersistenciaManager.Instance != null && !Application.isEditor)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_PhotoTaken", true);
            PersistenciaManager.Instance.SalvarTudo();
        }
    }

    void HideAllPieces()
    {
        foreach (Transform piece in puzzlePieces)
            if (piece != null) piece.gameObject.SetActive(false);
    }
}

// =========================================================================================
public class RotatingPiece : MonoBehaviour
{
    private RotationPuzzleManager manager;
    private bool isTurning = false;
    private Quaternion targetRotation;
    private Collider meuColisor;

    public void Setup(RotationPuzzleManager m)
    {
        manager = m;
        meuColisor = GetComponent<Collider>();
    }

    public void AoOlhar() { }
    public void AoSair() { }

    public void Interagir()
    {
        if (isTurning || manager == null) return;
        
        targetRotation = transform.localRotation * Quaternion.Euler(manager.rotationAxis * 90f);
        isTurning = true;
        manager.PlayTurnSound();

        if (meuColisor != null) meuColisor.enabled = false;
    }

    void Update()
    {
        if (isTurning)
        {
            transform.localRotation = Quaternion.RotateTowards(transform.localRotation, targetRotation, 450f * Time.unscaledDeltaTime);
            
            if (Quaternion.Angle(transform.localRotation, targetRotation) < 0.1f)
            {
                transform.localRotation = targetRotation;
                isTurning = false;
                
                if (meuColisor != null) meuColisor.enabled = true;
                
                // DELEGA A CHECAGEM PRO MANAGER (Evita o maldito crash de memória)
                manager.AvisarQueGiroTerminou();
            }
        }
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SlidingPuzzleManager : MonoBehaviour
{
    public enum Dificuldade { Facil, Medio, Dificil }

    [Header("--- DIFICULDADE ---")]
    public Dificuldade dificuldade = Dificuldade.Medio;

    [Header("--- POSIÇÕES (8 PEÇAS + 1 VAZIO) ---")]
    public List<Vector3> posicoesVitoriaPecas = new List<Vector3>(8);
    public Vector3 posicaoVitoriaVazio;

    [Header("--- REFERÊNCIAS ---")]
    public List<Transform> puzzlePieces; 
    public Transform emptySpaceMarker;
    
    [Header("--- SAVE SYSTEM ---")]
    public string uniqueID = "Puzzle_Final_Ultra_Fix_V9";

    [Header("--- RECOMPENSA IGUAL AO BAÚ ---")]
    public GameObject finalQuadToPhotograph; // O que você tira foto
    public GameObject permanentQuad; // O que fica pra sempre
    public string nomeDaRunaNesteBau; 
    public bool darItemInventario = false;
    public int idDoItemInventario = 0;
    
    [Header("--- UI DA RECOMPENSA ---")]
    public GameObject painelPretoRecompensa;
    public GameObject painelCustomizadoDaRecompensa; 
    public TextMeshProUGUI textoRecompensa;

    [Header("--- AUDIO & INTERAÇÃO ---")]
    public AudioSource audioSource;
    public AudioClip pieceSlideSound;
    public KeyCode teclaInteracao = KeyCode.E;
    public float distanciaInteracao = 4.0f;

    // --- LÓGICA DE GRADE INTERNA ---
    private Vector3[] slotsPosicoes = new Vector3[9];
    private int[] ocupacaoGrade = new int[9]; 
    private int slotVazioAtual = 7; 

    private bool isAnimating = false;
    private bool isSolved = false;
    private bool rewardGiven = false; 
    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
        
        if (puzzlePieces.Count != 8 || emptySpaceMarker == null || posicoesVitoriaPecas.Count != 8) return;

        for (int i = 0; i < 7; i++) slotsPosicoes[i] = posicoesVitoriaPecas[i]; 
        slotsPosicoes[7] = posicaoVitoriaVazio;                                
        slotsPosicoes[8] = posicoesVitoriaPecas[7];                           

        ConfigurarPuzzle();
    }

    void ConfigurarPuzzle()
    {
        for (int i = 0; i < puzzlePieces.Count; i++)
        {
            int slotVitoria = (i < 7) ? i : 8;
            puzzlePieces[i].localPosition = slotsPosicoes[slotVitoria];
            ocupacaoGrade[slotVitoria] = i;

            var script = puzzlePieces[i].GetComponent<SlidingPiece>() ?? puzzlePieces[i].gameObject.AddComponent<SlidingPiece>();
            script.pieceID = i; 
            
            if (!puzzlePieces[i].GetComponent<Collider>()) puzzlePieces[i].gameObject.AddComponent<MeshCollider>();
        }

        emptySpaceMarker.localPosition = slotsPosicoes[7];
        ocupacaoGrade[7] = -1; 
        slotVazioAtual = 7;
        
        var vScript = emptySpaceMarker.GetComponent<SlidingPiece>() ?? emptySpaceMarker.gameObject.AddComponent<SlidingPiece>();
        vScript.pieceID = -1; 

        if (finalQuadToPhotograph) finalQuadToPhotograph.SetActive(false);
        if (permanentQuad) permanentQuad.SetActive(false);
        if (painelPretoRecompensa) painelPretoRecompensa.SetActive(false);
        if (painelCustomizadoDaRecompensa) painelCustomizadoDaRecompensa.SetActive(false);
    }

    void Start() { if (!LoadSaveData()) AutoShuffle(); }

    void Update()
    {
        // GATILHO DA RECOMPENSA: Espera o puzzle ser resolvido e o quad da foto sumir
        if (isSolved && !rewardGiven && finalQuadToPhotograph != null && !finalQuadToPhotograph.activeInHierarchy)
        {
            StartCoroutine(SequenciaRecompensa());
        }

        if (isSolved || isAnimating) return;

        if (Input.GetKeyDown(teclaInteracao)) Interagir();
    }

    void Interagir()
    {
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, distanciaInteracao, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            SlidingPiece hitPiece = hit.transform.GetComponent<SlidingPiece>();
            if (hitPiece != null) {
                if (hitPiece.pieceID == -1) TentarMoverVizinhoParaVazio();
                else TentarMoverPeca(hitPiece.pieceID);
            }
        }
    }

    void TentarMoverVizinhoParaVazio()
    {
        if (isAnimating || isSolved) return; // Trava extra

        for (int i = 0; i < 9; i++) {
            int pID = ocupacaoGrade[i];
            if (pID != -1 && SaoVizinhos(i, slotVazioAtual)) {
                TentarMoverPeca(pID);
                return;
            }
        }
    }

    public void TentarMoverPeca(int pecaID)
    {
        // 🔥 A TRAVA BLINDADA QUE MATA O BUG: Se já tá animando ou resolvido, ignora a ordem! 🔥
        if (isAnimating || isSolved) return; 

        int slotDaPeca = -1;
        for (int i = 0; i < 9; i++) if (ocupacaoGrade[i] == pecaID) { slotDaPeca = i; break; }

        if (SaoVizinhos(slotDaPeca, slotVazioAtual)) {
            isAnimating = true; // Tranca na mesma hora antes do coroutine iniciar!
            StartCoroutine(AnimarTroca(pecaID, slotDaPeca, slotVazioAtual));
        }
    }

    bool SaoVizinhos(int a, int b)
    {
        int rowA = a / 3; int colA = a % 3;
        int rowB = b / 3; int colB = b % 3;
        return (Mathf.Abs(rowA - rowB) + Mathf.Abs(colA - colB)) == 1;
    }

    IEnumerator AnimarTroca(int pecaID, int de, int para)
    {
        Transform tPeca = puzzlePieces[pecaID];
        Vector3 destino = slotsPosicoes[para];
        Vector3 origem = slotsPosicoes[de];

        if (audioSource && pieceSlideSound) audioSource.PlayOneShot(pieceSlideSound);

        float tempo = 0;
        while (tempo < 1f) {
            tempo += Time.unscaledDeltaTime * 15f;
            tPeca.localPosition = Vector3.Lerp(origem, destino, tempo);
            yield return null;
        }

        tPeca.localPosition = destino;
        emptySpaceMarker.localPosition = origem;

        ocupacaoGrade[para] = pecaID;
        ocupacaoGrade[de] = -1;
        slotVazioAtual = de;

        isAnimating = false; // Destranca pra próxima peça
        CheckWinCondition();
        SaveGame();
    }

    void CheckWinCondition()
    {
        for (int i = 0; i < 7; i++) if (ocupacaoGrade[i] != i) return;
        if (ocupacaoGrade[8] != 7) return;

        Vencer();
    }

    void Vencer()
    {
        isSolved = true;
        
        foreach (var p in puzzlePieces) p.gameObject.SetActive(false);
        if (emptySpaceMarker) emptySpaceMarker.gameObject.SetActive(false);
        
        if (finalQuadToPhotograph) finalQuadToPhotograph.SetActive(true);
        
        if (PersistenciaManager.Instance != null) 
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_Solved", true);
    }

    // --- SEQUÊNCIA DE RECOMPENSA IDÊNTICA AO TREASURE CHEST ---
    IEnumerator SequenciaRecompensa()
    {
        rewardGiven = true;
        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_RewardGiven", true);

        // 1. ENTREGA A RUNA
        if (InventarioRunas.Instance != null && !string.IsNullOrEmpty(nomeDaRunaNesteBau))
        {
            InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRunaNesteBau);
        }

        // 2. ENTREGA O ITEM NO INVENTÁRIO (Se houver)
        if (darItemInventario && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ReceberItem(idDoItemInventario);
        }

        // 3. SALVA O JOGO (Idêntico ao Baú)
        if (!Application.isEditor && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarTudo();
        }

        // 4. LIGA A UI
        if(painelPretoRecompensa) painelPretoRecompensa.SetActive(true);
        if(textoRecompensa && !string.IsNullOrEmpty(nomeDaRunaNesteBau)) 
            textoRecompensa.text = "Você pegou a " + nomeDaRunaNesteBau + "!";
        if(painelCustomizadoDaRecompensa) painelCustomizadoDaRecompensa.SetActive(true);

        yield return new WaitForSecondsRealtime(3f);

        // 5. DESLIGA A UI
        if(painelPretoRecompensa) painelPretoRecompensa.SetActive(false);
        if(painelCustomizadoDaRecompensa) painelCustomizadoDaRecompensa.SetActive(false);
        
        // 6. ATIVA O QUAD PERMANENTE
        if (permanentQuad) permanentQuad.SetActive(true);
    }

    void AutoShuffle()
    {
        int movimentos = 30;
        if (dificuldade == Dificuldade.Facil) movimentos = 10;
        else if (dificuldade == Dificuldade.Medio) movimentos = 30;
        else movimentos = 80;

        for (int i = 0; i < movimentos; i++)
        {
            List<int> vizinhos = new List<int>();
            for (int j = 0; j < 9; j++) if (SaoVizinhos(slotVazioAtual, j)) vizinhos.Add(j);
            int escolhido = vizinhos[Random.Range(0, vizinhos.Count)];
            int pID = ocupacaoGrade[escolhido];
            if (pID != -1) {
                puzzlePieces[pID].localPosition = slotsPosicoes[slotVazioAtual];
                emptySpaceMarker.localPosition = slotsPosicoes[escolhido];
                
                ocupacaoGrade[slotVazioAtual] = pID;
                ocupacaoGrade[escolhido] = -1;
                slotVazioAtual = escolhido;
            }
        }
        SaveGame();
    }

    void SaveGame()
    {
        if (PersistenciaManager.Instance == null) return;
        PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_HasSave", true);
        PersistenciaManager.Instance.SalvarInt(uniqueID + "_Vazio", slotVazioAtual);
        for (int i = 0; i < 9; i++) PersistenciaManager.Instance.SalvarInt(uniqueID + "_Slot_" + i, ocupacaoGrade[i]);
    }

    bool LoadSaveData()
    {
        if (PersistenciaManager.Instance == null) return false;
        
        rewardGiven = PersistenciaManager.Instance.ObterEstado(uniqueID + "_RewardGiven");
        if (rewardGiven)
        {
            isSolved = true;
            foreach (var p in puzzlePieces) p.gameObject.SetActive(false);
            if (emptySpaceMarker) emptySpaceMarker.gameObject.SetActive(false);
            if (finalQuadToPhotograph) finalQuadToPhotograph.SetActive(false);
            if (permanentQuad) permanentQuad.SetActive(true);
            return true;
        }

        if (PersistenciaManager.Instance.ObterEstado(uniqueID + "_Solved")) {
            Vencer();
            return true;
        }

        if (!PersistenciaManager.Instance.ObterEstado(uniqueID + "_HasSave")) return false;
        slotVazioAtual = PersistenciaManager.Instance.ObterInt(uniqueID + "_Vazio");
        for (int i = 0; i < 9; i++) {
            int pID = PersistenciaManager.Instance.ObterInt(uniqueID + "_Slot_" + i);
            ocupacaoGrade[i] = pID;
            if (pID != -1) puzzlePieces[pID].localPosition = slotsPosicoes[i];
            else emptySpaceMarker.localPosition = slotsPosicoes[i];
        }
        return true;
    }

    public void ResetarPuzzle()
    {
        if (isSolved || isAnimating) return; 
        
        ConfigurarPuzzle(); 
        AutoShuffle(); 
    }
}

public class SlidingPiece : MonoBehaviour
{
    [HideInInspector] public int pieceID;
}
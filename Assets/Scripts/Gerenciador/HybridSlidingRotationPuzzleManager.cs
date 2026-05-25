using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class HybridSlidingRotationPuzzleManager : MonoBehaviour
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

    [Tooltip("Se ligado, ignora saves antigos no Editor para evitar quad final aparecendo por save velho.")]
    public bool ignorarSaveNoEditor = true;

    [Header("--- RECOMPENSA IGUAL AO BAÚ ---")]
    public GameObject finalQuadToPhotograph;
    public GameObject permanentQuad;
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
    public AudioClip pieceTurnSound;
    public AudioClip puzzleSolvedSound;

    public KeyCode teclaInteracao = KeyCode.E;
    public KeyCode teclaRotacionar = KeyCode.R;
    public float distanciaInteracao = 4.0f;

    [Header("--- ROTAÇÃO ---")]
    public Vector3 rotationAxis = new Vector3(0, 0, 1);
    public float velocidadeRotacao = 450f;

    [Header("--- EMBARALHAMENTO DE ROTAÇÃO ---")]
    public bool usarRotacoesManuaisIniciais = false;

    [Tooltip("Giros iniciais por peça. 0, 1, 2 ou 3.")]
    public int[] manualStartingTurns;

    [Header("--- VISUAL / BRILHO ---")]
    [Range(0.1f, 1f)]
    public float brilhoPecaErrada = 0.75f;

    [Range(0.1f, 2f)]
    public float brilhoPecaCorreta = 1f;

    [Tooltip("URP/Lit normalmente usa _BaseColor. Standard normalmente usa _Color.")]
    public string propriedadeCorShader = "_BaseColor";

    private Vector3[] slotsPosicoes = new Vector3[9];
    private int[] ocupacaoGrade = new int[9];
    private int slotVazioAtual = 7;

    private Quaternion[] rotacoesCorretas;

    private bool isSliding = false;
    private bool isRotating = false;
    private bool isSolved = false;
    private bool rewardGiven = false;
    private bool inicializado = false;
    private bool podeChecarVitoria = false;
    private bool finalQuadFoiMostrado = false;

    private Camera mainCam;

    private MaterialPropertyBlock propertyBlock;
    private Dictionary<Renderer, Color> coresOriginais = new Dictionary<Renderer, Color>();

    void Awake()
    {
        mainCam = Camera.main;
        propertyBlock = new MaterialPropertyBlock();

        if (puzzlePieces == null || puzzlePieces.Count != 8 || emptySpaceMarker == null || posicoesVitoriaPecas == null || posicoesVitoriaPecas.Count != 8)
        {
            Debug.LogError("[HybridPuzzle] Configuração inválida: precisa de exatamente 8 peças, 8 posições de vitória e 1 emptySpaceMarker.");
            return;
        }

        rotacoesCorretas = new Quaternion[puzzlePieces.Count];

        for (int i = 0; i < 7; i++)
            slotsPosicoes[i] = posicoesVitoriaPecas[i];

        slotsPosicoes[7] = posicaoVitoriaVazio;
        slotsPosicoes[8] = posicoesVitoriaPecas[7];

        ConfigurarPuzzleBase();
    }

    void Start()
    {
        StartCoroutine(InicializarSeguro());
    }

    IEnumerator InicializarSeguro()
    {
        if (PersistenciaManager.Instance != null)
        {
            yield return new WaitUntil(() =>
                PersistenciaManager.Instance.DadosProntosParaUso &&
                !PersistenciaManager.Instance.EstaCarregando
            );
        }

        bool carregou = LoadSaveData();

        if (!carregou)
        {
            AplicarRotacoesIniciais();
            AutoShuffleSemPecaCorreta();
            AtualizarBrilhoTodasAsPecas();
            SaveGame(false);
        }

        yield return null;

        podeChecarVitoria = true;
        inicializado = true;
    }

    void ConfigurarPuzzleBase()
    {
        if (puzzlePieces == null || puzzlePieces.Count != 8 || emptySpaceMarker == null)
            return;

        podeChecarVitoria = false;
        isSliding = false;
        isRotating = false;
        isSolved = false;
        rewardGiven = false;
        finalQuadFoiMostrado = false;

        for (int i = 0; i < ocupacaoGrade.Length; i++)
            ocupacaoGrade[i] = -999;

        for (int i = 0; i < puzzlePieces.Count; i++)
        {
            Transform peca = puzzlePieces[i];

            if (peca == null)
                continue;

            rotacoesCorretas[i] = peca.localRotation;
            CachearCoresOriginais(peca);

            int slotVitoria = (i < 7) ? i : 8;

            peca.gameObject.SetActive(true);
            peca.localPosition = slotsPosicoes[slotVitoria];
            peca.localRotation = rotacoesCorretas[i];

            ocupacaoGrade[slotVitoria] = i;

            HybridSlidingRotationPiece script = peca.GetComponent<HybridSlidingRotationPiece>();

            if (script == null)
                script = peca.gameObject.AddComponent<HybridSlidingRotationPiece>();

            script.pieceID = i;
            script.manager = this;

            Collider col = peca.GetComponent<Collider>();

            if (col == null)
                peca.gameObject.AddComponent<MeshCollider>();
            else
                col.enabled = true;
        }

        emptySpaceMarker.gameObject.SetActive(true);
        emptySpaceMarker.localPosition = slotsPosicoes[7];

        ocupacaoGrade[7] = -1;
        slotVazioAtual = 7;

        HybridSlidingRotationPiece vazioScript = emptySpaceMarker.GetComponent<HybridSlidingRotationPiece>();

        if (vazioScript == null)
            vazioScript = emptySpaceMarker.gameObject.AddComponent<HybridSlidingRotationPiece>();

        vazioScript.pieceID = -1;
        vazioScript.manager = this;

        if (finalQuadToPhotograph)
            finalQuadToPhotograph.SetActive(false);

        if (permanentQuad)
            permanentQuad.SetActive(false);

        if (painelPretoRecompensa)
            painelPretoRecompensa.SetActive(false);

        if (painelCustomizadoDaRecompensa)
            painelCustomizadoDaRecompensa.SetActive(false);
    }

    void Update()
    {
        if (!inicializado)
            return;

        if (isSolved && finalQuadFoiMostrado && !rewardGiven && finalQuadToPhotograph != null && !finalQuadToPhotograph.activeInHierarchy)
        {
            MarcarFotoTirada();
            return;
        }

        if (isSolved || rewardGiven || isSliding || isRotating)
            return;

        if (Input.GetKeyDown(teclaInteracao))
            InteragirMover();

        if (Input.GetKeyDown(teclaRotacionar))
            InteragirRotacionar();
    }

    void InteragirMover()
    {
        HybridSlidingRotationPiece hitPiece = RaycastPiece();

        if (hitPiece == null)
            return;

        if (hitPiece.pieceID < 0)
            return;

        TentarMoverPeca(hitPiece.pieceID);
    }

    void InteragirRotacionar()
    {
        HybridSlidingRotationPiece hitPiece = RaycastPiece();

        if (hitPiece == null)
            return;

        if (hitPiece.pieceID < 0)
            return;

        TentarRotacionarPeca(hitPiece.pieceID);
    }

    private HybridSlidingRotationPiece RaycastPiece()
    {
        if (mainCam == null)
            mainCam = Camera.main;

        if (mainCam == null)
            return null;

        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, distanciaInteracao, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            HybridSlidingRotationPiece hitPiece = hit.transform.GetComponent<HybridSlidingRotationPiece>();

            if (hitPiece == null)
                hitPiece = hit.transform.GetComponentInParent<HybridSlidingRotationPiece>();

            return hitPiece;
        }

        return null;
    }

    public void TentarMoverPeca(int pecaID)
    {
        if (!inicializado || isSliding || isRotating || isSolved)
            return;

        if (pecaID < 0 || pecaID >= puzzlePieces.Count)
            return;

        int slotDaPeca = EncontrarSlotDaPeca(pecaID);

        if (slotDaPeca < 0)
            return;

        if (!SaoVizinhos(slotDaPeca, slotVazioAtual))
            return;

        isSliding = true;
        StartCoroutine(AnimarTroca(pecaID, slotDaPeca, slotVazioAtual));
    }

    public void TentarRotacionarPeca(int pecaID)
    {
        if (!inicializado || isSliding || isRotating || isSolved)
            return;

        if (pecaID < 0 || pecaID >= puzzlePieces.Count)
            return;

        Transform peca = puzzlePieces[pecaID];

        if (peca == null)
            return;

        Quaternion alvo = peca.localRotation * Quaternion.Euler(rotationAxis * 90f);

        isRotating = true;
        StartCoroutine(AnimarRotacao(pecaID, alvo));
    }

    int EncontrarSlotDaPeca(int pecaID)
    {
        for (int i = 0; i < ocupacaoGrade.Length; i++)
        {
            if (ocupacaoGrade[i] == pecaID)
                return i;
        }

        return -1;
    }

    bool SaoVizinhos(int a, int b)
    {
        int rowA = a / 3;
        int colA = a % 3;

        int rowB = b / 3;
        int colB = b % 3;

        return (Mathf.Abs(rowA - rowB) + Mathf.Abs(colA - colB)) == 1;
    }

    IEnumerator AnimarTroca(int pecaID, int de, int para)
    {
        Transform tPeca = puzzlePieces[pecaID];

        if (tPeca == null)
        {
            isSliding = false;
            yield break;
        }

        Vector3 origem = tPeca.localPosition;
        Vector3 destino = slotsPosicoes[para];

        Collider col = tPeca.GetComponent<Collider>();

        if (col != null)
            col.enabled = false;

        if (audioSource && pieceSlideSound)
            audioSource.PlayOneShot(pieceSlideSound);

        float tempo = 0f;

        while (tempo < 1f)
        {
            tempo += Time.unscaledDeltaTime * 15f;
            tPeca.localPosition = Vector3.Lerp(origem, destino, tempo);
            yield return null;
        }

        tPeca.localPosition = destino;

        if (emptySpaceMarker)
            emptySpaceMarker.localPosition = slotsPosicoes[de];

        ocupacaoGrade[para] = pecaID;
        ocupacaoGrade[de] = -1;
        slotVazioAtual = de;

        if (col != null)
            col.enabled = true;

        isSliding = false;

        AtualizarBrilhoTodasAsPecas();
        SaveGame(false);
        CheckWinCondition();
    }

    IEnumerator AnimarRotacao(int pecaID, Quaternion alvo)
    {
        Transform peca = puzzlePieces[pecaID];

        if (peca == null)
        {
            isRotating = false;
            yield break;
        }

        if (audioSource && pieceTurnSound)
            audioSource.PlayOneShot(pieceTurnSound);

        Collider col = peca.GetComponent<Collider>();

        if (col != null)
            col.enabled = false;

        while (Quaternion.Angle(peca.localRotation, alvo) > 0.1f)
        {
            peca.localRotation = Quaternion.RotateTowards(
                peca.localRotation,
                alvo,
                velocidadeRotacao * Time.unscaledDeltaTime
            );

            yield return null;
        }

        peca.localRotation = alvo;

        if (col != null)
            col.enabled = true;

        isRotating = false;

        AtualizarBrilhoTodasAsPecas();
        SaveGame(false);
        CheckWinCondition();
    }

    void CheckWinCondition()
    {
        if (!podeChecarVitoria)
            return;

        if (isSolved)
            return;

        if (!TodasAsPosicoesCorretas())
            return;

        if (!TodasAsRotacoesCorretas())
            return;

        Vencer();
    }

    bool TodasAsPosicoesCorretas()
    {
        for (int i = 0; i < 7; i++)
        {
            if (ocupacaoGrade[i] != i)
                return false;
        }

        if (ocupacaoGrade[8] != 7)
            return false;

        if (slotVazioAtual != 7)
            return false;

        return true;
    }

    bool TodasAsRotacoesCorretas()
    {
        for (int i = 0; i < puzzlePieces.Count; i++)
        {
            Transform peca = puzzlePieces[i];

            if (peca == null)
                continue;

            if (Quaternion.Angle(peca.localRotation, rotacoesCorretas[i]) > 1f)
                return false;
        }

        return true;
    }

    bool PecaEstaCorreta(int pecaID)
    {
        if (pecaID < 0 || pecaID >= puzzlePieces.Count)
            return false;

        int slotAtual = EncontrarSlotDaPeca(pecaID);
        int slotCorreto = (pecaID < 7) ? pecaID : 8;

        bool posicaoCorreta = slotAtual == slotCorreto;
        bool rotacaoCorreta = Quaternion.Angle(puzzlePieces[pecaID].localRotation, rotacoesCorretas[pecaID]) <= 1f;

        return posicaoCorreta && rotacaoCorreta;
    }

    void Vencer()
    {
        if (isSolved)
            return;

        isSolved = true;

        if (audioSource && puzzleSolvedSound)
            audioSource.PlayOneShot(puzzleSolvedSound);

        EsconderTodasAsPecas();

        if (finalQuadToPhotograph)
        {
            finalQuadToPhotograph.SetActive(true);
            finalQuadFoiMostrado = true;
        }

        if (PersistenciaManager.Instance != null && PodeUsarSave())
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_Solved", true);
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_FinalQuadShowing", true);
            SaveGame(false);
            SalvarProgressoSeguro();
        }
    }

    private void MarcarFotoTirada()
    {
        if (rewardGiven)
            return;

        rewardGiven = true;
        finalQuadFoiMostrado = false;

        EsconderTodasAsPecas();

        if (finalQuadToPhotograph)
            finalQuadToPhotograph.SetActive(false);

        if (PersistenciaManager.Instance != null && PodeUsarSave())
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_RewardGiven", true);
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_FinalQuadShowing", false);
            SalvarProgressoSeguro();
        }

        Debug.Log("[HybridPuzzle] Foto tirada pelo script do quad. Puzzle finalizado.");
    }

    private void EsconderTodasAsPecas()
    {
        foreach (Transform p in puzzlePieces)
        {
            if (p != null)
                p.gameObject.SetActive(false);
        }

        if (emptySpaceMarker)
            emptySpaceMarker.gameObject.SetActive(false);
    }

    void AutoShuffleSemPecaCorreta()
    {
        int tentativasMaximas = 80;

        for (int tentativa = 0; tentativa < tentativasMaximas; tentativa++)
        {
            ResetarGradeParaEstadoResolvidoSemMexerRotacao();
            AutoShuffle();

            if (!AlgumaPecaNaPosicaoCorreta() && !TodasAsPosicoesCorretas())
                return;
        }

        Debug.LogWarning("[HybridPuzzle] Não conseguiu embaralhar sem nenhuma peça correta após várias tentativas. Usando último embaralhamento.");
    }

    void ResetarGradeParaEstadoResolvidoSemMexerRotacao()
    {
        for (int i = 0; i < ocupacaoGrade.Length; i++)
            ocupacaoGrade[i] = -999;

        for (int i = 0; i < puzzlePieces.Count; i++)
        {
            int slotVitoria = (i < 7) ? i : 8;

            if (puzzlePieces[i] != null)
                puzzlePieces[i].localPosition = slotsPosicoes[slotVitoria];

            ocupacaoGrade[slotVitoria] = i;
        }

        if (emptySpaceMarker)
            emptySpaceMarker.localPosition = slotsPosicoes[7];

        ocupacaoGrade[7] = -1;
        slotVazioAtual = 7;
    }

    bool AlgumaPecaNaPosicaoCorreta()
    {
        for (int i = 0; i < puzzlePieces.Count; i++)
        {
            int slotAtual = EncontrarSlotDaPeca(i);
            int slotCorreto = (i < 7) ? i : 8;

            if (slotAtual == slotCorreto)
                return true;
        }

        return false;
    }

    void AutoShuffle()
    {
        int movimentos = 30;

        if (dificuldade == Dificuldade.Facil)
            movimentos = 10;
        else if (dificuldade == Dificuldade.Medio)
            movimentos = 30;
        else
            movimentos = 80;

        int ultimoSlotMovido = -999;

        for (int i = 0; i < movimentos; i++)
        {
            List<int> vizinhos = new List<int>();

            for (int j = 0; j < 9; j++)
            {
                if (j == ultimoSlotMovido)
                    continue;

                if (SaoVizinhos(slotVazioAtual, j))
                    vizinhos.Add(j);
            }

            if (vizinhos.Count == 0)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (SaoVizinhos(slotVazioAtual, j))
                        vizinhos.Add(j);
                }
            }

            if (vizinhos.Count == 0)
                continue;

            int escolhido = vizinhos[Random.Range(0, vizinhos.Count)];
            int pID = ocupacaoGrade[escolhido];

            if (pID != -1 && pID >= 0 && pID < puzzlePieces.Count)
            {
                puzzlePieces[pID].localPosition = slotsPosicoes[slotVazioAtual];

                if (emptySpaceMarker)
                    emptySpaceMarker.localPosition = slotsPosicoes[escolhido];

                ocupacaoGrade[slotVazioAtual] = pID;
                ocupacaoGrade[escolhido] = -1;

                ultimoSlotMovido = slotVazioAtual;
                slotVazioAtual = escolhido;
            }
        }
    }

    void AplicarRotacoesIniciais()
    {
        for (int i = 0; i < puzzlePieces.Count; i++)
        {
            Transform peca = puzzlePieces[i];

            if (peca == null)
                continue;

            int turnos;

            if (usarRotacoesManuaisIniciais && manualStartingTurns != null && i < manualStartingTurns.Length)
                turnos = Mathf.Abs(manualStartingTurns[i]) % 4;
            else
                turnos = Random.Range(0, 4);

            peca.localRotation = rotacoesCorretas[i] * Quaternion.Euler(rotationAxis * (90f * turnos));
        }
    }

    void SaveGame(bool salvarDiscoAgora)
    {
        if (PersistenciaManager.Instance == null)
            return;

        if (!PodeUsarSave())
            return;

        PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_HasSave", true);
        PersistenciaManager.Instance.SalvarInt(uniqueID + "_Vazio", slotVazioAtual);

        for (int i = 0; i < 9; i++)
            PersistenciaManager.Instance.SalvarInt(uniqueID + "_Slot_" + i, ocupacaoGrade[i]);

        for (int i = 0; i < puzzlePieces.Count; i++)
        {
            if (puzzlePieces[i] == null)
                continue;

            Quaternion r = puzzlePieces[i].localRotation;

            PersistenciaManager.Instance.SalvarFloat(uniqueID + "_Rot_" + i + "_X", r.x);
            PersistenciaManager.Instance.SalvarFloat(uniqueID + "_Rot_" + i + "_Y", r.y);
            PersistenciaManager.Instance.SalvarFloat(uniqueID + "_Rot_" + i + "_Z", r.z);
            PersistenciaManager.Instance.SalvarFloat(uniqueID + "_Rot_" + i + "_W", r.w);
        }

        if (salvarDiscoAgora)
            SalvarProgressoSeguro();
    }

    bool LoadSaveData()
    {
        if (PersistenciaManager.Instance == null)
            return false;

        if (!PodeUsarSave())
            return false;

        rewardGiven = PersistenciaManager.Instance.ObterEstado(uniqueID + "_RewardGiven", false);

        if (rewardGiven)
        {
            isSolved = true;
            finalQuadFoiMostrado = false;

            EsconderTodasAsPecas();

            if (finalQuadToPhotograph)
                finalQuadToPhotograph.SetActive(false);

            if (permanentQuad)
                permanentQuad.SetActive(false);

            return true;
        }

        bool finalQuadShowing = PersistenciaManager.Instance.ObterEstado(uniqueID + "_FinalQuadShowing", false);

        if (PersistenciaManager.Instance.ObterEstado(uniqueID + "_Solved", false) || finalQuadShowing)
        {
            isSolved = true;
            finalQuadFoiMostrado = true;

            EsconderTodasAsPecas();

            if (finalQuadToPhotograph)
                finalQuadToPhotograph.SetActive(true);

            return true;
        }

        if (!PersistenciaManager.Instance.ObterEstado(uniqueID + "_HasSave", false))
            return false;

        slotVazioAtual = PersistenciaManager.Instance.ObterInt(uniqueID + "_Vazio", 7);

        for (int i = 0; i < 9; i++)
        {
            int pID = PersistenciaManager.Instance.ObterInt(uniqueID + "_Slot_" + i, -999);
            ocupacaoGrade[i] = pID;

            if (pID != -1 && pID >= 0 && pID < puzzlePieces.Count)
                puzzlePieces[pID].localPosition = slotsPosicoes[i];
            else if (pID == -1 && emptySpaceMarker != null)
                emptySpaceMarker.localPosition = slotsPosicoes[i];
        }

        for (int i = 0; i < puzzlePieces.Count; i++)
        {
            if (puzzlePieces[i] == null)
                continue;

            if (PersistenciaManager.Instance.TemFloat(uniqueID + "_Rot_" + i + "_X"))
            {
                Quaternion r = new Quaternion(
                    PersistenciaManager.Instance.ObterFloat(uniqueID + "_Rot_" + i + "_X"),
                    PersistenciaManager.Instance.ObterFloat(uniqueID + "_Rot_" + i + "_Y"),
                    PersistenciaManager.Instance.ObterFloat(uniqueID + "_Rot_" + i + "_Z"),
                    PersistenciaManager.Instance.ObterFloat(uniqueID + "_Rot_" + i + "_W", 1f)
                );

                puzzlePieces[i].localRotation = r;
            }
        }

        if (finalQuadToPhotograph)
            finalQuadToPhotograph.SetActive(false);

        if (permanentQuad)
            permanentQuad.SetActive(false);

        return true;
    }

    public void ResetarPuzzle()
    {
        if (isSliding || isRotating)
            return;

        ConfigurarPuzzleBase();
        AplicarRotacoesIniciais();
        AutoShuffleSemPecaCorreta();
        AtualizarBrilhoTodasAsPecas();

        isSolved = false;
        rewardGiven = false;
        finalQuadFoiMostrado = false;
        podeChecarVitoria = true;

        SaveGame(true);
    }

    private void CachearCoresOriginais(Transform peca)
    {
        Renderer[] renderers = peca.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            if (r == null)
                continue;

            if (coresOriginais.ContainsKey(r))
                continue;

            Color cor = Color.white;
            Material mat = r.sharedMaterial;

            if (mat != null)
            {
                if (mat.HasProperty(propriedadeCorShader))
                    cor = mat.GetColor(propriedadeCorShader);
                else if (mat.HasProperty("_BaseColor"))
                    cor = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))
                    cor = mat.GetColor("_Color");
            }

            coresOriginais.Add(r, cor);
        }
    }

    private void AtualizarBrilhoTodasAsPecas()
    {
        for (int i = 0; i < puzzlePieces.Count; i++)
            AtualizarBrilhoPeca(i);
    }

    private void AtualizarBrilhoPeca(int pecaID)
    {
        if (pecaID < 0 || pecaID >= puzzlePieces.Count)
            return;

        Transform peca = puzzlePieces[pecaID];

        if (peca == null)
            return;

        bool correta = PecaEstaCorreta(pecaID);
        float brilho = correta ? brilhoPecaCorreta : brilhoPecaErrada;

        Renderer[] renderers = peca.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            if (r == null)
                continue;

            Color baseColor = Color.white;

            if (coresOriginais.ContainsKey(r))
                baseColor = coresOriginais[r];

            Color corFinal = baseColor * brilho;
            corFinal.a = baseColor.a;

            r.GetPropertyBlock(propertyBlock);

            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(propriedadeCorShader))
                propertyBlock.SetColor(propriedadeCorShader, corFinal);
            else if (r.sharedMaterial != null && r.sharedMaterial.HasProperty("_BaseColor"))
                propertyBlock.SetColor("_BaseColor", corFinal);
            else
                propertyBlock.SetColor("_Color", corFinal);

            r.SetPropertyBlock(propertyBlock);
        }
    }

    private bool PodeUsarSave()
    {
#if UNITY_EDITOR
        if (ignorarSaveNoEditor)
            return false;
#endif

        return true;
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
}

public class HybridSlidingRotationPiece : MonoBehaviour
{
    [HideInInspector] public int pieceID;
    [HideInInspector] public HybridSlidingRotationPuzzleManager manager;

    public void AoOlhar() { }

    public void AoSair() { }

    public void Interagir() { }
}
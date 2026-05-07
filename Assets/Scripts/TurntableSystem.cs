using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class TurntableSystem : MonoBehaviour
{
    [Header("--- SAVE SYSTEM ---")]
    public string uniqueID; 

    [Header("--- CONFIGURAÇÃO DE IDs ---")]
    public int shovelRecordID = 1;   
    public int runeRecordID = 2;     

    [Header("--- MANIVELA (O Botão de Ativar) ---")]
    public int manivelaItemID = 3;   
    public GameObject visualManivela; 
    public GameObject textoFaltaManivela; 
    private bool temManivela = false; 

    [Header("--- COFRE (Disco da Pá) ---")]
    public Transform safeDoorPivot;
    public Vector3 safeClosedRotation;
    public Vector3 safeOpenRotation;
    public float safeOpenSpeed = 2f;
    public AudioClip safeOpenSound;
    private bool isSafeOpen = false;

    [Header("--- QUADRO SECRETO (Disco da Runa) ---")]
    public Transform quadroParaCair;
    public Vector3 quadroPosicaoPreso; 
    public Vector3 quadroRotacaoPreso; 
    public Vector3 quadroPosicaoCaido; 
    public Vector3 quadroRotacaoCaido; 
    public float velocidadeQuedaQuadro = 3f;
    public AudioClip somQuadroBatendo;
    public ReadableDocument documentoEscondido; 
    private bool quadroJaCaiu = false;

    [Header("--- LIVRO DO COFRE ---")]
    public Collider colliderLivro;

    [Header("--- VISUAIS E CONEXÕES ---")]
    public GameObject visualShovelRecord;   
    public GameObject visualRuneRecord; 
    public TVInterativa tvScript; 

    [Header("--- ÁUDIO E UI ---")]
    public AudioClip musicShovel;   
    public AudioClip musicRune; 
    public GameObject interactText;  
    public GameObject noDiskText;    
    public AudioSource sfxSource; 

    [Header("--- INTEGRAÇÃO COM SOUNDTRACK ---")]
    [Tooltip("Arraste o SoundtrackManager aqui. Se deixar vazio, ele tenta encontrar sozinho na cena.")]
    public SoundtrackManager soundtrackManager;

    [Tooltip("Tempo de fade para abafar/voltar a trilha quando o disco entra/sai do alcance audível.")]
    public float tempoFadeSoundtrackPorDisco = 1.5f;

    private AudioSource audioSource;
    private int currentDiskID = 0; 
    private bool isPlaying = false;
    
    private float tempoBloqueio = 0f; 
    private bool estaOlhando = false;
    private bool mostrandoErro = false;
    private Coroutine rotinaErro;

    private bool soundtrackAbafadoPeloDisco = false;
    private bool inicializado = false;

    void Start()
    {
        if (string.IsNullOrEmpty(uniqueID))
            Debug.LogError($"[ERRO] Vitrola '{gameObject.name}' sem Unique ID!");

        audioSource = GetComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f; 
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.loop = true; 
        audioSource.playOnAwake = false;

        if (safeDoorPivot) safeDoorPivot.localRotation = Quaternion.Euler(safeClosedRotation);
        if (colliderLivro) colliderLivro.enabled = false;
        
        if (quadroParaCair) 
        {
            quadroParaCair.localPosition = quadroPosicaoPreso;
            quadroParaCair.localRotation = Quaternion.Euler(quadroRotacaoPreso);
        }

        if (soundtrackManager == null)
            soundtrackManager = Object.FindFirstObjectByType<SoundtrackManager>();

        if (visualShovelRecord) visualShovelRecord.SetActive(false);
        if (visualRuneRecord) visualRuneRecord.SetActive(false);
        if (visualManivela) visualManivela.SetActive(false);
        if (interactText) interactText.SetActive(false);
        if (noDiskText) noDiskText.SetActive(false);
        if (textoFaltaManivela) textoFaltaManivela.SetActive(false);

        StartCoroutine(CarregarSaveSeguro());
    }

    IEnumerator CarregarSaveSeguro()
    {
        if (PersistenciaManager.Instance != null)
            yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);

        CarregarSave();
        inicializado = true;
    }

    void CarregarSave()
    {
        if (PersistenciaManager.Instance == null) return;

        isSafeOpen = PersistenciaManager.Instance.ObterEstado(uniqueID + "_safe");

        if (isSafeOpen && safeDoorPivot) 
        {
            safeDoorPivot.localRotation = Quaternion.Euler(safeOpenRotation);

            if (colliderLivro)
                colliderLivro.enabled = true; 
        }

        quadroJaCaiu = PersistenciaManager.Instance.ObterEstado(uniqueID + "_quadro");

        if (quadroJaCaiu && quadroParaCair)
        {
            quadroParaCair.localPosition = quadroPosicaoCaido;
            quadroParaCair.localRotation = Quaternion.Euler(quadroRotacaoCaido);

            if (documentoEscondido != null)
                documentoEscondido.LiberarDocumento();
        }

        temManivela = PersistenciaManager.Instance.ObterEstado(uniqueID + "_manivela");

        if (temManivela && visualManivela)
            visualManivela.SetActive(true);

        bool temDiscoPa = PersistenciaManager.Instance.ObterEstado(uniqueID + "_shovelDisk");
        bool temDiscoRuna = PersistenciaManager.Instance.ObterEstado(uniqueID + "_runeDisk");

        if (temDiscoPa)
            LoadDisk(shovelRecordID, true);
        else if (temDiscoRuna)
            LoadDisk(runeRecordID, true);
    }

    void Update()
    {
        if (!inicializado) return;

        if (isPlaying && currentDiskID != 0)
        {
            if (currentDiskID == shovelRecordID && visualShovelRecord) 
                visualShovelRecord.transform.Rotate(Vector3.up * 100 * Time.deltaTime);

            if (currentDiskID == runeRecordID && visualRuneRecord) 
                visualRuneRecord.transform.Rotate(Vector3.up * 100 * Time.deltaTime);
                
            if (visualManivela)
                visualManivela.transform.Rotate(Vector3.right * 100 * Time.deltaTime); 
        }

        GerenciarSoundtrackPeloAudioDoDisco();
    }

    void GerenciarSoundtrackPeloAudioDoDisco()
    {
        if (soundtrackManager == null)
            soundtrackManager = Object.FindFirstObjectByType<SoundtrackManager>();

        if (soundtrackManager == null || FPS_Master.Instance == null || audioSource == null)
            return;

        bool discoAudivel = DiscoEstaAudivelParaOJogador();

        if (discoAudivel && !soundtrackAbafadoPeloDisco)
        {
            soundtrackAbafadoPeloDisco = true;
            soundtrackManager.SetSoundtrackAbafadoPorDisco(true, tempoFadeSoundtrackPorDisco);
        }
        else if (!discoAudivel && soundtrackAbafadoPeloDisco)
        {
            soundtrackAbafadoPeloDisco = false;
            soundtrackManager.SetSoundtrackAbafadoPorDisco(false, tempoFadeSoundtrackPorDisco);
        }
    }

    private bool DiscoEstaAudivelParaOJogador()
    {
        if (!isPlaying) return false;
        if (currentDiskID == 0) return false;
        if (audioSource == null) return false;
        if (!audioSource.isPlaying) return false;
        if (audioSource.clip == null) return false;
        if (FPS_Master.Instance == null) return false;

        float distancia = Vector3.Distance(transform.position, FPS_Master.Instance.transform.position);
        return distancia <= audioSource.maxDistance;
    }

    public void AoOlhar() 
    { 
        if (!inicializado) return;

        estaOlhando = true;

        if (!mostrandoErro && interactText)
            interactText.SetActive(true); 
        
        if (currentDiskID != 0 && !temManivela && textoFaltaManivela) 
            textoFaltaManivela.SetActive(true);
    }
    
    public void AoSair() 
    { 
        estaOlhando = false;

        if (interactText) interactText.SetActive(false); 
        if (noDiskText) noDiskText.SetActive(false); 
        if (textoFaltaManivela) textoFaltaManivela.SetActive(false);
    }

    public void Interagir()
    {
        if (!inicializado) return;
        if (Time.unscaledTime < tempoBloqueio || mostrandoErro) return;

        tempoBloqueio = Time.unscaledTime + 0.5f; 

        int itemInHand = InventoryManager.Instance != null ? InventoryManager.Instance.itemSelecionado : -1;

        if (currentDiskID == 0)
        {
            if (itemInHand == shovelRecordID || itemInHand == runeRecordID)
            {
                if (InventoryManager.Instance != null)
                    InventoryManager.Instance.ConsumirItem(itemInHand);

                LoadDisk(itemInHand, false);
                SalvarProgressoSeguro();
            }
            else if (itemInHand == manivelaItemID && !temManivela)
            {
                InstalarManivela();
            }
            else
            {
                if (rotinaErro != null)
                    StopCoroutine(rotinaErro);

                rotinaErro = StartCoroutine(ShowErrorFeedback());
            }
        }
        else
        {
            EjectDisk();
        }
    }

    void InstalarManivela()
    {
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ConsumirItem(manivelaItemID);
            
        temManivela = true;

        if (visualManivela) visualManivela.SetActive(true);
        if (textoFaltaManivela) textoFaltaManivela.SetActive(false);

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_manivela", true);

        if (currentDiskID != 0)
            AtivarFuncoesDoDisco(currentDiskID, false);

        SalvarProgressoSeguro();
    }

    void LoadDisk(int id, bool isLoadingSave)
    {
        currentDiskID = id;

        if (interactText)
            interactText.SetActive(false); 

        if (id == shovelRecordID)
        {
            if (visualShovelRecord) visualShovelRecord.SetActive(true);

            audioSource.clip = musicShovel;
            SalvarEstadoDisco(true, false);
        }
        else if (id == runeRecordID)
        {
            if (visualRuneRecord) visualRuneRecord.SetActive(true);

            audioSource.clip = musicRune;
            SalvarEstadoDisco(false, true);
        }

        if (temManivela)
        {
            AtivarFuncoesDoDisco(id, isLoadingSave);
        }
        else
        {
            if (textoFaltaManivela)
                textoFaltaManivela.SetActive(true);
        }
    }

    void AtivarFuncoesDoDisco(int id, bool isLoadingSave)
    {
        isPlaying = true;

        if (audioSource.clip != null)
            audioSource.Play(); 

        GerenciarSoundtrackPeloAudioDoDisco();

        if (id == shovelRecordID)
        {
            if (!isSafeOpen && !isLoadingSave)
                StartCoroutine(AbrirCofreRoutine());
        }
        else if (id == runeRecordID)
        {
            bool eventoRunaJaAconteceu = PersistenciaManager.Instance != null &&
                                         PersistenciaManager.Instance.ObterEstado(uniqueID + "_runeEventDone");

            if (!eventoRunaJaAconteceu && !isLoadingSave)
            {
                if (tvScript) tvScript.ReceberSinalDoDisco();

                if (!quadroJaCaiu)
                    StartCoroutine(RotinaDerrubarQuadro());

                if (PersistenciaManager.Instance != null)
                    PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_runeEventDone", true);

                SalvarProgressoSeguro();
            }
        }
    }

    IEnumerator RotinaDerrubarQuadro()
    {
        quadroJaCaiu = true;

        yield return new WaitForSeconds(1.5f);

        if (quadroParaCair == null) yield break;

        Vector3 startPos = quadroPosicaoPreso; 
        Quaternion startRot = Quaternion.Euler(quadroRotacaoPreso); 
        Quaternion endRot = Quaternion.Euler(quadroRotacaoCaido);

        float time = 0;

        while (time < 1f)
        {
            time += Time.deltaTime * velocidadeQuedaQuadro;
            quadroParaCair.localPosition = Vector3.Lerp(startPos, quadroPosicaoCaido, time);
            quadroParaCair.localRotation = Quaternion.Lerp(startRot, endRot, time);
            yield return null;
        }
        
        quadroParaCair.localPosition = quadroPosicaoCaido;
        quadroParaCair.localRotation = endRot;

        if (sfxSource && somQuadroBatendo)
            sfxSource.PlayOneShot(somQuadroBatendo);

        if (documentoEscondido != null)
            documentoEscondido.LiberarDocumento();

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_quadro", true);

        SalvarProgressoSeguro();
    }

    IEnumerator AbrirCofreRoutine()
    {
        isSafeOpen = true;
        
        if (sfxSource && safeOpenSound)
            sfxSource.PlayOneShot(safeOpenSound);
        
        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_safe", true);

        if (safeDoorPivot == null)
        {
            SalvarProgressoSeguro();
            yield break;
        }

        Quaternion startRot = safeDoorPivot.localRotation;
        Quaternion endRot = Quaternion.Euler(safeOpenRotation);

        float time = 0;

        while (time < 1f)
        {
            time += Time.deltaTime * safeOpenSpeed;
            safeDoorPivot.localRotation = Quaternion.Lerp(startRot, endRot, time);
            yield return null;
        }

        safeDoorPivot.localRotation = endRot;

        if (colliderLivro)
            colliderLivro.enabled = true;

        SalvarProgressoSeguro();
    }

    void EjectDisk()
    {
        isPlaying = false;

        if (audioSource)
            audioSource.Stop();
        
        int diskToReturn = currentDiskID;
        currentDiskID = 0;

        LiberarSoundtrackSeNecessario();

        SalvarEstadoDisco(false, false);
        
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ReceberItemDeVolta(diskToReturn);

        if (diskToReturn == shovelRecordID && visualShovelRecord)
            visualShovelRecord.SetActive(false);
        else if (diskToReturn == runeRecordID && visualRuneRecord)
            visualRuneRecord.SetActive(false);

        if (interactText)
            interactText.SetActive(true);

        SalvarProgressoSeguro();
    }

    void SalvarEstadoDisco(bool shovelIn, bool runeIn)
    {
        if (PersistenciaManager.Instance == null) return;

        PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_shovelDisk", shovelIn);
        PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_runeDisk", runeIn);
    }

    IEnumerator ShowErrorFeedback()
    {
        mostrandoErro = true;

        if (interactText) interactText.SetActive(false);

        if (noDiskText) noDiskText.SetActive(true);

        yield return new WaitForSeconds(2.0f);

        if (noDiskText) noDiskText.SetActive(false);

        mostrandoErro = false;

        if (estaOlhando && interactText)
            interactText.SetActive(true);
    }

    private void LiberarSoundtrackSeNecessario()
    {
        if (!soundtrackAbafadoPeloDisco)
            return;

        soundtrackAbafadoPeloDisco = false;

        if (soundtrackManager == null)
            soundtrackManager = Object.FindFirstObjectByType<SoundtrackManager>();

        if (soundtrackManager != null)
            soundtrackManager.SetSoundtrackAbafadoPorDisco(false, tempoFadeSoundtrackPorDisco);
    }

    private void OnDisable()
    {
        LiberarSoundtrackSeNecessario();
    }

    private void OnDestroy()
    {
        LiberarSoundtrackSeNecessario();
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
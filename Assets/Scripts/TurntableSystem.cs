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

    // Estado Interno
    private AudioSource audioSource;
    private int currentDiskID = 0; 
    private bool isPlaying = false;
    
    // Travas da UI de Interação
    private float tempoBloqueio = 0f; 
    private bool estaOlhando = false;
    private bool mostrandoErro = false;
    private Coroutine rotinaErro;

    // Sistema de Raio de Áudio
    private AudioSource globalSoundtrack;
    private float globalOriginalVolume = 0.5f;
    private bool playerIsInsideRadius = false;
    private Coroutine fadeRoutine;

    void Start()
    {
        if (string.IsNullOrEmpty(uniqueID)) Debug.LogError($"[ERRO] Vitrola '{gameObject.name}' sem Unique ID!");

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

        GameObject stObj = GameObject.Find("Soundtrack");
        if (stObj != null)
        {
            globalSoundtrack = stObj.GetComponent<AudioSource>();
            globalOriginalVolume = globalSoundtrack.volume;
        }

        if (visualShovelRecord) visualShovelRecord.SetActive(false);
        if (visualRuneRecord) visualRuneRecord.SetActive(false);
        if (visualManivela) visualManivela.SetActive(false);
        if (interactText) interactText.SetActive(false);
        if (noDiskText) noDiskText.SetActive(false);
        if (textoFaltaManivela) textoFaltaManivela.SetActive(false);

        CarregarSave();
    }

    void CarregarSave()
    {
        if (PersistenciaManager.Instance == null) return;

        isSafeOpen = PersistenciaManager.Instance.ObterEstado(uniqueID + "_safe");
        if (isSafeOpen && safeDoorPivot) 
        {
            safeDoorPivot.localRotation = Quaternion.Euler(safeOpenRotation);
            if (colliderLivro) colliderLivro.enabled = true; 
        }

        quadroJaCaiu = PersistenciaManager.Instance.ObterEstado(uniqueID + "_quadro");
        if (quadroJaCaiu && quadroParaCair)
        {
            quadroParaCair.localPosition = quadroPosicaoCaido;
            quadroParaCair.localRotation = Quaternion.Euler(quadroRotacaoCaido);
            if (documentoEscondido != null) documentoEscondido.LiberarDocumento();
        }

        temManivela = PersistenciaManager.Instance.ObterEstado(uniqueID + "_manivela");
        if (temManivela && visualManivela) visualManivela.SetActive(true);

        bool temDiscoPa = PersistenciaManager.Instance.ObterEstado(uniqueID + "_shovelDisk");
        bool temDiscoRuna = PersistenciaManager.Instance.ObterEstado(uniqueID + "_runeDisk");

        if (temDiscoPa) LoadDisk(shovelRecordID, true);
        else if (temDiscoRuna) LoadDisk(runeRecordID, true);
    }

    void Update()
    {
        if (isPlaying && currentDiskID != 0)
        {
            if (currentDiskID == shovelRecordID && visualShovelRecord) 
                visualShovelRecord.transform.Rotate(Vector3.up * 100 * Time.deltaTime);
            if (currentDiskID == runeRecordID && visualRuneRecord) 
                visualRuneRecord.transform.Rotate(Vector3.up * 100 * Time.deltaTime);
                
            if (visualManivela)
                visualManivela.transform.Rotate(Vector3.right * 100 * Time.deltaTime); 
        }

        GerenciarRaioDeAudio();
    }

    void GerenciarRaioDeAudio()
    {
        if (globalSoundtrack == null || FPS_Master.Instance == null) return;

        float dist = Vector3.Distance(transform.position, FPS_Master.Instance.transform.position);
        bool shouldMuteGlobal = isPlaying && (dist <= audioSource.maxDistance);

        if (shouldMuteGlobal && !playerIsInsideRadius)
        {
            playerIsInsideRadius = true;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeGlobalSoundtrack(0f)); 
        }
        else if (!shouldMuteGlobal && playerIsInsideRadius)
        {
            playerIsInsideRadius = false;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeGlobalSoundtrack(globalOriginalVolume)); 
        }
    }

    IEnumerator FadeGlobalSoundtrack(float targetVolume)
    {
        float currentVol = globalSoundtrack.volume;
        float time = 0;
        float duration = 1.5f;

        while (time < duration)
        {
            globalSoundtrack.volume = Mathf.Lerp(currentVol, targetVolume, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        globalSoundtrack.volume = targetVolume;
    }

    public void AoOlhar() 
    { 
        estaOlhando = true;
        if (!mostrandoErro && interactText) interactText.SetActive(true); 
        
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
        if (Time.unscaledTime < tempoBloqueio || mostrandoErro) return;
        tempoBloqueio = Time.unscaledTime + 0.5f; 

        int itemInHand = InventoryManager.Instance != null ? InventoryManager.Instance.itemSelecionado : -1;

        if (currentDiskID == 0)
        {
            if (itemInHand == shovelRecordID || itemInHand == runeRecordID)
            {
                InventoryManager.Instance.ConsumirItem(itemInHand);
                LoadDisk(itemInHand, false);

                // 🔥 TRAVA TRANSACIONAL DE AÇO: Salva o jogo imediatamente.
                // Garante que o disco saiu do bolso e entrou na vitrola no MESMO milissegundo no HD.
                if (PersistenciaManager.Instance != null) PersistenciaManager.Instance.SalvarTudo();
            }
            else if (itemInHand == manivelaItemID && !temManivela)
            {
                InstalarManivela();
            }
            else
            {
                if (rotinaErro != null) StopCoroutine(rotinaErro);
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
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_manivela", true);
            PersistenciaManager.Instance.SalvarTudo();
        }

        if (currentDiskID != 0)
        {
            AtivarFuncoesDoDisco(currentDiskID, false);
        }
    }

    void LoadDisk(int id, bool isLoadingSave)
    {
        currentDiskID = id;
        if (interactText) interactText.SetActive(false); 

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
            if (textoFaltaManivela) textoFaltaManivela.SetActive(true);
        }
    }

    void AtivarFuncoesDoDisco(int id, bool isLoadingSave)
    {
        isPlaying = true;
        audioSource.Play(); 

        if (id == shovelRecordID)
        {
            if (!isSafeOpen && !isLoadingSave)
            {
                StartCoroutine(AbrirCofreRoutine());
            }
        }
        else if (id == runeRecordID)
        {
            bool eventoRunaJaAconteceu = PersistenciaManager.Instance != null && PersistenciaManager.Instance.ObterEstado(uniqueID + "_runeEventDone");

            if (!eventoRunaJaAconteceu && !isLoadingSave)
            {
                if (tvScript) tvScript.ReceberSinalDoDisco();
                if (!quadroJaCaiu) StartCoroutine(RotinaDerrubarQuadro());

                if (PersistenciaManager.Instance != null)
                {
                    PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_runeEventDone", true);
                }
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

        if (sfxSource && somQuadroBatendo) sfxSource.PlayOneShot(somQuadroBatendo);
        if (documentoEscondido != null) documentoEscondido.LiberarDocumento();

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_quadro", true);
    }

    IEnumerator AbrirCofreRoutine()
    {
        isSafeOpen = true;
        
        if (sfxSource && safeOpenSound) sfxSource.PlayOneShot(safeOpenSound);
        
        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_safe", true);

        if (safeDoorPivot == null) yield break;

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

        if (colliderLivro) colliderLivro.enabled = true;
    }

    void EjectDisk()
    {
        isPlaying = false;
        audioSource.Stop();
        
        int diskToReturn = currentDiskID;
        currentDiskID = 0;

        SalvarEstadoDisco(false, false);
        
        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ReceberItemDeVolta(diskToReturn);

        if (diskToReturn == shovelRecordID && visualShovelRecord) visualShovelRecord.SetActive(false);
        else if (diskToReturn == runeRecordID && visualRuneRecord) visualRuneRecord.SetActive(false);

        if (interactText) interactText.SetActive(true);
        
        // 🔥 TRAVA: Salva o jogo imediatamente ao ejetar
        if (PersistenciaManager.Instance != null) PersistenciaManager.Instance.SalvarTudo();
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
        if (estaOlhando && interactText) interactText.SetActive(true);
    }

    [ContextMenu("Generate Unique ID")]
    private void GenerateID() { uniqueID = System.Guid.NewGuid().ToString(); }
}
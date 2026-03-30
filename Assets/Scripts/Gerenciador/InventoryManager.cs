using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement; 

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("--- INVENTÁRIO (Arraste os prefabs aqui) ---")]
    public List<GameObject> itensRegistrados; 

    [Header("--- DEBUG / TESTES (Use no Editor) ---")]
    public bool modoDebug = true;
    public bool comecarComTudo = false;
    public int itemInicialDebug = -1;

    [Header("--- DEBUG / TESTES DA CÂMERA ---")]
    public bool debug_DarCameraNoStart = false;
    public bool debug_DarUpgradeLuzNoStart = false;
    public int idDaCamera = 6;

    [Header("Configurações de Gameplay")]
    public float delayTroca = 0.2f;
    public int itemSelecionado = -1; 
    
    public int idLanterna = 0;
    
    [Header("Animação de Saque (Sway)")]
    public float forcaDropSaque = 0.4f;
    public float velocidadeSaque = 10f;

    private float tempoParaProximaTroca = 0f;
    private Vector3 posicaoOriginalLocal;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ReequiparVisualmente());
    }

    IEnumerator ReequiparVisualmente()
    {
        yield return null; 
        AtualizarVisual(false); 
    }

    void Start()
    {
        posicaoOriginalLocal = transform.localPosition;
        
        if (itensRegistrados == null || itensRegistrados.Count == 0)
            Debug.LogError("[InventoryManager] ERRO: Lista de itens vazia!");

        if (modoDebug && Application.isEditor)
        {
            AplicarDebugInicial();
        }
        else
        {
            AtualizarVisual(false);
        }
    }

    void AplicarDebugInicial()
    {
        if (comecarComTudo)
        {
            for (int i = 0; i < EstadoGlobal.armasDesbloqueadas.Length; i++)
            {
                EstadoGlobal.armasDesbloqueadas[i] = true;
            }
        }

        if (debug_DarCameraNoStart)
        {
            DesbloquearItem(idDaCamera);
        }

        if (debug_DarUpgradeLuzNoStart)
        {
            if (RealityCamera.Instance != null)
            {
                RealityCamera.Instance.ReceberUpgradeLanterna();
            }
        }

        if (itemInicialDebug >= 0)
        {
            DesbloquearItem(itemInicialDebug);
            itemSelecionado = itemInicialDebug;
        }
        else
        {
            if (debug_DarCameraNoStart) itemSelecionado = idDaCamera;
            else itemSelecionado = -1;
        }

        AtualizarVisual(false);
    }

    void Update()
    {
        transform.localPosition = Vector3.Lerp(transform.localPosition, posicaoOriginalLocal, Time.deltaTime * velocidadeSaque);

        if (FPS_Master.travadoInteracao) return;

        if (Input.GetKeyDown(KeyCode.F)) 
        {
            bool cameraNoRosto = RealityCamera.Instance != null && RealityCamera.Instance.modoAtivo;
            if (!cameraNoRosto) ProcessarAtalhoLanterna();
        }
        
        if (!TemAlgumItem()) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f && Time.time >= tempoParaProximaTroca)
        {
            tempoParaProximaTroca = Time.time + delayTroca;
            NavegarInventario(scroll > 0 ? 1 : -1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) TentarEquipar(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TentarEquipar(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TentarEquipar(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TentarEquipar(3);
        if (Input.GetKeyDown(KeyCode.H)) TentarEquipar(-1); 
    }

    // --- A ÚNICA MUDANÇA PARA O FADE DE COLETA FUNCIONAR ---
    public void ReceberItem(int id)
    {
        DesbloquearItem(id);
        itemSelecionado = id; 

        if (id >= 0 && id < itensRegistrados.Count && itensRegistrados[id] != null)
        {
            ItemIdentificador idScript = itensRegistrados[id].GetComponent<ItemIdentificador>();
            if (idScript != null && HUDItemNome.Instance != null)
            {
                HUDItemNome.Instance.MostrarFadeDeColeta(idScript.nomeDoItem);
            }
        }

        AtualizarVisual(true);
    }

    public void ReceberItemDeVolta(int id)
    {
        ReceberItem(id);
    }

    public void ConsumirItem(int id)
    {
        BloquearItem(id);
        if (itemSelecionado == id)
        {
            itemSelecionado = -1; 
            AtualizarVisual(true);
        }
    }

    public void TentarEquipar(int id)
    {
        if (id == -1 || ItemEstaDesbloqueado(id))
        {
            if (itemSelecionado != id)
            {
                itemSelecionado = id;
                AtualizarVisual(true);
            }
        }
    }

    public void ForcarAtualizacaoUI() => AtualizarVisual(true);

    void ProcessarAtalhoLanterna()
    {
        if (itemSelecionado == idLanterna) return;

        if (ItemEstaDesbloqueado(idLanterna))
            TentarEquipar(idLanterna);
    }

    void NavegarInventario(int direcao)
    {
        int total = itensRegistrados.Count;
        int tentativa = itemSelecionado;
        
        for (int i = 0; i < total + 2; i++)
        {
            tentativa += direcao;
            
            if (tentativa >= total) tentativa = -1;
            if (tentativa < -1) tentativa = total - 1;

            if (tentativa == -1 || ItemEstaDesbloqueado(tentativa))
            {
                itemSelecionado = tentativa;
                AtualizarVisual(true);
                return;
            }
        }
    }

    void AtualizarVisual(bool animar = true)
    {
        if (animar) transform.localPosition = posicaoOriginalLocal + new Vector3(0, -forcaDropSaque, 0);

        string nomeParaHUD = "";
        bool cameraEstaNoRosto = RealityCamera.Instance != null && RealityCamera.Instance.modoAtivo;

        for (int i = 0; i < itensRegistrados.Count; i++)
        {
            if (itensRegistrados[i] == null) continue;
            
            bool ativar = (i == itemSelecionado);

            // A câmera nunca desliga se estiver no rosto
            if (i == idDaCamera && cameraEstaNoRosto)
            {
                ativar = true; 
            }
            
            itensRegistrados[i].SetActive(ativar);

            if (i == itemSelecionado) 
            {
                var idScript = itensRegistrados[i].GetComponent<ItemIdentificador>(); 
                nomeParaHUD = idScript ? idScript.nomeDoItem : itensRegistrados[i].name;
            }
        }

        if (itemSelecionado == -1) nomeParaHUD = "";
        if (HUDItemNome.Instance != null && animar) HUDItemNome.Instance.MostrarNome(nomeParaHUD);
    }

    bool ItemEstaDesbloqueado(int id) 
    {
        if (EstadoGlobal.armasDesbloqueadas == null) return false;
        return (id >= 0 && id < EstadoGlobal.armasDesbloqueadas.Length && EstadoGlobal.armasDesbloqueadas[id]);
    }

    void DesbloquearItem(int id) 
    { 
        if(id >= 0 && id < EstadoGlobal.armasDesbloqueadas.Length) EstadoGlobal.armasDesbloqueadas[id] = true; 
    }

    void BloquearItem(int id) 
    { 
        if(id >= 0 && id < EstadoGlobal.armasDesbloqueadas.Length) EstadoGlobal.armasDesbloqueadas[id] = false; 
    }

    bool TemAlgumItem() 
    { 
        foreach (bool b in EstadoGlobal.armasDesbloqueadas) if (b) return true; 
        return false; 
    }
}
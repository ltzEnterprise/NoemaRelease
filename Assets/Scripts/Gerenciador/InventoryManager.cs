using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement; 

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("--- INVENTÁRIO (Arraste os prefabs aqui) ---")]
    public List<GameObject> itensRegistrados; 

    [Header("--- DEBUG / TESTES (Editor) ---")]
    public bool comecarComTudo = false;
    public bool debug_DarUpgradeLuzNoStart = false;

    [Header("--- CONFIGURAÇÕES DE GAMEPLAY ---")]
    public float delayTroca = 0.2f;
    public int itemSelecionado = -1; 
    [Tooltip("Necessário pro código saber qual item não pode ser desligado enquanto mira")]
    public int idDaCamera = 6; 

    [Header("--- ANIMAÇÃO DE SAQUE ---")]
    public float forcaDropSaque = 0.4f;
    [Tooltip("Quanto maior, mais rápido. Ex: 5 = 0.2 segundos pra sacar.")]
    public float velocidadeSaque = 5f; 

    private float tempoParaProximaTroca = 0f;
    private Vector3[] posicoesOriginais;
    
    // Guarda as animações ativas pra uma arma não bugar a outra se você trocar rápido
    private Coroutine[] corrotinasSaque; 

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
        if (itensRegistrados == null || itensRegistrados.Count == 0) return;

        posicoesOriginais = new Vector3[itensRegistrados.Count];
        corrotinasSaque = new Coroutine[itensRegistrados.Count]; 

        for (int i = 0; i < itensRegistrados.Count; i++)
        {
            if (itensRegistrados[i] != null)
            {
                posicoesOriginais[i] = itensRegistrados[i].transform.localPosition;
            }
        }

        if (Application.isEditor) AplicarDebugInicial();
        
        AtualizarVisual(false);
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

        if (debug_DarUpgradeLuzNoStart && RealityCamera.Instance != null)
        {
            RealityCamera.Instance.ReceberUpgradeLanterna();
        }
    }

    void Update()
    {
        // O Update agora tá limpo. A animação acontece na Coroutine lá embaixo.
        
        if (FPS_Master.travadoInteracao) return;
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

    public void ReceberItemDeVolta(int id) { ReceberItem(id); }

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
        string nomeParaHUD = "";
        bool cameraEstaNoRosto = RealityCamera.Instance != null && RealityCamera.Instance.modoAtivo;

        for (int i = 0; i < itensRegistrados.Count; i++)
        {
            if (itensRegistrados[i] == null) continue;
            
            bool ativar = (i == itemSelecionado);

            // Mantém a câmera ligada se ela estiver no rosto
            if (i == idDaCamera && cameraEstaNoRosto)
            {
                ativar = true; 
            }
            
            itensRegistrados[i].SetActive(ativar);

            if (i == itemSelecionado) 
            {
                var idScript = itensRegistrados[i].GetComponent<ItemIdentificador>(); 
                nomeParaHUD = idScript ? idScript.nomeDoItem : itensRegistrados[i].name;

                if (animar) 
                {
                    // Cancela o saque antigo pra não dar conflito se o cara trocar de arma rápido
                    if (corrotinasSaque[i] != null) StopCoroutine(corrotinasSaque[i]);
                    
                    // Inicia o saque perfeito
                    corrotinasSaque[i] = StartCoroutine(RotinaDeSaque(i));
                }
            }
        }

        if (itemSelecionado == -1) nomeParaHUD = "";
        if (HUDItemNome.Instance != null && animar) HUDItemNome.Instance.MostrarNome(nomeParaHUD);
    }

    IEnumerator RotinaDeSaque(int index)
    {
        Transform itemTransform = itensRegistrados[index].transform;
        Vector3 posFinal = posicoesOriginais[index];
        Vector3 posInicial = posFinal + new Vector3(0, -forcaDropSaque, 0);

        // Joga a arma lá embaixo
        itemTransform.localPosition = posInicial;

        float tempoPercorrido = 0f;
        
        // Converte a Velocidade em Segundos. Ex: Velocidade 5 = 0.2s. 
        float tempoTotal = 1f / Mathf.Max(0.1f, velocidadeSaque); 

        while (tempoPercorrido < tempoTotal)
        {
            tempoPercorrido += Time.deltaTime;
            // Interpola linearmente garantindo precisão absoluta
            itemTransform.localPosition = Vector3.Lerp(posInicial, posFinal, tempoPercorrido / tempoTotal);
            yield return null;
        }

        // Garante a colagem final na posição certa
        itemTransform.localPosition = posFinal;
        corrotinasSaque[index] = null;
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
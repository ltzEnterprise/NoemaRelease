using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement; 

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("--- INVENTÁRIO (Arraste os prefabs aqui) ---")]
    public List<GameObject> itensRegistrados; 

    [Header("--- CONFIGURAÇÕES DE GAMEPLAY ---")]
    public bool comecarComTodosOsItens = false; 
    public float delayTroca = 0.2f;
    public int itemSelecionado = -1; 
    public int idDaCamera = 6; 

    [Header("--- ANIMAÇÃO DE SAQUE ---")]
    public float forcaDropSaque = 0.4f;
    public float velocidadeSaque = 5f; 

    private float tempoParaProximaTroca = 0f;
    private Vector3[] posicoesOriginais;
    private Coroutine[] corrotinasSaque; 

    void Awake()
    {
        Instance = this; 
        
        if (itensRegistrados != null)
        {
            foreach (var item in itensRegistrados) 
            {
                if (item != null) item.SetActive(false);
            }
        }
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private string GetChaveInventario()
    {
        if (SistemaGlobal.Instance == null || !SistemaGlobal.Instance.slotFoiDefinido)
        {
            Debug.LogError("[INVENTÁRIO] Slot não definido ao acessar inventário.");
            return string.Empty;
        }
        return "Slot_" + SistemaGlobal.Instance.slotAtual + "_Inv_ItemSelected";
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(AguardarSistemaParaCarregarInventario());
    }

    IEnumerator AguardarSistemaParaCarregarInventario()
    {
        yield return new WaitUntil(() => SistemaGlobal.Instance != null && SistemaGlobal.Instance.sistemaPronto);
        
        string key = GetChaveInventario();
        if (!string.IsNullOrEmpty(key) && PersistenciaManager.Instance != null)
        {
            itemSelecionado = PersistenciaManager.Instance.ObterInt(key, -1);
        }
        
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
                posicoesOriginais[i] = itensRegistrados[i].transform.localPosition;
        }

        StartCoroutine(InitInventarioSeguro());
    }

    IEnumerator InitInventarioSeguro()
    {
        yield return new WaitUntil(() => SistemaGlobal.Instance != null && SistemaGlobal.Instance.sistemaPronto);
        yield return new WaitUntil(() => PersistenciaManager.Instance != null && PersistenciaManager.Instance.DadosProntosParaUso);

        string key = GetChaveInventario();
        if (!string.IsNullOrEmpty(key))
        {
            itemSelecionado = PersistenciaManager.Instance.ObterInt(key, -1);
        }

        if (comecarComTodosOsItens)
        {
            for (int i = 0; i < itensRegistrados.Count; i++) DesbloquearItem(i);
            if (itemSelecionado == -1) itemSelecionado = 0;
        }

        AtualizarVisual(false);
    }

    void Update()
    {
        if (FPS_Master.travadoInteracao) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scroll) > 0.05f && Time.time >= tempoParaProximaTroca)
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
        
        string key = GetChaveInventario();
        if (!string.IsNullOrEmpty(key) && PersistenciaManager.Instance != null) 
        {
            PersistenciaManager.Instance.SalvarInt(key, id);
            // 🔥 REMOVIDO: SalvarTudo(). O disco só roda quando GameManager mandar.
        }

        if (id >= 0 && id < itensRegistrados.Count && itensRegistrados[id] != null)
        {
            ItemIdentificador idScript = itensRegistrados[id].GetComponent<ItemIdentificador>();
            if (idScript != null && HUDItemNome.Instance != null)
                HUDItemNome.Instance.MostrarFadeDeColeta(idScript.nomeDoItem);
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
            
            string key = GetChaveInventario();
            if (!string.IsNullOrEmpty(key) && PersistenciaManager.Instance != null) 
            {
                PersistenciaManager.Instance.SalvarInt(key, -1);
                // 🔥 REMOVIDO: SalvarTudo().
            }
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
                string key = GetChaveInventario();
                if (!string.IsNullOrEmpty(key) && PersistenciaManager.Instance != null) 
                {
                    PersistenciaManager.Instance.SalvarInt(key, id);
                }
                AtualizarVisual(true);
            }
        }
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
                string key = GetChaveInventario();
                if (!string.IsNullOrEmpty(key) && PersistenciaManager.Instance != null) 
                {
                    PersistenciaManager.Instance.SalvarInt(key, tentativa);
                }
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

            if (i != itemSelecionado && i != idDaCamera) 
            {
                if (corrotinasSaque[i] != null) 
                {
                    StopCoroutine(corrotinasSaque[i]);
                    corrotinasSaque[i] = null;
                }
                itensRegistrados[i].transform.localPosition = posicoesOriginais[i];
            }

            bool ativar = (i == itemSelecionado) || (i == idDaCamera && cameraEstaNoRosto);
            
            itensRegistrados[i].SetActive(ativar);

            if (i == itemSelecionado) 
            {
                var idScript = itensRegistrados[i].GetComponent<ItemIdentificador>(); 
                nomeParaHUD = idScript ? idScript.nomeDoItem : itensRegistrados[i].name;

                if (animar) 
                {
                    if (corrotinasSaque[i] != null) StopCoroutine(corrotinasSaque[i]);
                    corrotinasSaque[i] = StartCoroutine(RotinaDeSaque(i));
                }
                else
                {
                    itensRegistrados[i].transform.localPosition = posicoesOriginais[i];
                }
            }
        }

        if (HUDItemNome.Instance != null && animar) HUDItemNome.Instance.MostrarNome(itemSelecionado == -1 ? "" : nomeParaHUD);
    }

    IEnumerator RotinaDeSaque(int index)
    {
        Transform itemTransform = itensRegistrados[index].transform;

        Vector3 posFinal = posicoesOriginais[index];
        Vector3 posInicial = posFinal + new Vector3(0, -forcaDropSaque, 0);
        
        itemTransform.localPosition = posInicial;
        float tempoPercorrido = 0f;
        float tempoTotal = 1f / Mathf.Max(0.1f, velocidadeSaque); 

        while (tempoPercorrido < tempoTotal)
        {
            tempoPercorrido += Time.deltaTime;
            itemTransform.localPosition = Vector3.Lerp(posInicial, posFinal, tempoPercorrido / tempoTotal);
            yield return null;
        }
        itemTransform.localPosition = posFinal;
    }

    bool ItemEstaDesbloqueado(int id) 
    {
        if (comecarComTodosOsItens) return true; 
        
        if (PersistenciaManager.Instance == null) return false;
        return PersistenciaManager.Instance.ObterEstado("InvUnlocked_" + id, false);
    }

    void DesbloquearItem(int id) 
    { 
        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado("InvUnlocked_" + id, true);
    }

    void BloquearItem(int id) 
    { 
        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado("InvUnlocked_" + id, false);
    }
}
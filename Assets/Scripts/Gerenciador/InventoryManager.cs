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
        if (Instance == null)
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
        else Destroy(gameObject);
    }

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (PersistenciaManager.Instance != null)
        {
            itemSelecionado = PersistenciaManager.Instance.ObterInt("Inv_ItemSelected", -1);
        }
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
                posicoesOriginais[i] = itensRegistrados[i].transform.localPosition;
        }
        AtualizarVisual(false);
    }

    void Update()
    {
        if (FPS_Master.travadoInteracao) return;

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
        
        if (PersistenciaManager.Instance != null) 
        {
            PersistenciaManager.Instance.SalvarInt("Inv_ItemSelected", id);
            PersistenciaManager.Instance.SalvarTudo(); // CRAVA NO HD
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
            if (PersistenciaManager.Instance != null) 
            {
                PersistenciaManager.Instance.SalvarInt("Inv_ItemSelected", -1);
                PersistenciaManager.Instance.SalvarTudo();
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
                if (PersistenciaManager.Instance != null) 
                    PersistenciaManager.Instance.SalvarInt("Inv_ItemSelected", id);
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
                if (PersistenciaManager.Instance != null) 
                    PersistenciaManager.Instance.SalvarInt("Inv_ItemSelected", tentativa);
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
            }
        }

        if (HUDItemNome.Instance != null && animar) HUDItemNome.Instance.MostrarNome(itemSelecionado == -1 ? "" : nomeParaHUD);
    }

    IEnumerator RotinaDeSaque(int index)
    {
        // 🔥 A GAMBIARRA FOI ARRANCADA. A arma só desliza pra cima agora sem disparar o OnEnable e foder as posições.
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
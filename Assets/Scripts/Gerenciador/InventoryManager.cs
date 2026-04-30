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

        PrepararArraysEPosicoes();

        if (itensRegistrados != null)
        {
            foreach (var item in itensRegistrados) 
            {
                if (item != null) item.SetActive(false);
            }
        }
    }

    void OnEnable() 
    { 
        SceneManager.sceneLoaded += OnSceneLoaded; 
    }

    void OnDisable() 
    { 
        SceneManager.sceneLoaded -= OnSceneLoaded; 
    }

    void Start()
    {
        PrepararArraysEPosicoes();
        StartCoroutine(InitInventarioSeguro());
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MenuPrincipal") return;

        PrepararArraysEPosicoes();
        StartCoroutine(InitInventarioSeguro());
    }

    private void PrepararArraysEPosicoes()
    {
        if (itensRegistrados == null || itensRegistrados.Count == 0) return;

        if (posicoesOriginais == null || posicoesOriginais.Length != itensRegistrados.Count)
            posicoesOriginais = new Vector3[itensRegistrados.Count];

        if (corrotinasSaque == null || corrotinasSaque.Length != itensRegistrados.Count)
            corrotinasSaque = new Coroutine[itensRegistrados.Count];

        for (int i = 0; i < itensRegistrados.Count; i++)
        {
            if (itensRegistrados[i] != null)
                posicoesOriginais[i] = itensRegistrados[i].transform.localPosition;
        }
    }

    IEnumerator InitInventarioSeguro()
    {
        yield return new WaitUntil(() => PersistenciaManager.Instance != null);
        yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso && !PersistenciaManager.Instance.EstaCarregando);

        CarregarItemSelecionadoDoSave();

        if (comecarComTodosOsItens)
        {
            for (int i = 0; i < itensRegistrados.Count; i++)
                DesbloquearItem(i);

            if (itemSelecionado == -1 && itensRegistrados.Count > 0)
                itemSelecionado = 0;

            SalvarItemSelecionadoNaRAM(itemSelecionado);
        }

        AtualizarVisual(false);
    }

    void Update()
    {
        if (FPS_Master.travadoInteracao) return;

        if (itensRegistrados == null || itensRegistrados.Count == 0) return;
        PrepararArraysEPosicoes();

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

    private string GetChaveInventario()
    {
        if (SistemaGlobal.Instance == null || !SistemaGlobal.Instance.slotFoiDefinido || SistemaGlobal.Instance.slotAtual <= 0)
        {
            return string.Empty;
        }

        return "Slot_" + SistemaGlobal.Instance.slotAtual + "_Inv_ItemSelected";
    }

    private string GetChaveItemDesbloqueado(int id)
    {
        if (SistemaGlobal.Instance == null || !SistemaGlobal.Instance.slotFoiDefinido || SistemaGlobal.Instance.slotAtual <= 0)
        {
            return string.Empty;
        }

        return "Slot_" + SistemaGlobal.Instance.slotAtual + "_InvUnlocked_" + id;
    }

    public void ReceberItem(int id)
    {
        if (id < 0 || itensRegistrados == null || id >= itensRegistrados.Count) return;

        DesbloquearItem(id);
        itemSelecionado = id; 

        SalvarItemSelecionadoNaRAM(id);
        SalvarProgressoSeguro();

        if (id >= 0 && id < itensRegistrados.Count && itensRegistrados[id] != null)
        {
            ItemIdentificador idScript = itensRegistrados[id].GetComponent<ItemIdentificador>();
            if (idScript != null && HUDItemNome.Instance != null)
                HUDItemNome.Instance.MostrarFadeDeColeta(idScript.nomeDoItem);
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
            SalvarItemSelecionadoNaRAM(-1);
            AtualizarVisual(true);
        }

        SalvarProgressoSeguro();
    }

    public void TentarEquipar(int id)
    {
        if (id == -1 || ItemEstaDesbloqueado(id))
        {
            if (itemSelecionado != id)
            {
                itemSelecionado = id;
                SalvarItemSelecionadoNaRAM(id);
                SalvarProgressoSeguro();
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
                SalvarItemSelecionadoNaRAM(tentativa);
                SalvarProgressoSeguro();
                AtualizarVisual(true);
                return;
            }
        }
    }

    void AtualizarVisual(bool animar = true)
    {
        if (itensRegistrados == null) return;

        PrepararArraysEPosicoes();

        if (posicoesOriginais == null || posicoesOriginais.Length != itensRegistrados.Count) return;
        if (corrotinasSaque == null || corrotinasSaque.Length != itensRegistrados.Count) return;

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

        if (HUDItemNome.Instance != null && animar)
            HUDItemNome.Instance.MostrarNome(itemSelecionado == -1 ? "" : nomeParaHUD);
    }

    IEnumerator RotinaDeSaque(int index)
    {
        if (index < 0 || index >= itensRegistrados.Count) yield break;
        if (itensRegistrados[index] == null) yield break;

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
        if (id < 0) return false;
        if (comecarComTodosOsItens) return true; 
        if (PersistenciaManager.Instance == null) return false;

        string key = GetChaveItemDesbloqueado(id);

        if (string.IsNullOrEmpty(key))
            return PersistenciaManager.Instance.ObterEstado("InvUnlocked_" + id, false);

        return PersistenciaManager.Instance.ObterEstado(key, false) ||
               PersistenciaManager.Instance.ObterEstado("InvUnlocked_" + id, false);
    }

    void DesbloquearItem(int id) 
    { 
        if (id < 0) return;
        if (PersistenciaManager.Instance == null) return;

        string key = GetChaveItemDesbloqueado(id);

        if (!string.IsNullOrEmpty(key))
            PersistenciaManager.Instance.RegistrarEstado(key, true);

        PersistenciaManager.Instance.RegistrarEstado("InvUnlocked_" + id, true);
    }

    void BloquearItem(int id) 
    { 
        if (id < 0) return;
        if (PersistenciaManager.Instance == null) return;

        string key = GetChaveItemDesbloqueado(id);

        if (!string.IsNullOrEmpty(key))
            PersistenciaManager.Instance.RegistrarEstado(key, false);

        PersistenciaManager.Instance.RegistrarEstado("InvUnlocked_" + id, false);
    }

    private void CarregarItemSelecionadoDoSave()
    {
        if (PersistenciaManager.Instance == null) return;

        string key = GetChaveInventario();

        if (!string.IsNullOrEmpty(key))
            itemSelecionado = PersistenciaManager.Instance.ObterInt(key, PersistenciaManager.Instance.ObterInt("Inv_ItemSelected", -1));
        else
            itemSelecionado = PersistenciaManager.Instance.ObterInt("Inv_ItemSelected", -1);
    }

    private void SalvarItemSelecionadoNaRAM(int id)
    {
        if (PersistenciaManager.Instance == null) return;

        string key = GetChaveInventario();

        if (!string.IsNullOrEmpty(key))
            PersistenciaManager.Instance.SalvarInt(key, id);

        PersistenciaManager.Instance.SalvarInt("Inv_ItemSelected", id);
    }

    private void SalvarProgressoSeguro()
    {
        if (PersistenciaManager.Instance == null) return;

        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        PersistenciaManager.Instance.SalvarTudo(true);
    }
}

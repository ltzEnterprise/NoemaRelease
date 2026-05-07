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
    private Coroutine rotinaTrocaInventario;
    private int itemVisualAtual = -1;

    void Awake()
    {
        Instance = this; 

        PrepararArraysEPosicoes(false);

        if (itensRegistrados != null)
        {
            foreach (var item in itensRegistrados) 
            {
                if (item != null)
                    item.SetActive(false);
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
        PrepararArraysEPosicoes(false);
        StartCoroutine(InitInventarioSeguro());
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MenuPrincipal") return;

        PrepararArraysEPosicoes(false);
        StartCoroutine(InitInventarioSeguro());
    }

    private void PrepararArraysEPosicoes(bool forcarRecalculo)
    {
        if (itensRegistrados == null || itensRegistrados.Count == 0) return;

        if (posicoesOriginais == null || posicoesOriginais.Length != itensRegistrados.Count)
        {
            posicoesOriginais = new Vector3[itensRegistrados.Count];
            forcarRecalculo = true;
        }

        if (!forcarRecalculo)
            return;

        for (int i = 0; i < itensRegistrados.Count; i++)
        {
            if (itensRegistrados[i] != null)
                posicoesOriginais[i] = itensRegistrados[i].transform.localPosition;
        }
    }

    [ContextMenu("Recalcular Posições Originais dos Itens")]
    public void RecalcularPosicoesOriginais()
    {
        PrepararArraysEPosicoes(true);
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
        TrocarItemSelecionado(id, true);

        SalvarProgressoSeguro();

        if (id >= 0 && id < itensRegistrados.Count && itensRegistrados[id] != null)
        {
            ItemIdentificador idScript = itensRegistrados[id].GetComponent<ItemIdentificador>();

            if (idScript != null && HUDItemNome.Instance != null)
                HUDItemNome.Instance.MostrarFadeDeColeta(idScript.nomeDoItem);
        }
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
            TrocarItemSelecionado(-1, true);
        }

        SalvarProgressoSeguro();
    }

    public void TentarEquipar(int id)
    {
        if (id == -1 || ItemEstaDesbloqueado(id))
        {
            if (itemSelecionado != id)
            {
                TrocarItemSelecionado(id, true);
                SalvarProgressoSeguro();
            }
        }
    }

    private void TrocarItemSelecionado(int novoItem, bool animar)
    {
        itemSelecionado = novoItem;
        SalvarItemSelecionadoNaRAM(novoItem);
        AtualizarVisual(animar);
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
                TrocarItemSelecionado(tentativa, true);
                SalvarProgressoSeguro();
                return;
            }
        }
    }

    void AtualizarVisual(bool animar = true)
    {
        if (itensRegistrados == null) return;

        PrepararArraysEPosicoes(false);

        if (posicoesOriginais == null || posicoesOriginais.Length != itensRegistrados.Count) return;

        if (rotinaTrocaInventario != null)
        {
            StopCoroutine(rotinaTrocaInventario);
            rotinaTrocaInventario = null;
        }

        if (animar)
        {
            rotinaTrocaInventario = StartCoroutine(RotinaTrocaVisual(itemVisualAtual, itemSelecionado));
        }
        else
        {
            AplicarVisualInstantaneo();
        }

        string nomeParaHUD = "";

        if (itemSelecionado >= 0 && itemSelecionado < itensRegistrados.Count && itensRegistrados[itemSelecionado] != null)
        {
            var idScript = itensRegistrados[itemSelecionado].GetComponent<ItemIdentificador>(); 
            nomeParaHUD = idScript ? idScript.nomeDoItem : itensRegistrados[itemSelecionado].name;
        }

        if (HUDItemNome.Instance != null && animar)
            HUDItemNome.Instance.MostrarNome(itemSelecionado == -1 ? "" : nomeParaHUD);
    }

    private void AplicarVisualInstantaneo()
    {
        bool cameraEstaNoRosto = RealityCamera.Instance != null && RealityCamera.Instance.modoAtivo;

        for (int i = 0; i < itensRegistrados.Count; i++)
        {
            if (itensRegistrados[i] == null) continue;

            itensRegistrados[i].transform.localPosition = posicoesOriginais[i];

            bool ativar = (i == itemSelecionado) || (i == idDaCamera && cameraEstaNoRosto);
            itensRegistrados[i].SetActive(ativar);
        }

        itemVisualAtual = itemSelecionado;
    }

    IEnumerator RotinaTrocaVisual(int itemAnterior, int itemNovo)
    {
        bool cameraEstaNoRosto = RealityCamera.Instance != null && RealityCamera.Instance.modoAtivo;

        itemAnterior = ValidarIndiceVisual(itemAnterior) ? itemAnterior : DescobrirItemVisualAtivo();

        for (int i = 0; i < itensRegistrados.Count; i++)
        {
            if (itensRegistrados[i] == null) continue;

            bool deveManterPorCamera = (i == idDaCamera && cameraEstaNoRosto);
            bool ehAnterior = i == itemAnterior;
            bool ehNovo = i == itemNovo;

            if (!ehAnterior && !ehNovo && !deveManterPorCamera)
            {
                itensRegistrados[i].transform.localPosition = posicoesOriginais[i];
                itensRegistrados[i].SetActive(false);
            }
        }

        Transform anteriorTransform = null;
        Transform novoTransform = null;

        Vector3 anteriorInicio = Vector3.zero;
        Vector3 anteriorFim = Vector3.zero;
        Vector3 novoInicio = Vector3.zero;
        Vector3 novoFim = Vector3.zero;

        bool animarAnterior = itemAnterior >= 0 &&
                              itemAnterior < itensRegistrados.Count &&
                              itensRegistrados[itemAnterior] != null &&
                              itemAnterior != itemNovo;

        bool animarNovo = itemNovo >= 0 &&
                          itemNovo < itensRegistrados.Count &&
                          itensRegistrados[itemNovo] != null;

        if (animarAnterior)
        {
            itensRegistrados[itemAnterior].SetActive(true);
            anteriorTransform = itensRegistrados[itemAnterior].transform;
            anteriorInicio = anteriorTransform.localPosition;
            anteriorFim = posicoesOriginais[itemAnterior] + new Vector3(0f, -forcaDropSaque, 0f);
        }

        if (animarNovo)
        {
            itensRegistrados[itemNovo].SetActive(true);
            novoTransform = itensRegistrados[itemNovo].transform;
            novoFim = posicoesOriginais[itemNovo];
            novoInicio = novoFim + new Vector3(0f, -forcaDropSaque, 0f);
            novoTransform.localPosition = novoInicio;
        }

        float tempoPercorrido = 0f;
        float tempoTotal = 1f / Mathf.Max(0.1f, velocidadeSaque);

        while (tempoPercorrido < tempoTotal)
        {
            tempoPercorrido += Time.deltaTime;
            float p = Mathf.Clamp01(tempoPercorrido / tempoTotal);

            if (anteriorTransform != null)
                anteriorTransform.localPosition = Vector3.Lerp(anteriorInicio, anteriorFim, p);

            if (novoTransform != null)
                novoTransform.localPosition = Vector3.Lerp(novoInicio, novoFim, p);

            yield return null;
        }

        if (animarAnterior)
        {
            bool manterCameraAtiva = itemAnterior == idDaCamera && cameraEstaNoRosto;

            itensRegistrados[itemAnterior].transform.localPosition = posicoesOriginais[itemAnterior];

            if (!manterCameraAtiva)
                itensRegistrados[itemAnterior].SetActive(false);
        }

        if (animarNovo)
        {
            itensRegistrados[itemNovo].transform.localPosition = posicoesOriginais[itemNovo];
            itensRegistrados[itemNovo].SetActive(true);
        }

        for (int i = 0; i < itensRegistrados.Count; i++)
        {
            if (itensRegistrados[i] == null) continue;

            bool ativar = (i == itemNovo) || (i == idDaCamera && cameraEstaNoRosto);

            itensRegistrados[i].SetActive(ativar);

            if (!ativar)
                itensRegistrados[i].transform.localPosition = posicoesOriginais[i];
        }

        itemVisualAtual = itemNovo;
        rotinaTrocaInventario = null;
    }

    private bool ValidarIndiceVisual(int id)
    {
        return id >= 0 && itensRegistrados != null && id < itensRegistrados.Count && itensRegistrados[id] != null;
    }

    private int DescobrirItemVisualAtivo()
    {
        if (itensRegistrados == null) return -1;

        for (int i = 0; i < itensRegistrados.Count; i++)
        {
            if (i == idDaCamera && RealityCamera.Instance != null && RealityCamera.Instance.modoAtivo)
                continue;

            if (itensRegistrados[i] != null && itensRegistrados[i].activeSelf)
                return i;
        }

        return -1;
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
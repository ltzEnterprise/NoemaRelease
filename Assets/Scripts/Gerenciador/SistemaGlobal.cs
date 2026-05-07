using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class SistemaGlobal : MonoBehaviour
{
    public static SistemaGlobal Instance;

    [Header("CONFIGURAÇÃO GERAL")]
    public string nomeCenaPadrao = "DreamSceane"; 
    public string nomeCenaMenu = "MenuPrincipal";

    [Header("ESTADO DO SAVE")]
    public int slotAtual = -1; 
    public bool slotFoiDefinido = false;
    public bool sistemaPronto = false; 

    [HideInInspector] public bool deveCarregarPosicaoAoIniciar = false;
    [HideInInspector] public bool acabouDeCarregar = false; 

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SistemaGlobal] Cópia local/duplicada destruída. Mantendo a instância global.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void DefinirSlot(int slot)
    {
        if (slot <= 0)
        {
            Debug.LogError("[SISTEMA] Tentativa de definir slot inválido: " + slot);
            return;
        }

        slotAtual = slot;
        slotFoiDefinido = true;

        Debug.Log($"<color=yellow>[SISTEMA] Slot {slot} definido e validado para esta sessão.</color>");
    }

    public void SalvarJogo(Vector3 posicaoPlayer, string nomeCena)
    {
        if (!slotFoiDefinido || slotAtual <= 0)
        {
            Debug.LogError("[SistemaGlobal] Tentativa de salvar sem slot definido.");
            return;
        }

        if (PersistenciaManager.Instance == null)
        {
            Debug.LogError("[SistemaGlobal] PersistenciaManager.Instance está nulo.");
            return;
        }

        if (!sistemaPronto)
        {
            Debug.LogWarning("[SistemaGlobal] Save abortado: sistema ainda não está pronto.");
            return;
        }

        string prefixo = "Slot_" + slotAtual;

        PersistenciaManager.Instance.SalvarString(prefixo + "_Cena", nomeCena);
        PersistenciaManager.Instance.SalvarFloat(prefixo + "_PosX", posicaoPlayer.x);
        PersistenciaManager.Instance.SalvarFloat(prefixo + "_PosY", posicaoPlayer.y);
        PersistenciaManager.Instance.SalvarFloat(prefixo + "_PosZ", posicaoPlayer.z);

        EstadoGlobal.SalvarNoSlot(slotAtual);

        PersistenciaManager.Instance.SalvarTudo(true);

        Debug.Log($"<color=cyan>[SAVE SUCCESS] Slot: {slotAtual} | Cena: {nomeCena}</color>");
    }

    public void CarregarJogo(int slot)
    {
        sistemaPronto = false;
        DefinirSlot(slot);

        if (PersistenciaManager.Instance == null)
        {
            Debug.LogError("[SistemaGlobal] Não existe PersistenciaManager para carregar o jogo.");
            return;
        }

        if (ExisteSave(slot))
        {
            PersistenciaManager.Instance.LimparDicionario();
            PersistenciaManager.Instance.CarregarDoDisco(slot);

            EstadoGlobal.CarregarDoSlot(slot);

            deveCarregarPosicaoAoIniciar = true;
            acabouDeCarregar = true;

            string cenaParaCarregar = PersistenciaManager.Instance.ObterString("Slot_" + slot + "_Cena");

            if (string.IsNullOrEmpty(cenaParaCarregar))
                cenaParaCarregar = nomeCenaPadrao;

            Debug.Log($"<color=green>[LOAD] Slot {slot} carregando cena {cenaParaCarregar}</color>");

            SceneManager.LoadScene(cenaParaCarregar);
        }
        else
        {
            IniciarNovoJogo(slot, nomeCenaPadrao);
        }
    }

    public void IniciarNovoJogo(int slot, string cenaInicial)
    {
        sistemaPronto = false;
        DefinirSlot(slot);

        if (PersistenciaManager.Instance == null)
        {
            Debug.LogError("[SistemaGlobal] Não existe PersistenciaManager para iniciar novo jogo.");
            return;
        }

        EstadoGlobal.ResetarTudo();

        PersistenciaManager.Instance.LimparDicionario();
        PersistenciaManager.Instance.IniciarNovoJogo(slot);

        string cenaParaSalvar = string.IsNullOrEmpty(cenaInicial) ? nomeCenaPadrao : cenaInicial;

        PersistenciaManager.Instance.SalvarString("Slot_" + slot + "_Cena", cenaParaSalvar);
        PersistenciaManager.Instance.SalvarTudo(true);

        deveCarregarPosicaoAoIniciar = false;
        acabouDeCarregar = false;

        SceneManager.LoadScene(cenaParaSalvar);
    }

    public void ApagarSave(int slot)
    {
        string pasta = Path.Combine(Application.persistentDataPath, "Saves");
        string caminho = Path.Combine(pasta, $"Save_Slot_{slot}.json");
        string backup = caminho + ".bak";
        string temp = caminho + ".tmp";
        string pending = caminho + ".bak.pending";
        string count = caminho + ".bak.count";

        if (File.Exists(caminho)) File.Delete(caminho);
        if (File.Exists(backup)) File.Delete(backup);
        if (File.Exists(temp)) File.Delete(temp);
        if (File.Exists(pending)) File.Delete(pending);
        if (File.Exists(count)) File.Delete(count);

        EstadoGlobal.ResetarTudo();

        if (slotAtual == slot)
        {
            slotFoiDefinido = false;
            sistemaPronto = false;
            slotAtual = -1;
            deveCarregarPosicaoAoIniciar = false;
            acabouDeCarregar = false;

            if (PersistenciaManager.Instance != null)
                PersistenciaManager.Instance.LimparDicionario();
        }

        Debug.Log($"[SistemaGlobal] Save do slot {slot} apagado.");
    }

    public bool ExisteSave(int slot)
    {
        string caminho = Path.Combine(Application.persistentDataPath, "Saves", $"Save_Slot_{slot}.json");
        return File.Exists(caminho);
    }

    public string GetDataSave(int slot)
    {
        string caminho = Path.Combine(Application.persistentDataPath, "Saves", $"Save_Slot_{slot}.json");

        if (File.Exists(caminho))
            return File.GetLastWriteTime(caminho).ToString("dd/MM HH:mm");

        return "Vazio";
    }
}
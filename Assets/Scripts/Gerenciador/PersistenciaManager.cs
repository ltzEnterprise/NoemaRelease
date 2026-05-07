using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine.SceneManagement;

[Serializable] public class IntEntry { public string k; public int v; }
[Serializable] public class FloatEntry { public string k; public float v; }
[Serializable] public class StringEntry { public string k; public string v; }

[Serializable]
public class DadosDeSave
{
    public List<IntEntry> ints = new List<IntEntry>();
    public List<FloatEntry> floats = new List<FloatEntry>();
    public List<StringEntry> strings = new List<StringEntry>();
}

public class PersistenciaManager : MonoBehaviour
{
    public static PersistenciaManager Instance;

    [Header("--- DEV MODE ---")]
    public bool desativarSaveNoEditor = false;

    private Dictionary<string, bool> estadosObjetosRAM = new Dictionary<string, bool>();
    private Dictionary<string, int> cacheInt = new Dictionary<string, int>();
    private Dictionary<string, float> cacheFloat = new Dictionary<string, float>();
    private Dictionary<string, string> cacheString = new Dictionary<string, string>();

    private DadosDeSave dadosParaSerializar = new DadosDeSave();

    private bool inicializado = false;
    private float ultimoSaveTempo = 0f;
    private float intervaloMinimoSave = 2f;

    private const int SAVES_PARA_ATUALIZAR_BACKUP = 3;

    public bool DadosProntosParaUso { get; private set; } = false;
    public bool EstaCarregando { get; private set; } = false;

    public string DiretorioSaves
    {
        get
        {
            string dir = Path.Combine(Application.persistentDataPath, "Saves");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PersistenciaManager] Cópia local/duplicada destruída. Mantendo a instância global.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        inicializado = true;
    }

    void OnApplicationQuit()
    {
        SalvarTudo(true);
    }

    private void GarantirListasValidas()
    {
        if (dadosParaSerializar.ints == null) dadosParaSerializar.ints = new List<IntEntry>();
        if (dadosParaSerializar.floats == null) dadosParaSerializar.floats = new List<FloatEntry>();
        if (dadosParaSerializar.strings == null) dadosParaSerializar.strings = new List<StringEntry>();
    }

    public void IniciarNovoJogo(int slot)
    {
        ResetarDicionarios();

        if (SistemaGlobal.Instance != null)
            SistemaGlobal.Instance.DefinirSlot(slot);

        DadosProntosParaUso = true;
        EstaCarregando = false;
    }

    public void CarregarDoDisco(int slot)
    {
        EstaCarregando = true;
        DadosProntosParaUso = false;

        if (SistemaGlobal.Instance != null)
            SistemaGlobal.Instance.DefinirSlot(slot);

        ResetarDicionarios();

        #if UNITY_EDITOR
        if (desativarSaveNoEditor)
        {
            DadosProntosParaUso = true;
            EstaCarregando = false;
            return;
        }
        #endif

        string caminho = Path.Combine(DiretorioSaves, $"Save_Slot_{slot}.json");

        if (!File.Exists(caminho))
        {
            DadosProntosParaUso = true;
            EstaCarregando = false;
            return;
        }

        try
        {
            string json = File.ReadAllText(caminho);
            dadosParaSerializar = JsonUtility.FromJson<DadosDeSave>(json);

            if (dadosParaSerializar == null)
            {
                ResetarDicionarios();
            }
            else
            {
                GarantirListasValidas();
                SincronizarListasParaCache();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[SAVE] JSON corrompido: " + e.Message);
            TentarBackup(slot);
        }
        finally
        {
            DadosProntosParaUso = true;
            EstaCarregando = false;
        }
    }

    private void TentarBackup(int slot)
    {
        string caminhoBackup = Path.Combine(DiretorioSaves, $"Save_Slot_{slot}.json.bak");

        if (!File.Exists(caminhoBackup))
        {
            ResetarDicionarios();
            return;
        }

        try
        {
            string json = File.ReadAllText(caminhoBackup);
            dadosParaSerializar = JsonUtility.FromJson<DadosDeSave>(json);

            if (dadosParaSerializar == null)
            {
                ResetarDicionarios();
            }
            else
            {
                GarantirListasValidas();
                SincronizarListasParaCache();
            }
        }
        catch
        {
            ResetarDicionarios();
        }
    }

    private void SincronizarListasParaCache()
    {
        estadosObjetosRAM.Clear();

        cacheInt.Clear();
        foreach (var e in dadosParaSerializar.ints)
            if (e != null && !string.IsNullOrEmpty(e.k)) cacheInt[e.k] = e.v;

        cacheFloat.Clear();
        foreach (var e in dadosParaSerializar.floats)
            if (e != null && !string.IsNullOrEmpty(e.k)) cacheFloat[e.k] = e.v;

        cacheString.Clear();
        foreach (var e in dadosParaSerializar.strings)
            if (e != null && !string.IsNullOrEmpty(e.k)) cacheString[e.k] = e.v;
    }

    public void ResetarDicionarios()
    {
        cacheInt.Clear();
        cacheFloat.Clear();
        cacheString.Clear();
        estadosObjetosRAM.Clear();

        dadosParaSerializar = new DadosDeSave();
        GarantirListasValidas();
    }

    public void LimparDicionario()
    {
        DadosProntosParaUso = false;
        EstaCarregando = false;
        ResetarDicionarios();
    }

    public void SalvarTudo(bool forcarSaveAbsoluto = false)
    {
        #if UNITY_EDITOR
        if (desativarSaveNoEditor) return;
        #endif

        if (!inicializado) return;

        if (SistemaGlobal.Instance == null || !SistemaGlobal.Instance.slotFoiDefinido || SistemaGlobal.Instance.slotAtual <= 0)
        {
            Debug.LogError("[SAVE BLOQUEADO] Slot não definido.");
            return;
        }

        int slot = SistemaGlobal.Instance.slotAtual;
        string cenaAtual = SceneManager.GetActiveScene().name;
        bool estaNoMenu = cenaAtual == "MenuPrincipal";

        if (!forcarSaveAbsoluto)
        {
            if (Time.unscaledTime - ultimoSaveTempo < intervaloMinimoSave) return;
            ultimoSaveTempo = Time.unscaledTime;

            if (EstaCarregando) return;
            if (!DadosProntosParaUso) return;
            if (!SceneManager.GetActiveScene().isLoaded) return;

            if (estaNoMenu) return;
            if (!GameManager.CenaPronta) return;
            if (!SistemaGlobal.Instance.sistemaPronto) return;
        }

        bool podeSalvarObjetosDaCena =
            !estaNoMenu &&
            GameManager.CenaPronta &&
            SistemaGlobal.Instance.sistemaPronto;

        if (podeSalvarObjetosDaCena)
        {
            LimparRegistroGlobalMorto();
            SerializarSaveableItemsVivos();
        }

        try
        {
            dadosParaSerializar.ints.Clear();
            foreach (var kv in cacheInt)
                dadosParaSerializar.ints.Add(new IntEntry { k = kv.Key, v = kv.Value });

            dadosParaSerializar.floats.Clear();
            foreach (var kv in cacheFloat)
                dadosParaSerializar.floats.Add(new FloatEntry { k = kv.Key, v = kv.Value });

            dadosParaSerializar.strings.Clear();
            foreach (var kv in cacheString)
                dadosParaSerializar.strings.Add(new StringEntry { k = kv.Key, v = kv.Value });

            string json = JsonUtility.ToJson(dadosParaSerializar, true);

            string caminho = GetCaminhoSave(slot);
            string tempPath = GetCaminhoTemp(slot);

            string jsonAnteriorDoSavePrincipal = null;

            if (File.Exists(caminho))
                jsonAnteriorDoSavePrincipal = File.ReadAllText(caminho);

            File.WriteAllText(tempPath, json);

            if (File.Exists(caminho))
                File.Delete(caminho);

            File.Move(tempPath, caminho);

            AtualizarBackupDeTresSaves(slot, jsonAnteriorDoSavePrincipal, json);

            Debug.Log($"<color=cyan>[SAVE DISCO] Arquivo escrito no slot {slot}: {caminho}</color>");
        }
        catch (Exception e)
        {
            Debug.LogError("[SAVE] Erro ao escrever no disco: " + e.Message);
        }
    }

    private string GetCaminhoSave(int slot)
    {
        return Path.Combine(DiretorioSaves, $"Save_Slot_{slot}.json");
    }

    private string GetCaminhoTemp(int slot)
    {
        return GetCaminhoSave(slot) + ".tmp";
    }

    private string GetCaminhoBackup(int slot)
    {
        return GetCaminhoSave(slot) + ".bak";
    }

    private string GetCaminhoBackupPendente(int slot)
    {
        return GetCaminhoSave(slot) + ".bak.pending";
    }

    private string GetCaminhoContadorBackup(int slot)
    {
        return GetCaminhoSave(slot) + ".bak.count";
    }

    private int LerContadorBackup(int slot)
    {
        string caminhoContador = GetCaminhoContadorBackup(slot);

        if (!File.Exists(caminhoContador))
            return 0;

        try
        {
            string texto = File.ReadAllText(caminhoContador);

            if (int.TryParse(texto, out int valor))
                return Mathf.Clamp(valor, 0, SAVES_PARA_ATUALIZAR_BACKUP - 1);
        }
        catch { }

        return 0;
    }

    private void SalvarContadorBackup(int slot, int valor)
    {
        string caminhoContador = GetCaminhoContadorBackup(slot);

        try
        {
            File.WriteAllText(caminhoContador, valor.ToString());
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BACKUP] Não foi possível salvar contador de backup: " + e.Message);
        }
    }

    private void AtualizarBackupDeTresSaves(int slot, string jsonAnteriorDoSavePrincipal, string jsonNovoSavePrincipal)
    {
        string caminhoBackup = GetCaminhoBackup(slot);
        string caminhoPendente = GetCaminhoBackupPendente(slot);

        try
        {
            if (!File.Exists(caminhoPendente))
            {
                string candidatoInicial = !string.IsNullOrEmpty(jsonAnteriorDoSavePrincipal)
                    ? jsonAnteriorDoSavePrincipal
                    : jsonNovoSavePrincipal;

                File.WriteAllText(caminhoPendente, candidatoInicial);
            }

            int contador = LerContadorBackup(slot);
            contador++;

            if (contador >= SAVES_PARA_ATUALIZAR_BACKUP)
            {
                if (File.Exists(caminhoPendente))
                    File.Copy(caminhoPendente, caminhoBackup, true);

                if (File.Exists(caminhoPendente))
                    File.Delete(caminhoPendente);

                contador = 0;

                Debug.Log($"<color=orange>[BACKUP] Backup do slot {slot} atualizado com versão de {SAVES_PARA_ATUALIZAR_BACKUP} saves atrás.</color>");
            }

            SalvarContadorBackup(slot, contador);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BACKUP] Falha ao atualizar backup de 3 saves: " + e.Message);
        }
    }

    public void ResetarControleBackupDoSlot(int slot)
    {
        try
        {
            string pendente = GetCaminhoBackupPendente(slot);
            string contador = GetCaminhoContadorBackup(slot);

            if (File.Exists(pendente)) File.Delete(pendente);
            if (File.Exists(contador)) File.Delete(contador);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BACKUP] Falha ao resetar controle de backup do slot " + slot + ": " + e.Message);
        }
    }

    private void LimparRegistroGlobalMorto()
    {
        var keys = new List<string>(SaveableItem.registroGlobal.Keys);

        foreach (var key in keys)
        {
            if (SaveableItem.registroGlobal[key] == null)
                SaveableItem.registroGlobal.Remove(key);
        }
    }

    private void SerializarSaveableItemsVivos()
    {
        foreach (var kvp in SaveableItem.registroGlobal)
        {
            if (kvp.Value == null) continue;

            RegistrarEstado(kvp.Key, kvp.Value.gameObject.activeSelf);

            if (kvp.Value.gameObject.activeSelf)
                SalvarTransform(kvp.Key, kvp.Value.transform);
        }
    }

    public void SalvarInt(string k, int v)
    {
        if (string.IsNullOrEmpty(k)) return;
        cacheInt[k] = v;
    }

    public int ObterInt(string k, int padrao = 0)
    {
        return cacheInt.TryGetValue(k, out int v) ? v : padrao;
    }

    public void SalvarFloat(string k, float v)
    {
        if (string.IsNullOrEmpty(k)) return;
        cacheFloat[k] = v;
    }

    public float ObterFloat(string k, float padrao = 0f)
    {
        return cacheFloat.TryGetValue(k, out float v) ? v : padrao;
    }

    public bool TemFloat(string k)
    {
        return cacheFloat.ContainsKey(k);
    }

    public void SalvarString(string k, string v)
    {
        if (string.IsNullOrEmpty(k)) return;
        cacheString[k] = v ?? "";
    }

    public string ObterString(string k, string padrao = "")
    {
        return cacheString.TryGetValue(k, out string v) ? v : padrao;
    }

    public bool TemEstadoSalvo(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        if (cacheInt.ContainsKey(id + "_Active")) return true;
        if (estadosObjetosRAM.ContainsKey(id)) return true;

        return false;
    }

    public void RegistrarEstado(string id, bool estado)
    {
        if (string.IsNullOrEmpty(id)) return;

        estadosObjetosRAM[id] = estado;
        SalvarInt(id + "_Active", estado ? 1 : 0);
    }

    public bool ObterEstado(string id, bool valorPadrao = false)
    {
        if (string.IsNullOrEmpty(id)) return valorPadrao;

        if (cacheInt.TryGetValue(id + "_Active", out int v)) return v == 1;
        if (estadosObjetosRAM.ContainsKey(id)) return estadosObjetosRAM[id];

        return valorPadrao;
    }

    public void SalvarTransform(string id, Transform t)
    {
        if (string.IsNullOrEmpty(id) || t == null) return;

        SalvarFloat(id + "_px", t.position.x);
        SalvarFloat(id + "_py", t.position.y);
        SalvarFloat(id + "_pz", t.position.z);

        SalvarFloat(id + "_rx", t.eulerAngles.x);
        SalvarFloat(id + "_ry", t.eulerAngles.y);
        SalvarFloat(id + "_rz", t.eulerAngles.z);
    }

    public void CarregarTransform(string id, Transform t)
    {
        if (string.IsNullOrEmpty(id) || t == null) return;

        if (cacheFloat.ContainsKey(id + "_px"))
        {
            t.position = new Vector3(
                ObterFloat(id + "_px"),
                ObterFloat(id + "_py"),
                ObterFloat(id + "_pz")
            );

            t.eulerAngles = new Vector3(
                ObterFloat(id + "_rx"),
                ObterFloat(id + "_ry"),
                ObterFloat(id + "_rz")
            );
        }
    }

    public bool CarregarEstadoObjeto(string id, bool valorPadrao)
    {
        return ObterEstado(id, valorPadrao);
    }

    public bool CarregarEstadoObjeto(string id)
    {
        return ObterEstado(id, false);
    }
}
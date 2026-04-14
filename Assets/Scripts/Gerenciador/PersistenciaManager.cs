using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections; // Necessário para a Coroutine de Autosave

[Serializable] public class IntEntry { public string k; public int v; }
[Serializable] public class FloatEntry { public string k; public float v; }
[Serializable] public class StringEntry { public string k; public string v; }

[Serializable]
public class DadosDeSave {
    public List<IntEntry> ints = new List<IntEntry>();
    public List<FloatEntry> floats = new List<FloatEntry>();
    public List<StringEntry> strings = new List<StringEntry>();
}

public class PersistenciaManager : MonoBehaviour
{
    public static PersistenciaManager Instance;

    private Dictionary<string, bool> estadosObjetosRAM = new Dictionary<string, bool>();
    private Dictionary<string, int> cacheInt = new Dictionary<string, int>();
    private Dictionary<string, float> cacheFloat = new Dictionary<string, float>();
    private Dictionary<string, string> cacheString = new Dictionary<string, string>();

    private DadosDeSave dadosParaSerializar = new DadosDeSave();

    private int slotCarregado = -1;
    private bool inicializado = false;

    private float ultimoSaveTempo = 0f;
    private float intervaloMinimoSave = 2f;

    public string DiretorioSaves => Path.Combine(Application.persistentDataPath, "Saves");

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (transform.parent == null)
                DontDestroyOnLoad(gameObject);

            if (!Directory.Exists(DiretorioSaves))
                Directory.CreateDirectory(DiretorioSaves);

            inicializado = true;
            
            // Inicia o Autosave a cada 5 minutos (300 segundos)
            StartCoroutine(RotinaAutoSave());
        }
        else Destroy(gameObject);
    }

    // =========================
    // AUTO SAVE
    // =========================
    
    // Salva a cada 5 minutos em background (super leve)
    private IEnumerator RotinaAutoSave()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(300f);
            if (!ModoSemSave() && inicializado)
            {
                SalvarTudo();
            }
        }
    }

    void OnApplicationQuit() => SalvarTudo();

    void OnApplicationFocus(bool focus)
    {
        if (!focus && Time.unscaledTime - ultimoSaveTempo > intervaloMinimoSave)
        {
            SalvarTudo();
            ultimoSaveTempo = Time.unscaledTime;
        }
    }

    public bool ModoSemSave() =>
        SistemaGlobal.Instance == null || SistemaGlobal.Instance.slotAtual <= 0;

    // =========================
    // LOAD
    // =========================
    private void GarantirDadosCarregados()
    {
        if (ModoSemSave()) return;

        int slot = SistemaGlobal.Instance.slotAtual;

        if (slotCarregado != slot && slot > 0)
            CarregarDoDisco(slot);
    }

    private void GarantirListasValidas()
    {
        if (dadosParaSerializar.ints == null) dadosParaSerializar.ints = new List<IntEntry>();
        if (dadosParaSerializar.floats == null) dadosParaSerializar.floats = new List<FloatEntry>();
        if (dadosParaSerializar.strings == null) dadosParaSerializar.strings = new List<StringEntry>();
    }

    private void CarregarDoDisco(int slot)
    {
        slotCarregado = slot;

        string caminho = Path.Combine(DiretorioSaves, $"Save_Slot_{slot}.json");

        if (!File.Exists(caminho))
        {
            ResetarDicionarios();
            return;
        }

        try
        {
            string json = File.ReadAllText(caminho);
            dadosParaSerializar = JsonUtility.FromJson<DadosDeSave>(json);

            if (dadosParaSerializar == null)
            {
                ResetarDicionarios();
                return;
            }

            GarantirListasValidas();
            SincronizarListasParaCache();
        }
        catch (Exception e)
        {
            Debug.LogError("[SAVE] Erro ao carregar: " + e.Message);
            TentarBackup(slot);
        }
    }

    private void TentarBackup(int slot)
    {
        string caminho = Path.Combine(DiretorioSaves, $"Save_Slot_{slot}.json.bak");

        if (!File.Exists(caminho))
        {
            ResetarDicionarios();
            return;
        }

        try
        {
            dadosParaSerializar = JsonUtility.FromJson<DadosDeSave>(File.ReadAllText(caminho));
            GarantirListasValidas();
            SincronizarListasParaCache();
        }
        catch
        {
            ResetarDicionarios();
        }
    }

    private void SincronizarListasParaCache()
    {
        // 🔥 AQUI ESTAVA O MAIOR ERRO: O cache da RAM precisava ser limpo ao carregar um novo slot!
        estadosObjetosRAM.Clear(); 
        
        cacheInt.Clear();
        foreach (var e in dadosParaSerializar.ints)
            cacheInt[e.k] = e.v;

        cacheFloat.Clear();
        foreach (var e in dadosParaSerializar.floats)
            cacheFloat[e.k] = e.v;

        cacheString.Clear();
        foreach (var e in dadosParaSerializar.strings)
            cacheString[e.k] = e.v;
    }

    private void ResetarDicionarios()
    {
        cacheInt.Clear();
        cacheFloat.Clear();
        cacheString.Clear();
        estadosObjetosRAM.Clear();

        dadosParaSerializar = new DadosDeSave();
        GarantirListasValidas();
    }

    // =========================
    // SAVE
    // =========================
    public void SalvarTudo()
    {
        if (!inicializado || ModoSemSave()) return;

        int slot = SistemaGlobal.Instance.slotAtual;
        if (slot <= 0) return;

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
            string caminho = Path.Combine(DiretorioSaves, $"Save_Slot_{slot}.json");

            if (File.Exists(caminho))
                File.Copy(caminho, caminho + ".bak", true);

            File.WriteAllText(caminho, json);
        }
        catch (Exception e)
        {
            Debug.LogError("[SAVE] Erro ao salvar: " + e.Message);
        }
    }

    // =========================
    // API
    // =========================
    public void SalvarInt(string k, int v) { GarantirDadosCarregados(); cacheInt[k] = v; }
    public int ObterInt(string k) { GarantirDadosCarregados(); return cacheInt.TryGetValue(k, out int v) ? v : 0; }

    public void SalvarFloat(string k, float v) { GarantirDadosCarregados(); cacheFloat[k] = v; }
    public float ObterFloat(string k) { GarantirDadosCarregados(); return cacheFloat.TryGetValue(k, out float v) ? v : 0f; }

    public void SalvarString(string k, string v) { GarantirDadosCarregados(); cacheString[k] = v ?? ""; }
    public string ObterString(string k) { GarantirDadosCarregados(); return cacheString.TryGetValue(k, out string v) ? v : ""; }

    public bool TemFloat(string k)
    {
        GarantirDadosCarregados();
        return cacheFloat.ContainsKey(k);
    }

    public bool TemEstadoSalvo(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        GarantirDadosCarregados();
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

    public bool ObterEstado(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;

        GarantirDadosCarregados();

        if (cacheInt.TryGetValue(id + "_Active", out int v))
            return v == 1;

        return estadosObjetosRAM.ContainsKey(id) && estadosObjetosRAM[id];
    }

    // =========================
    // RESTORE BACKUP
    // =========================
    public void RestaurarBackup(int slot)
    {
        string path = Path.Combine(DiretorioSaves, $"Save_Slot_{slot}.json");
        string backup = path + ".bak";

        if (!File.Exists(backup))
        {
            Debug.LogWarning("[SAVE] Backup não encontrado.");
            return;
        }

        try
        {
            File.Copy(backup, path, true);
            slotCarregado = -1;
            ResetarDicionarios();
            CarregarDoDisco(slot);
        }
        catch (Exception e)
        {
            Debug.LogError("[SAVE] Erro ao restaurar backup: " + e.Message);
        }
    }

    public void SalvarTransform(string id, Transform t)
    {
        SalvarFloat(id + "_x", t.position.x);
        SalvarFloat(id + "_y", t.position.y);
        SalvarFloat(id + "_z", t.position.z);
    }

    public void CarregarTransform(string id, Transform t)
    {
        if (cacheFloat.ContainsKey(id + "_x"))
        {
            t.position = new Vector3(
                ObterFloat(id + "_x"),
                ObterFloat(id + "_y"),
                ObterFloat(id + "_z")
            );
        }
    }
    
    // =========================
    // COMPATIBILIDADE COM SCRIPTS ANTIGOS
    // =========================
    public bool CarregarEstadoObjeto(string id, bool valorPadrao)
    {
        if (!TemEstadoSalvo(id)) return valorPadrao;
        return ObterEstado(id);
    }

    public bool CarregarEstadoObjeto(string id)
    {
        return ObterEstado(id);
    }

    public void LimparDicionario()
    {
        slotCarregado = -1; // Força re-leitura do disco se precisar
        ResetarDicionarios();
    }
}
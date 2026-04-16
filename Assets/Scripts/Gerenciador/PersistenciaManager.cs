using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections; 

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

    [Header("--- DEV MODE ---")]
    [Tooltip("Se marcado, IGNORA gravação no HD durante o Play na Unity para não corromper saves oficiais.")]
    public bool desativarSaveNoEditor = false; 

    private Dictionary<string, bool> estadosObjetosRAM = new Dictionary<string, bool>();
    private Dictionary<string, int> cacheInt = new Dictionary<string, int>();
    private Dictionary<string, float> cacheFloat = new Dictionary<string, float>();
    private Dictionary<string, string> cacheString = new Dictionary<string, string>();

    private DadosDeSave dadosParaSerializar = new DadosDeSave();

    private int slotCarregado = -1; 
    private bool inicializado = false;
    private float ultimoSaveTempo = 0f;
    private float intervaloMinimoSave = 2f;

    // 🔥 TRAVA DE SEGURANÇA: Avisa o resto do jogo se a leitura de disco terminou.
    public bool DadosProntosParaUso { get; private set; } = false;

    public string DiretorioSaves => Path.Combine(Application.persistentDataPath, "Saves");

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
            
            if (!Directory.Exists(DiretorioSaves)) 
                Directory.CreateDirectory(DiretorioSaves);
                
            inicializado = true;
            StartCoroutine(RotinaAutoSave());
        }
        else Destroy(gameObject);
    }

    private IEnumerator RotinaAutoSave()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(300f);
            if (inicializado && slotCarregado != -1 && !ModoSemSave()) SalvarTudo();
        }
    }

    void OnApplicationQuit() { if (slotCarregado != -1) SalvarTudo(); }

    void OnApplicationFocus(bool focus)
    {
        if (!focus && Time.unscaledTime - ultimoSaveTempo > intervaloMinimoSave && slotCarregado != -1)
        {
            SalvarTudo();
            ultimoSaveTempo = Time.unscaledTime;
        }
    }

    public bool ModoSemSave() => SistemaGlobal.Instance == null || SistemaGlobal.Instance.slotAtual <= 0;

    private void GarantirListasValidas()
    {
        if (dadosParaSerializar.ints == null) dadosParaSerializar.ints = new List<IntEntry>();
        if (dadosParaSerializar.floats == null) dadosParaSerializar.floats = new List<FloatEntry>();
        if (dadosParaSerializar.strings == null) dadosParaSerializar.strings = new List<StringEntry>();
    }

    public void IniciarNovoJogo(int slot)
    {
        ResetarDicionarios();
        slotCarregado = slot; 
        DadosProntosParaUso = true; 
        Debug.Log($"[SAVE] Iniciado NOVO JOGO no Slot {slot}. RAM limpa.");
    }

    public void CarregarDoDisco(int slot)
    {
        DadosProntosParaUso = false;
        slotCarregado = slot;

        #if UNITY_EDITOR
        if (desativarSaveNoEditor)
        {
            ResetarDicionarios();
            DadosProntosParaUso = true;
            return;
        }
        #endif

        string caminho = Path.Combine(DiretorioSaves, $"Save_Slot_{slot}.json");

        if (!File.Exists(caminho)) 
        { 
            ResetarDicionarios(); 
            DadosProntosParaUso = true;
            return; 
        }

        try
        {
            string json = File.ReadAllText(caminho);
            dadosParaSerializar = JsonUtility.FromJson<DadosDeSave>(json);
            
            if (dadosParaSerializar == null) { ResetarDicionarios(); }
            else 
            {
                GarantirListasValidas();
                SincronizarListasParaCache();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[SAVE] Erro crítico ao ler JSON: " + e.Message + " | Tentando Backup.");
            TentarBackup(slot);
        }
        finally
        {
            DadosProntosParaUso = true;
            Debug.Log($"[SAVE] Carregamento do Slot {slot} finalizado na RAM.");
        }
    }

    private void TentarBackup(int slot)
    {
        string caminho = Path.Combine(DiretorioSaves, $"Save_Slot_{slot}.json.bak");
        if (!File.Exists(caminho)) { ResetarDicionarios(); return; }
        try
        {
            dadosParaSerializar = JsonUtility.FromJson<DadosDeSave>(File.ReadAllText(caminho));
            GarantirListasValidas();
            SincronizarListasParaCache();
        }
        catch { ResetarDicionarios(); }
    }

    private void SincronizarListasParaCache()
    {
        estadosObjetosRAM.Clear(); 
        cacheInt.Clear(); foreach (var e in dadosParaSerializar.ints) cacheInt[e.k] = e.v;
        cacheFloat.Clear(); foreach (var e in dadosParaSerializar.floats) cacheFloat[e.k] = e.v;
        cacheString.Clear(); foreach (var e in dadosParaSerializar.strings) cacheString[e.k] = e.v;
    }

    public void ResetarDicionarios()
    {
        cacheInt.Clear(); cacheFloat.Clear(); cacheString.Clear(); estadosObjetosRAM.Clear();
        dadosParaSerializar = new DadosDeSave(); GarantirListasValidas();
    }

    public void LimparDicionario()
    {
        slotCarregado = -1;
        DadosProntosParaUso = false;
        ResetarDicionarios();
    }

    public void SalvarTudo()
    {
        #if UNITY_EDITOR
        if (desativarSaveNoEditor) return;
        #endif

        if (!inicializado || ModoSemSave() || slotCarregado == -1) return;
        int slot = SistemaGlobal.Instance.slotAtual;
        if (slot <= 0) return;

        foreach (var kvp in SaveableItem.registroGlobal)
        {
            if (kvp.Value != null)
            {
                RegistrarEstado(kvp.Key, kvp.Value.gameObject.activeSelf);
                if (kvp.Value.gameObject.activeSelf) 
                {
                    SalvarTransform(kvp.Key, kvp.Value.transform);
                }
            }
        }

        try
        {
            dadosParaSerializar.ints.Clear(); foreach (var kv in cacheInt) dadosParaSerializar.ints.Add(new IntEntry { k = kv.Key, v = kv.Value });
            dadosParaSerializar.floats.Clear(); foreach (var kv in cacheFloat) dadosParaSerializar.floats.Add(new FloatEntry { k = kv.Key, v = kv.Value });
            dadosParaSerializar.strings.Clear(); foreach (var kv in cacheString) dadosParaSerializar.strings.Add(new StringEntry { k = kv.Key, v = kv.Value });

            string json = JsonUtility.ToJson(dadosParaSerializar, true);
            string caminho = Path.Combine(DiretorioSaves, $"Save_Slot_{slot}.json");

            if (File.Exists(caminho)) File.Copy(caminho, caminho + ".bak", true);
            File.WriteAllText(caminho, json);
        }
        catch (Exception e) { Debug.LogError("[SAVE] Erro ao escrever no disco: " + e.Message); }
    }

    public void SalvarInt(string k, int v) { cacheInt[k] = v; }
    public int ObterInt(string k, int padrao = 0) { return cacheInt.TryGetValue(k, out int v) ? v : padrao; }
    
    public void SalvarFloat(string k, float v) { cacheFloat[k] = v; }
    public float ObterFloat(string k, float padrao = 0f) { return cacheFloat.TryGetValue(k, out float v) ? v : padrao; }
    
    public void SalvarString(string k, string v) { cacheString[k] = v ?? ""; }
    public string ObterString(string k, string padrao = "") { return cacheString.TryGetValue(k, out string v) ? v : padrao; }
    
    public bool TemFloat(string k) { return cacheFloat.ContainsKey(k); }

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
        SalvarFloat(id + "_px", t.position.x); SalvarFloat(id + "_py", t.position.y); SalvarFloat(id + "_pz", t.position.z);
        SalvarFloat(id + "_rx", t.eulerAngles.x); SalvarFloat(id + "_ry", t.eulerAngles.y); SalvarFloat(id + "_rz", t.eulerAngles.z);
    }

    public void CarregarTransform(string id, Transform t)
    {
        if (cacheFloat.ContainsKey(id + "_px"))
        {
            t.position = new Vector3(ObterFloat(id + "_px"), ObterFloat(id + "_py"), ObterFloat(id + "_pz"));
            t.eulerAngles = new Vector3(ObterFloat(id + "_rx"), ObterFloat(id + "_ry"), ObterFloat(id + "_rz"));
        }
    }

    public void RestaurarBackup(int slot)
    {
        string path = Path.Combine(DiretorioSaves, $"Save_Slot_{slot}.json");
        string backup = path + ".bak";
        if (!File.Exists(backup)) return;

        try
        {
            File.Copy(backup, path, true);
            LimparDicionario(); 
            CarregarDoDisco(slot); 
        }
        catch (Exception e) { Debug.LogError("[SAVE] Falha ao restaurar: " + e.Message); }
    }

    // 🔥 AS FUNÇÕES RESTAURADAS 🔥
    public bool CarregarEstadoObjeto(string id, bool valorPadrao) { return ObterEstado(id, valorPadrao); }
    public bool CarregarEstadoObjeto(string id) { return ObterEstado(id, false); }
}
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.SceneManagement; // Adicionado para lidar com a cena do jogador

[System.Serializable]
public class DadosDeSave
{
    public List<string> chavesInt = new List<string>();
    public List<int> valoresInt = new List<int>();

    public List<string> chavesFloat = new List<string>();
    public List<float> valoresFloat = new List<float>();
    
    public List<string> chavesString = new List<string>(); // Novo: Para salvar o nome da cena!
    public List<string> valoresString = new List<string>();
}

public class PersistenciaManager : MonoBehaviour
{
    public static PersistenciaManager Instance;

    private Dictionary<string, bool> estadosObjetosRAM = new Dictionary<string, bool>();

    private DadosDeSave dadosAtuais = new DadosDeSave();
    private int slotCarregado = -1;
    
    public string DiretorioSaves => Application.persistentDataPath + "/Saves";

    public bool ModoSemSave()
    {
        if (SistemaGlobal.Instance == null) return true;
        if (SistemaGlobal.Instance.slotAtual <= 0) return true;
        return false;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
            
            if (!Directory.Exists(DiretorioSaves))
            {
                Directory.CreateDirectory(DiretorioSaves);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private string GetCaminhoSave(int slot) 
    { 
        return DiretorioSaves + $"/SaveData_Slot_{slot}.save"; 
    }
    
    private string GetCaminhoBackup(int slot) 
    { 
        return DiretorioSaves + $"/SaveData_Slot_{slot}_Backup.save"; 
    }

    private void GarantirDadosCarregados()
    {
        if (ModoSemSave()) return;
        
        int slot = SistemaGlobal.Instance.slotAtual;
        if (slotCarregado != slot)
        {
            slotCarregado = slot;
            CarregarDoDisco(slot);
        }
    }

    private void CarregarDoDisco(int slot)
    {
        string caminho = GetCaminhoSave(slot);
        if (File.Exists(caminho))
        {
            string json = File.ReadAllText(caminho);
            dadosAtuais = JsonUtility.FromJson<DadosDeSave>(json);
        }
        else
        {
            dadosAtuais = new DadosDeSave();
        }
    }

    // --- TIPOS BÁSICOS DE SAVE ---
    private void SalvarInt(string chave, int valor)
    {
        GarantirDadosCarregados();
        int idx = dadosAtuais.chavesInt.IndexOf(chave);
        if (idx >= 0) dadosAtuais.valoresInt[idx] = valor;
        else { dadosAtuais.chavesInt.Add(chave); dadosAtuais.valoresInt.Add(valor); }
    }

    private bool TemInt(string chave) { GarantirDadosCarregados(); return dadosAtuais.chavesInt.Contains(chave); }
    private int ObterInt(string chave) { GarantirDadosCarregados(); int idx = dadosAtuais.chavesInt.IndexOf(chave); return idx >= 0 ? dadosAtuais.valoresInt[idx] : 0; }

    private void SalvarFloat(string chave, float valor)
    {
        GarantirDadosCarregados();
        int idx = dadosAtuais.chavesFloat.IndexOf(chave);
        if (idx >= 0) dadosAtuais.valoresFloat[idx] = valor;
        else { dadosAtuais.chavesFloat.Add(chave); dadosAtuais.valoresFloat.Add(valor); }
    }

    private bool TemFloat(string chave) { GarantirDadosCarregados(); return dadosAtuais.chavesFloat.Contains(chave); }
    private float ObterFloat(string chave) { GarantirDadosCarregados(); int idx = dadosAtuais.chavesFloat.IndexOf(chave); return idx >= 0 ? dadosAtuais.valoresFloat[idx] : 0f; }

    private void SalvarString(string chave, string valor)
    {
        GarantirDadosCarregados();
        int idx = dadosAtuais.chavesString.IndexOf(chave);
        if (idx >= 0) dadosAtuais.valoresString[idx] = valor;
        else { dadosAtuais.chavesString.Add(chave); dadosAtuais.valoresString.Add(valor); }
    }

    public bool TemString(string chave) { GarantirDadosCarregados(); return dadosAtuais.chavesString.Contains(chave); }
    public string ObterString(string chave) { GarantirDadosCarregados(); int idx = dadosAtuais.chavesString.IndexOf(chave); return idx >= 0 ? dadosAtuais.valoresString[idx] : ""; }

    // --- LÓGICA DE OBJETOS E TRANSFORMS ---
    public void RegistrarEstado(string id, bool estado)
    {
        if (string.IsNullOrEmpty(id)) return;
        
        if (estadosObjetosRAM.ContainsKey(id)) estadosObjetosRAM[id] = estado;
        else estadosObjetosRAM.Add(id, estado);
        
        if (ModoSemSave()) return;
        
        SalvarInt(GetChave(id, "Active"), estado ? 1 : 0);
    }

    public bool ObterEstado(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        
        if (!ModoSemSave())
        {
            string chave = GetChave(id, "Active");
            if (TemInt(chave)) return ObterInt(chave) == 1;
        }
        
        return estadosObjetosRAM.ContainsKey(id) && estadosObjetosRAM[id];
    }

    public bool CarregarEstadoObjeto(string id, bool valorPadrao)
    {
        if (string.IsNullOrEmpty(id)) return valorPadrao;
        if (ModoSemSave()) return valorPadrao;
        
        string chave = GetChave(id, "Active");
        if (TemInt(chave)) return ObterInt(chave) == 1;
        
        return valorPadrao;
    }

    public void SalvarTransform(string id, Transform t)
    {
        if (ModoSemSave()) return;
        
        string chave = GetChave(id, "Pos");
        SalvarFloat(chave + "X", t.position.x);
        SalvarFloat(chave + "Y", t.position.y);
        SalvarFloat(chave + "Z", t.position.z);
        SalvarFloat(chave + "RotX", t.rotation.eulerAngles.x);
        SalvarFloat(chave + "RotY", t.rotation.eulerAngles.y);
        SalvarFloat(chave + "RotZ", t.rotation.eulerAngles.z);
        
        // NOVO: Toda vez que salva um transform, salva a cena em que ele tá!
        SalvarString(chave + "Cena", SceneManager.GetActiveScene().name); 
    }

    public void CarregarTransform(string id, Transform t)
    {
        if (ModoSemSave()) return;
        
        string chave = GetChave(id, "Pos");
        if (!TemFloat(chave + "X")) return;
        
        Vector3 pos = new Vector3(ObterFloat(chave + "X"), ObterFloat(chave + "Y"), ObterFloat(chave + "Z"));
        Vector3 rot = new Vector3(ObterFloat(chave + "RotX"), ObterFloat(chave + "RotY"), ObterFloat(chave + "RotZ"));
        
        // Teleporte seguro cravado usando a função do próprio FPS_Master pra não brigar com física
        if (t.CompareTag("Player") && FPS_Master.Instance != null)
        {
            FPS_Master.Instance.Teleportar(pos);
            t.rotation = Quaternion.Euler(rot);
        }
        else
        {
            t.position = pos;
            t.rotation = Quaternion.Euler(rot);
        }
    }

    private string GetChave(string id, string tipo)
    {
        int slot = SistemaGlobal.Instance.slotAtual;
        return $"Slot_{slot}_{id}_{tipo}";
    }

    public void SalvarTudo()
    {
        if (ModoSemSave()) return;
        
        GarantirDadosCarregados();
        
        int slot = SistemaGlobal.Instance.slotAtual;
        string caminhoOriginal = GetCaminhoSave(slot);
        string caminhoBackup = GetCaminhoBackup(slot);

        if (File.Exists(caminhoOriginal))
        {
            File.Copy(caminhoOriginal, caminhoBackup, true);
            string dataAtual = PlayerPrefs.GetString($"Slot_{slot}_Data", "");
            PlayerPrefs.SetString($"Slot_{slot}_Backup_Data", dataAtual);
        }

        string json = JsonUtility.ToJson(dadosAtuais, true); 
        File.WriteAllText(caminhoOriginal, json);

        string novaData = System.DateTime.Now.ToString("dd/MM HH:mm");
        PlayerPrefs.SetString($"Slot_{slot}_Data", novaData);
        PlayerPrefs.SetInt($"Slot_{slot}_SaveExistente", 1);
        
        PlayerPrefs.Save();
        
        if (GameManager.Instance != null) 
        {
            GameManager.Instance.SalvarProgresso();
        }
    }

    public void RestaurarBackup(int slot)
    {
        string caminhoOriginal = GetCaminhoSave(slot);
        string caminhoBackup = GetCaminhoBackup(slot);
        string caminhoTemp = DiretorioSaves + $"/SaveData_Slot_{slot}_Temp.save";

        if (File.Exists(caminhoBackup))
        {
            if (File.Exists(caminhoTemp)) File.Delete(caminhoTemp);
            if (File.Exists(caminhoOriginal)) File.Move(caminhoOriginal, caminhoTemp); 
            File.Move(caminhoBackup, caminhoOriginal); 
            if (File.Exists(caminhoTemp)) File.Move(caminhoTemp, caminhoBackup); 

            string dataAtual = PlayerPrefs.GetString($"Slot_{slot}_Data", "");
            string dataBackup = PlayerPrefs.GetString($"Slot_{slot}_Backup_Data", "");
            
            PlayerPrefs.SetString($"Slot_{slot}_Data", dataBackup);
            PlayerPrefs.SetString($"Slot_{slot}_Backup_Data", dataAtual);
            PlayerPrefs.Save();

            Debug.Log($"[SAVE] Backup do slot {slot} restaurado e datas trocadas!");

            if (!ModoSemSave() && SistemaGlobal.Instance != null && SistemaGlobal.Instance.slotAtual == slot)
            {
                CarregarDoDisco(slot);
            }
        }
    }

    public void LimparDicionario() 
    { 
        estadosObjetosRAM.Clear(); 
    }
}
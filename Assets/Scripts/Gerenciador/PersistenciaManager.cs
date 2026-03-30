using UnityEngine;
using System.Collections.Generic;

public class PersistenciaManager : MonoBehaviour
{
    public static PersistenciaManager Instance;

    private Dictionary<string, bool> estadosObjetos = new Dictionary<string, bool>();

    // --- A INTELIGÊNCIA QUE SALVA A SUA VIDA NO EDITOR ---
    public bool ModoSemSave()
    {
        // Se deu Play direto na cena (SistemaGlobal não existe)
        if (SistemaGlobal.Instance == null) return true;
        
        // Se existe, mas o jogador não escolheu nenhum slot válido (0 ou -1)
        if (SistemaGlobal.Instance.slotAtual <= 0) return true;
        
        return false;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // --- ESTADO (Ativo/Inativo) ---
    public void RegistrarEstado(string id, bool estado)
    {
        if (string.IsNullOrEmpty(id)) return;
        
        if (estadosObjetos.ContainsKey(id)) estadosObjetos[id] = estado;
        else estadosObjetos.Add(id, estado);

        // Se for teste isolado, salva só na RAM e foge antes de gravar no HD
        if (ModoSemSave()) return;

        SalvarInt(GetChave(id, "Active"), estado ? 1 : 0);
        SalvarTudo();
    }

    public bool ObterEstado(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        
        if (!ModoSemSave())
        {
            string chave = GetChave(id, "Active");
            if (PlayerPrefs.HasKey(chave)) return PlayerPrefs.GetInt(chave) == 1;
        }
        
        return estadosObjetos.ContainsKey(id) && estadosObjetos[id];
    }

    public bool CarregarEstadoObjeto(string id, bool valorPadrao)
    {
        if (string.IsNullOrEmpty(id)) return valorPadrao;
        
        // Se for teste isolado, ignora o PlayerPrefs e devolve o valor original do mapa
        if (ModoSemSave()) return valorPadrao;

        string chave = GetChave(id, "Active");
        
        if (PlayerPrefs.HasKey(chave)) 
            return PlayerPrefs.GetInt(chave) == 1;
        
        return valorPadrao;
    }

    // --- POSIÇÃO E ROTAÇÃO ---
    public void SalvarTransform(string id, Transform t)
    {
        if (ModoSemSave()) return;

        string chave = GetChave(id, "Pos");
        PlayerPrefs.SetFloat(chave + "X", t.position.x);
        PlayerPrefs.SetFloat(chave + "Y", t.position.y);
        PlayerPrefs.SetFloat(chave + "Z", t.position.z);
        PlayerPrefs.SetFloat(chave + "RotX", t.rotation.eulerAngles.x);
        PlayerPrefs.SetFloat(chave + "RotY", t.rotation.eulerAngles.y);
        PlayerPrefs.SetFloat(chave + "RotZ", t.rotation.eulerAngles.z);
        SalvarTudo();
    }

    public void CarregarTransform(string id, Transform t)
    {
        if (ModoSemSave()) return;

        string chave = GetChave(id, "Pos");
        if (!PlayerPrefs.HasKey(chave + "X")) return;

        Vector3 pos = new Vector3(
            PlayerPrefs.GetFloat(chave + "X"),
            PlayerPrefs.GetFloat(chave + "Y"),
            PlayerPrefs.GetFloat(chave + "Z")
        );
        Vector3 rot = new Vector3(
            PlayerPrefs.GetFloat(chave + "RotX"),
            PlayerPrefs.GetFloat(chave + "RotY"),
            PlayerPrefs.GetFloat(chave + "RotZ")
        );

        CharacterController cc = t.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        t.position = pos;
        t.rotation = Quaternion.Euler(rot);
        if (cc) cc.enabled = true;
    }

    private string GetChave(string id, string tipo)
    {
        // Agora ele tem certeza que só vai rodar isso se o slot for real
        int slot = SistemaGlobal.Instance.slotAtual;
        return $"Slot_{slot}_{id}_{tipo}";
    }

    private void SalvarInt(string chave, int valor) { PlayerPrefs.SetInt(chave, valor); }

    public void SalvarTudo()
    {
        if (ModoSemSave()) return;

        PlayerPrefs.Save();
        if (GameManager.Instance != null) GameManager.Instance.SalvarProgresso();
    }

    public void LimparDicionario() { estadosObjetos.Clear(); }
}
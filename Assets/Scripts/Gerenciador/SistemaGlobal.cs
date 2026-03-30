using UnityEngine;
using UnityEngine.SceneManagement;

public class SistemaGlobal : MonoBehaviour
{
    public static SistemaGlobal Instance;

    [Header("CONFIGURAÇÃO GERAL")]
    public string nomeCenaPadrao = "DreamSceane"; 
    
    [Header("ESTADO DO SAVE")]
    public int slotAtual = 1; 
    
    // Flag importante para o InterfaceManager
    [HideInInspector] public bool deveCarregarPosicaoAoIniciar = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (transform.parent == null) DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SalvarJogo(Vector3 posicaoPlayer, string nomeCena)
    {
        string prefixo = "Slot_" + slotAtual;
        PlayerPrefs.SetInt(prefixo + "_Existe", 1);
        PlayerPrefs.SetString(prefixo + "_Cena", nomeCena);
        
        PlayerPrefs.SetFloat(prefixo + "_PosX", posicaoPlayer.x);
        PlayerPrefs.SetFloat(prefixo + "_PosY", posicaoPlayer.y);
        PlayerPrefs.SetFloat(prefixo + "_PosZ", posicaoPlayer.z);
        
        PlayerPrefs.SetString(prefixo + "_Data", System.DateTime.Now.ToString("dd/MM HH:mm"));

        EstadoGlobal.SalvarNoSlot(slotAtual);
        
        PlayerPrefs.Save();
        Debug.Log("Jogo Salvo no Slot " + slotAtual);
    }

    public void CarregarJogo(int slot)
    {
        slotAtual = slot;
        string prefixo = "Slot_" + slot;

        if (PlayerPrefs.HasKey(prefixo + "_Existe"))
        {
            EstadoGlobal.CarregarDoSlot(slot);
            deveCarregarPosicaoAoIniciar = true; // ATIVA O TELEPORTE
            
            string cenaParaCarregar = PlayerPrefs.GetString(prefixo + "_Cena");
            SceneManager.LoadScene(cenaParaCarregar);
        }
        else
        {
            EstadoGlobal.ResetarTudo();
            // Limpa persistência de objetos da sessão anterior
            if(PersistenciaManager.Instance) PersistenciaManager.Instance.LimparDicionario();
            
            deveCarregarPosicaoAoIniciar = false; // NÃO TELEPORTA (Novo Jogo)
            
            if (!string.IsNullOrEmpty(nomeCenaPadrao))
                SceneManager.LoadScene(nomeCenaPadrao);
            else
                Debug.LogError("ERRO: Nome da cena padrão vazio no SistemaGlobal!");
        }
    }

    public void ApagarSave(int slot)
    {
        string prefixo = "Slot_" + slot;
        PlayerPrefs.DeleteKey(prefixo + "_Existe");
        PlayerPrefs.DeleteKey(prefixo + "_Cena");
        PlayerPrefs.DeleteKey(prefixo + "_Data");
        PlayerPrefs.DeleteKey(prefixo + "_PosX");
        PlayerPrefs.DeleteKey(prefixo + "_PosY");
        PlayerPrefs.DeleteKey(prefixo + "_PosZ");
        
        EstadoGlobal.ResetarTudo();
        
        string p = "Slot_" + slot + "_Global_";
        for (int i = 0; i < 10; i++) {
            PlayerPrefs.DeleteKey(p + "Arma_" + i);
            PlayerPrefs.DeleteKey(p + "Casa_" + i);
        }
        PlayerPrefs.DeleteKey(p + "TemChave");
        PlayerPrefs.DeleteKey(p + "TemDiscoRuna");

        // Limpa objetos salvos deste slot também
        // (Aqui é um pouco mais complexo limpar chaves dinâmicas, mas o básico tá feito)

        PlayerPrefs.Save();
    }

    public bool ExisteSave(int slot) { return PlayerPrefs.HasKey("Slot_" + slot + "_Existe"); }
    public string GetDataSave(int slot) { return PlayerPrefs.GetString("Slot_" + slot + "_Data", "Vazio"); }
}
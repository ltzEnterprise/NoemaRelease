using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

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
        if (PersistenciaManager.Instance == null) return;

        string prefixo = "Slot_" + slotAtual;
        
        // Tudo vai pro JSON agora!
        PersistenciaManager.Instance.SalvarString(prefixo + "_Cena", nomeCena);
        PersistenciaManager.Instance.SalvarFloat(prefixo + "_PosX", posicaoPlayer.x);
        PersistenciaManager.Instance.SalvarFloat(prefixo + "_PosY", posicaoPlayer.y);
        PersistenciaManager.Instance.SalvarFloat(prefixo + "_PosZ", posicaoPlayer.z);

        // Atualiza os dados do EstadoGlobal antes de fechar o pacote
        EstadoGlobal.SalvarNoSlot(slotAtual);
        
        // O PersistenciaManager pega tudo isso e crava no arquivo físico
        PersistenciaManager.Instance.SalvarTudo();
        
        Debug.Log("<color=cyan>[SistemaGlobal] Jogo Salvo 100% no arquivo JSON (Slot " + slotAtual + ")</color>");
    }

    public void CarregarJogo(int slot)
    {
        slotAtual = slot;

        if (ExisteSave(slot))
        {
            // Força a limpeza da RAM pra ler o arquivo certinho
            if(PersistenciaManager.Instance) PersistenciaManager.Instance.LimparDicionario(); 
            
            EstadoGlobal.CarregarDoSlot(slot);
            deveCarregarPosicaoAoIniciar = true; // ATIVA O TELEPORTE
            
            string cenaParaCarregar = PersistenciaManager.Instance.ObterString("Slot_" + slot + "_Cena");
            if (string.IsNullOrEmpty(cenaParaCarregar)) cenaParaCarregar = nomeCenaPadrao;
            
            SceneManager.LoadScene(cenaParaCarregar);
        }
        else
        {
            EstadoGlobal.ResetarTudo();
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
        // Deleta os arquivos diretos do HD! Sem laço de repetição escroto.
        string caminho = Path.Combine(Application.persistentDataPath, "Saves", $"Save_Slot_{slot}.json");
        string caminhoBak = caminho + ".bak";

        if (File.Exists(caminho)) File.Delete(caminho);
        if (File.Exists(caminhoBak)) File.Delete(caminhoBak);

        EstadoGlobal.ResetarTudo();
        
        // Se apagou o save que tava jogando, limpa a memória
        if (slotAtual == slot && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.LimparDicionario();
        }

        Debug.Log($"[SistemaGlobal] Save do Slot {slot} pulverizado do HD.");
    }

    public bool ExisteSave(int slot) 
    { 
        // Vê fisicamente se o arquivo tá lá
        string caminho = Path.Combine(Application.persistentDataPath, "Saves", $"Save_Slot_{slot}.json");
        return File.Exists(caminho); 
    }

    public string GetDataSave(int slot) 
    { 
        // Em vez de salvar a data num texto e dar trabalho pra ler, eu puxo a data de modificação real do arquivo pelo Windows!
        string caminho = Path.Combine(Application.persistentDataPath, "Saves", $"Save_Slot_{slot}.json");
        if (File.Exists(caminho))
        {
            return File.GetLastWriteTime(caminho).ToString("dd/MM HH:mm");
        }
        return "Vazio";
    }
}
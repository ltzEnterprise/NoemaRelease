using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.SceneManagement; 

[System.Serializable]
public class SlotConfigRuna
{
    public string nomeIdentificador;
    public RunaData data;
    public UnityEvent eventoParaColetar; 
}

public class InventarioRunas : MonoBehaviour
{
    public static InventarioRunas Instance;

    [Header("Configuração das Runas")]
    public List<SlotConfigRuna> configuracaoRunas = new List<SlotConfigRuna>();

    [Header("Runas Atualmente Coletadas")]
    public List<RunaData> runasNaMao = new List<RunaData>();

    [HideInInspector] public Vector3 ultimaPosicaoSalva;
    [HideInInspector] public bool deveCarregarPosicao = false;

    private void Awake() 
    { 
        if (Instance == null) 
        {
            Instance = this;
            
            // Tira o objeto de dentro de qualquer "pai" e joga ele na raiz da cena
            transform.SetParent(null); 
            
            DontDestroyOnLoad(gameObject); 
            SceneManager.sceneLoaded += OnSceneLoaded;
        } 
        else Destroy(gameObject);
    }

    private void Start()
    {
        CarregarRunasDoSave();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RedesenharRunasNaTela());
    }

    System.Collections.IEnumerator RedesenharRunasNaTela()
    {
        yield return null; 
        
        foreach (var runa in runasNaMao)
        {
            AtualizarUI(runa.icone);
        }
    }

    // CARREGA AS RUNAS QUANDO O JOGO ABRE
    private void CarregarRunasDoSave()
    {
        if (Application.isEditor || PersistenciaManager.Instance == null) return;

        foreach (var slot in configuracaoRunas)
        {
            // Lê do PersistenciaManager se a runa tá lá
            bool temRuna = PersistenciaManager.Instance.ObterEstado("Runa_" + slot.nomeIdentificador);
            if (temRuna && !runasNaMao.Contains(slot.data))
            {
                runasNaMao.Add(slot.data);
            }
        }
        StartCoroutine(RedesenharRunasNaTela());
    }

    public void SalvarPosicaoAtual(Vector3 posicao)
    {
        ultimaPosicaoSalva = posicao;
        deveCarregarPosicao = true;
    }

    public void ColetarRunaPeloNome(string nome)
    {
        SlotConfigRuna slot = configuracaoRunas.Find(x => x.nomeIdentificador == nome);
        
        if (slot != null)
        {
            if (!runasNaMao.Contains(slot.data))
            {
                runasNaMao.Add(slot.data);
                if (slot.eventoParaColetar != null) slot.eventoParaColetar.Invoke();
                
                AtualizarUI(slot.data.icone);

                // --- SALVA A RUNA IMEDIATAMENTE NO DISCO ---
                if (!Application.isEditor && PersistenciaManager.Instance != null)
                {
                    PersistenciaManager.Instance.RegistrarEstado("Runa_" + nome, true);
                    PersistenciaManager.Instance.SalvarTudo();
                }

                if (runasNaMao.Count == 3 && DayNightCycle.Instance != null)
                {
                    DayNightCycle.Instance.ChangeTo(DayNightCycle.TimeState.DramaticDay); 
                }
            }
        }
    }

    void AtualizarUI(Sprite icone)
    {
        if (AreaDasRunas.Instance != null) AreaDasRunas.Instance.AdicionarRunaNaTela(icone);
    }

    public bool TemARunaPeloNome(string nome)
    {
        SlotConfigRuna slot = configuracaoRunas.Find(x => x.nomeIdentificador == nome);
        return slot != null && runasNaMao.Contains(slot.data);
    }
    
    public void UsarTodasAsRunasNoAltar()
    {
        runasNaMao.Clear();
        if (AreaDasRunas.Instance != null) AreaDasRunas.Instance.LimparTodasAsRunasDaTela();

        // --- ZERA AS RUNAS DO SAVE ---
        if (!Application.isEditor && PersistenciaManager.Instance != null)
        {
            foreach (var slot in configuracaoRunas)
            {
                PersistenciaManager.Instance.RegistrarEstado("Runa_" + slot.nomeIdentificador, false);
            }
            PersistenciaManager.Instance.SalvarTudo();
        }
    }
}
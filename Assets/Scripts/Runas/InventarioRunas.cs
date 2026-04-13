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

    [Header("--- CONTROLE DE CENAS ---")]
    [Tooltip("Nome da cena 2D onde as runas NÃO devem aparecer na tela.")]
    public string nomeDaCena2D = "Mundo2D";

    [Header("Runas Atualmente Coletadas")]
    public List<RunaData> runasNaMao = new List<RunaData>();

    [HideInInspector] public Vector3 ultimaPosicaoSalva;
    [HideInInspector] public bool deveCarregarPosicao = false;

    private void Awake() 
    { 
        if (Instance == null) 
        {
            Instance = this;
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
        // 🔥 A MÁGICA TÁ AQUI: Se for a cena 2D, aborta o desenho da UI.
        // As runas continuam salvas na lista 'runasNaMao', só não vão pra tela.
        if (scene.name == nomeDaCena2D) return;

        // Se for a cena 3D, desenha tudo que tá guardado na memória.
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
            bool temRuna = PersistenciaManager.Instance.ObterEstado("Runa_" + slot.nomeIdentificador);
            if (temRuna && !runasNaMao.Contains(slot.data))
            {
                runasNaMao.Add(slot.data);
            }
        }

        // Também protege no Start caso o jogo comece direto no 2D por algum motivo
        if (SceneManager.GetActiveScene().name != nomeDaCena2D)
        {
            StartCoroutine(RedesenharRunasNaTela());
        }
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
                
                // Só desenha a UI nova se não estiver no mundo 2D
                if (SceneManager.GetActiveScene().name != nomeDaCena2D)
                {
                    AtualizarUI(slot.data.icone);
                }

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
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

    public void RecarregarDoSave()
    {
        runasNaMao.Clear();
        CarregarRunasDoSave();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == nomeDaCena2D) return;
        StartCoroutine(RedesenharRunasNaTela());
    }

    System.Collections.IEnumerator RedesenharRunasNaTela()
    {
        yield return null; 
        
        if (AreaDasRunas.Instance != null) 
            AreaDasRunas.Instance.LimparTodasAsRunasDaTela();

        foreach (var runa in runasNaMao)
        {
            AtualizarUI(runa.icone);
        }
    }

    private void CarregarRunasDoSave()
    {
        if (Application.isEditor || PersistenciaManager.Instance == null) return;

        foreach (var slot in configuracaoRunas)
        {
            bool temRuna = PersistenciaManager.Instance.ObterEstado("Runa_" + slot.nomeIdentificador, false);
            if (temRuna && !runasNaMao.Contains(slot.data))
            {
                runasNaMao.Add(slot.data);
            }
        }

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
                
                if (SceneManager.GetActiveScene().name != nomeDaCena2D)
                {
                    AtualizarUI(slot.data.icone);
                }

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

    // 🔥 A FUNÇÃO SECRETA: Registra a runa na RAM, mas NÃO SALVA no HD. Proteção contra Softlock!
    public void ColetarRunaSemForcarSaveHD(string nome)
    {
        SlotConfigRuna slot = configuracaoRunas.Find(x => x.nomeIdentificador == nome);
        
        if (slot != null)
        {
            if (!runasNaMao.Contains(slot.data))
            {
                runasNaMao.Add(slot.data);
                if (slot.eventoParaColetar != null) slot.eventoParaColetar.Invoke();
                
                if (SceneManager.GetActiveScene().name != nomeDaCena2D)
                {
                    AtualizarUI(slot.data.icone);
                }

                if (!Application.isEditor && PersistenciaManager.Instance != null)
                {
                    // Fica só na RAM temporária. Se fechar o jogo, a runa volta pro mapa.
                    PersistenciaManager.Instance.RegistrarEstado("Runa_" + nome, true);
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
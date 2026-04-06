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
            
            // --- A MÁGICA AQUI ---
            // Tira o objeto de dentro de qualquer "pai" e joga ele na raiz da cena
            transform.SetParent(null); 
            
            DontDestroyOnLoad(gameObject); 
            SceneManager.sceneLoaded += OnSceneLoaded;
        } 
        else Destroy(gameObject);
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

                // CORREÇÃO: Usa DayNightCycle
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
    }
}
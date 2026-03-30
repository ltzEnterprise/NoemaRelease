using UnityEngine;

public class SaveableItem : MonoBehaviour
{
    [Header("Identificação")]
    public string uniqueID;

    [Header("Configuração")]
    public bool salvarPosicao = false;

    private bool inicializado = false;
    private bool estaSaindoDoJogo = false;

    void Awake()
    {
        Application.quitting += () => { estaSaindoDoJogo = true; };
    }

    void Start()
    {
        if (string.IsNullOrEmpty(uniqueID)) return;

        // TRAVA DO EDITOR: Não lê save sujo do HD se você estiver testando na Unity.
        // Assim as coisas sempre começam no estado padrão que você deixou na Scene.
        if (Application.isEditor) return;

        if (PersistenciaManager.Instance != null)
        {
            bool estadoSalvo = PersistenciaManager.Instance.CarregarEstadoObjeto(uniqueID, gameObject.activeSelf);
            
            if (gameObject.activeSelf != estadoSalvo)
            {
                gameObject.SetActive(estadoSalvo);
                if (!estadoSalvo) return; 
            }

            if (salvarPosicao && estadoSalvo)
            {
                PersistenciaManager.Instance.CarregarTransform(uniqueID, transform);
            }
        }

        inicializado = true;
    }

    void OnDisable()
    {
        if (!inicializado) return;
        if (estaSaindoDoJogo) return;
        if (!gameObject.scene.isLoaded) return; 
        
        // TRAVA DO EDITOR: Se você apertar STOP na Unity, ele não salva a morte do objeto.
        if (Application.isEditor) return; 

        SalvarMeuEstado(false);
    }
    
    void OnDestroy()
    {
        if (!inicializado) return;
        if (estaSaindoDoJogo) return;
        if (!gameObject.scene.isLoaded) return;
        
        // TRAVA DO EDITOR: Mesma coisa aqui.
        if (Application.isEditor) return; 

        SalvarMeuEstado(false); 
    }

    void SalvarMeuEstado(bool estaAtivo)
    {
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, estaAtivo);

            if (estaAtivo && salvarPosicao)
            {
                PersistenciaManager.Instance.SalvarTransform(uniqueID, transform);
            }
        }
    }

    [ContextMenu("Gerar ID Único")]
    private void GenerateID()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }
}
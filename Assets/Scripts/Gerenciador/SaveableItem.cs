using UnityEngine;

public class SaveableItem : MonoBehaviour
{
    [Header("Identificação")]
    public string uniqueID;

    [Header("Configuração")]
    public bool salvarPosicao = false;

    void Start()
    {
        if (string.IsNullOrEmpty(uniqueID)) return;
        if (Application.isEditor) return; // Trava do editor

        // MÁGICA DE CARREGAMENTO: Se estiver desligado no Save, ele desliga AQUI.
        if (PersistenciaManager.Instance != null)
        {
            bool estadoSalvo = PersistenciaManager.Instance.CarregarEstadoObjeto(uniqueID, gameObject.activeSelf);
            
            if (gameObject.activeSelf != estadoSalvo)
            {
                gameObject.SetActive(estadoSalvo);
                if (!estadoSalvo) return; // Se for pra ficar desligado, morre o script aqui.
            }

            if (salvarPosicao && estadoSalvo)
            {
                PersistenciaManager.Instance.CarregarTransform(uniqueID, transform);
            }
        }
    }

    // FUNÇÃO MANUAL PRA SALVAR (Você chama isso quando ele for destruído/pego no jogo)
    public void ForcarSaveDesligado()
    {
        if (Application.isEditor) return;
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, false);
            PersistenciaManager.Instance.SalvarTudo(); // Salva na hora pro disco
        }
        gameObject.SetActive(false);
    }

    // FUNÇÃO PRA SALVAR POSIÇÃO 
    public void SalvarPosicaoAtual()
    {
        if (Application.isEditor) return;
        if (PersistenciaManager.Instance != null && salvarPosicao)
        {
            PersistenciaManager.Instance.SalvarTransform(uniqueID, transform);
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, gameObject.activeSelf);
            PersistenciaManager.Instance.SalvarTudo();
        }
    }

    [ContextMenu("Gerar ID Único")]
    private void GenerateID()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }
}
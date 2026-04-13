using UnityEngine;
using System.Collections;

public class SaveableItem : MonoBehaviour
{
    [Header("Identificação Única")]
    public string uniqueID;

    [Header("Configuração de Persistência")]
    public bool salvarPosicao = false;
    public bool autoRegistrarAoDesativar = true;

    private bool carregado = false;

    void Start()
    {
        if (string.IsNullOrEmpty(uniqueID) || PersistenciaManager.Instance == null) return;

        StartCoroutine(CarregarComDelay());
    }

    IEnumerator CarregarComDelay()
    {
        yield return null; // 🔥 evita conflito de inicialização
        CarregarDados();
    }

    private void CarregarDados()
    {
        if (carregado) return;

        // 🔥 A MÁGICA AQUI: Ele só mexe no objeto se DE FATO existir um save para ele.
        // Se for jogo novo ou reset, ele ignora e deixa o objeto ativo igual na cena original!
        if (PersistenciaManager.Instance.TemEstadoSalvo(uniqueID))
        {
            bool estadoSalvo = PersistenciaManager.Instance.ObterEstado(uniqueID);

            if (gameObject.activeSelf != estadoSalvo)
            {
                gameObject.SetActive(estadoSalvo);
            }

            if (estadoSalvo && salvarPosicao)
            {
                PersistenciaManager.Instance.CarregarTransform(uniqueID, transform);
                Physics.SyncTransforms();
            }
        }

        carregado = true;
    }

    // --- REGISTRO MANUAL (INALTERADO) ---

    public void RegistrarColeta()
    {
        if (PersistenciaManager.Instance == null) return;

        PersistenciaManager.Instance.RegistrarEstado(uniqueID, false);
        gameObject.SetActive(false);
    }

    public void RegistrarPosicaoEAtivo()
    {
        if (PersistenciaManager.Instance == null) return;

        PersistenciaManager.Instance.RegistrarEstado(uniqueID, gameObject.activeSelf);

        if (salvarPosicao)
        {
            PersistenciaManager.Instance.SalvarTransform(uniqueID, transform);
        }
    }

    // 🔥 Proteção pesada no OnDisable para a Unity não bugar na troca de cena
    private void OnDisable()
    {
        if (!Application.isPlaying) return;
        if (gameObject != null && !gameObject.scene.isLoaded) return;

        if (autoRegistrarAoDesativar && carregado && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, false);
        }
    }

    [ContextMenu("Gerar ID Único")]
    private void GenerateID()
    {
        uniqueID = System.Guid.NewGuid().ToString().ToUpper();
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueID)) GenerateID();
    }
}
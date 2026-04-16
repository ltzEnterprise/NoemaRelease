using UnityEngine;
using System.Collections.Generic;

public class SaveableItem : MonoBehaviour
{
    [Header("NÃO DEIXE ID REPETIDO!")]
    public string uniqueID;

    public static Dictionary<string, SaveableItem> registroGlobal = new Dictionary<string, SaveableItem>();

    void Awake()
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            Debug.LogError($"[ERRO DE SAVE] O objeto {gameObject.name} tá sem ID!");
            return;
        }

        if (registroGlobal.ContainsKey(uniqueID) && registroGlobal[uniqueID] != this)
        {
            Debug.LogError($"<color=red>[ERRO FATAL]</color> ID DUPLICADO: '{uniqueID}' em '{gameObject.name}'.");
        }
        else
        {
            registroGlobal[uniqueID] = this;
        }
    }

    void Start()
    {
        AplicarEstadoDoSave();
    }

    void AplicarEstadoDoSave()
    {
        if (PersistenciaManager.Instance != null && PersistenciaManager.Instance.TemEstadoSalvo(uniqueID))
        {
            bool taAtivo = PersistenciaManager.Instance.ObterEstado(uniqueID, gameObject.activeSelf);
            
            if (gameObject.activeSelf != taAtivo) 
            {
                gameObject.SetActive(taAtivo);
            }

            if (taAtivo) 
            {
                PersistenciaManager.Instance.CarregarTransform(uniqueID, transform);
                Physics.SyncTransforms(); 
            }
        }
    }

    void OnDestroy()
    {
        if (registroGlobal.ContainsKey(uniqueID) && registroGlobal[uniqueID] == this)
        {
            registroGlobal.Remove(uniqueID);
        }
    }

    [ContextMenu("Gerar ID Único")]
    private void GenerateID() { uniqueID = System.Guid.NewGuid().ToString().ToUpper(); }

    private void OnValidate() { if (string.IsNullOrEmpty(uniqueID)) GenerateID(); }
}
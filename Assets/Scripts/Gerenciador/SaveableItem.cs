using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SaveableItem : MonoBehaviour
{
    [Header("NÃO DEIXE ID REPETIDO!")]
    public string uniqueID;

    public static Dictionary<string, SaveableItem> registroGlobal = new Dictionary<string, SaveableItem>();

    private bool estadoAplicado = false;

    void Awake()
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            Debug.LogError($"[ERRO DE SAVE] O objeto {gameObject.name} está sem ID.");
            return;
        }

        RegistrarNoGlobal();
    }

    void OnEnable()
    {
        RegistrarNoGlobal();
    }

    void Start()
    {
        StartCoroutine(AplicarEstadoSeguro());
    }

    private void RegistrarNoGlobal()
    {
        if (string.IsNullOrEmpty(uniqueID)) return;

        if (registroGlobal.ContainsKey(uniqueID))
        {
            if (registroGlobal[uniqueID] == null)
            {
                registroGlobal[uniqueID] = this;
                return;
            }

            if (registroGlobal[uniqueID] != this)
            {
                Debug.LogError($"[SAVE] ID DUPLICADO REAL: '{uniqueID}' em '{gameObject.name}'. O save desse objeto pode conflitar.");
                registroGlobal[uniqueID] = this;
            }
        }
        else
        {
            registroGlobal.Add(uniqueID, this);
        }
    }

    IEnumerator AplicarEstadoSeguro()
    {
        if (estadoAplicado) yield break;

        yield return new WaitUntil(() => PersistenciaManager.Instance != null);
        yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);
        yield return null;

        if (string.IsNullOrEmpty(uniqueID)) yield break;

        if (PersistenciaManager.Instance.TemEstadoSalvo(uniqueID))
        {
            bool estadoAtivoSalvo = PersistenciaManager.Instance.ObterEstado(uniqueID, gameObject.activeSelf);

            if (estadoAtivoSalvo)
            {
                PersistenciaManager.Instance.CarregarTransform(uniqueID, transform);
                Physics.SyncTransforms();

                if (!gameObject.activeSelf)
                    gameObject.SetActive(true);
            }
            else
            {
                PersistenciaManager.Instance.CarregarTransform(uniqueID, transform);
                Physics.SyncTransforms();

                if (gameObject.activeSelf)
                    gameObject.SetActive(false);
            }
        }

        estadoAplicado = true;
    }

    void OnDestroy()
    {
        if (!string.IsNullOrEmpty(uniqueID) &&
            registroGlobal.ContainsKey(uniqueID) &&
            registroGlobal[uniqueID] == this)
        {
            registroGlobal.Remove(uniqueID);
        }
    }

    [ContextMenu("Gerar ID Único")]
    private void GenerateID()
    {
        uniqueID = System.Guid.NewGuid().ToString().ToUpper();
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(uniqueID))
            GenerateID();
    }
}
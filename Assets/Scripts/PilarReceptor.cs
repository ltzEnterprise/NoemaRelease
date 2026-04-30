using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PilarReceptor : MonoBehaviour
{
    public static List<PilarReceptor> todosPilares = new List<PilarReceptor>();

    [Header("--- SAVE SYSTEM ---")]
    public string uniqueID; 

    [Header("Identificação")]
    public int idLinkPilar = 0; 

    [Header("O que aparece?")]
    public GameObject objetoNoPilar; 

    public bool ativado = false;

    private bool inicializado = false;

    void Awake()
    {
        todosPilares.RemoveAll(item => item == null);

        if (!todosPilares.Contains(this))
            todosPilares.Add(this);

        if (objetoNoPilar)
            objetoNoPilar.SetActive(false);
    }

    void Start()
    {
        StartCoroutine(CarregarEstadoSeguro());
    }

    IEnumerator CarregarEstadoSeguro()
    {
        if (string.IsNullOrEmpty(uniqueID)) 
        {
            Debug.LogWarning($"[Pilar] O pilar '{gameObject.name}' está sem UniqueID configurado!");
            inicializado = true;
            yield break;
        }

        if (PersistenciaManager.Instance != null)
            yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);

        if (PersistenciaManager.Instance != null)
        {
            bool estadoSalvo = PersistenciaManager.Instance.ObterEstado(uniqueID, false);
            
            if (estadoSalvo)
            {
                ativado = true;

                if (objetoNoPilar)
                    objetoNoPilar.SetActive(true);
            }
        }

        inicializado = true;
        ChecarCondicaoCutscene();
    }

    void OnDestroy()
    {
        if (todosPilares.Contains(this))
            todosPilares.Remove(this);
    }

    public void AtivarObjeto()
    {
        if (!inicializado) return;
        if (ativado) return; 

        ativado = true;

        if (objetoNoPilar) 
            objetoNoPilar.SetActive(true);

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);
            SalvarProgressoSeguro();
        }

        ChecarCondicaoCutscene();
    }

    void ChecarCondicaoCutscene()
    {
        int contador = 0;

        foreach (var pilar in todosPilares)
        {
            if (pilar != null && pilar.ativado)
                contador++;
        }

        if (contador >= todosPilares.Count && todosPilares.Count > 0)
        {
            Debug.Log("Todos os pilares ativos! Iniciando Cutscene...");

            if (GerenciadorCutscene.Instance != null)
                GerenciadorCutscene.Instance.IniciarFinal();
        }
    }

    public static PilarReceptor BuscarPilarPorID(int id)
    {
        foreach (var p in todosPilares)
        {
            if (p != null && p.idLinkPilar == id)
                return p;
        }

        return null;
    }

    private void SalvarProgressoSeguro()
    {
        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(false);
    }

    [ContextMenu("Gerar ID Único")]
    private void GenerateID()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }
}
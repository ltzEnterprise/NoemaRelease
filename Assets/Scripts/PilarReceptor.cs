using UnityEngine;
using System.Collections.Generic;

public class PilarReceptor : MonoBehaviour
{
    public static List<PilarReceptor> todosPilares = new List<PilarReceptor>();
    private static int pilaresAtivadosCount = 0; // Contador global

    [Header("Identificação")]
    public int idLinkPilar = 0; 

    [Header("O que aparece?")]
    public GameObject objetoNoPilar; 

    public bool ativado = false;

    void Awake()
    {
        if (objetoNoPilar) objetoNoPilar.SetActive(false);
    }

    void OnEnable()
    {
        if (!todosPilares.Contains(this)) todosPilares.Add(this);
    }

    void OnDisable()
    {
        if (todosPilares.Contains(this)) todosPilares.Remove(this);
    }

    public void AtivarObjeto()
    {
        if (ativado) return; 

        ativado = true;
        pilaresAtivadosCount++; // Conta +1 pilar ativado

        if (objetoNoPilar) 
            objetoNoPilar.SetActive(true);

        // SE COMPLETOU TODOS OS PILARES DA CENA
        if (pilaresAtivadosCount >= todosPilares.Count && todosPilares.Count > 0)
        {
            Debug.Log("Todos os pilares ativos! Iniciando Cutscene...");
            if (GerenciadorCutscene.Instance != null)
            {
                GerenciadorCutscene.Instance.IniciarFinal();
            }
        }
    }

    public static PilarReceptor BuscarPilarPorID(int id)
    {
        foreach (var p in todosPilares)
        {
            if (p.idLinkPilar == id) return p;
        }
        return null;
    }
}
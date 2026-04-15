using UnityEngine;
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

    void Awake()
    {
        // 🔥 Limpa pilares mortos da lista caso a cena recarregue
        todosPilares.RemoveAll(item => item == null);

        if (!todosPilares.Contains(this)) todosPilares.Add(this);

        if (objetoNoPilar) objetoNoPilar.SetActive(false);
    }

    void Start()
    {
        if (string.IsNullOrEmpty(uniqueID)) 
        {
            Debug.LogWarning($"[Pilar] O pilar '{gameObject.name}' tá sem UniqueID configurado!");
            return;
        }

        // --- CARREGA O SAVE ---
        if (!Application.isEditor && PersistenciaManager.Instance != null)
        {
            bool estadoSalvo = PersistenciaManager.Instance.ObterEstado(uniqueID);
            
            if (estadoSalvo)
            {
                ativado = true;
                if (objetoNoPilar) objetoNoPilar.SetActive(true);
            }
        }
    }

    void OnDestroy()
    {
        if (todosPilares.Contains(this)) todosPilares.Remove(this);
    }

    public void AtivarObjeto()
    {
        if (ativado) return; 

        ativado = true;

        if (objetoNoPilar) 
            objetoNoPilar.SetActive(true);

        // --- SALVA NO HD NA MESMA HORA ---
        if (!Application.isEditor && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);
            PersistenciaManager.Instance.SalvarTudo();
        }

        ChecarCondicaoCutscene();
    }

    void ChecarCondicaoCutscene()
    {
        // 🔥 A MÁGICA: Em vez de contar com variável estática que buga no Load, 
        // ele só passa o olho em todos os pilares reais da cena e conta.
        int contador = 0;
        foreach(var pilar in todosPilares)
        {
            if (pilar.ativado) contador++;
        }

        if (contador >= todosPilares.Count && todosPilares.Count > 0)
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

    [ContextMenu("Gerar ID Único")]
    private void GenerateID()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }
}
using UnityEngine;
using System.Collections.Generic;

public class PilarReceptor : MonoBehaviour
{
    public static List<PilarReceptor> todosPilares = new List<PilarReceptor>();
    private static int pilaresAtivadosCount = 0; // Contador global

    [Header("--- SAVE SYSTEM ---")]
    public string uniqueID; 

    [Header("Identificação")]
    public int idLinkPilar = 0; 

    [Header("O que aparece?")]
    public GameObject objetoNoPilar; 

    public bool ativado = false;

    void Awake()
    {
        // Trava de segurança pra cena recarregar limpa
        if (!todosPilares.Contains(this)) todosPilares.Add(this);
        
        // Se for o primeiro pilar a acordar, zera o contador de saves/cenas anteriores
        if (todosPilares.Count == 1) pilaresAtivadosCount = 0;

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
                pilaresAtivadosCount++; 
                if (objetoNoPilar) objetoNoPilar.SetActive(true);
                
                // Nota: A gente NÃO chama o IniciarFinal() aqui no Start pra não repetir 
                // a cutscene toda vez que você der Load no jogo.
            }
        }
    }

    void OnDestroy()
    {
        // Limpa da lista de verdade quando a cena fecha
        if (todosPilares.Contains(this)) todosPilares.Remove(this);
        if (todosPilares.Count == 0) pilaresAtivadosCount = 0;
    }

    public void AtivarObjeto()
    {
        if (ativado) return; 

        ativado = true;
        pilaresAtivadosCount++; // Conta +1 pilar ativado

        if (objetoNoPilar) 
            objetoNoPilar.SetActive(true);

        // --- SALVA NO HD NA MESMA HORA ---
        if (!Application.isEditor && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);
            PersistenciaManager.Instance.SalvarTudo();
        }

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

    [ContextMenu("Gerar ID Único")]
    private void GenerateID()
    {
        uniqueID = System.Guid.NewGuid().ToString();
    }
}
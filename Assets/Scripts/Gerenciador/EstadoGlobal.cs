using UnityEngine;

public static class EstadoGlobal
{
    // Arrays de Dados
    public static bool[] armasDesbloqueadas = new bool[15]; 
    public static bool[] casasResolvidas = new bool[10]; 

    // Variáveis Especiais
    public static bool temAChave = false; 
    public static bool temDiscoRuna = false; 

    public static void ResetarTudo()
    {
        for (int i = 0; i < armasDesbloqueadas.Length; i++) armasDesbloqueadas[i] = false;
        for (int i = 0; i < casasResolvidas.Length; i++) casasResolvidas[i] = false;
        
        temAChave = false;
        temDiscoRuna = false;

        Debug.Log("[EstadoGlobal] Todas as variáveis foram resetadas.");
    }

    public static void SalvarNoSlot(int slot)
    {
        if (PersistenciaManager.Instance == null) return;

        // Salva Armas/Itens no JSON
        for (int i = 0; i < armasDesbloqueadas.Length; i++)
            PersistenciaManager.Instance.RegistrarEstado("EG_Arma_" + i, armasDesbloqueadas[i]);
            
        // Salva Casas no JSON
        for (int i = 0; i < casasResolvidas.Length; i++)
            PersistenciaManager.Instance.RegistrarEstado("EG_Casa_" + i, casasResolvidas[i]);
        
        // Salva Especiais no JSON
        PersistenciaManager.Instance.RegistrarEstado("EG_TemChave", temAChave);
        PersistenciaManager.Instance.RegistrarEstado("EG_TemDiscoRuna", temDiscoRuna);
    }

    public static void CarregarDoSlot(int slot)
    {
        if (PersistenciaManager.Instance == null) return;

        // Carrega Armas/Itens do JSON
        for (int i = 0; i < armasDesbloqueadas.Length; i++)
            armasDesbloqueadas[i] = PersistenciaManager.Instance.ObterEstado("EG_Arma_" + i);
            
        // Carrega Casas do JSON
        for (int i = 0; i < casasResolvidas.Length; i++)
            casasResolvidas[i] = PersistenciaManager.Instance.ObterEstado("EG_Casa_" + i);
        
        // Carrega Especiais do JSON
        temAChave = PersistenciaManager.Instance.ObterEstado("EG_TemChave");
        temDiscoRuna = PersistenciaManager.Instance.ObterEstado("EG_TemDiscoRuna");
    }
}
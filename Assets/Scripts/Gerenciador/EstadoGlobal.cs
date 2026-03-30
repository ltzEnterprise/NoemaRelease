using UnityEngine;

public static class EstadoGlobal
{
    // Arrays de Dados
    public static bool[] armasDesbloqueadas = new bool[15]; // Aumentei pra 15 pra garantir espaço pra tudo
    public static bool[] casasResolvidas = new bool[10]; 

    // Variáveis Especiais (Agora moram aqui para não quebrar)
    public static bool temAChave = false; 
    public static bool temDiscoRuna = false; 

    public static void ResetarTudo()
    {
        for (int i = 0; i < armasDesbloqueadas.Length; i++) armasDesbloqueadas[i] = false;
        for (int i = 0; i < casasResolvidas.Length; i++) casasResolvidas[i] = false;
        
        temAChave = false;
        temDiscoRuna = false;

        Debug.Log("Estado Global Resetado.");
    }

    public static void SalvarNoSlot(int slot)
    {
        string p = "Slot_" + slot + "_Global_";

        // Salva Armas/Itens
        for (int i = 0; i < armasDesbloqueadas.Length; i++)
            PlayerPrefs.SetInt(p + "Arma_" + i, armasDesbloqueadas[i] ? 1 : 0);
            
        // Salva Casas
        for (int i = 0; i < casasResolvidas.Length; i++)
            PlayerPrefs.SetInt(p + "Casa_" + i, casasResolvidas[i] ? 1 : 0);
        
        // Salva Especiais
        PlayerPrefs.SetInt(p + "TemChave", temAChave ? 1 : 0);
        PlayerPrefs.SetInt(p + "TemDiscoRuna", temDiscoRuna ? 1 : 0);
        
        PlayerPrefs.Save();
    }

    public static void CarregarDoSlot(int slot)
    {
        string p = "Slot_" + slot + "_Global_";

        // Carrega Armas/Itens
        for (int i = 0; i < armasDesbloqueadas.Length; i++)
            armasDesbloqueadas[i] = PlayerPrefs.GetInt(p + "Arma_" + i, 0) == 1;
            
        // Carrega Casas
        for (int i = 0; i < casasResolvidas.Length; i++)
            casasResolvidas[i] = PlayerPrefs.GetInt(p + "Casa_" + i, 0) == 1;
        
        // Carrega Especiais
        temAChave = PlayerPrefs.GetInt(p + "TemChave", 0) == 1;
        temDiscoRuna = PlayerPrefs.GetInt(p + "TemDiscoRuna", 0) == 1;
    }
}
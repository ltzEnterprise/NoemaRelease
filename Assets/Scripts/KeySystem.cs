using UnityEngine;

public class KeySystem : MonoBehaviour
{
    [Header("--- IDENTIDADE DA CHAVE ---")]
    [Tooltip("Dê um ID único aqui (Ex: Chave_Porao, Chave_Sotao, Chave_Mestra)")]
    public string keyID = "Chave_Generica"; 

    [Header("--- CONFIG ---")]
    public bool destroyOnPickup = true;
    
    [Header("--- UI ---")]
    public GameObject textoInteragir; 
    public GameObject iconeHUD; 

    [Header("--- AUDIO ---")]
    public AudioClip somPegar;

    // --- MÉTODOS GLOBAIS COM PERSISTENCIA MANAGER ---
    
    public static bool TemChave(string id) 
    {
        if (PersistenciaManager.Instance != null)
        {
            // Lê do dicionário (ou do HD) usando o prefixo Key_ pra não misturar com portas
            return PersistenciaManager.Instance.ObterEstado("Key_" + id);
        }
        return false;
    }

    public static void AdicionarChave(string id)
    {
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado("Key_" + id, true);
        }
    }

    public static void GastarChave(string id)
    {
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado("Key_" + id, false);
        }
    }

    // --- LÓGICA DO OBJETO FÍSICO ---
    void Start()
    {
        if (textoInteragir) textoInteragir.SetActive(false);

        // Se o jogador já tem a chave no save atual, apaga ela da cena
        if (TemChave(keyID) && destroyOnPickup)
        {
            gameObject.SetActive(false);
        }
    }

    public void Interagir()
    {
        Pickup();
    }

    public void Pickup()
    {
        AdicionarChave(keyID);

        if (somPegar) AudioSource.PlayClipAtPoint(somPegar, transform.position);
        
        if (iconeHUD) iconeHUD.SetActive(true);

        if (textoInteragir) textoInteragir.SetActive(false);

        if (destroyOnPickup) gameObject.SetActive(false);
    }
    
    public void AoOlhar() { if (textoInteragir) textoInteragir.SetActive(true); }
    public void AoSair() { if (textoInteragir) textoInteragir.SetActive(false); }
}
using UnityEngine;

public class KeySystem : MonoBehaviour
{
    [Header("--- KEY IDENTITY (GROUND ITEM) ---")]
    public string keyID = "Chave_Generica"; 
    public bool destroyOnPickup = true;
    
    [Header("--- UI & AUDIO ---")]
    public GameObject interactText; 
    public AudioClip pickupSound;

    // --- BRIDGE TO KEY MANAGER ---
    // Mantido em PT-BR para não quebrar o seu script ChildPuzzleDoor!
    public static bool TemChave(string id) 
    {
        if (KeyManager.Instance != null)
            return KeyManager.Instance.CheckIfHasKey(id);
        return false;
    }

    public static void AdicionarChave(string id)
    {
        if (KeyManager.Instance != null)
            KeyManager.Instance.TurnOnKeyIcon(id);

        if (!Application.isEditor && PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado("Key_" + id, true);
    }

    public static void GastarChave(string id)
    {
        if (KeyManager.Instance != null)
            KeyManager.Instance.TurnOffKeyIcon(id);

        if (!Application.isEditor && PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado("Key_" + id, false);
    }

    // --- LÓGICA DO ITEM NO CHÃO ---
    void Start()
    {
        if (interactText) interactText.SetActive(false);
        if (TemChave(keyID) && destroyOnPickup) gameObject.SetActive(false);
    }

    public void Interagir() { Pickup(); }

    public void Pickup()
    {
        AdicionarChave(keyID); 
        if (pickupSound) AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        if (interactText) interactText.SetActive(false);
        if (destroyOnPickup) gameObject.SetActive(false);
    }
    
    public void AoOlhar() { if (interactText) interactText.SetActive(true); }
    public void AoSair() { if (interactText) interactText.SetActive(false); }
}
using UnityEngine;

public class CameraEvent : MonoBehaviour
{
    [Header("Item Config")]
    public int cameraID = 6; 
    public GameObject interactText; 

    [Header("Scene Events (Jumpscare)")]
    public GameObject[] objectsToHide; 
    public GameObject[] objectsToShow;  

    [Header("Audio")]
    public AudioClip scareSound; 

    void Start()
    {
        if (interactText) interactText.SetActive(false);
    }

    // --- RAYCAST SYSTEM ---
    public void AoOlhar()
    {
        if (interactText) interactText.SetActive(true);
    }

    public void AoSair()
    {
        if (interactText) interactText.SetActive(false);
    }

    public void Interagir()
    {
        TriggerEvent();
    }
    // -----------------------

    void TriggerEvent()
    {
        // CORREÇÃO: Desliga o texto explicitamente antes de se matar
        if (interactText) interactText.SetActive(false);

        // 1. Return Camera to Inventory
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ReceberItem(cameraID);
        }

        // 2. Swap Objects
        foreach (var obj in objectsToHide) if (obj) obj.SetActive(false);
        foreach (var obj in objectsToShow) if (obj) obj.SetActive(true);

        // 3. Sound
        if (scareSound) AudioSource.PlayClipAtPoint(scareSound, transform.position, 1.0f);

        // 4. Disable this trigger so it doesn't happen again
        gameObject.SetActive(false);
    }
}
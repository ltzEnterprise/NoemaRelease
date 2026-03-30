using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TeleportArea : MonoBehaviour
{
    [Header("--- DESTINATION ---")]
    [Tooltip("Drag the Empty Object that represents where the player will spawn")]
    public Transform destinationPoint; 

    [Header("--- SETTINGS ---")]
    public KeyCode teleportKey = KeyCode.T;
    
    [Header("--- UI & EFFECTS (Optional) ---")]
    [Tooltip("On-screen text: 'Press T to travel'")]
    public GameObject interactionTextUI; 
    public AudioSource audioSource;
    public AudioClip teleportSound;

    private bool playerInArea = false;

    void Start()
    {
        // Ensures the UI text starts disabled
        if (interactionTextUI) interactionTextUI.SetActive(false);
        
        // Forces the collider to be a trigger
        GetComponent<Collider>().isTrigger = true; 
    }

    // When the player ENTERS the area
    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<FPS_Master>() != null)
        {
            playerInArea = true;
            if (interactionTextUI) interactionTextUI.SetActive(true);
        }
    }

    // When the player LEAVES the area
    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<FPS_Master>() != null)
        {
            playerInArea = false;
            if (interactionTextUI) interactionTextUI.SetActive(false);
        }
    }

    void Update()
    {
        // If player is inside the trigger AND presses T
        if (playerInArea && Input.GetKeyDown(teleportKey))
        {
            ExecuteTeleport();
        }
    }

    void ExecuteTeleport()
    {
        if (destinationPoint == null)
        {
            Debug.LogError("Missing Destination Point on TeleportArea!");
            return;
        }

        if (audioSource && teleportSound) audioSource.PlayOneShot(teleportSound);

        if (interactionTextUI) interactionTextUI.SetActive(false);
        playerInArea = false; // Reset to prevent double triggers

        // Calls your existing teleport logic from FPS_Master
        FPS_Master.Instance.Teleportar(destinationPoint.position);
    }
}
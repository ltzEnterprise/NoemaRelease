using UnityEngine;
using System.Collections;

public class MagicalPhoto : MonoBehaviour
{
    [Header("--- PHOTO ANIMATION ---")]
    [Tooltip("Where the photo rests on the screen (e.g., bottom left)")]
    public Vector3 restingPosition;
    [Tooltip("Where the photo goes when aiming (center of the screen)")]
    public Vector3 aimingPosition;
    public float animationSpeed = 10f;

    [Header("--- ALIGNMENT SETTINGS ---")]
    [Tooltip("The empty GameObject marking the exact spot and rotation the player needs to match.")]
    public Transform idealPoint;
    
    [Tooltip("How many meters away can the player be and still trigger it?")]
    public float maxDistance = 2.0f;
    
    [Tooltip("How many degrees off can the camera angle be? (15 is a good start)")]
    public float maxAngle = 15.0f;

    [Header("--- THE MAGIC ---")]
    [Tooltip("The object that will appear in the world.")]
    public GameObject objectToReveal;
    
    [Tooltip("Sound to play when the object is revealed.")]
    public AudioSource audioSource;
    public AudioClip revealSound;

    private bool isAiming = false;
    private bool alreadyUsed = false;

    void Start()
    {
        if (objectToReveal) objectToReveal.SetActive(false);
        transform.localPosition = restingPosition;
    }

    void Update()
    {
        if (alreadyUsed) return;

        // --- AIMING CONTROL (RIGHT CLICK) ---
        if (Input.GetMouseButtonDown(1)) isAiming = true;
        if (Input.GetMouseButtonUp(1)) isAiming = false;

        // Smooth animation between resting and aiming positions
        Vector3 targetPos = isAiming ? aimingPosition : restingPosition;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * animationSpeed);

        // --- TRY TO REVEAL (LEFT CLICK) ---
        if (isAiming && Input.GetMouseButtonDown(0))
        {
            TryRevealObject();
        }
    }

    void TryRevealObject()
    {
        if (idealPoint == null || Camera.main == null) return;

        Transform playerCam = Camera.main.transform;

        // 1. Calculate distance in meters
        float currentDistance = Vector3.Distance(playerCam.position, idealPoint.position);

        // 2. Calculate angle difference in degrees
        float currentAngle = Vector3.Angle(playerCam.forward, idealPoint.forward);

        // 3. Check if player is within the allowed margins
        if (currentDistance <= maxDistance && currentAngle <= maxAngle)
        {
            Success();
        }
        else
        {
            // This print helps you test and adjust the margins in the Inspector
            Debug.Log($"Missed! Distance: {currentDistance:F2}m (Max: {maxDistance}) | Angle: {currentAngle:F2}° (Max: {maxAngle})");
        }
    }

    void Success()
    {
        alreadyUsed = true;
        
        if (objectToReveal) objectToReveal.SetActive(true);
        
        if (audioSource && revealSound) audioSource.PlayOneShot(revealSound);

        // Calling your exact save system methods so nothing breaks
        if (PersistenciaManager.Instance != null && objectToReveal != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(objectToReveal.name, true);
            PersistenciaManager.Instance.SalvarTudo();
        }

        StartCoroutine(HidePhotoRoutine());
    }

    IEnumerator HidePhotoRoutine()
    {
        float t = 0;
        Vector3 currentPos = transform.localPosition;
        Vector3 bottomPos = currentPos + new Vector3(0, -2f, 0);

        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            transform.localPosition = Vector3.Lerp(currentPos, bottomPos, t);
            yield return null;
        }
        
        gameObject.SetActive(false);
    }
}
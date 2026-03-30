using UnityEngine;

public class PirateLever : MonoBehaviour
{
    [Header("--- SETTINGS ---")]
    public PirateSymbol symbol; 
    public bool isOn = false;

    [Header("--- UI / FEEDBACK ---")]
    public GameObject interactText; 

    [Header("--- REFERENCES ---")]
    public CaptainLeverPuzzle puzzleManager; 
    public Transform handleObject; 

    [Header("--- ANIMATION (Eixo X) ---")]
    public float angleOff = 125f; 
    public float angleOn = 0f;    
    public float smoothSpeed = 10f;

    [Header("--- AUDIO ---")]
    public AudioSource audioSource;
    public AudioClip sfxClick;

    private Quaternion targetRotation;
    
    // Trava de segurança do Raycast
    private float tempoUltimoClique = 0f;

    void Start()
    {
        if (interactText) interactText.SetActive(false); 
        UpdateTargetRotation();
        
        if (handleObject) handleObject.localRotation = targetRotation;
    }

    void Update()
    {
        if (handleObject)
        {
            // Lerp faz a animação suave de puxar a alavanca
            handleObject.localRotation = Quaternion.Lerp(
                handleObject.localRotation, 
                targetRotation, 
                Time.deltaTime * smoothSpeed
            );
        }
    }

    // --- INTERAÇÃO ---
    public void AoOlhar() { if (interactText) interactText.SetActive(true); }
    public void AoSair() { if (interactText) interactText.SetActive(false); }
    
    public void Interagir() 
    { 
        // Trava anti-spam (0.3 segundos de resfriamento entre os cliques)
        if (Time.time < tempoUltimoClique + 0.3f) return;
        tempoUltimoClique = Time.time;
        
        Toggle(); 
    }

    public void Toggle()
    {
        isOn = !isOn;

        if (audioSource && sfxClick) audioSource.PlayOneShot(sfxClick);
        
        UpdateTargetRotation();

        // Avisa o Puzzle Manager que a alavanca mudou de estado
        if (puzzleManager != null) 
        {
            puzzleManager.CheckRules();
        }
    }

    void UpdateTargetRotation()
    {
        float angle = isOn ? angleOn : angleOff;
        
        // Puxa o Y e Z originais da alavanca para não distorcer o modelo 3D novo
        if (handleObject != null)
        {
            targetRotation = Quaternion.Euler(angle, handleObject.localEulerAngles.y, handleObject.localEulerAngles.z);
        }
        else
        {
            targetRotation = Quaternion.Euler(angle, 0, 0);
        }
    }

    // --- FUNÇÃO PARA RESETAR A ALAVANCA ---
    public void ForceReset()
    {
        isOn = false; // Força desligado
        UpdateTargetRotation(); // Atualiza rotação visual
    }
}
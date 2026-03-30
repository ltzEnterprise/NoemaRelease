using UnityEngine;

public class ReceiverDoor : MonoBehaviour
{
    [Header("--- SAVE NATIVO ---")]
    [Tooltip("Dê um nome único pra essa porta. Ex: Porta_Ruina_1")]
    public string doorID = "";

    [Header("Drag visual object (or Pivot Parent) here")]
    public Transform doorVisual; 

    [Header("Animation Settings")]
    public float speed = 5f;
    public bool openOnYAxis = false; 

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;

    private bool shouldBeOpen = false;
    private Vector3 closedScale;
    private Vector3 openScale;

    void Start()
    {
        if (doorVisual != null)
        {
            // Salva o tamanho original (Porta Fechada)
            closedScale = doorVisual.localScale;
            
            // Calcula o tamanho aberta (Zero no eixo escolhido)
            openScale = closedScale;
            if (openOnYAxis) 
                openScale.y = 0; 
            else 
                openScale.x = 0;
        }

        // --- MÁGICA DO SAVE AQUI ---
        if (!string.IsNullOrEmpty(doorID))
        {
            // Puxa o save do HD (0 = fechada, 1 = aberta)
            int estadoSalvo = PlayerPrefs.GetInt(doorID, 0); 
            shouldBeOpen = (estadoSalvo == 1);

            // Força a escala IMEDIATAMENTE pra não tocar animação sozinha quando carrega o jogo
            if (doorVisual != null)
            {
                doorVisual.localScale = shouldBeOpen ? openScale : closedScale;
            }
        }
        else
        {
            Debug.LogWarning($"[AVISO] A porta {gameObject.name} tá sem ID de save! Escreve um nome lá no Inspector caralho.");
        }
    }

    void Update()
    {
        if (doorVisual != null)
        {
            Vector3 target = shouldBeOpen ? openScale : closedScale;
            doorVisual.localScale = Vector3.Lerp(doorVisual.localScale, target, Time.deltaTime * speed);
        }
    }

    public void SetState(bool shouldOpen) 
    {
        if (shouldBeOpen != shouldOpen)
        {
            shouldBeOpen = shouldOpen;
            PlaySound();

            // --- SALVA O ESTADO NO HD NA HORA QUE ABRE/FECHA ---
            if (!string.IsNullOrEmpty(doorID))
            {
                PlayerPrefs.SetInt(doorID, shouldBeOpen ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }

    void PlaySound()
    {
        if (audioSource != null)
        {
            if (shouldBeOpen && openSound) audioSource.PlayOneShot(openSound);
            else if (!shouldBeOpen && closeSound) audioSource.PlayOneShot(closeSound);
        }
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;

public class DoorWithKeyTeleport : MonoBehaviour
{
    [Header("--- BLOQUEADOR ---")]
    [Tooltip("Coloque o objeto que bloqueia (ex: plasma). Se ficar vazio, funciona normal.")]
    public GameObject bloqueador;

    [Header("--- SAVE SYSTEM ---")]
    public string uniqueID = "Porta_Teleporte_01"; 

    [Header("--- DESTINATION ---")]
    public Transform destinationPoint; 
    public float verticalOffset = 0.1f; 

    [Header("--- KEY SYSTEM ---")]
    public bool needsKey = true;
    public string requiredKeyID = "Chave_Porao";

    [Header("--- UI & FEEDBACK ---")]
    public GameObject interactText; 
    public GameObject lockedMessagePanel; 
    public TextMeshProUGUI feedbackText; 
    public Image fadeImage; 

    [Header("--- AUDIO ---")]
    public AudioSource audioSource;
    public AudioClip lockedSound;
    public AudioClip unlockSound;
    public AudioClip teleportSound;

    private bool isOpen = false;
    private bool isTransitioning = false;
    private bool isShowingMessage = false; 
    private bool taOlhando = false; 

    private bool TaBloqueado()
    {
        return bloqueador != null && bloqueador.activeInHierarchy;
    }

    void Start() 
    {
        if (interactText) interactText.SetActive(false);
        if (lockedMessagePanel) lockedMessagePanel.SetActive(false);
        
        if (fadeImage) 
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0,0,0,0);
        }

        // 🔥 CORREÇÃO DE RACE CONDITION NO LOAD
        StartCoroutine(CarregarSeguro());
    }

    IEnumerator CarregarSeguro()
    {
        yield return null; 

        if (!Application.isEditor && PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            isOpen = PersistenciaManager.Instance.CarregarEstadoObjeto(uniqueID, false);
            if (isOpen) needsKey = false;
        }
    }

    void Update()
    {
        if (TaBloqueado() && interactText != null && interactText.activeSelf)
        {
            interactText.SetActive(false);
        }
    }

    public void AoOlhar()
    {
        taOlhando = true; 

        if (TaBloqueado()) return; 

        if (isTransitioning || isShowingMessage) return; 
        
        if (interactText) interactText.SetActive(true);
    }

    public void AoSair()
    {
        taOlhando = false; 

        if (interactText) interactText.SetActive(false);
    }

    public void Interagir()
    {
        if (TaBloqueado()) return; 

        if (isTransitioning) return;

        if (!needsKey || isOpen)
        {
            StartCoroutine(TeleportSequence());
            return;
        }

        if (KeySystem.TemChave(requiredKeyID))
        {
            OpenDoor();
        }
        else if (!isShowingMessage)
        {
            StartCoroutine(LockedFeedback());
        }
    }

    void OpenDoor()
    {
        isOpen = true;
        KeySystem.GastarChave(requiredKeyID);

        if (!Application.isEditor && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);
            PersistenciaManager.Instance.SalvarTudo(); // Salva logo no disco que gastou a chave e destrancou
        }

        if (audioSource && unlockSound) audioSource.PlayOneShot(unlockSound);
        StartCoroutine(TeleportSequence());
    }

    IEnumerator LockedFeedback()
    {
        isShowingMessage = true; 
        
        if (interactText) interactText.SetActive(false);

        if (audioSource && lockedSound) audioSource.PlayOneShot(lockedSound);
        
        if (lockedMessagePanel) 
        {
            lockedMessagePanel.SetActive(true);
            if (feedbackText) feedbackText.text = "Precisa da " + requiredKeyID.Replace("_", " ");
            yield return new WaitForSeconds(2f);
            lockedMessagePanel.SetActive(false);
        }
        
        isShowingMessage = false; 

        if (taOlhando && interactText && !isTransitioning && !TaBloqueado()) 
        {
            interactText.SetActive(true);
        }
    }

    IEnumerator TeleportSequence()
    {
        isTransitioning = true;
        if (interactText) interactText.SetActive(false);
        if (FPS_Master.Instance) FPS_Master.Instance.AlterarEstadoJogador(true, false);
        if (audioSource && teleportSound) audioSource.PlayOneShot(teleportSound);

        if (fadeImage) 
        {
            fadeImage.gameObject.SetActive(true);
            float a = 0; 
            while(a < 1) { a += Time.deltaTime * 3; fadeImage.color = new Color(0,0,0,a); yield return null; }
        }

        yield return new WaitForSeconds(0.5f);

        if (FPS_Master.Instance && destinationPoint)
        {
            FPS_Master.Instance.Teleportar(destinationPoint.position + (Vector3.up * verticalOffset));
            FPS_Master.Instance.transform.rotation = destinationPoint.rotation;
            Physics.SyncTransforms();
        }

        yield return new WaitForSeconds(0.5f);

        if (fadeImage) 
        {
            float a = 1; 
            while(a > 0) { a -= Time.deltaTime * 2; fadeImage.color = new Color(0,0,0,a); yield return null; }
            fadeImage.gameObject.SetActive(false);
        }

        if (FPS_Master.Instance) FPS_Master.Instance.AlterarEstadoJogador(false, false);
        isTransitioning = false;
    }
}
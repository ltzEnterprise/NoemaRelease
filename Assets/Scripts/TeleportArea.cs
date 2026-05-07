using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class TeleportArea : MonoBehaviour
{
    [Header("--- SAVE SYSTEM ---")]
    [Tooltip("ID único desse teleporte. Use o mesmo ID no baú que depende dele.")]
    public string uniqueID = "TP_Castelo_01";

    [Header("--- DESTINATION ---")]
    [Tooltip("Drag the Empty Object that represents where the player will spawn")]
    public Transform destinationPoint; 

    [Header("--- SETTINGS ---")]
    public KeyCode teleportKey = KeyCode.T;

    [Tooltip("Altura extra para evitar nascer dentro/debaixo do chão.")]
    public float offsetVerticalTeleporte = 0.15f;

    [Tooltip("Se ativado, o player vai copiar apenas a rotação Y do destinationPoint. Nunca copia X/Z.")]
    public bool usarRotacaoDoDestino = false;
    
    [Header("--- UI & EFFECTS (Optional) ---")]
    [Tooltip("On-screen text: 'Press T to travel'")]
    public GameObject interactionTextUI; 
    public AudioSource audioSource;
    public AudioClip teleportSound;

    private bool playerInArea = false;

    private static HashSet<string> teleportsUsadosNestaSessao = new HashSet<string>();

    void Start()
    {
        if (interactionTextUI)
            interactionTextUI.SetActive(false);
        
        GetComponent<Collider>().isTrigger = true; 
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<FPS_Master>() != null || other.GetComponentInParent<FPS_Master>() != null)
        {
            playerInArea = true;

            if (interactionTextUI)
                interactionTextUI.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<FPS_Master>() != null || other.GetComponentInParent<FPS_Master>() != null)
        {
            playerInArea = false;

            if (interactionTextUI)
                interactionTextUI.SetActive(false);
        }
    }

    void Update()
    {
        if (playerInArea && Input.GetKeyDown(teleportKey))
        {
            ExecuteTeleport();
        }
    }

    void ExecuteTeleport()
    {
        if (destinationPoint == null)
        {
            Debug.LogError("[TeleportArea] Missing Destination Point on TeleportArea!");
            return;
        }

        if (FPS_Master.Instance == null)
        {
            Debug.LogError("[TeleportArea] FPS_Master.Instance está nulo.");
            return;
        }

        if (audioSource && teleportSound)
            audioSource.PlayOneShot(teleportSound);

        if (interactionTextUI)
            interactionTextUI.SetActive(false);

        playerInArea = false;

        RegistrarTeleportUsado();

        Vector3 destinoSeguro = destinationPoint.position + Vector3.up * offsetVerticalTeleporte;

        FPS_Master.Instance.Teleportar(destinoSeguro);

        if (usarRotacaoDoDestino)
        {
            Vector3 rotacaoAtual = FPS_Master.Instance.transform.eulerAngles;

            FPS_Master.Instance.transform.rotation = Quaternion.Euler(
                rotacaoAtual.x,
                destinationPoint.eulerAngles.y,
                rotacaoAtual.z
            );

            if (FPS_Master.Instance.cameraJogador != null)
                FPS_Master.Instance.cameraJogador.transform.localRotation = Quaternion.identity;
        }

        Physics.SyncTransforms();

        SalvarProgressoSeguro();
    }

    private void RegistrarTeleportUsado()
    {
        if (string.IsNullOrEmpty(uniqueID))
        {
            Debug.LogWarning("[TeleportArea] uniqueID vazio. O baú não vai conseguir detectar esse teleporte.");
            return;
        }

        teleportsUsadosNestaSessao.Add(uniqueID);

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_Usado", true);
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);
        }

        Debug.Log("[TeleportArea] Teleporte usado e registrado: " + uniqueID);
    }

    public static bool TeleportFoiUsadoNestaSessao(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        return teleportsUsadosNestaSessao.Contains(id);
    }

    public static void RegistrarTeleportUsadoExternamente(string id)
    {
        if (string.IsNullOrEmpty(id))
            return;

        teleportsUsadosNestaSessao.Add(id);
    }

    private void SalvarProgressoSeguro()
    {
        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(true);
    }
}
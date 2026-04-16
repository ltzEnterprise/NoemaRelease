using UnityEngine;

public class DebugColisaoPlayer : MonoBehaviour
{
    [Header("--- CONTROLE DE DEBUG ---")]
    public bool debugAtivo = true;

    private float tempoUltimoAviso = 0f;

    // 1. SE VOCÊ USA CHARACTER CONTROLLER (FPS Padrão)
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!debugAtivo) return;

        // O CharacterController floda o console 60 vezes por segundo enquanto você anda.
        // Esse timer de 1 segundo impede que o seu PC trave de tanta mensagem.
        if (Time.unscaledTime > tempoUltimoAviso + 1f)
        {
            Debug.Log($"<color=green>[CHARACTER CONTROLLER]</color> Bateu em: <b>{hit.gameObject.name}</b>", hit.gameObject);
            tempoUltimoAviso = Time.unscaledTime;
        }
    }

    // 2. SE VOCÊ USA RIGIDBODY (Física Padrão)
    private void OnCollisionEnter(Collision collision)
    {
        if (!debugAtivo) return;
        Debug.Log($"<color=orange>[RIGIDBODY FÍSICO]</color> Encostou em: <b>{collision.gameObject.name}</b>", collision.gameObject);
    }

    // 3. DETECTA TRIGGERS (Áreas invisíveis)
    private void OnTriggerEnter(Collider other)
    {
        if (!debugAtivo) return;
        Debug.Log($"<color=cyan>[TRIGGER]</color> Atravessou: <b>{other.gameObject.name}</b>", other.gameObject);
    }
}
using UnityEngine;

public class MoonTeleport : MonoBehaviour
{
    [Header("Destination Settings")]
    [Tooltip("Drag the Empty GameObject where the player should land.")]
    public Transform destination;

    [Header("Physics Settings")]
    [Tooltip("Should the player keep flying or stop completely?")]
    public bool keepVelocity = false; 

    [Header("Cooldown Settings")]
    [Tooltip("Tempo em segundos que o teleporte fica desativado globalmente")]
    public float delayTeleporte = 1f;

    // O SEGREDO: static faz esse timer ser o mesmo para todas as instâncias desse script
    private static float tempoProximoTeleporte = 0f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Se o tempo atual for menor que o timer travado, ignora a colisão e foda-se
        if (Time.time < tempoProximoTeleporte) return;

        // Checks if it is the Player entering the Moon
        if (collision.CompareTag("Player"))
        {
            TeleportPlayer(collision.gameObject);
        }
    }

    void TeleportPlayer(GameObject player)
    {
        // 1. Teleport Logic
        if (destination != null)
        {
            player.transform.position = destination.position;
            Debug.Log("Moon Power! Teleported to: " + destination.name);

            // Trava o teleporte de TODAS as luas pelo próximo 1 segundo
            tempoProximoTeleporte = Time.time + delayTeleporte;
        }
        else
        {
            Debug.LogWarning("Moon has no destination assigned!");
            return;
        }

        // 2. Physics Reset (Optional)
        // If false, we stop the player so they don't fly off instantly
        if (!keepVelocity)
        {
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Note: In Unity 6 use linearVelocity. In older versions use velocity.
                rb.linearVelocity = Vector2.zero; 
            }
        }
    }
}
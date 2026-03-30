using UnityEngine;

public class SpikeTrap : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Se o Player estiver acima dessa altura, morre.")]
    public float alturaMortal = 0.2f; 
    
    // TRAVA: Garante que o código de morte rode uma vez só e não vire uma metralhadora
    private bool jaMatou = false; 

    private void OnCollisionEnter2D(Collision2D collision)
    {
        VerificarMorte(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        VerificarMorte(collision.gameObject);
    }

    void VerificarMorte(GameObject player)
    {
        if (jaMatou) return;

        if (player.CompareTag("Player"))
        {
            Vector3 posicaoRelativa = transform.InverseTransformPoint(player.transform.position);

            if (posicaoRelativa.y > alturaMortal)
            {
                KillPlayer(player);
            }
        }
    }

    void KillPlayer(GameObject player)
    {
        jaMatou = true;
        Debug.Log("💀 Espetado!");

        // 1. A MARRETADA: Congela o player na exata posição do impacto. 
        // Ele não cai do mapa, não ativa outro trigger e dá paz pra animação tocar.
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false; // Arranca a física dele na hora
        }

        // 2. CHAMA A ANIMAÇÃO DE VERDADE
        if (ManagerMundo2D.Instance != null)
        {
            ManagerMundo2D.Instance.ReiniciarFaseAtual();
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 centro = transform.position;
        Vector3 cima = transform.up * alturaMortal;
        Gizmos.DrawLine(centro + cima - transform.right, centro + cima + transform.right);
    }
}
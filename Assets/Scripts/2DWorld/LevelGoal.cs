using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    private bool jaAtivado = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !jaAtivado)
        {
            jaAtivado = true;

            // 1. Corta os controles na hora que encosta
            PlayerMovement2D player = other.GetComponent<PlayerMovement2D>();
            if (player != null) player.podeAndar = false;

            // 2. Congela o player no ar pra ele não cair nem girar na tela enquanto a fase acaba
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false; 
            }

            // 3. Reseta a gravidade da Unity e endireita o player ANTES da próxima fase carregar
            Physics2D.gravity = new Vector2(0, -9.81f);
            other.transform.rotation = Quaternion.Euler(0, 0, 0);

            // 4. Avisa o Manager pra fazer a mágica do reset visual (Íris) e trocar a cena
            if (ManagerMundo2D.Instance != null)
            {
                ManagerMundo2D.Instance.AvancarFase();
            }
        }
    }
}
using UnityEngine;

public class MagicWater : MonoBehaviour
{
    public float tempoLevitacao = 3f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Tenta achar o PlayerMovement2D no objeto que entrou
        PlayerMovement2D player = collision.GetComponent<PlayerMovement2D>();

        if (player != null)
        {
            // Ativa o modo Deus (Levitação)
            player.StartLevitation(tempoLevitacao);
            
            // Opcional: Tocar som ou efeito visual aqui
            Debug.Log("💧 Player entrou na água mágica! Voando...");
        }
    }
}
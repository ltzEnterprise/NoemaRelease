using UnityEngine;

public class GravityMonitor : MonoBehaviour
{
    public enum DirecaoGravidade { Normal, Cima, Esquerda, Direita }

    [Header("Configuração")]
    public DirecaoGravidade novaGravidade = DirecaoGravidade.Cima;
    
    [Tooltip("Se marcado, o monitor quebra (fica vermelho) após o primeiro uso.")]
    public bool usarApenasUmaVez = true;

    private float gravidadePadrao = 9.81f; 
    private bool jaUsado = false;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (jaUsado && usarApenasUmaVez) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            foreach (ContactPoint2D contact in collision.contacts)
            {
                Vector2 normalLocal = transform.InverseTransformDirection(contact.normal);

                if (normalLocal.y < -0.5f)
                {
                    AtivarMonitor(collision.gameObject);
                    return;
                }
            }
        }
    }

    void AtivarMonitor(GameObject player)
    {
        if (usarApenasUmaVez)
        {
            jaUsado = true;
            GetComponent<SpriteRenderer>().color = Color.red; 
            Debug.Log("🛑 Monitor usado e desativado.");
        }
        else
        {
            Debug.Log("♻️ Monitor ativado (Reutilizável).");
        }

        MudarGravidade(player);
    }

    void MudarGravidade(GameObject player)
    {
        Vector2 vetorGravidade = Vector2.zero;
        float rotacaoZ = 0f;

        switch (novaGravidade)
        {
            case DirecaoGravidade.Normal:
                vetorGravidade = new Vector2(0, -gravidadePadrao);
                rotacaoZ = 0f;
                break;
            case DirecaoGravidade.Cima:
                vetorGravidade = new Vector2(0, gravidadePadrao); 
                rotacaoZ = 180f; 
                break;
            case DirecaoGravidade.Esquerda:
                vetorGravidade = new Vector2(-gravidadePadrao, 0);
                rotacaoZ = -90f; 
                break;
            case DirecaoGravidade.Direita:
                vetorGravidade = new Vector2(gravidadePadrao, 0);
                rotacaoZ = 90f;
                break;
        }

        Physics2D.gravity = vetorGravidade;
        
        player.transform.rotation = Quaternion.Euler(0, 0, rotacaoZ);
        
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if(rb) rb.linearVelocity = Vector2.zero;

        Debug.Log("🌌 Gravidade alterada para: " + novaGravidade);
    }
}
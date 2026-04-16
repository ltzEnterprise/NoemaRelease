using UnityEngine;
using UnityEngine.InputSystem; 
using System.Collections;

public class PlayerMovement2D : MonoBehaviour
{
    [Header("⚙️ Configurações de Movimento")]
    public float moveSpeed = 8f;
    public float jumpForce = 25f; 
    public float climbSpeed = 6f;
    public float levitateSpeed = 5f;

    [Header("Detecção de Chão")]
    public Transform groundCheckPos;
    public LayerMask groundLayer; 
    public float raioDoPe = 0.4f; 
    
    // Variáveis Internas
    private Rigidbody2D rb;
    private bool isGrounded;
    private float gravidadeOriginal; 
    
    // Estados
    private bool isClimbing = false;
    private bool canClimb = false;
    private bool isLevitating = false;
    
    // --- A VARIÁVEL NOVA AQUI ---
    public bool podeAndar = true; 
    
    // Variáveis de Inversão
    private bool controlesInvertidos = false; 
    private Collider2D ultimaAguaTocada = null; 

    private Coroutine levitationCoroutine; 

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb) 
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation; 
            gravidadeOriginal = rb.gravityScale;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<MagicWaterReverse>() != null)
        {
            ReceberAguaReversa(collision);
        }
    }

    public void ReceberAguaReversa(Collider2D colAgua)
    {
        if (colAgua == ultimaAguaTocada) return; 
        ultimaAguaTocada = colAgua; 
        AlternarControles();     
    }

    void AlternarControles()
    {
        controlesInvertidos = !controlesInvertidos;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr) sr.color = controlesInvertidos ? new Color(0.6f, 0f, 1f) : Color.white;
    }

    public void StartLevitation(float duration) 
    { 
        if (levitationCoroutine != null) StopCoroutine(levitationCoroutine);
        levitationCoroutine = StartCoroutine(LevitateRoutine(duration)); 
    }

    IEnumerator LevitateRoutine(float duration)
    {
        isLevitating = true;
        rb.gravityScale = 0f; 
        rb.linearVelocity = Vector2.zero; 
        yield return new WaitForSeconds(duration);
        CancelarEfeitos();
    }

    public void CancelarEfeitos()
    {
        if (levitationCoroutine != null) StopCoroutine(levitationCoroutine);
        isLevitating = false;
        
        controlesInvertidos = false; 
        ultimaAguaTocada = null; 
        
        if(GetComponent<SpriteRenderer>()) GetComponent<SpriteRenderer>().color = Color.white;
        if (rb != null) rb.gravityScale = gravidadeOriginal;
    }

    void EnterLadder() { canClimb = true; }
    void ExitLadder() { canClimb = false; isClimbing = false; }

    void Update()
    {
        if (Keyboard.current == null) return;

        float x = 0;
        float y = 0;

        // --- SÓ LÊ O TECLADO SE PUDER ANDAR ---
        if (podeAndar)
        {
            if (Keyboard.current.dKey.isPressed) x = 1;
            if (Keyboard.current.aKey.isPressed) x = -1;

            if (controlesInvertidos) x = -x; 

            if (Keyboard.current.wKey.isPressed) y = 1;
            if (Keyboard.current.sKey.isPressed) y = -1;
        }

        if (groundCheckPos)
            isGrounded = Physics2D.OverlapCircle(groundCheckPos.position, raioDoPe, groundLayer);

        if (isLevitating)
        {
            rb.linearVelocity = new Vector2(x * moveSpeed, y * levitateSpeed);
            FlipSprite(x);
            return; 
        }

        if (canClimb && Mathf.Abs(y) > 0.1f && podeAndar) isClimbing = true;
        
        if (isClimbing)
        {
            rb.linearVelocity = transform.up * (y * climbSpeed) + transform.right * (x * moveSpeed);
            if (Keyboard.current.spaceKey.wasPressedThisFrame && podeAndar)
            {
                isClimbing = false;
                Pular();
            }
        }
        else
        {
            Vector2 direcaoMovimento = transform.right * (x * moveSpeed);
            Vector2 velocidadeVertical = transform.up * Vector2.Dot(rb.linearVelocity, transform.up);
            rb.linearVelocity = direcaoMovimento + velocidadeVertical;

            if (Keyboard.current.spaceKey.wasPressedThisFrame && isGrounded && podeAndar)
            {
                Pular();
            }
        }
        FlipSprite(x);
    }

    void Pular()
    {
        Vector2 velocidadeLateral = transform.right * Vector2.Dot(rb.linearVelocity, transform.right);
        rb.linearVelocity = velocidadeLateral;
        rb.AddForce(transform.up * jumpForce, ForceMode2D.Impulse);
    }

    void FlipSprite(float x)
    {
        if (x > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (x < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    private void OnDrawGizmos()
    {
        if (groundCheckPos != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheckPos.position, raioDoPe);
        }
    }
}
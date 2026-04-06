using UnityEngine;

// Igualzinho o seu FPS_Master, mas feito pra voar livremente e gravar
public class FPS_Flycam : MonoBehaviour
{
    [Header("Velocidade do Voo")]
    public float velocidadeAndar = 10f;
    public float velocidadeCorrer = 25f;
    
    [Header("Ajuste Rápido (Scroll do Mouse)")]
    public float sensibilidadeScroll = 5f;

    [Header("Câmera e Mira")]
    public Camera cameraJogador;
    private float sensibilidadeMouse = 2.0f;
    private float rotacaoX = 0f;

    void Awake() 
    {
        if (cameraJogador == null) cameraJogador = Camera.main;

        // A mesma mágica do seu FPS_Master pra não dar snap na visão
        if (cameraJogador != null)
        {
            rotacaoX = cameraJogador.transform.localEulerAngles.x;
            if (rotacaoX > 180f) rotacaoX -= 360f; 
        }
        
        CarregarConfiguracoes();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Mata qualquer peso de gravidade caso você coloque isso num objeto com Rigidbody
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    public void CarregarConfiguracoes()
    {
        // Puxa as mesmas configs do seu jogo original
        sensibilidadeMouse = PlayerPrefs.GetFloat("MouseSensitivity", 2.06f);
        
        float fovSalvo = PlayerPrefs.GetFloat("PlayerFOV", 70f);
        if (cameraJogador != null) 
        {
            cameraJogador.fieldOfView = fovSalvo;
        }
    }

    void Update() 
    {
        if (Time.timeScale == 0) return;

        AjustarVelocidadePeloScroll();
        MoverCamera();
        Voar();
    }

    void AjustarVelocidadePeloScroll()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            velocidadeAndar += scroll * sensibilidadeScroll;
            velocidadeAndar = Mathf.Max(0.5f, velocidadeAndar); // Não deixa zerar
            velocidadeCorrer = velocidadeAndar * 2.5f; // Mantém a proporção da corrida
        }
    }

    void Voar()
    {
        float inputX = 0f;
        float inputZ = 0f;
        float inputY = 0f;

        // WASD Clássico
        if (Input.GetKey(KeyCode.D)) inputX += 1f;
        if (Input.GetKey(KeyCode.A)) inputX -= 1f;
        if (Input.GetKey(KeyCode.W)) inputZ += 1f;
        if (Input.GetKey(KeyCode.S)) inputZ -= 1f;

        // Sobe e Desce (Espaço e Ctrl)
        if (Input.GetKey(KeyCode.Space)) inputY += 1f;
        if (Input.GetKey(KeyCode.LeftControl)) inputY -= 1f;

        // Normaliza pra não ir mais rápido na diagonal
        Vector3 direcaoInput = new Vector3(inputX, inputY, inputZ).normalized;

        float vel = Input.GetKey(KeyCode.LeftShift) ? velocidadeCorrer : velocidadeAndar;

        // A MÁGICA PRA VOAR DIRETO (Baseado no seu próprio código de movimento)
        // Ao invés do CharacterController, a gente empurra a posição baseado pra onde a câmera olha
        Vector3 forward = cameraJogador.transform.forward;
        Vector3 right = cameraJogador.transform.right;
        Vector3 up = Vector3.up; // Subir e descer sempre reto pro teto/chão do mundo

        Vector3 movimentoGeral = (forward * direcaoInput.z) + (right * direcaoInput.x) + (up * direcaoInput.y);

        transform.position += movimentoGeral * vel * Time.deltaTime;
    }

    void MoverCamera() 
    {
        if (cameraJogador == null) return;

        // IDENTICO ao seu FPS_Master
        transform.Rotate(0, Input.GetAxis("Mouse X") * sensibilidadeMouse, 0);
        
        rotacaoX -= Input.GetAxis("Mouse Y") * sensibilidadeMouse;
        rotacaoX = Mathf.Clamp(rotacaoX, -90f, 90f);
        
        cameraJogador.transform.localRotation = Quaternion.Euler(rotacaoX, 0, 0);
    }
}
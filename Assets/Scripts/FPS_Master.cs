using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class FPS_Master : MonoBehaviour
{
    public static FPS_Master Instance;
    public static bool travadoInteracao = false; 

    [Header("Física do Player")]
    public float velocidadeAndar = 5f;
    public float velocidadeCorrer = 10f;
    public float forcaPulo = 6f;
    public float gravidade = 20f;
    
    [Header("Inércia / Deslize")]
    [Tooltip("Quão rápido o boneco chega na velocidade máxima?")]
    public float aceleracao = 10f;
    [Tooltip("Quão rápido o boneco freia quando você solta a tecla?")]
    public float desaceleracao = 15f;
    
    [Header("Segurança")]
    public float alturaLimiteVoid = -50f;
    public Transform pontoDeRespawnCentral;

    [Header("Interação")]
    public Camera cameraJogador;
    public float distanciaInteracao = 3f; 
    public float tempoDeTolerancia = 0.5f; 
    public LayerMask camadasInteracao; 
    public Image miraUI; 

    [Header("Visual")]
    public GameObject modeloDoCorpo; 

    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    
    // --- VARIÁVEIS DE VELOCIDADE ATUAL ---
    private float velocidadeAtualX = 0f;
    private float velocidadeAtualZ = 0f;
    // ---------------------------------
    
    // --- VARIÁVEL DE CONFIGURAÇÃO ---
    private float sensibilidadeMouse = 2.0f; 
    // ---------------------------------

    private float rotacaoX = 0f;
    private Vector3 spawnPosInicial;
    private Transform ultimoObjeto;
    private float timerDesaparecer = 0f;

    void Awake() 
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        controller = GetComponent<CharacterController>();
        spawnPosInicial = (pontoDeRespawnCentral != null) ? pontoDeRespawnCentral.position : transform.position;
        
        // Puxa a câmera logo de cara se você esquecer de arrastar no Inspector
        if (cameraJogador == null) cameraJogador = Camera.main;

        // --- A MÁGICA PRA NÃO "SNAPAR" A VISÃO ---
        if (cameraJogador != null)
        {
            // Pega a rotação que você configurou no Editor antes de dar Play
            rotacaoX = cameraJogador.transform.localEulerAngles.x;
            
            // A Unity as vezes lê -10 graus como 350. Isso aqui converte pra não bugar a trava de 90 graus da cabeça
            if (rotacaoX > 180f) rotacaoX -= 360f; 
        }
        
        CarregarConfiguracoes();
    }

    void Start() 
    {
        CarregarConfiguracoes(); 
    }

    public void CarregarConfiguracoes()
    {
        sensibilidadeMouse = PlayerPrefs.GetFloat("MouseSensitivity", 2.06f);
        
        float fovSalvo = PlayerPrefs.GetFloat("PlayerFOV", 70f);
        if (cameraJogador != null) 
        {
            cameraJogador.fieldOfView = fovSalvo;
        }
        
        Debug.Log($"[PLAYER] Configurações Carregadas: Sensibilidade = {sensibilidadeMouse} | FOV = {fovSalvo}");
    }

    void Update() 
    {
        if (Time.timeScale == 0) return;

        CalcularFisicaEMovimento();

        if (!travadoInteracao)
        {
            MoverCamera();
            ProcessarInteracao();
        }

        if (transform.position.y < alturaLimiteVoid) 
        {
            Teleportar(spawnPosInicial);
        }
    }

    void CalcularFisicaEMovimento()
    {
        if (controller.isGrounded) 
        {
            float inputX = 0f;
            float inputZ = 0f;

            // FORÇA BRUTA NO WASD - IGNORA SETINHAS COMPLETAMENTE
            if (!travadoInteracao)
            {
                if (Input.GetKey(KeyCode.D)) inputX += 1f;
                if (Input.GetKey(KeyCode.A)) inputX -= 1f;
                if (Input.GetKey(KeyCode.W)) inputZ += 1f;
                if (Input.GetKey(KeyCode.S)) inputZ -= 1f;

                // Normaliza pra não andar mais rápido na diagonal
                Vector2 inputNormalizado = new Vector2(inputX, inputZ).normalized;
                inputX = inputNormalizado.x;
                inputZ = inputNormalizado.y;
            }

            float velAlvo = (Input.GetKey(KeyCode.LeftShift) ? velocidadeCorrer : velocidadeAndar);
            float alvoX = inputX * velAlvo;
            float alvoZ = inputZ * velAlvo;

            // --- A MÁGICA DA INÉRCIA (LERP) ---
            // Se o jogador estiver apertando alguma tecla, usa aceleração. Se não, freia usando desaceleração.
            float taxaInterpolacao = (inputX != 0 || inputZ != 0) ? aceleracao : desaceleracao;

            velocidadeAtualX = Mathf.Lerp(velocidadeAtualX, alvoX, Time.deltaTime * taxaInterpolacao);
            velocidadeAtualZ = Mathf.Lerp(velocidadeAtualZ, alvoZ, Time.deltaTime * taxaInterpolacao);
            // ----------------------------------

            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            
            // Aplica as velocidades suaves na direção do corpo
            moveDirection.x = (forward.x * velocidadeAtualZ) + (right.x * velocidadeAtualX);
            moveDirection.z = (forward.z * velocidadeAtualZ) + (right.z * velocidadeAtualX);

            if (!travadoInteracao && Input.GetButton("Jump")) 
                moveDirection.y = forcaPulo;
            else 
                moveDirection.y = -5f; // Mantém a pressão pro chão funcionar direito nas ladeiras
        }
        else
        {
            if (travadoInteracao)
            {
                moveDirection.x = 0;
                moveDirection.z = 0;
                velocidadeAtualX = 0f;
                velocidadeAtualZ = 0f;
            }
        }

        moveDirection.y -= gravidade * Time.deltaTime;
        moveDirection.y = Mathf.Max(moveDirection.y, -20f); 

        controller.Move(moveDirection * Time.deltaTime);
    }

    void MoverCamera() 
    {
        if (cameraJogador == null) return;

        // O Mouse X vira o corpo inteiro pros lados (e ele já respeita a rotação Y inicial que tá no Editor)
        transform.Rotate(0, Input.GetAxis("Mouse X") * sensibilidadeMouse, 0);
        
        // Aplica o movimento do mouse na rotação inicial que a gente salvou lá no Awake
        rotacaoX -= Input.GetAxis("Mouse Y") * sensibilidadeMouse;
        rotacaoX = Mathf.Clamp(rotacaoX, -90f, 90f);
        
        cameraJogador.transform.localRotation = Quaternion.Euler(rotacaoX, 0, 0);
    }
    
    void ProcessarInteracao()
    {
        if (cameraJogador == null) return;

        Ray raio = cameraJogador.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)); 
        RaycastHit hit;
        bool detectou = Physics.Raycast(raio, out hit, distanciaInteracao, camadasInteracao, QueryTriggerInteraction.Ignore);

        if (detectou)
        {
            Transform objAtual = hit.transform;
            timerDesaparecer = tempoDeTolerancia; 

            if (objAtual != ultimoObjeto)
            {
                bool ehParente = (ultimoObjeto != null) && (objAtual.IsChildOf(ultimoObjeto) || ultimoObjeto.IsChildOf(objAtual));
                if (!ehParente)
                {
                    if (ultimoObjeto != null) ultimoObjeto.SendMessageUpwards("AoSair", SendMessageOptions.DontRequireReceiver);
                    objAtual.SendMessageUpwards("AoOlhar", SendMessageOptions.DontRequireReceiver);
                    ultimoObjeto = objAtual;
                }
                else ultimoObjeto = objAtual; 
            }
            if (miraUI) miraUI.color = Color.red;
            
            if (Input.GetKeyDown(KeyCode.E))
            {
                objAtual.SendMessageUpwards("Interagir", SendMessageOptions.DontRequireReceiver);
            }
        }
        else
        {
            if (timerDesaparecer > 0)
            {
                timerDesaparecer -= Time.deltaTime;
                if (ultimoObjeto != null)
                {
                    if (miraUI) miraUI.color = Color.red; 
                    if (Input.GetKeyDown(KeyCode.E)) 
                    {
                         ultimoObjeto.SendMessageUpwards("Interagir", SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
            else LimparVisual(); 
        }
    }

    public void LimparVisual()
    {
        if (ultimoObjeto != null)
        {
            ultimoObjeto.SendMessageUpwards("AoSair", SendMessageOptions.DontRequireReceiver);
            ultimoObjeto = null;
        }
        if (miraUI) miraUI.color = Color.white;
    }

    public void Teleportar(Vector3 novaPosicao) 
    {
        bool estavaAtivo = controller.enabled;
        controller.enabled = false; 
        transform.position = novaPosicao + Vector3.up * 0.1f;
        Physics.SyncTransforms();
        moveDirection = Vector3.zero; 
        velocidadeAtualX = 0f;
        velocidadeAtualZ = 0f;
        controller.enabled = estavaAtivo; 
    }

    public void AlterarEstadoJogador(bool travar, bool mostrarCursor)
    {
        travadoInteracao = travar; 
        if (travar)
        {
            moveDirection.x = 0;
            moveDirection.z = 0;
            velocidadeAtualX = 0f;
            velocidadeAtualZ = 0f;
        }
        if (mostrarCursor) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        else { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    public void FicarInvisivelMasFisico(bool invisivel)
    {
        if (modeloDoCorpo != null) modeloDoCorpo.SetActive(!invisivel);
        else
        {
            MeshRenderer mesh = GetComponent<MeshRenderer>();
            if (mesh) mesh.enabled = !invisivel;
        }
    }
}
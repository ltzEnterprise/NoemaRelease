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
            float inputX = travadoInteracao ? 0 : Input.GetAxis("Horizontal");
            float inputZ = travadoInteracao ? 0 : Input.GetAxis("Vertical");
            float vel = (Input.GetKey(KeyCode.LeftShift) ? velocidadeCorrer : velocidadeAndar);

            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            
            float curSpeedX = vel * inputX;
            float curSpeedY = vel * inputZ;
            
            moveDirection.x = (forward.x * curSpeedY) + (right.x * curSpeedX);
            moveDirection.z = (forward.z * curSpeedY) + (right.z * curSpeedX);

            if (!travadoInteracao && Input.GetButton("Jump")) 
                moveDirection.y = forcaPulo;
            else 
                moveDirection.y = -5f;
        }
        else
        {
            if (travadoInteracao)
            {
                moveDirection.x = 0;
                moveDirection.z = 0;
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
        controller.enabled = estavaAtivo; 
    }

    public void AlterarEstadoJogador(bool travar, bool mostrarCursor)
    {
        travadoInteracao = travar; 
        if (travar)
        {
            moveDirection.x = 0;
            moveDirection.z = 0;
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
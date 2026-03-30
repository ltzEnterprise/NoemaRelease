using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsPickupObject : MonoBehaviour
{
    [Header("--- CONEXÕES ---")]
    [Tooltip("Arraste a Câmera do Jogador aqui pra garantir")]
    public Transform cameraPlayer; 

    [Header("--- CONFIGURAÇÃO ESTILO PORTAL ---")]
    public float distanciaSegurar = 2.5f; 
    public float forcaMovimento = 15f;    
    public float forcaArremesso = 10f; 
    public float distanciaMaximaQuebra = 4f; 

    [Header("--- UI ---")]
    public GameObject textoInteragir; 

    // Variáveis Internas
    private bool estaSegurando = false;
    private Rigidbody rb;
    private int layerOriginal;
    
    // Controle de Quebra e Bug do Clique
    private bool jaChegouNaMao = false;
    private float tempoQuePegou = 0f;

    // Unity 6 Physics
    private float originalDamping; 
    private float originalAngularDamping;
    private bool gravidadeOriginal;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        layerOriginal = gameObject.layer;
        
        originalDamping = rb.linearDamping; 
        originalAngularDamping = rb.angularDamping;
        gravidadeOriginal = rb.useGravity;

        if (textoInteragir) textoInteragir.SetActive(false);
        
        if (cameraPlayer == null && Camera.main != null) 
            cameraPlayer = Camera.main.transform;
    }

    public void AoOlhar()
    {
        if (estaSegurando) return; 
        if (textoInteragir && !textoInteragir.activeSelf) textoInteragir.SetActive(true);
    }

    public void AoSair()
    {
        if (textoInteragir) textoInteragir.SetActive(false);
    }

    public void Interagir()
    {
        if (estaSegurando) Soltar();
        else Pegar();
    }

    void Update()
    {
        // Só deixa arremessar se já passou 0.2 segundos desde que pegou. Mata o bug do double-click.
        if (estaSegurando && Time.time > tempoQuePegou + 0.2f && Input.GetMouseButtonDown(0))
        {
            Arremessar();
        }
    }

    void FixedUpdate()
    {
        if (estaSegurando && cameraPlayer != null)
        {
            MoverEstiloPortal();
        }
    }

    void Pegar()
    {
        if (cameraPlayer == null) 
        {
            Debug.LogError("Cade a câmera? Arrasta ela pro script do cubo no Inspector!");
            return;
        }

        estaSegurando = true;
        jaChegouNaMao = false; 
        tempoQuePegou = Time.time; 
        
        rb.useGravity = false;
        rb.linearDamping = 10f;          
        rb.angularDamping = 10f; 

        if (textoInteragir) textoInteragir.SetActive(false);
    }

    void MoverEstiloPortal()
    {
        Vector3 posicaoAlvo = cameraPlayer.position + (cameraPlayer.forward * distanciaSegurar);
        Vector3 direcao = posicaoAlvo - rb.position;
        float distancia = direcao.magnitude;

        if (!jaChegouNaMao)
        {
            if (distancia < 0.5f) jaChegouNaMao = true; 
        }
        else
        {
            if (distancia > distanciaMaximaQuebra)
            {
                Soltar();
                return;
            }
        }

        rb.linearVelocity = direcao * forcaMovimento;

        Quaternion rotacaoAlvo = Quaternion.Euler(0, cameraPlayer.eulerAngles.y, 0);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, rotacaoAlvo, 15f * Time.fixedDeltaTime));
    }

    public void Soltar()
    {
        estaSegurando = false;
        jaChegouNaMao = false;

        rb.useGravity = gravidadeOriginal;
        rb.linearDamping = originalDamping;
        rb.angularDamping = originalAngularDamping;
    }

    void Arremessar()
    {
        Transform camRef = cameraPlayer;
        Soltar(); 

        if (camRef != null)
        {
            rb.AddForce(camRef.forward * forcaArremesso, ForceMode.Impulse);
        }
    }
}
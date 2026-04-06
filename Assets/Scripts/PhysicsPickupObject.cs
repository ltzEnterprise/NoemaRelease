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

    [Header("--- SEGURANÇA (BLOQUEIO) ---")]
    [Tooltip("Quais Layers vão bloquear a sua mão e o texto?")]
    public LayerMask camadasBloqueadoras = Physics.DefaultRaycastLayers;

    [Header("--- UI ---")]
    public GameObject textoInteragir; 

    // Variáveis Internas
    private bool estaSegurando = false;
    private bool sendoOlhado = false; // <-- Nova variável de controle visual
    private Rigidbody rb;
    private Collider meuCollider;
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
        meuCollider = GetComponent<Collider>();
        layerOriginal = gameObject.layer;
        
        originalDamping = rb.linearDamping; 
        originalAngularDamping = rb.angularDamping;
        gravidadeOriginal = rb.useGravity;

        if (textoInteragir) textoInteragir.SetActive(false);
        
        if (cameraPlayer == null && Camera.main != null) 
            cameraPlayer = Camera.main.transform;
    }

    // --- FUNÇÃO QUE CHECA A PAREDE MAGNÉTICA ---
    bool CaminhoEstaBloqueado()
    {
        if (cameraPlayer == null || meuCollider == null) return false;

        // Estica uma linha da câmera até o centro do cubo
        if (Physics.Linecast(cameraPlayer.position, meuCollider.bounds.center, out RaycastHit hit, camadasBloqueadoras, QueryTriggerInteraction.Ignore))
        {
            // Se bateu em algo que NÃO é o cubo e NÃO é o jogador, tá bloqueado.
            if (hit.collider.gameObject != this.gameObject && hit.collider.transform.root != cameraPlayer.root)
            {
                return true; 
            }
        }
        return false;
    }

    public void AoOlhar()
    {
        if (estaSegurando) return; 
        sendoOlhado = true; // Avisa o Update que o player tá com a mira aqui
    }

    public void AoSair()
    {
        sendoOlhado = false; // Player tirou a mira, desliga tudo
        if (textoInteragir) textoInteragir.SetActive(false);
    }

    public void Interagir()
    {
        if (estaSegurando) 
        {
            Soltar();
        }
        else 
        {
            // O bloqueio definitivo na hora de apertar 'E'
            if (!CaminhoEstaBloqueado())
            {
                Pegar();
            }
        }
    }

    void Update()
    {
        // --- CONTROLE CONTÍNUO DO TEXTO ---
        if (sendoOlhado && !estaSegurando)
        {
            bool bloqueado = CaminhoEstaBloqueado();
            
            if (textoInteragir)
            {
                // Liga e desliga o texto de forma burra e rápida acompanhando o obstáculo
                if (bloqueado && textoInteragir.activeSelf) textoInteragir.SetActive(false);
                else if (!bloqueado && !textoInteragir.activeSelf) textoInteragir.SetActive(true);
            }
        }

        // Arremesso
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
        sendoOlhado = false; // Desliga o UI porque já tá na mão
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
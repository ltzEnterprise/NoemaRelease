using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsPickupObject : MonoBehaviour
{
    [Header("--- SAVE SYSTEM ---")]
    [Tooltip("ID único desse cubo/objeto. Ex: Cubo_Botao_Principal_01")]
    public string uniqueID = "Cubo_Fisico_01";

    [Tooltip("Se ligado, o cubo só salva posição quando estiver pressionando um botão.")]
    public bool salvarPosicaoSomenteSeEstiverEmBotao = true;

    [Tooltip("Se ligado, ao carregar e NÃO houver save válido em botão, o cubo volta para a posição original da cena.")]
    public bool voltarParaPosicaoOriginalAoCarregar = true;

    [Header("--- CONEXÕES ---")]
    [Tooltip("Arraste a câmera do jogador aqui. Se vazio, tenta Camera.main.")]
    public Transform cameraPlayer;

    [Header("--- CONFIGURAÇÃO ESTILO PORTAL ---")]
    public float distanciaSegurar = 2.5f;

    [Tooltip("Velocidade de suavização enquanto segura. Maior = mais rápido.")]
    public float velocidadeSegurar = 18f;

    [Tooltip("Velocidade de rotação enquanto segura.")]
    public float velocidadeRotacao = 16f;

    [Tooltip("Força apenas do arremesso com botão esquerdo. Soltar com E NÃO usa impulso.")]
    public float forcaArremesso = 10f;

    public float distanciaMaximaQuebra = 4f;

    [Header("--- SEGURANÇA / BLOQUEIO ---")]
    public LayerMask camadasBloqueadoras = Physics.DefaultRaycastLayers;

    [Header("--- UI ---")]
    public GameObject textoInteragir;

    [Header("--- DEBUG ---")]
    public bool debugLogs = false;

    private bool estaSegurando = false;
    private bool sendoOlhado = false;
    private bool jaChegouNaMao = false;

    private Rigidbody rb;
    private Collider meuCollider;

    private Vector3 posicaoOriginalMundo;
    private Quaternion rotacaoOriginalMundo;

    private float tempoQuePegou = 0f;

    private float originalDamping;
    private float originalAngularDamping;
    private bool gravidadeOriginal;
    private bool kinematicOriginal;
    private RigidbodyInterpolation interpolacaoOriginal;
    private CollisionDetectionMode collisionOriginal;

    private Vector3 velocidadeSuavizacao = Vector3.zero;

    private Coroutine rotinaInicializar;

    private string ChaveEmBotao
    {
        get { return uniqueID + "_EmBotao"; }
    }

    private string ChavePosX
    {
        get { return uniqueID + "_PosX"; }
    }

    private string ChavePosY
    {
        get { return uniqueID + "_PosY"; }
    }

    private string ChavePosZ
    {
        get { return uniqueID + "_PosZ"; }
    }

    private string ChaveRotX
    {
        get { return uniqueID + "_RotX"; }
    }

    private string ChaveRotY
    {
        get { return uniqueID + "_RotY"; }
    }

    private string ChaveRotZ
    {
        get { return uniqueID + "_RotZ"; }
    }

    private string ChaveRotW
    {
        get { return uniqueID + "_RotW"; }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        meuCollider = GetComponent<Collider>();

        posicaoOriginalMundo = transform.position;
        rotacaoOriginalMundo = transform.rotation;

        if (rb != null)
        {
            originalDamping = rb.linearDamping;
            originalAngularDamping = rb.angularDamping;
            gravidadeOriginal = rb.useGravity;
            kinematicOriginal = rb.isKinematic;
            interpolacaoOriginal = rb.interpolation;
            collisionOriginal = rb.collisionDetectionMode;

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }

    void Start()
    {
        if (textoInteragir)
            textoInteragir.SetActive(false);

        if (cameraPlayer == null && Camera.main != null)
            cameraPlayer = Camera.main.transform;

        rotinaInicializar = StartCoroutine(InicializarSaveSeguro());
    }

    private IEnumerator InicializarSaveSeguro()
    {
        if (PersistenciaManager.Instance != null)
        {
            yield return new WaitUntil(() =>
                PersistenciaManager.Instance.DadosProntosParaUso &&
                !PersistenciaManager.Instance.EstaCarregando
            );

            CarregarEstadoInicial();
        }
        else
        {
            if (voltarParaPosicaoOriginalAoCarregar)
                AplicarTransformOriginal();
        }

        rotinaInicializar = null;
    }

    private void CarregarEstadoInicial()
    {
        if (PersistenciaManager.Instance == null || string.IsNullOrEmpty(uniqueID))
            return;

        bool estavaEmBotao = PersistenciaManager.Instance.ObterEstado(ChaveEmBotao, false);

        if (estavaEmBotao && PersistenciaManager.Instance.TemFloat(ChavePosX))
        {
            Vector3 pos = new Vector3(
                PersistenciaManager.Instance.ObterFloat(ChavePosX),
                PersistenciaManager.Instance.ObterFloat(ChavePosY),
                PersistenciaManager.Instance.ObterFloat(ChavePosZ)
            );

            Quaternion rot = new Quaternion(
                PersistenciaManager.Instance.ObterFloat(ChaveRotX),
                PersistenciaManager.Instance.ObterFloat(ChaveRotY),
                PersistenciaManager.Instance.ObterFloat(ChaveRotZ),
                PersistenciaManager.Instance.ObterFloat(ChaveRotW, 1f)
            );

            AplicarTransform(pos, rot);

            if (debugLogs)
                Debug.Log("[PhysicsPickupObject] Carregou posição salva em botão: " + uniqueID);

            return;
        }

        if (voltarParaPosicaoOriginalAoCarregar)
        {
            AplicarTransformOriginal();

            if (debugLogs)
                Debug.Log("[PhysicsPickupObject] Voltou para posição original: " + uniqueID);
        }
    }

    private bool CaminhoEstaBloqueado()
    {
        if (cameraPlayer == null || meuCollider == null)
            return false;

        Vector3 origem = cameraPlayer.position;
        Vector3 destino = meuCollider.bounds.center;

        if (Physics.Linecast(origem, destino, out RaycastHit hit, camadasBloqueadoras, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == null)
                return false;

            if (hit.collider.gameObject == gameObject)
                return false;

            if (hit.collider.transform.IsChildOf(transform))
                return false;

            if (cameraPlayer != null && hit.collider.transform.root == cameraPlayer.root)
                return false;

            return true;
        }

        return false;
    }

    public void AoOlhar()
    {
        if (estaSegurando)
            return;

        sendoOlhado = true;
    }

    public void AoSair()
    {
        sendoOlhado = false;

        if (textoInteragir)
            textoInteragir.SetActive(false);
    }

    public void Interagir()
    {
        if (estaSegurando)
        {
            Soltar(false);
            return;
        }

        if (!CaminhoEstaBloqueado())
            Pegar();
    }

    void Update()
    {
        if (sendoOlhado && !estaSegurando)
        {
            bool bloqueado = CaminhoEstaBloqueado();

            if (textoInteragir)
            {
                bool deveMostrar = !bloqueado;

                if (textoInteragir.activeSelf != deveMostrar)
                    textoInteragir.SetActive(deveMostrar);
            }
        }

        if (estaSegurando && Time.time > tempoQuePegou + 0.2f && Input.GetMouseButtonDown(0))
        {
            Arremessar();
        }
    }

    void FixedUpdate()
    {
        if (estaSegurando && cameraPlayer != null)
            MoverObjetoSegurado();
    }

    void Pegar()
    {
        if (cameraPlayer == null)
        {
            Debug.LogError("[PhysicsPickupObject] Câmera não encontrada. Arraste a câmera no Inspector.");
            return;
        }

        estaSegurando = true;
        sendoOlhado = false;
        jaChegouNaMao = false;
        tempoQuePegou = Time.time;
        velocidadeSuavizacao = Vector3.zero;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.useGravity = false;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;

        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        rb.isKinematic = true;

        if (textoInteragir)
            textoInteragir.SetActive(false);
    }

    void MoverObjetoSegurado()
    {
        Vector3 posicaoAlvo = cameraPlayer.position + cameraPlayer.forward * distanciaSegurar;
        float distancia = Vector3.Distance(rb.position, posicaoAlvo);

        if (!jaChegouNaMao)
        {
            if (distancia < 0.35f)
                jaChegouNaMao = true;
        }
        else
        {
            if (distancia > distanciaMaximaQuebra)
            {
                Soltar(false);
                return;
            }
        }

        Vector3 novaPosicao = Vector3.SmoothDamp(
            rb.position,
            posicaoAlvo,
            ref velocidadeSuavizacao,
            1f / Mathf.Max(0.01f, velocidadeSegurar),
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        rb.MovePosition(novaPosicao);

        Quaternion rotacaoAlvo = Quaternion.Euler(0f, cameraPlayer.eulerAngles.y, 0f);

        Quaternion novaRotacao = Quaternion.Slerp(
            rb.rotation,
            rotacaoAlvo,
            velocidadeRotacao * Time.fixedDeltaTime
        );

        rb.MoveRotation(novaRotacao);
    }

    public void Soltar()
    {
        Soltar(false);
    }

    public void Soltar(bool arremessar)
    {
        estaSegurando = false;
        jaChegouNaMao = false;
        velocidadeSuavizacao = Vector3.zero;

        rb.isKinematic = kinematicOriginal;
        rb.useGravity = gravidadeOriginal;
        rb.linearDamping = originalDamping;
        rb.angularDamping = originalAngularDamping;
        rb.interpolation = interpolacaoOriginal;
        rb.collisionDetectionMode = collisionOriginal;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        if (arremessar && cameraPlayer != null)
        {
            rb.AddForce(cameraPlayer.forward * forcaArremesso, ForceMode.Impulse);
        }

        SalvarOuLimparPosicaoConformeBotao();
    }

    void Arremessar()
    {
        Soltar(true);
    }

    private void AplicarTransformOriginal()
    {
        AplicarTransform(posicaoOriginalMundo, rotacaoOriginalMundo);
    }

    private void AplicarTransform(Vector3 pos, Quaternion rot)
    {
        if (rb == null)
        {
            transform.position = pos;
            transform.rotation = rot;
            return;
        }

        bool estavaKinematic = rb.isKinematic;

        rb.isKinematic = true;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        transform.position = pos;
        transform.rotation = rot;

        Physics.SyncTransforms();

        rb.isKinematic = estavaKinematic;
    }

    private void SalvarOuLimparPosicaoConformeBotao()
    {
        if (PersistenciaManager.Instance == null || string.IsNullOrEmpty(uniqueID))
            return;

        bool estaEmBotao = EstaPressionandoAlgumBotao();

        if (salvarPosicaoSomenteSeEstiverEmBotao && !estaEmBotao)
        {
            PersistenciaManager.Instance.RegistrarEstado(ChaveEmBotao, false);
            PersistenciaManager.Instance.SalvarTudo(true);

            if (debugLogs)
                Debug.Log("[PhysicsPickupObject] Não estava em botão. Save de posição marcado como inválido: " + uniqueID);

            return;
        }

        PersistenciaManager.Instance.RegistrarEstado(ChaveEmBotao, estaEmBotao);
        PersistenciaManager.Instance.SalvarFloat(ChavePosX, transform.position.x);
        PersistenciaManager.Instance.SalvarFloat(ChavePosY, transform.position.y);
        PersistenciaManager.Instance.SalvarFloat(ChavePosZ, transform.position.z);

        PersistenciaManager.Instance.SalvarFloat(ChaveRotX, transform.rotation.x);
        PersistenciaManager.Instance.SalvarFloat(ChaveRotY, transform.rotation.y);
        PersistenciaManager.Instance.SalvarFloat(ChaveRotZ, transform.rotation.z);
        PersistenciaManager.Instance.SalvarFloat(ChaveRotW, transform.rotation.w);

        PersistenciaManager.Instance.SalvarTudo(true);

        if (debugLogs)
            Debug.Log("[PhysicsPickupObject] Posição salva. Em botão=" + estaEmBotao + " | " + uniqueID);
    }

    private bool EstaPressionandoAlgumBotao()
    {
        if (meuCollider == null)
            return false;

        PhysicalButton[] botoes = Object.FindObjectsByType<PhysicalButton>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        if (botoes == null || botoes.Length == 0)
            return false;

        Bounds boundsObjeto = meuCollider.bounds;

        foreach (PhysicalButton botao in botoes)
        {
            if (botao == null)
                continue;

            Vector3 centro = botao.transform.TransformPoint(botao.sensorCenter);
            Vector3 metade = botao.sensorSize * 0.5f;

            Collider[] hits = Physics.OverlapBox(
                centro,
                metade,
                botao.transform.rotation,
                ~0,
                QueryTriggerInteraction.Ignore
            );

            foreach (Collider hit in hits)
            {
                if (hit == null)
                    continue;

                if (hit == meuCollider || hit.transform.IsChildOf(transform) || transform.IsChildOf(hit.transform))
                    return true;

                if (hit.attachedRigidbody != null && rb != null && hit.attachedRigidbody == rb)
                    return true;

                if (boundsObjeto.Intersects(hit.bounds) && hit.attachedRigidbody == rb)
                    return true;
            }
        }

        return false;
    }

    private void OnDisable()
    {
        if (estaSegurando && rb != null)
            Soltar(false);

        SalvarOuLimparPosicaoConformeBotao();
    }

    private void OnApplicationQuit()
    {
        SalvarOuLimparPosicaoConformeBotao();
    }

    void OnDrawGizmosSelected()
    {
        if (cameraPlayer == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(cameraPlayer.position + cameraPlayer.forward * distanciaSegurar, 0.15f);
    }
}
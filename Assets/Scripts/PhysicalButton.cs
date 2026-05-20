using UnityEngine;

public class PhysicalButton : MonoBehaviour
{
    [Header("CONNECTION")]
    [Tooltip("Arraste aqui a porta específica que este botão controla.")]
    public ReceiverDoor targetDoor;

    [Header("Visuals")]
    public Transform movingPart;

    [Header("Config")]
    public float pressDistance = 0.1f;
    public float speed = 5f;

    [Header("Sensor Local")]
    public Vector3 sensorCenter = new Vector3(0, 0.5f, 0);
    public Vector3 sensorSize = new Vector3(0.8f, 0.5f, 0.8f);

    [Header("Debug")]
    public bool debugLogs = false;

    private Vector3 originalPos;
    private Vector3 pressedPos;

    private bool isPressed = false;
    private bool lastPressedState = false;
    private bool inicializado = false;

    void Start()
    {
        if (movingPart == null)
        {
            if (transform.childCount > 0)
                movingPart = transform.GetChild(0);
            else
                movingPart = transform;
        }

        originalPos = movingPart.localPosition;
        pressedPos = originalPos - new Vector3(0f, pressDistance, 0f);

        inicializado = true;

        AtualizarPortaSeMudou(true);
    }

    void Update()
    {
        if (!inicializado) return;

        isPressed = DetectarPressao();

        Vector3 targetPos = isPressed ? pressedPos : originalPos;
        movingPart.localPosition = Vector3.Lerp(
            movingPart.localPosition,
            targetPos,
            Time.deltaTime * speed
        );

        AtualizarPortaSeMudou(false);
    }

    private bool DetectarPressao()
    {
        Vector3 centroMundo = transform.TransformPoint(sensorCenter);
        Vector3 metade = sensorSize * 0.5f;

        Collider[] hits = Physics.OverlapBox(
            centroMundo,
            metade,
            transform.rotation,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits)
        {
            if (hit == null) continue;

            Transform t = hit.transform;

            if (t == transform) continue;
            if (movingPart != null && t == movingPart) continue;

            if (t.IsChildOf(transform)) continue;

            bool ehPlayer =
                hit.GetComponent<FPS_Master>() != null ||
                hit.GetComponentInParent<FPS_Master>() != null ||
                hit.CompareTag("Player");

            bool temFisica = hit.attachedRigidbody != null;

            if (ehPlayer || temFisica)
            {
                if (debugLogs)
                    Debug.Log("[PhysicalButton] Pressionado por: " + hit.name);

                return true;
            }
        }

        return false;
    }

    private void AtualizarPortaSeMudou(bool forcar)
    {
        if (targetDoor == null)
        {
            if (debugLogs)
                Debug.LogWarning("[PhysicalButton] targetDoor está vazio no botão: " + gameObject.name);

            return;
        }

        if (!forcar && isPressed == lastPressedState)
            return;

        lastPressedState = isPressed;
        targetDoor.SetState(isPressed);

        if (debugLogs)
            Debug.Log("[PhysicalButton] Porta " + targetDoor.name + " recebeu estado: " + isPressed);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.35f);

        Matrix4x4 matrizAntiga = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(
            transform.TransformPoint(sensorCenter),
            transform.rotation,
            Vector3.one
        );

        Gizmos.DrawCube(Vector3.zero, sensorSize);
        Gizmos.matrix = matrizAntiga;
    }
}
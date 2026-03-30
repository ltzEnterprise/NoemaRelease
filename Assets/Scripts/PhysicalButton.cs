using UnityEngine;

public class PhysicalButton : MonoBehaviour
{
    [Header("CONNECTION")]
    public ReceiverDoor targetDoor; 

    [Header("Visuals")]
    public Transform movingPart; 
    
    [Header("Config")]
    public float pressDistance = 0.1f;
    public float speed = 5f;
    
    public Vector3 sensorCenter = new Vector3(0, 0.5f, 0);
    public Vector3 sensorSize = new Vector3(0.8f, 0.5f, 0.8f);

    private Vector3 originalPos;
    private Vector3 pressedPos;
    private bool isPressed = false;

    void Start()
    {
        if (movingPart == null) movingPart = transform.GetChild(0);
        
        originalPos = movingPart.localPosition;
        pressedPos = originalPos - new Vector3(0, pressDistance, 0); 
    }

    void Update()
    {
        // Escaneia TUDO o que tá na área do botão (sem precisar de LayerMask)
        Collider[] hits = Physics.OverlapBox(transform.position + sensorCenter, sensorSize / 2, Quaternion.identity);
        
        isPressed = false;
        foreach (var hit in hits)
        {
            // Ignora o próprio botão (pra ele não apertar a si mesmo)
            if (hit.transform == transform || hit.transform == movingPart) continue;
            
            // Ignora triggers invisíveis (tipo áreas de áudio)
            if (hit.isTrigger) continue;

            // Se for um objeto com física (Cubo) OU for o Player, ele ativa!
            if (hit.attachedRigidbody != null || hit.GetComponent<FPS_Master>() != null || hit.CompareTag("Player"))
            {
                isPressed = true;
                break; 
            }
        }

        // Animação do botão afundando e subindo
        Vector3 targetPos = isPressed ? pressedPos : originalPos;
        movingPart.localPosition = Vector3.Lerp(movingPart.localPosition, targetPos, Time.deltaTime * speed);

        // Manda o sinal pra porta
        if (targetDoor != null)
        {
            targetDoor.SetState(isPressed);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1, 1, 0, 0.5f);
        Gizmos.DrawCube(transform.position + sensorCenter, sensorSize);
    }
}
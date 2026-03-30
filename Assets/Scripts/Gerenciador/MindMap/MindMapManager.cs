using UnityEngine;

public class MindMapManager : MonoBehaviour
{
    [Header("Referências da UI")]
    public GameObject mindMapPanel;
    public RectTransform mapContent;

    [Header("Configurações de Zoom")]
    public float zoomSpeed = 0.1f;
    public float minZoom = 0.5f;
    public float maxZoom = 2f;

    private bool isOpen = false;

    void Start()
    {
        mindMapPanel.SetActive(false);
    }

    void Update()
    {
        int difficulty = PlayerPrefs.GetInt("DificuldadeJogo", 1); // 0 = Easily, 1 = Normal

        // Só abre se estiver no Easily
        if (Input.GetKeyDown(KeyCode.Tab) && difficulty == 0)
        {
            ToggleMap();
        }

        // Se o mapa estiver aberto e o cara trocar a dificuldade pro Normal, fecha na cara dele
        if (isOpen && difficulty != 0)
        {
            ToggleMap();
        }

        // Aplica o Zoom da rodinha do mouse se estiver aberto
        if (isOpen)
        {
            HandleZoom();
        }
    }

    void ToggleMap()
    {
        isOpen = !isOpen;
        mindMapPanel.SetActive(isOpen);

        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (FPS_Master.Instance != null) FPS_Master.travadoInteracao = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (FPS_Master.Instance != null) FPS_Master.travadoInteracao = false;
        }
    }

    void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            Vector3 scale = mapContent.localScale;
            scale += Vector3.one * scroll * zoomSpeed;

            scale.x = Mathf.Clamp(scale.x, minZoom, maxZoom);
            scale.y = Mathf.Clamp(scale.y, minZoom, maxZoom);
            scale.z = 1f;

            mapContent.localScale = scale;
        }
    }
}
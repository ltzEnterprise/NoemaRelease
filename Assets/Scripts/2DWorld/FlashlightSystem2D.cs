using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Tilemaps;

public class FlashlightSystem2D : MonoBehaviour
{
    [Header("Geral")]
    public Camera mainCamera;

    [Header("--- INTEGRAÇÃO COM MANAGER ---")]
    public ManagerMundo2D managerMundo; 
    [Tooltip("O ID/Índice da fase onde o tutorial deve aparecer (ex: 9)")]
    public int idFaseEscura = 9; 

    [Header("Fog")]
    public GameObject overlayEscuridao; 

    [Header("Luzes")]
    public GameObject mascaraLanternaObj; 
    public SpriteMask spriteMaskComponente;
    
    public SpriteRenderer luzVisualBranca; 
    public SpriteRenderer luzVisualUV;     

    [Header("Tamanhos")]
    public float escalaLuzBranca = 1.0f;
    public float escalaLuzUV = 2.5f;

    [Header("Chão (UV)")]
    public GameObject tilemapEscondidoObj; 
    private TilemapRenderer tilemapRenderer; 

    [Header("UI Texto")]
    public CanvasGroup textoTutorialCanvasGroup;
    public float tempoParaAparecerTexto = 5.0f;

    private bool sistemaAtivo = false;
    private int modoAtual = 0; 
    private float timerSemLuz = 0f;

    void Awake()
    {
        if (tilemapEscondidoObj)
            tilemapRenderer = tilemapEscondidoObj.GetComponent<TilemapRenderer>();
    }

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        
        if (textoTutorialCanvasGroup) 
        {
            textoTutorialCanvasGroup.alpha = 0f;
            textoTutorialCanvasGroup.blocksRaycasts = false; 
        }
        if (tilemapEscondidoObj) tilemapEscondidoObj.SetActive(true);
        if (tilemapRenderer) tilemapRenderer.enabled = false;
        AtualizarGraficos(); 
    }

    public void AtivarSistema(bool ativar)
    {
        sistemaAtivo = ativar;
        modoAtual = 0; 
        timerSemLuz = 0f;

        if (overlayEscuridao) 
        {
            overlayEscuridao.SetActive(ativar);
        }
        
        AtualizarGraficos();
    }

    void LateUpdate()
    {
        if (!sistemaAtivo) return;

        MoverLanterna();
        LerInputMouse();
        GerenciarLogicaTexto();
    }

    void MoverLanterna()
    {
        if (mainCamera == null || mascaraLanternaObj == null) return;
        Plane planoDeJogo = new Plane(Vector3.back, Vector3.zero); 
        Ray raio = mainCamera.ScreenPointToRay(Input.mousePosition);

        float distancia;
        if (planoDeJogo.Raycast(raio, out distancia))
        {
            Vector3 pontoDeBatida = raio.GetPoint(distancia);
            mascaraLanternaObj.transform.position = pontoDeBatida;
        }
    }

    void LerInputMouse()
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            modoAtual++;
            if (modoAtual > 2) modoAtual = 0; 

            timerSemLuz = 0f;
            if (textoTutorialCanvasGroup) textoTutorialCanvasGroup.alpha = 0f;

            AtualizarGraficos();
        }
    }

    void GerenciarLogicaTexto()
    {
        // Agora lê a variável pública correta direto do Manager
        bool estaNaFaseEscura = (managerMundo != null && managerMundo.currentLevel == idFaseEscura);

        if (modoAtual == 0 && estaNaFaseEscura)
        {
            timerSemLuz += Time.deltaTime;
            if (timerSemLuz >= tempoParaAparecerTexto)
            {
                if (textoTutorialCanvasGroup)
                    textoTutorialCanvasGroup.alpha += Time.deltaTime * 2f; 
            }
        }
        else
        {
            timerSemLuz = 0f;
            if (textoTutorialCanvasGroup) textoTutorialCanvasGroup.alpha = 0f;
        }
    }

    void AtualizarGraficos()
    {
        if (modoAtual == 0) 
        {
            if (spriteMaskComponente) spriteMaskComponente.enabled = false;
            if (luzVisualBranca) luzVisualBranca.gameObject.SetActive(false);
            if (luzVisualUV) luzVisualUV.gameObject.SetActive(false);
            
            if (tilemapRenderer) tilemapRenderer.enabled = false;
        }
        else if (modoAtual == 1) 
        {
            if (spriteMaskComponente)
            {
                spriteMaskComponente.enabled = true;
                mascaraLanternaObj.transform.localScale = new Vector3(escalaLuzBranca, escalaLuzBranca, 1f);
            }
            if (luzVisualBranca) luzVisualBranca.gameObject.SetActive(true);
            if (luzVisualUV) luzVisualUV.gameObject.SetActive(false);
            
            if (tilemapRenderer) tilemapRenderer.enabled = false;
        }
        else if (modoAtual == 2) 
        {
            if (spriteMaskComponente)
            {
                spriteMaskComponente.enabled = true;
                mascaraLanternaObj.transform.localScale = new Vector3(escalaLuzUV, escalaLuzUV, 1f);
            }
            if (luzVisualBranca) luzVisualBranca.gameObject.SetActive(false);
            if (luzVisualUV) luzVisualUV.gameObject.SetActive(true);
            if (tilemapRenderer) tilemapRenderer.enabled = true;
        }
    }
}
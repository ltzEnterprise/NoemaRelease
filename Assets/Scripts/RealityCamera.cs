using UnityEngine;
using UnityEngine.UI; 
using System.Collections;
using System.Collections.Generic;

public class RealityCamera : MonoBehaviour
{
    public static RealityCamera Instance;

    [Header("--- ID DO ITEM ---")]
    public int idCamera = 6; 

    [Header("--- REFERÊNCIAS ---")]
    public GameObject modeloCameraMao; 
    public GameObject hudCamera; 
    public Camera cameraPlayer;        

    [Header("--- EFEITOS ---")]
    public Image flashBranco;          
    public GameObject luzNormalObj;    
    public GameObject luzUVObj;        
    public AudioSource audioSource;
    
    [Header("--- UI DE AJUDA ---")]
    public GameObject textoAjudaEntrar; 
    public GameObject textoAjudaLuz;    

    [Header("--- CONFIGURAÇÃO DA FOTO (SPHERECAST) ---")]
    public float alcanceMaximo = 5000f; 
    public float raioDaMira = 0.8f; 
    public LayerMask layerParede; 
    public LayerMask layerObjetosFoto;

    [Header("--- COOLDOWN ---")]
    public float cooldownFoto = 0.5f;
    private float proximaFoto = 0f;

    [Header("--- INPUTS ---")]
    public KeyCode teclaEntrarSair = KeyCode.G; 
    public KeyCode teclaLuz = KeyCode.F;        
    public KeyCode teclaFoto = KeyCode.Mouse0;
    
    public KeyCode teclaZoomIn = KeyCode.RightBracket; 
    public KeyCode teclaZoomOut = KeyCode.LeftBracket; 

    [Header("--- ZOOM ---")]
    public float zoomSpeed = 40f; 
    public float minFOV = 20f;    
    private float maxFOV = 60f; 

    [Header("--- SONS ---")]
    public AudioClip somClique;      
    public AudioClip somFotoSucesso; 
    public AudioClip somTrocaLuz;
    public AudioClip somUpgradeRecebido; 

    public enum ModoLanterna { Normal, UV, Desligada }
    public ModoLanterna modoLuzAtual = ModoLanterna.Desligada;

    public bool temUpgradeLanterna = false; 
    public bool modoAtivo { get; private set; } = false; 
    
    private float fovOriginal;
    private Renderer[] renderersVisuais;
    
    // 🔥 Referência para podermos resetar o flash se o cara spammar clique
    private Coroutine flashCoroutine;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else 
        {
            Destroy(gameObject); 
            return;
        }

        if (flashBranco) 
        { 
            flashBranco.gameObject.SetActive(false);
            flashBranco.raycastTarget = false; 
        }
        
        if (modeloCameraMao) renderersVisuais = modeloCameraMao.GetComponentsInChildren<Renderer>();
    }

    void Start()
    {
        if (PersistenciaManager.Instance != null)
        {
            if (PersistenciaManager.Instance.ObterEstado("Camera_TemUpgradeLanterna"))
            {
                temUpgradeLanterna = true;
            }
        }
    }

    public void ReceberUpgradeLanterna()
    {
        if (temUpgradeLanterna) return; 

        temUpgradeLanterna = true;
        
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado("Camera_TemUpgradeLanterna", true);
            PersistenciaManager.Instance.SalvarTudo();
        }

        if (audioSource && somUpgradeRecebido) audioSource.PlayOneShot(somUpgradeRecebido);
    }

    void OnEnable() 
    { 
        modoAtivo = false; 
        ToggleRenderers(true);
        if (hudCamera) hudCamera.SetActive(false);
        AtualizarLuzes(); 
        EsconderAmbosOsTextos(); 
    }
    
    void OnDisable() 
    { 
        modoAtivo = false;
        if (hudCamera) hudCamera.SetActive(false); 
        if (luzNormalObj) luzNormalObj.SetActive(false); 
        if (luzUVObj) luzUVObj.SetActive(false); 
        
        if (cameraPlayer != null && fovOriginal > 0) cameraPlayer.fieldOfView = fovOriginal;
        
        EsconderAmbosOsTextos(); 
    }

    void EsconderAmbosOsTextos()
    {
        if (textoAjudaEntrar) textoAjudaEntrar.SetActive(false);
        if (textoAjudaLuz) textoAjudaLuz.SetActive(false);
    }

    void Update()
    {
        if (Time.timeScale == 0 || FPS_Master.travadoInteracao || Cursor.visible)
        {
            if (hudCamera) hudCamera.SetActive(false);
            EsconderAmbosOsTextos();
            return; 
        }

        if (modoAtivo && hudCamera && !hudCamera.activeSelf) 
        {
            hudCamera.SetActive(true);
        }

        bool temCameraNaMao = (InventoryManager.Instance && InventoryManager.Instance.itemSelecionado == idCamera);
        
        if (Input.GetKeyDown(teclaEntrarSair) && (temCameraNaMao || modoAtivo))
        {
            modoAtivo = !modoAtivo;

            if (modoAtivo) 
            {
                if (cameraPlayer != null)
                {
                    fovOriginal = cameraPlayer.fieldOfView;
                    maxFOV = fovOriginal; 
                }
                
                if (hudCamera) hudCamera.SetActive(true);
                ToggleRenderers(false); 
                AtualizarLuzes();
                
                if (textoAjudaEntrar) textoAjudaEntrar.SetActive(false);
                if (textoAjudaLuz && temUpgradeLanterna) textoAjudaLuz.SetActive(true);
            }
            else 
            {
                if (hudCamera) hudCamera.SetActive(false);
                ToggleRenderers(true); 
                DesligarLuzesTotais();
                
                if (textoAjudaLuz) textoAjudaLuz.SetActive(false);
                
                if (cameraPlayer != null) cameraPlayer.fieldOfView = fovOriginal;

                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.TentarEquipar(idCamera);
                }
            }
        }

        if (!modoAtivo) 
        {
            if (textoAjudaLuz && textoAjudaLuz.activeSelf) textoAjudaLuz.SetActive(false);

            if (temCameraNaMao)
            {
                if (textoAjudaEntrar && !textoAjudaEntrar.activeSelf) textoAjudaEntrar.SetActive(true);
            }
            else
            {
                if (textoAjudaEntrar && textoAjudaEntrar.activeSelf) textoAjudaEntrar.SetActive(false);
            }
            
            if (temCameraNaMao && renderersVisuais != null && renderersVisuais.Length > 0 && !renderersVisuais[0].enabled)
            {
                ToggleRenderers(true);
            }

            return; 
        }
        else 
        {
            if (textoAjudaEntrar && textoAjudaEntrar.activeSelf) textoAjudaEntrar.SetActive(false);
            if (temCameraNaMao) ToggleRenderers(false);
        }

        if (Input.GetKeyDown(teclaLuz)) TrocarLuz();
        if (Input.GetKeyDown(teclaFoto)) TentarFotoEsfera(); 

        if (cameraPlayer != null)
        {
            if (Input.GetKey(teclaZoomIn) || Input.GetKey(teclaZoomOut))
            {
                if (Input.GetKey(teclaZoomIn)) cameraPlayer.fieldOfView -= zoomSpeed * Time.deltaTime;
                if (Input.GetKey(teclaZoomOut)) cameraPlayer.fieldOfView += zoomSpeed * Time.deltaTime;
                
                cameraPlayer.fieldOfView = Mathf.Clamp(cameraPlayer.fieldOfView, minFOV, maxFOV);
            }
        }
    }

    void TentarFotoEsfera()
    {
        if (Time.time < proximaFoto) return;
        proximaFoto = Time.time + cooldownFoto;

        if (cameraPlayer == null) return;

        Vector3 origem = cameraPlayer.transform.position + (cameraPlayer.transform.forward * 0.2f);
        Vector3 direcao = cameraPlayer.transform.forward;

        RaycastHit[] hits = Physics.SphereCastAll(origem, raioDaMira, direcao, alcanceMaximo, layerObjetosFoto, QueryTriggerInteraction.Ignore);

        if (hits.Length > 0)
        {
            System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

            foreach (RaycastHit hit in hits)
            {
                AmbientSkyObject alvo = hit.collider.GetComponent<AmbientSkyObject>();
                if (alvo == null) alvo = hit.collider.GetComponentInParent<AmbientSkyObject>();

                if (alvo != null)
                {
                    Vector3 centroDoObjeto = hit.collider.bounds.center;

                    if (Physics.Linecast(cameraPlayer.transform.position, centroDoObjeto, out RaycastHit hitParede, layerParede, QueryTriggerInteraction.Ignore))
                    {
                        Debug.LogWarning($"📷 FOTO BLOQUEADA: Tentou ver o [{alvo.gameObject.name}], mas o objeto [{hitParede.collider.gameObject.name}] entrou na frente!");
                        Debug.DrawLine(cameraPlayer.transform.position, hitParede.point, Color.red, 3f);
                    }
                    else
                    {
                        ExecutarFoto(alvo);
                        return;
                    }
                }
            }
        }

        if (audioSource && somClique) audioSource.PlayOneShot(somClique);
    }

    void ExecutarFoto(AmbientSkyObject alvo)
    {
        if (audioSource && somFotoSucesso) audioSource.PlayOneShot(somFotoSucesso);

        // 🔥 A MÁGICA ACONTECE AQUI. MANDA O SISTEMA GLOBAL RODAR O FLASH.
        if (flashBranco) 
        { 
            if (SistemaGlobal.Instance != null)
            {
                if (flashCoroutine != null) SistemaGlobal.Instance.StopCoroutine(flashCoroutine);
                flashCoroutine = SistemaGlobal.Instance.StartCoroutine(RotinaFlashBlindada(flashBranco));
            }
            else 
            {
                // Fallback de segurança se o SistemaGlobal não existir (ex: testando cena isolada)
                if (flashCoroutine != null) StopCoroutine(flashCoroutine);
                flashCoroutine = StartCoroutine(RotinaFlashBlindada(flashBranco));
            }
        }

        switch (alvo.tipoDeInteracao)
        {
            case AmbientSkyObject.TipoFoto.ChaveFinal:
                KeySystem keyScript = alvo.GetComponent<KeySystem>();
                if (keyScript == null) keyScript = alvo.GetComponentInParent<KeySystem>();
                
                if (keyScript != null) keyScript.Pickup(); 
                break;
                
            case AmbientSkyObject.TipoFoto.LinkPilar:
                PilarReceptor pilar = PilarReceptor.BuscarPilarPorID(alvo.idLinkPilar);
                if (pilar != null) pilar.AtivarObjeto();
                break;
        }
        alvo.Sumir(); 
    }

    // 🔥 NOVA ROTINA DE FLASH INDEPENDENTE E BLINDADA
    private IEnumerator RotinaFlashBlindada(Image flash)
    {
        if (flash == null) yield break;

        flash.transform.SetAsLastSibling();
        flash.gameObject.SetActive(true);
        flash.color = Color.white;
        
        CanvasRenderer cr = flash.GetComponent<CanvasRenderer>();
        if (cr != null) cr.SetAlpha(1f); 

        yield return new WaitForSeconds(0.05f);

        float tempo = 0.5f;
        while (tempo > 0)
        {
            tempo -= Time.deltaTime;
            if (flash != null && flash.canvasRenderer != null) 
            {
                flash.canvasRenderer.SetAlpha(tempo * 2); 
            }
            yield return null;
        }
        
        if (flash != null) flash.gameObject.SetActive(false);
    }

    void DesligarLuzesTotais()
    {
        if (luzNormalObj) luzNormalObj.SetActive(false);
        if (luzUVObj) luzUVObj.SetActive(false);
    }

    void AtualizarLuzes()
    {
        if (luzNormalObj) luzNormalObj.SetActive(false);
        if (luzUVObj) luzUVObj.SetActive(false);
        
        if (!modoAtivo) return;

        if (modoLuzAtual == ModoLanterna.Normal && luzNormalObj) luzNormalObj.SetActive(true);
        else if (modoLuzAtual == ModoLanterna.UV && luzUVObj) luzUVObj.SetActive(true);
    }

    void TrocarLuz()
    {
        if (!temUpgradeLanterna) return;

        switch (modoLuzAtual)
        {
            case ModoLanterna.Desligada: modoLuzAtual = ModoLanterna.Normal; break;
            case ModoLanterna.Normal: modoLuzAtual = ModoLanterna.UV; break;
            case ModoLanterna.UV: modoLuzAtual = ModoLanterna.Desligada; break;
        }
        if (audioSource && somTrocaLuz) audioSource.PlayOneShot(somTrocaLuz);
        AtualizarLuzes();
    }

    void ToggleRenderers(bool estado)
    {
        if (renderersVisuais != null)
            foreach (Renderer r in renderersVisuais) if (r) r.enabled = estado;
    }
}
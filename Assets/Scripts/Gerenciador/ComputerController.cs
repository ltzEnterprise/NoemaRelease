using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal; 
using System.Collections;

public class ComputerController : MonoBehaviour
{
    public string idDoComputador = "PC_Principal";

    [Header("Configuração de Cena")]
    public string nomeDaCena2D = "Mundo2D";
    public Transform pontoDeSpawnPadrao;

    [Header("Efeitos e UI")]
    public GameObject telaDesktop;
    public GameObject telaBSOD_Runa; 
    public GameObject telaBonusNoite; 
    
    public Transform pontoDeZoom;
    public CanvasGroup faderUI;
    public Transform player3D;

    [Header("--- TRANSIÇÃO 2D PARA 3D ---")]
    public GameObject telaAzulUITransicao; 

    [Header("--- LOADING LOCAL OPCIONAL ---")]
    public GameObject painelLoadingLocal;
    public bool forcarPainelLoadingFullscreen = true;
    public float tempoMinimoLoadingLocal = 0.5f;

    [Header("--- TEMPOS DA CUTSCENE PÓS-CRASH ---")]
    public float tempoTelaAzul = 1.0f;
    public float tempoPainelUpgrade = 3.0f;

    [Header("--- TEXTOS DE MIRA ---")]
    public GameObject textoInteragirPC;    
    public GameObject textoInteragirRuna;  

    [Header("--- RECOMPENSAS DO PÓS-CRASH ---")]
    public GameObject painelUpgradeCamera; 
    public int idDaCameraNoInventario = 6; 
    
    public GameObject painelAvisoRuna;     
    public string nomeDaRuna = "Runa_Investigacao"; 
    
    public string idChaveDaCasa = "Chave_Casa_Noite"; 
    public GameObject iconeChaveHUD;       

    [Header("Áudio")]
    public AudioSource audioSourcePC;
    public AudioClip somBootOuTroca; 
    public AudioClip somPegarRuna;
    public AudioClip somDesligarPC;
    public AudioClip somUpgradeCamera; 

    [Header("--- EFEITO PÓS-CRASH (URP) ---")]
    public ScriptableRendererFeature pixelURPFeature;

    private bool pcQueimado;
    private bool modoRunaAtivo;
    private bool cutsceneRodando = false; 

    private bool estaOlhando = false;
    private float tempoUltimoClique = 0f;

    private bool deveRodarCutscenePosCrash = false;

    private string ChavePcQueimado => "PC_" + idDoComputador + "_Queimado";
    private string ChaveCutsceneVista => "PC_" + idDoComputador + "_CutscenePosCrashVista";
    private string ChaveChaveCasaRecebida => "PC_" + idDoComputador + "_ChaveCasaRecebida";
    private string ChavePainelMundo2D => "PC_" + idDoComputador + "_PainelMundo2D_Ativo";
    private string ChavePainelMundo2DConcluido => "PC_" + idDoComputador + "_PainelMundo2D_Concluido";
    private string ChavePainelExtra => "PC_" + idDoComputador + "_PainelExtra_Ativo";

    void Awake()
    {
        if (PersistenciaManager.Instance != null)
        {
            bool crashEvent = PersistenciaManager.Instance.ObterEstado("PC_Crash_Event", false);
            bool cutsceneVista = PersistenciaManager.Instance.ObterEstado(ChaveCutsceneVista, false);
            bool queimado = PersistenciaManager.Instance.ObterEstado(ChavePcQueimado, false);

            deveRodarCutscenePosCrash = crashEvent && !cutsceneVista && !queimado;

            if (deveRodarCutscenePosCrash)
            {
                if (pixelURPFeature != null)
                    pixelURPFeature.SetActive(true);

                AtivarTelaAzulImediata();
            }
        }
    }

    void Start()
    {
        StartCoroutine(InicializarSeguro());
    }

    private IEnumerator InicializarSeguro()
    {
        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(true, false);

        if (textoInteragirPC) textoInteragirPC.SetActive(false);
        if (textoInteragirRuna) textoInteragirRuna.SetActive(false);
        if (painelAvisoRuna) painelAvisoRuna.SetActive(false);
        if (painelUpgradeCamera) painelUpgradeCamera.SetActive(false);
        if (painelLoadingLocal) painelLoadingLocal.SetActive(false);

        if (telaAzulUITransicao && !deveRodarCutscenePosCrash)
            telaAzulUITransicao.SetActive(false);

        yield return new WaitUntil(() =>
            PersistenciaManager.Instance != null &&
            PersistenciaManager.Instance.DadosProntosParaUso
        );

        bool crashEvent = PersistenciaManager.Instance.ObterEstado("PC_Crash_Event", false);
        bool cutsceneVista = PersistenciaManager.Instance.ObterEstado(ChaveCutsceneVista, false);
        bool queimado = PersistenciaManager.Instance.ObterEstado(ChavePcQueimado, false);

        deveRodarCutscenePosCrash = crashEvent && !cutsceneVista && !queimado;

        if (deveRodarCutscenePosCrash)
            AtivarTelaAzulImediata();

        CarregarEstadoInicial();
    }

    private void AtivarTelaAzulImediata()
    {
        if (telaAzulUITransicao == null) return;

        Canvas canvas = telaAzulUITransicao.GetComponentInParent<Canvas>(true);

        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32767;
        }

        telaAzulUITransicao.SetActive(true);
        telaAzulUITransicao.transform.SetAsLastSibling();

        RectTransform rt = telaAzulUITransicao.GetComponent<RectTransform>();

        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }
    }

    private void MostrarPainelUpgradeSemDestruirLayout()
    {
        if (painelUpgradeCamera == null) return;

        Canvas canvas = painelUpgradeCamera.GetComponentInParent<Canvas>(true);

        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
            canvas.overrideSorting = true;
            canvas.sortingOrder = 32766;
        }

        painelUpgradeCamera.SetActive(true);
        painelUpgradeCamera.transform.SetAsLastSibling();
    }

    private void CarregarEstadoInicial()
    {
        if (PersistenciaManager.Instance == null) return;

        bool mundoPixelado = PersistenciaManager.Instance.ObterEstado("World_Is_Pixelated", false);

        if (pixelURPFeature != null)
            pixelURPFeature.SetActive(mundoPixelado);

        if (PersistenciaManager.Instance.ObterEstado(ChavePcQueimado, false))
        {
            pcQueimado = true;
            modoRunaAtivo = false;
            DesligarTudo();

            if (telaAzulUITransicao)
                telaAzulUITransicao.SetActive(false);

            if (FPS_Master.Instance != null)
                FPS_Master.Instance.AlterarEstadoJogador(false, false);

            return;
        }

        if (PersistenciaManager.Instance.ObterEstado("PC_Crash_Event", false))
        {
            AtivarModoPósCrash(false);
            return;
        }

        bool painelExtraAtivo =
            PersistenciaManager.Instance.ObterEstado(ChavePainelExtra, false) ||
            PersistenciaManager.Instance.ObterEstado("FuseBox_Noite_Resolvida", false);

        bool painelMundo2DAtivo =
            PersistenciaManager.Instance.ObterEstado(ChavePainelMundo2D, false) ||
            PersistenciaManager.Instance.ObterEstado("FuseBox_Dia_Resolvida", false);

        bool painelMundo2DConcluido =
            PersistenciaManager.Instance.ObterEstado(ChavePainelMundo2DConcluido, false);

        if (painelExtraAtivo)
        {
            AtivarModoBonusNoite(false);

            if (FPS_Master.Instance != null)
                FPS_Master.Instance.AlterarEstadoJogador(false, false);

            return;
        }

        if (painelMundo2DAtivo && !painelMundo2DConcluido)
        {
            AtivarModoNormal(false);

            if (FPS_Master.Instance != null)
                FPS_Master.Instance.AlterarEstadoJogador(false, false);

            return;
        }

        DesligarTudo();

        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(false, false);
    }

    public void AoOlhar()
    {
        estaOlhando = true;

        if (pcQueimado || cutsceneRodando) 
        {
            if (textoInteragirPC) textoInteragirPC.SetActive(false);
            if (textoInteragirRuna) textoInteragirRuna.SetActive(false);
            return;
        }

        if (modoRunaAtivo)
        {
            if (textoInteragirPC) textoInteragirPC.SetActive(false);
            if (textoInteragirRuna) textoInteragirRuna.SetActive(true);
        }
        else
        {
            if (textoInteragirRuna) textoInteragirRuna.SetActive(false);
            if (textoInteragirPC) textoInteragirPC.SetActive(true);
        }
    }

    public void AoSair()
    {
        estaOlhando = false;

        if (textoInteragirPC) textoInteragirPC.SetActive(false);
        if (textoInteragirRuna) textoInteragirRuna.SetActive(false);
    }

    void TocarSom()
    {
        if (audioSourcePC && somBootOuTroca)
            audioSourcePC.PlayOneShot(somBootOuTroca);
    }

    public void LigarPCProMundo2D()
    {
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado("FuseBox_Dia_Resolvida", true);
            PersistenciaManager.Instance.RegistrarEstado(ChavePainelMundo2D, true);
            PersistenciaManager.Instance.RegistrarEstado(ChavePainelMundo2DConcluido, false);
            PersistenciaManager.Instance.SalvarTudo(true);
        }

        AtivarModoNormal(true);
    }

    public void LigarPcSetaNoite()
    {
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado("FuseBox_Noite_Resolvida", true);
            PersistenciaManager.Instance.RegistrarEstado(ChavePainelExtra, true);
            PersistenciaManager.Instance.SalvarTudo(true);
        }

        AtivarModoBonusNoite(true);
    }

    void AtivarModoNormal(bool tocarSom = true)
    {
        DesligarTudo();

        if (telaDesktop)
            telaDesktop.SetActive(true);

        if (tocarSom)
            TocarSom();
    }

    void AtivarModoBonusNoite(bool tocarSom = true)
    {
        DesligarTudo();

        if (telaBonusNoite)
            telaBonusNoite.SetActive(true);

        if (tocarSom)
            TocarSom();
    }

    private void AtivarModoPósCrash(bool tocarSom = true)
    {
        modoRunaAtivo = true;
        DesligarTudo();

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(ChavePainelMundo2DConcluido, true);
            PersistenciaManager.Instance.RegistrarEstado(ChavePainelMundo2D, false);
            PersistenciaManager.Instance.RegistrarEstado("World_Is_Pixelated", true);
        }

        if (telaBSOD_Runa)
            telaBSOD_Runa.SetActive(true); 

        if (tocarSom)
            TocarSom();

        if (pixelURPFeature != null)
            pixelURPFeature.SetActive(true);

        GarantirChaveDaCasa();
        ReposicionarPlayerPosCrash();

        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.ChangeTo(DayNightCycle.TimeState.Night);

        if (deveRodarCutscenePosCrash)
        {
            StartCoroutine(SequenciaDeAberturaDaCena());
        }
        else
        {
            if (telaAzulUITransicao)
                telaAzulUITransicao.SetActive(false);

            if (painelUpgradeCamera)
                painelUpgradeCamera.SetActive(false);

            if (FPS_Master.Instance != null)
                FPS_Master.Instance.AlterarEstadoJogador(false, false);
        }

        SalvarEstadoAtualDoJogo();
    }

    private void GarantirChaveDaCasa()
    {
        if (PersistenciaManager.Instance != null &&
            PersistenciaManager.Instance.ObterEstado(ChaveChaveCasaRecebida, false))
        {
            if (iconeChaveHUD)
                iconeChaveHUD.SetActive(true);

            return;
        }

        KeySystem.AdicionarChave(idChaveDaCasa);

        if (iconeChaveHUD)
            iconeChaveHUD.SetActive(true);

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado(ChaveChaveCasaRecebida, true);
    }

    private void ReposicionarPlayerPosCrash()
    {
        if (PersistenciaManager.Instance != null &&
            PersistenciaManager.Instance.ObterEstado("Player3D_HasSave", false) &&
            player3D != null &&
            FPS_Master.Instance != null)
        {
            float x = PersistenciaManager.Instance.ObterFloat("Player3D_PosX");
            float y = PersistenciaManager.Instance.ObterFloat("Player3D_PosY");
            float z = PersistenciaManager.Instance.ObterFloat("Player3D_PosZ");
            float rotY = PersistenciaManager.Instance.ObterFloat("Player3D_RotY");

            FPS_Master.Instance.Teleportar(new Vector3(x, y, z));
            player3D.rotation = Quaternion.Euler(0, rotY, 0);

            if (FPS_Master.Instance.cameraJogador != null)
                FPS_Master.Instance.cameraJogador.transform.localRotation = Quaternion.identity;

            Physics.SyncTransforms(); 
        }
        else if (pontoDeSpawnPadrao != null && player3D != null && FPS_Master.Instance != null)
        {
            FPS_Master.Instance.Teleportar(pontoDeSpawnPadrao.position);
            player3D.rotation = pontoDeSpawnPadrao.rotation;

            if (FPS_Master.Instance.cameraJogador != null)
                FPS_Master.Instance.cameraJogador.transform.localRotation = Quaternion.identity;

            Physics.SyncTransforms();
        }
    }

    private IEnumerator SequenciaDeAberturaDaCena()
    {
        cutsceneRodando = true;
        AoSair(); 

        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(true, false);

        AtivarTelaAzulImediata();

        yield return new WaitForSecondsRealtime(tempoTelaAzul); 

        if (telaAzulUITransicao)
            telaAzulUITransicao.SetActive(false);

        yield return null;

        MostrarPainelUpgradeSemDestruirLayout();

        if (audioSourcePC != null && somUpgradeCamera != null)
            audioSourcePC.PlayOneShot(somUpgradeCamera);

        try
        {
            if (InventoryManager.Instance != null)
            {
                InventoryManager.Instance.ReceberItem(idDaCameraNoInventario);
                InventoryManager.Instance.TentarEquipar(idDaCameraNoInventario);
            }

            if (RealityCamera.Instance != null)
                RealityCamera.Instance.ReceberUpgradeLanterna();
        }
        catch (System.Exception) { }

        yield return new WaitForSecondsRealtime(tempoPainelUpgrade);
        
        if (painelUpgradeCamera)
            painelUpgradeCamera.SetActive(false);

        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(ChaveCutsceneVista, true);
            PersistenciaManager.Instance.RegistrarEstado(ChaveChaveCasaRecebida, true);
            PersistenciaManager.Instance.RegistrarEstado(ChavePainelMundo2DConcluido, true);
            PersistenciaManager.Instance.RegistrarEstado(ChavePainelMundo2D, false);
        }

        cutsceneRodando = false;

        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(false, false);

        SalvarEstadoAtualDoJogo();

        if (estaOlhando)
            AoOlhar();
    }

    public void Interagir()
    {
        if (pcQueimado || cutsceneRodando) return; 

        if (Time.unscaledTime < tempoUltimoClique + 0.5f) return;

        tempoUltimoClique = Time.unscaledTime;

        if (modoRunaAtivo)
        {
            AoSair(); 
            StartCoroutine(SequenciaPegarRuna());
        }
        else if ((telaDesktop != null && telaDesktop.activeSelf) || (telaBonusNoite != null && telaBonusNoite.activeSelf))
        {
            AoSair(); 
            StartCoroutine(SequenciaEntradaMatrix());
        }
        else
        {
            if (audioSourcePC && somDesligarPC)
                audioSourcePC.PlayOneShot(somDesligarPC);

            Debug.Log("PC sem energia! Resolva a FuseBox primeiro.");
        }
    }

    private IEnumerator SequenciaPegarRuna()
    {
        cutsceneRodando = true;

        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(true, false);

        if (audioSourcePC && somPegarRuna)
            audioSourcePC.PlayOneShot(somPegarRuna);

        if (InventarioRunas.Instance != null)
            InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRuna);
        else if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado("Runa_" + nomeDaRuna, true);
        
        GarantirChaveDaCasa();

        if (painelAvisoRuna)
            painelAvisoRuna.SetActive(true);

        yield return new WaitForSecondsRealtime(1.5f);

        if (painelAvisoRuna)
            painelAvisoRuna.SetActive(false); 

        if (audioSourcePC && somDesligarPC)
            audioSourcePC.PlayOneShot(somDesligarPC);

        DesligarTudo(); 
        
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(ChavePcQueimado, true);
            PersistenciaManager.Instance.RegistrarEstado("PC_Crash_Event", false); 
            PersistenciaManager.Instance.RegistrarEstado("World_Is_Pixelated", true);
        }
        
        pcQueimado = true;
        modoRunaAtivo = false;
        cutsceneRodando = false;

        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(false, false);

        SalvarEstadoAtualDoJogo();
    }

    private IEnumerator SequenciaEntradaMatrix()
    {
        cutsceneRodando = true;

        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(true, false);

        if (player3D != null && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarFloat("Player3D_PosX", player3D.position.x);
            PersistenciaManager.Instance.SalvarFloat("Player3D_PosY", player3D.position.y);
            PersistenciaManager.Instance.SalvarFloat("Player3D_PosZ", player3D.position.z);
            PersistenciaManager.Instance.SalvarFloat("Player3D_RotY", player3D.eulerAngles.y);
            PersistenciaManager.Instance.RegistrarEstado("Player3D_HasSave", true);
        }

        SalvarEstadoAtualDoJogo();

        float tempo = 0f;

        if (Camera.main == null || pontoDeZoom == null)
        {
            CarregarCena2DComLoading();
            yield break;
        }

        Vector3 startPos = Camera.main.transform.position;
        Quaternion startRot = Camera.main.transform.rotation;

        while (tempo < 1.5f)
        {
            tempo += Time.deltaTime;
            float t = tempo / 1.5f;
            t = t * t * (3f - 2f * t);
            
            Camera.main.transform.position = Vector3.Lerp(startPos, pontoDeZoom.position, t);
            Camera.main.transform.rotation = Quaternion.Slerp(startRot, pontoDeZoom.rotation, t);

            yield return null;
        }

        if (faderUI)
        {
            tempo = 0f;

            while (tempo < 1.0f)
            {
                tempo += Time.deltaTime;
                faderUI.alpha = tempo;
                yield return null;
            }
        }

        CarregarCena2DComLoading();
    }

    private void CarregarCena2DComLoading()
    {
        if (painelLoadingLocal != null)
        {
            StartCoroutine(CarregarCena2DComPainelLocal());
            return;
        }

        InterfaceManager interfaceManager = InterfaceManagerDisponivel();

        if (interfaceManager != null)
            interfaceManager.IniciarLoadingParaCena(nomeDaCena2D);
        else
            SceneManager.LoadScene(nomeDaCena2D);
    }

    private IEnumerator CarregarCena2DComPainelLocal()
    {
        AtivarPainelLoadingLocal();

        yield return null;

        AsyncOperation operacao = SceneManager.LoadSceneAsync(nomeDaCena2D);
        operacao.allowSceneActivation = false;

        float tempo = 0f;

        while (operacao.progress < 0.9f || tempo < tempoMinimoLoadingLocal)
        {
            tempo += Time.unscaledDeltaTime;
            yield return null;
        }

        operacao.allowSceneActivation = true;
    }

    private void AtivarPainelLoadingLocal()
    {
        if (painelLoadingLocal == null) return;

        Canvas canvas = painelLoadingLocal.GetComponentInParent<Canvas>(true);

        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);
            canvas.sortingOrder = 30000;
        }

        painelLoadingLocal.SetActive(true);
        painelLoadingLocal.transform.SetAsLastSibling();

        if (forcarPainelLoadingFullscreen)
        {
            RectTransform rt = painelLoadingLocal.GetComponent<RectTransform>();

            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;
            }
        }
    }

    private InterfaceManager InterfaceManagerDisponivel()
    {
        InterfaceManager[] interfaces = Object.FindObjectsByType<InterfaceManager>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        if (interfaces == null || interfaces.Length == 0)
            return null;

        foreach (InterfaceManager ui in interfaces)
        {
            if (ui != null && ui.gameObject.activeInHierarchy)
                return ui;
        }

        return interfaces[0];
    }

    private void SalvarEstadoAtualDoJogo()
    {
        if (PersistenciaManager.Instance == null) return;

        if (SistemaGlobal.Instance != null && SistemaGlobal.Instance.slotFoiDefinido)
        {
            Vector3 posicao = player3D != null ? player3D.position : Vector3.zero;
            SistemaGlobal.Instance.SalvarJogo(posicao, SceneManager.GetActiveScene().name);
        }
        else
        {
            PersistenciaManager.Instance.SalvarTudo(true);
        }
    }

    private void DesligarTudo() 
    { 
        if (telaDesktop)
            telaDesktop.SetActive(false); 

        if (telaBSOD_Runa)
            telaBSOD_Runa.SetActive(false); 

        if (telaBonusNoite)
            telaBonusNoite.SetActive(false); 
    }

    public void TurnOffPixelEffect()
    {
        if (pixelURPFeature != null)
            pixelURPFeature.SetActive(false);

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado("World_Is_Pixelated", false);

        SalvarEstadoAtualDoJogo();
    }

    void OnDisable()
    {
        if (textoInteragirPC)
            textoInteragirPC.SetActive(false);

        if (textoInteragirRuna)
            textoInteragirRuna.SetActive(false);
    }

    void OnDestroy()
    {
        if (textoInteragirPC)
            textoInteragirPC.SetActive(false);

        if (textoInteragirRuna)
            textoInteragirRuna.SetActive(false);
    }
}
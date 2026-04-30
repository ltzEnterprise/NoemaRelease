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
        if (telaAzulUITransicao) telaAzulUITransicao.SetActive(false);

        yield return new WaitUntil(() =>
            PersistenciaManager.Instance != null &&
            PersistenciaManager.Instance.DadosProntosParaUso
        );

        CarregarEstadoInicial();
    }

    private void CarregarEstadoInicial()
    {
        if (PersistenciaManager.Instance == null) return;

        if (PersistenciaManager.Instance.ObterEstado("World_Is_Pixelated"))
        {
            if (pixelURPFeature != null) pixelURPFeature.SetActive(true);
        }
        else
        {
            if (pixelURPFeature != null) pixelURPFeature.SetActive(false);
        }

        if (PersistenciaManager.Instance.ObterEstado("PC_" + idDoComputador + "_Queimado"))
        {
            pcQueimado = true;
            DesligarTudo();

            if (FPS_Master.Instance != null)
                FPS_Master.Instance.AlterarEstadoJogador(false, false);

            return;
        }

        if (PersistenciaManager.Instance.ObterEstado("PC_Crash_Event"))
        {
            AtivarModoPósCrash(false);
        }
        else if (PersistenciaManager.Instance.ObterEstado("FuseBox_Noite_Resolvida"))
        {
            AtivarModoBonusNoite(false);

            if (FPS_Master.Instance != null)
                FPS_Master.Instance.AlterarEstadoJogador(false, false);
        }
        else if (PersistenciaManager.Instance.ObterEstado("FuseBox_Dia_Resolvida"))
        {
            AtivarModoNormal(false);

            if (FPS_Master.Instance != null)
                FPS_Master.Instance.AlterarEstadoJogador(false, false);
        }
        else
        {
            DesligarTudo();

            if (FPS_Master.Instance != null)
                FPS_Master.Instance.AlterarEstadoJogador(false, false);
        }
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
        AtivarModoNormal(true);
    }

    public void LigarPcSetaNoite()
    {
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
        
        if (telaBSOD_Runa)
            telaBSOD_Runa.SetActive(true); 

        if (telaAzulUITransicao)
            telaAzulUITransicao.SetActive(true);

        if (tocarSom)
            TocarSom();

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.RegistrarEstado("World_Is_Pixelated", true);

        if (pixelURPFeature != null)
            pixelURPFeature.SetActive(true);

        if (PersistenciaManager.Instance != null &&
            PersistenciaManager.Instance.ObterEstado("Player3D_HasSave") &&
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

        if (DayNightCycle.Instance != null)
            DayNightCycle.Instance.ChangeTo(DayNightCycle.TimeState.Night);

        StartCoroutine(SequenciaDeAberturaDaCena());
    }

    private IEnumerator SequenciaDeAberturaDaCena()
    {
        cutsceneRodando = true;
        AoSair(); 

        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(true, false);

        yield return new WaitForSecondsRealtime(tempoTelaAzul); 

        if (telaAzulUITransicao)
            telaAzulUITransicao.SetActive(false);

        if (painelUpgradeCamera != null)
        {
            Canvas canvasPai = painelUpgradeCamera.GetComponentInParent<Canvas>(true);

            if (canvasPai != null)
                canvasPai.gameObject.SetActive(true);

            painelUpgradeCamera.SetActive(true);
            painelUpgradeCamera.transform.SetAsLastSibling(); 
        }

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
            {
                RealityCamera.Instance.ReceberUpgradeLanterna();
            }
        }
        catch (System.Exception) { }

        yield return new WaitForSecondsRealtime(tempoPainelUpgrade);
        
        if (painelUpgradeCamera)
            painelUpgradeCamera.SetActive(false);

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
        
        KeySystem.AdicionarChave(idChaveDaCasa);

        if (iconeChaveHUD)
            iconeChaveHUD.SetActive(true);

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
            PersistenciaManager.Instance.RegistrarEstado("PC_" + idDoComputador + "_Queimado", true);
            PersistenciaManager.Instance.RegistrarEstado("PC_Crash_Event", false); 
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
        if (InterfaceManagerDisponivel() != null)
        {
            InterfaceManagerDisponivel().IniciarLoadingParaCena(nomeDaCena2D);
        }
        else
        {
            SceneManager.LoadScene(nomeDaCena2D);
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
            PersistenciaManager.Instance.SalvarTudo(false);
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
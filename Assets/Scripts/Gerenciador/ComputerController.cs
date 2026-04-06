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
    [Tooltip("A TELA PRETA COM O TEXTO DE UPGRADE!")]
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

    void Start()
    {
        if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(true, false);

        if (textoInteragirPC) textoInteragirPC.SetActive(false);
        if (textoInteragirRuna) textoInteragirRuna.SetActive(false);
        if (painelAvisoRuna) painelAvisoRuna.SetActive(false);
        if (painelUpgradeCamera) painelUpgradeCamera.SetActive(false);
        if (telaAzulUITransicao) telaAzulUITransicao.SetActive(false);

        if (PlayerPrefs.GetInt("World_Is_Pixelated", 0) == 1)
        {
            if (pixelURPFeature != null) pixelURPFeature.SetActive(true);
        }
        else
        {
            if (pixelURPFeature != null) pixelURPFeature.SetActive(false);
        }

        // CARREGA A PROGRESSÃO DIRETO DO SAVE
        if (PlayerPrefs.GetInt("PC_" + idDoComputador + "_Queimado", 0) == 1)
        {
            pcQueimado = true;
            DesligarTudo();
            if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(false, false);
            return;
        }

        if (PlayerPrefs.GetInt("PC_Crash_Event", 0) == 1)
        {
            AtivarModoPósCrash(false);
        }
        else if (PlayerPrefs.GetInt("FuseBox_Noite_Resolvida", 0) == 1)
        {
            AtivarModoBonusNoite(false);
            if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(false, false);
        }
        else if (PlayerPrefs.GetInt("FuseBox_Dia_Resolvida", 0) == 1)
        {
            AtivarModoNormal(false);
            if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(false, false);
        }
        else
        {
            DesligarTudo();
            if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(false, false);
        }
    }

    public void AoOlhar()
    {
        if (pcQueimado || cutsceneRodando) return; 

        if (modoRunaAtivo && textoInteragirRuna) textoInteragirRuna.SetActive(true);
        else if (!modoRunaAtivo && textoInteragirPC) textoInteragirPC.SetActive(true);
    }

    public void AoSair()
    {
        if (textoInteragirPC) textoInteragirPC.SetActive(false);
        if (textoInteragirRuna) textoInteragirRuna.SetActive(false);
    }

    void TocarSom() { if (audioSourcePC && somBootOuTroca) audioSourcePC.PlayOneShot(somBootOuTroca); }

    public void LigarPCProMundo2D() { AtivarModoNormal(true); }
    public void LigarPcSetaNoite() { AtivarModoBonusNoite(true); }

    void AtivarModoNormal(bool tocarSom = true)
    {
        DesligarTudo();
        if (telaDesktop) telaDesktop.SetActive(true);
        if (tocarSom) TocarSom();
    }

    void AtivarModoBonusNoite(bool tocarSom = true)
    {
        DesligarTudo();
        if (telaBonusNoite) telaBonusNoite.SetActive(true);
        if (tocarSom) TocarSom();
    }

    private void AtivarModoPósCrash(bool tocarSom = true)
    {
        modoRunaAtivo = true;
        DesligarTudo();
        
        if (telaBSOD_Runa) telaBSOD_Runa.SetActive(true); 
        if (telaAzulUITransicao) telaAzulUITransicao.SetActive(true);

        if (tocarSom) TocarSom();

        PlayerPrefs.SetInt("World_Is_Pixelated", 1);
        PlayerPrefs.Save();
        if (pixelURPFeature != null) pixelURPFeature.SetActive(true);

        if (PlayerPrefs.GetInt("Player3D_HasSave", 0) == 1 && player3D != null && FPS_Master.Instance != null)
        {
            float x = PlayerPrefs.GetFloat("Player3D_PosX");
            float y = PlayerPrefs.GetFloat("Player3D_PosY");
            float z = PlayerPrefs.GetFloat("Player3D_PosZ");
            float rotY = PlayerPrefs.GetFloat("Player3D_RotY");

            FPS_Master.Instance.Teleportar(new Vector3(x, y, z));
            player3D.rotation = Quaternion.Euler(0, rotY, 0);

            if(FPS_Master.Instance.cameraJogador != null)
                FPS_Master.Instance.cameraJogador.transform.localRotation = Quaternion.identity;

            Physics.SyncTransforms(); 
            PlayerPrefs.SetInt("Player3D_HasSave", 1);
            PlayerPrefs.Save();
            
            if (PersistenciaManager.Instance != null) PersistenciaManager.Instance.SalvarTudo();
        }
        else if (pontoDeSpawnPadrao != null && player3D != null && FPS_Master.Instance != null)
        {
            FPS_Master.Instance.Teleportar(pontoDeSpawnPadrao.position);
            player3D.rotation = pontoDeSpawnPadrao.rotation;

            if(FPS_Master.Instance.cameraJogador != null)
                FPS_Master.Instance.cameraJogador.transform.localRotation = Quaternion.identity;

            Physics.SyncTransforms();
        }

        if (DayNightCycle.Instance != null) DayNightCycle.Instance.ChangeTo(DayNightCycle.TimeState.Night);

        StartCoroutine(SequenciaDeAberturaDaCena());
    }

    // ========================================================
    // ROTINA BLINDADA: FORÇA A TELA PRETA APARECER CUSTE O QUE CUSTAR
    // ========================================================
    private IEnumerator SequenciaDeAberturaDaCena()
    {
        Debug.Log("[PC] Iniciando Cutscene Pós-Crash...");
        cutsceneRodando = true;
        if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(true, false);

        yield return new WaitForSecondsRealtime(tempoTelaAzul); // Usa Realtime pra ignorar pause
        if (telaAzulUITransicao) telaAzulUITransicao.SetActive(false);

        // 1. FORÇA O PAINEL A LIGAR PRIMEIRO DE TUDO!
        if (painelUpgradeCamera != null)
        {
            // Força o Canvas pai a ligar (caso a Unity tenha desativado por engano)
            Canvas canvasPai = painelUpgradeCamera.GetComponentInParent<Canvas>(true);
            if (canvasPai != null) canvasPai.gameObject.SetActive(true);

            painelUpgradeCamera.SetActive(true);
            painelUpgradeCamera.transform.SetAsLastSibling(); // Joga pra frente de todas as outras UIs
            Debug.Log("[PC] A TELA DE UPGRADE FOI FORÇADA A LIGAR AGORA.");
        }
        else
        {
            Debug.LogError("[PC ERROR] O SLOT DO 'Painel Upgrade Camera' TÁ VAZIO NO INSPECTOR!");
        }

        if (audioSourcePC != null && somUpgradeCamera != null) audioSourcePC.PlayOneShot(somUpgradeCamera);

        // 2. BLOCO PROTEGIDO: Se o inventário der erro, a tela preta NÃO some.
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
        catch (System.Exception e)
        {
            Debug.LogError("[PC WARNING] Deu erro no Inventário/Câmera, mas a Tela Preta vai continuar! Erro: " + e.Message);
        }

        // 3. FICA NA TELA PELO TEMPO QUE VOCÊ MANDOU
        yield return new WaitForSecondsRealtime(tempoPainelUpgrade);
        
        // 4. DESLIGA A TELA PRETA
        if (painelUpgradeCamera) painelUpgradeCamera.SetActive(false);

        Debug.Log("[PC] Fim da Cutscene. Devolvendo controle.");
        cutsceneRodando = false;
        if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(false, false);
        if (PersistenciaManager.Instance != null) PersistenciaManager.Instance.SalvarTudo();
    }

    public void Interagir()
    {
        if (pcQueimado || cutsceneRodando) return; 

        if (modoRunaAtivo)
        {
            StartCoroutine(SequenciaPegarRuna());
        }
        else if (telaDesktop.activeSelf || telaBonusNoite.activeSelf)
        {
            StartCoroutine(SequenciaEntradaMatrix());
        }
        else
        {
            if (audioSourcePC && somDesligarPC) audioSourcePC.PlayOneShot(somDesligarPC);
            Debug.Log("PC sem energia! Resolva a FuseBox primeiro.");
        }
    }

    private IEnumerator SequenciaPegarRuna()
    {
        cutsceneRodando = true;
        if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(true, false);
        AoSair(); 

        if (audioSourcePC && somPegarRuna) audioSourcePC.PlayOneShot(somPegarRuna);
        if (InventarioRunas.Instance != null) InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRuna);
        
        KeySystem.AdicionarChave(idChaveDaCasa);
        if (iconeChaveHUD) iconeChaveHUD.SetActive(true);
        if (painelAvisoRuna) painelAvisoRuna.SetActive(true);

        yield return new WaitForSecondsRealtime(1.5f);
        if (painelAvisoRuna) painelAvisoRuna.SetActive(false); 

        if (audioSourcePC && somDesligarPC) audioSourcePC.PlayOneShot(somDesligarPC);
        DesligarTudo(); 
        
        PlayerPrefs.SetInt("PC_" + idDoComputador + "_Queimado", 1);
        PlayerPrefs.SetInt("PC_Crash_Event", 0); 
        PlayerPrefs.Save();
        
        pcQueimado = true;
        modoRunaAtivo = false;
        
        cutsceneRodando = false;
        if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(false, false);
        if (PersistenciaManager.Instance != null) PersistenciaManager.Instance.SalvarTudo();
    }

    private IEnumerator SequenciaEntradaMatrix()
    {
        cutsceneRodando = true;
        if (FPS_Master.Instance != null) FPS_Master.Instance.AlterarEstadoJogador(true, false);

        if (player3D != null)
        {
            PlayerPrefs.SetFloat("Player3D_PosX", player3D.position.x);
            PlayerPrefs.SetFloat("Player3D_PosY", player3D.position.y);
            PlayerPrefs.SetFloat("Player3D_PosZ", player3D.position.z);
            PlayerPrefs.SetFloat("Player3D_RotY", player3D.eulerAngles.y);
            PlayerPrefs.SetInt("Player3D_HasSave", 1);
            PlayerPrefs.Save();
            
            if (PersistenciaManager.Instance != null) PersistenciaManager.Instance.SalvarTudo();
        }

        float tempo = 0;
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
            tempo = 0;
            while(tempo < 1.0f) {
                tempo += Time.deltaTime;
                faderUI.alpha = tempo;
                yield return null;
            }
        }

        SceneManager.LoadScene(nomeDaCena2D);
    }

    private void DesligarTudo() 
    { 
        if (telaDesktop) telaDesktop.SetActive(false); 
        if (telaBSOD_Runa) telaBSOD_Runa.SetActive(false); 
        if (telaBonusNoite) telaBonusNoite.SetActive(false); 
    }

    public void TurnOffPixelEffect() { /*...*/ }
    void OnDisable() { /*...*/ }
    void OnDestroy() { /*...*/ }
}
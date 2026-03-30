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

    [Header("--- EFEITO PÓS-CRASH (URP) ---")]
    [Tooltip("Arraste aqui a Feature do Shader IrisWipe/Pixelado do URP")]
    public ScriptableRendererFeature pixelURPFeature;

    private bool pcQueimado;
    private bool modoRunaAtivo;

    void Start()
    {
        if (textoInteragirPC) textoInteragirPC.SetActive(false);
        if (textoInteragirRuna) textoInteragirRuna.SetActive(false);
        if (painelAvisoRuna) painelAvisoRuna.SetActive(false);
        if (painelUpgradeCamera) painelUpgradeCamera.SetActive(false);

        // --- CONTROLE DO EFEITO PIXELADO NO 3D ---
        if (PlayerPrefs.GetInt("World_Is_Pixelated", 0) == 1)
        {
            if (pixelURPFeature != null) pixelURPFeature.SetActive(true);
        }
        else
        {
            if (pixelURPFeature != null) pixelURPFeature.SetActive(false);
        }

        // 1. O PC JÁ QUEIMOU?
        if (PlayerPrefs.GetInt("PC_" + idDoComputador + "_Queimado", 0) == 1)
        {
            pcQueimado = true;
            DesligarTudo();
            return;
        }

        // 2. VOLTOU DO CRASH AGORA?
        if (PlayerPrefs.GetInt("PC_Crash_Event", 0) == 1)
        {
            AtivarModoPósCrash(false);
        }
        // 3. ENIGMA DA NOITE RESOLVIDO?
        else if (PlayerPrefs.GetInt("FuseBox_Noite_Resolvida", 0) == 1)
        {
            AtivarModoBonusNoite(false);
        }
        else
        {
            // 4. PADRÃO
            DesligarTudo();
        }
    }

    public void AoOlhar()
    {
        if (pcQueimado) return;

        if (modoRunaAtivo && textoInteragirRuna) textoInteragirRuna.SetActive(true);
        else if (!modoRunaAtivo && textoInteragirPC) textoInteragirPC.SetActive(true);
    }

    public void AoSair()
    {
        if (textoInteragirPC) textoInteragirPC.SetActive(false);
        if (textoInteragirRuna) textoInteragirRuna.SetActive(false);
    }

    void TocarSom()
    {
        if (audioSourcePC && somBootOuTroca) audioSourcePC.PlayOneShot(somBootOuTroca);
    }

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
        if (tocarSom) TocarSom();

        // --- LIGA O EFEITO PIXELADO NO MUNDO 3D E SALVA ---
        PlayerPrefs.SetInt("World_Is_Pixelated", 1);
        PlayerPrefs.Save();
        if (pixelURPFeature != null) pixelURPFeature.SetActive(true);

        if (PlayerPrefs.GetInt("Player3D_HasSave", 0) == 1 && player3D != null)
        {
            CharacterController cc = player3D.GetComponent<CharacterController>();
            if(cc) cc.enabled = false;

            float x = PlayerPrefs.GetFloat("Player3D_PosX");
            float y = PlayerPrefs.GetFloat("Player3D_PosY");
            float z = PlayerPrefs.GetFloat("Player3D_PosZ");
            float rotY = PlayerPrefs.GetFloat("Player3D_RotY");

            player3D.position = new Vector3(x, y, z);
            player3D.rotation = Quaternion.Euler(0, rotY, 0);

            if(cc) cc.enabled = true;
            
            PlayerPrefs.SetInt("Player3D_HasSave", 1);
            PlayerPrefs.Save();
            
            if (PersistenciaManager.Instance != null) PersistenciaManager.Instance.SalvarTudo();
        }
        else if (pontoDeSpawnPadrao != null && player3D != null)
        {
            player3D.position = pontoDeSpawnPadrao.position;
        }

        if (DayNightCycle.Instance != null) DayNightCycle.Instance.ChangeTo(DayNightCycle.TimeState.Night);
        
        if (RealityCamera.Instance != null && !RealityCamera.Instance.temUpgradeLanterna) 
        {
            RealityCamera.Instance.ReceberUpgradeLanterna();
            StartCoroutine(AvisoUpgradeCamera());
        }
    }

    IEnumerator AvisoUpgradeCamera()
    {
        if (painelUpgradeCamera) painelUpgradeCamera.SetActive(true);
        
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ReceberItem(idDaCameraNoInventario);
            InventoryManager.Instance.TentarEquipar(idDaCameraNoInventario);
        }

        yield return new WaitForSeconds(3.5f);
        if (painelUpgradeCamera) painelUpgradeCamera.SetActive(false);
    }

    public void Interagir()
    {
        if (pcQueimado) return;

        if (modoRunaAtivo)
        {
            StartCoroutine(SequenciaPegarRuna());
        }
        else
        {
            if (!telaDesktop.activeSelf && !telaBSOD_Runa.activeSelf && !telaBonusNoite.activeSelf)
            {
                AtivarModoNormal(true); 
                return; 
            }
            StartCoroutine(SequenciaEntradaMatrix());
        }
    }

    private IEnumerator SequenciaPegarRuna()
    {
        AoSair(); 

        if (audioSourcePC && somPegarRuna) audioSourcePC.PlayOneShot(somPegarRuna);
        if (InventarioRunas.Instance != null) InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRuna);
        
        KeySystem.AdicionarChave(idChaveDaCasa);
        if (iconeChaveHUD) iconeChaveHUD.SetActive(true);

        if (painelAvisoRuna) painelAvisoRuna.SetActive(true);

        yield return new WaitForSeconds(1.0f);
        if (audioSourcePC && somDesligarPC) audioSourcePC.PlayOneShot(somDesligarPC);
        DesligarTudo();
        
        PlayerPrefs.SetInt("PC_" + idDoComputador + "_Queimado", 1);
        PlayerPrefs.SetInt("PC_Crash_Event", 0); 
        PlayerPrefs.Save();
        
        pcQueimado = true;
        modoRunaAtivo = false;

        yield return new WaitForSeconds(3.5f);
        if (painelAvisoRuna) painelAvisoRuna.SetActive(false);
    }

    private IEnumerator SequenciaEntradaMatrix()
    {
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

    public void TurnOffPixelEffect()
    {
        PlayerPrefs.SetInt("World_Is_Pixelated", 0);
        PlayerPrefs.Save();
        if (pixelURPFeature != null) pixelURPFeature.SetActive(false);
    }

    // --- PROTEÇÃO ABSOLUTA PARA O EDITOR DA UNITY NÃO BUGAR ---
    void OnDisable()
    {
        if (pixelURPFeature != null) pixelURPFeature.SetActive(false);
    }

    void OnDestroy()
    {
        if (pixelURPFeature != null) pixelURPFeature.SetActive(false);
    }
}
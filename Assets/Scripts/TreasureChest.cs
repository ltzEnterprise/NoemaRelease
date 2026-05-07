using UnityEngine;
using System.Collections;
using TMPro;

public class TreasureChest : MonoBehaviour
{
    [Header("Save System")]
    public string uniqueID; 

    [Header("Configurações Básicas")]
    public Transform tampaDoBau; 
    public string nomeDaRunaNesteBau; 

    [Header("Recompensa: Item de Inventário")]
    public bool darItemInventario = false;
    public int idDoItemInventario = 0;
    
    [Header("O Seu Painel Novo")]
    public GameObject painelCustomizadoDaRecompensa; 
    
    [Header("--- SISTEMA DE CHAVE ---")]
    public bool requerChave = false;  
    public string idChaveNecessaria; 

    [Header("--- TRAVA POR TELEPORTE ---")]
    [Tooltip("Se ativado, o baú só funciona depois que o jogador usar o teleporte indicado abaixo.")]
    public bool bloquearAteUsarTeleport = false;

    [Tooltip("Precisa ser igual ao uniqueID do TeleportArea necessário.")]
    public string idTeleportNecessario = "TP_Castelo_01";

    [Tooltip("Mensagem opcional quando tentar usar o baú antes de passar pelo teleporte.")]
    public GameObject textoPrecisaTeleport;

    [Header("--- DEMO MODE ---")]
    public bool finalizaDemo = false;
    public GameObject painelFimDemo; 
    public string nomeCenaMenu = "Menu";
    public float tempoParaVoltarMenu = 5f;

    [Header("UI & Mensagens (Suas Variáveis Antigas)")]
    public GameObject textoInteragir; 
    public GameObject textoPrecisaChave; 
    public GameObject painelPretoRecompensa;     
    public TextMeshProUGUI textoRecompensa; 
    public GameObject iconeChaveParaEsconder; 

    [Header("Sons")]
    public AudioSource audioSource;
    public AudioClip somAbrir;
    public AudioClip somTrancado;

    private bool jaAbriu = false;
    private bool estaOlhando = false; 
    private bool mostrandoErro = false;
    private bool inicializado = false; 

    private Coroutine rotinaAvisoChave;
    private Coroutine rotinaAvisoTeleport;

    void Start()
    {
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoPrecisaChave) textoPrecisaChave.SetActive(false);
        if (textoPrecisaTeleport) textoPrecisaTeleport.SetActive(false);
        if (painelFimDemo) painelFimDemo.SetActive(false); 
        if (painelPretoRecompensa) painelPretoRecompensa.SetActive(false);
        if (painelCustomizadoDaRecompensa) painelCustomizadoDaRecompensa.SetActive(false);

        StartCoroutine(InicializarSeguro());
    }

    IEnumerator InicializarSeguro()
    {
        if (PersistenciaManager.Instance != null)
            yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            jaAbriu = PersistenciaManager.Instance.ObterEstado(uniqueID, false);

            if (jaAbriu)
            {
                if (tampaDoBau)
                    tampaDoBau.localRotation = Quaternion.Euler(-90, 0, 0);

                if (painelPretoRecompensa) painelPretoRecompensa.SetActive(false);
                if (painelCustomizadoDaRecompensa) painelCustomizadoDaRecompensa.SetActive(false);
            }
        }

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(idTeleportNecessario))
        {
            bool tpUsado =
                PersistenciaManager.Instance.ObterEstado(idTeleportNecessario + "_Usado", false) ||
                PersistenciaManager.Instance.ObterEstado(idTeleportNecessario, false);

            if (tpUsado)
                TeleportArea.RegistrarTeleportUsadoExternamente(idTeleportNecessario);
        }

        inicializado = true;
    }

    public void AoOlhar()
    {
        if (!inicializado) return;
        if (jaAbriu) return;

        estaOlhando = true;

        if (!mostrandoErro && textoInteragir) 
            textoInteragir.SetActive(true);
    }

    public void AoSair()
    {
        estaOlhando = false;

        if (textoInteragir)
            textoInteragir.SetActive(false);
    }

    public void Interagir()
    {
        if (!inicializado) return;
        if (jaAbriu || mostrandoErro) return;

        if (!TeleportNecessarioFoiUsado())
        {
            Debug.LogWarning("[TreasureChest] Baú bloqueado. Teleporte necessário ainda não foi usado: " + idTeleportNecessario);

            if (audioSource && somTrancado)
                audioSource.PlayOneShot(somTrancado);

            if (rotinaAvisoTeleport != null)
                StopCoroutine(rotinaAvisoTeleport);

            rotinaAvisoTeleport = StartCoroutine(AvisoTeleportFaltando());
            return;
        }

        if (!requerChave)
        {
            AbrirBau();
        }
        else
        {
            if (KeySystem.TemChave(idChaveNecessaria))
            {
                AbrirBau();
            }
            else
            {
                if (audioSource && somTrancado)
                    audioSource.PlayOneShot(somTrancado);

                if (rotinaAvisoChave != null)
                    StopCoroutine(rotinaAvisoChave);

                rotinaAvisoChave = StartCoroutine(AvisoChaveFaltando());
            }
        }
    }

    private bool TeleportNecessarioFoiUsado()
    {
        if (!bloquearAteUsarTeleport)
            return true;

        if (string.IsNullOrEmpty(idTeleportNecessario))
            return true;

        if (TeleportArea.TeleportFoiUsadoNestaSessao(idTeleportNecessario))
            return true;

        if (PersistenciaManager.Instance == null)
            return false;

        bool usado =
            PersistenciaManager.Instance.ObterEstado(idTeleportNecessario + "_Usado", false) ||
            PersistenciaManager.Instance.ObterEstado(idTeleportNecessario, false);

        if (usado)
            TeleportArea.RegistrarTeleportUsadoExternamente(idTeleportNecessario);

        return usado;
    }

    public void ForcarEsconderMensagens()
    {
        if (rotinaAvisoChave != null)
        {
            StopCoroutine(rotinaAvisoChave);
            rotinaAvisoChave = null;
        }

        if (rotinaAvisoTeleport != null)
        {
            StopCoroutine(rotinaAvisoTeleport);
            rotinaAvisoTeleport = null;
        }

        mostrandoErro = false;

        if (textoPrecisaChave)
            textoPrecisaChave.SetActive(false);

        if (textoPrecisaTeleport)
            textoPrecisaTeleport.SetActive(false);

        if (textoInteragir)
            textoInteragir.SetActive(false);
    }

    void AbrirBau()
    {
        jaAbriu = true;
        
        if (textoInteragir)
            textoInteragir.SetActive(false);

        if (textoPrecisaChave)
            textoPrecisaChave.SetActive(false);

        if (textoPrecisaTeleport)
            textoPrecisaTeleport.SetActive(false);

        mostrandoErro = false;

        if (audioSource && somAbrir)
            audioSource.PlayOneShot(somAbrir);

        if (tampaDoBau)
        {
            StopAllCoroutines();
            StartCoroutine(AnimarTampa());
        }

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);

        if (requerChave)
        {
            KeySystem.GastarChave(idChaveNecessaria);

            if (iconeChaveParaEsconder != null)
                iconeChaveParaEsconder.SetActive(false);
        }

        if (InventarioRunas.Instance != null && !string.IsNullOrEmpty(nomeDaRunaNesteBau))
            InventarioRunas.Instance.ColetarRunaSemForcarSaveHD(nomeDaRunaNesteBau);

        if (darItemInventario && InventoryManager.Instance != null)
            InventoryManager.Instance.ReceberItem(idDoItemInventario);

        SalvarProgressoSeguro();

        StartCoroutine(SequenciaRecompensa());
    }

    IEnumerator AnimarTampa()
    {
        float t = 0;
        Quaternion startRot = tampaDoBau.localRotation;
        Quaternion endRot = Quaternion.Euler(-90, 0, 0);

        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            tampaDoBau.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        tampaDoBau.localRotation = endRot;
    }

    IEnumerator AvisoChaveFaltando()
    {
        mostrandoErro = true;

        if (textoInteragir)
            textoInteragir.SetActive(false);
        
        if (textoPrecisaChave)
        {
            textoPrecisaChave.SetActive(true);
            yield return new WaitForSeconds(2f);
            textoPrecisaChave.SetActive(false);
        }
        
        mostrandoErro = false;
        rotinaAvisoChave = null;

        if (!jaAbriu && estaOlhando && textoInteragir) 
            textoInteragir.SetActive(true);
    }

    IEnumerator AvisoTeleportFaltando()
    {
        mostrandoErro = true;

        if (textoInteragir)
            textoInteragir.SetActive(false);
        
        if (textoPrecisaTeleport)
        {
            textoPrecisaTeleport.SetActive(true);
            yield return new WaitForSeconds(2f);
            textoPrecisaTeleport.SetActive(false);
        }
        else if (textoPrecisaChave)
        {
            textoPrecisaChave.SetActive(true);
            yield return new WaitForSeconds(2f);
            textoPrecisaChave.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }
        
        mostrandoErro = false;
        rotinaAvisoTeleport = null;

        if (!jaAbriu && estaOlhando && textoInteragir) 
            textoInteragir.SetActive(true);
    }

    IEnumerator SequenciaRecompensa()
    {
        if (painelPretoRecompensa)
            painelPretoRecompensa.SetActive(true);
        
        if (textoRecompensa && !string.IsNullOrEmpty(nomeDaRunaNesteBau)) 
            textoRecompensa.text = "Você pegou a " + nomeDaRunaNesteBau + "!";
            
        if (painelCustomizadoDaRecompensa)
            painelCustomizadoDaRecompensa.SetActive(true);
        
        yield return new WaitForSecondsRealtime(3f);
        
        if (painelPretoRecompensa)
            painelPretoRecompensa.SetActive(false);

        if (painelCustomizadoDaRecompensa)
            painelCustomizadoDaRecompensa.SetActive(false);

        if (finalizaDemo)
        {
            if (FPS_Master.Instance != null)
                FPS_Master.Instance.AlterarEstadoJogador(true, false);
            
            if (painelFimDemo != null)
                painelFimDemo.SetActive(true);
            
            yield return new WaitForSecondsRealtime(tempoParaVoltarMenu);
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            UnityEngine.SceneManagement.SceneManager.LoadScene(nomeCenaMenu);
        }
    }

    private void SalvarProgressoSeguro()
    {
        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(true);
    }
}
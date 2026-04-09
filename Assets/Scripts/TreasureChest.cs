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

    void Start()
    {
        if(textoInteragir) textoInteragir.SetActive(false);
        if(textoPrecisaChave) textoPrecisaChave.SetActive(false);
        if(painelFimDemo) painelFimDemo.SetActive(false); 
        if(painelPretoRecompensa) painelPretoRecompensa.SetActive(false);
        if(painelCustomizadoDaRecompensa) painelCustomizadoDaRecompensa.SetActive(false);

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            jaAbriu = PersistenciaManager.Instance.ObterEstado(uniqueID);
            if (jaAbriu)
            {
                if (tampaDoBau) tampaDoBau.localRotation = Quaternion.Euler(-90, 0, 0);
            }
        }
    }

    public void AoOlhar()
    {
        if (jaAbriu) return;
        estaOlhando = true;

        if (!mostrandoErro && textoInteragir) 
        {
            textoInteragir.SetActive(true);
        }
    }

    public void AoSair()
    {
        estaOlhando = false;
        if (textoInteragir) textoInteragir.SetActive(false);
    }

    public void Interagir()
    {
        if (jaAbriu || mostrandoErro) return;

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
                if(audioSource && somTrancado) audioSource.PlayOneShot(somTrancado);
                StartCoroutine(AvisoChaveFaltando());
            }
        }
    }

    void AbrirBau()
    {
        jaAbriu = true;
        
        if (textoInteragir) textoInteragir.SetActive(false);
        if(audioSource && somAbrir) audioSource.PlayOneShot(somAbrir);

        if (tampaDoBau)
        {
            StopAllCoroutines();
            StartCoroutine(AnimarTampa());
        }

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);
        }

        if (requerChave)
        {
            KeySystem.GastarChave(idChaveNecessaria);
            if (iconeChaveParaEsconder != null) iconeChaveParaEsconder.SetActive(false);
        }

        if (InventarioRunas.Instance != null && !string.IsNullOrEmpty(nomeDaRunaNesteBau))
        {
            InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRunaNesteBau);
        }

        if (darItemInventario && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ReceberItem(idDoItemInventario);
        }

        if (!Application.isEditor && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarTudo();
        }

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
    }

    IEnumerator AvisoChaveFaltando()
    {
        mostrandoErro = true;

        if (textoInteragir) textoInteragir.SetActive(false);
        
        if (textoPrecisaChave)
        {
            textoPrecisaChave.SetActive(true);
            yield return new WaitForSeconds(2f);
            textoPrecisaChave.SetActive(false);
        }
        
        mostrandoErro = false;

        if (!jaAbriu && estaOlhando && textoInteragir) 
        {
            textoInteragir.SetActive(true);
        }
    }

    IEnumerator SequenciaRecompensa()
    {
        // Liga a sua UI antiga (se tiver)
        if(painelPretoRecompensa) painelPretoRecompensa.SetActive(true);
        
        // Escreve o texto antigo (se tiver)
        if(textoRecompensa && !string.IsNullOrEmpty(nomeDaRunaNesteBau)) 
            textoRecompensa.text = "Você pegou a " + nomeDaRunaNesteBau + "!";
            
        // LIGA O SEU PAINEL NOVO
        if(painelCustomizadoDaRecompensa) painelCustomizadoDaRecompensa.SetActive(true);
        
        // Fica exatos 3 segundos
        yield return new WaitForSecondsRealtime(3f);
        
        // Desliga a porra toda
        if(painelPretoRecompensa) painelPretoRecompensa.SetActive(false);
        if(painelCustomizadoDaRecompensa) painelCustomizadoDaRecompensa.SetActive(false);

        if (finalizaDemo)
        {
            if (FPS_Master.Instance != null)
                FPS_Master.Instance.AlterarEstadoJogador(true, false);
            
            if (painelFimDemo != null) painelFimDemo.SetActive(true);
            
            yield return new WaitForSecondsRealtime(tempoParaVoltarMenu);
            
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(nomeCenaMenu);
        }
    }
}
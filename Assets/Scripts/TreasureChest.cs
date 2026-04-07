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
    
    [Header("--- SISTEMA DE CHAVE ---")]
    public bool requerChave = false;  
    [Tooltip("ID da chave necessária (Ex: Chave_Porao)")]
    public string idChaveNecessaria; 

    [Header("--- DEMO MODE ---")]
    [Tooltip("Se marcado, abrir este baú FINALIZA A DEMO.")]
    public bool finalizaDemo = false;
    public GameObject painelFimDemo; 
    public string nomeCenaMenu = "Menu";
    public float tempoParaVoltarMenu = 5f;

    [Header("UI & Mensagens")]
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
    private bool estaOlhando = false; // A trava que faltava pra não bugar o texto
    private bool mostrandoErro = false; // Trava pro erro não bugar se spammar o clique

    void Start()
    {
        if(textoInteragir) textoInteragir.SetActive(false);
        if(textoPrecisaChave) textoPrecisaChave.SetActive(false);
        if(painelFimDemo) painelFimDemo.SetActive(false); 
        if(painelPretoRecompensa) painelPretoRecompensa.SetActive(false);

        // CARREGA ESTADO
        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            jaAbriu = PersistenciaManager.Instance.ObterEstado(uniqueID);
            if (jaAbriu)
            {
                if (tampaDoBau) tampaDoBau.localRotation = Quaternion.Euler(-90, 0, 0);
            }
        }
    }

    // --- SISTEMA RAYCAST ---

    public void AoOlhar()
    {
        if (jaAbriu) return;
        
        estaOlhando = true;

        // Só acende o botão E se não estiver tocando a animação de erro de chave
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
    // -----------------------

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

        // A MÁGICA: Só liga o texto de interagir de volta se a porra do jogador AINDA estiver olhando
        if (!jaAbriu && estaOlhando && textoInteragir) 
        {
            textoInteragir.SetActive(true);
        }
    }

    IEnumerator SequenciaRecompensa()
    {
        if(painelPretoRecompensa) painelPretoRecompensa.SetActive(true);
        if(textoRecompensa) textoRecompensa.text = "Você pegou a " + nomeDaRunaNesteBau + "!";
        
        yield return new WaitForSeconds(3f);
        
        if(painelPretoRecompensa) painelPretoRecompensa.SetActive(false);

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
using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections;

public class DoorWithKeyTeleport : MonoBehaviour
{
    [Header("--- SAVE SYSTEM ---")]
    [Tooltip("Dê um nome único. Ex: Porta_Teleporte_01")]
    public string uniqueID = "Porta_Teleporte_01"; 

    [Header("--- DESTINO ---")]
    public Transform destinationPoint; 
    public float verticalOffset = 0.1f; 

    [Header("--- SISTEMA DE CHAVE ---")]
    public bool precisaDeChave = true;
    public string idDaChaveNecessaria = "Chave_Porao";

    [Header("--- UI DA CHAVE (PRA SUMIR) ---")]
    public GameObject iconeChaveHUD; 
    
    [Header("--- UI & FEEDBACK ---")]
    public GameObject textoInteragir; 
    public GameObject painelMensagemTrancada; 
    public TextMeshProUGUI textoFeedback; 
    public Image fadeImage; 

    [Header("--- AUDIO ---")]
    public AudioSource audioSource;
    public AudioClip somTrancado;
    public AudioClip somDestrancar;
    public AudioClip somTeleporte;

    private bool estaAberta = false;
    private bool emTransicao = false;
    private bool mostrandoMensagem = false; 

    void Start() 
    {
        if (textoInteragir) textoInteragir.SetActive(false);
        if (painelMensagemTrancada) painelMensagemTrancada.SetActive(false);
        
        if (fadeImage) 
        {
            fadeImage.gameObject.SetActive(true);
            fadeImage.color = new Color(0,0,0,0);
        }

        // --- PUXA A INTELIGÊNCIA DO SEU SAVE SYSTEM ---
        if (PersistenciaManager.Instance != null)
        {
            // Se não tem save, retorna false. Se tiver, retorna o estado da porta.
            estaAberta = PersistenciaManager.Instance.CarregarEstadoObjeto(uniqueID, false);
            
            if (estaAberta)
            {
                precisaDeChave = false;
                if (iconeChaveHUD) iconeChaveHUD.SetActive(false); 
            }
        }
    }

    public void AoOlhar()
    {
        if (emTransicao) return;
        if (textoInteragir) textoInteragir.SetActive(true);
    }

    public void AoSair()
    {
        if (textoInteragir) textoInteragir.SetActive(false);
        if (painelMensagemTrancada) painelMensagemTrancada.SetActive(false);
    }

    public void Interagir()
    {
        if (emTransicao) return;

        if (!precisaDeChave || estaAberta)
        {
            StartCoroutine(SequenciaTeleporte());
            return;
        }

        if (KeySystem.TemChave(idDaChaveNecessaria))
        {
            AbrirPorta();
        }
        else if (!mostrandoMensagem)
        {
            StartCoroutine(FeedbackTrancado());
        }
    }

    void AbrirPorta()
    {
        estaAberta = true;
        
        KeySystem.GastarChave(idDaChaveNecessaria);
        
        if (iconeChaveHUD != null) iconeChaveHUD.SetActive(false);

        // --- REGISTRA NO SEU SAVE SYSTEM ---
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);
        }

        if (audioSource && somDestrancar) audioSource.PlayOneShot(somDestrancar);
        
        StartCoroutine(SequenciaTeleporte());
    }

    IEnumerator FeedbackTrancado()
    {
        mostrandoMensagem = true; 
        
        if (audioSource && somTrancado) audioSource.PlayOneShot(somTrancado);
        
        if (painelMensagemTrancada) 
        {
            painelMensagemTrancada.SetActive(true);
            if (textoFeedback) textoFeedback.text = "Precisa da " + idDaChaveNecessaria.Replace("_", " ");
            yield return new WaitForSeconds(2f);
            painelMensagemTrancada.SetActive(false);
        }
        
        mostrandoMensagem = false; 
    }

    IEnumerator SequenciaTeleporte()
    {
        emTransicao = true;
        if (textoInteragir) textoInteragir.SetActive(false);
        
        if (FPS_Master.Instance) FPS_Master.Instance.AlterarEstadoJogador(true, false);

        if (audioSource && somTeleporte) audioSource.PlayOneShot(somTeleporte);

        if (fadeImage) 
        {
            fadeImage.gameObject.SetActive(true);
            float a = 0; 
            while(a < 1) { a += Time.deltaTime * 3; fadeImage.color = new Color(0,0,0,a); yield return null; }
        }

        yield return new WaitForSeconds(0.5f);

        if (FPS_Master.Instance && destinationPoint)
        {
            FPS_Master.Instance.Teleportar(destinationPoint.position + (Vector3.up * verticalOffset));
            FPS_Master.Instance.transform.rotation = destinationPoint.rotation;
            Physics.SyncTransforms();
        }

        yield return new WaitForSeconds(0.5f);

        if (fadeImage) 
        {
            float a = 1; 
            while(a > 0) { a -= Time.deltaTime * 2; fadeImage.color = new Color(0,0,0,a); yield return null; }
            fadeImage.gameObject.SetActive(false);
        }

        if (FPS_Master.Instance) FPS_Master.Instance.AlterarEstadoJogador(false, false);
        emTransicao = false;
    }
}
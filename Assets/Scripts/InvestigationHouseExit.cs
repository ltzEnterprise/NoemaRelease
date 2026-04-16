using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class InvestigationHouseExit : MonoBehaviour
{
    [Header("--- CONFIGURAÇÃO ---")]
    public string nomeDaCenaPrincipal = "DreamSceane";
    public string nomeDaRunaNecessaria = "Runa_Investigacao"; 

    [Header("--- UI E FEEDBACK ---")]
    public GameObject textoPortaTrancada; 
    public AudioSource fonteAudio;
    public AudioClip somTrancado;    
    public AudioClip somSairDaCasa;  

    private bool jaEstaSaindo = false;

    void Start()
    {
        if(textoPortaTrancada) textoPortaTrancada.SetActive(false);
    }

    public void Interagir()
    {
        if (jaEstaSaindo) return;

        if (InventarioRunas.Instance != null && InventarioRunas.Instance.TemARunaPeloNome(nomeDaRunaNecessaria))
        {
            StartCoroutine(SairDaCasaComSom());
        }
        else
        {
            StopAllCoroutines();
            StartCoroutine(AvisoTrancado());
        }
    }

    IEnumerator SairDaCasaComSom()
    {
        jaEstaSaindo = true;
        
        if (FPS_Master.Instance != null) FPS_Master.travadoInteracao = true;

        if (somSairDaCasa && fonteAudio) 
        {
            fonteAudio.PlayOneShot(somSairDaCasa);
            yield return new WaitForSeconds(Mathf.Min(somSairDaCasa.length, 1f)); 
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (SistemaGlobal.Instance != null)
        {
            SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = true;
        }

        // 🔥 A TRAVA ANTICRASH AQUI 🔥
        // Se a engine crashar durante o LoadScene, a RAM inteira vai pro ralo.
        // A gente salva TUDO (incluindo as runas que estavam só na RAM) pro HD no milissegundo antes da transição.
        if (PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.SalvarTudo();
        }

        SceneManager.LoadScene(nomeDaCenaPrincipal);
    }

    IEnumerator AvisoTrancado()
    {
        if (somTrancado && fonteAudio) fonteAudio.PlayOneShot(somTrancado);
        
        if (textoPortaTrancada) textoPortaTrancada.SetActive(true);
        yield return new WaitForSeconds(2.5f);
        if (textoPortaTrancada) textoPortaTrancada.SetActive(false);
    }
}
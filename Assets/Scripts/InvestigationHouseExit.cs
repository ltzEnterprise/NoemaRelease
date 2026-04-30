using UnityEngine; 
using UnityEngine.SceneManagement;
using System.Collections;

public class InvestigationHouseExit : MonoBehaviour
{
    [Header("--- CONFIGURAÇÃO ---")]
    public string nomeDaCenaPrincipal = "DreamSceane";

    [Header("--- UI E FEEDBACK ---")]
    public GameObject textoPortaTrancada; 
    public AudioSource fonteAudio;
    public AudioClip somTrancado;    
    public AudioClip somSairDaCasa;  

    private bool jaEstaSaindo = false;

    void Start()
    {
        if (textoPortaTrancada)
            textoPortaTrancada.SetActive(false);
    }

    public void Interagir()
    {
        if (jaEstaSaindo) return;

        StartCoroutine(SairDaCasaComSom());
    }

    IEnumerator SairDaCasaComSom()
    {
        jaEstaSaindo = true;
        
        if (textoPortaTrancada)
            textoPortaTrancada.SetActive(false);

        if (FPS_Master.Instance != null)
            FPS_Master.travadoInteracao = true;

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
            SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = false;
        }

        SalvarSaidaDaCasa();

        SceneManager.LoadScene(nomeDaCenaPrincipal);
    }

    private void SalvarSaidaDaCasa()
    {
        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(true);
    }

    IEnumerator AvisoTrancado()
    {
        if (somTrancado && fonteAudio)
            fonteAudio.PlayOneShot(somTrancado);
        
        if (textoPortaTrancada)
            textoPortaTrancada.SetActive(true);

        yield return new WaitForSeconds(2.5f);

        if (textoPortaTrancada)
            textoPortaTrancada.SetActive(false);
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class InvestigationHouseExit : MonoBehaviour
{
    [Header("--- CONFIGURAÇÃO ---")]
    public string nomeDaCenaPrincipal = "DreamSceane";
    public string nomeDaRunaNecessaria = "Runa_Investigacao"; // Mesmo nome do Manager

    [Header("--- UI E FEEDBACK ---")]
    public GameObject textoPortaTrancada; 
    public AudioSource fonteAudio;
    public AudioClip somTrancado;    // Som de "Tuc tuc", está trancada ainda
    public AudioClip somSairDaCasa;  // Som da maçaneta abrindo pra ir embora

    private bool jaEstaSaindo = false;

    void Start()
    {
        if(textoPortaTrancada) textoPortaTrancada.SetActive(false);
        // Não toca mais som de trancar aqui, o Manager já faz isso
    }

    // CHAMADO PELO RAYCAST DO JOGADOR
    public void Interagir()
    {
        if (jaEstaSaindo) return;

        // Verifica no Singleton se a runa foi pega (pelo Manager)
        if (InventarioRunas.Instance != null && InventarioRunas.Instance.TemARunaPeloNome(nomeDaRunaNecessaria))
        {
            StartCoroutine(SairDaCasaComSom());
        }
        else
        {
            // Se tentar abrir sem resolver o puzzle
            StopAllCoroutines();
            StartCoroutine(AvisoTrancado());
        }
    }

    IEnumerator SairDaCasaComSom()
    {
        jaEstaSaindo = true;
        
        // Trava o Player
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

        // Avisa o SistemaGlobal para posicionar o player na frente da porta na outra cena
        if (SistemaGlobal.Instance != null)
        {
            SistemaGlobal.Instance.deveCarregarPosicaoAoIniciar = true;
        }

        // Como InventarioRunas e InventoryManager são DontDestroyOnLoad,
        // tudo o que você pegou aqui vai junto.
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
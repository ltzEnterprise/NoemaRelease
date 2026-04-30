using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GerenciadorCutscene : MonoBehaviour
{
    public static GerenciadorCutscene Instance;

    [Header("--- SAVE SYSTEM ---")]
    [Tooltip("ID único desta cutscene. Ex: Cutscene_ChaveGigante_Final")]
    public string uniqueID = "Cutscene_ChaveGigante_Final";
    public bool executarApenasUmaVez = true;

    [Header("Objetos Principais")]
    public FPS_Master playerMaster; 
    public Camera cameraDoJogador;  
    public GameObject chaveGigante; 
    public Camera cameraCutscene;   

    [Header("--- UI ---")]
    [Tooltip("Arraste aqui a imagem da retícula/crosshair para ela sumir durante a cutscene.")]
    public GameObject reticula;

    [Header("Efeitos Finais")]
    public CanvasGroup grupoFadeBranco; 
    public float duracaoDissolver = 2.0f;

    [Header("Configuração das Cenas")]
    [Tooltip("Tempo que o jogo espera antes de trocar a câmera (para o flash da foto sumir)")]
    public float atrasoParaIniciar = 1.5f; 
    public Transform[] angulosDeCamera; 
    public float duracaoCadaCena = 4.0f;
    public float velocidadeAfastar = 0.5f;

    [Header("Áudio")]
    public AudioSource audioSourceSFX;   
    public AudioSource audioSourceMusic; 
    public AudioClip somInicioCutscene;  
    public AudioClip musicaCutscene;     
    public AudioClip somFlashFinal;      

    private bool emCutscene = false;
    private bool cutsceneJaExecutada = false;
    private bool reticulaEstadoAnterior = true;

    void Awake()
    {
        Instance = this;

        if (cameraCutscene) cameraCutscene.gameObject.SetActive(false);
        if (chaveGigante) chaveGigante.SetActive(false);

        if (grupoFadeBranco) 
        {
            grupoFadeBranco.alpha = 0f;
            grupoFadeBranco.gameObject.SetActive(false);
        }
    }

    void Start()
    {
        StartCoroutine(CarregarEstadoSeguro());
    }

    IEnumerator CarregarEstadoSeguro()
    {
        if (PersistenciaManager.Instance != null)
        {
            yield return new WaitUntil(() =>
                PersistenciaManager.Instance.DadosProntosParaUso &&
                !PersistenciaManager.Instance.EstaCarregando
            );

            if (!string.IsNullOrEmpty(uniqueID))
                cutsceneJaExecutada = PersistenciaManager.Instance.ObterEstado(uniqueID + "_Visto", false);
        }
    }

    public void IniciarFinal()
    {
        if (emCutscene) return;

        if (executarApenasUmaVez && cutsceneJaExecutada)
        {
            Debug.Log($"[GerenciadorCutscene] Cutscene '{uniqueID}' já foi vista. Ignorando repetição.");
            return;
        }

        StartCoroutine(RotinaCutscene());
    }

    IEnumerator RotinaCutscene()
    {
        emCutscene = true;
        EsconderReticula();

        if (playerMaster) 
            playerMaster.AlterarEstadoJogador(true, false); 

        yield return new WaitForSeconds(atrasoParaIniciar);

        if (cameraDoJogador) cameraDoJogador.gameObject.SetActive(false); 
        if (cameraCutscene) cameraCutscene.gameObject.SetActive(true);

        if (audioSourceSFX && somInicioCutscene) audioSourceSFX.PlayOneShot(somInicioCutscene);
        if (audioSourceMusic && musicaCutscene)
        {
            audioSourceMusic.clip = musicaCutscene;
            audioSourceMusic.volume = 1f;
            audioSourceMusic.Play();
        }

        if (angulosDeCamera != null)
        {
            foreach (Transform angulo in angulosDeCamera)
            {
                if (angulo == null) continue;

                if (cameraCutscene)
                {
                    cameraCutscene.transform.position = angulo.position;
                    cameraCutscene.transform.rotation = angulo.rotation;
                }

                float tempoCam = 0;
                while (tempoCam < duracaoCadaCena)
                {
                    if (cameraCutscene)
                        cameraCutscene.transform.Translate(Vector3.back * velocidadeAfastar * Time.deltaTime);

                    tempoCam += Time.deltaTime;
                    yield return null;
                }
            }
        }

        if (audioSourceMusic != null && audioSourceMusic.isPlaying)
        {
            float volInicial = audioSourceMusic.volume;
            float tempoSom = 0;
            while (tempoSom < 1.0f) 
            {
                tempoSom += Time.deltaTime;
                audioSourceMusic.volume = Mathf.Lerp(volInicial, 0f, tempoSom);
                yield return null;
            }
            audioSourceMusic.Stop();
            audioSourceMusic.volume = volInicial; 
        }

        if (grupoFadeBranco)
        {
            grupoFadeBranco.gameObject.SetActive(true);
            grupoFadeBranco.alpha = 1f; 
        }

        if (audioSourceSFX && somFlashFinal) audioSourceSFX.PlayOneShot(somFlashFinal);
        if (chaveGigante) chaveGigante.SetActive(true);

        if (cameraDoJogador) cameraDoJogador.gameObject.SetActive(true);

        if (playerMaster)
            playerMaster.AlterarEstadoJogador(false, false); 

        if (cameraCutscene) cameraCutscene.gameObject.SetActive(false);

        SalvarCutsceneVista();

        yield return new WaitForSeconds(0.5f);

        if (grupoFadeBranco)
        {
            float tempoFade = 0;
            while (tempoFade < duracaoDissolver)
            {
                tempoFade += Time.deltaTime;
                grupoFadeBranco.alpha = Mathf.Lerp(1f, 0f, tempoFade / duracaoDissolver);
                yield return null;
            }
            grupoFadeBranco.alpha = 0f;
            grupoFadeBranco.gameObject.SetActive(false);
        }

        RestaurarReticula();
        emCutscene = false;
    }

    private void EsconderReticula()
    {
        if (reticula == null) return;
        reticulaEstadoAnterior = reticula.activeSelf;
        reticula.SetActive(false);
    }

    private void RestaurarReticula()
    {
        if (reticula == null) return;
        reticula.SetActive(reticulaEstadoAnterior);
    }

    private void SalvarCutsceneVista()
    {
        cutsceneJaExecutada = true;

        if (!string.IsNullOrEmpty(uniqueID) && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_Visto", true);

            if (GameManager.Instance != null && GameManager.CenaPronta)
                GameManager.Instance.SalvarProgresso();
            else
                PersistenciaManager.Instance.SalvarTudo(true);
        }
    }

    void OnDisable()
    {
        if (emCutscene)
            RestaurarReticula();
    }
}

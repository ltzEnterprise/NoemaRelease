using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GerenciadorCutscene : MonoBehaviour
{
    public static GerenciadorCutscene Instance;

    [Header("Objetos Principais")]
    public FPS_Master playerMaster; 
    public Camera cameraDoJogador;  
    public GameObject chaveGigante; 
    public Camera cameraCutscene;   

    [Header("Efeitos Finais")]
    public CanvasGroup grupoFadeBranco; 
    public float duracaoDissolver = 2.0f;

    [Header("Configuração das Cenas")]
    [Tooltip("Tempo que o jogo espera antes de trocar a câmera (para o flash da foto sumir)")]
    public float atrasoParaIniciar = 1.5f; // <--- O ATRASO AQUI
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

    public void IniciarFinal()
    {
        if (emCutscene) return;
        StartCoroutine(RotinaCutscene());
    }

    IEnumerator RotinaCutscene()
    {
        emCutscene = true;
        
        // 1. Tira controle limpo IMEDIATAMENTE (o cara tirou a foto e congela no lugar)
        if (playerMaster) 
        {
            playerMaster.AlterarEstadoJogador(true, false); 
        }

        // 2. ESPERA O FLASH DA CÂMERA FOTOGRÁFICA SUMIR DA TELA
        yield return new WaitForSeconds(atrasoParaIniciar);

        // 3. Agora sim, com a tela limpa, desliga a câmera do jogador
        if (cameraDoJogador) cameraDoJogador.gameObject.SetActive(false); 
        if (cameraCutscene) cameraCutscene.gameObject.SetActive(true);

        // 4. Áudio
        if (audioSourceSFX && somInicioCutscene) audioSourceSFX.PlayOneShot(somInicioCutscene);
        if (audioSourceMusic && musicaCutscene)
        {
            audioSourceMusic.clip = musicaCutscene;
            audioSourceMusic.volume = 1f;
            audioSourceMusic.Play();
        }

        // 5. Câmeras (Cutscene rolando)
        if (angulosDeCamera != null)
        {
            foreach (Transform angulo in angulosDeCamera)
            {
                if (angulo == null) continue;
                cameraCutscene.transform.position = angulo.position;
                cameraCutscene.transform.rotation = angulo.rotation;

                float tempoCam = 0;
                while (tempoCam < duracaoCadaCena)
                {
                    cameraCutscene.transform.Translate(Vector3.back * velocidadeAfastar * Time.deltaTime);
                    tempoCam += Time.deltaTime;
                    yield return null;
                }
            }
        }

        // 6. Fade Out Música
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

        // 7. Flash Branco Final (Da transição de cena, não o da foto)
        if (grupoFadeBranco)
        {
            grupoFadeBranco.gameObject.SetActive(true);
            grupoFadeBranco.alpha = 1f; 
        }
        
        if (audioSourceSFX && somFlashFinal) audioSourceSFX.PlayOneShot(somFlashFinal);
        if (chaveGigante) chaveGigante.SetActive(true);

        // 8. Volta controle
        if (cameraDoJogador) cameraDoJogador.gameObject.SetActive(true);

        if (playerMaster)
        {
            playerMaster.AlterarEstadoJogador(false, false); 
        }
        
        if (cameraCutscene) cameraCutscene.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        // 9. Dissolver Suave
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

        emCutscene = false;
    }
}
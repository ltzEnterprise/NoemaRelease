using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CastleDoor : MonoBehaviour
{
    [Header("--- SAVE SYSTEM (OPCIONAL) ---")]
    [Tooltip("Preencha se quiser salvar que a barricada/tábuas foram quebradas.")]
    public string uniqueID;

    [Header("Configuração de Teleporte")]
    public Transform pontoDestino;

    [Tooltip("Altura extra aplicada no destino para evitar nascer dentro/debaixo do chão.")]
    public float offsetVerticalTeleporte = 0.15f;
    
    [Header("Barricada / Tábuas")]
    public GameObject grupoTabuas;
    public AudioClip somQuebrarTabua; 

    [Header("Lógica do Jogo")]
    public bool verificarCasasCompletas = true;

    [Header("Fade (Visual)")]
    public Image painelPreto; 
    public float velocidadeFade = 2f;

    [Header("UI & Mensagens")]
    public GameObject textoBloqueado;
    public GameObject textoInteragir;

    [Header("Sons")]
    public AudioSource audioSource;
    public AudioClip somTeleporte; 
    public AudioClip somTrancado;

    private bool emProcesso = false;
    private bool estaOlhando = false;

    void Start()
    {
        if (textoBloqueado) textoBloqueado.SetActive(false);
        if (textoInteragir) textoInteragir.SetActive(false);
        
        if (painelPreto) 
        {
            painelPreto.gameObject.SetActive(false);
            painelPreto.color = new Color(0, 0, 0, 0);
        }

        StartCoroutine(CarregarEstadoSeguro());
    }

    IEnumerator CarregarEstadoSeguro()
    {
        if (PersistenciaManager.Instance != null)
            yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            bool barricadaQuebrada = PersistenciaManager.Instance.ObterEstado(uniqueID + "_barricadaQuebrada", false);

            if (barricadaQuebrada && grupoTabuas != null)
                grupoTabuas.SetActive(false);
        }
    }

    public void AoOlhar()
    {
        if (emProcesso) return;
        estaOlhando = true;

        if (textoBloqueado && textoBloqueado.activeSelf) return;

        if (textoInteragir) textoInteragir.SetActive(true);
    }

    public void AoSair()
    {
        estaOlhando = false;
        
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoBloqueado) textoBloqueado.SetActive(false);
    }

    public void Interagir()
    {
        if (emProcesso) return;

        if (grupoTabuas != null && grupoTabuas.activeSelf)
        {
            QuebrarBarricada();
            return;
        }

        TentarAtravessar();
    }

    void QuebrarBarricada()
    {
        if (audioSource && somQuebrarTabua)
            audioSource.PlayOneShot(somQuebrarTabua);

        if (grupoTabuas)
            grupoTabuas.SetActive(false);

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
        {
            PersistenciaManager.Instance.RegistrarEstado(uniqueID + "_barricadaQuebrada", true);
            PersistenciaManager.Instance.SalvarTudo(true);
        }
        
        if (textoInteragir)
            textoInteragir.SetActive(true);
    }

    void TentarAtravessar()
    {
        if (verificarCasasCompletas)
        {
            bool c1 = EstadoGlobal.casasResolvidas != null && EstadoGlobal.casasResolvidas.Length > 0 && EstadoGlobal.casasResolvidas[0];
            bool c2 = EstadoGlobal.casasResolvidas != null && EstadoGlobal.casasResolvidas.Length > 1 && EstadoGlobal.casasResolvidas[1];
            bool c3 = EstadoGlobal.casasResolvidas != null && EstadoGlobal.casasResolvidas.Length > 2 && EstadoGlobal.casasResolvidas[2];

            if (c1 && c2 && c3)
            {
                StartCoroutine(RotinaTeleporte());
            }
            else
            {
                if (audioSource && somTrancado)
                    audioSource.PlayOneShot(somTrancado);

                StopCoroutine("MostrarAvisoBloqueado");
                StartCoroutine("MostrarAvisoBloqueado");
            }
        }
        else
        {
            StartCoroutine(RotinaTeleporte());
        }
    }

    IEnumerator RotinaTeleporte()
    {
        emProcesso = true;

        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoBloqueado) textoBloqueado.SetActive(false);

        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(true, false);
        
        if (audioSource && somTeleporte)
            audioSource.PlayOneShot(somTeleporte);

        if (painelPreto)
        {
            painelPreto.gameObject.SetActive(true);

            float alpha = 0;

            while (alpha < 1)
            {
                alpha += Time.deltaTime * velocidadeFade;
                painelPreto.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.5f);

        if (FPS_Master.Instance != null && pontoDestino)
        {
            Vector3 destinoSeguro = pontoDestino.position + Vector3.up * offsetVerticalTeleporte;

            FPS_Master.Instance.Teleportar(destinoSeguro);
            FPS_Master.Instance.transform.rotation = pontoDestino.rotation;

            Physics.SyncTransforms();
        }

        SalvarProgressoSeguro();

        yield return new WaitForSeconds(0.5f);

        if (painelPreto)
        {
            float alpha = 1;

            while (alpha > 0)
            {
                alpha -= Time.deltaTime * velocidadeFade;
                painelPreto.color = new Color(0, 0, 0, alpha);
                yield return null;
            }

            painelPreto.gameObject.SetActive(false);
        }

        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(false, false);
            
        emProcesso = false;
    }

    void SalvarProgressoSeguro()
    {
        if (GameManager.Instance != null && GameManager.CenaPronta)
        {
            GameManager.Instance.SalvarProgresso();
            return;
        }

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(true);
    }

    IEnumerator MostrarAvisoBloqueado()
    {
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoBloqueado) textoBloqueado.SetActive(true);
        
        yield return new WaitForSeconds(3f);
        
        if (textoBloqueado) textoBloqueado.SetActive(false);
        
        if (!emProcesso && estaOlhando && textoInteragir)
            textoInteragir.SetActive(true);
    }
}
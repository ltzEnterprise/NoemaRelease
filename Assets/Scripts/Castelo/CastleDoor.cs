using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CastleDoor : MonoBehaviour
{
    [Header("Configuração de Teleporte")]
    public Transform pontoDestino; // Onde o player vai sair
    
    [Header("Barricada / Tábuas")]
    public GameObject grupoTabuas; // Se tiver tábuas na frente, arraste aqui
    public AudioClip somQuebrarTabua; 

    [Header("Lógica do Jogo")]
    public bool verificarCasasCompletas = true; // True na entrada, False na saída

    [Header("Fade (Visual)")]
    public Image painelPreto; 
    public float velocidadeFade = 2f;

    [Header("UI & Mensagens")]
    public GameObject textoBloqueado; // "Trancado..."
    public GameObject textoInteragir; // "Entrar" ou "Sair"

    [Header("Sons")]
    public AudioSource audioSource;
    public AudioClip somTeleporte; 
    public AudioClip somTrancado;

    // Controle interno
    private bool emProcesso = false;
    private bool estaOlhando = false; // <--- Variável adicionada pra não bugar o texto

    void Start()
    {
        if (textoBloqueado) textoBloqueado.SetActive(false);
        if (textoInteragir) textoInteragir.SetActive(false);
        
        if (painelPreto) 
        {
            painelPreto.gameObject.SetActive(false);
            painelPreto.color = new Color(0, 0, 0, 0);
        }
    }

    // --- MÉTODOS RAYCAST ---
    public void AoOlhar()
    {
        if (emProcesso) return;
        estaOlhando = true;

        // Se o aviso de trancado tiver na tela, não sobrepõe ele com o "Entrar"
        if (textoBloqueado && textoBloqueado.activeSelf) return;

        if (textoInteragir) textoInteragir.SetActive(true);
    }

    public void AoSair()
    {
        estaOlhando = false;
        
        // Desliga tudo imediatamente quando virar as costas
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoBloqueado) textoBloqueado.SetActive(false);
    }

    public void Interagir()
    {
        if (emProcesso) return;

        // 1. Verifica Barricada (se existir)
        if (grupoTabuas != null && grupoTabuas.activeSelf)
        {
            QuebrarBarricada();
            return;
        }

        // 2. Tenta Entrar
        TentarAtravessar();
    }
    // -----------------------

    void QuebrarBarricada()
    {
        if (audioSource && somQuebrarTabua) audioSource.PlayOneShot(somQuebrarTabua);
        if (grupoTabuas) grupoTabuas.SetActive(false);
        
        // Atualiza visual instantaneamente se o jogador ainda estiver olhando
        if (textoInteragir) textoInteragir.SetActive(true);
    }

    void TentarAtravessar()
    {
        if (verificarCasasCompletas)
        {
            // Checa as 3 casas no EstadoGlobal
            bool c1 = EstadoGlobal.casasResolvidas != null && EstadoGlobal.casasResolvidas.Length > 0 && EstadoGlobal.casasResolvidas[0];
            bool c2 = EstadoGlobal.casasResolvidas != null && EstadoGlobal.casasResolvidas.Length > 1 && EstadoGlobal.casasResolvidas[1];
            bool c3 = EstadoGlobal.casasResolvidas != null && EstadoGlobal.casasResolvidas.Length > 2 && EstadoGlobal.casasResolvidas[2];

            if (c1 && c2 && c3)
            {
                StartCoroutine(RotinaTeleporte());
            }
            else
            {
                if (audioSource && somTrancado) audioSource.PlayOneShot(somTrancado);
                StopCoroutine("MostrarAvisoBloqueado");
                StartCoroutine("MostrarAvisoBloqueado");
            }
        }
        else
        {
            // Porta de saída (sem tranca)
            StartCoroutine(RotinaTeleporte());
        }
    }

    IEnumerator RotinaTeleporte()
    {
        emProcesso = true;
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoBloqueado) textoBloqueado.SetActive(false);

        // Trava o jogador (WASD 0, Gravidade ON)
        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(true, false);
        
        if (audioSource && somTeleporte) audioSource.PlayOneShot(somTeleporte);

        // FADE OUT
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

        // TELEPORTE SEGURO
        if (FPS_Master.Instance != null && pontoDestino)
        {
            FPS_Master.Instance.Teleportar(pontoDestino.position);
            
            // Ajustamos a rotação
            FPS_Master.Instance.transform.rotation = pontoDestino.rotation;
            Physics.SyncTransforms();
        }

        yield return new WaitForSeconds(0.5f);

        // FADE IN
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

        // Destrava
        if (FPS_Master.Instance != null)
            FPS_Master.Instance.AlterarEstadoJogador(false, false);
            
        emProcesso = false;
    }

    IEnumerator MostrarAvisoBloqueado()
    {
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoBloqueado) textoBloqueado.SetActive(true);
        
        yield return new WaitForSeconds(3f);
        
        if (textoBloqueado) textoBloqueado.SetActive(false);
        
        // Só volta o texto original se o jogador ainda estiver com a mira na porta
        if (!emProcesso && estaOlhando && textoInteragir) textoInteragir.SetActive(true);
    }
}
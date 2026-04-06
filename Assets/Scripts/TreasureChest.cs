using UnityEngine;
using System.Collections;
using TMPro;

public class TreasureChest : MonoBehaviour
{
    [Header("--- BLOQUEADOR ---")]
    [Tooltip("Coloque o objeto que bloqueia (ex: plasma). Se ficar vazio, funciona normal.")]
    public GameObject bloqueador;

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
    private Collider colisorDoBau;
    private bool estaOlhando = false; // A trava que faltava pra não bugar o texto

    // Função que checa em tempo real se a parada tá bloqueada
    private bool TaBloqueado()
    {
        return bloqueador != null && bloqueador.activeInHierarchy;
    }

    void Start()
    {
        if(textoInteragir) textoInteragir.SetActive(false);
        if(textoPrecisaChave) textoPrecisaChave.SetActive(false);
        if(painelFimDemo) painelFimDemo.SetActive(false); 
        if(painelPretoRecompensa) painelPretoRecompensa.SetActive(false);

        colisorDoBau = GetComponent<Collider>();

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

    void Update()
    {
        // Monitoramento constante do bloqueador
        if (TaBloqueado())
        {
            if (textoInteragir && textoInteragir.activeSelf) textoInteragir.SetActive(false);
            if (textoPrecisaChave && textoPrecisaChave.activeSelf) textoPrecisaChave.SetActive(false);
        }
    }

    // --- O RADAR ANTI-PAREDE TÁ AQUI ---
    bool ChecarVisaoLimpa()
    {
        Camera cam = Camera.main;
        if (cam == null) return false;

        // Pega o centro exato do baú para evitar que o raio bata no chão e falhe
        Vector3 centroDoBau = colisorDoBau != null ? colisorDoBau.bounds.center : transform.position;
        Vector3 direcao = centroDoBau - cam.transform.position;
        float distancia = direcao.magnitude;

        RaycastHit hit;
        // Dispara um raio do seu olho pro centro do baú. Ignora Triggers invisíveis.
        if (Physics.Raycast(cam.transform.position, direcao, out hit, distancia, ~0, QueryTriggerInteraction.Ignore))
        {
            // Se o raio bateu em algo que NÃO é o baú ou a tampa dele, tem uma parede no meio
            if (hit.transform != transform && !hit.transform.IsChildOf(transform))
            {
                return false; 
            }
        }
        return true; // Visão 100% limpa, sem paredes
    }

    // --- SISTEMA RAYCAST ---

    public void AoOlhar()
    {
        if (TaBloqueado()) return; // Morre aqui se tiver bloqueado
        if (jaAbriu) return;

        // Só acende o texto se não tiver parede na frente
        if (ChecarVisaoLimpa())
        {
            estaOlhando = true;
            if (textoInteragir && !textoInteragir.activeSelf) textoInteragir.SetActive(true);
        }
        else
        {
            estaOlhando = false;
            if (textoInteragir && textoInteragir.activeSelf) textoInteragir.SetActive(false);
        }
    }

    public void AoSair()
    {
        estaOlhando = false;
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoPrecisaChave) textoPrecisaChave.SetActive(false); // Mata o erro se virar as costas
    }

    public void Interagir()
    {
        if (TaBloqueado()) return; // Foda-se o clique se tiver bloqueado
        if (jaAbriu) return;

        // Se o cara apertar E através da parede, barra a ação na hora
        if (!ChecarVisaoLimpa()) return;

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
                StopAllCoroutines();
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
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoPrecisaChave)
        {
            textoPrecisaChave.SetActive(true);
            yield return new WaitForSeconds(2f);
            textoPrecisaChave.SetActive(false);
        }
        
        // A MÁGICA: Só liga o texto de interagir de volta se a porra do jogador AINDA estiver olhando
        if (!jaAbriu && estaOlhando && textoInteragir) textoInteragir.SetActive(true);
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
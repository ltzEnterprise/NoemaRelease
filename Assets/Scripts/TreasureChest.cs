using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class TreasureChest : MonoBehaviour
{
    [Header("Save System")]
    public string uniqueID;

    [Header("Configurações Básicas")]
    public Transform tampaDoBau;
    public string nomeDaRunaNesteBau;

    [Header("Recompensa: Item de Inventário")]
    public bool darItemInventario = false;
    public int idDoItemInventario = 0;

    [Header("O Seu Painel Novo")]
    public GameObject painelCustomizadoDaRecompensa;

    [Header("--- SISTEMA DE CHAVE ---")]
    public bool requerChave = false;
    public string idChaveNecessaria;

    [Header("--- TRAVA POR TELEPORTE ---")]
    public bool bloquearAteUsarTeleport = false;

    [Tooltip("Só precisa preencher se bloquearAteUsarTeleport estiver ligado.")]
    public TeleportArea teleportNecessario;

    public GameObject textoPrecisaTeleport;

    [Header("--- DEMO MODE ---")]
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
    private bool estaOlhando = false;
    private bool inicializado = false;
    private bool inicializando = false;

    private Coroutine rotinaAvisoChave;
    private Coroutine rotinaAvisoTeleport;
    private Coroutine rotinaRecompensa;
    private Coroutine rotinaTampa;

    private void Start()
    {
        DesligarUIInicial();
        StartCoroutine(InicializarSeguro());
    }

    private void DesligarUIInicial()
    {
        if (textoInteragir) textoInteragir.SetActive(false);
        if (textoPrecisaChave) textoPrecisaChave.SetActive(false);
        if (textoPrecisaTeleport) textoPrecisaTeleport.SetActive(false);
        if (painelFimDemo) painelFimDemo.SetActive(false);
        if (painelPretoRecompensa) painelPretoRecompensa.SetActive(false);
        if (painelCustomizadoDaRecompensa) painelCustomizadoDaRecompensa.SetActive(false);
    }

    private IEnumerator InicializarSeguro()
    {
        if (inicializando)
            yield break;

        inicializando = true;

        float timeout = 5f;

        while (PersistenciaManager.Instance == null && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (PersistenciaManager.Instance != null)
        {
            timeout = 5f;

            while ((!PersistenciaManager.Instance.DadosProntosParaUso || PersistenciaManager.Instance.EstaCarregando) && timeout > 0f)
            {
                timeout -= Time.unscaledDeltaTime;
                yield return null;
            }

            CarregarEstadoDoBau();
        }
        else
        {
            Debug.LogWarning("[TreasureChest] PersistenciaManager não apareceu. Baú vai funcionar sem carregar save inicial: " + gameObject.name);
        }

        inicializado = true;
        inicializando = false;

        Debug.Log("[TreasureChest] Inicializado: " + gameObject.name +
                  " | uniqueID=" + uniqueID +
                  " | jaAbriu=" + jaAbriu +
                  " | requerChave=" + requerChave +
                  " | bloquearAteUsarTeleport=" + bloquearAteUsarTeleport);
    }

    private void CarregarEstadoDoBau()
    {
        if (PersistenciaManager.Instance == null)
            return;

        if (string.IsNullOrEmpty(uniqueID))
        {
            Debug.LogWarning("[TreasureChest] uniqueID vazio. O baú funciona, mas não salva aberto: " + gameObject.name);
            return;
        }

        jaAbriu = PersistenciaManager.Instance.ObterEstado(uniqueID, false);

        if (jaAbriu)
        {
            if (SaveDoBauEstaInconsistente())
            {
                Debug.LogWarning("[TreasureChest] Save inconsistente: baú estava salvo como aberto, mas a recompensa principal não está no inventário. Liberando baú novamente. Baú=" + gameObject.name);

                jaAbriu = false;
                PersistenciaManager.Instance.RegistrarEstado(uniqueID, false);
                PersistenciaManager.Instance.SalvarTudo(true);
                return;
            }

            AplicarVisualAberto();
            ForcarEsconderMensagens();
        }
    }

    private bool SaveDoBauEstaInconsistente()
    {
        if (string.IsNullOrEmpty(nomeDaRunaNesteBau))
            return false;

        if (InventarioRunas.Instance == null)
            return false;

        return !InventarioRunas.Instance.TemRuna(nomeDaRunaNesteBau);
    }

    private void AplicarVisualAberto()
    {
        if (tampaDoBau)
            tampaDoBau.localRotation = Quaternion.Euler(-90, 0, 0);

        if (painelPretoRecompensa) painelPretoRecompensa.SetActive(false);
        if (painelCustomizadoDaRecompensa) painelCustomizadoDaRecompensa.SetActive(false);
    }

    public void AoOlhar()
    {
        if (jaAbriu) return;

        estaOlhando = true;

        if (textoInteragir)
            textoInteragir.SetActive(true);
    }

    public void AoSair()
    {
        estaOlhando = false;

        if (textoInteragir)
            textoInteragir.SetActive(false);
    }

    public void Interagir()
    {
        Debug.Log("[TreasureChest] Interagir chamado: " + gameObject.name +
                  " | inicializado=" + inicializado +
                  " | inicializando=" + inicializando +
                  " | jaAbriu=" + jaAbriu +
                  " | requerChave=" + requerChave +
                  " | bloquearTP=" + bloquearAteUsarTeleport +
                  " | tp=" + (teleportNecessario != null ? teleportNecessario.name : "NULL"));

        if (!inicializado && !inicializando)
            StartCoroutine(InicializarSeguro());

        if (jaAbriu)
        {
            Debug.Log("[TreasureChest] Bloqueou porque já abriu: " + gameObject.name);
            return;
        }

        ForcarEsconderMensagens();

        if (!PodePassarPelaTravaDoTeleport())
        {
            TocarSomTrancado();

            if (rotinaAvisoTeleport != null)
                StopCoroutine(rotinaAvisoTeleport);

            rotinaAvisoTeleport = StartCoroutine(AvisoTeleportFaltando());
            return;
        }

        if (!PodePassarPelaTravaDaChave())
        {
            TocarSomTrancado();

            if (rotinaAvisoChave != null)
                StopCoroutine(rotinaAvisoChave);

            rotinaAvisoChave = StartCoroutine(AvisoChaveFaltando());
            return;
        }

        AbrirBau();
    }

    private bool PodePassarPelaTravaDoTeleport()
    {
        if (!bloquearAteUsarTeleport)
            return true;

        if (teleportNecessario == null)
        {
            Debug.LogError("[TreasureChest] bloquearAteUsarTeleport ligado, mas teleportNecessario vazio. Baú=" + gameObject.name);
            return false;
        }

        bool usado = teleportNecessario.FoiUsado();

        Debug.Log("[TreasureChest] Checando TP: baú=" + gameObject.name +
                  " | TP=" + teleportNecessario.name +
                  " | usado=" + usado);

        return usado;
    }

    private bool PodePassarPelaTravaDaChave()
    {
        if (!requerChave)
            return true;

        if (string.IsNullOrEmpty(idChaveNecessaria))
        {
            Debug.LogError("[TreasureChest] requerChave está ligado, mas idChaveNecessaria está vazio. Baú=" + gameObject.name);
            return false;
        }

        bool temChave = KeySystem.TemChave(idChaveNecessaria);

        Debug.Log("[TreasureChest] Checando chave: baú=" + gameObject.name +
                  " | chave=" + idChaveNecessaria +
                  " | temChave=" + temChave);

        return temChave;
    }

    private void TocarSomTrancado()
    {
        if (audioSource && somTrancado)
            audioSource.PlayOneShot(somTrancado);
    }

    public void ForcarEsconderMensagens()
    {
        if (rotinaAvisoChave != null)
        {
            StopCoroutine(rotinaAvisoChave);
            rotinaAvisoChave = null;
        }

        if (rotinaAvisoTeleport != null)
        {
            StopCoroutine(rotinaAvisoTeleport);
            rotinaAvisoTeleport = null;
        }

        if (textoPrecisaChave)
            textoPrecisaChave.SetActive(false);

        if (textoPrecisaTeleport)
            textoPrecisaTeleport.SetActive(false);

        if (textoInteragir)
            textoInteragir.SetActive(false);
    }

    private void AbrirBau()
    {
        if (jaAbriu)
            return;

        jaAbriu = true;

        ForcarEsconderMensagens();

        if (audioSource && somAbrir)
            audioSource.PlayOneShot(somAbrir);

        if (tampaDoBau)
        {
            if (rotinaTampa != null)
                StopCoroutine(rotinaTampa);

            rotinaTampa = StartCoroutine(AnimarTampa());
        }

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(uniqueID))
            PersistenciaManager.Instance.RegistrarEstado(uniqueID, true);

        if (requerChave)
        {
            KeySystem.GastarChave(idChaveNecessaria);

            if (iconeChaveParaEsconder)
                iconeChaveParaEsconder.SetActive(false);
        }

        if (InventarioRunas.Instance != null && !string.IsNullOrEmpty(nomeDaRunaNesteBau))
            InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRunaNesteBau);

        if (darItemInventario && InventoryManager.Instance != null)
            InventoryManager.Instance.ReceberItem(idDoItemInventario);

        if (PersistenciaManager.Instance != null)
            PersistenciaManager.Instance.SalvarTudo(true);

        if (rotinaRecompensa != null)
            StopCoroutine(rotinaRecompensa);

        rotinaRecompensa = StartCoroutine(SequenciaRecompensa());

        Debug.Log("[TreasureChest] BAÚ ABERTO: " + gameObject.name);
    }

    private IEnumerator AnimarTampa()
    {
        if (tampaDoBau == null)
            yield break;

        float t = 0f;
        Quaternion startRot = tampaDoBau.localRotation;
        Quaternion endRot = Quaternion.Euler(-90, 0, 0);

        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            tampaDoBau.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        tampaDoBau.localRotation = endRot;
        rotinaTampa = null;
    }

    private IEnumerator AvisoChaveFaltando()
    {
        if (textoInteragir)
            textoInteragir.SetActive(false);

        if (textoPrecisaChave)
        {
            textoPrecisaChave.SetActive(true);
            yield return new WaitForSeconds(2f);
            textoPrecisaChave.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        rotinaAvisoChave = null;

        if (!jaAbriu && estaOlhando && textoInteragir)
            textoInteragir.SetActive(true);
    }

    private IEnumerator AvisoTeleportFaltando()
    {
        if (textoInteragir)
            textoInteragir.SetActive(false);

        if (textoPrecisaTeleport)
        {
            textoPrecisaTeleport.SetActive(true);
            yield return new WaitForSeconds(2f);
            textoPrecisaTeleport.SetActive(false);
        }
        else if (textoPrecisaChave)
        {
            textoPrecisaChave.SetActive(true);
            yield return new WaitForSeconds(2f);
            textoPrecisaChave.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(2f);
        }

        rotinaAvisoTeleport = null;

        if (!jaAbriu && estaOlhando && textoInteragir)
            textoInteragir.SetActive(true);
    }

    private IEnumerator SequenciaRecompensa()
    {
        if (painelPretoRecompensa)
            painelPretoRecompensa.SetActive(true);

        if (textoRecompensa && !string.IsNullOrEmpty(nomeDaRunaNesteBau))
            textoRecompensa.text = "Você pegou a " + nomeDaRunaNesteBau + "!";

        if (painelCustomizadoDaRecompensa)
            painelCustomizadoDaRecompensa.SetActive(true);

        yield return new WaitForSecondsRealtime(3f);

        if (painelPretoRecompensa)
            painelPretoRecompensa.SetActive(false);

        if (painelCustomizadoDaRecompensa)
            painelCustomizadoDaRecompensa.SetActive(false);

        if (finalizaDemo)
        {
            if (FPS_Master.Instance != null)
                FPS_Master.Instance.AlterarEstadoJogador(true, false);

            if (painelFimDemo)
                painelFimDemo.SetActive(true);

            yield return new WaitForSecondsRealtime(tempoParaVoltarMenu);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            SceneManager.LoadScene(nomeCenaMenu);
        }

        rotinaRecompensa = null;
    }

    [ContextMenu("DEBUG - Abrir Baú Agora")]
    private void DebugAbrirBauAgora()
    {
        AbrirBau();
    }
}
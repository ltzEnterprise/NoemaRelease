using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FuseBoxController : MonoBehaviour
{
    [Header("--- CONEXÃO DIRETA COM O PC ---")]
    [Tooltip("Arraste o seu PC Principal aqui para a energia ligar ele direto")]
    public ComputerController pcPrincipal; 

    [Header("--- CONEXÕES ---")]
    public Camera cameraDoJogador; 

    [Header("--- PROGRESSÃO (PÉ DE CABRA) ---")]
    public bool estaDestrancada = false; 
    public int idPeDeCabra = 5; 
    public GameObject textoTrancadoUI; 
    public CanvasGroup faderTelaPreta; 
    public AudioClip somPeDeCabra; 
    private string idSaveLock = "FuseBox_Unlocked_Status";

    [Header("--- POSIÇÃO DOS BOTÕES (X) ---")]
    public float posicaoGlobalX_OFF = -0.1038869f;
    public float posicaoGlobalX_ON = 0.0364544f;
    public AudioClip somClick; 

    [Header("--- OBJETIVOS ---")]
    public float voltagemAlvoDia = 245f; 
    public float voltagemAlvoNoite = 280f; 
    public float margemDeErro = 1.0f; 

    [Header("--- RECOMPENSA EXTRA (NOITE) ---")]
    public GameObject quadExtraNoite;

    [Header("--- SISTEMA DE MEMÓRIA ---")]
    public float tempoDeTolerancia = 0.2f; 
    private GameObject ultimoObjetoValido;
    private float timerMemoria = 0f;

    [Header("--- AGULHA ---")]
    public Transform ponteiroVoltagem;
    public Vector3 anguloEulerMin = new Vector3(0, 0, -90); 
    public Vector3 anguloEulerMax = new Vector3(0, 0, 90);
    public float voltagemMaximaGauge = 300f;
    public float intensidadeTremor = 1.0f;

    [Header("--- REFERÊNCIAS ---")]
    public List<FuseSwitch> interruptores; 
    public Transform alavancaMestre;       
    public Transform portaDaCaixa;         
    [Tooltip("Arraste o Collider da porta aqui. Ele ficará 'Trigger' ao abrir/fechar para não empurrar o player.")]
    public Collider colisorDaPorta; 

    [Header("--- VISUAL E AUDIO ---")]
    public Renderer rendererLuzVerde;
    public Renderer rendererLuzVermelha;
    [ColorUsage(true, true)] public Color corVerdeAcesa = Color.green * 2;
    [ColorUsage(true, true)] public Color corVermelhaAcesa = Color.red * 2;
    private Color corDesligada = Color.black;
    
    public AudioSource audioSource;
    public AudioClip somPorta, somErro, somSucesso; 

    // Estados
    private bool interagindo = false;
    private bool resolvidoDia = false;
    private bool resolvidoNoite = false;
    private bool alavancaSubindo = false; 
    private bool animandoErro = false; 
    private bool emCutscene = false; 

    // Agulha e Porta
    private Quaternion rotMin;
    private Quaternion rotMax;
    private float voltagemAtual = 0f;
    private float voltagemSmooth = 0f;
    
    public Vector3 rotacaoPortaFechada; 
    public Vector3 rotacaoPortaAberta; 
    public Vector3 eixoAlavanca = new Vector3(1, 0, 0); 
    public float anguloBaixo = 0f;
    public float anguloCima = -65f;

    private float tempoUltimoClique = 0f;
    private float cooldownClique = 0.1f; 
    private float cooldownSairEntrar = 0f; 

    void Start()
    {
        rotMin = Quaternion.Euler(anguloEulerMin);
        rotMax = Quaternion.Euler(anguloEulerMax);

        // 🔥 TUDO AGORA É LIDO DO PERSISTENCIA MANAGER
        if (PersistenciaManager.Instance != null)
        {
            if (PersistenciaManager.Instance.ObterEstado(idSaveLock)) estaDestrancada = true;
            if (PersistenciaManager.Instance.ObterEstado("FuseBox_Dia_Resolvida")) resolvidoDia = true;
            if (PersistenciaManager.Instance.ObterEstado("FuseBox_Noite_Resolvida")) resolvidoNoite = true;
        }

        if (portaDaCaixa) portaDaCaixa.localEulerAngles = rotacaoPortaFechada;
        if (alavancaMestre) alavancaMestre.localRotation = Quaternion.AngleAxis(anguloBaixo, eixoAlavanca);
        
        DesligarLuz(rendererLuzVerde);
        DesligarLuz(rendererLuzVermelha);
        
        if (quadExtraNoite) quadExtraNoite.SetActive(false);
        if (textoTrancadoUI) textoTrancadoUI.SetActive(false);
        if (faderTelaPreta) faderTelaPreta.gameObject.SetActive(false);
        
        foreach(var sw in interruptores) sw.ForcarDesligamento();
        RecalcularVoltagemInstantanea();
    }

    void Update()
    {
        AtualizarAgulha();
        AnimarPorta();
        if (!animandoErro) AnimarAlavancaNormal();

        if (emCutscene) return; 

        if (interagindo)
        {
            if (Input.GetKeyDown(KeyCode.E) && Time.time > cooldownSairEntrar) Sair();

            if (!animandoErro)
            {
                AtualizarMiraComMemoria();

                if (Input.GetMouseButtonDown(0))
                {
                    if (Time.time < tempoUltimoClique + cooldownClique) return;
                    ExecutarInteracao(); 
                }
            }
        }
    }

    public void Interagir()
    {
        if (emCutscene || Time.time < cooldownSairEntrar) return;

        if (interagindo)
        {
            Sair();
            return;
        }

        if (estaDestrancada)
        {
            if (PodeClicar()) Entrar(); 
            else TocarSomTravado();
            return;
        }

        if (InventoryManager.Instance != null && InventoryManager.Instance.itemSelecionado == idPeDeCabra)
            StartCoroutine(CutsceneAbrirComPeDeCabra());
        else
            StartCoroutine(MostrarTextoTrancado());
    }

    IEnumerator MostrarTextoTrancado()
    {
        if (textoTrancadoUI) textoTrancadoUI.SetActive(true);
        if (audioSource && somPorta) audioSource.PlayOneShot(somPorta);
        yield return new WaitForSeconds(2.5f);
        if (textoTrancadoUI) textoTrancadoUI.SetActive(false);
    }

    IEnumerator CutsceneAbrirComPeDeCabra()
    {
        emCutscene = true;
        if (FPS_Master.Instance) FPS_Master.travadoInteracao = true;

        if (faderTelaPreta) { faderTelaPreta.alpha = 1f; faderTelaPreta.gameObject.SetActive(true); }
        if (audioSource && somPeDeCabra) audioSource.PlayOneShot(somPeDeCabra);
        
        yield return new WaitForSeconds(1.5f); 

        estaDestrancada = true;
        if (!Application.isEditor && PersistenciaManager.Instance != null)
        {
            PersistenciaManager.Instance.RegistrarEstado(idSaveLock, true);
            PersistenciaManager.Instance.SalvarTudo();
        }

        if (faderTelaPreta) { faderTelaPreta.alpha = 0f; faderTelaPreta.gameObject.SetActive(false); }

        emCutscene = false;
        Entrar();
    }

    void AtualizarMiraComMemoria()
    {
        if (cameraDoJogador == null) 
        {
            if (Camera.main != null) cameraDoJogador = Camera.main;
            else return; 
        }

        Ray raio = cameraDoJogador.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(raio, 100f);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        bool encontrouAlgoNesseFrame = false;

        foreach (RaycastHit hit in hits)
        {
            FuseSwitch botao = hit.transform.GetComponent<FuseSwitch>();
            if (botao == null) botao = hit.transform.GetComponentInParent<FuseSwitch>();
            bool ehAlavanca = (hit.transform == alavancaMestre || hit.transform.IsChildOf(alavancaMestre));

            if (botao != null)
            {
                ultimoObjetoValido = botao.gameObject;
                timerMemoria = tempoDeTolerancia;
                encontrouAlgoNesseFrame = true;
                break;
            }
            else if (ehAlavanca)
            {
                ultimoObjetoValido = alavancaMestre.gameObject;
                timerMemoria = tempoDeTolerancia;
                encontrouAlgoNesseFrame = true;
                break;
            }
        }

        if (!encontrouAlgoNesseFrame)
        {
            if (timerMemoria > 0) timerMemoria -= Time.deltaTime;
            else ultimoObjetoValido = null;
        }
    }

    void ExecutarInteracao()
    {
        if (ultimoObjetoValido == null) return;
        if (resolvidoDia && !DayNightCycle.Instance.isNight) return;
        if (resolvidoNoite) return;

        tempoUltimoClique = Time.time;

        FuseSwitch botao = ultimoObjetoValido.GetComponent<FuseSwitch>();
        if (botao == null) botao = ultimoObjetoValido.GetComponentInParent<FuseSwitch>();

        if (botao != null) { botao.Alternar(); return; }

        if (ultimoObjetoValido == alavancaMestre.gameObject || ultimoObjetoValido.transform.IsChildOf(alavancaMestre))
        {
            PuxarAlavanca();
        }
    }

    public void RecalcularVoltagemInstantanea()
    {
        voltagemAtual = 0;
        foreach (var sw in interruptores) if (sw.estaLigado) voltagemAtual += sw.volts;

        if (voltagemAtual >= voltagemMaximaGauge && !animandoErro)
        {
            StartCoroutine(RotinaSobrecargaAutomatica());
        }
    }

    IEnumerator RotinaSobrecargaAutomatica()
    {
        animandoErro = true;
        if (audioSource && somErro) audioSource.PlayOneShot(somErro);
        LigarLuz(rendererLuzVermelha, corVermelhaAcesa);
        yield return new WaitForSeconds(0.5f);
        foreach (var sw in interruptores) sw.ForcarDesligamento();
        voltagemAtual = 0; 
        yield return new WaitForSeconds(0.3f);
        DesligarLuz(rendererLuzVermelha);
        animandoErro = false; 
    }

    void PuxarAlavanca()
    {
        if (animandoErro) return; 

        float alvoAtual = (!resolvidoDia) ? voltagemAlvoDia : ((DayNightCycle.Instance.isNight && !resolvidoNoite) ? voltagemAlvoNoite : 0f);
        if (alvoAtual == 0f) return;

        if (Mathf.Abs(voltagemAtual - alvoAtual) <= margemDeErro)
        {
            alavancaSubindo = true; 
            if (audioSource && somSucesso) audioSource.PlayOneShot(somSucesso);
            LigarLuz(rendererLuzVerde, corVerdeAcesa);

            if (!resolvidoDia)
            {
                resolvidoDia = true;
                if (PersistenciaManager.Instance != null)
                {
                    PersistenciaManager.Instance.RegistrarEstado("FuseBox_Dia_Resolvida", true);
                    PersistenciaManager.Instance.SalvarTudo();
                }
                
                if (pcPrincipal != null) pcPrincipal.LigarPCProMundo2D(); 
            }
            else
            {
                resolvidoNoite = true;
                if (quadExtraNoite) quadExtraNoite.SetActive(true); 
                
                if (PersistenciaManager.Instance != null)
                {
                    PersistenciaManager.Instance.RegistrarEstado("FuseBox_Noite_Resolvida", true);
                    PersistenciaManager.Instance.SalvarTudo();
                }

                if (pcPrincipal != null) pcPrincipal.LigarPcSetaNoite();
            }

            Invoke("Sair", 1.5f);
        }
        else
        {
            StartCoroutine(RotinaErroAlavanca());
        }
    }

    IEnumerator RotinaErroAlavanca()
    {
        animandoErro = true;
        if (audioSource && somErro) audioSource.PlayOneShot(somErro);
        LigarLuz(rendererLuzVermelha, corVermelhaAcesa);

        float voltagemSalva = voltagemAtual;
        voltagemAtual = voltagemMaximaGauge * 1.2f; 

        float t = 0f;
        Quaternion rotBaixo = Quaternion.AngleAxis(anguloBaixo, eixoAlavanca);
        Quaternion rotFalha = Quaternion.AngleAxis(anguloCima * 0.4f, eixoAlavanca); 

        while(t < 1f) { t += Time.deltaTime * 5f; alavancaMestre.localRotation = Quaternion.Lerp(rotBaixo, rotFalha, t); yield return null; }
        
        foreach (var sw in interruptores) sw.ForcarDesligamento();
        voltagemAtual = 0; 

        t = 0f;
        while(t < 1f) { t += Time.deltaTime * 5f; alavancaMestre.localRotation = Quaternion.Lerp(rotFalha, rotBaixo, t); yield return null; }

        yield return new WaitForSeconds(0.2f);
        DesligarLuz(rendererLuzVermelha);
        animandoErro = false; 
    }

    void AtualizarAgulha()
    {
        if (!ponteiroVoltagem) return;
        voltagemSmooth = Mathf.Lerp(voltagemSmooth, voltagemAtual, Time.deltaTime * 3f);
        float t = Mathf.Clamp01(voltagemSmooth / voltagemMaximaGauge);
        Quaternion rotacaoBase = Quaternion.Slerp(rotMin, rotMax, t);

        if (voltagemSmooth > 5f && intensidadeTremor > 0)
        {
            float tremor = Random.Range(-intensidadeTremor, intensidadeTremor);
            ponteiroVoltagem.localRotation = rotacaoBase * Quaternion.Euler(0, 0, tremor);
        }
        else ponteiroVoltagem.localRotation = rotacaoBase;
    }

    void LigarLuz(Renderer r, Color c) { if(r) { r.material.EnableKeyword("_EMISSION"); r.material.SetColor("_EmissionColor", c); } }
    void DesligarLuz(Renderer r) { if(r) { r.material.SetColor("_EmissionColor", corDesligada); } }

    void AnimarPorta()
    {
        if (!portaDaCaixa) return;

        Vector3 anguloAlvo = interagindo ? rotacaoPortaAberta : rotacaoPortaFechada;
        Quaternion rotAlvo = Quaternion.Euler(anguloAlvo);

        portaDaCaixa.localRotation = Quaternion.Lerp(portaDaCaixa.localRotation, rotAlvo, Time.deltaTime * 5f);

        if (colisorDaPorta != null)
        {
            if (Quaternion.Angle(portaDaCaixa.localRotation, rotAlvo) > 2.0f)
            {
                colisorDaPorta.isTrigger = true; 
            }
            else
            {
                portaDaCaixa.localRotation = rotAlvo; 
                colisorDaPorta.isTrigger = false; 
            }
        }
    }

    void AnimarAlavancaNormal()
    {
        if (!alavancaMestre) return;
        bool deveEstarEmCima = alavancaSubindo;
        if (resolvidoDia && DayNightCycle.Instance != null && DayNightCycle.Instance.isNight && !resolvidoNoite) deveEstarEmCima = false;
        
        float anguloAlvo = deveEstarEmCima ? anguloCima : anguloBaixo;
        Quaternion rotAlvo = Quaternion.AngleAxis(anguloAlvo, eixoAlavanca);
        alavancaMestre.localRotation = Quaternion.Lerp(alavancaMestre.localRotation, rotAlvo, Time.deltaTime * 5f);
    }

    public bool PodeClicar() 
    { 
        if (resolvidoNoite) return false;
        if (resolvidoDia && DayNightCycle.Instance != null && !DayNightCycle.Instance.isNight) return false;
        return !interagindo; 
    }
    
    public void AoOlhar() { } 
    public void AoSair() { }

    void TocarSomTravado() { if (audioSource && somPorta) audioSource.PlayOneShot(somPorta); }

    void Entrar()
    {
        cooldownSairEntrar = Time.time + 0.2f; 
        interagindo = true;
        if (audioSource && somPorta) audioSource.PlayOneShot(somPorta);
        
        if (resolvidoDia && DayNightCycle.Instance != null && DayNightCycle.Instance.isNight && !resolvidoNoite)
        {
            alavancaSubindo = false; 
            DesligarLuz(rendererLuzVerde); 
        }

        if (FPS_Master.Instance) FPS_Master.travadoInteracao = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Sair()
    {
        cooldownSairEntrar = Time.time + 0.2f; 
        interagindo = false;
        if (FPS_Master.Instance) FPS_Master.travadoInteracao = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
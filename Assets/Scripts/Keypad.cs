using UnityEngine;
using TMPro; 
using System.Collections;

public class Keypad : MonoBehaviour
{
    [Header("--- BLOQUEADOR ---")]
    public GameObject bloqueador;

    [Header("Configurações")]
    public string senhaCorreta = "1487";
    public int limiteDigitos = 4;

    [Header("--- SENHA EXTRA (Opcional) ---")]
    public string senhaExtra = ""; 
    public GameObject quadPainelExtra; 

    [Header("Conexões")]
    public GameObject portaParaAbrir; 
    public Transform pontoDeRetorno; 
    public Camera cameraFixaKeypad;

    [Header("Recompensa: Runa")]
    public bool darRunaAoAcertar = true;
    public string nomeDaRuna = "Runa_Investigacao";
    public GameObject painelAvisoRuna;

    [Header("Recompensa: Manivela da Vitrola")]
    public bool darManivelaAoAcertar = false;
    public int idDaManivela = 3; 
    public GameObject manivelaFlutuante; 

    [Header("Feedback")]
    public GameObject textoInteragirProprio; 
    public TextMeshPro displayTexto; 
    public AudioSource audioSource;
    public AudioClip somBip, somErro, somSucesso;

    private string inputAtual = "";
    private bool jogadorUsando = false; 
    private bool jaResolveuPrincipal = false; 
    
    private float tempoUltimoClique = 0f;
    private float cooldownClique = 0.2f; 

    private Coroutine rotinaReset;
    private Coroutine rotinaPainelRuna;
    private bool aguardandoLimpeza = false;
    private bool estaOlhando = false; 
    private bool inicializado = false;
    private bool aguardandoSoltarE = false;

    private bool TaBloqueado()
    {
        return bloqueador != null && bloqueador.activeInHierarchy;
    }

    void Start() 
    { 
        if (cameraFixaKeypad) 
        {
            cameraFixaKeypad.gameObject.SetActive(false);
            var listener = cameraFixaKeypad.GetComponent<AudioListener>();
            if (listener) listener.enabled = false;
        }

        if (textoInteragirProprio) textoInteragirProprio.SetActive(false);
        if (painelAvisoRuna) painelAvisoRuna.SetActive(false);
        if (quadPainelExtra) quadPainelExtra.SetActive(false);

        StartCoroutine(InicializarSeguro());
    }

    IEnumerator InicializarSeguro()
    {
        if (PersistenciaManager.Instance != null)
            yield return new WaitUntil(() => PersistenciaManager.Instance.DadosProntosParaUso);

        AtualizarDisplay();
        inicializado = true;
    }

    public void AoOlhar() 
    { 
        if (!inicializado) return;
        estaOlhando = true;
        if (TaBloqueado()) return; 

        if (!jogadorUsando && textoInteragirProprio) 
            textoInteragirProprio.SetActive(true); 
    }

    public void AoSair() 
    { 
        estaOlhando = false;
        if (textoInteragirProprio) 
            textoInteragirProprio.SetActive(false); 
    }

    public void Interagir()
    {
        if (!inicializado) return;
        if (TaBloqueado()) return; 
        if (!jogadorUsando) EntrarModoKeypad();
    }

    void Update()
    {
        if (TaBloqueado() && textoInteragirProprio && textoInteragirProprio.activeSelf)
        {
            textoInteragirProprio.SetActive(false);
        }

        if (jogadorUsando && TaBloqueado())
        {
            SairModoKeypad(); 
            return;
        }

        if (!jogadorUsando) return;

        if (aguardandoSoltarE)
        {
            if (!Input.GetKey(KeyCode.E))
                aguardandoSoltarE = false;

            return;
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
        {
            SairModoKeypad();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (Time.time < tempoUltimoClique + cooldownClique) return;
            ProcessarCliqueMouse();
        }
    }

    void ProcessarCliqueMouse()
    {
        if (cameraFixaKeypad == null) return;

        Ray raio = cameraFixaKeypad.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(raio, 100f);
        System.Array.Sort(hits, (x, y) => x.distance.CompareTo(y.distance));

        foreach (RaycastHit hit in hits)
        {
            KeypadButton botao = hit.transform.GetComponent<KeypadButton>();
            if (botao != null)
            {
                tempoUltimoClique = Time.time; 
                botao.Pressionar();
                AddInput(botao.value);
                return; 
            }
        }
    }

    void EntrarModoKeypad()
    {
        jogadorUsando = true;
        aguardandoSoltarE = true;

        if (textoInteragirProprio) textoInteragirProprio.SetActive(false);

        if (pontoDeRetorno != null && FPS_Master.Instance != null)
        {
            FPS_Master.Instance.Teleportar(pontoDeRetorno.position);
            Physics.SyncTransforms();
        }

        if (FPS_Master.Instance != null)
        {
            FPS_Master.Instance.FicarInvisivelMasFisico(true);

            if (FPS_Master.Instance.cameraJogador) 
            {
                FPS_Master.Instance.cameraJogador.enabled = false;
                var listener = FPS_Master.Instance.cameraJogador.GetComponent<AudioListener>();
                if (listener) listener.enabled = false;
            }

            FPS_Master.Instance.AlterarEstadoJogador(true, true);
        }

        if (cameraFixaKeypad) 
        {
            cameraFixaKeypad.gameObject.SetActive(true);
            var listener = cameraFixaKeypad.GetComponent<AudioListener>();
            if (listener) listener.enabled = true;
        }
    }

    void SairModoKeypad()
    {
        jogadorUsando = false;
        aguardandoSoltarE = false;
        
        if (cameraFixaKeypad) 
        {
            cameraFixaKeypad.gameObject.SetActive(false);
            var listener = cameraFixaKeypad.GetComponent<AudioListener>();
            if (listener) listener.enabled = false;
        }

        if (FPS_Master.Instance != null)
        {
            FPS_Master.Instance.FicarInvisivelMasFisico(false);
            
            if (FPS_Master.Instance.cameraJogador) 
            {
                FPS_Master.Instance.cameraJogador.enabled = true;
                var listener = FPS_Master.Instance.cameraJogador.GetComponent<AudioListener>();
                if (listener) listener.enabled = true;
            }

            FPS_Master.Instance.AlterarEstadoJogador(false, false);
        }

        if (estaOlhando && textoInteragirProprio && !TaBloqueado()) 
            textoInteragirProprio.SetActive(true);
    }

    public void AddInput(string numero)
    {
        if (string.IsNullOrEmpty(numero)) return;

        if (numero.ToLower() == "enter")
        {
            VerificarSenha();
            return;
        }

        if (numero.ToLower() == "clear") 
        { 
            inputAtual = ""; 
            aguardandoLimpeza = false;

            if (rotinaReset != null)
                StopCoroutine(rotinaReset);

            AtualizarDisplay(); 
            return; 
        }

        if (aguardandoLimpeza)
        {
            aguardandoLimpeza = false;
            inputAtual = "";

            if (rotinaReset != null)
                StopCoroutine(rotinaReset);
        }

        if (inputAtual.Length < limiteDigitos)
        {
            inputAtual += numero;

            if (audioSource && somBip)
                audioSource.PlayOneShot(somBip);

            AtualizarDisplay();
        }
    }

    void VerificarSenha() 
    { 
        if (string.IsNullOrEmpty(inputAtual)) return; 

        if (inputAtual == senhaCorreta) 
        {
            Sucesso(); 
        }
        else if (!string.IsNullOrEmpty(senhaExtra) && inputAtual == senhaExtra)
        {
            SucessoExtra();
        }
        else 
        {
            Erro(); 
        }
    }

    void Sucesso()
    {
        if (audioSource && somSucesso)
            audioSource.PlayOneShot(somSucesso);

        if (displayTexto)
        {
            displayTexto.text = "OK";
            displayTexto.color = Color.green;
        }

        if (!jaResolveuPrincipal)
        {
            jaResolveuPrincipal = true;

            if (portaParaAbrir != null)
            {
                portaParaAbrir.SendMessage("AbrirPeloKeypad", SendMessageOptions.DontRequireReceiver);
                portaParaAbrir.SendMessage("Interagir", SendMessageOptions.DontRequireReceiver);
            }

            if (darRunaAoAcertar)
            {
                EntregarRunaComSeguranca();

                if (painelAvisoRuna)
                {
                    if (rotinaPainelRuna != null)
                        StopCoroutine(rotinaPainelRuna);

                    rotinaPainelRuna = StartCoroutine(MostrarAvisoRuna());
                }
            }

            if (darManivelaAoAcertar)
            {
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.ReceberItem(idDaManivela); 
                    InventoryManager.Instance.TentarEquipar(idDaManivela); 
                }
                
                if (manivelaFlutuante != null)
                {
                    manivelaFlutuante.SetActive(false); 
                }
            }
        }

        PrepararResetDisplay(); 
        StartCoroutine(DelaySaida());
    }

    void EntregarRunaComSeguranca()
    {
        InventarioRunas inventario = InventarioRunas.Instance;

        if (inventario == null)
        {
            inventario = Object.FindFirstObjectByType<InventarioRunas>();

            if (inventario != null)
                InventarioRunas.Instance = inventario;
        }

        if (inventario != null)
        {
            inventario.ColetarRunaPeloNome(nomeDaRuna);
            inventario.RecarregarDoSave();
            return;
        }

        Debug.LogWarning("[Keypad] InventarioRunas não encontrado. Salvando a runa direto no PersistenciaManager: " + nomeDaRuna);

        if (PersistenciaManager.Instance != null && !string.IsNullOrEmpty(nomeDaRuna))
        {
            PersistenciaManager.Instance.RegistrarEstado("Runa_" + nomeDaRuna, true);
            PersistenciaManager.Instance.SalvarTudo(true);
        }
    }

    void SucessoExtra()
    {
        if (audioSource && somSucesso)
            audioSource.PlayOneShot(somSucesso);

        if (displayTexto)
        {
            displayTexto.text = "EXTRA";
            displayTexto.color = Color.cyan;
        }

        if (quadPainelExtra)
            quadPainelExtra.SetActive(true);

        PrepararResetDisplay();
    }

    void Erro()
    {
        if (audioSource && somErro)
            audioSource.PlayOneShot(somErro);

        if (displayTexto)
        {
            displayTexto.text = "ERRO";
            displayTexto.color = Color.red;
        }
        
        PrepararResetDisplay();
    }

    void PrepararResetDisplay()
    {
        aguardandoLimpeza = true;
        inputAtual = "";
        
        if (rotinaReset != null)
            StopCoroutine(rotinaReset);

        rotinaReset = StartCoroutine(ResetDisplayDelay());
    }

    IEnumerator DelaySaida() 
    { 
        yield return new WaitForSeconds(1.5f); 
        SairModoKeypad(); 
    }
    
    IEnumerator MostrarAvisoRuna() 
    { 
        if (painelAvisoRuna)
            painelAvisoRuna.SetActive(true); 

        yield return new WaitForSeconds(3f); 

        if (painelAvisoRuna)
            painelAvisoRuna.SetActive(false);

        rotinaPainelRuna = null;
    }
    
    void AtualizarDisplay() 
    { 
        if (displayTexto != null)
        {
            displayTexto.text = inputAtual;
            displayTexto.color = Color.white;
        } 
    }
    
    IEnumerator ResetDisplayDelay() 
    { 
        yield return new WaitForSeconds(1.5f); 
        aguardandoLimpeza = false;
        AtualizarDisplay(); 
    }
}
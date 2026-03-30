using UnityEngine;
using TMPro; 
using System.Collections;

public class Keypad : MonoBehaviour
{
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
    public int idDaManivela = 3; // O ID da manivela no seu InventoryManager
    public GameObject manivelaFlutuante; // Arraste a manivela que roda no mapa aqui

    [Header("Feedback")]
    public GameObject textoInteragirProprio; 
    public TextMeshPro displayTexto; 
    public AudioSource audioSource;
    public AudioClip somBip, somErro, somSucesso;

    private string inputAtual = "";
    private bool jogadorUsando = false; 
    private bool jaResolveuPrincipal = false; // Garante que não vai te dar a runa 2 vezes se digitar a senha de novo
    
    private float tempoUltimoClique = 0f;
    private float cooldownClique = 0.2f; 

    private Coroutine rotinaReset;
    private bool aguardandoLimpeza = false;

    void Start() 
    { 
        if(cameraFixaKeypad) 
        {
            cameraFixaKeypad.gameObject.SetActive(false);
            var listener = cameraFixaKeypad.GetComponent<AudioListener>();
            if(listener) listener.enabled = false;
        }

        if(textoInteragirProprio) textoInteragirProprio.SetActive(false);
        if(painelAvisoRuna) painelAvisoRuna.SetActive(false);
        if(quadPainelExtra) quadPainelExtra.SetActive(false);

        AtualizarDisplay(); 
    }

    public void AoOlhar() 
    { 
        // O painel NUNCA MAIS trava, então o texto sempre aparece se não tiver usando
        if (!jogadorUsando && textoInteragirProprio) 
            textoInteragirProprio.SetActive(true); 
    }

    public void AoSair() 
    { 
        if (textoInteragirProprio) 
            textoInteragirProprio.SetActive(false); 
    }

    public void Interagir()
    {
        if (!jogadorUsando) EntrarModoKeypad();
    }

    void Update()
    {
        if (!jogadorUsando) return;

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
        if (textoInteragirProprio) textoInteragirProprio.SetActive(false);

        if (pontoDeRetorno != null)
        {
            FPS_Master.Instance.Teleportar(pontoDeRetorno.position);
        }

        FPS_Master.Instance.FicarInvisivelMasFisico(true);

        if(FPS_Master.Instance.cameraJogador) 
        {
            FPS_Master.Instance.cameraJogador.enabled = false;
            var listener = FPS_Master.Instance.cameraJogador.GetComponent<AudioListener>();
            if(listener) listener.enabled = false;
        }

        if(cameraFixaKeypad) 
        {
            cameraFixaKeypad.gameObject.SetActive(true);
            var listener = cameraFixaKeypad.GetComponent<AudioListener>();
            if(listener) listener.enabled = true;
        }
        
        FPS_Master.Instance.AlterarEstadoJogador(true, true);
    }

    void SairModoKeypad()
    {
        jogadorUsando = false;
        
        if(cameraFixaKeypad) 
        {
            cameraFixaKeypad.gameObject.SetActive(false);
            var listener = cameraFixaKeypad.GetComponent<AudioListener>();
            if(listener) listener.enabled = false;
        }

        if(FPS_Master.Instance.cameraJogador) 
        {
            FPS_Master.Instance.cameraJogador.enabled = true;
            var listener = FPS_Master.Instance.cameraJogador.GetComponent<AudioListener>();
            if(listener) listener.enabled = true;
        }

        FPS_Master.Instance.FicarInvisivelMasFisico(false);
        FPS_Master.Instance.AlterarEstadoJogador(false, false);
    }

    public void AddInput(string numero)
    {
        if (numero.ToLower() == "enter") { VerificarSenha(); return; }
        if (numero.ToLower() == "clear") 
        { 
            inputAtual = ""; 
            aguardandoLimpeza = false;
            if (rotinaReset != null) StopCoroutine(rotinaReset);
            AtualizarDisplay(); 
            return; 
        }

        if (aguardandoLimpeza)
        {
            aguardandoLimpeza = false;
            inputAtual = "";
            if (rotinaReset != null) StopCoroutine(rotinaReset);
        }

        if (inputAtual.Length < limiteDigitos)
        {
            inputAtual += numero;
            if(audioSource && somBip) audioSource.PlayOneShot(somBip);
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
        if(audioSource && somSucesso) audioSource.PlayOneShot(somSucesso);
        if(displayTexto) { displayTexto.text = "OK"; displayTexto.color = Color.green; }

        if (!jaResolveuPrincipal)
        {
            jaResolveuPrincipal = true;

            if (portaParaAbrir != null)
            {
                portaParaAbrir.SendMessage("AbrirPeloKeypad", SendMessageOptions.DontRequireReceiver);
                portaParaAbrir.SendMessage("Interagir", SendMessageOptions.DontRequireReceiver);
            }

            if (darRunaAoAcertar && InventarioRunas.Instance != null)
            {
                InventarioRunas.Instance.ColetarRunaPeloNome(nomeDaRuna);
                if (painelAvisoRuna) StartCoroutine(MostrarAvisoRuna());
            }

            // --- A MÁGICA DA MANIVELA AQUI ---
            if (darManivelaAoAcertar)
            {
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.ReceberItem(idDaManivela); // Dá pro inventário
                    InventoryManager.Instance.TentarEquipar(idDaManivela); // Força ele a segurar a manivela na hora
                }
                
                if (manivelaFlutuante != null)
                {
                    manivelaFlutuante.SetActive(false); // Apaga a manivela flutuante do cenário
                }
            }
        }

        PrepararResetDisplay(); 
        StartCoroutine(DelaySaida());
    }

    void SucessoExtra()
    {
        if(audioSource && somSucesso) audioSource.PlayOneShot(somSucesso);
        if(displayTexto) { displayTexto.text = "EXTRA"; displayTexto.color = Color.cyan; }
        if (quadPainelExtra) quadPainelExtra.SetActive(true);

        PrepararResetDisplay();
    }

    void Erro()
    {
        if(audioSource && somErro) audioSource.PlayOneShot(somErro);
        if(displayTexto) { displayTexto.text = "ERRO"; displayTexto.color = Color.red; }
        
        PrepararResetDisplay();
    }

    void PrepararResetDisplay()
    {
        aguardandoLimpeza = true;
        inputAtual = "";
        
        if (rotinaReset != null) StopCoroutine(rotinaReset);
        rotinaReset = StartCoroutine(ResetDisplayDelay());
    }

    IEnumerator DelaySaida() { yield return new WaitForSeconds(1.5f); SairModoKeypad(); }
    IEnumerator MostrarAvisoRuna() { painelAvisoRuna.SetActive(true); yield return new WaitForSeconds(3f); painelAvisoRuna.SetActive(false); }
    void AtualizarDisplay() { if (displayTexto != null) { displayTexto.text = inputAtual; displayTexto.color = Color.white; } }
    
    IEnumerator ResetDisplayDelay() 
    { 
        yield return new WaitForSeconds(1.5f); 
        aguardandoLimpeza = false;
        AtualizarDisplay(); 
    }
}
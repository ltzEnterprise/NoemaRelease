using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using System.Collections;

public class FinalPC : MonoBehaviour
{
    [Header("1. Configuração do Zoom (Camera Trilho)")]
    public Transform pontoInicial; // NOVO: Crie um objeto vazio onde a câmera COMEÇA
    public Transform pontoDeZoom;  // Onde a câmera TERMINA
    public float tempoZoom = 3.0f; 

    [Header("2. Configuração da Tela Preta")]
    public CanvasGroup painelPreto; 
    public float tempoFade = 1.0f; 
    public float tempoTelaPretaSozinha = 2.0f; 

    [Header("3. Configuração dos Créditos")]
    public GameObject textoCreditos; 
    public float tempoCreditos = 4.0f; 

    [Header("4. Áudio Final")]
    public AudioSource musicaFinal; 

    [Header("5. Outros")]
    public GameObject textoAperteE;
    public string nomeCenaMenu = "MenuPrincipal"; 
    public MonoBehaviour scriptMovimento; // O script que controla o player (FirstPersonController)
    public CharacterController characterController; // NOVO: Arraste o CharacterController aqui se tiver

    private bool playerPerto = false;
    private bool jaAtivou = false;
    private Camera cam;

    void Start()
    {
        cam = Camera.main; 
        
        // Garante estado inicial invisível
        if (painelPreto != null) 
        {
            painelPreto.alpha = 0; 
            painelPreto.gameObject.SetActive(false); 
        }
        
        if (textoCreditos != null) textoCreditos.SetActive(false);
        if (textoAperteE != null) textoAperteE.SetActive(false);
    }

    void Update()
    {
        if (playerPerto && !jaAtivou && Input.GetKeyDown(KeyCode.E))
        {
            if (textoAperteE != null) textoAperteE.SetActive(false);
            IniciarSequencia();
        }
    }

    void IniciarSequencia()
    {
        jaAtivou = true;
        
        // Desativa scripts de controle para o player não brigar com a câmera
        if (scriptMovimento != null) scriptMovimento.enabled = false;
        if (characterController != null) characterController.enabled = false; 

        // TOCA A MÚSICA
        if (musicaFinal != null)
        {
            musicaFinal.Play();
        }
        else
        {
            Debug.LogWarning("AudioSource não foi arrastado para o Inspector!");
        }

        StartCoroutine(SequenciaFinal());
    }

    IEnumerator SequenciaFinal()
    {
        // === PASSO 1: POSICIONAR NO INÍCIO ===
        // Aqui está o segredo: Teleportamos a câmera para o ponto inicial manual
        // Isso impede que ela pegue a posição errada (embaixo do mapa)
        if (pontoInicial != null)
        {
            cam.transform.position = pontoInicial.position;
            cam.transform.rotation = pontoInicial.rotation;
        }

        // === PASSO 2: ZOOM (LERP) ===
        float t = 0;
        
        // Pegamos as posições fixas dos objetos vazios
        Vector3 posStart = pontoInicial.position;
        Quaternion rotStart = pontoInicial.rotation;
        
        Vector3 posEnd = pontoDeZoom.position;
        Quaternion rotEnd = pontoDeZoom.rotation;

        while (t < 1.0f)
        {
            t += Time.deltaTime / tempoZoom; 
            // Move suavemente do Ponto A (Inicial) ao Ponto B (Zoom)
            cam.transform.position = Vector3.Lerp(posStart, posEnd, t);
            cam.transform.rotation = Quaternion.Lerp(rotStart, rotEnd, t);
            yield return null;
        }

        // === PASSO 3: FADE PRETO ===
        if (painelPreto != null)
        {
            painelPreto.gameObject.SetActive(true); 
            t = 0;
            while (t < 1.0f)
            {
                t += Time.deltaTime / tempoFade; 
                painelPreto.alpha = t; 
                yield return null;
            }
            painelPreto.alpha = 1; // Garante que fica totalmente preto
        }
        else
        {
            Debug.LogError("O Painel Preto (Canvas Group) não foi associado no Inspector!");
        }

        // === TELA PRETA (SUSPENSE) ===
        yield return new WaitForSeconds(tempoTelaPretaSozinha);

        // === CRÉDITOS ===
        if (textoCreditos != null) textoCreditos.SetActive(true);

        yield return new WaitForSeconds(tempoCreditos);

        // === MENU ===
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(nomeCenaMenu);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !jaAtivou)
        {
            playerPerto = true;
            if (textoAperteE != null) textoAperteE.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerPerto = false;
            if (textoAperteE != null) textoAperteE.SetActive(false);
        }
    }
}
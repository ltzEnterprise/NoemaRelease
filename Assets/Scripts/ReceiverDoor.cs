using UnityEngine;

public class ReceiverDoor : MonoBehaviour
{
    [Header("--- SAVE NATIVO ---")]
    [Tooltip("Dá um nome único para esta porta. Ex: Porta_Ruina_1")]
    public string doorID = "";

    [Header("--- VISUAL ---")]
    [Tooltip("Arrasta o objeto visual da porta aqui")]
    public Transform doorVisual; 

    public enum TipoAbertura { DeslizarParaLado, EncolherNaBorda }
    [Tooltip("Deslizar move a porta. Encolher cria um pivô automático na borda e aperta a malha.")]
    public TipoAbertura modoAbertura = TipoAbertura.DeslizarParaLado;

    public enum LadoAbertura { Esquerda, Direita, Cima, Baixo }
    [Tooltip("Para que lado a porta deve ir? Ou onde fica a borda que está presa à parede?")]
    public LadoAbertura lado = LadoAbertura.Esquerda;

    [Header("Animation Settings")]
    public float speed = 5f;

    [Header("Sounds")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;

    private bool shouldBeOpen = false;
    
    // Variáveis para o modo Deslizar
    private Vector3 posicaoFechada;
    private Vector3 posicaoAberta;

    // Variáveis para o modo Encolher
    private Transform pivotAutomatico;
    private Vector3 escalaFechada;
    private Vector3 escalaAberta;

    void Start()
    {
        if (string.IsNullOrEmpty(doorID)) doorID = gameObject.name;

        if (doorVisual != null)
        {
            // 1. Identificar a borda/tamanho real da porta lendo a malha 3D
            Vector3 tamanhoLocal = Vector3.one;
            MeshFilter mf = doorVisual.GetComponentInChildren<MeshFilter>();
            
            if (mf != null)
            {
                tamanhoLocal = mf.sharedMesh.bounds.size;
                // Multiplicamos pela escala para ter o tamanho exato na cena
                tamanhoLocal.Scale(doorVisual.localScale);
            }
            else
            {
                // Se for um modelo sem MeshFilter nativo, usa a escala
                tamanhoLocal = doorVisual.localScale;
            }

            if (modoAbertura == TipoAbertura.DeslizarParaLado)
            {
                posicaoFechada = doorVisual.localPosition;
                posicaoAberta = posicaoFechada;

                // Move exatamente o tamanho do modelo
                if (lado == LadoAbertura.Esquerda) posicaoAberta.x -= tamanhoLocal.x;
                else if (lado == LadoAbertura.Direita) posicaoAberta.x += tamanhoLocal.x;
                else if (lado == LadoAbertura.Cima) posicaoAberta.y += tamanhoLocal.y;
                else if (lado == LadoAbertura.Baixo) posicaoAberta.y -= tamanhoLocal.y;
            }
            else if (modoAbertura == TipoAbertura.EncolherNaBorda)
            {
                // 2. Cria um "Pivô" real na borda exata. Animação 100% nativa da Unity e sem gambiarras.
                pivotAutomatico = new GameObject(doorVisual.name + "_Pivot").transform;
                pivotAutomatico.SetParent(doorVisual.parent);
                
                Vector3 posicaoDaBorda = doorVisual.localPosition;
                
                // Puxa o pivô exatamente para a extremidade da porta
                if (lado == LadoAbertura.Esquerda) posicaoDaBorda.x -= tamanhoLocal.x / 2f;
                else if (lado == LadoAbertura.Direita) posicaoDaBorda.x += tamanhoLocal.x / 2f;
                else if (lado == LadoAbertura.Cima) posicaoDaBorda.y += tamanhoLocal.y / 2f;
                else if (lado == LadoAbertura.Baixo) posicaoDaBorda.y -= tamanhoLocal.y / 2f;

                pivotAutomatico.localPosition = posicaoDaBorda;
                pivotAutomatico.localRotation = doorVisual.localRotation;
                
                // Coloca a porta dentro deste novo pivô.
                doorVisual.SetParent(pivotAutomatico);

                escalaFechada = pivotAutomatico.localScale;
                escalaAberta = escalaFechada;

                if (lado == LadoAbertura.Esquerda || lado == LadoAbertura.Direita)
                    escalaAberta.x = 0f;
                else
                    escalaAberta.y = 0f;
            }
        }

        int estadoSalvo = PlayerPrefs.GetInt(doorID, 0); 
        shouldBeOpen = (estadoSalvo == 1);

        // Aplica o estado guardado imediatamente
        if (doorVisual != null)
        {
            if (modoAbertura == TipoAbertura.DeslizarParaLado)
                doorVisual.localPosition = shouldBeOpen ? posicaoAberta : posicaoFechada;
            else if (pivotAutomatico != null)
                pivotAutomatico.localScale = shouldBeOpen ? escalaAberta : escalaFechada;
        }
    }

    void Update()
    {
        if (doorVisual == null) return;

        // Lógica de Animação perfeitamente limpa
        if (modoAbertura == TipoAbertura.DeslizarParaLado)
        {
            Vector3 alvo = shouldBeOpen ? posicaoAberta : posicaoFechada;
            doorVisual.localPosition = Vector3.Lerp(doorVisual.localPosition, alvo, Time.deltaTime * speed);
        }
        else if (modoAbertura == TipoAbertura.EncolherNaBorda && pivotAutomatico != null)
        {
            Vector3 alvo = shouldBeOpen ? escalaAberta : escalaFechada;
            pivotAutomatico.localScale = Vector3.Lerp(pivotAutomatico.localScale, alvo, Time.deltaTime * speed);
        }
    }

    public void SetState(bool shouldOpen) 
    {
        if (shouldBeOpen != shouldOpen)
        {
            shouldBeOpen = shouldOpen;
            PlaySound();

            if (!string.IsNullOrEmpty(doorID))
            {
                PlayerPrefs.SetInt(doorID, shouldBeOpen ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
    }

    void PlaySound()
    {
        if (audioSource != null)
        {
            if (shouldBeOpen && openSound) audioSource.PlayOneShot(openSound);
            else if (!shouldBeOpen && closeSound) audioSource.PlayOneShot(closeSound);
        }
    }
}
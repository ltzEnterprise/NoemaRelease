using UnityEngine;

public class ReceiverDoor : MonoBehaviour
{
    [Header("--- SAVE NATIVO ---")]
    [Tooltip("Dá um nome único para esta porta. Ex: Porta_Ruina_1")]
    public string doorID = "";

    [Header("--- VISUAL ---")]
    [Tooltip("Arrasta o objeto visual da porta aqui")]
    public Transform doorVisual; 

    public enum LadoSumir { Esquerda, Direita, Cima, Baixo }
    [Tooltip("Para qual lado a porta deve 'sumir' (encolher)?")]
    public LadoSumir direcaoParaSumir = LadoSumir.Esquerda;

    [Header("Animação")]
    public float speed = 5f;

    [Header("Áudio")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;

    private bool shouldBeOpen = false;
    private Transform pivotAutomatico;
    private Vector3 escalaFechada;
    private Vector3 escalaAberta;

    void Start()
    {
        if (string.IsNullOrEmpty(doorID)) doorID = gameObject.name;

        if (doorVisual != null)
        {
            // 1. Identificar a borda real lendo a malha 3D
            Vector3 tamanhoLocal = doorVisual.localScale;
            MeshFilter mf = doorVisual.GetComponentInChildren<MeshFilter>();
            
            if (mf != null)
            {
                tamanhoLocal = mf.sharedMesh.bounds.size;
                tamanhoLocal.Scale(doorVisual.localScale); // Ajusta pela escala que tá na cena
            }

            // 2. Cria o Pivô Invisível na borda exata da porta
            pivotAutomatico = new GameObject(doorVisual.name + "_PivotSumico").transform;
            pivotAutomatico.SetParent(doorVisual.parent);
            
            Vector3 posicaoDaBorda = doorVisual.localPosition;
            
            // Puxa o pivô pra extremidade
            if (direcaoParaSumir == LadoSumir.Esquerda) posicaoDaBorda.x -= tamanhoLocal.x / 2f;
            else if (direcaoParaSumir == LadoSumir.Direita) posicaoDaBorda.x += tamanhoLocal.x / 2f;
            else if (direcaoParaSumir == LadoSumir.Cima) posicaoDaBorda.y += tamanhoLocal.y / 2f;
            else if (direcaoParaSumir == LadoSumir.Baixo) posicaoDaBorda.y -= tamanhoLocal.y / 2f;

            pivotAutomatico.localPosition = posicaoDaBorda;
            pivotAutomatico.localRotation = doorVisual.localRotation;
            
            // Coloca a porta como filha do pivô
            doorVisual.SetParent(pivotAutomatico);

            escalaFechada = pivotAutomatico.localScale;
            escalaAberta = escalaFechada;

            // Define qual eixo vai "sumir" virando zero
            if (direcaoParaSumir == LadoSumir.Esquerda || direcaoParaSumir == LadoSumir.Direita)
                escalaAberta.x = 0f;
            else
                escalaAberta.y = 0f;
        }

        int estadoSalvo = PlayerPrefs.GetInt(doorID, 0); 
        shouldBeOpen = (estadoSalvo == 1);

        // Aplica o estado guardado imediatamente
        if (pivotAutomatico != null)
        {
            pivotAutomatico.localScale = shouldBeOpen ? escalaAberta : escalaFechada;
        }
    }

    void Update()
    {
        if (pivotAutomatico == null) return;

        // Animação cravada: encolhe até sumir no eixo certo
        Vector3 alvo = shouldBeOpen ? escalaAberta : escalaFechada;
        pivotAutomatico.localScale = Vector3.Lerp(pivotAutomatico.localScale, alvo, Time.deltaTime * speed);
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
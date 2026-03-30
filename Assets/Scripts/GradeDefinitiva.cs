using UnityEngine;

public class GradeDefinitiva : MonoBehaviour
{
    [Header("Configurações Básicas")]
    [Tooltip("Distância em metros para a grade sumir.")]
    public float distanciaParaAbrir = 3.0f;
    
    public GameObject visualGrade;
    public Collider colisorBloqueio;
    
    // Se não arrastar nada aqui, ele pega a Camera.main automática
    public Transform jogadorOuCamera; 

    [Header("Sons (Mantido)")]
    public AudioSource audioSource;
    public AudioClip somAparecer;
    public AudioClip somDesaparecer;
    [Range(0f, 1f)] public float volume = 0.5f;

    private bool estaAtiva = true; // Estado atual da grade

    void Start()
    {
        // Auto-Setup para evitar erros
        if (!visualGrade) visualGrade = GetComponent<Renderer>()?.gameObject;
        if (!colisorBloqueio) colisorBloqueio = GetComponent<Collider>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();

        // Se não definiu o jogador, usa a câmera principal
        if (jogadorOuCamera == null)
        {
            if (Camera.main != null) jogadorOuCamera = Camera.main.transform;
            else Debug.LogError("ERRO: Não achei o Player nem a Câmera Main!");
        }

        // Configuração de Áudio original
        if (audioSource)
        {
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.loop = false;
            audioSource.playOnAwake = false;
        }

        // Garante estado inicial (Fechada/Visível)
        AplicarEstado(true, false);
    }

    void Update()
    {
        if (!jogadorOuCamera) return;

        // 1. Calcula a distância real
        float distancia = Vector3.Distance(transform.position, jogadorOuCamera.position);

        // 2. Lógica de Decisão com "Folga" (Histerese) para não piscar o som
        // Se está perto (menor que o limite) -> DESATIVA (Sobe/Some)
        if (distancia < distanciaParaAbrir)
        {
            if (estaAtiva) // Só muda se estava fechada
            {
                estaAtiva = false;
                AplicarEstado(false, true); // False = Some, True = Toca Som
            }
        }
        // Se afastou (maior que limite + folga de 0.5m) -> ATIVA (Aparece)
        else if (distancia > (distanciaParaAbrir + 0.5f))
        {
            if (!estaAtiva) // Só muda se estava aberta
            {
                estaAtiva = true;
                AplicarEstado(true, true);
            }
        }
    }

    void AplicarEstado(bool ativa, bool tocarSom)
    {
        if (visualGrade) visualGrade.SetActive(ativa);
        if (colisorBloqueio) colisorBloqueio.enabled = ativa;

        if (tocarSom) TocarSom(ativa);
    }

    void TocarSom(bool apareceu)
    {
        if (!audioSource) return;

        AudioClip clip = apareceu ? somAparecer : somDesaparecer;
        if (!clip) return;

        // Stop garante que não sobreponha sons estranhamente
        audioSource.Stop(); 
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.Play();
    }

    // Desenha o círculo verde na Scene pra você ajustar a distância
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distanciaParaAbrir);
    }
}
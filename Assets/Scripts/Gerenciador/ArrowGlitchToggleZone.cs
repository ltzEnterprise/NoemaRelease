using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class ArrowGlobalGlitchToggle : MonoBehaviour
{
    [Header("--- SAVE ---")]
    public string uniqueID = "Puzzle_Setas_Glitch_Global";

    [Header("--- SEQUÊNCIA DAS SETAS ---")]
    public List<KeyCode> sequenciaSetas = new List<KeyCode>
    {
        KeyCode.UpArrow,
        KeyCode.LeftArrow,
        KeyCode.UpArrow,
        KeyCode.RightArrow,
        KeyCode.DownArrow,
        KeyCode.RightArrow
    };

    [Header("--- SHADER E RENDERER ---")]
    public Material glitchMaterial;
    public UniversalRendererData rendererData;
    public string featureName = "FullScreenPassRendererFeature";
    public float intensidadeGlitch = 1.0f;

    [Header("--- OBJETO QUE APARECE COM O GLITCH ---")]
    public GameObject objetoParaAparecer;

    [Header("--- SONS ---")]
    public AudioClip somSucesso;
    public AudioClip somErroForaDoCollider;
    [Range(0f, 1f)] public float volumeSom = 1f;

    [Header("--- PLAYER ---")]
    public string tagPlayer = "Player";

    private ScriptableRendererFeature glitchFeature;
    private AudioSource audioSource;

    private bool jogadorDentro = false;
    private bool glitchLigado = false;
    private bool inicializado = false;

    private int indexAtual = 0;

    private string ChaveGlitchLigado
    {
        get { return uniqueID + "_GlitchLigado"; }
    }

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    void Start()
    {
        BuscarGlitchFeature();
        StartCoroutine(InicializarSeguro());
    }

    private void BuscarGlitchFeature()
    {
        glitchFeature = null;

        if (rendererData == null)
        {
            Debug.LogError("[ArrowGlobalGlitchToggle] rendererData está vazio no Inspector.");
            return;
        }

        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature != null && feature.name == featureName)
            {
                glitchFeature = feature;
                Debug.Log("[ArrowGlobalGlitchToggle] Renderer Feature encontrada: " + feature.name);
                return;
            }
        }

        Debug.LogError("[ArrowGlobalGlitchToggle] Não achei a Renderer Feature chamada: " + featureName);
    }

    IEnumerator InicializarSeguro()
    {
        if (PersistenciaManager.Instance != null)
        {
            yield return new WaitUntil(() =>
                PersistenciaManager.Instance.DadosProntosParaUso &&
                !PersistenciaManager.Instance.EstaCarregando
            );

            glitchLigado = PersistenciaManager.Instance.ObterEstado(ChaveGlitchLigado, false);
        }

        AplicarEstadoAtual(false);

        inicializado = true;
    }

    void Update()
    {
        if (!inicializado) return;
        if (sequenciaSetas == null || sequenciaSetas.Count == 0) return;

        VerificarSequencia();
    }

    private void VerificarSequencia()
    {
        bool apertouSeta =
            Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow) ||
            Input.GetKeyDown(KeyCode.RightArrow);

        if (!apertouSeta)
            return;

        if (Input.GetKeyDown(sequenciaSetas[indexAtual]))
        {
            indexAtual++;

            if (indexAtual >= sequenciaSetas.Count)
            {
                indexAtual = 0;
                SequenciaCompleta();
            }

            return;
        }

        if (Input.GetKeyDown(sequenciaSetas[0]))
            indexAtual = 1;
        else
            indexAtual = 0;
    }

    private void SequenciaCompleta()
    {
        if (!jogadorDentro)
        {
            TocarSom(somErroForaDoCollider);
            Debug.Log("[ArrowGlobalGlitchToggle] Código certo, mas fora do collider.");
            return;
        }

        glitchLigado = !glitchLigado;

        AplicarEstadoAtual(true);
        SalvarEstado();

        Debug.Log("[ArrowGlobalGlitchToggle] Glitch global agora está: " + glitchLigado);
    }

    private void AplicarEstadoAtual(bool tocarSom)
    {
        if (glitchLigado)
            LigarGlitch();
        else
            DesligarGlitch();

        if (objetoParaAparecer != null)
            objetoParaAparecer.SetActive(glitchLigado);

        if (tocarSom)
            TocarSom(somSucesso);
    }

    private void LigarGlitch()
    {
        if (glitchFeature != null && !glitchFeature.isActive)
            glitchFeature.SetActive(true);

        if (glitchMaterial != null)
            glitchMaterial.SetFloat("_Intensity", intensidadeGlitch);

        Debug.Log("[ArrowGlobalGlitchToggle] LigarGlitch chamado. Intensidade: " + intensidadeGlitch);
    }

    private void DesligarGlitch()
    {
        if (glitchMaterial != null)
            glitchMaterial.SetFloat("_Intensity", 0f);

        if (glitchFeature != null && glitchFeature.isActive)
            glitchFeature.SetActive(false);

        Debug.Log("[ArrowGlobalGlitchToggle] DesligarGlitch chamado.");
    }

    private void SalvarEstado()
    {
        if (PersistenciaManager.Instance == null)
            return;

        if (string.IsNullOrEmpty(uniqueID))
            return;

        PersistenciaManager.Instance.RegistrarEstado(ChaveGlitchLigado, glitchLigado);
        PersistenciaManager.Instance.SalvarTudo(true);
    }

    private void TocarSom(AudioClip clip)
    {
        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip, volumeSom);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(tagPlayer)) return;

        jogadorDentro = true;
        indexAtual = 0;

        Debug.Log("[ArrowGlobalGlitchToggle] Player entrou no collider.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(tagPlayer)) return;

        jogadorDentro = false;
        indexAtual = 0;

        Debug.Log("[ArrowGlobalGlitchToggle] Player saiu do collider.");
    }
}
using UnityEngine;

public class Crowbar : MonoBehaviour
{
    [Header("Status")]
    public float alcance = 2.5f; 
    public float taxaDeAtaque = 0.5f;

    [Header("Animação")]
    public float forcaDoGolpe = 45f; 
    public float velocidadeRetorno = 5f;

    [Header("Referências")]
    public Camera cameraFPS;
    public AudioSource audioSource;
    public AudioClip somVento; 
    public AudioClip somImpacto; 

    private Vector3 posicaoOriginal;
    private Quaternion rotacaoOriginal;
    private Quaternion rotacaoExtra;
    private float proximoAtaque = 0f;

    void Awake()
    {
        posicaoOriginal = transform.localPosition;
        rotacaoOriginal = transform.localRotation;
        rotacaoExtra = Quaternion.identity;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.Stop();
        }
    }

    void OnEnable()
    {
        transform.localPosition = posicaoOriginal;
        transform.localRotation = rotacaoOriginal;
        rotacaoExtra = Quaternion.identity;

        if (audioSource != null)
            audioSource.Stop();
    }

    void Update()
    {
        if (FPS_Master.travadoInteracao) return;

        rotacaoExtra = Quaternion.Lerp(rotacaoExtra, Quaternion.identity, Time.deltaTime * velocidadeRetorno);
        
        transform.localPosition = posicaoOriginal;
        transform.localRotation = rotacaoOriginal * rotacaoExtra;

        if (Input.GetButtonDown("Fire1") && Time.time >= proximoAtaque)
        {
            Atacar();
        }
    }

    void Atacar()
    {
        proximoAtaque = Time.time + taxaDeAtaque;
        rotacaoExtra = Quaternion.Euler(forcaDoGolpe, 0, 0);

        if (audioSource != null && somVento != null)
            audioSource.PlayOneShot(somVento);

        if (cameraFPS == null)
            cameraFPS = Camera.main;

        if (cameraFPS == null)
            return;

        RaycastHit hit;

        if (Physics.Raycast(cameraFPS.transform.position, cameraFPS.transform.forward, out hit, alcance, ~0, QueryTriggerInteraction.Ignore))
        {
            WoodenBarricade barricada = hit.transform.GetComponentInParent<WoodenBarricade>();
            
            if (barricada != null)
            {
                barricada.Interagir();

                if (audioSource != null && somImpacto != null)
                    audioSource.PlayOneShot(somImpacto);
            }
        }
    }
}
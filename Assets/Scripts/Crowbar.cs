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
    }

    void OnEnable()
    {
        transform.localPosition = posicaoOriginal;
        transform.localRotation = rotacaoOriginal;
        rotacaoExtra = Quaternion.identity;
    }

    void Update()
    {
        // Trava se estiver em menu
        if (FPS_Master.travadoInteracao) return;

        // Animação de retorno
        rotacaoExtra = Quaternion.Lerp(rotacaoExtra, Quaternion.identity, Time.deltaTime * velocidadeRetorno);
        
        transform.localPosition = posicaoOriginal;
        transform.localRotation = rotacaoOriginal * rotacaoExtra;

        // Ataque (Botão Esquerdo)
        if (Input.GetButtonDown("Fire1") && Time.time >= proximoAtaque)
        {
            Atacar();
        }
    }

    void Atacar()
    {
        proximoAtaque = Time.time + taxaDeAtaque;
        rotacaoExtra = Quaternion.Euler(forcaDoGolpe, 0, 0);

        if (audioSource && somVento) audioSource.PlayOneShot(somVento);

        RaycastHit hit;
        if (Physics.Raycast(cameraFPS.transform.position, cameraFPS.transform.forward, out hit, alcance))
        {
            // Verifica se acertou uma Barricada (Novo nome: WoodenBarricade)
            WoodenBarricade barricada = hit.transform.GetComponentInParent<WoodenBarricade>();
            
            // Opcional: Se quiser que o clique também quebre a barricada (além do E)
            if (barricada != null)
            {
                barricada.Interagir(); // Chama a função de quebrar
            }
            
            if (audioSource && somImpacto) audioSource.PlayOneShot(somImpacto);
        }
    }
}
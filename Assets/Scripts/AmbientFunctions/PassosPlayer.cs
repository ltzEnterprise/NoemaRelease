using UnityEngine;

public class PassosPlayer : MonoBehaviour
{
    [Header("Configurações")]
    public float intervaloPassos = 0.5f; // Tempo entre cada passo
    public AudioSource fonteDeAudio;     // Onde sai o som (pode ser o pé ou o corpo)
    
    [Header("Sons")]
    public AudioClip[] sonsDeGrama;      // Lista de arquivos de som (arraste vários aqui)
    public AudioClip[] sonsPadrao;       // Som genérico (concreto/madeira)

    private CharacterController cc;
    private float proximoPasso = 0;

    void Start()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        // Só toca se: Estiver no chão E se movendo
        if (cc.isGrounded && cc.velocity.magnitude > 2f && Time.time > proximoPasso)
        {
            TocarPasso();
            proximoPasso = Time.time + intervaloPassos;
        }
    }

    void TocarPasso()
    {
        // Ajusta o Pitch aleatoriamente (Deixa o som mais grave ou agudo pra variar)
        fonteDeAudio.pitch = Random.Range(0.8f, 1.1f);
        fonteDeAudio.volume = Random.Range(0.8f, 1.0f); // Leve variação de volume

        // Detectar o chão
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 2.5f))
        {
            if (hit.collider.CompareTag("Grama"))
            {
                ReproduzirAleatorio(sonsDeGrama);
            }
            else
            {
                ReproduzirAleatorio(sonsPadrao);
            }
        }
    }

    void ReproduzirAleatorio(AudioClip[] lista)
    {
        if (lista.Length > 0)
        {
            // Sorteia um som da lista
            int index = Random.Range(0, lista.Length);
            fonteDeAudio.PlayOneShot(lista[index]);
        }
    }
}
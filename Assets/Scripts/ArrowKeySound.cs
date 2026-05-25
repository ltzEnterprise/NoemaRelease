using UnityEngine;

public class ArrowKeySound : MonoBehaviour
{
    [Header("--- ÁUDIO ---")]
    public AudioSource audioSource;
    public AudioClip somSeta;

    [Header("--- CONFIG ---")]
    [Range(0f, 1f)] public float volume = 1f;

    [Tooltip("Se ligado, permite tocar o som várias vezes rapidamente.")]
    public bool permitirSobreporSom = true;

    [Tooltip("Teclas que vão tocar o som. Pode adicionar/remover no Inspector.")]
    public KeyCode[] teclas = new KeyCode[]
    {
        KeyCode.UpArrow,
        KeyCode.DownArrow,
        KeyCode.LeftArrow,
        KeyCode.RightArrow
    };

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (somSeta == null || audioSource == null)
            return;

        for (int i = 0; i < teclas.Length; i++)
        {
            if (Input.GetKeyDown(teclas[i]))
            {
                TocarSom();
                return;
            }
        }
    }

    private void TocarSom()
    {
        if (permitirSobreporSom)
        {
            audioSource.PlayOneShot(somSeta, volume);
        }
        else
        {
            audioSource.Stop();
            audioSource.clip = somSeta;
            audioSource.volume = volume;
            audioSource.Play();
        }
    }
}
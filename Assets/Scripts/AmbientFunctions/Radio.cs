using UnityEngine;

public class Radio : MonoBehaviour
{
    [Header("Configurações")]
    public AudioSource caixinhaDeSom; // O componente que toca a música
    public GameObject textoAviso;     // O texto "Aperte E" (Opcional)
    public string teclaInteracao = "e";

    private bool playerPerto = false;

    void Start()
    {
        // Garante que o texto comece desligado
        if(textoAviso != null) textoAviso.SetActive(false);
    }

    void Update()
    {
        // Se tá perto e apertou a tecla
        if (playerPerto && Input.GetKeyDown(teclaInteracao))
        {
            AlternarMusica();
        }
    }

    void AlternarMusica()
    {
        if (caixinhaDeSom.isPlaying)
        {
            caixinhaDeSom.Pause(); // Se tá tocando, pausa
        }
        else
        {
            caixinhaDeSom.Play(); // Se tá parado, toca
        }
    }

    // --- DETECTA O PLAYER ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerPerto = true;
            if (textoAviso != null) textoAviso.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerPerto = false;
            if (textoAviso != null) textoAviso.SetActive(false);
        }
    }
}
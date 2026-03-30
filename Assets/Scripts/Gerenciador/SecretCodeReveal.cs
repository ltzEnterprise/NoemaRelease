using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class SecretCodeReveal : MonoBehaviour
{
    [Header("--- CONFIGURAÇÃO ---")]
    [Tooltip("A sequência de teclas para ativar o segredo.")]
    public List<KeyCode> sequenciaSecreta;

    [Header("--- O QUE ACONTECE ---")]
    [Tooltip("O objeto (ou grupo) que vai aparecer no mapa.")]
    public GameObject objetoParaAparecer;
    
    [Tooltip("O som que toca ao acertar.")]
    public AudioClip somSucesso;
    
    [Tooltip("Volume do som (0 a 1).")]
    [Range(0f, 1f)] public float volumeSom = 1f;

    // Variáveis internas
    private int indexAtual = 0;
    private AudioSource audioSource;

    void Start()
    {
        // Garante que o objeto comece escondido
        if (objetoParaAparecer != null)
        {
            objetoParaAparecer.SetActive(false);
        }

        // Configura o áudio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // SE VOCÊ NÃO PREENCHEU A LISTA NO INSPECTOR, EU PREENCHO AQUI
        // C = Cima, E = Esquerda, D = Direita, B = Baixo
        if (sequenciaSecreta == null || sequenciaSecreta.Count == 0)
        {
            sequenciaSecreta = new List<KeyCode>
            {
                KeyCode.UpArrow,    // C
                KeyCode.LeftArrow,  // E
                KeyCode.UpArrow,    // C
                KeyCode.RightArrow, // D
                KeyCode.DownArrow,  // B
                KeyCode.RightArrow  // D
            };
        }
    }

    void Update()
    {
        // Só processa se tiver teclas configuradas
        if (sequenciaSecreta.Count == 0) return;

        if (Input.anyKeyDown)
        {
            // Verifica se a tecla pressionada é a que a gente espera
            if (Input.GetKeyDown(sequenciaSecreta[indexAtual]))
            {
                // Acertou a tecla atual, avança para a próxima
                indexAtual++;

                // Chegou no final da sequência?
                if (indexAtual >= sequenciaSecreta.Count)
                {
                    AtivarSegredo();
                    indexAtual = 0; // Reseta para poder fazer de novo (ou remova se for só uma vez)
                }
            }
            else
            {
                // Errou a sequência!
                // Mas espera... e se a tecla errada for o COMEÇO da sequência de novo?
                // Ex: Se a senha é CIMA, BAIXO. E eu aperto CIMA, CIMA.
                // O segundo CIMA deve contar como o início da nova tentativa.
                
                if (Input.GetKeyDown(sequenciaSecreta[0]))
                {
                    indexAtual = 1; // Reinicia já com 1 acerto
                }
                else
                {
                    indexAtual = 0; // Reinicia do zero
                }
            }
        }
    }

    void AtivarSegredo()
    {
        Debug.Log("SENHA SECRETA ATIVADA!");

        if (objetoParaAparecer != null)
        {
            // Se já estiver ativo, desativa (Toggle). Se quiser que só ative, use .SetActive(true)
            bool estadoAtual = objetoParaAparecer.activeSelf;
            objetoParaAparecer.SetActive(!estadoAtual); 
        }

        if (somSucesso != null && audioSource != null)
        {
            audioSource.PlayOneShot(somSucesso, volumeSom);
        }
    }
}
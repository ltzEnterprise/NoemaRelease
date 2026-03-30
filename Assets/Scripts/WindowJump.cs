using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WindowJump : MonoBehaviour
{
    [Header("Configurações de Pulo")]
    public Transform landingSpot;     
    public bool oneTimeOnly = true;   

    [Header("UI - Fade")]
    public Image blackScreenPanel;    
    public float fadeSpeed = 3f;
    [Tooltip("Texto que aparece quando o jogador olha pra janela (Ex: 'Pular [E]')")]
    public GameObject interactText; 

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip jumpSound;       
    public AudioClip landSound;       

    private bool isActing = false;
    private bool hasUsed = false;

    void Start()
    {
        // Garante que o texto começa desligado
        if (interactText) interactText.SetActive(false);

        if (blackScreenPanel) 
        {
            blackScreenPanel.gameObject.SetActive(false);
            blackScreenPanel.color = new Color(0, 0, 0, 0);
        }
    }

    // --- MÉTODOS DO RAYCAST ---
    public void AoOlhar()
    {
        // Só mostra o texto se não estiver no meio da ação E se ainda puder ser usada
        if (!isActing && !(hasUsed && oneTimeOnly))
        {
            if (interactText) interactText.SetActive(true);
        }
    }

    public void AoSair()
    {
        // Esconde o texto quando o jogador vira a cara
        if (interactText) interactText.SetActive(false);
    }
    // --------------------------

    // Chamado pelo Raycast do FPS_Master via SendMessageUpwards("Interagir")
    public void Interagir()
    {
        if (isActing || (hasUsed && oneTimeOnly)) return;
        
        // Apaga o texto imediatamente na hora que aperta o botão
        if (interactText) interactText.SetActive(false);

        StartCoroutine(JumpSequence());
    }

    IEnumerator JumpSequence()
    {
        isActing = true;

        // Trava o player no lugar pra não andar cego e cair no void
        if (FPS_Master.Instance != null)
        {
            FPS_Master.Instance.AlterarEstadoJogador(true, false);
        }

        if (audioSource && jumpSound) audioSource.PlayOneShot(jumpSound);

        // 1. Fade pra preto
        if (blackScreenPanel)
        {
            blackScreenPanel.gameObject.SetActive(true);
            float alpha = 0;
            while (alpha < 1)
            {
                alpha += Time.deltaTime * fadeSpeed;
                blackScreenPanel.color = new Color(0, 0, 0, Mathf.Clamp01(alpha));
                yield return null;
            }
        }

        yield return new WaitForSeconds(0.2f);

        // 2. Teleporte usando APENAS a função blindada do FPS_Master (sem girar a porra da câmera)
        if (FPS_Master.Instance != null && landingSpot != null)
        {
            FPS_Master.Instance.Teleportar(landingSpot.position);
        }

        if (audioSource && landSound) audioSource.PlayOneShot(landSound);
        yield return new WaitForSeconds(0.3f); 

        // 3. Clarear tela
        if (blackScreenPanel)
        {
            float alpha = 1;
            while (alpha > 0)
            {
                alpha -= Time.deltaTime * fadeSpeed;
                blackScreenPanel.color = new Color(0, 0, 0, Mathf.Clamp01(alpha));
                yield return null;
            }
            blackScreenPanel.gameObject.SetActive(false);
        }

        // Destrava o player
        if (FPS_Master.Instance != null)
        {
            FPS_Master.Instance.AlterarEstadoJogador(false, false);
        }

        isActing = false;
        if (oneTimeOnly) hasUsed = true;
    }
}
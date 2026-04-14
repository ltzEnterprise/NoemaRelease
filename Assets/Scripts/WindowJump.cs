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
    
    // 🔥 A TRAVA BLINDADA 🔥
    private bool estaOlhando = false;

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

    void Update()
    {
        // 🔥 SISTEMA FORÇADO PRA NÃO BUGAR A UI 🔥
        if (interactText != null)
        {
            // A regra: Só aparece se estiver olhando, NÃO estiver pulando E não tiver esgotado o uso
            bool deveAparecer = estaOlhando && !isActing && !(hasUsed && oneTimeOnly);
            
            if (interactText.activeSelf != deveAparecer)
            {
                interactText.SetActive(deveAparecer);
            }
        }
    }

    // --- MÉTODOS DO RAYCAST ---
    public void AoOlhar()
    {
        estaOlhando = true; 
    }

    public void AoSair()
    {
        estaOlhando = false; 
    }
    // --------------------------

    // Chamado pelo Raycast do FPS_Master via SendMessageUpwards("Interagir")
    public void Interagir()
    {
        if (isActing || (hasUsed && oneTimeOnly)) return;

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

        // 2. Teleporte cego
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
        
        estaOlhando = false; // Garante que a mira desliga quando você chega do outro lado
    }
}
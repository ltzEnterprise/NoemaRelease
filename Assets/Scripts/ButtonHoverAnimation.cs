using UnityEngine;
using UnityEngine.EventSystems; // Necessário para detectar o mouse
using TMPro; // Necessário para mexer no Texto
using System.Collections;

public class ButtonHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Configurações")]
    public TextMeshProUGUI textComponent; // O texto que vai crescer
    public float scaleMultiplier = 1.1f;  // O quanto vai crescer (1.1 = 10% maior)
    public float animationSpeed = 0.15f;   // Tempo que leva para crescer

    private Vector3 originalScale;
    private Coroutine currentCoroutine;

    void Start()
    {
        // Se esqueceu de arrastar o texto, ele tenta achar no próprio objeto ou nos filhos
        if (textComponent == null)
            textComponent = GetComponentInChildren<TextMeshProUGUI>();

        // Salva o tamanho original do texto para saber como voltar depois
        if (textComponent != null)
            originalScale = textComponent.transform.localScale;
    }

    // Quando o mouse ENTRA no botão
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (textComponent == null) return;
        Vector3 targetScale = originalScale * scaleMultiplier;
        StartAnimation(targetScale);
    }

    // Quando o mouse SAI do botão
    public void OnPointerExit(PointerEventData eventData)
    {
        if (textComponent == null) return;
        StartAnimation(originalScale);
    }

    // Se o painel for desativado enquanto o botão tava grande, reseta o tamanho
    void OnDisable()
    {
        if (textComponent != null && originalScale != Vector3.zero)
        {
            textComponent.transform.localScale = originalScale;
            if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        }
    }

    // Função auxiliar para gerenciar a animação
    private void StartAnimation(Vector3 target)
    {
        if (!gameObject.activeInHierarchy) return; // Evita erro se tentar animar com o objeto desligado
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(AnimateScale(target));
    }

    // A mágica da animação suave (Lerp)
    IEnumerator AnimateScale(Vector3 target)
    {
        Vector3 initialScale = textComponent.transform.localScale;
        float time = 0;

        while (time < animationSpeed)
        {
            // Interpola suavemente do tamanho atual para o alvo
            textComponent.transform.localScale = Vector3.Lerp(initialScale, target, time / animationSpeed);
            
            // O SEGREDO: unscaledDeltaTime ignora o pause do jogo!
            time += Time.unscaledDeltaTime; 
            yield return null;
        }

        // Garante que fique exatamente do tamanho final
        textComponent.transform.localScale = target;
    }
}
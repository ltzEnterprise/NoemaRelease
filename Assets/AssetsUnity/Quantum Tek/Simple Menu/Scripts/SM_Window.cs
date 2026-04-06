using UnityEngine;

namespace QuantumTek.SimpleMenu
{
    [AddComponentMenu("Quantum Tek/Simple Menu/Window")]
    [DisallowMultipleComponent]
    public class SM_Window : MonoBehaviour
    {
        [Header("Object References")]
        public Transform content;
        public Animator animator; // Mantido só pra não quebrar seu Inspector, mas será ignorado.

        [Header("Animation Variables (IGNORADOS)")]
        public SM_AnimationType animationType;
        public string animatorBool = "Shown";
        public string animatorShowTrigger = "Show";
        public string animatorHideTrigger = "Hide";

        [HideInInspector] public bool active;

        protected void Awake()
        {
            // Se o Animator existir, desliga a porra toda pra ele não interferir
            if (animator != null) animator.enabled = false;

            if (content) active = content.gameObject.activeSelf;
            Toggle(active);
        }

        public void Toggle(bool shown)
        {
            active = shown;

            // 🔥 MODO NUCLEAR: FODA-SE A ANIMAÇÃO. LIGA E DESLIGA NA FORÇA BRUTA. 🔥
            if (content != null)
            {
                content.gameObject.SetActive(shown);
            }
        }
    }
}
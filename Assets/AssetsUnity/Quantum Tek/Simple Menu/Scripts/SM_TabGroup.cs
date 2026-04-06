using System.Collections.Generic;
using UnityEngine;

namespace QuantumTek.SimpleMenu
{
    [System.Serializable]
    public enum SM_TabAlign
    {
        Center,
        Left,
        Right
    }

    [AddComponentMenu("Quantum Tek/Simple Menu/Tab Group")]
    [DisallowMultipleComponent]
    public class SM_TabGroup : MonoBehaviour
    {
        [Header("Object References")]
        [SerializeField] protected Transform content;
        [SerializeField] protected Animator animator;
        [Space]
        [Header("Animation Variables")]
        [SerializeField] protected SM_AnimationType animationType;
        [SerializeField] protected string animatorBool = "Shown";
        [SerializeField] protected string animatorShowTrigger = "Show";
        [SerializeField] protected string animatorHideTrigger = "Hide";
        [Space]
        [Header("Tab Variables")]
        [SerializeField] protected Vector2 tabOffset;
        [SerializeField] protected SM_TabAlign alignment;

        protected List<SM_TabWindow> windows;
        protected List<SM_Tab> tabs;
        protected SM_TabWindow current;
        [HideInInspector] public bool active;

        // 🔥 A SOLUÇÃO BRUTA QUE VOCÊ PEDIU 🔥
        // Toda vez que esse grupo de abas for ativado na tela, ele FORÇA a primeira aba (Gameplay)
        protected void OnEnable()
        {
            if (content == null) return;
            
            GetWindows(); // Atualiza a lista garantindo que vai achar tudo
            
            if (windows != null && windows.Count > 0)
            {
                // Desliga TODAS pra garantir que não vai ter aba sobreposta
                for (int i = 0; i < windows.Count; i++)
                {
                    if (windows[i] != null) windows[i].Toggle(false);
                }
                
                // Pega a primeira aba (Gameplay) e FORÇA ELA A FICAR SELECIONADA
                current = windows[0];
                current.Toggle(true);
            }
        }

        protected void Start()
        {
            if (content) active = content.gameObject.activeSelf;
            GetWindows();

            int windowCount = windows.Count;
            for (int i = 0; i < windowCount; ++i)
            { if (windows[i].content.gameObject.activeSelf) current = windows[i]; }

            ChangeTab(current);
            
            Toggle(active);
        }

        protected void GetWindows()
        {
            // 🔥 TRUE ADICIONADO AQUI: Faz ele achar as abas mesmo se estiverem invisíveis/desligadas
            SM_TabWindow[] tempWindows = content.GetComponentsInChildren<SM_TabWindow>(true);
            windows = new List<SM_TabWindow>(tempWindows);
            
            tabs = new List<SM_Tab>();
            int windowCount = windows.Count;
            for (int i = 0; i < windowCount; ++i)
            { tabs.Add(windows[i].tab); }
        }

        public void Toggle(bool shown)
        {
            active = shown;

            if (animator)
            {
                if (animationType == SM_AnimationType.ActiveState) { if (content) content.gameObject.SetActive(shown); }
                else if (animationType == SM_AnimationType.AnimatorBool) animator.SetBool(animatorBool, shown);
                else if (animationType == SM_AnimationType.AnimatorTrigger) animator.SetBool(shown ? animatorShowTrigger : animatorHideTrigger, shown);
            }
        }

        public void ChangeTab(SM_TabWindow tab)
        {
            if (!tab) return;
            
            // 🔥 PREVINE BUG: Só desliga a aba atual se você clicar em UMA DIFERENTE
            if (current != null && current != tab) current.Toggle(false);
            
            current = tab;
            if (current) current.Toggle(true);
        }

        public void AlignTabs()
        {
            if (windows == null || windows.Count == 0 || tabs == null || tabs.Count == 0) GetWindows();

            float tabsWidth = 0;
            int tabCount = tabs.Count;
            RectTransform tabTransform;
            for (int i = 0; i < tabCount; ++i)
            { tabTransform = tabs[i].GetComponent<RectTransform>(); if (!tabTransform) continue; tabsWidth += tabTransform.rect.width; }
            
            float currentTabWidth = 0;
            for (int i = 0; i < tabCount; ++i)
            {
                tabTransform = tabs[i].GetComponent<RectTransform>();
                if (!tabTransform) continue;
                float tabWidth = tabTransform.rect.width;
                float tabHeight = tabTransform.rect.height;

                if (alignment == SM_TabAlign.Center)
                {
                    tabTransform.anchorMin = new Vector2(0.5f, 1);
                    tabTransform.anchorMax = new Vector2(0.5f, 1);
                    tabTransform.pivot = new Vector2(0.5f, 1);
                    tabTransform.anchoredPosition = new Vector2(-tabsWidth / 2 + tabsWidth / 2 / tabCount + currentTabWidth, tabHeight + tabOffset.y);
                    currentTabWidth += tabWidth;
                }
                else if (alignment == SM_TabAlign.Left)
                {
                    tabTransform.anchorMin = new Vector2(0, 1);
                    tabTransform.anchorMax = new Vector2(0, 1);
                    tabTransform.pivot = new Vector2(0, 1);
                    tabTransform.anchoredPosition = new Vector2(currentTabWidth + tabOffset.x, tabHeight + tabOffset.y);
                    currentTabWidth += tabWidth;
                }
                else if (alignment == SM_TabAlign.Right)
                {
                    currentTabWidth += tabWidth;
                    tabTransform.anchorMin = new Vector2(1, 1);
                    tabTransform.anchorMax = new Vector2(1, 1);
                    tabTransform.pivot = new Vector2(1, 1);
                    tabTransform.anchoredPosition = new Vector2(-tabsWidth + currentTabWidth - tabOffset.x, tabHeight + tabOffset.y);
                }
            }
        }
    }
}
using UnityEngine;
using TMPro;

// Tirei o RequireComponent pra não forçar UI em texto 3D e bugar seu cenário
public class LocalizedText : MonoBehaviour
{
    [Header("--- TRADUÇÕES ---")]
    [TextArea(2, 4)] public string textoPT;
    [TextArea(2, 4)] public string textoEN;

    [Header("--- LAYOUT OVERRIDE ---")]
    public bool useLayoutOverride = false;

    [Header("Portuguese Layout")]
    public Vector2 ptAnchoredPosition = Vector2.zero;
    public Vector3 ptScale = Vector3.one;

    [Header("English Layout")]
    public Vector2 enAnchoredPosition = Vector2.zero;
    public Vector3 enScale = Vector3.one;

    // TMP_Text aceita tanto TextMeshProUGUI (Canvas) quanto TextMeshPro (3D)
    private TMP_Text myText; 
    private RectTransform myRect;
    private Transform myTransform;

    private void Awake()
    {
        InicializarComponentes();
    }

    private void OnEnable()
    {
        UpdateText();
    }

    private void Start()
    {
        UpdateText();
    }

    // 🔥 ATUALIZA E SALVA AS ALTERAÇÕES EM TEMPO REAL (EDIT E PLAY MODE) 🔥
    #if UNITY_EDITOR
    private void OnValidate()
    {
        // O delayCall evita que a Unity entre em loop infinito ao validar o componente
        UnityEditor.EditorApplication.delayCall += () => {
            if (this == null || gameObject == null) return;
            
            UpdateText();
            
            // Força a Unity a salvar a alteração que você fez no Inspector
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
            UnityEditor.EditorUtility.SetDirty(this);
        };
    }
    #endif

    private void InicializarComponentes()
    {
        if (myText == null) myText = GetComponent<TMP_Text>();
        
        if (myRect == null) myRect = GetComponent<RectTransform>();
        if (myTransform == null) myTransform = transform;
    }

    public void UpdateText()
    {
        InicializarComponentes();

        if (myText == null) return;

        // Verifica o idioma. Se o Manager não existir (Edit Mode), assume PT.
        int lang = (LanguageManager.Instance != null) ? LanguageManager.Instance.currentLanguage : 0;
        
        // 1. Aplica o Texto
        myText.text = (lang == 0) ? textoPT : textoEN;

        // 2. Aplica Layout se estiver marcado
        if (useLayoutOverride)
        {
            if (myRect != null) // SE FOR UI (CANVAS)
            {
                if (lang == 0)
                {
                    myRect.anchoredPosition = ptAnchoredPosition;
                    myRect.localScale = ptScale;
                }
                else
                {
                    myRect.anchoredPosition = enAnchoredPosition;
                    myRect.localScale = enScale;
                }
            }
            else if (myTransform != null) // SE FOR TEXTO 3D (MAPA)
            {
                if (lang == 0)
                {
                    // No 3D a gente usa localPosition. Altera o X e Y, mas respeita o Z que já tá lá.
                    myTransform.localPosition = new Vector3(ptAnchoredPosition.x, ptAnchoredPosition.y, myTransform.localPosition.z);
                    myTransform.localScale = ptScale;
                }
                else
                {
                    myTransform.localPosition = new Vector3(enAnchoredPosition.x, enAnchoredPosition.y, myTransform.localPosition.z);
                    myTransform.localScale = enScale;
                }
            }
        }
    }

    [ContextMenu("Salvar Posição e Escala Atual para o PT")]
    public void SalvarLayoutPT()
    {
        InicializarComponentes();
        
        if (myRect != null) ptAnchoredPosition = myRect.anchoredPosition;
        else if (myTransform != null) ptAnchoredPosition = new Vector2(myTransform.localPosition.x, myTransform.localPosition.y);
        
        ptScale = myTransform.localScale;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Salvar Posição e Escala Atual para o EN")]
    public void SalvarLayoutEN()
    {
        InicializarComponentes();
        
        if (myRect != null) enAnchoredPosition = myRect.anchoredPosition;
        else if (myTransform != null) enAnchoredPosition = new Vector2(myTransform.localPosition.x, myTransform.localPosition.y);
        
        enScale = myTransform.localScale;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
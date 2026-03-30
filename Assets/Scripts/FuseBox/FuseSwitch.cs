using UnityEngine;

public class FuseSwitch : MonoBehaviour
{
    [Header("--- MATEMÁTICA ---")]
    public float volts = 10f;
    public bool estaLigado = false;

    [Header("--- CONFIG ---")]
    public float velocidade = 15f;
    
    private FuseBoxController controladorMestre;
    private AudioSource source;
    
    // Variáveis para travar a posição original
    private float meuY_Fixo;
    private float meuZ_Fixo;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        controladorMestre = GetComponentInParent<FuseBoxController>();

        // 1. Salva posição original
        meuY_Fixo = transform.localPosition.y;
        meuZ_Fixo = transform.localPosition.z;

        // 2. Força OFF
        estaLigado = false;
        if (controladorMestre != null)
        {
            float xOFF = controladorMestre.posicaoGlobalX_OFF;
            transform.localPosition = new Vector3(xOFF, meuY_Fixo, meuZ_Fixo);
        }
    }

    void Update()
    {
        if (controladorMestre == null) return;

        float alvoX = estaLigado 
            ? controladorMestre.posicaoGlobalX_ON 
            : controladorMestre.posicaoGlobalX_OFF;

        Vector3 destino = new Vector3(alvoX, meuY_Fixo, meuZ_Fixo);
        transform.localPosition = Vector3.Lerp(transform.localPosition, destino, Time.deltaTime * velocidade);
    }

    public void Alternar()
    {
        estaLigado = !estaLigado;
        if (source && controladorMestre && controladorMestre.somClick) 
            source.PlayOneShot(controladorMestre.somClick);
        if (controladorMestre) controladorMestre.RecalcularVoltagemInstantanea();
    }

    public void ForcarDesligamento()
    {
        estaLigado = false;
    }
}
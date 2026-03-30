using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MagicBlock : MonoBehaviour
{
    [Header("Item Escondido (Chave)")]
    [Tooltip("Arraste a chave que JÁ ESTÁ na cena aqui.")]
    public GameObject objetoChaveNaCena; 
    public bool aparecerSoUmaVez = true;

    [System.Serializable]
    public class EstadoConfig
    {
        public string nome = "Estado Vazio"; 
        
        public List<GameObject> ligar = new List<GameObject>();
        public List<GameObject> desligar = new List<GameObject>();
    }

    [Header("CONFIGURAÇÃO MANUAL")]
    public List<EstadoConfig> estados; 

    [Header("Animação")]
    [Tooltip("Marque para o bloco dar um pulinho quando for atingido")]
    public bool usarAnimacaoPulo = false; // <--- NOME NOVO E DESATIVADO POR PADRÃO
    public float alturaAnimacao = 0.5f;
    public float velocidadeAnimacao = 5f;
    
    private Vector3 posicaoInicial;
    private bool isAnimating = false;

    [Header("Física")]
    [Tooltip("Cooldown para não ativar duas vezes seguidas muito rápido")]
    private float hitCooldown = 0.2f;
    private float lastHitTime;
    
    private float sensibilidadeBatida = -0.2f; 

    private int currentStateIndex = 0;
    private bool jaApareceu = false;

    void Start()
    {
        posicaoInicial = transform.position;

        if (objetoChaveNaCena != null)
        {
            objetoChaveNaCena.SetActive(false);
        }

        if (estados == null || estados.Count == 0)
        {
            estados = new List<EstadoConfig>();
            estados.Add(new EstadoConfig());
        }

        UpdateObjects(); 
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            if (Time.time - lastHitTime < hitCooldown || isAnimating) return;

            foreach (ContactPoint2D contact in col.contacts)
            {
                Vector3 pontoNoMundo = contact.point;
                Vector3 pontoLocal = transform.InverseTransformPoint(pontoNoMundo);

                if (pontoLocal.y < sensibilidadeBatida)
                {
                    ActivateBlock();
                    lastHitTime = Time.time;
                    return; 
                }
            }
        }
    }

    void ActivateBlock()
    {
        if (objetoChaveNaCena != null)
        {
            if (!aparecerSoUmaVez || (aparecerSoUmaVez && !jaApareceu))
            {
                objetoChaveNaCena.SetActive(true);
                jaApareceu = true;
            }
        }

        // Usa o nome novo aqui
        if (usarAnimacaoPulo)
        {
            StartCoroutine(RotinaAnimacaoBatida());
        }

        currentStateIndex++;
        if (currentStateIndex >= estados.Count) currentStateIndex = 0;

        UpdateObjects();
    }

    private IEnumerator RotinaAnimacaoBatida()
    {
        isAnimating = true;
        Vector3 posAlvo = posicaoInicial + (Vector3.up * alturaAnimacao);

        while (transform.position != posAlvo)
        {
            transform.position = Vector3.MoveTowards(transform.position, posAlvo, velocidadeAnimacao * Time.deltaTime);
            yield return null;
        }

        while (transform.position != posicaoInicial)
        {
            transform.position = Vector3.MoveTowards(transform.position, posicaoInicial, velocidadeAnimacao * Time.deltaTime);
            yield return null;
        }

        isAnimating = false;
    }

    void UpdateObjects()
    {
        if (currentStateIndex >= estados.Count) return;

        EstadoConfig configAtual = estados[currentStateIndex];

        if (configAtual.desligar != null)
        {
            foreach (var obj in configAtual.desligar)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        if (configAtual.ligar != null)
        {
            foreach (var obj in configAtual.ligar)
            {
                if (obj != null) obj.SetActive(true);
            }
        }
    }
}
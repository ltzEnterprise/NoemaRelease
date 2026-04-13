using UnityEngine;
using TMPro; 
using System.Collections;

public class LadderSystem : MonoBehaviour
{
    [Header("--- BLOQUEADOR ---")]
    [Tooltip("Coloque o objeto que bloqueia (ex: plasma). Se ficar vazio, funciona normal.")]
    public GameObject bloqueador;

    [Header("Configuração")]
    public GameObject visualDaEscada; 
    public int idItemEscada = 3; 
    public bool jaFoiColocada = false; 

    [Header("Colisores da Escada")]
    [Tooltip("Collider da parte de cima da escada")]
    public Collider colliderCima;
    [Tooltip("Collider da parte de baixo da escada")]
    public Collider colliderBaixo;

    [Header("Teleporte")]
    public Transform pontoBaixo; // Onde o player pisa ao descer
    public Transform pontoCima;  // Onde o player pisa ao subir

    [Header("UI")]
    public TextMeshProUGUI textoDeInteracao; // Canvas WorldSpace ou ScreenSpace

    // Função que checa em tempo real se a parada tá bloqueada
    private bool TaBloqueado()
    {
        return bloqueador != null && bloqueador.activeInHierarchy;
    }

    void Start()
    {
        AtualizarEstadoVisual();
        if (textoDeInteracao) textoDeInteracao.gameObject.SetActive(false);
    }

    void Update()
    {
        // Monitoramento constante: Se bloqueou enquanto olhava, desliga a UI
        if (TaBloqueado() && textoDeInteracao != null && textoDeInteracao.gameObject.activeSelf)
        {
            textoDeInteracao.gameObject.SetActive(false);
        }
    }

    void AtualizarEstadoVisual()
    {
        if (visualDaEscada) visualDaEscada.SetActive(jaFoiColocada);
        
        // Liga o collider de cima SOMENTE se a escada já foi colocada
        if (colliderCima) colliderCima.enabled = jaFoiColocada;
        
        // O collider de baixo sempre precisa estar ligado para o player clicar e "Colocar a escada"
        // Então garantimos que ele tá ativo
        if (colliderBaixo) colliderBaixo.enabled = true;
    }

    // --- CHAMADO PELOS TRIGGERS ---

    public void NotificarOlhar(bool olhandoParaCima)
    {
        if (TaBloqueado()) return; // Morre aqui se tiver bloqueado

        if (textoDeInteracao == null) return;

        textoDeInteracao.gameObject.SetActive(true);

        if (!jaFoiColocada)
        {
            // Só mostra opção de colocar se olhar para a base
            if (!olhandoParaCima) 
            {
                // Verifica qual a língua atual
                int lang = (LanguageManager.Instance != null) ? LanguageManager.Instance.currentLanguage : 0;
                bool temItem = InventoryManager.Instance != null && InventoryManager.Instance.itemSelecionado == idItemEscada;
                
                if (temItem)
                {
                    textoDeInteracao.text = (lang == 0) ? "[E] Colocar Escada" : "[E] Place Ladder";
                    textoDeInteracao.color = Color.white;
                }
                else
                {
                    textoDeInteracao.text = (lang == 0) ? "Preciso de uma escada..." : "I need a ladder...";
                    textoDeInteracao.color = Color.red;
                }
            }
            else
            {
                textoDeInteracao.gameObject.SetActive(false);
            }
        }
        else
        {
            // Escada já existe, mostra subir/descer com tradução embutida
            int lang = (LanguageManager.Instance != null) ? LanguageManager.Instance.currentLanguage : 0;
            
            if (olhandoParaCima)
            {
                textoDeInteracao.text = (lang == 0) ? "[E] Descer" : "[E] Go Down";
            }
            else
            {
                textoDeInteracao.text = (lang == 0) ? "[E] Subir" : "[E] Go Up";
            }
            
            textoDeInteracao.color = Color.white;
        }
    }

    public void NotificarSair()
    {
        if (textoDeInteracao) textoDeInteracao.gameObject.SetActive(false);
    }

    public void NotificarInteracao(bool clicouEmCima)
    {
        if (TaBloqueado()) return; // Foda-se o clique se tiver bloqueado

        if (!jaFoiColocada)
        {
            // Tenta colocar a escada (só se clicou na base)
            if (!clicouEmCima) TentarColocarEscada();
        }
        else
        {
            // Usa a escada
            UsarEscada(clicouEmCima);
        }
    }

    // --- LÓGICA ---

    void TentarColocarEscada()
    {
        if (InventoryManager.Instance != null && InventoryManager.Instance.itemSelecionado == idItemEscada)
        {
            InventoryManager.Instance.ConsumirItem(idItemEscada);
            jaFoiColocada = true;
            AtualizarEstadoVisual(); // Isso agora também vai ligar o collider de cima!
            
            // Atualiza texto imediato
            if (textoDeInteracao) textoDeInteracao.gameObject.SetActive(false);
        }
    }

    void UsarEscada(bool clicouEmCima)
    {
        // Se clicou em cima, quer descer (vai pro ponto baixo).
        // Se clicou embaixo, quer subir (vai pro ponto cima).
        Transform destino = clicouEmCima ? pontoBaixo : pontoCima;
        
        if (FPS_Master.Instance != null)
        {
            FPS_Master.Instance.Teleportar(destino.position);
            
            // Opcional: Rotacionar player para olhar pra frente
            FPS_Master.Instance.transform.rotation = destino.rotation;
            Physics.SyncTransforms();
        }
    }
}
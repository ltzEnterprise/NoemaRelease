using UnityEngine;
using TMPro; 
using System.Collections;

public class LadderSystem : MonoBehaviour
{
    [Header("Configuração")]
    public GameObject visualDaEscada; 
    public int idItemEscada = 3; 
    public bool jaFoiColocada = false; 

    [Header("Teleporte")]
    public Transform pontoBaixo; // Onde o player pisa ao descer
    public Transform pontoCima;  // Onde o player pisa ao subir

    [Header("UI")]
    public TextMeshProUGUI textoDeInteracao; // Canvas WorldSpace ou ScreenSpace

    void Start()
    {
        AtualizarEstadoVisual();
        if (textoDeInteracao) textoDeInteracao.gameObject.SetActive(false);
    }

    void AtualizarEstadoVisual()
    {
        if (visualDaEscada) visualDaEscada.SetActive(jaFoiColocada);
        
        // Se a escada não foi colocada, os colisores de subir/descer devem estar desligados?
        // Depende. Se você quer que o player clique no "nada" pra colocar, deixe ligado.
        // Se você tem um colisor específico "Base" para colocar, configure aqui.
    }

    // --- CHAMADO PELOS TRIGGERS ---

    public void NotificarOlhar(bool olhandoParaCima)
    {
        if (textoDeInteracao == null) return;

        textoDeInteracao.gameObject.SetActive(true);

        if (!jaFoiColocada)
        {
            // Só mostra opção de colocar se olhar para a base (opcional)
            if (!olhandoParaCima) 
            {
                bool temItem = InventoryManager.Instance != null && InventoryManager.Instance.itemSelecionado == idItemEscada;
                textoDeInteracao.text = temItem ? "[E] Colocar Escada" : "Preciso de uma escada...";
                textoDeInteracao.color = temItem ? Color.white : Color.red;
            }
            else
            {
                textoDeInteracao.gameObject.SetActive(false);
            }
        }
        else
        {
            // Escada já existe, mostra subir/descer
            textoDeInteracao.text = olhandoParaCima ? "[E] Descer" : "[E] Subir";
            textoDeInteracao.color = Color.white;
        }
    }

    public void NotificarSair()
    {
        if (textoDeInteracao) textoDeInteracao.gameObject.SetActive(false);
    }

    public void NotificarInteracao(bool clicouEmCima)
    {
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
            AtualizarEstadoVisual();
            
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
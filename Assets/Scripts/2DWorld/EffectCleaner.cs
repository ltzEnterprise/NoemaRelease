using UnityEngine;
using UnityEngine.Tilemaps;

public class EffectCleaner : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Tag dos blocos fantasma")]
    public string tagDosBlocos = "GhostBlock"; 

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            // 1. CANCELA A LEVITAÇÃO DO PLAYER
            PlayerMovement2D player = collision.GetComponent<PlayerMovement2D>();
            if (player != null)
            {
                player.CancelarEfeitos();
            }

            // 2. FORÇA OS BLOCOS E DESCOBRE SE ALGO FOI RESETADO
            bool tinhaEfeitoPraLimpar = ForcarBlocosSolidos();
            
            // 3. SE LIMPOU, CHAMA O MANAGER PRA TOCAR O SOM
            if (tinhaEfeitoPraLimpar)
            {
                if (ManagerMundo2D.Instance != null)
                {
                    ManagerMundo2D.Instance.TocarSomResetAgua();
                    Debug.Log("Efeitos removidos! Manager tocou o som.");
                }
            }
        }
    }

    bool ForcarBlocosSolidos()
    {
        GameObject[] blocos = GameObject.FindGameObjectsWithTag(tagDosBlocos);
        if (blocos.Length == 0) return false;

        bool resetouAlgo = false;

        foreach (GameObject obj in blocos)
        {
            Collider2D col = obj.GetComponent<Collider2D>();
            if (col != null)
            {
                if (col.enabled == false) 
                {
                    resetouAlgo = true; 
                }
                col.enabled = true;
            }

            Tilemap tilemap = obj.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                Color c = tilemap.color;
                c.a = 1f;
                tilemap.color = c;
            }
            
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 1f;
                sr.color = c;
            }
        }

        return resetouAlgo;
    }
}
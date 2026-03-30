using UnityEngine;
using UnityEngine.Tilemaps; // Necessário para mexer em Tilemaps
using System.Collections;

public class MagicWaterPhase : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("A Tag do Tilemap ou dos blocos")]
    public string tagDosBlocos = "GhostBlock"; 

    [Tooltip("Coloque valor entre 0 e 255 (Ex: 150)")]
    [Range(0, 255)] 
    public float opacidadeAlvo = 150f; // <--- AGORA É 0 A 255

    public float tempoDeEfeito = 3.0f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            StopAllCoroutines();
            StartCoroutine(AtivarModoFantasmaTemporario());
        }
    }

    IEnumerator AtivarModoFantasmaTemporario()
    {
        MudarEstadoDosBlocos(true);
        Debug.Log($"👻 Modo Fantasma! Opacidade definida para {opacidadeAlvo}/255");

        yield return new WaitForSeconds(tempoDeEfeito);

        MudarEstadoDosBlocos(false);
        Debug.Log("🧱 Efeito acabou.");
    }

    void MudarEstadoDosBlocos(bool modoFantasma)
    {
        GameObject[] blocos = GameObject.FindGameObjectsWithTag(tagDosBlocos);
        if (blocos.Length == 0) return;

        // Converte o seu 150 para o formato da Unity (0.58)
        float alpha = modoFantasma ? (opacidadeAlvo / 255f) : 1f;

        foreach (GameObject obj in blocos)
        {
            // 1. Tenta achar TILEMAP (Se for o cenário inteiro)
            Tilemap tilemap = obj.GetComponent<Tilemap>();
            if (tilemap != null)
            {
                // TilemapCollider2D geralmente é usado com Composite, então desligamos o colisor
                Collider2D col = obj.GetComponent<Collider2D>();
                if (col) col.enabled = !modoFantasma;

                Color c = tilemap.color;
                c.a = alpha;
                tilemap.color = c;
            }
            
            // 2. Tenta achar SPRITE (Se for bloco solto)
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Collider2D col = obj.GetComponent<Collider2D>();
                if (col) col.enabled = !modoFantasma;

                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }
}
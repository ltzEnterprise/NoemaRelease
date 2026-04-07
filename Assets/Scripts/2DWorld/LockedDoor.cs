using UnityEngine;

public class LockedDoor : MonoBehaviour
{
    public enum LadoLivre { Nenhum, Esquerda, Direita, Cima, Baixo }

    [Header("Configuração")]
    public string tagDaChave = "Key";
    public bool abrirAoEncostarPlayer = false; 

    [Header("Atalho (Lado que abre sem chave)")]
    public LadoLivre ladoQueAbreSemChave = LadoLivre.Nenhum;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        VerificarAbertura(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        VerificarAbertura(collision.gameObject);
    }

    void VerificarAbertura(GameObject objetoQueBateu)
    {
        // 1. Lógica da Chave (Normal)
        if (objetoQueBateu.CompareTag("Key"))
        {
            AbrirPorta();
            Destroy(objetoQueBateu); 
            return;
        }

        // 2. Lógica do Player (Verifica Atalho)
        if (objetoQueBateu.CompareTag("Player"))
        {
            // Se estiver marcado para abrir ao encostar (comum)
            if (abrirAoEncostarPlayer)
            {
                AbrirPorta();
                return;
            }

            // --- LÓGICA DO LADO OPOSTO ---
            if (ladoQueAbreSemChave != LadoLivre.Nenhum)
            {
                if (ChecarSeVeioDoLadoCerto(objetoQueBateu.transform.position))
                {
                    Debug.Log("🔓 Atalho! Veio do lado livre.");
                    AbrirPorta();
                }
            }
        }
    }

    bool ChecarSeVeioDoLadoCerto(Vector3 posPlayer)
    {
        Vector3 posPorta = transform.position;
        float margem = 0.2f; // Tolerância

        switch (ladoQueAbreSemChave)
        {
            case LadoLivre.Esquerda: // Player está na esquerda da porta (X menor)
                return posPlayer.x < posPorta.x - margem;
            
            case LadoLivre.Direita: // Player está na direita da porta (X maior)
                return posPlayer.x > posPorta.x + margem;

            case LadoLivre.Cima: // Player está acima
                return posPlayer.y > posPorta.y + margem;

            case LadoLivre.Baixo: // Player está abaixo
                return posPlayer.y < posPorta.y - margem;
        }
        return false;
    }

    public void AbrirPorta()
    {
        Debug.Log("PORTA ABERTA!");
        gameObject.SetActive(false); 
    }
}
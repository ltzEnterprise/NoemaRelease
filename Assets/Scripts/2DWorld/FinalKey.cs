using UnityEngine;

public class FinalKey : MonoBehaviour
{
    public AudioClip somPegar;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (somPegar)
                AudioSource.PlayClipAtPoint(somPegar, transform.position);

            // Correção: Chama o ManagerMundo2D (Maiúsculo)
            if (ManagerMundo2D.Instance != null)
            {
                ManagerMundo2D.Instance.PegarChaveFinal();
            }

            Destroy(gameObject);
        }
    }
}
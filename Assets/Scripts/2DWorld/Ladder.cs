using UnityEngine;

public class Ladder : MonoBehaviour
{
    // Coloque este script no objeto da Escada (que tem o BoxCollider2D Trigger)

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Envia uma mensagem "cega". Se o objeto tiver a função EnterLadder, ela roda.
        collision.SendMessage("EnterLadder", SendMessageOptions.DontRequireReceiver);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        collision.SendMessage("ExitLadder", SendMessageOptions.DontRequireReceiver);
    }
}
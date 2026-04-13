using UnityEngine;

public class DevColliderScanner : MonoBehaviour
{
    [Header("--- FILTRO ---")]
    [Tooltip("Coloque aqui a Tag do seu chão (Ex: 'Chao') para ele ignorar e não flodar o console.")]
    public string tagParaIgnorar = "Chao";

    // 1. SE O SEU PLAYER USA CHARACTER CONTROLLER (O motivo de não ter funcionado antes)
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        RastrearHierarquia(hit.gameObject);
    }

    // 2. SE ESBARRAR NUM TRIGGER INVISÍVEL
    void OnTriggerEnter(Collider other)
    {
        RastrearHierarquia(other.gameObject);
    }

    // 3. SE O SEU PLAYER USAR UM RIGIDBODY NORMAL
    void OnCollisionEnter(Collision collision)
    {
        RastrearHierarquia(collision.gameObject);
    }

    void RastrearHierarquia(GameObject objetoTocado)
    {
        // Ignora o chão pra não ficar spamando a cada passo
        if (objetoTocado.CompareTag(tagParaIgnorar)) return;

        // Começa com o nome do objeto que bateu
        string caminhoCompleto = objetoTocado.name;
        Transform paiAtual = objetoTocado.transform.parent;

        // Vai subindo na hierarquia até achar a raiz (quem não tem pai)
        while (paiAtual != null)
        {
            caminhoCompleto = paiAtual.name + " -> " + caminhoCompleto;
            paiAtual = paiAtual.parent;
        }

        // Imprime bonitinho e colorido no Console
        Debug.Log($"[DEV SCANNER] O Player esbarrou em: <color=yellow>{caminhoCompleto}</color>");
    }
}
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Player")]
    public Transform player; 

    [Header("Ajustes")]
    [Range(0, 1)] public float suavidade = 0.125f; 
    public Vector3 offset = new Vector3(0, 0, -10); 

    void Start()
    {
        if (player != null)
        {
            transform.position = player.position + offset;
        }
    }

    void FixedUpdate() 
    {
        if (player == null) return;
        Vector3 posicaoDesejada = player.position + offset;
        Vector3 posicaoSuavizada = Vector3.Lerp(transform.position, posicaoDesejada, suavidade);
        transform.position = posicaoSuavizada;

    }
}
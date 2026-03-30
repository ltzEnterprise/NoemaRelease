using UnityEngine;

public class FloatingCrank : MonoBehaviour
{
    [Header("Animation Settings")]
    public float rotationSpeed = 50f;
    public float bobbingAmplitude = 0.2f;
    public float bobbingSpeed = 2f;

    private Vector3 startPosition;

    void Start()
    {
        startPosition = transform.position;
    }

    void Update()
    {
        // Gira no próprio eixo (Eixo Y)
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

        // Sobe e desce suavemente (Eixo Y) usando a função Seno
        float newY = startPosition.y + Mathf.Sin(Time.time * bobbingSpeed) * bobbingAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
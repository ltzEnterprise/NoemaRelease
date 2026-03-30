using UnityEngine;
using System.Collections;

public class ReactiveTarget : MonoBehaviour
{
    [Header("What does it open?")]
    public Transform plasmaDoor; // ARRASTE O OBJETO DA PORTA AQUI

    [Header("Target Settings")]
    public float fallAngle = 90f;
    public float fallSpeed = 5f;

    [Header("Door Settings")]
    public float doorSpeed = 3f; // Quão rápido ela fecha
    
    private bool hasBeenHit = false;
    private Quaternion initialRotation;
    private Quaternion finalRotation;
    private Vector3 originalDoorScale; // Guarda a escala original da porta

    void Start()
    {
        initialRotation = transform.rotation;
        // Calcula o tombo (se girar errado, mude Vector3.right para Vector3.forward)
        finalRotation = initialRotation * Quaternion.Euler(Vector3.right * fallAngle);

        if (plasmaDoor != null)
        {
            originalDoorScale = plasmaDoor.localScale;
        }
    }

    public void TargetHit() // Antigo FoiAtingido()
    {
        if (hasBeenHit) return;
        hasBeenHit = true;

        Debug.Log("Target Hit!");
        
        // 1. Tomba o alvo
        StartCoroutine(FallTarget());

        // 2. Fecha a porta com animação
        if(plasmaDoor != null)
        {
            StartCoroutine(CloseDoorSmoothly());
        }
    }

    IEnumerator FallTarget()
    {
        float time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime * fallSpeed;
            transform.rotation = Quaternion.Slerp(initialRotation, finalRotation, time);
            yield return null;
        }
    }

    IEnumerator CloseDoorSmoothly()
    {
        float time = 0f;

        // Vai diminuindo a escala X (largura) até zerar
        while (time < 1f)
        {
            time += Time.deltaTime * doorSpeed;
            
            // "Lerp" vai do valor original até 0
            float newScaleX = Mathf.Lerp(originalDoorScale.x, 0, time);
            
            // Aplica a nova escala (mantém Y e Z iguais, só espreme o X)
            plasmaDoor.localScale = new Vector3(newScaleX, originalDoorScale.y, originalDoorScale.z);
            
            yield return null;
        }

        // Garante que sumiu de vez no final e restaura a escala para não dar pau de noite
        plasmaDoor.gameObject.SetActive(false);
        plasmaDoor.localScale = originalDoorScale; 
    }
}
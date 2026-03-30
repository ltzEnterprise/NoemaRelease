using UnityEngine;
using UnityEngine.UI;

public class PlayerInteracao : MonoBehaviour
{
    [Header("Ajustes")]
    public float distanciaInteracao = 2.5f; 
    public float tempoDeTolerancia = 0.15f; 
    public LayerMask camadasInteracao; 
    public Image miraUI;

    private Transform ultimoObjeto;
    private float timerDesaparecer = 0f;

    void Update()
    {
        Ray raio = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        bool detectou = Physics.Raycast(raio, out hit, distanciaInteracao, camadasInteracao, QueryTriggerInteraction.Ignore);

        if (detectou)
        {
            Transform objAtual = hit.transform;
            timerDesaparecer = tempoDeTolerancia;

            if (objAtual != ultimoObjeto)
            {
                bool ehParente = (ultimoObjeto != null) && (objAtual.IsChildOf(ultimoObjeto) || ultimoObjeto.IsChildOf(objAtual));

                if (!ehParente)
                {
                    if (ultimoObjeto != null) 
                        ultimoObjeto.SendMessageUpwards("AoSair", SendMessageOptions.DontRequireReceiver);
                    
                    objAtual.SendMessageUpwards("AoOlhar", SendMessageOptions.DontRequireReceiver);
                    ultimoObjeto = objAtual;
                }
                else
                {
                    ultimoObjeto = objAtual; 
                }
            }

            if (miraUI) miraUI.color = Color.red;

            if (Input.GetKeyDown(KeyCode.E))
            {
                objAtual.SendMessageUpwards("Interagir", SendMessageOptions.DontRequireReceiver);
            }
        }
        else
        {
            if (timerDesaparecer > 0)
            {
                timerDesaparecer -= Time.deltaTime;
            }
            else
            {
                LimparVisual(); 
            }
        }
    }

    // --- A FUNÇÃO QUE FALTAVA ---
    public void LimparVisual()
    {
        if (ultimoObjeto != null)
        {
            ultimoObjeto.SendMessageUpwards("AoSair", SendMessageOptions.DontRequireReceiver);
            ultimoObjeto = null;
        }
        if (miraUI) miraUI.color = Color.white;
    }
}
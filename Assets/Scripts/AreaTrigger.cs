using UnityEngine;

public class AreaTrigger : MonoBehaviour
{
    private BoxCollider detector;
    public string thisArea;
    private SoundtrackManager soundtrackManager;

    void Start()
    {
        detector = GetComponent<BoxCollider>();
        // Garante que é Trigger para não bater no player como parede
        if (detector) detector.isTrigger = true; 
        
        // Busca o gerenciador de som na cena
        GameObject sm = GameObject.Find("SoundtrackManager");
        if (sm) soundtrackManager = sm.GetComponent<SoundtrackManager>();
    }

    private void OnTriggerEnter(Collider other) // Mudei pra Enter, costuma ser melhor pra música
    {
        if (other.CompareTag("Player") && soundtrackManager != null)
        {
            soundtrackManager.SwitchSoundtrack(thisArea);
        }
    }
}
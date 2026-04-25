using UnityEngine;

public class AreaTrigger : MonoBehaviour
{
    [Tooltip("Qual música deve tocar quando o jogador bater neste colisor?")]
    public string nomeDestaArea;

    private SoundtrackManager manager;

    void Start()
    {
        var detector = GetComponent<BoxCollider>();
        if (detector) detector.isTrigger = true; 
        
        GameObject sm = GameObject.Find("SoundtrackManager");
        if (sm) manager = sm.GetComponent<SoundtrackManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Bateu no colisor da porta, manda o Cérebro mudar pra essa música. Foda-se o Exit.
        if (other.CompareTag("Player") && manager != null)
        {
            manager.SwitchSoundtrack(nomeDestaArea);
        }
    }
}
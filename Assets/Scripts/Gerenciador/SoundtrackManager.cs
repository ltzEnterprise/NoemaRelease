using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundtrackManager : MonoBehaviour
{
    [Header("Configurações de transição")]
    public float fadeTime;
    public float interval;

    [Header("Configurações de áudio")]
    public float volume;

    [Header("Áudios e labels")]
    [SerializeField] private List<AudioClip> soundtracks;
    [SerializeField] private List<string> labels;
    [SerializeField] private AudioClip starterSoundtrack;

    private AudioSource audioSource;
    private float fadeSpan;

    void Start()
    {
        // SEGURANÇA: Tenta achar o objeto. Se não achar, não quebra.
        GameObject stObj = GameObject.Find("Soundtrack");
        if (stObj != null)
        {
            audioSource = stObj.GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.clip = starterSoundtrack;
            audioSource.loop = true;
            audioSource.volume = volume;
            audioSource.Play();
            fadeSpan = (fadeTime - interval)/2;
        }
        else
        {
            Debug.LogWarning("[SoundtrackManager] O objeto 'Soundtrack' foi desativado ou não existe. Música ignorada.");
        }
    }

    public void SwitchSoundtrack(string name)
    {
        // Se o áudio estiver desligado, aborta a função (não dá erro)
        if (audioSource == null || !audioSource.gameObject.activeInHierarchy) return;

        int index = labels.IndexOf(name);
        if (index == -1) return; // Segurança extra se a música não existir

        float targetVolume = volume;
        StopAllCoroutines(); // Para não bugar se tocar duas vezes seguidas
        StartCoroutine(FadeVolume(targetVolume, index, interval));
    }

    private IEnumerator FadeVolume(float target, int index, float interval)
    {
        float starterVolume = volume;
        
        // Fade Out
        while (audioSource.volume >= 0.0005f)
        {
            audioSource.volume -= (starterVolume / fadeSpan) * Time.deltaTime;
            yield return null; // Time.deltaTime aqui estava errado, o correto é null ou WaitForEndOfFrame
        }
        
        audioSource.volume = 0;
        yield return new WaitForSeconds(interval);
        
        // Troca a música
        audioSource.clip = soundtracks[index];
        audioSource.Play();
        
        // Fade In
        while (audioSource.volume <= target)
        {
            audioSource.volume += (target / fadeSpan) * Time.deltaTime;
            yield return null;
        }
        
        audioSource.volume = target;
    }
}
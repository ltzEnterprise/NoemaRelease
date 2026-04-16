using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AmbientAudioFader : MonoBehaviour
{
    [Header("--- CONFIGURAÇÃO DE FADE ---")]
    [Range(0f, 1f)]
    public float maxVolume = 0.5f;
    
    [Tooltip("Tempo exato de duração do fade (1 segundo como você pediu)")]
    public float fadeDuration = 1.0f;

    [Header("--- REFERÊNCIAS ---")]
    public AudioSource audioSource;

    private bool isQuitting = false;

    void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f; 
    }

    void Start()
    {
        PlayWithFadeIn();
    }

    public void PlayWithFadeIn()
    {
        if (!audioSource.isPlaying) 
            audioSource.Play();
        
        StopAllCoroutines();
        StartCoroutine(FadeVolumeRoutine(maxVolume, false));
    }

    // Chama isso pra parar a música com 1 segundo de fade out
    public void StopWithFadeOut()
    {
        StopAllCoroutines();
        StartCoroutine(FadeVolumeRoutine(0f, true));
    }

    private IEnumerator FadeVolumeRoutine(float targetVolume, bool stopAtEnd)
    {
        float currentTime = 0f;
        float startVolume = audioSource.volume;

        while (currentTime < fadeDuration)
        {
            currentTime += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, currentTime / fadeDuration);
            yield return null;
        }

        audioSource.volume = targetVolume;

        if (stopAtEnd)
            audioSource.Stop();
    }
    
    private void OnDestroy()
    {
        if (isQuitting) return;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.volume = 0f;
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }
}
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AmbientAudioFader : MonoBehaviour
{
    [Header("--- FADE SETTINGS ---")]
    [Range(0f, 1f)]
    public float maxVolume = 0.5f;
    
    [Tooltip("Duration for Fade In (Start) and Fade Out (Scene Change)")]
    public float fadeDuration = 0.5f;

    [Header("--- REFERENCES ---")]
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

    // Call this if you want to stop it manually during gameplay
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

    // --- DETECTION FOR SCENE CHANGE ---
    
    private void OnDestroy()
    {
        // Check if the application is actually closing to avoid errors in the Editor
        if (isQuitting) return;

        // When the scene changes and this object is destroyed, we try a quick fade out.
        // Note: This only works effectively if the scene transition isn't 100% instant.
        if (audioSource != null && audioSource.isPlaying)
        {
            // We can't use Coroutines here because the object is dying, 
            // but we can set the volume to 0 to prevent the "pop" sound.
            audioSource.volume = 0f;
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }
}
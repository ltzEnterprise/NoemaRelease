using UnityEngine;
using UnityEngine.Video;

public class VideoStart : MonoBehaviour
{
    public VideoPlayer video;
    public CanvasGroup fade;

    void OnEnable()
    {
        StopAllCoroutines(); // Impede que o script rode duas vezes ao mesmo tempo
        
        fade.alpha = 1f;
        video.playOnAwake = false;
        video.targetCameraAlpha = 0f;

        // O segredo: Parar totalmente o vídeo antes de mandar preparar/tocar de novo
        video.Stop(); 
        video.Prepare();
        
        StartCoroutine(WaitForVideo());
    }

    System.Collections.IEnumerator WaitForVideo()
    {
        while (!video.isPrepared)
            yield return null;

        video.Play();

        // Garante que o vídeo realmente começou a cuspir frames
        while (video.isPlaying == false || video.frame <= 0)
            yield return null;

        video.targetCameraAlpha = 1f; 
        StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeOut()
    {
        float t = 0f;
        float duration = 0.5f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime; // Usa unscaled pra não bugar se o tempo tiver pausado
            fade.alpha = 1f - (t / duration);
            yield return null;
        }

        fade.alpha = 0f;
    }
}
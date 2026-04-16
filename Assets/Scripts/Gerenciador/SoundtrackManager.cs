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

    [Header("--- CONTROLE DE VOLUME INDIVIDUAL ---")]
    [Tooltip("Multiplicador para a música de INÍCIO. 1 = Volume padrão. 0.5 = Metade. 2 = Dobro.")]
    public float starterVolumeMultiplier = 1f;
    
    [Tooltip("Multiplicadores para a lista de músicas acima. A ordem tem que ser a MESMA da lista de áudios. Deixe em 1 para manter o volume padrão.")]
    [SerializeField] private List<float> volumeMultipliers = new List<float>();

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
            
            // Aplica o multiplicador na música inicial (Padrão 1 * volume normal)
            audioSource.volume = volume * starterVolumeMultiplier;
            
            audioSource.Play();
            
            // CORREÇÃO DE ERRO FATAL: 
            // Se o fadeTime e o interval fossem iguais, fadeSpan virava 0.
            // Dividir por zero na Unity crasava o jogo. O Mathf.Max impede que chegue a zero.
            fadeSpan = Mathf.Max(0.1f, (fadeTime - interval) / 2);
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

        // Pega o volume global como base
        float targetVolume = volume;
        
        // Se você adicionou um multiplicador pra essa música específica, ele aplica aqui
        if (volumeMultipliers != null && index < volumeMultipliers.Count)
        {
            targetVolume = volume * volumeMultipliers[index];
        }

        StopAllCoroutines(); // Para não bugar se tocar duas vezes seguidas
        StartCoroutine(FadeVolume(targetVolume, index, interval));
    }

    private IEnumerator FadeVolume(float target, int index, float interval)
    {
        float starterVolume = audioSource.volume; // Pega o volume atual real da source
        
        // Fade Out
        while (audioSource.volume >= 0.0005f)
        {
            audioSource.volume -= (starterVolume / fadeSpan) * Time.deltaTime;
            yield return null; 
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
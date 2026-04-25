using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ConfigMusica
{
    [Header("Identificação")]
    public string nomeArea;
    public AudioClip audioClip;
    [Tooltip("Volume individual. 1 = Volume global. 0.5 = Metade.")]
    [Range(0f, 2f)] public float multiplicadorVolume = 1f;

    [Header("--- SAÍDA DA ÁREA (Transição) ---")]
    public float tempoContinuarTocando = 0f;
    public float fadeOutNormal = 2f;
    [Tooltip("IGNORADO DURANTE A TROCA. Agora as músicas se emendam no Crossfade. Só usado se você for pro vazio.")]
    public float intervaloSemMusica = 1f;

    [Header("--- ENTRADA NA ÁREA ---")]
    public float fadeInTime = 2f;

    [Header("--- FINAL NATURAL DA MÚSICA (Loop) ---")]
    public float fadeOutFimDeMusica = 7f;
    public float silencioFimDeMusica = 20f;

    [Header("--- REGRAS DE HIERARQUIA ---")]
    public bool temQueTerminarAntesDeTrocar = false;
    public bool ePrioridade = false;
    public float fadeOutForcadoPrioridade = 1f;

    // --- CONTROLE INTERNO (NÃO MEXER) ---
    [HideInInspector] public float tempoSalvoNoMinuto = 0f;
    [HideInInspector] public bool jaTocouAPrimeiraVez = false;
}

public class SoundtrackManager : MonoBehaviour
{
    [Header("Configuração Global")]
    [Range(0f, 1f)] public float volumeGlobal = 1f;
    public string nomeMusicaInicial;

    [Header("O Maestro (Configure cada música aqui)")]
    public List<ConfigMusica> listaDeMusicas = new List<ConfigMusica>();

    // As duas caixas de som que fazem a mágica do Crossfade
    private AudioSource audioSource1;
    private AudioSource audioSource2;
    private AudioSource sourcePrincipal;

    private ConfigMusica musicaAtual;
    private ConfigMusica musicaAlvo;
    private Coroutine maestroRoutine;

    private Dictionary<string, bool> deveEsperarNestaVez = new Dictionary<string, bool>();

    private Coroutine fadeRoutine1;
    private Coroutine fadeRoutine2;

    void Start()
    {
        // Acha a sua caixa de som original
        audioSource1 = GetComponent<AudioSource>();
        if (audioSource1 == null)
        {
            GameObject stObj = GameObject.Find("Soundtrack");
            if (stObj != null) audioSource1 = stObj.GetComponent<AudioSource>();
        }

        if (audioSource1 != null)
        {
            // CRIA A SEGUNDA CAIXA DE SOM SOZINHO (Invisível) PRA NÃO TE DAR TRABALHO
            audioSource2 = audioSource1.gameObject.AddComponent<AudioSource>();
            
            audioSource1.loop = false;
            audioSource2.loop = false;
            sourcePrincipal = audioSource1;

            if (!string.IsNullOrEmpty(nomeMusicaInicial)) SwitchSoundtrack(nomeMusicaInicial);
            maestroRoutine = StartCoroutine(CerebroAudioDinamico());
        }
    }

    public void SwitchSoundtrack(string nomeArea)
    {
        if (audioSource1 == null) return;
        
        if (string.IsNullOrEmpty(nomeArea)) 
        {
            musicaAlvo = null; 
            return;
        }

        ConfigMusica nova = listaDeMusicas.Find(x => x.nomeArea == nomeArea);
        if (nova != null) musicaAlvo = nova;
    }

    private IEnumerator CerebroAudioDinamico()
    {
        while (true)
        {
            // 1. INÍCIO SECO OU VOLTA DO VAZIO
            if (musicaAtual == null && musicaAlvo != null)
            {
                musicaAtual = musicaAlvo;
                sourcePrincipal.clip = musicaAtual.audioClip;
                sourcePrincipal.time = musicaAtual.tempoSalvoNoMinuto;
                sourcePrincipal.Play();

                float target = volumeGlobal * musicaAtual.multiplicadorVolume;
                if (musicaAtual.jaTocouAPrimeiraVez) yield return IniciarFade(sourcePrincipal, target, musicaAtual.fadeInTime, false, null);
                else { sourcePrincipal.volume = target; musicaAtual.jaTocouAPrimeiraVez = true; }
            }

            if (musicaAtual != null)
            {
                while (true)
                {
                    // A) REGRA DE PRIORIDADE (Esmaga com Crossfade agressivo)
                    if (musicaAlvo != musicaAtual && musicaAlvo != null && musicaAlvo.ePrioridade)
                    {
                        ExecutarCrossfade(musicaAtual, musicaAlvo, musicaAlvo.fadeOutForcadoPrioridade);
                        break;
                    }

                    bool esperaAtiva = musicaAtual.temQueTerminarAntesDeTrocar && ChecarAlternancia(musicaAtual.nomeArea);

                    // B) TRANSIÇÃO DE ÁREAS (O Efeito Foda de Crossfade)
                    if (musicaAlvo != musicaAtual && musicaAlvo != null && !esperaAtiva)
                    {
                        yield return new WaitForSeconds(musicaAtual.tempoContinuarTocando);
                        if (musicaAlvo == musicaAtual) continue; // Abortou

                        if (musicaAtual.temQueTerminarAntesDeTrocar) InverterAlternancia(musicaAtual.nomeArea);

                        // Manda descer uma e subir a outra simultaneamente! Efeito contínuo.
                        ExecutarCrossfade(musicaAtual, musicaAlvo, musicaAtual.fadeOutNormal);
                        break;
                    }

                    // C) SE SAIU PRO VAZIO (Nenhuma música em volta)
                    if (musicaAlvo == null && !esperaAtiva)
                    {
                        yield return new WaitForSeconds(musicaAtual.tempoContinuarTocando);
                        if (musicaAlvo != null) continue; // Desistiu e voltou

                        if (musicaAtual.temQueTerminarAntesDeTrocar) InverterAlternancia(musicaAtual.nomeArea);

                        yield return IniciarFade(sourcePrincipal, 0f, musicaAtual.fadeOutNormal, true, musicaAtual);
                        
                        float tempoEsperaVazio = musicaAtual.intervaloSemMusica;
                        musicaAtual = null;
                        yield return new WaitForSeconds(tempoEsperaVazio);
                        break;
                    }

                    // D) FIM NATURAL E LOOPING
                    if (sourcePrincipal.isPlaying && musicaAtual.audioClip != null)
                    {
                        float restante = musicaAtual.audioClip.length - sourcePrincipal.time;
                        if (restante <= musicaAtual.fadeOutFimDeMusica)
                        {
                            yield return IniciarFade(sourcePrincipal, 0f, musicaAtual.fadeOutFimDeMusica, true, musicaAtual);
                            musicaAtual.tempoSalvoNoMinuto = 0f;

                            if (musicaAlvo != musicaAtual && musicaAlvo != null)
                            {
                                if (musicaAtual.temQueTerminarAntesDeTrocar) InverterAlternancia(musicaAtual.nomeArea);
                                musicaAtual = null;
                                break;
                            }

                            // O Silêncio só existe aqui no Loop agora!
                            float tempoSilencioFim = musicaAtual.silencioFimDeMusica;
                            musicaAtual = null;
                            yield return new WaitForSeconds(tempoSilencioFim);
                            break;
                        }
                        musicaAtual.tempoSalvoNoMinuto = sourcePrincipal.time;
                    }

                    yield return null;
                }
            }
            yield return null;
        }
    }

    // A mágica da troca dupla acontece aqui
    private void ExecutarCrossfade(ConfigMusica antiga, ConfigMusica nova, float tempoFadeOutAntiga)
    {
        AudioSource sourceAntigo = sourcePrincipal;
        
        // Joga a chave. A fonte que tava de reserva vira a principal agora
        sourcePrincipal = (sourcePrincipal == audioSource1) ? audioSource2 : audioSource1;

        musicaAtual = nova;
        sourcePrincipal.clip = musicaAtual.audioClip;
        sourcePrincipal.time = musicaAtual.tempoSalvoNoMinuto;
        sourcePrincipal.Play();

        float targetVol = volumeGlobal * musicaAtual.multiplicadorVolume;

        // Manda apagar a música antiga
        IniciarFade(sourceAntigo, 0f, tempoFadeOutAntiga, true, antiga);

        // Enquanto ao mesmo tempo manda subir a música nova
        if (musicaAtual.jaTocouAPrimeiraVez) {
            sourcePrincipal.volume = 0f;
            IniciarFade(sourcePrincipal, targetVol, musicaAtual.fadeInTime, false, null);
        } else {
            sourcePrincipal.volume = targetVol; // Se for a primeira vez, toca de sola
            musicaAtual.jaTocouAPrimeiraVez = true;
        }
    }

    // Gerencia o fade individual de cada caixa de som
    private Coroutine IniciarFade(AudioSource src, float target, float duration, bool stop, ConfigMusica config)
    {
        if (src == audioSource1) {
            if (fadeRoutine1 != null) StopCoroutine(fadeRoutine1);
            fadeRoutine1 = StartCoroutine(FadeVolumeSource(src, target, duration, stop, config));
            return fadeRoutine1;
        } else {
            if (fadeRoutine2 != null) StopCoroutine(fadeRoutine2);
            fadeRoutine2 = StartCoroutine(FadeVolumeSource(src, target, duration, stop, config));
            return fadeRoutine2;
        }
    }

    private IEnumerator FadeVolumeSource(AudioSource source, float target, float duration, bool stopAtEnd, ConfigMusica configSalvarTempo)
    {
        float start = source.volume;
        float t = 0;
        
        if (duration <= 0.01f) {
            source.volume = target;
        } else {
            while (t < duration) {
                t += Time.deltaTime;
                source.volume = Mathf.Lerp(start, target, t / duration);
                yield return null;
            }
            source.volume = target;
        }

        if (stopAtEnd) {
            if (configSalvarTempo != null) configSalvarTempo.tempoSalvoNoMinuto = source.time;
            source.Stop();
        }
    }

    private bool ChecarAlternancia(string area)
    {
        if (!deveEsperarNestaVez.ContainsKey(area)) deveEsperarNestaVez[area] = true;
        return deveEsperarNestaVez[area];
    }

    private void InverterAlternancia(string area)
    {
        deveEsperarNestaVez[area] = !deveEsperarNestaVez[area];
    }
}
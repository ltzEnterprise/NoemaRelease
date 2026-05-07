using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[System.Serializable]
public class ConfigMusica
{
    [Header("Identificação")]
    public string nomeArea;
    public AudioClip audioClip;
    [Tooltip("Volume individual. 1 = Volume global. 0.5 = Metade.")]
    [Range(0f, 2f)] public float multiplicadorVolume = 1f;

    [Header("--- TIPO DE TRANSIÇÃO ---")]
    [Tooltip("ATIVADO: Mistura as músicas na troca.\nDESATIVADO: Desliga a atual, espera o Intervalo Sem Música, e liga a nova.")]
    public bool usarCrossfade = true; 

    [Header("--- SAÍDA DA ÁREA (Transição) ---")]
    public float tempoContinuarTocando = 0f;
    public float fadeOutNormal = 2f;
    [Tooltip("Tempo de silêncio antes da próxima começar (Só funciona se o Crossfade estiver DESATIVADO ou se for pro vazio).")]
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

    [Header("--- MODO ALTERNATIVO DE DISTRIBUIÇÃO ---")]
    [Tooltip("DESATIVADO: usa o sistema atual normal.\nATIVADO: a música atual precisa terminar antes da próxima começar. A próxima usa a área atual do jogador, mas nunca repete a mesma música em sequência se ainda estiver na mesma área.")]
    public bool usarModoSequencialPorFimDaMusica = false;

    [Tooltip("No modo sequencial, se a próxima música escolhida for a mesma que acabou de tocar, sorteia outra música da lista.")]
    public bool evitarMusicaIgualEmSequencia = true;

    [Tooltip("No modo sequencial, a primeira música também entra com fade in.")]
    public bool primeiraMusicaSequencialComFade = false;

    [Header("--- CONTROLE EXTERNO DE VOLUME ---")]
    [Tooltip("Volume do Soundtrack quando algum sistema externo precisa abafar a trilha, sem parar o manager.")]
    [Range(0f, 1f)] public float multiplicadorVolumeQuandoAbafado = 0f;

    private AudioSource audioSource1;
    private AudioSource audioSource2;
    private AudioSource sourcePrincipal;

    private ConfigMusica musicaAtual;
    private ConfigMusica musicaAlvo;
    private Coroutine maestroRoutine;

    private Dictionary<string, bool> deveEsperarNestaVez = new Dictionary<string, bool>();

    private Coroutine fadeRoutine1;
    private Coroutine fadeRoutine2;

    private Coroutine rotinaControleExterno;
    private float multiplicadorExternoAtual = 1f;
    private bool soundtrackAbafadoExternamente = false;

    private Dictionary<AudioSource, float> volumesLogicos = new Dictionary<AudioSource, float>();

    void Start()
    {
        audioSource1 = GetComponent<AudioSource>();

        if (audioSource1 == null)
        {
            GameObject stObj = GameObject.Find("Soundtrack");

            if (stObj != null)
                audioSource1 = stObj.GetComponent<AudioSource>();
        }

        if (audioSource1 != null)
        {
            audioSource2 = audioSource1.gameObject.AddComponent<AudioSource>();
            audioSource2.outputAudioMixerGroup = audioSource1.outputAudioMixerGroup;
            
            audioSource1.loop = false;
            audioSource2.loop = false;

            sourcePrincipal = audioSource1;

            RegistrarVolumeLogico(audioSource1, audioSource1.volume);
            RegistrarVolumeLogico(audioSource2, 0f);

            if (!string.IsNullOrEmpty(nomeMusicaInicial))
                SwitchSoundtrack(nomeMusicaInicial);

            if (usarModoSequencialPorFimDaMusica)
                maestroRoutine = StartCoroutine(CerebroAudioSequencial());
            else
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

        if (nova != null)
            musicaAlvo = nova;
    }

    public void SetSoundtrackAbafadoPorDisco(bool abafar, float tempoFade)
    {
        if (audioSource1 == null) return;

        if (soundtrackAbafadoExternamente == abafar)
            return;

        soundtrackAbafadoExternamente = abafar;

        float alvo = abafar ? multiplicadorVolumeQuandoAbafado : 1f;

        if (rotinaControleExterno != null)
            StopCoroutine(rotinaControleExterno);

        rotinaControleExterno = StartCoroutine(FadeMultiplicadorExterno(alvo, tempoFade));
    }

    private IEnumerator FadeMultiplicadorExterno(float alvo, float duracao)
    {
        float inicio = multiplicadorExternoAtual;
        float t = 0f;

        if (duracao <= 0.01f)
        {
            multiplicadorExternoAtual = alvo;
            AplicarVolumesFisicosAtuais();
            rotinaControleExterno = null;
            yield break;
        }

        while (t < duracao)
        {
            t += Time.deltaTime;
            multiplicadorExternoAtual = Mathf.Lerp(inicio, alvo, t / duracao);
            AplicarVolumesFisicosAtuais();
            yield return null;
        }

        multiplicadorExternoAtual = alvo;
        AplicarVolumesFisicosAtuais();

        rotinaControleExterno = null;
    }

    private void AplicarVolumesFisicosAtuais()
    {
        AplicarVolumeFisico(audioSource1);
        AplicarVolumeFisico(audioSource2);
    }

    private void AplicarVolumeFisico(AudioSource src)
    {
        if (src == null) return;

        float volumeLogico = ObterVolumeLogico(src);
        src.volume = volumeLogico * multiplicadorExternoAtual;
    }

    private void RegistrarVolumeLogico(AudioSource src, float volume)
    {
        if (src == null) return;

        volumesLogicos[src] = Mathf.Max(0f, volume);
        src.volume = volumesLogicos[src] * multiplicadorExternoAtual;
    }

    private float ObterVolumeLogico(AudioSource src)
    {
        if (src == null) return 0f;

        if (volumesLogicos.TryGetValue(src, out float volume))
            return volume;

        float estimado = multiplicadorExternoAtual > 0.001f ? src.volume / multiplicadorExternoAtual : src.volume;
        volumesLogicos[src] = estimado;
        return estimado;
    }

    // ============================================================
    // MODO 1: SISTEMA ORIGINAL
    // ============================================================

    private IEnumerator CerebroAudioDinamico()
    {
        while (true)
        {
            if (musicaAtual == null && musicaAlvo != null)
            {
                musicaAtual = musicaAlvo;
                sourcePrincipal.clip = musicaAtual.audioClip;
                sourcePrincipal.time = musicaAtual.tempoSalvoNoMinuto;
                sourcePrincipal.Play();

                float target = volumeGlobal * musicaAtual.multiplicadorVolume;

                if (musicaAtual.jaTocouAPrimeiraVez)
                {
                    yield return IniciarFade(sourcePrincipal, target, musicaAtual.fadeInTime, false, null);
                }
                else
                {
                    RegistrarVolumeLogico(sourcePrincipal, target);
                    musicaAtual.jaTocouAPrimeiraVez = true;
                }
            }

            if (musicaAtual != null)
            {
                while (true)
                {
                    if (musicaAlvo != musicaAtual && musicaAlvo != null && musicaAlvo.ePrioridade)
                    {
                        if (musicaAtual.usarCrossfade)
                        {
                            ExecutarCrossfade(musicaAtual, musicaAlvo, musicaAlvo.fadeOutForcadoPrioridade);
                        }
                        else
                        {
                            yield return IniciarFade(sourcePrincipal, 0f, musicaAlvo.fadeOutForcadoPrioridade, true, musicaAtual);
                            musicaAtual = null; 
                        }

                        break;
                    }

                    bool esperaAtiva = musicaAtual.temQueTerminarAntesDeTrocar && ChecarAlternancia(musicaAtual.nomeArea);

                    if (musicaAlvo != musicaAtual && musicaAlvo != null && !esperaAtiva)
                    {
                        yield return new WaitForSeconds(musicaAtual.tempoContinuarTocando);

                        if (musicaAlvo == musicaAtual)
                            continue; 

                        if (musicaAtual.temQueTerminarAntesDeTrocar)
                            InverterAlternancia(musicaAtual.nomeArea);

                        if (musicaAtual.usarCrossfade)
                        {
                            ExecutarCrossfade(musicaAtual, musicaAlvo, musicaAtual.fadeOutNormal);
                        }
                        else
                        {
                            yield return IniciarFade(sourcePrincipal, 0f, musicaAtual.fadeOutNormal, true, musicaAtual);

                            float tempoEspera = musicaAtual.intervaloSemMusica;
                            musicaAtual = null; 

                            yield return new WaitForSeconds(tempoEspera);
                        }

                        break;
                    }

                    if (musicaAlvo == null && !esperaAtiva)
                    {
                        yield return new WaitForSeconds(musicaAtual.tempoContinuarTocando);

                        if (musicaAlvo != null)
                            continue; 

                        if (musicaAtual.temQueTerminarAntesDeTrocar)
                            InverterAlternancia(musicaAtual.nomeArea);

                        yield return IniciarFade(sourcePrincipal, 0f, musicaAtual.fadeOutNormal, true, musicaAtual);
                        
                        float tempoEsperaVazio = musicaAtual.intervaloSemMusica;
                        musicaAtual = null;

                        yield return new WaitForSeconds(tempoEsperaVazio);
                        break;
                    }

                    if (sourcePrincipal.isPlaying && musicaAtual.audioClip != null)
                    {
                        float restante = musicaAtual.audioClip.length - sourcePrincipal.time;

                        if (restante <= musicaAtual.fadeOutFimDeMusica)
                        {
                            yield return IniciarFade(sourcePrincipal, 0f, musicaAtual.fadeOutFimDeMusica, true, musicaAtual);
                            musicaAtual.tempoSalvoNoMinuto = 0f;

                            if (musicaAlvo != musicaAtual && musicaAlvo != null)
                            {
                                if (musicaAtual.temQueTerminarAntesDeTrocar)
                                    InverterAlternancia(musicaAtual.nomeArea);

                                musicaAtual = null;
                                break;
                            }

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

    // ============================================================
    // MODO 2: MÚSICAS TERMINAM ANTES DE TROCAR
    // ============================================================

    private IEnumerator CerebroAudioSequencial()
    {
        while (true)
        {
            if (musicaAtual == null)
            {
                ConfigMusica primeira = EscolherPrimeiraMusicaSequencial();

                if (primeira != null)
                    yield return TocarMusicaSequencial(primeira, primeiraMusicaSequencialComFade);
            }

            if (musicaAtual != null && sourcePrincipal != null && sourcePrincipal.isPlaying && musicaAtual.audioClip != null)
            {
                float restante = musicaAtual.audioClip.length - sourcePrincipal.time;
                float tempoFadeFinal = Mathf.Max(0.05f, musicaAtual.fadeOutFimDeMusica);

                if (restante <= tempoFadeFinal)
                {
                    ConfigMusica musicaQueTerminou = musicaAtual;

                    yield return IniciarFade(sourcePrincipal, 0f, tempoFadeFinal, true, musicaQueTerminou);

                    if (musicaQueTerminou != null)
                        musicaQueTerminou.tempoSalvoNoMinuto = 0f;

                    musicaAtual = null;

                    ConfigMusica proxima = EscolherProximaMusicaSequencial(musicaQueTerminou);

                    if (proxima != null)
                    {
                        float espera = 0f;

                        if (musicaQueTerminou != null)
                            espera = Mathf.Max(0f, musicaQueTerminou.intervaloSemMusica);

                        if (espera > 0f)
                            yield return new WaitForSeconds(espera);

                        yield return TocarMusicaSequencial(proxima, true);
                    }
                }
                else
                {
                    musicaAtual.tempoSalvoNoMinuto = sourcePrincipal.time;
                }
            }

            yield return null;
        }
    }

    private ConfigMusica EscolherPrimeiraMusicaSequencial()
    {
        if (musicaAlvo != null && musicaAlvo.audioClip != null)
            return musicaAlvo;

        List<ConfigMusica> validas = ObterMusicasValidas(null, true);

        if (validas.Count == 0)
            return null;

        return validas[Random.Range(0, validas.Count)];
    }

    private ConfigMusica EscolherProximaMusicaSequencial(ConfigMusica ultimaMusica)
    {
        if (musicaAlvo != null && musicaAlvo.audioClip != null)
        {
            if (!evitarMusicaIgualEmSequencia)
                return musicaAlvo;

            if (!MusicasSaoIguais(musicaAlvo, ultimaMusica))
                return musicaAlvo;
        }

        List<ConfigMusica> alternativas = ObterMusicasValidas(ultimaMusica, evitarMusicaIgualEmSequencia);

        if (alternativas.Count > 0)
            return alternativas[Random.Range(0, alternativas.Count)];

        if (musicaAlvo != null && musicaAlvo.audioClip != null)
            return musicaAlvo;

        List<ConfigMusica> qualquerValida = ObterMusicasValidas(null, false);

        if (qualquerValida.Count > 0)
            return qualquerValida[Random.Range(0, qualquerValida.Count)];

        return null;
    }

    private List<ConfigMusica> ObterMusicasValidas(ConfigMusica excluir, bool evitarIgual)
    {
        List<ConfigMusica> resultado = new List<ConfigMusica>();

        if (listaDeMusicas == null)
            return resultado;

        foreach (ConfigMusica m in listaDeMusicas)
        {
            if (m == null) continue;
            if (m.audioClip == null) continue;

            if (evitarIgual && MusicasSaoIguais(m, excluir))
                continue;

            resultado.Add(m);
        }

        return resultado;
    }

    private bool MusicasSaoIguais(ConfigMusica a, ConfigMusica b)
    {
        if (a == null || b == null)
            return false;

        if (a == b)
            return true;

        if (a.audioClip != null && b.audioClip != null && a.audioClip == b.audioClip)
            return true;

        if (!string.IsNullOrEmpty(a.nomeArea) && a.nomeArea == b.nomeArea)
            return true;

        return false;
    }

    private IEnumerator TocarMusicaSequencial(ConfigMusica musica, bool comFade)
    {
        if (musica == null || musica.audioClip == null || sourcePrincipal == null)
            yield break;

        musicaAtual = musica;

        sourcePrincipal.clip = musicaAtual.audioClip;
        sourcePrincipal.time = 0f;
        sourcePrincipal.Play();

        float target = volumeGlobal * musicaAtual.multiplicadorVolume;

        if (comFade)
        {
            RegistrarVolumeLogico(sourcePrincipal, 0f);
            yield return IniciarFade(sourcePrincipal, target, musicaAtual.fadeInTime, false, null);
        }
        else
        {
            RegistrarVolumeLogico(sourcePrincipal, target);
        }

        musicaAtual.jaTocouAPrimeiraVez = true;
    }

    // ============================================================
    // FADES / CROSSFADE / AUXILIARES
    // ============================================================

    private void ExecutarCrossfade(ConfigMusica antiga, ConfigMusica nova, float tempoFadeOutAntiga)
    {
        AudioSource sourceAntigo = sourcePrincipal;
        
        sourcePrincipal = (sourcePrincipal == audioSource1) ? audioSource2 : audioSource1;

        musicaAtual = nova;
        sourcePrincipal.clip = musicaAtual.audioClip;
        sourcePrincipal.time = musicaAtual.tempoSalvoNoMinuto;
        sourcePrincipal.Play();

        float targetVol = volumeGlobal * musicaAtual.multiplicadorVolume;

        IniciarFade(sourceAntigo, 0f, tempoFadeOutAntiga, true, antiga);

        if (musicaAtual.jaTocouAPrimeiraVez)
        {
            RegistrarVolumeLogico(sourcePrincipal, 0f);
            IniciarFade(sourcePrincipal, targetVol, musicaAtual.fadeInTime, false, null);
        }
        else
        {
            RegistrarVolumeLogico(sourcePrincipal, targetVol);
            musicaAtual.jaTocouAPrimeiraVez = true;
        }
    }

    private Coroutine IniciarFade(AudioSource src, float target, float duration, bool stop, ConfigMusica config)
    {
        if (src == audioSource1)
        {
            if (fadeRoutine1 != null)
                StopCoroutine(fadeRoutine1);

            fadeRoutine1 = StartCoroutine(FadeVolumeSource(src, target, duration, stop, config));
            return fadeRoutine1;
        }
        else
        {
            if (fadeRoutine2 != null)
                StopCoroutine(fadeRoutine2);

            fadeRoutine2 = StartCoroutine(FadeVolumeSource(src, target, duration, stop, config));
            return fadeRoutine2;
        }
    }

    private IEnumerator FadeVolumeSource(AudioSource source, float target, float duration, bool stopAtEnd, ConfigMusica configSalvarTempo)
    {
        float start = ObterVolumeLogico(source);
        float t = 0f;
        
        if (duration <= 0.01f)
        {
            RegistrarVolumeLogico(source, target);
        }
        else
        {
            while (t < duration)
            {
                t += Time.deltaTime;

                float volumeLogico = Mathf.Lerp(start, target, t / duration);
                RegistrarVolumeLogico(source, volumeLogico);

                yield return null;
            }

            RegistrarVolumeLogico(source, target);
        }

        if (stopAtEnd)
        {
            if (configSalvarTempo != null)
                configSalvarTempo.tempoSalvoNoMinuto = source.time;

            source.Stop();
            RegistrarVolumeLogico(source, 0f);
        }
    }

    private bool ChecarAlternancia(string area)
    {
        if (!deveEsperarNestaVez.ContainsKey(area))
            deveEsperarNestaVez[area] = true;

        return deveEsperarNestaVez[area];
    }

    private void InverterAlternancia(string area)
    {
        deveEsperarNestaVez[area] = !deveEsperarNestaVez[area];
    }
}
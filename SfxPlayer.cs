using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class SfxPlayer : MonoBehaviour
{
    private SfxManager manager;
    private AudioSource audioSource;
    private Sfx currentSfx;

    private List<SfxEffectModule> processAudioModules;
    private bool hasProcessAudioModules;

    public void Initialize(SfxManager manager, AudioSource source)
    {
        this.manager = manager;
        this.audioSource = source;
        this.processAudioModules = new List<SfxEffectModule>();
    }

    public void Play(Sfx sfx)
    {
        currentSfx = sfx;
        processAudioModules.Clear();
        hasProcessAudioModules = false;

        AudioClip clip = sfx.clips[Random.Range(0, sfx.clips.Count)];
        audioSource.clip = clip;

        if (sfx.effectModules != null)
        {
            foreach (var module in sfx.effectModules)
            {
                if (module == null || !module.enabled)
                    continue;

                if (module.hasInitMethod)
                {
                    module.InitAudioSource(audioSource);
                }

                if (module.hasProcessAudioMethod)
                {
                    processAudioModules.Add(module);
                    hasProcessAudioModules = true;
                }
            }
        }

        audioSource.Play();
    }

    public void Stop()
    {
        audioSource.Stop();
        currentSfx = null;
        processAudioModules.Clear();
        hasProcessAudioModules = false;
    }

    private void Update()
    {
        if (currentSfx != null && !audioSource.isPlaying)
        {
            currentSfx = null;
            processAudioModules.Clear();
            hasProcessAudioModules = false;
            manager.ReturnToPool(this);
        }
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (!hasProcessAudioModules)
            return;

        for (int i = 0; i < processAudioModules.Count; i++)
        {
            var module = processAudioModules[i];
            if (module != null && module.enabled)
            {
                module.ProcessAudio(data, channels);
            }
        }
    }
}

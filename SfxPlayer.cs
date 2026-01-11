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

    // Tail handling
    private float maxTailTime;
    private float clipEndTime;
    private bool inTailPhase;
    private static AudioClip silentClip;

    public void Initialize(SfxManager manager, AudioSource source)
    {
        this.manager = manager;
        this.audioSource = source;
        this.processAudioModules = new List<SfxEffectModule>();

        // Create a shared silent clip for tail processing
        if (silentClip == null)
        {
            int sampleRate = AudioSettings.outputSampleRate;
            int samples = sampleRate; // 1 second of silence
            silentClip = AudioClip.Create("SfxSilentTail", samples, 1, sampleRate, false);
            float[] silence = new float[samples];
            silentClip.SetData(silence, 0);
        }
    }

    public void Play(Sfx sfx)
    {
        currentSfx = sfx;
        processAudioModules.Clear();
        hasProcessAudioModules = false;
        inTailPhase = false;
        maxTailTime = 0f;

        AudioClip clip = sfx.clips[Random.Range(0, sfx.clips.Count)];
        audioSource.clip = clip;
        // reset these to default. may get set by modules later.
        audioSource.pitch = 1f;
        audioSource.volume = 1f;
        audioSource.spatialBlend = 0f;

        audioSource.loop = false;

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

                // Track max tail time
                if (module.tailTime > maxTailTime)
                {
                    maxTailTime = module.tailTime;
                }
            }
        }

        clipEndTime = Time.time + clip.length;
        audioSource.Play();
    }

    public void Stop()
    {
        audioSource.Stop();
        audioSource.loop = false;
        currentSfx = null;
        processAudioModules.Clear();
        hasProcessAudioModules = false;
        inTailPhase = false;
    }

    private void Update()
    {
        if (currentSfx == null)
            return;

        // Check if clip just finished and we have tail time
        if (!inTailPhase && !audioSource.isPlaying && maxTailTime > 0f)
        {
            // Enter tail phase - switch to silent clip to keep OnAudioFilterRead running
            inTailPhase = true;
            clipEndTime = Time.time + maxTailTime;
            audioSource.clip = silentClip;
            audioSource.loop = true;
            audioSource.Play();
            return;
        }

        // Check if tail phase is complete
        if (inTailPhase && Time.time >= clipEndTime)
        {
            Stop();
            manager.ReturnToPool(this);
            return;
        }

        // No tail time - return immediately when clip ends
        if (!inTailPhase && !audioSource.isPlaying)
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

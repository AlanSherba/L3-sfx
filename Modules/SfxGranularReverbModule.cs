using Less3.TypeTree;
using UnityEngine;

[TypeTreeMenu(typeof(Sfx), "Granular Reverb")]
public class SfxGranularReverbModule : SfxEffectModule
{
    public override string displayName => "Granular Reverb";

    [Header("Grain Settings")]
    [Range(0.01f, 0.2f)]
    [Tooltip("Size of each grain in seconds")]
    public float grainSize = 0.05f;

    [Range(1, 32)]
    [Tooltip("Number of simultaneous grains")]
    public int grainCount = 8;

    [Range(0f, 1f)]
    [Tooltip("Random pitch variation applied to grains")]
    public float pitchVariation = 0.1f;

    [Header("Reverb Settings")]
    [Range(0.1f, 2f)]
    [Tooltip("Buffer length in seconds - how far back grains can sample")]
    public float bufferLength = 0.5f;

    [Range(0f, 0.9f)]
    [Tooltip("Feedback amount - how much output feeds back into the buffer")]
    public float feedback = 0.3f;

    [Header("Mix")]
    [Range(0f, 1f)]
    [Tooltip("Dry/wet mix - 0 = dry only, 1 = wet only")]
    public float mix = 0.5f;

    public override bool hasProcessAudioMethod => true;

    // Runtime state - managed per-player instance
    private float[] buffer;
    private int bufferWritePos;
    private int bufferSize;
    private int sampleRate;
    private Grain[] grains;
    private int grainIndex;
    private float grainTimer;

    private struct Grain
    {
        public bool active;
        public float readPos;
        public float playbackRate;
        public int samplesRemaining;
        public int grainSamples;
    }

    public override void InitAudioSource(AudioSource audioSource)
    {
        sampleRate = AudioSettings.outputSampleRate;
        bufferSize = Mathf.CeilToInt(bufferLength * sampleRate);
        buffer = new float[bufferSize];
        bufferWritePos = 0;
        grainTimer = 0;
        grainIndex = 0;

        grains = new Grain[grainCount];
        for (int i = 0; i < grainCount; i++)
        {
            grains[i] = new Grain { active = false };
        }
    }

    public override bool hasInitMethod => true;

    public override void ProcessAudio(float[] data, int channels)
    {
        if (buffer == null || grains == null)
            return;

        int grainSamplesTarget = Mathf.CeilToInt(grainSize * sampleRate);
        float grainInterval = grainSize / grainCount;
        int grainIntervalSamples = Mathf.CeilToInt(grainInterval * sampleRate);

        for (int i = 0; i < data.Length; i += channels)
        {
            // Get input sample (mono mix for buffer)
            float inputSample = 0f;
            for (int c = 0; c < channels; c++)
            {
                inputSample += data[i + c];
            }
            inputSample /= channels;

            // Write to circular buffer
            buffer[bufferWritePos] = inputSample;
            bufferWritePos = (bufferWritePos + 1) % bufferSize;

            // Trigger new grains
            grainTimer++;
            if (grainTimer >= grainIntervalSamples)
            {
                grainTimer = 0;
                TriggerGrain(grainSamplesTarget);
            }

            // Process all active grains
            float wetSample = 0f;
            for (int g = 0; g < grains.Length; g++)
            {
                if (!grains[g].active)
                    continue;

                // Calculate envelope (simple triangle)
                float progress = 1f - (float)grains[g].samplesRemaining / grains[g].grainSamples;
                float envelope = progress < 0.5f ? progress * 2f : (1f - progress) * 2f;

                // Read from buffer with interpolation
                int readIndex = (int)grains[g].readPos;
                float frac = grains[g].readPos - readIndex;
                readIndex = ((readIndex % bufferSize) + bufferSize) % bufferSize;
                int nextIndex = (readIndex + 1) % bufferSize;

                float sample = buffer[readIndex] * (1f - frac) + buffer[nextIndex] * frac;
                wetSample += sample * envelope;

                // Advance grain position
                grains[g].readPos += grains[g].playbackRate;
                grains[g].samplesRemaining--;

                if (grains[g].samplesRemaining <= 0)
                {
                    grains[g].active = false;
                }
            }

            // Normalize by grain count
            wetSample /= Mathf.Max(1, grainCount * 0.5f);

            // Feedback into buffer
            buffer[((bufferWritePos - 1) + bufferSize) % bufferSize] += wetSample * feedback;

            // Mix dry and wet
            for (int c = 0; c < channels; c++)
            {
                float dry = data[i + c];
                data[i + c] = dry * (1f - mix) + wetSample * mix;
            }
        }
    }

    private void TriggerGrain(int grainSamples)
    {
        // Find inactive grain slot
        for (int i = 0; i < grains.Length; i++)
        {
            int idx = (grainIndex + i) % grains.Length;
            if (!grains[idx].active)
            {
                // Random position in buffer (behind write position)
                int maxOffset = Mathf.Min(bufferSize - grainSamples, bufferSize);
                int offset = Random.Range(grainSamples, maxOffset);
                float startPos = (bufferWritePos - offset + bufferSize) % bufferSize;

                // Random pitch variation
                float pitchMult = 1f + Random.Range(-pitchVariation, pitchVariation);

                grains[idx] = new Grain
                {
                    active = true,
                    readPos = startPos,
                    playbackRate = pitchMult,
                    samplesRemaining = grainSamples,
                    grainSamples = grainSamples
                };

                grainIndex = (idx + 1) % grains.Length;
                return;
            }
        }

        // All slots full - steal oldest
        grainIndex = (grainIndex + 1) % grains.Length;
        int maxOffset2 = Mathf.Min(bufferSize - grainSamples, bufferSize);
        int offset2 = Random.Range(grainSamples, maxOffset2);
        float startPos2 = (bufferWritePos - offset2 + bufferSize) % bufferSize;
        float pitchMult2 = 1f + Random.Range(-pitchVariation, pitchVariation);

        grains[grainIndex] = new Grain
        {
            active = true,
            readPos = startPos2,
            playbackRate = pitchMult2,
            samplesRemaining = grainSamples,
            grainSamples = grainSamples
        };
    }
}

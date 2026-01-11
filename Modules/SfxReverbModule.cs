using Less3.TypeTree;
using UnityEngine;

/// <summary>
/// Schroeder/Freeverb-style reverb using parallel comb filters and series allpass filters.
/// </summary>
[TypeTreeMenu(typeof(Sfx), "Reverb")]
public class SfxReverbModule : SfxEffectModule
{
    public override string displayName => "Reverb";

    [Header("Reverb Settings")]
    [Range(0f, 1f)]
    [Tooltip("Room size - affects decay time")]
    public float roomSize = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Damping - high frequency absorption")]
    public float damping = 0.5f;

    [Range(0f, 1f)]
    [Tooltip("Stereo width")]
    public float width = 1f;

    [Header("Mix")]
    [Range(0f, 1f)]
    [Tooltip("Dry/wet mix - 0 = dry only, 1 = wet only")]
    public float mix = 0.3f;

    public override bool hasInitMethod => true;
    public override bool hasProcessAudioMethod => true;

    // Tail time based on room size
    public override float tailTime => 1f + roomSize * 4f;

    // Comb filter delay times (in samples at 44100 Hz)
    private static readonly int[] CombTunings = { 1116, 1188, 1277, 1356, 1422, 1491, 1557, 1617 };
    private static readonly int[] AllpassTunings = { 556, 441, 341, 225 };
    private const int StereoSpread = 23;

    // Denormal prevention constant
    private const float Denormal = 1e-18f;

    private int sampleRate;
    private CombFilter[] combFiltersL;
    private CombFilter[] combFiltersR;
    private AllpassFilter[] allpassFiltersL;
    private AllpassFilter[] allpassFiltersR;

    // DC blocker state
    private float dcInL, dcOutL;
    private float dcInR, dcOutR;
    private const float DcCoeff = 0.995f;

    // Use classes instead of structs to avoid copy-on-access issues
    private class CombFilter
    {
        public float[] buffer;
        public int bufferSize;
        public int index;
        public float filterStore;

        public float Process(float input, float feedback, float damp1, float damp2)
        {
            float output = buffer[index];

            // Denormal prevention
            filterStore = output * damp2 + filterStore * damp1 + Denormal;
            filterStore -= Denormal;

            buffer[index] = input + filterStore * feedback;
            index++;
            if (index >= bufferSize) index = 0;

            return output;
        }

        public void Clear()
        {
            System.Array.Clear(buffer, 0, buffer.Length);
            filterStore = 0f;
            index = 0;
        }
    }

    private class AllpassFilter
    {
        public float[] buffer;
        public int bufferSize;
        public int index;

        public float Process(float input)
        {
            float bufOut = buffer[index];
            float output = bufOut - input;
            buffer[index] = input + bufOut * 0.5f;
            index++;
            if (index >= bufferSize) index = 0;

            return output;
        }

        public void Clear()
        {
            System.Array.Clear(buffer, 0, buffer.Length);
            index = 0;
        }
    }

    public override void InitAudioSource(AudioSource audioSource)
    {
        sampleRate = AudioSettings.outputSampleRate;
        float sampleRateScale = sampleRate / 44100f;

        // Initialize comb filters
        combFiltersL = new CombFilter[CombTunings.Length];
        combFiltersR = new CombFilter[CombTunings.Length];

        for (int i = 0; i < CombTunings.Length; i++)
        {
            int sizeL = Mathf.Max(1, Mathf.RoundToInt(CombTunings[i] * sampleRateScale));
            int sizeR = Mathf.Max(1, Mathf.RoundToInt((CombTunings[i] + StereoSpread) * sampleRateScale));

            combFiltersL[i] = new CombFilter
            {
                buffer = new float[sizeL],
                bufferSize = sizeL,
                index = 0,
                filterStore = 0
            };

            combFiltersR[i] = new CombFilter
            {
                buffer = new float[sizeR],
                bufferSize = sizeR,
                index = 0,
                filterStore = 0
            };
        }

        // Initialize allpass filters
        allpassFiltersL = new AllpassFilter[AllpassTunings.Length];
        allpassFiltersR = new AllpassFilter[AllpassTunings.Length];

        for (int i = 0; i < AllpassTunings.Length; i++)
        {
            int sizeL = Mathf.Max(1, Mathf.RoundToInt(AllpassTunings[i] * sampleRateScale));
            int sizeR = Mathf.Max(1, Mathf.RoundToInt((AllpassTunings[i] + StereoSpread) * sampleRateScale));

            allpassFiltersL[i] = new AllpassFilter
            {
                buffer = new float[sizeL],
                bufferSize = sizeL,
                index = 0
            };

            allpassFiltersR[i] = new AllpassFilter
            {
                buffer = new float[sizeR],
                bufferSize = sizeR,
                index = 0
            };
        }

        // Clear DC blocker state
        dcInL = dcOutL = 0f;
        dcInR = dcOutR = 0f;
    }

    public override void ProcessAudio(float[] data, int channels)
    {
        if (combFiltersL == null || allpassFiltersL == null)
            return;

        // Calculate coefficients
        float feedback = roomSize * 0.28f + 0.7f;
        float damp1 = damping * 0.4f;
        float damp2 = 1f - damp1;
        float wet1 = mix * (width * 0.5f + 0.5f);
        float wet2 = mix * (0.5f - width * 0.5f);
        float dry = 1f - mix;

        int dataLen = data.Length;

        for (int i = 0; i < dataLen; i += channels)
        {
            float inputL, inputR;

            if (channels >= 2 && i + 1 < dataLen)
            {
                inputL = data[i];
                inputR = data[i + 1];
            }
            else
            {
                inputL = data[i];
                inputR = data[i];
            }

            float input = (inputL + inputR) * 0.5f;

            // Process parallel comb filters
            float outL = 0f;
            float outR = 0f;

            for (int c = 0; c < combFiltersL.Length; c++)
            {
                outL += combFiltersL[c].Process(input, feedback, damp1, damp2);
                outR += combFiltersR[c].Process(input, feedback, damp1, damp2);
            }

            // Normalize comb filter output (8 filters summed)
            const float combGain = 0.125f; // 1/8
            outL *= combGain;
            outR *= combGain;

            // Process series allpass filters
            for (int a = 0; a < allpassFiltersL.Length; a++)
            {
                outL = allpassFiltersL[a].Process(outL);
                outR = allpassFiltersR[a].Process(outR);
            }

            // DC blocking filter (high-pass to remove DC offset)
            float newDcOutL = outL - dcInL + DcCoeff * dcOutL;
            dcInL = outL;
            dcOutL = newDcOutL;
            outL = newDcOutL;

            float newDcOutR = outR - dcInR + DcCoeff * dcOutR;
            dcInR = outR;
            dcOutR = newDcOutR;
            outR = newDcOutR;

            // Soft clip to prevent extreme values
            outL = SoftClip(outL);
            outR = SoftClip(outR);

            // Mix and output
            if (channels >= 2 && i + 1 < dataLen)
            {
                data[i] = inputL * dry + outL * wet1 + outR * wet2;
                data[i + 1] = inputR * dry + outR * wet1 + outL * wet2;
            }
            else
            {
                data[i] = inputL * dry + (outL + outR) * 0.5f * mix;
            }
        }
    }

    private static float SoftClip(float x)
    {
        // Soft saturation to prevent harsh clipping
        if (x > 1f) return 1f - 1f / (1f + x);
        if (x < -1f) return -1f + 1f / (1f - x);
        return x;
    }
}

using UnityEngine;

public class SfxSpatializerModule : SfxEffectModule
{
    [Header("Spatial Blend")]
    [Range(0f, 1f)]
    [Tooltip("0 = 2D, 1 = 3D")]
    public float spatialBlend = 1f;

    [Header("3D Sound Settings")]
    public float minDistance = 1f;
    public float maxDistance = 500f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    [Header("Spread")]
    [Range(0f, 360f)]
    public float spread = 0f;

    [Header("Doppler")]
    [Range(0f, 5f)]
    public float dopplerLevel = 1f;

    public override bool hasInitMethod => true;

    public override void InitAudioSource(AudioSource audioSource)
    {
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = rolloffMode;
        audioSource.spread = spread;
        audioSource.dopplerLevel = dopplerLevel;
    }
}

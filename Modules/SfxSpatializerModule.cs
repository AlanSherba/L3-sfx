using Less3.TypeTree;
using UnityEngine;

[TypeTreeMenu(typeof(Sfx), "Spatializer")]
public class SfxSpatializerModule : SfxEffectModule
{
    public override string displayName => "Spatializer";

    
    [Range(0f, 1f)]
    [Tooltip("0 = 2D, 1 = 3D")]
    public float spatialBlend = 1f;

    [Header("3D Sound Settings")]
    public float minDistance = 1f;
    public float maxDistance = 500f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    public override bool hasInitMethod => true;

    public override void InitAudioSource(AudioSource audioSource)
    {
        audioSource.spatialBlend = spatialBlend;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = rolloffMode;
        audioSource.spread = 0f;
        audioSource.dopplerLevel = 0f;
    }
}

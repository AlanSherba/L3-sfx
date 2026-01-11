using Less3.TypeTree;
using UnityEngine;

/// <summary>
/// Randomizes pitch and volume of the audio source on play for variation.
/// </summary>
[TypeTreeMenu(typeof(Sfx), "Randomize")]
public class SfxRandomizeModule : SfxEffectModule
{
    public override string displayName => "Randomize";

    [Header("Volume")]
    [Range(0f, 1f)]
    public float volumeMin = 0.8f;
    [Range(0f, 1f)]
    public float volumeMax = 1f;

    [Header("Pitch")]
    [Range(0.5f, 2f)]
    public float pitchMin = 0.95f;
    [Range(0.5f, 2f)]
    public float pitchMax = 1.05f;

    public override bool hasInitMethod => true;

    public override void InitAudioSource(AudioSource audioSource)
    {
        audioSource.volume = Random.Range(volumeMin, volumeMax);
        audioSource.pitch = Random.Range(pitchMin, pitchMax);
    }
}

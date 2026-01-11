using UnityEngine;

/// <summary>
/// An SfxEffectModule is a scriptable object that can be used to apply effects to an audio source.
/// Depending on what the effect module does, you may want to implement the InitAudioSource and ProcessAudio methods.
/// </summary>
public abstract class SfxEffectModule : ScriptableObject
{
    public virtual string displayName => GetType().Name;
    public bool enabled = true;
    
    // These bools should be set to true only if the methods below are implemented.
    public virtual bool hasInitMethod => false;
    public virtual bool hasProcessAudioMethod => false;

    public virtual void InitAudioSource(AudioSource audioSource) { }
    public virtual void ProcessAudio(float[] data, int channels) { }
}

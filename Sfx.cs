using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Sfx", menuName = "L3/Sfx")]
public class Sfx : ScriptableObject
{
    public List<AudioClip> clips = new List<AudioClip>();
    public List<SfxEffectModule> effectModules = new List<SfxEffectModule>();
}

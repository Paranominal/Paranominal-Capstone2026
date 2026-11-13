using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Audio/Music Data", fileName = "New Music")]
public class MusicDataSO : ScriptableObject
{
    // [Header("Mixer Settings")]
    // [SerializeField] private AudioMixerGroup mixerGroup;

    // Behaviour
    [Header("Audio Track")]
    [SerializeField] private AudioClip mainClip;
    [SerializeField] private bool loopMainClip = true;
    [SerializeField] private bool hasBuildClip = true;
    [SerializeField, ShowIf("hasBuildClip", true)] private AudioClip buildClip;
    public AudioClip MainClip => mainClip;
    public bool LoopMainClip => loopMainClip;
    public bool HasBuildClip => hasBuildClip && buildClip != null;
    public AudioClip BuildClip => buildClip;

    // // Spatial
    // [Header("Spacial Blend")]
    // [Range(0f, 1f)] public float spatialBlend = 0f;
    // [Min(0f)] public float minDistance = 1f;
    // [Min(0f)] public float maxDistance = 500f;
    // public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;
}

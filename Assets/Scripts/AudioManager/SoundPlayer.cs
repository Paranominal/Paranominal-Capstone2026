using UnityEngine;

// Summary: Plays a sound on demand via PlaySound(). Designed to be invoked from Animation Events, UnityEvents (button clicks etc.), or other scripts. 
// The workhorse component for any "fire this sound when X happens" use case where X isn't a built-in Unity callback the component watches itself.
[AddComponentMenu("Audio/Sound Player")]
[RequireComponent(typeof(AudioSource))]
public class SoundPlayer : MonoBehaviour
{
    [SerializeField] private SoundDataSO sound;
    [SerializeField] private AudioSource source;

    // Called when the component is first added or Reset is clicked.
    // Pre-fills the source field with the AudioSource on this GameObject so designers don't need to drag it in manually.
    private void Reset()
    {
        source = GetComponent<AudioSource>();
    }

    // Plays the configured sound through the configured source.
    // Public so Animation Events, UnityEvents, and other scripts can call it.
    public void PlaySound()
    {
        if (sound == null) return;
        AudioManager.PlaySound(sound, source);
    }
}

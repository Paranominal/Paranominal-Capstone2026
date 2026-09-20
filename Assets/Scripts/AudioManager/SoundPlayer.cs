using System.Collections.Generic;
using UnityEngine;

// Summary: Plays a sound on demand via PlaySound(). Designed to be invoked from Animation Events, UnityEvents (button clicks etc.), or other scripts. 
// The workhorse component for any "fire this sound when X happens" use case where X isn't a built-in Unity callback the component watches itself.
[AddComponentMenu("Audio/Sound Player")]
[RequireComponent(typeof(AudioSource))]
public class SoundPlayer : MonoBehaviour
{
    [SerializeField] private List<SoundDataSO> sounds;
    [SerializeField] private AudioSource source;

    // Called when the component is first added or Reset is clicked.
    // Pre-fills the source field with the AudioSource on this GameObject so designers don't need to drag it in manually.
    private void Start()
    {
        if (sounds.Count < 1)
        {
            Debug.LogWarning($"[{this}] No Sounds set! disabling SoundPlayer");
            enabled = false;
        } 
    }

    private void Reset()
    {
        if (!source)
        {
            Debug.LogWarning($"[{this}] No AudioSource was set! attempting to get");
            source = GetComponent<AudioSource>();
            if (!source) Debug.LogWarning($"[{this}] AudioSource was not found!");
            else Debug.LogWarning($"[{this}] AudioSource was found! ({source})");
        }
    }

    // Plays the configured sound through the configured source.
    // Public so Animation Events, UnityEvents, and other scripts can call it.
    public void PlaySound(int index)
    {
        if (index < 0 || index >= sounds.Count)
        {
            Debug.LogWarning($"[{this}] PlaySound() index was not in range!");
            return; 
        }
        AudioManager.PlaySound(sounds[index], source);
    }
}

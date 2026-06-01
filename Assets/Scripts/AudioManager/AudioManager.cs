using UnityEngine;

// Summary: Central runtime entry point for playing sounds. Holds a default 2D AudioSource for non-positional playback.
// Persists across scene loads via DontDestroyOnLoad.
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    private AudioSource ownSource;

    private void Awake()
    {
        // Duplicate protection - First manager wins, duplicates destroy themselves.
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        ownSource = GetComponent<AudioSource>();
        DontDestroyOnLoad(gameObject);
    }

    // Plays a sound through the manager's own 2D AudioSource using PlayOneShot. 
    // Fire-and-forget; can't be stopped or looped. Use the overloads for looping or stoppable sounds.
    public static void PlaySound(SoundDataSO sound)
    {
        if (!ValidateCall(sound)) return;
        if (!sound.CanPlayNow()) return;

        AudioClip clip = sound.GetClip();
        if (clip == null) return;

        if (sound.loop)
        {
            Debug.LogWarning($"[AudioManager] '{sound.name}' is set to loop but was " +
                             "played without a caller-supplied source. Loop ignored.", sound);
        }

        // Mixer group has to be set on the source itself; PlayOneShot inherits it.
        instance.ownSource.outputAudioMixerGroup = sound.mixerGroup;
        instance.ownSource.pitch = sound.GetRandomPitch();
        instance.ownSource.PlayOneShot(clip, sound.GetRandomVolume());
    }

    // Plays a sound through the caller's AudioSource. Applies all of the SO's settings (volume, pitch, mixer, spatial, loop).
    // Supports looping and can be stopped by the caller.
    public static void PlaySound(SoundDataSO sound, AudioSource source)
    {
        if (!ValidateCall(sound)) return;
        if (source == null)
        {
            Debug.LogWarning($"[AudioManager] PlaySound called with a null source for " +
                             $"'{sound.name}'. Use the single-arg overload for 2D playback.", sound);
            return;
        }
        if (!sound.CanPlayNow()) return;

        AudioClip clip = sound.GetClip();
        if (clip == null) return;

        ConfigureSource(source, sound, clip);
        source.Play();
    }

    // Applies the SO's full playback settings to the given source.
    private static void ConfigureSource(AudioSource source, SoundDataSO sound, AudioClip clip)
    {
        source.clip = clip;
        source.volume = sound.GetRandomVolume();
        source.pitch = sound.GetRandomPitch();
        source.outputAudioMixerGroup = sound.mixerGroup;
        source.loop = sound.loop;
        source.spatialBlend = sound.spatialBlend;
        source.minDistance = sound.minDistance;
        source.maxDistance = sound.maxDistance;
        source.rolloffMode = sound.rolloffMode;
    }

    // Shared null-checks. Returns false (and logs) if the call can't proceed.
    private static bool ValidateCall(SoundDataSO sound)
    {
        if (instance == null)
        {
            Debug.LogError("[AudioManager] No AudioManager in the scene. " +
                           "Add one before calling PlaySound.");
            return false;
        }
        if (sound == null)
        {
            Debug.LogWarning("[AudioManager] PlaySound called with a null SoundDataSO.");
            return false;
        }
        return true;
    }
}

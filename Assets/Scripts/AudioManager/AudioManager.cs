using UnityEngine;

// Summary: 
// Central runtime entry point for playing sounds. Holds a default 2D AudioSource for non-positional playback, and consumes SoundDataSO assets to configure either its own source or a caller-supplied one. 
// Persists across scene loads via DontDestroyOnLoad.
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    private AudioSource ownSource;

    private void Awake()
    {
        // Strict singleton: first manager wins, duplicates destroy themselves.
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
    // Fire-and-forget; can't be stopped or looped. 
    // Use an overload with a supplied source for looping or stoppable sounds.
    public static void PlaySound(SoundDataSO sound)
    {
        if (!TryPrepare(sound, instance != null ? instance.ownSource : null, out AudioClip clip))
            return;

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

    // Plays a sound through a supplied AudioSource using Play.
    // Applies all of the SO's settings (volume, pitch, mixer, spatial, loop) and supports looping / stopping via the supplied source. 
    // Interrupts anything currently playing on the source.
    public static void PlaySound(SoundDataSO sound, AudioSource source)
    {
        if (!TryPrepare(sound, source, out AudioClip clip)) return;

        ConfigureSource(source, sound, clip);
        source.Play();
    }

    // Plays a sound through a supplied AudioSource using PlayOneShot.
    // Layers the clip on top of anything already playing on the source, without interrupting it. 
    // Applies pitch, mixer, and spatial settings; volume is applied via the PlayOneShot parameter. 
    // Loop is ignored (PlayOneShot can't loop) and a warning is logged if the SO is set to loop.
    public static void PlaySound(SoundDataSO sound, AudioSource source, bool oneShot)
    {
        if (!oneShot)
        {
            PlaySound(sound, source);
            return;
        }

        if (!TryPrepare(sound, source, out AudioClip clip)) return;

        if (sound.loop)
        {
            Debug.LogWarning($"[AudioManager] '{sound.name}' is set to loop but was " +
                             "played as a one-shot on a caller source. Loop ignored.", sound);
        }

        // Apply the settings PlayOneShot will inherit from the source.
        // Volume is passed as the PlayOneShot parameter; clip and loop are ignored by PlayOneShot so no need to set them.
        source.outputAudioMixerGroup = sound.mixerGroup;
        source.pitch = sound.GetRandomPitch();
        source.spatialBlend = sound.spatialBlend;
        source.minDistance = sound.minDistance;
        source.maxDistance = sound.maxDistance;
        source.rolloffMode = sound.rolloffMode;

        source.PlayOneShot(clip, sound.GetRandomVolume());
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

    // Shared setup for all PlaySound overloads. Runs validation, cooldown, and clip selection. Returns false if any check fails; caller should bail.
    private static bool TryPrepare(SoundDataSO sound, AudioSource source, out AudioClip clip)
    {
        clip = null;

        if (!ValidateCall(sound)) return false;

        if (source == null)
        {
            Debug.LogWarning($"[AudioManager] PlaySound called with a null source for " +
                             $"'{sound.name}'.", sound);
            return false;
        }

        if (!sound.CanPlayNow()) return false;

        clip = sound.GetClip();
        return clip != null;
    }

    // Null-checks the manager instance and the SO. Returns false (and logs) if the call can't proceed.
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
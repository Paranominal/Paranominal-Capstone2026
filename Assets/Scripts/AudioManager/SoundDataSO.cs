using UnityEngine;
using UnityEngine.Audio;

// Summary: Per-sound data asset. Holds clips and playback settings for a single sound,
// and provides the small bits of logic (clip selection, randomisation, cooldown) the
// AudioManager uses when playing it. Does not touch AudioSources itself.
[CreateAssetMenu(menuName = "Audio/Sound Data", fileName = "New Sound")]
public class SoundDataSO : ScriptableObject
{
    public enum SelectionMode
    {
        Random,
        RandomNoRepeat,
        Sequential,
    }

    // Core
    public AudioClip[] clips;
    public AudioMixerGroup mixerGroup;

    // Selection
    public SelectionMode selectionMode = SelectionMode.Random;

    // Variation
    // Volume: base level + per-play offset range. Final volume is clamped to 0..1.
    [Range(0f, 1f)] public float volume = 1f;
    public Vector2 volumeOffsetRange = Vector2.zero;
    // Pitch: per-play range; set x == y for a fixed pitch.
    public Vector2 pitch = new Vector2(1f, 1f);

    // Spatial
    [Range(0f, 1f)] public float spatialBlend = 0f;
    [Min(0f)] public float minDistance = 1f;
    [Min(0f)] public float maxDistance = 500f;
    public AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    // Behaviour
    public bool loop = false;
    [Min(0f)] public float minInterval = 0f;

    // Runtime-only state (not serialized; shared across all callers of this SO)
    private int lastClipIndex = -1;
    private float lastPlayTime = -Mathf.Infinity;

    // Summary: Returns the next clip to play based on selectionMode. Null if no clips.
    public AudioClip GetClip()
    {
        if (clips == null || clips.Length == 0)
            return null;

        if (clips.Length == 1)
        {
            lastClipIndex = 0;
            return clips[0];
        }

        int index;
        switch (selectionMode)
        {
            case SelectionMode.Sequential:
                index = (lastClipIndex + 1) % clips.Length;
                break;

            case SelectionMode.RandomNoRepeat:
                // Pick from clips excluding the last one played
                index = Random.Range(0, clips.Length - 1);
                if (index >= lastClipIndex) index++;
                break;

            case SelectionMode.Random:
            default:
                index = Random.Range(0, clips.Length);
                break;
        }

        lastClipIndex = index;
        return clips[index];
    }

    // Summary: Samples a volume by applying a random offset to the base volume.
    // Clamped to 0..1.
    public float GetRandomVolume()
    {
        float offset = Random.Range(volumeOffsetRange.x, volumeOffsetRange.y);
        return Mathf.Clamp01(volume + offset);
    }

    // Summary: Samples a pitch within the configured range.
    public float GetRandomPitch() => Random.Range(pitch.x, pitch.y);

    // Summary: Returns true if enough time has passed since the last play.
    // Stamps lastPlayTime if it returns true.
    public bool CanPlayNow()
    {
        if (minInterval <= 0f)
        {
            lastPlayTime = Time.time;
            return true;
        }

        if (Time.time - lastPlayTime < minInterval)
            return false;

        lastPlayTime = Time.time;
        return true;
    }
}

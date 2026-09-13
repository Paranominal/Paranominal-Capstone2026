using UnityEngine;
using UnityEngine.Audio;

// Summary: Handles fear-driven audio effects including heartbeat playback and lowpass filtering.
public class FearAudioEffects : MonoBehaviour
{
    [Header("Heartbeat Clips")]
    [SerializeField] private SoundDataSO lowBeat;
    [SerializeField] private SoundDataSO mediumBeat;
    [SerializeField] private SoundDataSO highBeat;

    [Header("Heartbeat Volume")]
    [SerializeField] private float volumeMin = 0.1f;
    [SerializeField] private float volumeMax = 1.0f;

    [Header("Heartbeat Interval (Seconds)")]
    [SerializeField] private float lowInterval = 1.2f;
    [SerializeField] private float mediumInterval = 0.9f;
    [SerializeField] private float highInterval = 0.6f;

    private AudioSource heartbeatSource;
    private float beatTimer;
    private bool heartbeatActive;
    private FearBar.FearRank currentRank;

    private void Awake()
    {
        heartbeatSource = gameObject.AddComponent<AudioSource>();
        heartbeatSource.playOnAwake = false;

        if (lowBeat != null)
            heartbeatSource.outputAudioMixerGroup = lowBeat.mixerGroup;
    }

    public void UpdateIntensity(float normalizedFear, bool isInEncounter)
    {
        UpdateHeartbeat(normalizedFear);
    }

    public void OnRankChanged(FearBar.FearRank rank)
    {
        currentRank = rank;

        bool shouldPlay = rank != FearBar.FearRank.Fine;

        if (shouldPlay && !heartbeatActive)
        {
            heartbeatActive = true;
            beatTimer = 0f;
        }
        else if (!shouldPlay && heartbeatActive)
        {
            heartbeatActive = false;
        }
    }

    private void UpdateHeartbeat(float normalizedFear)
    {
        if (!heartbeatActive) return;

        beatTimer -= Time.deltaTime;

        if (beatTimer <= 0f)
        {
            PlayBeat(normalizedFear);
            beatTimer = GetCurrentInterval();
        }
    }

    private float GetCurrentInterval()
    {
        return currentRank switch
        {
            FearBar.FearRank.Low => lowInterval,
            FearBar.FearRank.Medium => mediumInterval,
            FearBar.FearRank.High => highInterval,
            _ => lowInterval,
        };
    }

    private void PlayBeat(float normalizedFear)
    {
        SoundDataSO beatSO = GetCurrentBeatSO();
        if (beatSO == null) return;

        AudioClip clip = beatSO.GetClip();
        if (clip == null) return;

        heartbeatSource.pitch = beatSO.GetRandomPitch();
        heartbeatSource.volume = GetHeartbeatVolume(normalizedFear);
        heartbeatSource.PlayOneShot(clip);
    }

    private SoundDataSO GetCurrentBeatSO()
    {
        return currentRank switch
        {
            FearBar.FearRank.Low => lowBeat,
            FearBar.FearRank.Medium => mediumBeat,
            FearBar.FearRank.High => highBeat,
            _ => null,
        };
    }

    private float GetHeartbeatVolume(float normalizedFear)
    {
        return Mathf.Lerp(volumeMin, volumeMax, normalizedFear);
    }
}
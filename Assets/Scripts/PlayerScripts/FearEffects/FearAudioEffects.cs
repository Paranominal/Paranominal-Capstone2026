using UnityEngine;
using UnityEngine.Audio;

// Summary: Handles fear-driven audio effects including heartbeat playback and lowpass filtering.
public class FearAudioEffects : MonoBehaviour
{
    [Header("Heartbeat Clips")]
    [SerializeField] private SoundDataSO fineBeat;
    [SerializeField] private SoundDataSO criticalBeat;
    [SerializeField] private SoundDataSO doomedBeat;

    [Header("Heartbeat Volume")]
    [SerializeField] private float volumeMin = 0.1f;
    [SerializeField] private float volumeMax = 1.0f;

    [Header("Heartbeat Interval (Seconds)")]
    [SerializeField] private float fineInterval = 1.2f;
    [SerializeField] private float criticalInterval = 0.9f;
    [SerializeField] private float doomedInterval = 0.6f;

    private AudioSource heartbeatSource;
    private float beatTimer;
    private bool heartbeatActive;
    private FearBar.FearRank currentRank;

    private void Awake()
    {
        heartbeatSource = gameObject.AddComponent<AudioSource>();
        heartbeatSource.playOnAwake = false;

        if (fineBeat != null)
            heartbeatSource.outputAudioMixerGroup = fineBeat.mixerGroup;
    }

    public void UpdateIntensity(float normalizedFear, bool isInEncounter)
    {
        UpdateHeartbeat(normalizedFear);
    }

    public void OnRankChanged(FearBar.FearRank rank)
    {
        currentRank = rank;

        bool shouldPlay = rank != FearBar.FearRank.Healthy;

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
            FearBar.FearRank.Fine => fineInterval,
            FearBar.FearRank.Critical => criticalInterval,
            FearBar.FearRank.Doomed => doomedInterval,
            _ => fineInterval,
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
            FearBar.FearRank.Fine => fineBeat,
            FearBar.FearRank.Critical => criticalBeat,
            FearBar.FearRank.Doomed => doomedBeat,
            _ => null,
        };
    }

    private float GetHeartbeatVolume(float normalizedFear)
    {
        return Mathf.Lerp(volumeMin, volumeMax, normalizedFear);
    }
}
using UnityEngine;
using UnityEngine.Audio;

// Summary: Handles fear-driven audio effects including heartbeat playback and lowpass filtering.
public class FearAudioEffects : MonoBehaviour
{
    [Header("Heartbeat Clips")]
    [SerializeField] private SoundDataSO mediumIdleBeat;
    [SerializeField] private SoundDataSO mediumActionBeat;
    [SerializeField] private SoundDataSO lowIdleBeat;
    [SerializeField] private SoundDataSO lowActionBeat;

    [Header("Heartbeat Volume")]
    [SerializeField] private float mediumVolumeMin = 0.1f;
    [SerializeField] private float mediumVolumeMax = 0.4f;
    [SerializeField] private float lowVolumeMin = 0.4f;
    [SerializeField] private float lowVolumeMax = 1.0f;

    [Header("Heartbeat Interval (Seconds)")]
    [SerializeField] private float mediumInterval = 1.2f;
    [SerializeField] private float lowInterval = 0.8f;

    private AudioSource heartbeatSource;
    private float beatTimer;
    private bool heartbeatActive;
    private FearBar.FearRank currentRank;

    // Normalized fear boundaries for each rank (derived from FearBar thresholds: 33, 66)
    private const float MediumFearMin = 0.34f;
    private const float MediumFearMax = 0.66f;
    private const float LowFearMin = 0.67f;
    private const float LowFearMax = 1.0f;

    private void Awake()
    {
        heartbeatSource = gameObject.AddComponent<AudioSource>();
        heartbeatSource.playOnAwake = false;

        if (mediumIdleBeat != null)
            heartbeatSource.outputAudioMixerGroup = mediumIdleBeat.mixerGroup;
    }

    public void UpdateIntensity(float normalizedFear, bool isInEncounter)
    {
        UpdateHeartbeat(normalizedFear, isInEncounter);
    }

    public void OnRankChanged(FearBar.FearRank rank)
    {
        currentRank = rank;

        bool shouldPlay = rank == FearBar.FearRank.Medium || rank == FearBar.FearRank.Low;

        if (shouldPlay && !heartbeatActive)
        {
            heartbeatActive = true;
            beatTimer = 0f; // first beat plays immediately
        }
        else if (!shouldPlay && heartbeatActive)
        {
            heartbeatActive = false;
        }
    }

    private void UpdateHeartbeat(float normalizedFear, bool isInEncounter)
    {
        if (!heartbeatActive) return;

        beatTimer -= Time.deltaTime;

        if (beatTimer <= 0f)
        {
            PlayBeat(normalizedFear, isInEncounter);
            float interval = currentRank == FearBar.FearRank.Low ? lowInterval : mediumInterval;
            beatTimer = interval;
        }
    }

    private void PlayBeat(float normalizedFear, bool isInEncounter)
    {
        SoundDataSO beatSO = GetCurrentBeatSO(isInEncounter);
        if (beatSO == null) return;

        AudioClip clip = beatSO.GetClip();
        if (clip == null) return;

        heartbeatSource.pitch = beatSO.GetRandomPitch();
        heartbeatSource.volume = GetHeartbeatVolume(normalizedFear);
        heartbeatSource.PlayOneShot(clip);
    }

    private SoundDataSO GetCurrentBeatSO(bool isInEncounter)
    {
        return currentRank switch
        {
            FearBar.FearRank.Medium => isInEncounter ? mediumActionBeat : mediumIdleBeat,
            FearBar.FearRank.Low => isInEncounter ? lowActionBeat : lowIdleBeat,
            _ => null,
        };
    }

    // Remaps normalizedFear within the current rank's range and lerps between min/max volume.
    private float GetHeartbeatVolume(float normalizedFear)
    {
        float fearMin, fearMax, volMin, volMax;

        if (currentRank == FearBar.FearRank.Low)
        {
            fearMin = LowFearMin; fearMax = LowFearMax;
            volMin = lowVolumeMin; volMax = lowVolumeMax;
        }
        else
        {
            fearMin = MediumFearMin; fearMax = MediumFearMax;
            volMin = mediumVolumeMin; volMax = mediumVolumeMax;
        }

        float t = Mathf.InverseLerp(fearMin, fearMax, normalizedFear);
        return Mathf.Lerp(volMin, volMax, t);
    }
}
using System;
using UnityEngine;

public class MusicTrigger : MonoBehaviour
{
    [SerializeField] private SoundDataSO music;
    [SerializeField] private float crossfade;
    public enum MusicMode { FixedDuration, PlayUntil, PlayEntirety};
    [SerializeField] private MusicMode mode = MusicMode.FixedDuration;

    public event Action TriggerMusicChange;
    public event Action TriggerMusicSting;

    // method designed to change music to a new track
    // by default, starts the previous track up when the changed music ends.
    private void ChangeMusic(bool returnToPrev = true)
    {
        
    }

    // method designed to play a single moment of music
    // dips the main track volume by a percentage to play more clearly
    private void MusicSting(float musicFade = 0.5f)
    {

    }
}

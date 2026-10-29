using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class MusicTrigger : MonoBehaviour
{
    public enum MusicMode { FixedDuration, AudioDuration, PlayUntil };

    [SerializeField] private MusicMode mode = MusicMode.FixedDuration;
    [SerializeField] private MusicDataSO music;
    [SerializeField] private float crossfade;
    [SerializeField] private EnemySpawnPoint spawner;
    [SerializeField] private float fixedDurationTime = 30f;

    void Awake()
    {
        spawner.EnemySpawned += BossMusic;
    }

    // method designed to change music to a new track
    // by default, starts the previous track up when the changed music ends.
    private void ChangeMusic()
    {
        bool loop;
        // if using audio duration, dont loop.
        if (mode == MusicMode.FixedDuration)
        {
            StartCoroutine(FixedDuration());
            loop = true;
        } 
        else if (mode == MusicMode.PlayUntil)
        {
            StartCoroutine(PlayUntil());
            loop = true;
        }
        else loop = false;

        MusicManager.ChangeMusic(music, 0, loop, crossfade, false);
    }

    private void BossMusic(Enemy enemy)
    {
        ChangeMusic();
    }

    private IEnumerator PlayUntil()
    {
        yield return new WaitUntil(() => mode == MusicMode.AudioDuration);
    } 

    // Limitation: currently tied to realtime
    private IEnumerator FixedDuration()
    {
        
        yield return new WaitForSeconds(fixedDurationTime);
    } 

    // method designed to play a single moment of music
    // dips the main track volume by a percentage to play more clearly
    private void MusicSting(float musicFade = 0.5f)
    {

    }
}

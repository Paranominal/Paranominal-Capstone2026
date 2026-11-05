using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class MusicTrigger : MonoBehaviour
{
    public enum MusicMode { FixedDuration, AudioDuration, PlayUntil };

    [SerializeField] private MusicMode mode = MusicMode.FixedDuration;
    [SerializeField] private List<MusicDataSO> music;
    [SerializeField] private float crossfade;
    [SerializeField] private EnemySpawnPoint spawner;
    [SerializeField] private EnemySpawnPoint_Standalone standaloneSpawner;
    [SerializeField] private bool useTrigger;
    [SerializeField, ShowIf("useTrigger", true)] private LayerMask playerLayer;
    [SerializeField] private float fixedDurationTime = 30f;
    [SerializeField] private bool onlyTriggerOnce;
    private bool dontTriggerAgain;

    void Awake()
    {
        if (spawner)
        {
            spawner.EnemySpawned += BossMusic;
            spawner.EnemyDied += BossMusic; 
        }
        else if (standaloneSpawner)
        {
            standaloneSpawner.EnemySpawned += BossMusic;
            standaloneSpawner.EnemyDied += BossMusic;
        }
    }

    // method designed to change music to a new track
    // by default, starts the previous track up when the changed music ends.
    private void ChangeMusic(MusicDataSO track)
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

        MusicManager.ChangeMusic(track, 0, loop, crossfade, false);

        if (onlyTriggerOnce) dontTriggerAgain = true;
    }

    private void BossMusic(Enemy enemy)
    {
        ChangeMusic(music[0]);
    }

    private void EnemyDiesMusic(Enemy enemy)
    {
        ChangeMusic(music[1]);
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

    private void OnTriggerEnter(Collider other)
    {
        if (dontTriggerAgain) return;

        if (playerLayer.Contains(other.gameObject.layer))
        {
            ChangeMusic(music[0]);
        }
    }
}

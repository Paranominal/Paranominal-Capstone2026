using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] private AudioMixerGroup mix;
    [SerializeField] private bool playOnAwake;
    [SerializeField] private List<MusicDataSO> music;
    [SerializeField] private List<AudioSource> tracks;

    // pool of all audio loaded to memory, for cleanup later
    // private List<AudioClip> loadedMusic;

    void Reset()
    {
        if (tracks.Count == 0) tracks = GetComponents<AudioSource>().ToList();
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning($"[{this}] There can only be one MusicManager");
            Destroy(this);
        }

        if (playOnAwake) PlayOnAwake();
    }
    
    // public method for Events to trigger. 
    public static void PlayMusic(MusicDataSO music, int track, bool loop, bool skipBuild = false)
    {
        Instance.StartCoroutine(Instance.PlayMusicCoroutine(music, Instance.tracks[track], loop, skipBuild)); // int reference limits references to sources not on the Music Manager.
    }

    // public method for Events to trigger.
    public static void ChangeMusic(MusicDataSO music, int track, bool loop, float transitionDuration = 1f, bool skipBuild = false)
    {
        Instance.StartCoroutine(Instance.ChangeMusicCoroutine(music, Instance.tracks[track], loop, transitionDuration, skipBuild)); // int reference limits references to sources not on the Music Manager.
    }

    // // public method for Events to trigger.
    // public static void CrossFade(AudioSource prev, AudioSource next, float duration, bool doReplace = false)
    // {
    //     Instance.StartCoroutine(CrossFadeTracksCoroutine(prev, next, duration, doReplace));
    // }

    // set and play music from one of the layered tracks
    private IEnumerator PlayMusicCoroutine(MusicDataSO music, AudioSource track, bool loop, bool skipBuild = false)
    {
        //check for valid music clips
        if (!IsMusicValid(music)) yield break;

        // // load main clip
        // LoadClip(music.MainClip);
        // (if) load build clip
        if (music.HasBuildClip && !skipBuild)
        {
            // LoadClip(music.BuildClip);
            // // wait for both clips to load to memory
            yield return new WaitUntil(() =>
                ClipIsLoaded(music.MainClip) &&
                ClipIsLoaded(music.BuildClip));
            // load the build clip into the audio track
            SetTrackAudio(music.BuildClip, track);
            // dont loop the build clip
            track.loop = false;
            // play 
            if (!track.isPlaying) track.Play();
            // wait until the build clip ends
            yield return new WaitUntil(() => track.time >= music.BuildClip.length | !track.isPlaying);
        }
        // (else) only wait for main clip to load to memory if theres no build track
        else yield return new WaitUntil(() => ClipIsLoaded(music.MainClip));
        // load the main clip into the audio track
        SetTrackAudio(music.MainClip, track);
        // play the track
        if (!track.isPlaying) track.Play();
        // set loop state
        track.loop = loop;
    }

    // crossfade in new music, replacing the previous track
    public IEnumerator ChangeMusicCoroutine(MusicDataSO music, AudioSource track, bool loop, float transitionDuration = 1f, bool skipBuild = false)
    {
        //check for valid music data
        if (!IsMusicValid(music)) yield break;

        // create a new unpooled music track to handle transition, that will be pulled in during CrossFade
        AudioSource newTrack = NewAudioTrack();

        // start a coroutine that handles the audio transition and track replacement on its own
        Instance.StartCoroutine(CrossFadeCoroutine(track, newTrack, transitionDuration, true));

        // load main clip
        Instance.StartCoroutine(Instance.PlayMusicCoroutine(music, newTrack, loop));
    }

    // crossfades between two audio tracks. 
    // doReplace replaces the fade-out track's layer with the fade-in track
    private static IEnumerator CrossFadeCoroutine(AudioSource prev, AudioSource next, float duration, bool doReplace = false)
    {
        float elapsedTime = 0f;
        // cache existing volume level
        float cachedVolume = prev.volume;
        // during crossfade duration do crossfade
        while (elapsedTime < duration && duration != 0)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            // lerrrrrrp
            prev.volume = Mathf.Lerp(cachedVolume, 0, t);
            next.volume = Mathf.Lerp(0, cachedVolume, t);
            yield return null;
        }
        prev.volume = 0;
        next.volume = cachedVolume;

        // replace the now empty layer with the new one
        int i = Instance.tracks.IndexOf(prev);
        if (Instance.tracks.Contains(next)) Instance.tracks.RemoveAt(Instance.tracks.IndexOf(next));
        Instance.tracks[i] = next;
        Destroy(prev);
    }

    // a way to swap out clips instantly without a transition
    // returns the audio track being set
    public static AudioSource SetTrackAudio(AudioClip clip, AudioSource track) 
    {
        track.clip = clip;
        return track;
    }

    public static AudioSource NewAudioTrack(bool layerNewTrack = true)
    {
        AudioSource newTrack = Instance.gameObject.AddComponent<AudioSource>();
        newTrack.outputAudioMixerGroup = Instance.mix;
        if (layerNewTrack)
        {
            Debug.Log($"[{Instance.gameObject}.{Instance}] Created new Layered Audio Track ({newTrack})");
            Instance.tracks.Add(newTrack);
        }
        else Debug.Log($"[{Instance.gameObject}.{Instance}] Created new unpooled Audio Track ({newTrack})");
        return newTrack;
    }

    private float mutedVolumeCache = 1;
    private bool muted = false;
    public static void ToggleMuteMusic()
    {
        if (!Instance.muted)
        {
            Instance.mix.audioMixer.SetFloat("Music", 0);
            Instance.muted = true;
        }
        else
        {
            Instance.mix.audioMixer.SetFloat("Music", Instance.mutedVolumeCache);
            Instance.muted = true;
        }
    }

    public static void PauseMusic()
    {
        foreach (AudioSource track in Instance.tracks)
        {
            track.Pause();
        }
    }

    public static void PlayOrResumeMusic()
    {
        foreach (AudioSource track in Instance.tracks)
        {
            track.Play(); //   <-- this restarts music i think
        }
    }

    public static void StopMusic()
    {
        foreach (AudioSource track in Instance.tracks)
        {
            track.Stop();
        }
    }

    public static void PauseTrack(AudioSource track)
    {
        track.Pause();
    }

    public static void PlayOrResumeTrack(AudioSource track)
    {
        track.Play(); //   <-- this restarts music i think
    }

    public static void StopTrack(AudioSource track)
    {
        track.Stop();
    }
         
    private static bool ClipIsLoaded(AudioClip clip)
    {
        return clip.loadState == AudioDataLoadState.Loaded;
    }
    private static bool ClipIsLoading(AudioClip clip)
    {
        return clip.loadState == AudioDataLoadState.Loading;
    }
    private static bool ClipIsUnloaded(AudioClip clip)
    {
        return clip.loadState == AudioDataLoadState.Unloaded;
    }
    private static bool ClipFailedToLoad(AudioClip clip)
    {
        return clip.loadState == AudioDataLoadState.Failed;
    }
    private static bool ClipReadyToLoad(AudioClip clip)
    {
        if (clip == null) return false;
        else if (clip.loadState == AudioDataLoadState.Failed) return false;
        else if (clip.loadState == AudioDataLoadState.Loaded) return false;
        else if (clip.loadState == AudioDataLoadState.Loading) return false;
        else return true;
    }

    private static bool IsMusicValid(MusicDataSO music)
    {
        // check for invalid MusicData
        if (music.MainClip == null | music.HasBuildClip && music.BuildClip == null)
        {
            Debug.LogError($"[{Instance.gameObject}] Attempted to Play invalid MusicData ({music})! Play Track failed. Check that audio clips are set correctly.");
            return false;
        }
        else return true;
    }

    private static AudioClip LoadClip(AudioClip clip)
    {
        if (ClipReadyToLoad(clip))
        {
            clip.LoadAudioData();
            // loadedMusic.Add(clip); //     <---- the problem
        }
        return clip;
    }

    // private static AudioClip UnloadClip(AudioClip clip)
    // {
    //     if (ClipIsLoaded(clip))
    //     {
    //         clip.UnloadAudioData();
    //         Instance.loadedMusic.Remove(clip);
    //     }
    //     return clip;
    // }
    
    private static void PlayOnAwake()
    {
        if (Instance.music[0] == null)
        {
            Debug.LogWarning($"[{Instance.gameObject}] Attempted PlayOnAwake with no Music Clip set. Process cancelled.");
            return;
        }
        if (Instance.tracks[0] == null)
        {
            Debug.LogWarning($"[{Instance.gameObject}] Attempted PlayOnAwake with no Track Layers set. Process cancelled.");
            return;
        }
       Instance. StartCoroutine(Instance.PlayMusicCoroutine(Instance.music[0], Instance.tracks[0], Instance.music[0].LoopMainClip));
    }
}

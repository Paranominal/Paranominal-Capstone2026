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

    [SerializeField] private AudioMixerGroup musicMix;
    [SerializeField] private bool playOnAwake;
    [SerializeField] private List<MusicDataSO> musicClips;
    [SerializeField] private List<AudioSource> trackLayers;

    // pool of all audio loaded to memory, for cleanup later
    private List<AudioClip> loadedMusic;

    void Reset()
    {
        if (trackLayers.Count == 0) trackLayers = GetComponents<AudioSource>().ToList();
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
    public static void PlayMusic(MusicDataSO music, int layer, bool loop, bool skipBuild = false)
    {
        Instance.StartCoroutine(Instance.PlayMusicCoroutine(music, Instance.trackLayers[layer], loop, skipBuild)); // int reference limits references to sources not on the Music Manager.
    }

    // public method for Events to trigger.
    public static void ChangeMusic(MusicDataSO music, int layer, bool loop, float transitionDuration = 1f, bool skipBuild = false)
    {
        Instance.StartCoroutine(Instance.ChangeMusicCoroutine(music, Instance.trackLayers[layer], loop, transitionDuration, skipBuild)); // int reference limits references to sources not on the Music Manager.
    }

    // public method for Events to trigger.
    private static void  CrossFadeTracks(AudioSource prev, AudioSource next, float duration, bool doReplace = false)
    {
        Instance.StartCoroutine(CrossFadeTracksCoroutine(prev, next, duration, doReplace));
    }

    // set and play music from one of the layered tracks
    private IEnumerator PlayMusicCoroutine(MusicDataSO music, AudioSource track, bool loop, bool skipBuild = false)
    {
        //check for valid music clips
        if (!IsMusicValid(music)) yield break;

        // load main clip
        LoadClip(music.MainClip);
        // (if) load build clip
        if (music.HasBuildClip && !skipBuild)
        {
            LoadClip(music.BuildClip);
            // wait for both clips to load to memory
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
        Instance.StartCoroutine(CrossFadeTracksCoroutine(track, newTrack, transitionDuration, true));

        // load main clip
        Instance.StartCoroutine(Instance.PlayMusicCoroutine(music, newTrack, loop));
    }

    // crossfades between two audio tracks. 
    // doReplace replaces the fade-out track's layer with the fade-in track
    private static IEnumerator CrossFadeTracksCoroutine(AudioSource prev, AudioSource next, float duration, bool doReplace = false)
    {
        float elapsedTime = 0f;
        while (prev.isPlaying)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            // cache existing volume level
            float cachedVolume = prev.volume;
            // lerrrrrrp
            prev.volume = Mathf.Lerp(cachedVolume, 0, t);
            next.volume = Mathf.Lerp(0, cachedVolume, t);
            yield return null;
        }

        // set to avoid rounding errors (is this even necessary??)
        prev.volume = 0;
        next.volume = 1;

        // replace the now empty layer with the new one
        int i = Instance.trackLayers.IndexOf(prev);
        if (Instance.trackLayers.Contains(next)) Instance.trackLayers.RemoveAt(Instance.trackLayers.IndexOf(next));
        Instance.trackLayers[i] = next;
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
        newTrack.outputAudioMixerGroup = Instance.musicMix;
        if (layerNewTrack)
        {
            Debug.Log($"[{Instance.gameObject}.{Instance}] Created new Layered Audio Track ({newTrack})");
            Instance.trackLayers.Add(newTrack);
        }
        else Debug.Log($"[{Instance.gameObject}.{Instance}] Created new unpooled Audio Track ({newTrack})");
        return newTrack;
    }

    public static void PauseMusic()
    {
        foreach (AudioSource track in Instance.trackLayers)
        {
            track.Pause();
        }
    }

    public static void PlayOrResumeMusic()
    {
        foreach (AudioSource track in Instance.trackLayers)
        {
            track.Play();
        }
    }

    public static void StopMusic()
    {
        foreach (AudioSource track in Instance.trackLayers)
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
        track.Play();
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

    private static AudioClip UnloadClip(AudioClip clip)
    {
        if (ClipIsLoaded(clip))
        {
            clip.UnloadAudioData();
            Instance.loadedMusic.Remove(clip);
        }
        return clip;
    }
    
    private static void PlayOnAwake()
    {
        if (Instance.musicClips[0] == null)
        {
            Debug.LogWarning($"[{Instance.gameObject}] Attempted PlayOnAwake with no Music Clip set. Process cancelled.");
            return;
        }
        if (Instance.trackLayers[0] == null)
        {
            Debug.LogWarning($"[{Instance.gameObject}] Attempted PlayOnAwake with no Track Layers set. Process cancelled.");
            return;
        }
       Instance. StartCoroutine(Instance.PlayMusicCoroutine(Instance.musicClips[0], Instance.trackLayers[0], Instance.musicClips[0].LoopMainClip));
    }
}

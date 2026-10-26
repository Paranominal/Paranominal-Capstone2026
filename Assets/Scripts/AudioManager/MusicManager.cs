using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
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
        if (playOnAwake)
        {
            if (musicClips[0] == null)
            {
                Debug.LogWarning($"[{gameObject}] Attempted PlayOnAwake with no Music Clip set. Process cancelled.");
                return;
            }
            if (trackLayers[0] == null)
            {
                Debug.LogWarning($"[{gameObject}] Attempted PlayOnAwake with no Track Layers set. Process cancelled.");
                return;
            }
            StartCoroutine(PlayMusicCoroutine(musicClips[0], trackLayers[0], musicClips[0].LoopMainClip));
        }
    }
    
    // public method for Events to trigger. 
    public void PlayMusic(MusicDataSO music, int layer, bool loop, bool skipBuild = false)
    {
        StartCoroutine(PlayMusicCoroutine(music, trackLayers[layer], loop, skipBuild)); // int reference limits references to sources not on the Music Manager.
    }

    // public method for Events to trigger.
    public void ChangeMusic(MusicDataSO music, int layer, bool loop, float transitionDuration = 1f, bool skipBuild = false)
    {
        StartCoroutine(ChangeMusicCoroutine(music, trackLayers[layer], loop, transitionDuration, skipBuild)); // int reference limits references to sources not on the Music Manager.
    }

    // public method for Events to trigger.
    private void  CrossFadeTracks(AudioSource prev, AudioSource next, float duration, bool doReplace = false)
    {
        StartCoroutine(CrossFadeTracksCoroutine(prev, next, duration, doReplace));
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
        StartCoroutine(CrossFadeTracksCoroutine(track, newTrack, transitionDuration, true));

        // load main clip
        StartCoroutine(PlayMusicCoroutine(music, newTrack, loop));
    }

    // crossfades between two audio tracks. 
    // doReplace replaces the fade-out track's layer with the fade-in track
    private IEnumerator CrossFadeTracksCoroutine(AudioSource prev, AudioSource next, float duration, bool doReplace = false)
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
        int i = trackLayers.IndexOf(prev);
        if (trackLayers.Contains(next)) trackLayers.RemoveAt(trackLayers.IndexOf(next));
        trackLayers[i] = next;
        Destroy(prev);
    }

    // a way to swap out clips instantly without a transition
    // returns the audio track being set
    public AudioSource SetTrackAudio(AudioClip clip, AudioSource track) 
    {
        track.clip = clip;
        return track;
    }

    public AudioSource NewAudioTrack(bool layerNewTrack = true)
    {
        AudioSource newTrack = gameObject.AddComponent<AudioSource>();
        newTrack.outputAudioMixerGroup = musicMix;
        if (layerNewTrack)
        {
            Debug.Log($"[{gameObject}.{this}] Created new Layered Audio Track ({newTrack})");
            trackLayers.Add(newTrack);
        }
        else Debug.Log($"[{gameObject}.{this}] Created new unpooled Audio Track ({newTrack})");
        return newTrack;
    }

    public void PauseMusic()
    {
        foreach (AudioSource track in trackLayers)
        {
            track.Pause();
        }
    }

    public void PlayOrResumeMusic()
    {
        foreach (AudioSource track in trackLayers)
        {
            track.Play();
        }
    }

    public void StopMusic()
    {
        foreach (AudioSource track in trackLayers)
        {
            track.Stop();
        }
    }

    public void PauseTrack(AudioSource track)
    {
        track.Pause();
    }

    public void PlayOrResumeTrack(AudioSource track)
    {
        track.Play();
    }

    public void StopTrack(AudioSource track)
    {
        track.Stop();
    }
         
    private bool ClipIsLoaded(AudioClip clip)
    {
        return clip.loadState == AudioDataLoadState.Loaded;
    }
    private bool ClipIsLoading(AudioClip clip)
    {
        return clip.loadState == AudioDataLoadState.Loading;
    }
    private bool ClipIsUnloaded(AudioClip clip)
    {
        return clip.loadState == AudioDataLoadState.Unloaded;
    }
    private bool ClipFailedToLoad(AudioClip clip)
    {
        return clip.loadState == AudioDataLoadState.Failed;
    }
    private bool ClipReadyToLoad(AudioClip clip)
    {
        if (clip == null) return false;
        else if (clip.loadState == AudioDataLoadState.Failed) return false;
        else if (clip.loadState == AudioDataLoadState.Loaded) return false;
        else if (clip.loadState == AudioDataLoadState.Loading) return false;
        else return true;
    }

    private bool IsMusicValid(MusicDataSO music)
    {
        // check for invalid MusicData
        if (music.MainClip == null | music.HasBuildClip && music.BuildClip == null)
        {
            Debug.LogError($"[{gameObject}] Attempted to Play invalid MusicData ({music})! Play Track failed. Check that audio clips are set correctly.");
            return false;
        }
        else return true;
    }

    private AudioClip LoadClip(AudioClip clip)
    {
        if (ClipReadyToLoad(clip))
        {
            clip.LoadAudioData();
            // loadedMusic.Add(clip);
        }
        return clip;
    }

    private AudioClip UnloadClip(AudioClip clip)
    {
        if (ClipIsLoaded(clip))
        {
            clip.UnloadAudioData();
            loadedMusic.Remove(clip);
        }
        return clip;
    }
}

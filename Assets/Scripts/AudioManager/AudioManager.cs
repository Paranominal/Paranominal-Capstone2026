using UnityEngine;
using System;
using System.Collections.Generic;

public enum SoundType
{
    TEST,
    UI_HOVER,
    UI_CLICK,
    UI_GAMESTART,
    UI_OPTIONS,
    UI_QUIT,
}

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [SerializeField] private SoundList[] soundList;
    private static AudioManager instance;
    private AudioSource audioSource;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public static void PlaySound(SoundType sound, float volume = 1)
    {
        AudioClip[] clips = instance.soundList[(int)sound].sounds;
        AudioClip randomClip = clips[UnityEngine.Random.Range(0, clips.Length)];
        instance.audioSource.PlayOneShot(randomClip, volume);
    }

    public void Resize()
    {
        Dictionary<string, AudioClip[]> clips = new();
        for (int i = 0; i < soundList.Length; ++i)
        {
            if (soundList[i].sounds.Length > 0)
                clips.Add(soundList[i].name, soundList[i].sounds);
        }

        string[] names = Enum.GetNames(typeof(SoundType));
        Array.Resize(ref soundList, names.Length);
        for (int i = 0; i < soundList.Length; i++)
        {
            string currentName = names[i];
            soundList[i].name = currentName;
            if (clips.ContainsKey(currentName))
                soundList[i].sounds = clips[currentName];
            else
                soundList[i].sounds = null;
        }
    }
}

[Serializable]
public struct SoundList
{
    [HideInInspector] public string name;
    public AudioClip[] sounds;
}
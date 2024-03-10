using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : Singleton<AudioManager>
{
    public enum AudioType
    {
        MUSIC,
        FX
    }
    public List<AudioSource> sources = new List<AudioSource>();
    public GameObject sourceObject;
    public AudioLibrary library;
    public AudioMixerGroup[] mixers;
    public AudioSource oneShotMusic;
    public AudioSource oneShotFx;

    public override void Awake()
    {
        base.Awake();
        oneShotMusic = sourceObject.AddComponent<AudioSource>();
        oneShotMusic.outputAudioMixerGroup = mixers[0];
        oneShotFx = sourceObject.AddComponent<AudioSource>();
        oneShotFx.outputAudioMixerGroup = mixers[1];
    }

    public void PlayAudio(AudioClip clip, AudioType type, float fadeDuration, bool loop = true, float delay = 0, float volume = 1, bool stopOnZero = false)
    {
        //use an old one or create a new one
        AudioSource newMusic = null;
        foreach (AudioSource s in sources)
        {
            if (s.clip == clip)
            {
                newMusic = s;
                break;
            }
        }
        if(newMusic == null)
            newMusic = sourceObject.AddComponent<AudioSource>();

        newMusic.outputAudioMixerGroup = mixers[(int)type];
        newMusic.clip = clip;
        newMusic.loop = loop;
        newMusic.volume = 0;
        StartCoroutine(newMusic.FadeAudioSource(volume, fadeDuration, false, delay, stopOnZero));
        sources.Add(newMusic);
    }

    public void StopAudio(AudioClip clip, float fadeDuration, float delay = 0)
    {
        foreach(var s in sources)
        {
            if (s.clip == clip)
            {
                StartCoroutine(s.FadeAudioSource(0, fadeDuration, true, delay));
                sources.Remove(s);
                return;
            }
        }
        Debug.LogWarning($"{clip.name} wasn't playing, so I can't stop it!");
    }


    public void PlayMouseClick() => oneShotFx.PlayOneShot(library.mouseClick);
    public void PlayButtonPress() => oneShotFx.PlayOneShot(library.buttonClick);
    public void PlayPlayPress() => oneShotFx.PlayOneShot(library.buttonPlayClick);
    public void PlayTickPress() => oneShotFx.PlayOneShot(library.buttonTickClick);
    public void PlayButtonBackPress() => oneShotFx.PlayOneShot(library.buttonBackClick);
    public void PlayBuyModule() => oneShotFx.PlayOneShot(library.buyModule);
    public void PlayEquipModule() => oneShotFx.PlayOneShot(library.equipModule);
    public void PlayUnequipModule() => oneShotFx.PlayOneShot(library.unequipModule, 2.5f);
}

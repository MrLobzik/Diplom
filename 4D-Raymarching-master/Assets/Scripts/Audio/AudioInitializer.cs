using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioInitializer : MonoBehaviour
{
    [Header("Sound Database")]
    [SerializeField] private SoundEntry[] sounds;

    [Header("Music Database")]
    [SerializeField] private MusicEntry[] musicTracks;

    [Header("Default Volumes")]
    [Range(0f, 1f)] public float defaultMasterVolume = 1f;
    [Range(0f, 1f)] public float defaultMusicVolume = 0.7f;
    [Range(0f, 1f)] public float defaultSFXVolume = 1f;
    [Range(0f, 1f)] public float defaultUIVolume = 1f;
    [Range(0f, 1f)] public float defaultAmbientVolume = 0.8f;
    [Range(0f, 1f)] public float defaultVoiceVolume = 1f;

    [System.Serializable]
    public class SoundEntry
    {
        public string id;
        public AudioClip[] clips;
        public AudioMixerGroupType mixerGroup = AudioMixerGroupType.SFX;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        [Range(0f, 1f)] public float pitchVariation = 0.1f;
        [Range(0f, 1f)] public float spatialBlend = 0f;
        public float minDistance = 1f;
        public float maxDistance = 50f;
        public bool loop = false;
        public int priority = 128;
    }

    [System.Serializable]
    public class MusicEntry
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 0.7f;
        public bool loop = true;
        public int priority = 100;
    }

    public enum AudioMixerGroupType
    {
        SFX,
        UI,
        Ambient,
        Voice
    }

    private void Start()
    {
        InitializeAudio();
    }

    private void InitializeAudio()
    {
        AudioManager manager = AudioManager.Instance;

        // Регистрируем звуки
        foreach (var sound in sounds)
        {
            var data = new AudioManager.SoundData
            {
                id = sound.id,
                clips = sound.clips,
                mixerGroup = GetMixerGroup(sound.mixerGroup),
                volume = sound.volume,
                pitch = sound.pitch,
                pitchVariation = sound.pitchVariation,
                spatialBlend = sound.spatialBlend,
                minDistance = sound.minDistance,
                maxDistance = sound.maxDistance,
                loop = sound.loop,
                priority = sound.priority
            };

            manager.RegisterSound(data);
        }

        // Регистрируем музыку
        foreach (var music in musicTracks)
        {
            var track = new AudioManager.MusicTrack
            {
                id = music.id,
                clip = music.clip,
                mixerGroup = manager.musicGroup,
                volume = music.volume,
                loop = music.loop,
                priority = music.priority
            };

            manager.RegisterMusic(track);
        }

        // Устанавливаем громкость по умолчанию
        manager.SetMasterVolume(defaultMasterVolume);
        manager.SetMusicVolume(defaultMusicVolume);
        manager.SetSFXVolume(defaultSFXVolume);
        manager.SetUIVolume(defaultUIVolume);
        manager.SetAmbientVolume(defaultAmbientVolume);
        manager.SetVoiceVolume(defaultVoiceVolume);

        // Загружаем сохраненные настройки
        LoadAudioSettings();
    }

    private UnityEngine.Audio.AudioMixerGroup GetMixerGroup(AudioMixerGroupType type)
    {
        AudioManager manager = AudioManager.Instance;

        switch (type)
        {
            case AudioMixerGroupType.SFX:
                return manager.sfxGroup;
            case AudioMixerGroupType.UI:
                return manager.uiGroup;
            case AudioMixerGroupType.Ambient:
                return manager.ambientGroup;
            case AudioMixerGroupType.Voice:
                return manager.voiceGroup;
            default:
                return manager.sfxGroup;
        }
    }

    private void LoadAudioSettings()
    {
        AudioManager manager = AudioManager.Instance;

        float masterVol = PlayerPrefs.GetFloat("MasterVolume", defaultMasterVolume);
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", defaultMusicVolume);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", defaultSFXVolume);
        float uiVol = PlayerPrefs.GetFloat("UIVolume", defaultUIVolume);
        float ambientVol = PlayerPrefs.GetFloat("AmbientVolume", defaultAmbientVolume);
        float voiceVol = PlayerPrefs.GetFloat("VoiceVolume", defaultVoiceVolume);

        manager.SetMasterVolume(masterVol);
        manager.SetMusicVolume(musicVol);
        manager.SetSFXVolume(sfxVol);
        manager.SetUIVolume(uiVol);
        manager.SetAmbientVolume(ambientVol);
        manager.SetVoiceVolume(voiceVol);
    }

    public void SaveAudioSettings()
    {
        AudioManager manager = AudioManager.Instance;

        PlayerPrefs.SetFloat("MasterVolume", manager.GetMasterVolume());
        PlayerPrefs.SetFloat("MusicVolume", manager.GetMusicVolume());
        PlayerPrefs.SetFloat("SFXVolume", manager.GetSFXVolume());
        PlayerPrefs.Save();
    }
}


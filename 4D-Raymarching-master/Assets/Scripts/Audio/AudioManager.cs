using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    #region Singleton
    private static AudioManager _instance;
    public static AudioManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<AudioManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("[AudioManager]");
                    _instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }
    #endregion

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Mixer Groups")]
    public AudioMixerGroup masterGroup;
    public AudioMixerGroup musicGroup;
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup uiGroup;
    public AudioMixerGroup ambientGroup;
    public AudioMixerGroup voiceGroup;

    [Header("Audio Sources Pools")]
    [SerializeField] private int initialPoolSize = 20;
    [SerializeField] private int maxPoolSize = 50;

    [Header("Music Settings")]
    [SerializeField] private float musicFadeTime = 1f;
    [SerializeField] private float musicCrossFadeTime = 2f;

    [Header("3D Audio Settings")]
    [SerializeField] private float spatialBlend = 1f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 50f;
    [SerializeField] private AudioRolloffMode rolloffMode = AudioRolloffMode.Logarithmic;

    // Пул аудио источников
    private Queue<AudioSource> availableSources = new Queue<AudioSource>();
    private List<AudioSource> activeSources = new List<AudioSource>();
    private Transform poolContainer;

    // Музыкальные источники
    private AudioSource musicSourceA;
    private AudioSource musicSourceB;
    private bool isMusicSourceAActive = true;

    // Очередь музыки
    private Queue<AudioClip> musicQueue = new Queue<AudioClip>();
    private bool isMusicQueuePlaying;

    // Коллекции звуков
    private Dictionary<string, SoundData> soundDatabase = new Dictionary<string, SoundData>();
    private Dictionary<string, MusicTrack> musicDatabase = new Dictionary<string, MusicTrack>();

    // Настройки громкости
    private const string MASTER_VOLUME = "MasterVolume";
    private const string MUSIC_VOLUME = "MusicVolume";
    private const string SFX_VOLUME = "SFXVolume";
    private const string UI_VOLUME = "UIVolume";
    private const string AMBIENT_VOLUME = "AmbientVolume";
    private const string VOICE_VOLUME = "VoiceVolume";

    // События
    public event Action<string> OnMusicTrackChanged;
    public event Action OnMusicStopped;
    public event Action<float> OnMasterVolumeChanged;
    public event Action<float> OnMusicVolumeChanged;
    public event Action<float> OnSFXVolumeChanged;

    [System.Serializable]
    public class SoundData
    {
        public string id;
        public AudioClip[] clips;
        public AudioMixerGroup mixerGroup;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.1f, 3f)] public float pitch = 1f;
        [Range(0f, 1f)] public float pitchVariation = 0.1f;
        [Range(0f, 1f)] public float spatialBlend = 0f;
        public float minDistance = 1f;
        public float maxDistance = 50f;
        public bool loop = false;
        public bool playOnAwake = false;
        public float delay = 0f;
        public int priority = 128;
    }

    [System.Serializable]
    public class MusicTrack
    {
        public string id;
        public AudioClip clip;
        public AudioMixerGroup mixerGroup;
        [Range(0f, 1f)] public float volume = 0.7f;
        public bool loop = true;
        public float fadeInTime = 1f;
        public float fadeOutTime = 1f;
        public int priority = 100;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioManager();
    }

    private void InitializeAudioManager()
    {
        // Создаем контейнер для пула
        poolContainer = new GameObject("AudioSourcePool").transform;
        poolContainer.SetParent(transform);

        // Создаем музыкальные источники
        CreateMusicSources();

        // Инициализируем пул
        InitializePool();
    }

    private void CreateMusicSources()
    {
        GameObject musicObjA = new GameObject("MusicSourceA");
        musicObjA.transform.SetParent(transform);
        musicSourceA = musicObjA.AddComponent<AudioSource>();
        musicSourceA.outputAudioMixerGroup = musicGroup;
        musicSourceA.spatialBlend = 0f;

        GameObject musicObjB = new GameObject("MusicSourceB");
        musicObjB.transform.SetParent(transform);
        musicSourceB = musicObjB.AddComponent<AudioSource>();
        musicSourceB.outputAudioMixerGroup = musicGroup;
        musicSourceB.spatialBlend = 0f;
    }

    private void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            CreateNewAudioSource();
        }
    }

    private AudioSource CreateNewAudioSource()
    {
        GameObject go = new GameObject($"AudioSource_{availableSources.Count}");
        go.transform.SetParent(poolContainer);

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = rolloffMode;

        go.SetActive(false);
        availableSources.Enqueue(source);

        return source;
    }

    private AudioSource GetAudioSource()
    {
        AudioSource source = null;

        // Пытаемся получить из пула
        while (availableSources.Count > 0)
        {
            source = availableSources.Dequeue();
            if (source != null)
            {
                source.gameObject.SetActive(true);
                return source;
            }
        }

        // Создаем новый если не превышен лимит
        if (activeSources.Count < maxPoolSize)
        {
            source = CreateNewAudioSource();
            source.gameObject.SetActive(true);
            return source;
        }

        // Переиспользуем самый старый активный источник
        if (activeSources.Count > 0)
        {
            source = activeSources[0];
            source.Stop();
            activeSources.RemoveAt(0);
            return source;
        }

        return null;
    }

    private void ReturnToPool(AudioSource source)
    {
        if (source == null) return;

        source.Stop();
        source.clip = null;
        source.gameObject.SetActive(false);
        source.transform.SetParent(poolContainer);

        activeSources.Remove(source);
        availableSources.Enqueue(source);
    }

    #region Sound Database Management

    public void RegisterSound(SoundData soundData)
    {
        if (soundDatabase.ContainsKey(soundData.id))
        {
            Debug.LogWarning($"Sound with ID '{soundData.id}' already registered. Overwriting.");
            soundDatabase[soundData.id] = soundData;
        }
        else
        {
            soundDatabase.Add(soundData.id, soundData);
        }
    }

    public void RegisterMusic(MusicTrack musicTrack)
    {
        if (musicDatabase.ContainsKey(musicTrack.id))
        {
            Debug.LogWarning($"Music with ID '{musicTrack.id}' already registered. Overwriting.");
            musicDatabase[musicTrack.id] = musicTrack;
        }
        else
        {
            musicDatabase.Add(musicTrack.id, musicTrack);
        }
    }

    public void UnregisterSound(string id)
    {
        soundDatabase.Remove(id);
    }

    public void UnregisterMusic(string id)
    {
        musicDatabase.Remove(id);
    }

    #endregion

    #region Sound Playback

    /// <summary>
    /// Проигрывает звук по ID
    /// </summary>
    public AudioSource PlaySound(string soundId, Vector3 position = default, Transform parent = null)
    {
        if (!soundDatabase.ContainsKey(soundId))
        {
            Debug.LogWarning($"Sound with ID '{soundId}' not found in database.");
            return null;
        }

        return PlaySoundData(soundDatabase[soundId], position, parent);
    }

    /// <summary>
    /// Проигрывает звук напрямую из AudioClip
    /// </summary>
    public AudioSource PlayClip(AudioClip clip, AudioMixerGroup mixerGroup = null,
        Vector3 position = default, float volume = 1f, float pitch = 1f, bool loop = false)
    {
        SoundData data = new SoundData
        {
            id = "direct",
            clips = new AudioClip[] { clip },
            mixerGroup = mixerGroup ?? sfxGroup,
            volume = volume,
            pitch = pitch,
            loop = loop
        };

        return PlaySoundData(data, position);
    }

    private AudioSource PlaySoundData(SoundData data, Vector3 position = default, Transform parent = null)
    {
        if (data.clips == null || data.clips.Length == 0)
        {
            Debug.LogWarning($"No clips found for sound '{data.id}'.");
            return null;
        }

        AudioSource source = GetAudioSource();
        if (source == null) return null;

        // Выбираем случайный клип если их несколько
        AudioClip clip = data.clips[UnityEngine.Random.Range(0, data.clips.Length)];

        // Настраиваем источник
        source.clip = clip;
        source.outputAudioMixerGroup = data.mixerGroup;
        source.volume = data.volume;
        source.pitch = data.pitch + UnityEngine.Random.Range(-data.pitchVariation, data.pitchVariation);
        source.spatialBlend = data.spatialBlend;
        source.minDistance = data.minDistance;
        source.maxDistance = data.maxDistance;
        source.loop = data.loop;
        source.priority = data.priority;

        // Устанавливаем позицию
        if (parent != null)
        {
            source.transform.SetParent(parent);
            source.transform.localPosition = Vector3.zero;
        }
        else
        {
            source.transform.SetParent(null);
            source.transform.position = position;
        }

        // Проигрываем
        if (data.delay > 0)
        {
            source.PlayDelayed(data.delay);
        }
        else
        {
            source.Play();
        }

        activeSources.Add(source);

        // Автоматический возврат в пул
        if (!data.loop)
        {
            StartCoroutine(ReturnToPoolWhenFinished(source, clip.length / Mathf.Abs(source.pitch)));
        }

        return source;
    }

    /// <summary>
    /// Проигрывает 3D звук на позиции
    /// </summary>
    public AudioSource PlaySoundAtPosition(string soundId, Vector3 position)
    {
        return PlaySound(soundId, position);
    }

    /// <summary>
    /// Проигрывает звук на объекте (следует за объектом)
    /// </summary>
    public AudioSource PlaySoundOnObject(string soundId, GameObject target)
    {
        return PlaySound(soundId, target.transform.position, target.transform);
    }

    /// <summary>
    /// Проигрывает звук с параметрами
    /// </summary>
    public AudioSource PlaySoundWithParams(string soundId, Vector3 position,
        float volumeMultiplier = 1f, float pitchMultiplier = 1f)
    {
        if (!soundDatabase.ContainsKey(soundId))
        {
            Debug.LogWarning($"Sound with ID '{soundId}' not found.");
            return null;
        }

        SoundData data = soundDatabase[soundId];
        float originalVolume = data.volume;
        float originalPitch = data.pitch;

        data.volume *= volumeMultiplier;
        data.pitch *= pitchMultiplier;

        AudioSource source = PlaySoundData(data, position);

        data.volume = originalVolume;
        data.pitch = originalPitch;

        return source;
    }

    private IEnumerator ReturnToPoolWhenFinished(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay + 0.1f);

        if (source != null && !source.loop)
        {
            ReturnToPool(source);
        }
    }

    #endregion

    #region Music Playback

    /// <summary>
    /// Проигрывает музыку по ID
    /// </summary>
    public void PlayMusic(string musicId, bool fadeIn = true)
    {
        if (!musicDatabase.ContainsKey(musicId))
        {
            Debug.LogWarning($"Music with ID '{musicId}' not found.");
            return;
        }

        PlayMusicTrack(musicDatabase[musicId], fadeIn);
    }

    /// <summary>
    /// Проигрывает музыку напрямую из AudioClip
    /// </summary>
    public void PlayMusicClip(AudioClip clip, float volume = 0.7f, bool loop = true, bool fadeIn = true)
    {
        MusicTrack track = new MusicTrack
        {
            id = "direct_music",
            clip = clip,
            volume = volume,
            loop = loop,
            fadeInTime = musicFadeTime,
            fadeOutTime = musicFadeTime
        };

        PlayMusicTrack(track, fadeIn);
    }

    private void PlayMusicTrack(MusicTrack track, bool fadeIn)
    {
        AudioSource targetSource = isMusicSourceAActive ? musicSourceB : musicSourceA;
        AudioSource currentSource = isMusicSourceAActive ? musicSourceA : musicSourceB;

        targetSource.clip = track.clip;
        targetSource.outputAudioMixerGroup = track.mixerGroup ?? musicGroup;
        targetSource.loop = track.loop;
        targetSource.priority = track.priority;

        if (fadeIn)
        {
            StartCoroutine(CrossFadeMusic(currentSource, targetSource, track.volume, musicCrossFadeTime));
        }
        else
        {
            targetSource.volume = track.volume;
            targetSource.Play();
            currentSource.Stop();
        }

        isMusicSourceAActive = !isMusicSourceAActive;
        OnMusicTrackChanged?.Invoke(track.id);
    }

    private IEnumerator CrossFadeMusic(AudioSource from, AudioSource to, float targetVolume, float duration)
    {
        to.volume = 0f;
        to.Play();

        float elapsed = 0f;
        float startVolumeFrom = from.volume;
        float startVolumeTo = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            from.volume = Mathf.Lerp(startVolumeFrom, 0f, t);
            to.volume = Mathf.Lerp(startVolumeTo, targetVolume, t);

            yield return null;
        }

        from.volume = 0f;
        from.Stop();
        to.volume = targetVolume;
    }

    /// <summary>
    /// Останавливает музыку с затуханием
    /// </summary>
    public void StopMusic(bool fadeOut = true)
    {
        AudioSource currentSource = isMusicSourceAActive ? musicSourceA : musicSourceB;

        if (fadeOut)
        {
            StartCoroutine(FadeOutMusic(currentSource, musicFadeTime));
        }
        else
        {
            currentSource.Stop();
        }

        OnMusicStopped?.Invoke();
    }

    private IEnumerator FadeOutMusic(AudioSource source, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        source.volume = 0f;
        source.Stop();
    }

    /// <summary>
    /// Ставит музыку на паузу
    /// </summary>
    public void PauseMusic()
    {
        AudioSource currentSource = isMusicSourceAActive ? musicSourceA : musicSourceB;
        currentSource.Pause();
    }

    /// <summary>
    /// Возобновляет музыку
    /// </summary>
    public void ResumeMusic()
    {
        AudioSource currentSource = isMusicSourceAActive ? musicSourceA : musicSourceB;
        currentSource.UnPause();
    }

    /// <summary>
    /// Добавляет трек в очередь музыки
    /// </summary>
    public void QueueMusic(string musicId)
    {
        if (!musicDatabase.ContainsKey(musicId))
        {
            Debug.LogWarning($"Music with ID '{musicId}' not found.");
            return;
        }

        musicQueue.Enqueue(musicDatabase[musicId].clip);

        if (!isMusicQueuePlaying)
        {
            StartCoroutine(PlayMusicQueue());
        }
    }

    private IEnumerator PlayMusicQueue()
    {
        isMusicQueuePlaying = true;

        while (musicQueue.Count > 0)
        {
            AudioClip clip = musicQueue.Dequeue();
            PlayMusicClip(clip);

            yield return new WaitForSeconds(clip.length);
        }

        isMusicQueuePlaying = false;
    }

    #endregion

    #region Volume Control

    public void SetMasterVolume(float volume)
    {
        SetMixerVolume(MASTER_VOLUME, volume);
        OnMasterVolumeChanged?.Invoke(volume);
    }

    public void SetMusicVolume(float volume)
    {
        SetMixerVolume(MUSIC_VOLUME, volume);
        OnMusicVolumeChanged?.Invoke(volume);
    }

    public void SetSFXVolume(float volume)
    {
        SetMixerVolume(SFX_VOLUME, volume);
        OnSFXVolumeChanged?.Invoke(volume);
    }

    public void SetUIVolume(float volume)
    {
        SetMixerVolume(UI_VOLUME, volume);
    }

    public void SetAmbientVolume(float volume)
    {
        SetMixerVolume(AMBIENT_VOLUME, volume);
    }

    public void SetVoiceVolume(float volume)
    {
        SetMixerVolume(VOICE_VOLUME, volume);
    }

    private void SetMixerVolume(string parameter, float volume)
    {
        if (audioMixer != null)
        {
            // Конвертируем линейную громкость (0-1) в децибелы (-80 до 0)
            float dB = volume > 0 ? 20f * Mathf.Log10(volume) : -80f;
            audioMixer.SetFloat(parameter, dB);
        }
    }

    public float GetMasterVolume()
    {
        return GetMixerVolume(MASTER_VOLUME);
    }

    public float GetMusicVolume()
    {
        return GetMixerVolume(MUSIC_VOLUME);
    }

    public float GetSFXVolume()
    {
        return GetMixerVolume(SFX_VOLUME);
    }

    private float GetMixerVolume(string parameter)
    {
        if (audioMixer != null && audioMixer.GetFloat(parameter, out float dB))
        {
            return Mathf.Pow(10f, dB / 20f);
        }
        return 1f;
    }

    #endregion

    #region Utility

    /// <summary>
    /// Останавливает все звуки
    /// </summary>
    public void StopAllSounds()
    {
        foreach (var source in activeSources)
        {
            if (source != null)
            {
                source.Stop();
                ReturnToPool(source);
            }
        }
        activeSources.Clear();
    }

    /// <summary>
    /// Останавливает звуки по ID
    /// </summary>
    public void StopSound(string soundId)
    {
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            if (activeSources[i] != null && activeSources[i].clip != null)
            {
                // Находим в базе данных
                if (soundDatabase.TryGetValue(soundId, out SoundData data))
                {
                    foreach (var clip in data.clips)
                    {
                        if (activeSources[i].clip == clip)
                        {
                            ReturnToPool(activeSources[i]);
                            break;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Проверяет, проигрывается ли звук
    /// </summary>
    public bool IsPlaying(string soundId)
    {
        if (!soundDatabase.ContainsKey(soundId)) return false;

        SoundData data = soundDatabase[soundId];
        foreach (var source in activeSources)
        {
            if (source != null && source.isPlaying)
            {
                foreach (var clip in data.clips)
                {
                    if (source.clip == clip) return true;
                }
            }
        }
        return false;
    }

    #endregion

    private void OnDestroy()
    {
        StopAllSounds();
        StopMusic(false);
    }

    private void Update()
    {
        // Очистка завершенных источников
        CleanupFinishedSources();
    }

    private void CleanupFinishedSources()
    {
        for (int i = activeSources.Count - 1; i >= 0; i--)
        {
            var source = activeSources[i];
            if (source == null || (!source.isPlaying && !source.loop))
            {
                if (source != null) ReturnToPool(source);
            }
        }
    }
}


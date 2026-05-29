using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider uiVolumeSlider;
    [SerializeField] private Slider ambientVolumeSlider;
    [SerializeField] private Slider voiceVolumeSlider;

    [Header("Mute Toggles")]
    [SerializeField] private Toggle masterMuteToggle;
    [SerializeField] private Toggle musicMuteToggle;
    [SerializeField] private Toggle sfxMuteToggle;

    [Header("Values Display")]
    [SerializeField] private Text masterVolumeText;
    [SerializeField] private Text musicVolumeText;
    [SerializeField] private Text sfxVolumeText;

    private AudioManager audioManager;
    private AudioInitializer initializer;

    private float previousMasterVolume;
    private float previousMusicVolume;
    private float previousSFXVolume;

    private void Start()
    {
        audioManager = AudioManager.Instance;
        initializer = FindObjectOfType<AudioInitializer>();

        InitializeSliders();
        InitializeToggles();
        AddListeners();
    }

    private void InitializeSliders()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.value = audioManager.GetMasterVolume();
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.minValue = 0f;
            musicVolumeSlider.maxValue = 1f;
            musicVolumeSlider.value = audioManager.GetMusicVolume();
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.value = audioManager.GetSFXVolume();
        }

        UpdateVolumeTexts();
    }

    private void InitializeToggles()
    {
        if (masterMuteToggle != null)
        {
            masterMuteToggle.isOn = audioManager.GetMasterVolume() <= 0.001f;
        }

        if (musicMuteToggle != null)
        {
            musicMuteToggle.isOn = audioManager.GetMusicVolume() <= 0.001f;
        }

        if (sfxMuteToggle != null)
        {
            sfxMuteToggle.isOn = audioManager.GetSFXVolume() <= 0.001f;
        }
    }

    private void AddListeners()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);

        if (uiVolumeSlider != null)
            uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);

        if (ambientVolumeSlider != null)
            ambientVolumeSlider.onValueChanged.AddListener(OnAmbientVolumeChanged);

        if (voiceVolumeSlider != null)
            voiceVolumeSlider.onValueChanged.AddListener(OnVoiceVolumeChanged);

        if (masterMuteToggle != null)
            masterMuteToggle.onValueChanged.AddListener(OnMasterMuteChanged);

        if (musicMuteToggle != null)
            musicMuteToggle.onValueChanged.AddListener(OnMusicMuteChanged);

        if (sfxMuteToggle != null)
            sfxMuteToggle.onValueChanged.AddListener(OnSFXMuteChanged);
    }

    #region Volume Change Handlers

    private void OnMasterVolumeChanged(float value)
    {
        audioManager.SetMasterVolume(value);
        UpdateVolumeTexts();

        if (masterMuteToggle != null)
            masterMuteToggle.isOn = value <= 0.001f;
    }

    private void OnMusicVolumeChanged(float value)
    {
        audioManager.SetMusicVolume(value);
        UpdateVolumeTexts();

        if (musicMuteToggle != null)
            musicMuteToggle.isOn = value <= 0.001f;
    }

    private void OnSFXVolumeChanged(float value)
    {
        audioManager.SetSFXVolume(value);
        UpdateVolumeTexts();

        if (sfxMuteToggle != null)
            sfxMuteToggle.isOn = value <= 0.001f;
    }

    private void OnUIVolumeChanged(float value)
    {
        audioManager.SetUIVolume(value);
    }

    private void OnAmbientVolumeChanged(float value)
    {
        audioManager.SetAmbientVolume(value);
    }

    private void OnVoiceVolumeChanged(float value)
    {
        audioManager.SetVoiceVolume(value);
    }

    #endregion

    #region Mute Handlers

    private void OnMasterMuteChanged(bool isMuted)
    {
        if (isMuted)
        {
            previousMasterVolume = audioManager.GetMasterVolume();
            audioManager.SetMasterVolume(0f);
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = 0f;
        }
        else
        {
            float volume = previousMasterVolume > 0.001f ? previousMasterVolume : 1f;
            audioManager.SetMasterVolume(volume);
            if (masterVolumeSlider != null)
                masterVolumeSlider.value = volume;
        }
    }

    private void OnMusicMuteChanged(bool isMuted)
    {
        if (isMuted)
        {
            previousMusicVolume = audioManager.GetMusicVolume();
            audioManager.SetMusicVolume(0f);
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = 0f;
        }
        else
        {
            float volume = previousMusicVolume > 0.001f ? previousMusicVolume : 0.7f;
            audioManager.SetMusicVolume(volume);
            if (musicVolumeSlider != null)
                musicVolumeSlider.value = volume;
        }
    }

    private void OnSFXMuteChanged(bool isMuted)
    {
        if (isMuted)
        {
            previousSFXVolume = audioManager.GetSFXVolume();
            audioManager.SetSFXVolume(0f);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = 0f;
        }
        else
        {
            float volume = previousSFXVolume > 0.001f ? previousSFXVolume : 1f;
            audioManager.SetSFXVolume(volume);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = volume;
        }
    }

    #endregion

    private void UpdateVolumeTexts()
    {
        if (masterVolumeText != null)
            masterVolumeText.text = $"{(audioManager.GetMasterVolume() * 100):F0}%";

        if (musicVolumeText != null)
            musicVolumeText.text = $"{(audioManager.GetMusicVolume() * 100):F0}%";

        if (sfxVolumeText != null)
            sfxVolumeText.text = $"{(audioManager.GetSFXVolume() * 100):F0}%";
    }

    public void SaveSettings()
    {
        if (initializer != null)
        {
            initializer.SaveAudioSettings();
        }
    }

    public void ResetToDefaults()
    {
        if (initializer != null)
        {
            audioManager.SetMasterVolume(initializer.defaultMasterVolume);
            audioManager.SetMusicVolume(initializer.defaultMusicVolume);
            audioManager.SetSFXVolume(initializer.defaultSFXVolume);
            audioManager.SetUIVolume(initializer.defaultUIVolume);
            audioManager.SetAmbientVolume(initializer.defaultAmbientVolume);
            audioManager.SetVoiceVolume(initializer.defaultVoiceVolume);

            InitializeSliders();
            InitializeToggles();
        }
    }

    private void OnDestroy()
    {
        SaveSettings();
    }
}
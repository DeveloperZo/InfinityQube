using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Manages all volume settings and calculations for the AudioManager system.
/// Provides centralized volume control with support for multiple categories and future AudioMixer integration.
/// </summary>
public class AudioVolumeController : MonoBehaviour
{
    #region Volume Settings
    [Header("Volume Controls")]
    [Range(0f, 2f)] private float masterVolume = 1f;
    [Range(0f, 2f)] private float soundEffectsVolume = 0.8f;
    [Range(0f, 2f)] private float cubeImpactVolume = 0.7f;
    [Range(0f, 2f)] private float cubeDestructionVolume = 0.6f;
    [Range(0f, 1f)] private float backgroundAudioVolume = 0.3f;
    [Range(0f, 1f)] private float systemAudioVolume = 0.7f;
    [Range(0f, 1f)] private float waveCompositionVolume = 0.6f;
    #endregion

    #region Future AudioMixer Support
    [Header("AudioMixer Integration (Future)")]
    [SerializeField] private AudioMixerGroup masterMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup uiMixerGroup;
    #endregion

    #region Properties
    public float MasterVolume => masterVolume;
    public float SoundEffectsVolume => soundEffectsVolume;
    public float CubeImpactVolume => cubeImpactVolume;
    public float CubeDestructionVolume => cubeDestructionVolume;
    public float BackgroundAudioVolume => backgroundAudioVolume;
    public float SystemAudioVolume => systemAudioVolume;
    public float WaveCompositionVolume => waveCompositionVolume;

    /// <summary>
    /// Alias for sound effects volume (for backward compatibility)
    /// </summary>
    public float sfxVolume 
    { 
        get => soundEffectsVolume; 
        set => soundEffectsVolume = Mathf.Clamp01(value); 
    }

    public bool IsInitialized { get; private set; }
    #endregion

    #region Events
    public event System.Action<VolumeCategory, float> OnVolumeChanged;
    public event System.Action<float> OnMasterVolumeChanged;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the volume controller with optional starting values
    /// </summary>
    public void Initialize(Dictionary<VolumeCategory, float> initialVolumes = null)
    {
        if (initialVolumes != null)
        {
            foreach (var kvp in initialVolumes)
            {
                SetVolumeForCategory(kvp.Key, kvp.Value, false);
            }
        }

        ValidateAllVolumes();
        IsInitialized = true;
        DebugLog("AudioVolumeController initialized");
    }

    /// <summary>
    /// Validates and clamps all volume values to ensure they're within valid ranges
    /// </summary>
    private void ValidateAllVolumes()
    {
        masterVolume = Mathf.Clamp(masterVolume, 0f, 2f);
        soundEffectsVolume = Mathf.Clamp(soundEffectsVolume, 0f, 2f);
        cubeImpactVolume = Mathf.Clamp(cubeImpactVolume, 0f, 2f);
        cubeDestructionVolume = Mathf.Clamp(cubeDestructionVolume, 0f, 2f);
        backgroundAudioVolume = Mathf.Clamp01(backgroundAudioVolume);
        systemAudioVolume = Mathf.Clamp01(systemAudioVolume);
        waveCompositionVolume = Mathf.Clamp01(waveCompositionVolume);
    }
    #endregion

    #region Volume Management
    /// <summary>
    /// Gets the current volume level for a specific category
    /// </summary>
    public float GetCurrentVolumeLevel(VolumeCategory category)
    {
        switch (category)
        {
            case VolumeCategory.Master: return masterVolume;
            case VolumeCategory.SoundEffects: return soundEffectsVolume;
            case VolumeCategory.CubeImpact: return cubeImpactVolume;
            case VolumeCategory.CubeDestruction: return cubeDestructionVolume;
            case VolumeCategory.BackgroundAudio: return backgroundAudioVolume;
            case VolumeCategory.SystemAudio: return systemAudioVolume;
            case VolumeCategory.WaveComposition: return waveCompositionVolume;
            default: return 1f;
        }
    }

    /// <summary>
    /// Gets the effective volume for a category (category volume * master volume)
    /// </summary>
    public float GetEffectiveVolumeLevel(VolumeCategory category)
    {
        return GetCurrentVolumeLevel(category) * masterVolume;
    }

    /// <summary>
    /// Gets the effective volume with additional modifiers
    /// </summary>
    public float GetEffectiveVolume(VolumeCategory category, float additionalMultiplier = 1f)
    {
        return GetEffectiveVolumeLevel(category) * additionalMultiplier;
    }

    /// <summary>
    /// Sets the master volume and updates all dependent systems
    /// </summary>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp(volume, 0f, 2f);
        OnMasterVolumeChanged?.Invoke(masterVolume);
        UpdateAudioMixer(VolumeCategory.Master, masterVolume);
        DebugLog($"Master volume set to: {masterVolume:F2}");
    }

    /// <summary>
    /// Sets the volume for a specific category
    /// </summary>
    public void SetVolumeForCategory(VolumeCategory category, float volume, bool notify = true)
    {
        float clampedVolume = volume;
        
        switch (category)
        {
            case VolumeCategory.Master:
                SetMasterVolume(volume);
                return;
                
            case VolumeCategory.SoundEffects:
                soundEffectsVolume = Mathf.Clamp(volume, 0f, 2f);
                clampedVolume = soundEffectsVolume;
                break;
                
            case VolumeCategory.CubeImpact:
                cubeImpactVolume = Mathf.Clamp(volume, 0f, 2f);
                clampedVolume = cubeImpactVolume;
                break;
                
            case VolumeCategory.CubeDestruction:
                cubeDestructionVolume = Mathf.Clamp(volume, 0f, 2f);
                clampedVolume = cubeDestructionVolume;
                break;
                
            case VolumeCategory.BackgroundAudio:
                backgroundAudioVolume = Mathf.Clamp01(volume);
                clampedVolume = backgroundAudioVolume;
                break;
                
            case VolumeCategory.SystemAudio:
                systemAudioVolume = Mathf.Clamp01(volume);
                clampedVolume = systemAudioVolume;
                break;
                
            case VolumeCategory.WaveComposition:
                waveCompositionVolume = Mathf.Clamp01(volume);
                clampedVolume = waveCompositionVolume;
                break;
        }

        if (notify)
        {
            OnVolumeChanged?.Invoke(category, clampedVolume);
        }
        
        UpdateAudioMixer(category, clampedVolume);
        DebugLog($"{category} volume set to: {clampedVolume:F2}");
    }
    #endregion

    #region Volume Utilities
    /// <summary>
    /// Fades a volume category over time
    /// </summary>
    public IEnumerator FadeVolume(VolumeCategory category, float targetVolume, float fadeTime)
    {
        float startVolume = GetCurrentVolumeLevel(category);
        float elapsedTime = 0f;

        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeTime;
            float currentVolume = Mathf.Lerp(startVolume, targetVolume, t);
            SetVolumeForCategory(category, currentVolume, false);
            yield return null;
        }

        SetVolumeForCategory(category, targetVolume);
    }

    /// <summary>
    /// Gets all current volume settings as a dictionary
    /// </summary>
    public Dictionary<VolumeCategory, float> GetAllVolumeSettings()
    {
        var settings = new Dictionary<VolumeCategory, float>();
        foreach (VolumeCategory category in System.Enum.GetValues(typeof(VolumeCategory)))
        {
            settings[category] = GetCurrentVolumeLevel(category);
        }
        return settings;
    }

    /// <summary>
    /// Applies a set of volume settings
    /// </summary>
    public void ApplyVolumeSettings(Dictionary<VolumeCategory, float> settings)
    {
        foreach (var kvp in settings)
        {
            SetVolumeForCategory(kvp.Key, kvp.Value);
        }
    }
    #endregion

    #region AudioMixer Integration (Future)
    /// <summary>
    /// Updates the AudioMixer with new volume values (prepared for future implementation)
    /// </summary>
    private void UpdateAudioMixer(VolumeCategory category, float volume)
    {
        // Future implementation: Update AudioMixer parameters
        // This is prepared for when AudioMixer groups are configured
        
        if (masterMixerGroup == null) return;

        // Example implementation (commented out for future use):
        /*
        string parameterName = GetMixerParameterName(category);
        if (!string.IsNullOrEmpty(parameterName))
        {
            float dbValue = LinearToDecibel(volume);
            audioMixer.SetFloat(parameterName, dbValue);
        }
        */
    }

    /// <summary>
    /// Converts linear volume (0-1) to decibel scale for AudioMixer
    /// </summary>
    private float LinearToDecibel(float linear)
    {
        return linear > 0 ? 20f * Mathf.Log10(linear) : -80f;
    }

    /// <summary>
    /// Gets the AudioMixer parameter name for a volume category
    /// </summary>
    private string GetMixerParameterName(VolumeCategory category)
    {
        switch (category)
        {
            case VolumeCategory.Master: return "MasterVolume";
            case VolumeCategory.SoundEffects: return "SFXVolume";
            case VolumeCategory.BackgroundAudio: return "MusicVolume";
            case VolumeCategory.SystemAudio: return "UIVolume";
            default: return "";
        }
    }
    #endregion

    #region Debug
    /// <summary>
    /// Gets debug information about current volume settings
    /// </summary>
    public Dictionary<string, object> GetDebugInfo()
    {
        var info = new Dictionary<string, object>
        {
            ["Master Volume"] = $"{masterVolume:F2}",
            ["SFX Volume"] = $"{soundEffectsVolume:F2}",
            ["Cube Impact"] = $"{cubeImpactVolume:F2}",
            ["Cube Destruction"] = $"{cubeDestructionVolume:F2}",
            ["Background Audio"] = $"{backgroundAudioVolume:F2}",
            ["System Audio"] = $"{systemAudioVolume:F2}",
            ["Wave Composition"] = $"{waveCompositionVolume:F2}",
            ["AudioMixer Ready"] = masterMixerGroup != null ? "Yes" : "No"
        };
        return info;
    }

    private void DebugLog(string message)
    {
        Debug.Log($"[AudioVolumeController] {message}");
    }
    #endregion
}

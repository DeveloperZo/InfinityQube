using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages all cube-specific audio functionality for the AudioManager system.
/// Handles cube landing, capture, destruction, special effects, and escape sounds.
/// </summary>
public class CubeAudioSystem : MonoBehaviour
{
    #region Dependencies
    private AudioPlaybackSystem playbackSystem;
    private AudioVolumeController volumeController;
    private bool enableDebugLogs;
    private bool logAudioEvents;
    private bool enableWaveComposition;
    #endregion

    #region Audio Configuration
    [Header("Cube Audio Configuration")]
    [SerializeField] private CubeAudioConfiguration cubeAudioConfiguration;
    
    [Header("Legacy Audio Arrays")]
    [SerializeField] private AudioClip[] cubeImpactSounds;
    [SerializeField] private AudioClip[] cubeDestructionSounds;
    [SerializeField] private AudioClip[] specialEffectSounds;
    #endregion

    #region Runtime State
    private Dictionary<Enumerations.CubeType, AudioClip> lastPlayedCubeSounds = new Dictionary<Enumerations.CubeType, AudioClip>();
    #endregion

    #region Properties
    public bool IsInitialized { get; private set; }
    public Dictionary<Enumerations.CubeType, AudioClip> LastPlayedCubeSounds => lastPlayedCubeSounds;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the cube audio system with required dependencies and configuration
    /// </summary>
    public void Initialize(AudioPlaybackSystem playback, AudioVolumeController volume, 
        CubeAudioConfiguration config, AudioClip[] impacts, AudioClip[] destructions, 
        AudioClip[] specials, bool debugLogs, bool logEvents, bool waveComposition)
    {
        playbackSystem = playback;
        volumeController = volume;
        cubeAudioConfiguration = config;
        cubeImpactSounds = impacts;
        cubeDestructionSounds = destructions;
        specialEffectSounds = specials;
        enableDebugLogs = debugLogs;
        logAudioEvents = logEvents;
        enableWaveComposition = waveComposition;
        
        lastPlayedCubeSounds.Clear();
        
        ValidateConfiguration();
        IsInitialized = true;
        DebugLog("CubeAudioSystem initialized");
    }

    private void ValidateConfiguration()
    {
        if (cubeAudioConfiguration != null)
        {
            bool configValid = cubeAudioConfiguration.ValidateConfiguration();
            if (!configValid && enableDebugLogs)
            {
                DebugLog("CubeAudioConfiguration validation failed. Some cube types may not have audio assigned.");
            }
        }
        else if (enableDebugLogs)
        {
            DebugLog("CubeAudioConfiguration is not assigned. Cube-specific audio will use legacy fallbacks.");
        }
    }
    #endregion

    #region Cube Sound Methods
    /// <summary>
    /// Plays cube landing sound for a specific cube type with spatial positioning
    /// </summary>
    public void PlayCubeLandingSound(Enumerations.CubeType cubeType, Vector3 position)
    {
        if (!ValidatePlayback("PlayCubeLandingSound"))
            return;

        AudioClip selectedClip = null;
        float volume = volumeController.GetEffectiveVolumeLevel(VolumeCategory.CubeImpact);
        float pitch = 1f;
        
        // Try to get clip from cube audio configuration first
        if (cubeAudioConfiguration != null)
        {
            selectedClip = cubeAudioConfiguration.GetRandomClip(cubeType, SoundCategory.Landing);
            if (selectedClip != null)
            {
                AudioPlaybackSettings settings = cubeAudioConfiguration.GetPlaybackSettings(cubeType, SoundCategory.Landing);
                volume = settings.volume * volumeController.GetEffectiveVolume(VolumeCategory.CubeImpact, volumeController.sfxVolume);
                pitch = settings.pitch;
                
                if (enableDebugLogs && logAudioEvents)
                {
                    DebugLog($"Using configured cube landing sound for {cubeType}: {selectedClip.name} (Volume: {volume:F2}, Pitch: {pitch:F2})");
                }
            }
        }
        
        // Fallback to legacy cube impact sounds
        if (selectedClip == null)
        {
            selectedClip = GetRandomAudioClip(cubeImpactSounds);
            volume = volumeController.GetEffectiveVolume(VolumeCategory.CubeImpact, volumeController.sfxVolume);
            
            if (enableDebugLogs && logAudioEvents)
            {
                DebugLog($"Using fallback cube impact sound for {cubeType}: {selectedClip?.name ?? "null"}");
            }
        }
        
        // Play the selected clip
        if (selectedClip != null)
        {
            playbackSystem.PlayAudioClip(selectedClip, volume, position, pitch);
            
            // Track for wave composition
            if (enableWaveComposition)
            {
                lastPlayedCubeSounds[cubeType] = selectedClip;
            }
            
            if (enableDebugLogs && logAudioEvents)
            {
                DebugLog($"Successfully played cube landing sound for {cubeType} at position {position}");
            }
        }
        else if (enableDebugLogs)
        {
            DebugLog($"No audio clip available for cube landing sound (type: {cubeType})");
        }
    }

    /// <summary>
    /// Plays cube capture sound for a specific cube type
    /// </summary>
    public void PlayCubeCaptureSound(Enumerations.CubeType cubeType, Vector3 position = default)
    {
        if (!ValidatePlayback("PlayCubeCaptureSound"))
            return;

        if (cubeAudioConfiguration != null)
        {
            AudioClip clip = cubeAudioConfiguration.GetRandomClip(cubeType, SoundCategory.Capture);
            if (clip != null)
            {
                AudioPlaybackSettings settings = cubeAudioConfiguration.GetPlaybackSettings(cubeType, SoundCategory.Capture);
                float volume = settings.volume * volumeController.GetEffectiveVolumeLevel(VolumeCategory.SoundEffects);
                playbackSystem.PlayAudioClip(clip, volume, position, settings.pitch);
                
                if (logAudioEvents)
                {
                    DebugLog($"Played cube capture sound for {cubeType}: {clip.name} (Volume: {settings.volume:F2}, Pitch: {settings.pitch:F2})");
                }
                return;
            }
        }
        
        // No fallback for capture sounds - they are optional
        if (logAudioEvents)
        {
            DebugLog($"No capture sound available for cube type: {cubeType}");
        }
    }

    /// <summary>
    /// Plays cube destruction sound for a specific cube type
    /// </summary>
    public void PlayCubeDestructionSound(Enumerations.CubeType cubeType, Vector3 position = default)
    {
        if (!ValidatePlayback("PlayCubeDestructionSound"))
            return;

        if (cubeAudioConfiguration != null)
        {
            AudioClip clip = cubeAudioConfiguration.GetRandomClip(cubeType, SoundCategory.Destruction);
            if (clip != null)
            {
                AudioPlaybackSettings settings = cubeAudioConfiguration.GetPlaybackSettings(cubeType, SoundCategory.Destruction);
                float volume = settings.volume * volumeController.GetEffectiveVolumeLevel(VolumeCategory.CubeDestruction);
                playbackSystem.PlayAudioClip(clip, volume, position, settings.pitch);
                
                if (logAudioEvents)
                {
                    DebugLog($"Played cube destruction sound for {cubeType}: {clip.name} (Volume: {settings.volume:F2}, Pitch: {settings.pitch:F2})");
                }
                return;
            }
        }
        
        // Fallback to legacy cube destruction sounds
        PlayCubeDestructionSound(position);
    }

    /// <summary>
    /// Plays legacy cube destruction sound
    /// </summary>
    public void PlayCubeDestructionSound(Vector3 position = default)
    {
        if (!ValidatePlayback("PlayCubeDestructionSound"))
            return;

        if (cubeDestructionSounds == null || cubeDestructionSounds.Length == 0)
        {
            DebugLog("No cube destruction sounds assigned!");
            return;
        }

        AudioClip randomClip = GetRandomAudioClip(cubeDestructionSounds);
        float volume = volumeController.GetEffectiveVolumeLevel(VolumeCategory.CubeDestruction);
        
        playbackSystem.PlayAudioClip(randomClip, volume, position);
        
        if (logAudioEvents)
        {
            DebugLog($"Played cube destruction sound: {randomClip.name} at volume {volume}");
        }
    }

    /// <summary>
    /// Plays cube special effect sound for a specific cube type
    /// </summary>
    public void PlayCubeSpecialEffectSound(Enumerations.CubeType cubeType, Vector3 position = default)
    {
        if (!ValidatePlayback("PlayCubeSpecialEffectSound"))
            return;

        if (cubeAudioConfiguration != null)
        {
            AudioClip clip = cubeAudioConfiguration.GetRandomClip(cubeType, SoundCategory.SpecialEffect);
            if (clip != null)
            {
                AudioPlaybackSettings settings = cubeAudioConfiguration.GetPlaybackSettings(cubeType, SoundCategory.SpecialEffect);
                float volume = settings.volume * volumeController.GetEffectiveVolumeLevel(VolumeCategory.SoundEffects);
                playbackSystem.PlayAudioClip(clip, volume, position, settings.pitch);
                
                if (logAudioEvents)
                {
                    DebugLog($"Played cube special effect sound for {cubeType}: {clip.name} (Volume: {settings.volume:F2}, Pitch: {settings.pitch:F2})");
                }
                return;
            }
        }
        
        // No fallback for special effect sounds - they are optional
        if (logAudioEvents)
        {
            DebugLog($"No special effect sound available for cube type: {cubeType}");
        }
    }

    /// <summary>
    /// [POC] Plays cube escape sound for a specific cube type
    /// Currently uses destruction sound as placeholder until specific escape sounds are available
    /// </summary>
    public void PlayCubeEscapeSound(Enumerations.CubeType cubeType, Vector3 position = default)
    {
        if (!ValidatePlayback("PlayCubeEscapeSound"))
            return;

        // [POC] For now, use destruction sound with lower volume and pitch as escape sound
        // In future, this should have its own sound category in CubeAudioConfiguration
        
        if (cubeAudioConfiguration != null)
        {
            // Try to get destruction sound as placeholder
            AudioClip clip = cubeAudioConfiguration.GetRandomClip(cubeType, SoundCategory.Destruction);
            if (clip != null)
            {
                AudioPlaybackSettings settings = cubeAudioConfiguration.GetPlaybackSettings(cubeType, SoundCategory.Destruction);
                // Modify settings for escape sound - lower volume and pitch for "falling away" effect
                float escapeVolume = settings.volume * 0.7f * volumeController.GetEffectiveVolume(VolumeCategory.CubeDestruction, volumeController.sfxVolume);
                float escapePitch = settings.pitch * 0.85f;
                
                playbackSystem.PlayAudioClip(clip, escapeVolume, position, escapePitch);
                
                if (logAudioEvents)
                {
                    DebugLog($"[POC] Played cube escape sound for {cubeType}: {clip.name} (Volume: {escapeVolume:F2}, Pitch: {escapePitch:F2})");
                }
                return;
            }
        }
        
        // Fallback to legacy destruction sound with modifications
        if (cubeDestructionSounds != null && cubeDestructionSounds.Length > 0)
        {
            AudioClip fallbackClip = GetRandomAudioClip(cubeDestructionSounds);
            float escapeVolume = volumeController.GetEffectiveVolumeLevel(VolumeCategory.CubeDestruction) * 0.7f;
            float escapePitch = 0.85f;
            
            playbackSystem.PlayAudioClip(fallbackClip, escapeVolume, position, escapePitch);
            
            if (logAudioEvents)
            {
                DebugLog($"[POC] Using fallback escape sound for {cubeType} with lower volume and pitch");
            }
        }
        else if (logAudioEvents)
        {
            DebugLog($"[POC] No escape sound available for cube type: {cubeType}");
        }
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Convenience method for playing cube landing sounds at specific positions
    /// </summary>
    public void PlayCubeLandingSoundAtPosition(Enumerations.CubeType cubeType, Vector3 worldPosition, float customVolume = -1f)
    {
        if (customVolume >= 0f)
        {
            // Temporarily override cube impact volume
            float originalImpactVolume = volumeController.CubeImpactVolume;
            volumeController.SetVolumeForCategory(VolumeCategory.CubeImpact, customVolume, false);
            PlayCubeLandingSound(cubeType, worldPosition);
            volumeController.SetVolumeForCategory(VolumeCategory.CubeImpact, originalImpactVolume, false);
        }
        else
        {
            PlayCubeLandingSound(cubeType, worldPosition);
        }
    }

    /// <summary>
    /// Gets a random audio clip from an array of clips
    /// </summary>
    public AudioClip GetRandomAudioClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            if (enableDebugLogs)
            {
                DebugLog("GetRandomAudioClip called with null or empty clips array");
            }
            return null;
        }
        
        AudioClip selectedClip = clips[Random.Range(0, clips.Length)];
        if (enableDebugLogs && logAudioEvents)
        {
            DebugLog($"Selected random audio clip: {selectedClip?.name ?? "null"} from array of {clips.Length} clips");
        }
        
        return selectedClip;
    }

    /// <summary>
    /// Plays a named special effect sound
    /// </summary>
    public void PlayNamedSpecialEffect(string clipName, Vector3 position = default, float volume = -1f)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            if (enableDebugLogs)
            {
                DebugLog("PlayNamedSpecialEffect called with null or empty clip name");
            }
            return;
        }
        
        AudioClip foundClip = null;
        
        // Search through special effect sounds
        if (specialEffectSounds != null)
        {
            foreach (AudioClip clip in specialEffectSounds)
            {
                if (clip != null && clip.name.Equals(clipName, System.StringComparison.OrdinalIgnoreCase))
                {
                    foundClip = clip;
                    break;
                }
            }
        }
        
        if (foundClip != null)
        {
            float playVolume = volume >= 0f ? volume : volumeController.SoundEffectsVolume;
            playbackSystem.PlayAudioClip(foundClip, playVolume, position);
            
            if (enableDebugLogs && logAudioEvents)
            {
                DebugLog($"Played named special effect: {clipName} at position {position} with volume {playVolume:F2}");
            }
        }
        else if (enableDebugLogs)
        {
            DebugLog($"Named special effect sound '{clipName}' not found in specialEffectSounds array");
        }
    }
    #endregion

    #region Validation
    private bool ValidatePlayback(string method)
    {
        if (!IsInitialized)
        {
            if (enableDebugLogs)
            {
                DebugLog($"{method}: CubeAudioSystem not initialized!");
            }
            return false;
        }

        if (playbackSystem == null || !playbackSystem.IsInitialized)
        {
            if (enableDebugLogs)
            {
                DebugLog($"{method}: AudioPlaybackSystem not available!");
            }
            return false;
        }

        return true;
    }
    #endregion

    #region Configuration Updates
    /// <summary>
    /// Updates the cube audio configuration
    /// </summary>
    public void UpdateConfiguration(CubeAudioConfiguration newConfig)
    {
        cubeAudioConfiguration = newConfig;
        ValidateConfiguration();
    }

    /// <summary>
    /// Updates debug settings
    /// </summary>
    public void UpdateDebugSettings(bool debugLogs, bool logEvents)
    {
        enableDebugLogs = debugLogs;
        logAudioEvents = logEvents;
    }
    #endregion

    #region Cleanup
    /// <summary>
    /// Cleans up the cube audio system
    /// </summary>
    public void Cleanup()
    {
        lastPlayedCubeSounds.Clear();
        IsInitialized = false;
        DebugLog("CubeAudioSystem cleaned up");
    }
    #endregion

    #region Debug
    /// <summary>
    /// Gets debug information about the cube audio system
    /// </summary>
    public Dictionary<string, object> GetDebugInfo()
    {
        return new Dictionary<string, object>
        {
            ["Configuration Assigned"] = cubeAudioConfiguration != null,
            ["Legacy Impact Sounds"] = cubeImpactSounds?.Length ?? 0,
            ["Legacy Destruction Sounds"] = cubeDestructionSounds?.Length ?? 0,
            ["Special Effect Sounds"] = specialEffectSounds?.Length ?? 0,
            ["Tracked Cube Types"] = lastPlayedCubeSounds.Count,
            ["Is Initialized"] = IsInitialized
        };
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[CubeAudioSystem] {message}");
        }
    }
    #endregion
}

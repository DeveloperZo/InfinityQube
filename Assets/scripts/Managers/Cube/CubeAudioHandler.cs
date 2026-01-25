using System.Text;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Handles audio playback for cubes.
/// Extracted from CubeManager as part of SRP refactoring.
/// Manages cube-specific sounds: landing, capture, destruction, and special effects.
/// </summary>
public class CubeAudioHandler
{
    #region References
    private readonly CubeManager cube;
    private readonly AudioSource audioSource;
    private readonly CubeAudioConfiguration audioConfig;
    private bool enableDebugLogs;
    #endregion

    #region Constructor
    public CubeAudioHandler(
        CubeManager cubeManager,
        AudioSource source,
        CubeAudioConfiguration config,
        bool debugLogs)
    {
        cube = cubeManager;
        audioSource = source;
        audioConfig = config;
        enableDebugLogs = debugLogs;
    }
    #endregion

    #region Audio Playback
    /// <summary>
    /// Plays cube audio for the specified sound category.
    /// </summary>
    public void PlayCubeAudio(SoundCategory soundCategory, float volumeMultiplier = 1f)
    {
        if (audioConfig == null || audioSource == null)
        {
            // Fallback to AudioManager
            if (AudioManager.Instance != null)
            {
                CubeType effectiveType = cube.GetEffectiveType();
                switch (soundCategory)
                {
                    case SoundCategory.Landing:
                        AudioManager.Instance.PlayCubeLandingSound(effectiveType, cube.transform.position);
                        break;
                    case SoundCategory.Capture:
                        AudioManager.Instance.PlayCubeCaptureSound(effectiveType, cube.transform.position);
                        break;
                    case SoundCategory.Destruction:
                        AudioManager.Instance.PlayCubeDestructionSound(effectiveType, cube.transform.position);
                        break;
                    case SoundCategory.SpecialEffect:
                        AudioManager.Instance.PlayCubeSpecialEffectSound(effectiveType, cube.transform.position);
                        break;
                }
            }
            return;
        }
        
        CubeType effectiveTyp = cube.GetEffectiveType();
        AudioClip audioClip = audioConfig.GetRandomClip(effectiveTyp, soundCategory);
        
        if (audioClip == null)
        {
            audioClip = audioConfig.GetRandomClip(cube.type, soundCategory);
        }
        
        if (audioClip != null)
        {
            AudioPlaybackSettings settings = audioConfig.GetPlaybackSettings(effectiveTyp, soundCategory);
            
            float finalVolume = Mathf.Clamp01(settings.volume * volumeMultiplier);
            float finalPitch = Mathf.Clamp(settings.pitch, 0.5f, 2f);
            
            audioSource.clip = audioClip;
            audioSource.volume = finalVolume;
            audioSource.pitch = finalPitch;
            audioSource.Play();
            
            DebugLog($"Played {soundCategory} audio for {effectiveTyp} cube: {audioClip.name} (Vol: {finalVolume:F2}, Pitch: {finalPitch:F2})");
        }
        else
        {
            DebugLog($"No {soundCategory} audio available for {effectiveTyp} cube (fallback also checked)");
        }
    }
    
    /// <summary>
    /// Plays cube landing sound.
    /// </summary>
    public void PlayLandingSound()
    {
        PlayCubeAudio(SoundCategory.Landing);
    }
    
    /// <summary>
    /// Plays cube capture sound.
    /// </summary>
    public void PlayCaptureSound()
    {
        PlayCubeAudio(SoundCategory.Capture);
    }
    
    /// <summary>
    /// Plays cube destruction sound.
    /// </summary>
    public void PlayDestructionSound()
    {
        PlayCubeAudio(SoundCategory.Destruction);
    }
    
    /// <summary>
    /// Plays cube special effect sound.
    /// </summary>
    public void PlaySpecialEffectSound()
    {
        PlayCubeAudio(SoundCategory.SpecialEffect);
    }
    
    /// <summary>
    /// Handles cube capture audio and events.
    /// </summary>
    public void OnCubeCapture()
    {
        PlayCaptureSound();
        
        GameEvents.FireCubeCaptured(cube.position, cube.GetEffectiveType());
        DebugLog($"Fired GameEvents.OnCubeCaptured for {cube.GetEffectiveType()} cube at ({cube.position.x}, {cube.position.y})");
        DebugLog($"Cube {cube.GetEffectiveType()} captured - capture audio triggered");
    }
    #endregion

    #region Diagnostics
    /// <summary>
    /// Checks if audio system is configured and ready.
    /// </summary>
    public bool IsAudioSystemReady()
    {
        bool hasAudioSource = audioSource != null;
        bool hasAudioConfig = audioConfig != null;
        bool hasAudioManager = AudioManager.Instance != null;
        
        return hasAudioSource && (hasAudioConfig || hasAudioManager);
    }
    
    /// <summary>
    /// Gets diagnostic information about audio configuration.
    /// </summary>
    public string GetAudioDiagnostics()
    {
        var diagnostics = new StringBuilder();
        diagnostics.AppendLine($"=== Audio Diagnostics for {cube.type} Cube ===");
        diagnostics.AppendLine($"AudioSource: {(audioSource != null ? "Configured" : "Missing")}");
        diagnostics.AppendLine($"CubeAudioConfig: {(audioConfig != null ? "Assigned" : "Not Assigned")}");
        diagnostics.AppendLine($"AudioManager Available: {(AudioManager.Instance != null ? "Yes" : "No")}");
        diagnostics.AppendLine($"Audio System Ready: {IsAudioSystemReady()}");
        
        if (audioConfig != null)
        {
            var audioData = audioConfig.GetAudioData(cube.type);
            if (audioData != null)
            {
                diagnostics.AppendLine($"Audio Data Available: {audioData.HasAnyAudioClips()}");
                diagnostics.AppendLine($"Landing Clips: {(audioData.HasLandingClips() ? "Yes" : "No")}");
                diagnostics.AppendLine($"Capture Clips: {(audioData.HasCaptureClips() ? "Yes" : "No")}");
                diagnostics.AppendLine($"Destruction Clips: {(audioData.HasDestructionClips() ? "Yes" : "No")}");
                diagnostics.AppendLine($"Special Effect Clips: {(audioData.HasSpecialEffectClips() ? "Yes" : "No")}");
            }
            else
            {
                diagnostics.AppendLine($"No audio data found for cube type: {cube.type}");
            }
        }
        
        return diagnostics.ToString();
    }
    
    /// <summary>
    /// Stops audio playback and cleans up.
    /// </summary>
    public void Cleanup()
    {
        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }
    #endregion

    #region Debug
    public void SetDebugLogs(bool enabled)
    {
        enableDebugLogs = enabled;
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[CubeAudioHandler] {message}");
        }
    }
    #endregion
}

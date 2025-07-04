using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core audio playback system for the AudioManager.
/// Handles playing audio clips with volume control, spatial positioning, and playback tracking.
/// </summary>
public class AudioPlaybackSystem : MonoBehaviour
{
    #region Dependencies
    private AudioSourcePool audioSourcePool;
    private bool enableDebugLogs;
    private bool logAudioEvents;
    #endregion

    #region Runtime State
    private Dictionary<AudioClip, float> lastPlayedTimes = new Dictionary<AudioClip, float>();
    private int totalSoundsPlayed = 0;
    #endregion

    #region Properties
    public int TotalSoundsPlayed => totalSoundsPlayed;
    public bool IsInitialized { get; private set; }
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the playback system with required dependencies
    /// </summary>
    public void Initialize(AudioSourcePool sourcePool, bool debugLogs, bool logEvents)
    {
        audioSourcePool = sourcePool;
        enableDebugLogs = debugLogs;
        logAudioEvents = logEvents;
        
        lastPlayedTimes.Clear();
        totalSoundsPlayed = 0;
        
        IsInitialized = true;
        DebugLog("AudioPlaybackSystem initialized");
    }
    #endregion

    #region Core Playback Methods
    /// <summary>
    /// Plays an audio clip with volume control, spatial positioning, and automatic audio source return to pool
    /// </summary>
    /// <param name="clip">Audio clip to play</param>
    /// <param name="volume">Volume level (0-1)</param>
    /// <param name="position">World position for 3D spatial audio</param>
    /// <param name="pitch">Pitch adjustment (default 1.0)</param>
    public void PlayAudioClip(AudioClip clip, float volume = 1f, Vector3 position = default, float pitch = 1f)
    {
        if (!ValidatePlayback(clip, "PlayAudioClip"))
            return;

        // Check for rapid-fire prevention
        if (IsClipPlayedTooRecently(clip))
        {
            return;
        }

        AudioSource audioSource = audioSourcePool.GetAudioSource();
        if (audioSource == null)
        {
            if (enableDebugLogs)
            {
                DebugLog("Failed to get AudioSource for playback!");
            }
            return;
        }

        SetupAudioSourceForPlayback(audioSource, clip, volume, position, pitch);
        audioSource.Play();
        
        // Track the playback
        UpdatePlaybackTracking(clip, audioSource);
        
        // Schedule return to pool after clip finishes playing
        StartCoroutine(ReturnAudioSourceAfterPlayback(audioSource, clip.length / pitch));
        
        if (enableDebugLogs && logAudioEvents)
        {
            DebugLog($"Playing audio clip: {clip.name} at position {position} with volume {volume:F2} and pitch {pitch:F2}");
        }
    }

    /// <summary>
    /// Helper method to play audio clip with position and volume (for CubeManager integration)
    /// </summary>
    public void PlayAudioClip(AudioClip clip, Vector3 position, float volume)
    {
        PlayAudioClip(clip, volume, position, 1f);
    }

    /// <summary>
    /// Plays an audio clip with 3D spatial positioning
    /// </summary>
    public void PlaySpatialAudioClip(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (!ValidatePlayback(clip, "PlaySpatialAudioClip"))
            return;

        // Check for rapid-fire prevention
        if (IsClipPlayedTooRecently(clip))
        {
            return;
        }

        AudioSource audioSource = audioSourcePool.GetAvailableAudioSource();
        if (audioSource == null)
        {
            DebugLog("Failed to get AudioSource for spatial playback!");
            return;
        }

        // Set up spatial audio
        audioSource.transform.position = position;
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        audioSource.Play();
        
        // Track the playback
        UpdatePlaybackTracking(clip, audioSource);
        
        // Schedule return to pool after clip finishes
        StartCoroutine(ReturnAudioSourceAfterPlayback(audioSource, clip.length / pitch));
    }
    #endregion

    #region Playback Helpers
    /// <summary>
    /// Validates if playback can proceed
    /// </summary>
    private bool ValidatePlayback(AudioClip clip, string method)
    {
        if (clip == null)
        {
            if (enableDebugLogs)
            {
                DebugLog($"{method}: Attempted to play null AudioClip!");
            }
            return false;
        }

        if (!IsInitialized)
        {
            if (enableDebugLogs)
            {
                DebugLog($"{method}: AudioPlaybackSystem not initialized!");
            }
            return false;
        }

        if (audioSourcePool == null || !audioSourcePool.IsInitialized)
        {
            if (enableDebugLogs)
            {
                DebugLog($"{method}: AudioSourcePool not available!");
            }
            return false;
        }

        return true;
    }

    /// <summary>
    /// Checks if a clip was played too recently to prevent rapid-fire sounds
    /// </summary>
    private bool IsClipPlayedTooRecently(AudioClip clip)
    {
        if (lastPlayedTimes.ContainsKey(clip))
        {
            float timeSinceLastPlay = Time.time - lastPlayedTimes[clip];
            if (timeSinceLastPlay < 0.1f) // Prevent rapid-fire within 100ms
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Sets up audio source for playback with proper spatial positioning
    /// </summary>
    private void SetupAudioSourceForPlayback(AudioSource audioSource, AudioClip clip, float volume, Vector3 position, float pitch)
    {
        audioSource.clip = clip;
        audioSource.volume = volume;
        audioSource.pitch = pitch;
        
        // Set position for 3D audio if position is specified
        if (position != default)
        {
            // Configure for 3D spatial audio
            audioSource.transform.position = position;
            audioSource.spatialBlend = 1f; // Full 3D sound
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.maxDistance = 50f;
            audioSource.minDistance = 1f;
            audioSource.spread = 0f; // Directional sound
            audioSource.dopplerLevel = 0.1f; // Minimal doppler effect
        }
        else
        {
            // Configure for 2D audio
            audioSource.spatialBlend = 0f; // 2D sound
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }
    }

    /// <summary>
    /// Updates playback tracking information
    /// </summary>
    private void UpdatePlaybackTracking(AudioClip clip, AudioSource audioSource)
    {
        lastPlayedTimes[clip] = Time.time;
        totalSoundsPlayed++;
    }

    /// <summary>
    /// Coroutine to automatically return audio source to pool after playback
    /// </summary>
    private IEnumerator ReturnAudioSourceAfterPlayback(AudioSource audioSource, float delay)
    {
        yield return new WaitForSeconds(delay + 0.1f); // Small buffer for safety
        
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSourcePool.ReturnAudioSource(audioSource);
        }
    }
    #endregion

    #region Volume Management
    /// <summary>
    /// Updates the debug logging settings
    /// </summary>
    public void UpdateDebugSettings(bool debugLogs, bool logEvents)
    {
        enableDebugLogs = debugLogs;
        logAudioEvents = logEvents;
    }
    #endregion

    #region Cleanup
    /// <summary>
    /// Cleans up the playback system
    /// </summary>
    public void Cleanup()
    {
        StopAllCoroutines();
        lastPlayedTimes.Clear();
        IsInitialized = false;
        DebugLog("AudioPlaybackSystem cleaned up");
    }
    #endregion

    #region Debug
    /// <summary>
    /// Gets debug information about the playback system
    /// </summary>
    public Dictionary<string, object> GetDebugInfo()
    {
        return new Dictionary<string, object>
        {
            ["Total Sounds Played"] = totalSoundsPlayed,
            ["Tracked Clips"] = lastPlayedTimes.Count,
            ["Is Initialized"] = IsInitialized
        };
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[AudioPlaybackSystem] {message}");
        }
    }
    #endregion
}

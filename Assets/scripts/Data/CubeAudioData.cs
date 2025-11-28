using System;
using UnityEngine;
using static Enumerations;

[Serializable]
public class CubeAudioData
{
    [Header("Cube Type Configuration")]
    public CubeType cubeType = CubeType.Unit;
    
    [Header("Landing Sounds")]
    public AudioClipSet landingSounds = new AudioClipSet();
    
    [Header("Capture Sounds")]
    public AudioClipSet captureSounds = new AudioClipSet();
    
    [Header("Destruction Sounds")]
    public AudioClipSet destructionSounds = new AudioClipSet();
    
    [Header("Special Effect Sounds")]
    public AudioClipSet specialEffectSounds = new AudioClipSet();
    
    /// <summary>
    /// Gets a random audio clip from the specified sound category
    /// </summary>
    /// <param name="soundCategory">The category of sound to retrieve</param>
    /// <returns>Random AudioClip from the category, or null if category is empty</returns>
    public AudioClip GetRandomClip(SoundCategory soundCategory)
    {
        AudioClipSet clipSet = GetClipSet(soundCategory);
        return clipSet?.GetRandomClip();
    }
    
    /// <summary>
    /// Gets the AudioClipSet for the specified sound category
    /// </summary>
    /// <param name="soundCategory">The category of sound to retrieve</param>
    /// <returns>AudioClipSet for the category, or null if invalid category</returns>
    public AudioClipSet GetClipSet(SoundCategory soundCategory)
    {
        switch (soundCategory)
        {
            case SoundCategory.Landing:
                return landingSounds;
            case SoundCategory.Capture:
                return captureSounds;
            case SoundCategory.Destruction:
                return destructionSounds;
            case SoundCategory.SpecialEffect:
                return specialEffectSounds;
            default:
                return null;
        }
    }
    
    /// <summary>
    /// Validates that the audio data has at least one clip assigned in any category
    /// </summary>
    /// <returns>True if any audio clips are assigned, false if all categories are empty</returns>
    public bool HasAnyAudioClips()
    {
        return landingSounds.HasClips() || 
               captureSounds.HasClips() || 
               destructionSounds.HasClips() || 
               specialEffectSounds.HasClips();
    }
    
    /// <summary>
    /// Gets diagnostic information about this audio data
    /// </summary>
    /// <returns>String containing clip count information</returns>
    public string GetDiagnosticInfo()
    {
        return $"CubeType: {cubeType} | " +
               $"Landing: {landingSounds.clips?.Length ?? 0} | " +
               $"Capture: {captureSounds.clips?.Length ?? 0} | " +
               $"Destruction: {destructionSounds.clips?.Length ?? 0} | " +
               $"Special: {specialEffectSounds.clips?.Length ?? 0}";
    }
    
    /// <summary>
    /// Tests all audio clips in this data and returns validation results
    /// </summary>
    /// <returns>Tuple containing total clip count and valid clip count</returns>
    public (int total, int valid) TestAudioClips()
    {
        int total = 0;
        int valid = 0;
        
        // Test each category
        var categories = new[] 
        {
            ("Landing", landingSounds),
            ("Capture", captureSounds),
            ("Destruction", destructionSounds),
            ("Special Effect", specialEffectSounds)
        };
        
        foreach (var (categoryName, clipSet) in categories)
        {
            if (clipSet != null && clipSet.clips != null)
            {
                foreach (var clip in clipSet.clips)
                {
                    total++;
                    if (clip != null)
                    {
                        valid++;
                        Debug.Log($"[CubeAudioData] ✓ {cubeType}.{categoryName}: {clip.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[CubeAudioData] ✗ {cubeType}.{categoryName}: Null clip found");
                    }
                }
            }
        }
        
        return (total, valid);
    }
    
    /// <summary>
    /// Checks if this audio data has any landing clips configured
    /// </summary>
    /// <returns>True if landing sounds are available</returns>
    public bool HasLandingClips()
    {
        return landingSounds != null && landingSounds.HasClips();
    }
    
    /// <summary>
    /// Checks if this audio data has any capture clips configured
    /// </summary>
    /// <returns>True if capture sounds are available</returns>
    public bool HasCaptureClips()
    {
        return captureSounds != null && captureSounds.HasClips();
    }
    
    /// <summary>
    /// Checks if this audio data has any destruction clips configured
    /// </summary>
    /// <returns>True if destruction sounds are available</returns>
    public bool HasDestructionClips()
    {
        return destructionSounds != null && destructionSounds.HasClips();
    }
    
    /// <summary>
    /// Checks if this audio data has any special effect clips configured
    /// </summary>
    /// <returns>True if special effect sounds are available</returns>
    public bool HasSpecialEffectClips()
    {
        return specialEffectSounds != null && specialEffectSounds.HasClips();
    }
}

[Serializable]
public class AudioClipSet
{
    [Header("Audio Clips")]
    public AudioClip[] clips = new AudioClip[0];
    
    [Header("Playback Settings")]
    [Range(0.8f, 1.2f)]
    public float minPitch = 0.9f;
    
    [Range(0.8f, 1.2f)]
    public float maxPitch = 1.1f;
    
    [Range(0f, 1f)]
    public float volume = 1f;
    
    [Header("Variation Settings")]
    public bool enablePitchVariation = true;
    public bool enableVolumeVariation = false;
    
    [Range(0f, 0.2f)]
    public float volumeVariationRange = 0.1f;
    
    /// <summary>
    /// Gets a random audio clip from this set
    /// </summary>
    /// <returns>Random AudioClip, or null if no clips are available</returns>
    public AudioClip GetRandomClip()
    {
        if (clips == null || clips.Length == 0)
            return null;
            
        // Filter out null clips
        AudioClip[] validClips = System.Array.FindAll(clips, clip => clip != null);
        
        if (validClips.Length == 0)
            return null;
            
        return validClips[UnityEngine.Random.Range(0, validClips.Length)];
    }
    
    /// <summary>
    /// Gets a random pitch value within the configured range
    /// </summary>
    /// <returns>Random pitch value between minPitch and maxPitch</returns>
    public float GetRandomPitch()
    {
        if (!enablePitchVariation)
            return 1f;
            
        return UnityEngine.Random.Range(minPitch, maxPitch);
    }
    
    /// <summary>
    /// Gets a volume value with optional variation
    /// </summary>
    /// <returns>Volume value with random variation if enabled</returns>
    public float GetVolume()
    {
        if (!enableVolumeVariation)
            return volume;
            
        float variation = UnityEngine.Random.Range(-volumeVariationRange, volumeVariationRange);
        return Mathf.Clamp01(volume + variation);
    }
    
    /// <summary>
    /// Checks if this clip set has any valid audio clips
    /// </summary>
    /// <returns>True if at least one non-null clip is available</returns>
    public bool HasClips()
    {
        if (clips == null || clips.Length == 0)
            return false;
            
        foreach (AudioClip clip in clips)
        {
            if (clip != null)
                return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets the count of valid (non-null) audio clips
    /// </summary>
    /// <returns>Number of valid audio clips</returns>
    public int GetValidClipCount()
    {
        if (clips == null || clips.Length == 0)
            return 0;
            
        int count = 0;
        foreach (AudioClip clip in clips)
        {
            if (clip != null)
                count++;
        }
        
        return count;
    }
    
    /// <summary>
    /// Gets all audio clips in this set (including null entries)
    /// </summary>
    /// <returns>Array of all audio clips</returns>
    public AudioClip[] GetAllClips()
    {
        return clips ?? new AudioClip[0];
    }
}

/// <summary>
/// Enumeration for different sound categories
/// </summary>
public enum SoundCategory
{
    Landing,
    Capture,
    Destruction,
    SpecialEffect
}

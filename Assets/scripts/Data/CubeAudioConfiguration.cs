using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CubeAudioConfiguration", menuName = "Infinity Qube/Cube Audio Configuration")]
public class CubeAudioConfiguration : ScriptableObject
{
    [Header("Cube Audio Configuration")]
    [SerializeField] 
    public List<CubeAudioData> audioData = new List<CubeAudioData>();
    
    [Header("Fallback Audio Settings")]
    [SerializeField]
    public AudioClipSet fallbackLandingSounds = new AudioClipSet();
    
    [SerializeField]
    public AudioClipSet fallbackCaptureSounds = new AudioClipSet();
    
    [SerializeField]
    public AudioClipSet fallbackDestructionSounds = new AudioClipSet();
    
    [SerializeField]
    public AudioClipSet fallbackSpecialEffectSounds = new AudioClipSet();
    
    [Header("Global Audio Settings")]
    [Range(0f, 1f)]
    public float globalCubeAudioVolume = 0.8f;
    
    [Range(0.5f, 2f)]
    public float globalPitchModifier = 1f;
    
    public bool enableAudioDebugLogs = false;
    
    /// <summary>
    /// Gets audio data for a specific cube type
    /// </summary>
    /// <param name="cubeType">The cube type to get audio data for</param>
    /// <returns>CubeAudioData for the specified type, or null if not found</returns>
    public CubeAudioData GetAudioData(Enumerations.CubeType cubeType)
    {
        if (audioData == null)
            return null;
            
        foreach (CubeAudioData data in audioData)
        {
            if (data != null && data.cubeType == cubeType)
                return data;
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets a random audio clip for a specific cube type and sound category
    /// </summary>
    /// <param name="cubeType">The cube type</param>
    /// <param name="soundCategory">The sound category</param>
    /// <returns>Random AudioClip, or fallback clip if cube-specific clip not available</returns>
    public AudioClip GetRandomClip(Enumerations.CubeType cubeType, SoundCategory soundCategory)
    {
        // Try to get cube-specific audio first
        CubeAudioData cubeAudio = GetAudioData(cubeType);
        AudioClip clip = cubeAudio?.GetRandomClip(soundCategory);
        
        // If no cube-specific audio, try fallback
        if (clip == null)
        {
            clip = GetFallbackClip(soundCategory);
            
            if (enableAudioDebugLogs && clip != null)
            {
                Debug.Log($"[CubeAudioConfiguration] Using fallback audio for {cubeType}.{soundCategory}: {clip.name}");
            }
        }
        else if (enableAudioDebugLogs)
        {
            Debug.Log($"[CubeAudioConfiguration] Using cube-specific audio for {cubeType}.{soundCategory}: {clip.name}");
        }
        
        return clip;
    }
    
    /// <summary>
    /// Gets audio playback settings for a specific cube type and sound category
    /// </summary>
    /// <param name="cubeType">The cube type</param>
    /// <param name="soundCategory">The sound category</param>
    /// <returns>AudioPlaybackSettings with volume and pitch information</returns>
    public AudioPlaybackSettings GetPlaybackSettings(Enumerations.CubeType cubeType, SoundCategory soundCategory)
    {
        CubeAudioData cubeAudio = GetAudioData(cubeType);
        AudioClipSet clipSet = cubeAudio?.GetClipSet(soundCategory);
        
        // If no cube-specific settings, use fallback
        if (clipSet == null)
        {
            clipSet = GetFallbackClipSet(soundCategory);
        }
        
        if (clipSet == null)
        {
            return new AudioPlaybackSettings
            {
                volume = globalCubeAudioVolume,
                pitch = globalPitchModifier
            };
        }
        
        return new AudioPlaybackSettings
        {
            volume = clipSet.GetVolume() * globalCubeAudioVolume,
            pitch = clipSet.GetRandomPitch() * globalPitchModifier
        };
    }
    
    /// <summary>
    /// Gets a fallback audio clip for the specified sound category
    /// </summary>
    /// <param name="soundCategory">The sound category</param>
    /// <returns>Fallback AudioClip, or null if not available</returns>
    public AudioClip GetFallbackClip(SoundCategory soundCategory)
    {
        AudioClipSet fallbackSet = GetFallbackClipSet(soundCategory);
        return fallbackSet?.GetRandomClip();
    }
    
    /// <summary>
    /// Gets the fallback AudioClipSet for the specified sound category
    /// </summary>
    /// <param name="soundCategory">The sound category</param>
    /// <returns>Fallback AudioClipSet, or null if invalid category</returns>
    public AudioClipSet GetFallbackClipSet(SoundCategory soundCategory)
    {
        switch (soundCategory)
        {
            case SoundCategory.Landing:
                return fallbackLandingSounds;
            case SoundCategory.Capture:
                return fallbackCaptureSounds;
            case SoundCategory.Destruction:
                return fallbackDestructionSounds;
            case SoundCategory.SpecialEffect:
                return fallbackSpecialEffectSounds;
            default:
                return null;
        }
    }
    
    /// <summary>
    /// Validates the audio configuration and logs any issues
    /// </summary>
    /// <returns>True if configuration is valid, false if issues were found</returns>
    public bool ValidateConfiguration()
    {
        bool isValid = true;
        List<string> issues = new List<string>();
        
        // Check if we have audio data for all cube types
        System.Array cubeTypes = System.Enum.GetValues(typeof(Enumerations.CubeType));
        foreach (Enumerations.CubeType cubeType in cubeTypes)
        {
            CubeAudioData data = GetAudioData(cubeType);
            if (data == null)
            {
                issues.Add($"Missing audio data for cube type: {cubeType}");
                isValid = false;
            }
            else if (!data.HasAnyAudioClips())
            {
                issues.Add($"No audio clips assigned for cube type: {cubeType}");
            }
        }
        
        // Check fallback audio
        if (!fallbackLandingSounds.HasClips() && 
            !fallbackCaptureSounds.HasClips() && 
            !fallbackDestructionSounds.HasClips() && 
            !fallbackSpecialEffectSounds.HasClips())
        {
            issues.Add("No fallback audio clips assigned in any category");
            isValid = false;
        }
        
        // Log results
        if (issues.Count > 0)
        {
            string issueList = string.Join("\n- ", issues);
            Debug.LogWarning($"[CubeAudioConfiguration] Validation issues found:\n- {issueList}");
        }
        else
        {
            Debug.Log("[CubeAudioConfiguration] Configuration validation passed successfully");
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Gets diagnostic information about the current configuration
    /// </summary>
    /// <returns>String containing diagnostic information</returns>
    public string GetDiagnosticInfo()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Cube Audio Configuration Diagnostics ===");
        sb.AppendLine($"Global Volume: {globalCubeAudioVolume:F2}");
        sb.AppendLine($"Global Pitch Modifier: {globalPitchModifier:F2}");
        sb.AppendLine($"Debug Logs Enabled: {enableAudioDebugLogs}");
        sb.AppendLine();
        
        sb.AppendLine("Cube-Specific Audio Data:");
        if (audioData != null && audioData.Count > 0)
        {
            foreach (CubeAudioData data in audioData)
            {
                if (data != null)
                {
                    sb.AppendLine($"- {data.GetDiagnosticInfo()}");
                }
            }
        }
        else
        {
            sb.AppendLine("- No cube-specific audio data configured");
        }
        
        sb.AppendLine();
        sb.AppendLine("Fallback Audio:");
        sb.AppendLine($"- Landing: {fallbackLandingSounds.GetValidClipCount()} clips");
        sb.AppendLine($"- Capture: {fallbackCaptureSounds.GetValidClipCount()} clips");
        sb.AppendLine($"- Destruction: {fallbackDestructionSounds.GetValidClipCount()} clips");
        sb.AppendLine($"- Special Effect: {fallbackSpecialEffectSounds.GetValidClipCount()} clips");
        
        return sb.ToString();
    }
    
    /// <summary>
    /// Test method to validate all audio clips are properly assigned and playable
    /// </summary>
    [ContextMenu("Test All Audio Clips")]
    public void TestAllAudioClips()
    {
        Debug.Log("[CubeAudioConfiguration] Testing all audio clips...");
        
        int totalClips = 0;
        int validClips = 0;
        
        // Test cube-specific audio
        if (audioData != null)
        {
            foreach (CubeAudioData data in audioData)
            {
                if (data != null)
                {
                    var clipCounts = data.TestAudioClips();
                    totalClips += clipCounts.total;
                    validClips += clipCounts.valid;
                }
            }
        }
        
        // Test fallback audio
        var fallbackCounts = TestFallbackAudio();
        totalClips += fallbackCounts.total;
        validClips += fallbackCounts.valid;
        
        Debug.Log($"[CubeAudioConfiguration] Audio clip test complete: {validClips}/{totalClips} clips are valid");
        
        if (validClips == totalClips && totalClips > 0)
        {
            Debug.Log("[CubeAudioConfiguration] ✓ All audio clips passed validation!");
        }
        else if (totalClips == 0)
        {
            Debug.LogWarning("[CubeAudioConfiguration] ⚠ No audio clips configured for testing");
        }
        else
        {
            Debug.LogWarning($"[CubeAudioConfiguration] ⚠ {totalClips - validClips} audio clips failed validation");
        }
    }
    
    /// <summary>
    /// Tests fallback audio clips and returns count information
    /// </summary>
    /// <returns>Tuple containing total and valid clip counts</returns>
    private (int total, int valid) TestFallbackAudio()
    {
        int total = 0;
        int valid = 0;
        
        // Test each fallback category
        var fallbackSets = new[] 
        {
            ("Landing", fallbackLandingSounds),
            ("Capture", fallbackCaptureSounds),
            ("Destruction", fallbackDestructionSounds),
            ("Special Effect", fallbackSpecialEffectSounds)
        };
        
        foreach (var (category, clipSet) in fallbackSets)
        {
            if (clipSet != null && clipSet.HasClips())
            {
                var clips = clipSet.GetAllClips();
                foreach (var clip in clips)
                {
                    total++;
                    if (clip != null)
                    {
                        valid++;
                        if (enableAudioDebugLogs)
                        {
                            Debug.Log($"[CubeAudioConfiguration] ✓ Fallback {category}: {clip.name}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[CubeAudioConfiguration] ✗ Fallback {category}: Null clip found");
                    }
                }
            }
        }
        
        return (total, valid);
    }
    
    /// <summary>
    /// Enhanced validation with detailed error reporting and suggestions
    /// </summary>
    [ContextMenu("Validate Configuration")]
    public bool ValidateConfigurationWithDetails()
    {
        Debug.Log("[CubeAudioConfiguration] Starting detailed configuration validation...");
        
        bool isValid = true;
        List<string> errors = new List<string>();
        List<string> warnings = new List<string>();
        List<string> suggestions = new List<string>();
        
        // Check if we have audio data for all cube types
        System.Array cubeTypes = System.Enum.GetValues(typeof(Enumerations.CubeType));
        int configuredTypes = 0;
        
        foreach (Enumerations.CubeType cubeType in cubeTypes)
        {
            CubeAudioData data = GetAudioData(cubeType);
            if (data == null)
            {
                errors.Add($"Missing audio data for cube type: {cubeType}");
                isValid = false;
            }
            else if (!data.HasAnyAudioClips())
            {
                warnings.Add($"No audio clips assigned for cube type: {cubeType}");
            }
            else
            {
                configuredTypes++;
                
                // Check individual sound categories
                if (!data.HasLandingClips())
                {
                    warnings.Add($"{cubeType}: No landing sounds configured");
                }
                if (!data.HasCaptureClips())
                {
                    suggestions.Add($"{cubeType}: Consider adding capture sounds for better feedback");
                }
                if (!data.HasDestructionClips())
                {
                    suggestions.Add($"{cubeType}: Consider adding destruction sounds");
                }
            }
        }
        
        // Check fallback audio
        bool hasFallbacks = fallbackLandingSounds.HasClips() || 
                           fallbackCaptureSounds.HasClips() || 
                           fallbackDestructionSounds.HasClips() || 
                           fallbackSpecialEffectSounds.HasClips();
        
        if (!hasFallbacks)
        {
            errors.Add("No fallback audio clips assigned in any category");
            isValid = false;
        }
        else
        {
            // Check individual fallback categories
            if (!fallbackLandingSounds.HasClips())
            {
                warnings.Add("No fallback landing sounds - ensure cube-specific sounds cover all types");
            }
            if (!fallbackCaptureSounds.HasClips())
            {
                suggestions.Add("Consider adding fallback capture sounds for better audio coverage");
            }
        }
        
        // Volume validation
        if (globalCubeAudioVolume <= 0f)
        {
            warnings.Add($"Global cube audio volume is very low ({globalCubeAudioVolume:F2}) - audio may be inaudible");
        }
        if (globalCubeAudioVolume > 1f)
        {
            warnings.Add($"Global cube audio volume exceeds 1.0 ({globalCubeAudioVolume:F2}) - may cause audio distortion");
        }
        
        // Pitch validation
        if (globalPitchModifier < 0.5f || globalPitchModifier > 2f)
        {
            warnings.Add($"Global pitch modifier ({globalPitchModifier:F2}) is outside recommended range (0.5-2.0)");
        }
        
        // Report results
        Debug.Log($"[CubeAudioConfiguration] Configuration coverage: {configuredTypes}/{cubeTypes.Length} cube types");
        
        if (errors.Count > 0)
        {
            string errorList = string.Join("\n• ", errors);
            Debug.LogError($"[CubeAudioConfiguration] ❌ VALIDATION ERRORS:\n• {errorList}");
        }
        
        if (warnings.Count > 0)
        {
            string warningList = string.Join("\n• ", warnings);
            Debug.LogWarning($"[CubeAudioConfiguration] ⚠ WARNINGS:\n• {warningList}");
        }
        
        if (suggestions.Count > 0)
        {
            string suggestionList = string.Join("\n• ", suggestions);
            Debug.Log($"[CubeAudioConfiguration] 💡 SUGGESTIONS:\n• {suggestionList}");
        }
        
        if (isValid && warnings.Count == 0)
        {
            Debug.Log("[CubeAudioConfiguration] ✓ Configuration validation passed with no issues!");
        }
        else if (isValid)
        {
            Debug.Log("[CubeAudioConfiguration] ✓ Configuration validation passed with warnings");
        }
        
        return isValid;
    }
    
    /// <summary>
    /// Ensure we have audio data entries for all cube types on validation
    /// </summary>
    private void OnValidate()
    {
        if (audioData == null)
        {
            audioData = new List<CubeAudioData>();
        }
        
        // Ensure we have an entry for each cube type
        System.Array cubeTypes = System.Enum.GetValues(typeof(Enumerations.CubeType));
        foreach (Enumerations.CubeType cubeType in cubeTypes)
        {
            bool hasEntry = false;
            foreach (CubeAudioData data in audioData)
            {
                if (data != null && data.cubeType == cubeType)
                {
                    hasEntry = true;
                    break;
                }
            }
            
            if (!hasEntry)
            {
                CubeAudioData newData = new CubeAudioData();
                newData.cubeType = cubeType;
                audioData.Add(newData);
            }
        }
        
        // Remove any duplicate entries for the same cube type
        for (int i = audioData.Count - 1; i >= 0; i--)
        {
            if (audioData[i] == null) continue;
            
            for (int j = i - 1; j >= 0; j--)
            {
                if (audioData[j] != null && audioData[j].cubeType == audioData[i].cubeType)
                {
                    audioData.RemoveAt(i);
                    break;
                }
            }
        }
        
        // Sort by cube type for better organization in Inspector
        audioData.Sort((a, b) => a.cubeType.CompareTo(b.cubeType));
    }
}

/// <summary>
/// Helper struct for audio playback settings
/// </summary>
[System.Serializable]
public struct AudioPlaybackSettings
{
    public float volume;
    public float pitch;
    
    public AudioPlaybackSettings(float volume = 1f, float pitch = 1f)
    {
        this.volume = volume;
        this.pitch = pitch;
    }
}

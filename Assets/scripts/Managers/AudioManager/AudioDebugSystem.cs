using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles all testing, debugging, and validation functionality for the AudioManager system.
/// Implements IManagerDebugInterface and provides comprehensive testing tools.
/// </summary>
public class AudioDebugSystem : MonoBehaviour
{
    #region Dependencies
    private AudioSourcePool audioSourcePool;
    private AudioPlaybackSystem playbackSystem;
    private AudioVolumeController volumeController;
    private CubeAudioSystem cubeAudioSystem;
    private CubeAudioConfiguration cubeAudioConfiguration;
    #endregion

    #region Debug Configuration
    [Header("Debug Options")]
    [SerializeField] private bool enableDebugLogs = true;
    [SerializeField] private bool showAudioGizmos = false;
    [SerializeField] private bool logAudioEvents = false;
    
    [Header("Testing Tools")]
    [Range(0f, 1f)]
    [Tooltip("Volume slider for real-time audio testing")]
    [SerializeField] private float testingVolume = 0.8f;
    
    [Space(5)]
    [Tooltip("Use context menu 'Test Audio System' to test all cube types")]
    [SerializeField] private bool showTestingInstructions = true;
    #endregion

    #region Properties
    public bool EnableDebugLogs 
    { 
        get => enableDebugLogs; 
        set => enableDebugLogs = value; 
    }
    
    public bool ShowAudioGizmos => showAudioGizmos;
    public bool LogAudioEvents => logAudioEvents;
    public float TestingVolume => testingVolume;
    public bool IsInitialized { get; private set; }
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the debug system with required dependencies
    /// </summary>
    public void Initialize(AudioSourcePool pool, AudioPlaybackSystem playback, 
        AudioVolumeController volume, CubeAudioSystem cube, CubeAudioConfiguration config)
    {
        audioSourcePool = pool;
        playbackSystem = playback;
        volumeController = volume;
        cubeAudioSystem = cube;
        cubeAudioConfiguration = config;
        
        IsInitialized = true;
        DebugLog("AudioDebugSystem initialized");
    }
    #endregion

    #region IManagerDebugInterface Implementation
    /// <summary>
    /// Gets a human-readable string describing the current status of the audio system
    /// </summary>
    public string GetDebugStatus()
    {
        if (!IsInitialized)
        {
            return "Audio System: Not Initialized";
        }

        int activeSourceCount = audioSourcePool?.ActiveCount ?? 0;
        int availableSourceCount = audioSourcePool?.AvailableCount ?? 0;
        int totalSoundsPlayed = playbackSystem?.TotalSoundsPlayed ?? 0;
        float masterVolume = volumeController?.MasterVolume ?? 0f;
        
        return $"Audio System: Active | Sources: {activeSourceCount}/{availableSourceCount + activeSourceCount} | " +
               $"Sounds Played: {totalSoundsPlayed} | Master Vol: {masterVolume:F2}";
    }

    /// <summary>
    /// Gets comprehensive debug data for the audio system
    /// </summary>
    public Dictionary<string, object> GetDebugData()
    {
        var debugData = new Dictionary<string, object>
        {
            ["System Initialized"] = IsInitialized,
            ["Debug Logs Enabled"] = enableDebugLogs,
            ["Audio Gizmos Enabled"] = showAudioGizmos,
            ["Log Audio Events"] = logAudioEvents,
            ["Testing Volume"] = $"{testingVolume:F2}"
        };

        // Add pool data
        if (audioSourcePool != null && audioSourcePool.IsInitialized)
        {
            var poolData = audioSourcePool.GetDebugInfo();
            foreach (var kvp in poolData)
            {
                debugData[$"Pool: {kvp.Key}"] = kvp.Value;
            }
        }

        // Add playback data
        if (playbackSystem != null && playbackSystem.IsInitialized)
        {
            var playbackData = playbackSystem.GetDebugInfo();
            foreach (var kvp in playbackData)
            {
                debugData[$"Playback: {kvp.Key}"] = kvp.Value;
            }
        }

        // Add volume data
        if (volumeController != null && volumeController.IsInitialized)
        {
            var volumeData = volumeController.GetDebugInfo();
            foreach (var kvp in volumeData)
            {
                debugData[$"Volume: {kvp.Key}"] = kvp.Value;
            }
        }

        // Add cube audio data
        if (cubeAudioSystem != null && cubeAudioSystem.IsInitialized)
        {
            var cubeData = cubeAudioSystem.GetDebugInfo();
            foreach (var kvp in cubeData)
            {
                debugData[$"Cube Audio: {kvp.Key}"] = kvp.Value;
            }
        }

        return debugData;
    }
    #endregion

    #region Testing Methods
    /// <summary>
    /// Tests the entire audio system by playing sounds for all cube types
    /// </summary>
    [ContextMenu("Test Audio System")]
    public void TestAudioSystem()
    {
        if (!ValidateForTesting("TestAudioSystem"))
            return;
        
        DebugLog("=== AUDIO SYSTEM TEST STARTED ===");
        
        // Test all cube types
        System.Array cubeTypes = System.Enum.GetValues(typeof(Enumerations.CubeType));
        Vector3 testPosition = transform.position;
        
        foreach (Enumerations.CubeType cubeType in cubeTypes)
        {
            DebugLog($"Testing audio for cube type: {cubeType}");
            TestCubeLandingSound(cubeType);
            
            // Small delay between tests to avoid overwhelming the audio system
            System.Threading.Thread.Sleep(200);
        }
        
        DebugLog("=== AUDIO SYSTEM TEST COMPLETED ===");
        DebugPrintAudioInfo();
    }

    /// <summary>
    /// Tests landing sound for a specific cube type with current testing volume
    /// </summary>
    public void TestCubeLandingSound(Enumerations.CubeType cubeType)
    {
        if (!ValidateForTesting("TestCubeLandingSound"))
            return;
        
        Vector3 testPosition = transform.position + Vector3.right * UnityEngine.Random.Range(-2f, 2f);
        
        // Store original volume and use testing volume
        float originalImpactVolume = volumeController.CubeImpactVolume;
        volumeController.SetVolumeForCategory(VolumeCategory.CubeImpact, testingVolume, false);
        
        DebugLog($"Testing {cubeType} landing sound at position {testPosition} with volume {testingVolume:F2}");
        
        try
        {
            cubeAudioSystem.PlayCubeLandingSound(cubeType, testPosition);
        }
        catch (System.Exception ex)
        {
            DebugLog($"Error testing {cubeType} landing sound: {ex.Message}");
        }
        finally
        {
            // Restore original volume
            volumeController.SetVolumeForCategory(VolumeCategory.CubeImpact, originalImpactVolume, false);
        }
    }

    /// <summary>
    /// Tests real-time volume adjustment by playing a test sound
    /// </summary>
    [ContextMenu("Test Volume Adjustment")]
    public void TestVolumeAdjustment()
    {
        if (!ValidateForTesting("TestVolumeAdjustment"))
            return;
        
        // Play a test sound using the testing volume slider
        var cubeImpactSounds = GetTestAudioClips();
        if (cubeImpactSounds != null && cubeImpactSounds.Length > 0)
        {
            AudioClip testClip = cubeImpactSounds[0];
            Vector3 testPosition = transform.position;
            
            DebugLog($"Testing volume adjustment: {testingVolume:F2} volume");
            playbackSystem.PlayAudioClip(testClip, testingVolume, testPosition);
        }
        else
        {
            DebugLog("No cube impact sounds available for volume testing");
        }
    }

    /// <summary>
    /// Tests wave composition system
    /// </summary>
    [ContextMenu("Test Wave Composition")]
    public void TestWaveComposition()
    {
        DebugLog("Wave composition testing is handled by the main AudioManager");
    }

    /// <summary>
    /// Tests system feedback sounds
    /// </summary>
    [ContextMenu("Test System Feedback")]
    public void TestSystemFeedback()
    {
        DebugLog("System feedback testing is handled by the main AudioManager");
    }

    /// <summary>
    /// Tests background music system
    /// </summary>
    [ContextMenu("Test Background Music")]
    public void TestBackgroundMusic()
    {
        DebugLog("Background music testing is handled by the main AudioManager");
    }

    /// <summary>
    /// Tests volume control system
    /// </summary>
    [ContextMenu("Test Volume Controls")]
    public void TestVolumeControls()
    {
        if (!ValidateForTesting("TestVolumeControls"))
            return;

        DebugLog("=== VOLUME CONTROL SYSTEM TEST STARTED ===");
        
        // Test getting current volume levels for all categories
        foreach (VolumeCategory category in System.Enum.GetValues(typeof(VolumeCategory)))
        {
            float currentVolume = volumeController.GetCurrentVolumeLevel(category);
            float effectiveVolume = volumeController.GetEffectiveVolumeLevel(category);
            DebugLog($"{category}: Current={currentVolume:F2}, Effective={effectiveVolume:F2}");
        }
        
        DebugLog("=== VOLUME CONTROL SYSTEM TEST COMPLETED ===");
    }
    #endregion

    #region Validation Methods
    /// <summary>
    /// Validates and sets up proper audio folder structure for the project
    /// </summary>
    [ContextMenu("Validate Audio Folder Structure")]
    public void ValidateAudioFolderStructure()
    {
        DebugLog("=== AUDIO FOLDER STRUCTURE VALIDATION ===");
        
        // Check if Audio folder exists
        string audioFolderPath = "Assets/Audio";
        bool audioFolderExists = System.IO.Directory.Exists(audioFolderPath);
        
        if (audioFolderExists)
        {
            DebugLog("✓ Audio folder found at: " + audioFolderPath);
            
            // Check for recommended subfolders
            string[] recommendedFolders = { "CubeLanding", "CubeCapture", "CubeDestruction", "SpecialEffects", "Fallback" };
            
            foreach (string folder in recommendedFolders)
            {
                string fullPath = System.IO.Path.Combine(audioFolderPath, folder);
                if (System.IO.Directory.Exists(fullPath))
                {
                    DebugLog($"✓ {folder} subfolder found");
                }
                else
                {
                    DebugLog($"⚠ {folder} subfolder missing - recommended for organization");
                }
            }
        }
        else
        {
            DebugLog("⚠ Audio folder not found. Consider creating: " + audioFolderPath);
        }
        
        // Validate current audio clip assignments
        ValidateAudioClipAssignments();
    }

    /// <summary>
    /// Validates all audio clip assignments and provides helpful warnings
    /// </summary>
    public void ValidateAudioClipAssignments()
    {
        DebugLog("=== AUDIO CLIP ASSIGNMENT VALIDATION ===");
        
        List<string> issues = new List<string>();
        List<string> warnings = new List<string>();
        
        // Check CubeAudioConfiguration
        if (cubeAudioConfiguration == null)
        {
            issues.Add("CubeAudioConfiguration not assigned! Create one using: Assets > Create > Infinity Qube > Cube Audio Configuration");
        }
        else
        {
            DebugLog("✓ CubeAudioConfiguration assigned");
            bool configValid = cubeAudioConfiguration.ValidateConfiguration();
            if (!configValid)
            {
                warnings.Add("CubeAudioConfiguration has validation issues - see previous log messages");
            }
        }
        
        // Report results
        if (issues.Count > 0)
        {
            string issueList = string.Join("\n• ", issues);
            DebugLog($"❌ CRITICAL ISSUES FOUND:\n• {issueList}");
        }
        
        if (warnings.Count > 0)
        {
            string warningList = string.Join("\n• ", warnings);
            DebugLog($"⚠ WARNINGS:\n• {warningList}");
        }
        
        if (issues.Count == 0 && warnings.Count == 0)
        {
            DebugLog("✓ All audio clip assignments validated successfully!");
        }
    }

    /// <summary>
    /// Creates a default CubeAudioConfiguration ScriptableObject for initial setup
    /// </summary>
    [ContextMenu("Create Default Audio Configuration")]
    public void CreateDefaultAudioConfiguration()
    {
        DebugLog("=== CREATING DEFAULT AUDIO CONFIGURATION ===");
        
        if (cubeAudioConfiguration != null)
        {
            DebugLog("CubeAudioConfiguration already assigned. Use 'Validate Audio Folder Structure' to check current setup.");
            return;
        }
        
        // This is a hint for developers - actual ScriptableObject creation must be done through Unity's Asset menu
        DebugLog("To create a default audio configuration:");
        DebugLog("1. Right-click in Project window");
        DebugLog("2. Go to Create > Infinity Qube > Cube Audio Configuration");
        DebugLog("3. Name it 'DefaultCubeAudioConfiguration'");
        DebugLog("4. Assign it to the 'cubeAudioConfiguration' field in the AudioManager");
        DebugLog("5. Configure audio clips for each cube type in the ScriptableObject");
        
        #if UNITY_EDITOR
        // In editor, we can help by selecting the AudioManager so the field is visible
        UnityEditor.Selection.activeObject = this;
        #endif
    }
    #endregion

    #region Helper Methods
    private bool ValidateForTesting(string testName)
    {
        if (!IsInitialized)
        {
            DebugLog($"{testName}: AudioDebugSystem not initialized!");
            return false;
        }

        if (playbackSystem == null || !playbackSystem.IsInitialized)
        {
            DebugLog($"{testName}: AudioPlaybackSystem not available!");
            return false;
        }

        if (volumeController == null || !volumeController.IsInitialized)
        {
            DebugLog($"{testName}: AudioVolumeController not available!");
            return false;
        }

        if (cubeAudioSystem == null || !cubeAudioSystem.IsInitialized)
        {
            DebugLog($"{testName}: CubeAudioSystem not available!");
            return false;
        }

        return true;
    }

    private AudioClip[] GetTestAudioClips()
    {
        // Try to get clips from cube audio system for testing
        if (cubeAudioConfiguration != null)
        {
            var testClips = new List<AudioClip>();
            foreach (Enumerations.CubeType cubeType in System.Enum.GetValues(typeof(Enumerations.CubeType)))
            {
                var clip = cubeAudioConfiguration.GetRandomClip(cubeType, SoundCategory.Landing);
                if (clip != null)
                {
                    testClips.Add(clip);
                }
            }
            if (testClips.Count > 0)
            {
                return testClips.ToArray();
            }
        }
        return null;
    }

    /// <summary>
    /// Prints comprehensive audio system information to debug log
    /// </summary>
    public void DebugPrintAudioInfo()
    {
        DebugLog("=== AUDIO SYSTEM DEBUG INFO ===");
        DebugLog(GetDebugStatus());
        
        var debugData = GetDebugData();
        foreach (var kvp in debugData)
        {
            DebugLog($"{kvp.Key}: {kvp.Value}");
        }
        
        DebugLog("=== END AUDIO SYSTEM DEBUG INFO ===");
    }
    #endregion

    #region Configuration Updates
    /// <summary>
    /// Updates the cube audio configuration reference
    /// </summary>
    public void UpdateCubeAudioConfiguration(CubeAudioConfiguration config)
    {
        cubeAudioConfiguration = config;
    }

    /// <summary>
    /// Updates testing volume
    /// </summary>
    public void SetTestingVolume(float volume)
    {
        testingVolume = Mathf.Clamp01(volume);
    }
    #endregion

    #region Debug Logging
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[AudioDebugSystem] {message}");
        }
    }
    #endregion

    #region Cleanup
    /// <summary>
    /// Cleans up the debug system
    /// </summary>
    public void Cleanup()
    {
        IsInitialized = false;
        DebugLog("AudioDebugSystem cleaned up");
    }
    #endregion
}

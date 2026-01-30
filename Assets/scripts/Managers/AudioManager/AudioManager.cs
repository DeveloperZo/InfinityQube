using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Enumeration for different audio volume categories
/// </summary>
public enum VolumeCategory
{
    Master,
    SoundEffects,
    CubeImpact,
    CubeDestruction,
    BackgroundAudio,
    SystemAudio,
    WaveComposition
}

/// <summary>
/// Comprehensive audio management system for InfinityQube.
/// Acts as a facade to delegate work to specialized subsystems:
/// - AudioSourcePool: Handles audio source pooling and lifecycle
/// - AudioPlaybackSystem: Core audio playback functionality
/// - AudioVolumeController: Volume management and control
/// - CubeAudioSystem: Cube-specific audio functionality
/// - AudioDebugSystem: Testing and debugging functionality
/// </summary>
public class AudioManager : MonoBehaviour, IManagerDebugInterface
{
    #region Inspector Configuration
    [Header("Audio Configuration")]
    public AudioSource audioSourcePrefab;
    
    [SerializeField]
    [Tooltip("ScriptableObject containing all cube-specific audio configuration")]
    public CubeAudioConfiguration cubeAudioConfiguration;
    public AudioClip[] cubeImpactSounds;
    public AudioClip[] cubeDestructionSounds;
    public AudioClip[] specialEffectSounds;

    [Header("Volume Controls")]
    [Range(0f, 2f)] public float masterVolume = 1f;
    [Range(0f, 2f)] public float soundEffectsVolume = 0.8f;
    [Range(0f, 2f)] public float cubeImpactVolume = 0.7f;
    [Range(0f, 2f)] public float cubeDestructionVolume = 0.6f;
    [Range(0f, 1f)] public float backgroundAudioVolume = 0.3f;
    [Range(0f, 1f)] public float systemAudioVolume = 0.7f;
    [Range(0f, 1f)] public float waveCompositionVolume = 0.6f;

    [Header("Performance Settings")]
    public int audioSourcePoolSize = 10;
    public int maxSimultaneousSounds = 8;
    public bool useAudioSourcePooling = true;
    public float soundCleanupInterval = 5f;

    [Header("Debug Options")]
    public bool enableDebugLogs = false; // Default OFF - audio event noise
    public bool showAudioGizmos = false;
    public bool logAudioEvents = false;
    
    [Header("Testing Tools")]
    [Range(0f, 1f)]
    [Tooltip("Volume slider for real-time audio testing")]
    public float testingVolume = 0.8f;
    
    [Space(5)]
    [Tooltip("Use context menu 'Test Audio System' to test all cube types")]
    public bool showTestingInstructions = true;

    [Header("System Features")]
    public bool enableWaveComposition = true;
    #endregion

    #region Subsystem References
    private AudioSourcePool audioSourcePool;
    private AudioPlaybackSystem audioPlaybackSystem;
    private AudioVolumeController audioVolumeController;
    private CubeAudioSystem cubeAudioSystem;
    private AudioDebugSystem audioDebugSystem;
    #endregion

    #region Properties
    public static AudioManager Instance { get; private set; }
    public bool IsInitialized { get; private set; }
    
    // Delegated properties
    public int ActiveSources => audioSourcePool?.ActiveCount ?? 0;
    public int AvailablePoolSources => audioSourcePool?.AvailableCount ?? 0;
    public float CurrentMasterVolume => audioVolumeController?.MasterVolume ?? masterVolume;
    public float sfxVolume 
    { 
        get => audioVolumeController?.sfxVolume ?? soundEffectsVolume; 
        set { if (audioVolumeController != null) audioVolumeController.sfxVolume = value; }
    }
    
    // IManagerDebugInterface
    public bool EnableDebugLogs 
    { 
        get => enableDebugLogs; 
        set 
        { 
            enableDebugLogs = value;
            if (audioDebugSystem != null) audioDebugSystem.EnableDebugLogs = value;
        }
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
        CreateSubsystems();
    }

    private void Start()
    {
        InitializeSubsystems();
        IsInitialized = true;
        DebugLog("AudioManager initialized as facade");
    }

    private void OnDestroy()
    {
        CleanupSubsystems();
    }
    #endregion

    #region Initialization
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            DebugLog("Multiple AudioManagers found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }

    private void CreateSubsystems()
    {
        // Create subsystem components
        audioSourcePool = gameObject.AddComponent<AudioSourcePool>();
        audioPlaybackSystem = gameObject.AddComponent<AudioPlaybackSystem>();
        audioVolumeController = gameObject.AddComponent<AudioVolumeController>();
        cubeAudioSystem = gameObject.AddComponent<CubeAudioSystem>();
        audioDebugSystem = gameObject.AddComponent<AudioDebugSystem>();
    }

    private void InitializeSubsystems()
    {
        // Validate audio source prefab
        if (audioSourcePrefab == null)
        {
            DebugLog("AudioSource prefab is not assigned! Creating default.");
            audioSourcePrefab = CreateDefaultAudioSource();
        }

        // Initialize volume controller
        var initialVolumes = new Dictionary<VolumeCategory, float>
        {
            [VolumeCategory.Master] = masterVolume,
            [VolumeCategory.SoundEffects] = soundEffectsVolume,
            [VolumeCategory.CubeImpact] = cubeImpactVolume,
            [VolumeCategory.CubeDestruction] = cubeDestructionVolume,
            [VolumeCategory.BackgroundAudio] = backgroundAudioVolume,
            [VolumeCategory.SystemAudio] = systemAudioVolume,
            [VolumeCategory.WaveComposition] = waveCompositionVolume
        };
        audioVolumeController.Initialize(initialVolumes);

        // Initialize source pool
        audioSourcePool.Initialize(audioSourcePrefab, audioSourcePoolSize, maxSimultaneousSounds, 
            soundCleanupInterval, useAudioSourcePooling, transform);

        // Initialize playback system
        audioPlaybackSystem.Initialize(audioSourcePool, enableDebugLogs, logAudioEvents);

        // Initialize cube audio system
        cubeAudioSystem.Initialize(audioPlaybackSystem, audioVolumeController, cubeAudioConfiguration,
            cubeImpactSounds, cubeDestructionSounds, specialEffectSounds, 
            enableDebugLogs, logAudioEvents, enableWaveComposition);

        // Initialize debug system
        audioDebugSystem.Initialize(audioSourcePool, audioPlaybackSystem, audioVolumeController, 
            cubeAudioSystem, cubeAudioConfiguration);
    }

    private AudioSource CreateDefaultAudioSource()
    {
        GameObject defaultAudioSourceObj = new GameObject("DefaultAudioSource");
        AudioSource audioSource = defaultAudioSourceObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        return audioSource;
    }

    private void CleanupSubsystems()
    {
        audioSourcePool?.Cleanup();
        audioPlaybackSystem?.Cleanup();
        cubeAudioSystem?.Cleanup();
        audioDebugSystem?.Cleanup();
    }
    #endregion

    #region Public API - Audio Events
    /// <summary>
    /// Triggers an audio event based on game events
    /// </summary>
    public void TriggerAudioEvent(GameAudioEvent eventType, Vector3 worldPosition, float intensity = 1f)
    {
        if (!IsInitialized) return;

        // For now, just log the event - in the future this could be expanded
        if (logAudioEvents)
        {
            DebugLog($"Audio Event: {eventType} at {worldPosition} with intensity {intensity}");
        }

        // Handle specific events
        switch (eventType)
        {
            case GameAudioEvent.WaveStarted:
            case GameAudioEvent.WaveCompleted:
                // These would play system feedback sounds
                break;
                
            case GameAudioEvent.PlayerMoved:
                // Could play movement sound
                break;
                
            case GameAudioEvent.UnitMarkerPlaced:
            case GameAudioEvent.RecursionMarkerPlaced:
            case GameAudioEvent.MatrixMarkerPlaced:
                // These would play marker placement sounds
                break;
        }
    }

    /// <summary>
    /// Triggers a cube-specific audio event
    /// </summary>
    public void TriggerCubeAudioEvent(GameAudioEvent eventType, CubeType cubeType, Vector3 worldPosition, float volume = 1f)
    {
        if (!IsInitialized) return;

        switch (eventType)
        {
            case GameAudioEvent.CubeLanded:
                PlayCubeLandingSound(cubeType, worldPosition);
                break;
                
            case GameAudioEvent.CubeCaptured:
                PlayCubeCaptureSound(cubeType, worldPosition);
                break;
                
            case GameAudioEvent.CubeEscaped:
                PlayCubeEscapeSound(cubeType, worldPosition);
                break;
        }
    }
    #endregion

    #region Public API - Audio Playback (Delegated)
    public void PlayAudioClip(AudioClip clip, float volume = 1f, Vector3 position = default, float pitch = 1f)
    {
        audioPlaybackSystem?.PlayAudioClip(clip, volume, position, pitch);
    }

    public void PlayAudioClip(AudioClip clip, Vector3 position, float volume)
    {
        audioPlaybackSystem?.PlayAudioClip(clip, position, volume);
    }

    public void PlaySpatialAudioClip(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        audioPlaybackSystem?.PlaySpatialAudioClip(clip, position, volume, pitch);
    }
    #endregion

    #region Public API - Cube Audio (Delegated)
    public void PlayCubeLandingSound(CubeType cubeType, Vector3 position)
    {
        cubeAudioSystem?.PlayCubeLandingSound(cubeType, position);
    }

    public void PlayCubeLandingSoundAtPosition(CubeType cubeType, Vector3 worldPosition, float customVolume = -1f)
    {
        cubeAudioSystem?.PlayCubeLandingSoundAtPosition(cubeType, worldPosition, customVolume);
    }

    public void PlayCubeCaptureSound(CubeType cubeType, Vector3 position = default)
    {
        cubeAudioSystem?.PlayCubeCaptureSound(cubeType, position);
    }

    public void PlayCubeDestructionSound(CubeType cubeType, Vector3 position = default)
    {
        cubeAudioSystem?.PlayCubeDestructionSound(cubeType, position);
    }

    public void PlayCubeDestructionSound(Vector3 position = default)
    {
        cubeAudioSystem?.PlayCubeDestructionSound(position);
    }

    public void PlayCubeSpecialEffectSound(CubeType cubeType, Vector3 position = default)
    {
        cubeAudioSystem?.PlayCubeSpecialEffectSound(cubeType, position);
    }

    public void PlayCubeEscapeSound(CubeType cubeType, Vector3 position = default)
    {
        cubeAudioSystem?.PlayCubeEscapeSound(cubeType, position);
    }

    public void PlayNamedSpecialEffect(string clipName, Vector3 position = default, float volume = -1f)
    {
        cubeAudioSystem?.PlayNamedSpecialEffect(clipName, position, volume);
    }

    public AudioClip GetRandomAudioClip(AudioClip[] clips)
    {
        return cubeAudioSystem?.GetRandomAudioClip(clips);
    }
    #endregion

    #region Public API - Volume Control (Delegated)
    public float GetCurrentVolumeLevel(VolumeCategory category)
    {
        return audioVolumeController?.GetCurrentVolumeLevel(category) ?? 1f;
    }

    public float GetEffectiveVolumeLevel(VolumeCategory category)
    {
        return audioVolumeController?.GetEffectiveVolumeLevel(category) ?? 1f;
    }

    public void SetMasterVolume(float volume)
    {
        audioVolumeController?.SetMasterVolume(volume);
    }

    public void SetVolumeForCategory(VolumeCategory category, float volume)
    {
        audioVolumeController?.SetVolumeForCategory(category, volume);
    }

    public void SetSoundEffectsVolume(float volume)
    {
        audioVolumeController?.SetVolumeForCategory(VolumeCategory.SoundEffects, volume);
    }

    public void SetCubeImpactVolume(float volume)
    {
        audioVolumeController?.SetVolumeForCategory(VolumeCategory.CubeImpact, volume);
    }

    public void SetCubeDestructionVolume(float volume)
    {
        audioVolumeController?.SetVolumeForCategory(VolumeCategory.CubeDestruction, volume);
    }
    #endregion

    #region IManagerDebugInterface Implementation
    public string GetDebugStatus()
    {
        return audioDebugSystem?.GetDebugStatus() ?? "AudioManager: Not Initialized";
    }

    public Dictionary<string, object> GetDebugData()
    {
        return audioDebugSystem?.GetDebugData() ?? new Dictionary<string, object> { ["Status"] = "Not Initialized" };
    }

    public void ResetToDefaults()
    {
        DebugLog("Resetting audio system to default values");
        
        // Reset inspector values to defaults
        masterVolume = 1f;
        soundEffectsVolume = 0.8f;
        cubeImpactVolume = 0.7f;
        cubeDestructionVolume = 0.6f;
        backgroundAudioVolume = 0.3f;
        systemAudioVolume = 0.7f;
        waveCompositionVolume = 0.6f;
        
        // Apply to volume controller
        if (audioVolumeController != null && audioVolumeController.IsInitialized)
        {
            audioVolumeController.SetMasterVolume(masterVolume);
            audioVolumeController.SetVolumeForCategory(VolumeCategory.SoundEffects, soundEffectsVolume);
            audioVolumeController.SetVolumeForCategory(VolumeCategory.CubeImpact, cubeImpactVolume);
            audioVolumeController.SetVolumeForCategory(VolumeCategory.CubeDestruction, cubeDestructionVolume);
            audioVolumeController.SetVolumeForCategory(VolumeCategory.BackgroundAudio, backgroundAudioVolume);
            audioVolumeController.SetVolumeForCategory(VolumeCategory.SystemAudio, systemAudioVolume);
            audioVolumeController.SetVolumeForCategory(VolumeCategory.WaveComposition, waveCompositionVolume);
        }
        
        if (EnableDebugLogs)
        {
            Debug.Log("[AudioManager] Reset to defaults completed");
        }
    }

    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading for audio settings
        if (EnableDebugLogs)
        {
            Debug.Log($"[AudioManager] Loading configuration: {configName} (not yet implemented)");
        }
    }

    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving for audio settings
        if (EnableDebugLogs)
        {
            Debug.Log($"[AudioManager] Saving configuration: {configName} (not yet implemented)");
        }
    }

    [ContextMenu("Test Audio System")]
    public void TestAudioSystem()
    {
        audioDebugSystem?.TestAudioSystem();
    }

    [ContextMenu("Validate Audio Configuration")]
    public void ValidateAudioConfiguration()
    {
        audioDebugSystem?.ValidateAudioFolderStructure();
    }
    #endregion

    #region Debug
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[AudioManager] {message}");
        }
    }
    #endregion
}

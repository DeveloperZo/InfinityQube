using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
/// Data structure for tracking active wave composition layers
/// </summary>
[System.Serializable]
public class WaveCompositionLayer
{
    public AudioSource audioSource;
    public float startTime;
    public float duration;
    public bool isActive;
    public Enumerations.CubeType cubeType;
    public Vector3 position;
    
    public WaveCompositionLayer(AudioSource source, float start, float dur, Enumerations.CubeType type, Vector3 pos)
    {
        audioSource = source;
        startTime = start;
        duration = dur;
        isActive = true;
        cubeType = type;
        position = pos;
    }
    
    public bool IsFinished => Time.time >= startTime + duration;
}

/// <summary>
/// Comprehensive audio management system for InfinityQube that handles all game audio including:
/// - Cube-specific audio (landing, capture, destruction, special effects)
/// - Background music system with playlist management and fade transitions
/// - System feedback audio (wave events, marker placement/triggers, UI interactions)
/// - Wave composition system for dynamic layered audio based on cube movements
/// - Enhanced volume control hierarchy supporting all audio categories
/// - 3D spatial audio positioning and audio source pooling
/// - Comprehensive debug interface and testing capabilities
/// 
/// This manager consolidates functionality from AudioController and provides a unified
/// audio system with enhanced performance, debugging, and configuration capabilities.
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

    [Header("Performance Settings")]
    public int audioSourcePoolSize = 10;
    public int maxSimultaneousSounds = 8;
    public bool useAudioSourcePooling = true;
    public float soundCleanupInterval = 5f;

    [Header("Debug Options")]
    public bool showAudioGizmos = false;
    public bool logAudioEvents = false;
    
    [Header("Background Music")]
    [SerializeField] public AudioClip[] backgroundAmbientTracks;
    [SerializeField] private bool enableBackgroundMusic = true;
    [SerializeField] private bool shufflePlaylist = true;
    [SerializeField] private float trackTransitionTime = 2f;

    [Header("System Feedback Sounds")]
    [SerializeField] public AudioClip waveStartSound;
    [SerializeField] public AudioClip waveCompleteSound;
    [SerializeField] public AudioClip uiClickSound;
    [SerializeField] public AudioClip lightMarkerPlaceSound;
    [SerializeField] public AudioClip heavyMarkerPlaceSound;
    [SerializeField] public AudioClip primeMarkerPlaceSound;
    [SerializeField] public AudioClip lightMarkerTriggerSound;
    [SerializeField] public AudioClip heavyMarkerTriggerSound;
    [SerializeField] public AudioClip primeMarkerTriggerSound;

    [Header("Wave Composition System")]
    [SerializeField] public bool enableWaveComposition = true;
    [SerializeField] [Range(0.1f, 2f)] public float compositionLayerDelay = 0.3f;
    [SerializeField] [Range(2, 10)] public int maxCompositionLayers = 5;
    [SerializeField] public AnimationCurve volumeFalloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);
    [SerializeField] [Range(0f, 1f)] public float waveCompositionVolume = 0.6f;

    [Header("Testing Tools")]
    [Range(0f, 1f)]
    [Tooltip("Volume slider for real-time audio testing")]
    public float testingVolume = 0.8f;
    
    [Space(5)]
    [Tooltip("Use context menu 'Test Audio System' to test all cube types")]
    public bool showTestingInstructions = true;
    #endregion

    #region Runtime State
    private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
    private List<AudioSource> activeAudioSources = new List<AudioSource>();
    private Dictionary<AudioClip, float> lastPlayedTimes = new Dictionary<AudioClip, float>();
    private float lastCleanupTime = 0f;
    
    // Background music state
    private AudioSource backgroundAudioSource;
    private int currentTrackIndex = 0;
    private List<int> shuffledTrackIndices = new List<int>();
    private Coroutine backgroundPlaybackCoroutine;
    private bool isTransitioning = false;
    
    // Performance tracking
    private int totalSoundsPlayed = 0;
    private int pooledSourcesUsed = 0;
    private int instantiatedSources = 0;
    
    // Wave composition state
    private List<WaveCompositionLayer> activeCompositionLayers = new List<WaveCompositionLayer>();
    private Dictionary<Enumerations.CubeType, AudioClip> lastPlayedCubeSounds = new Dictionary<Enumerations.CubeType, AudioClip>();
    private float lastWaveStepTime = 0f;
    private Coroutine waveCompositionCoroutine;
    #endregion

    #region Properties
    public static AudioManager Instance { get; private set; }
    public bool IsInitialized { get; private set; }
    public int ActiveSources => activeAudioSources.Count;
    public int AvailablePoolSources => audioSourcePool.Count;
    public float CurrentMasterVolume => masterVolume;
    public bool IsPlayingBackground => backgroundAudioSource != null && backgroundAudioSource.isPlaying;
    public float CurrentBackgroundVolume => backgroundAudioVolume;
    public float sfxVolume 
    { 
        get => soundEffectsVolume; 
        set => soundEffectsVolume = Mathf.Clamp01(value); 
    }
    
    /// <summary>
    /// Get current volume level for a specific category
    /// </summary>
    /// <param name="category">Volume category to query</param>
    /// <returns>Current volume level for the category</returns>
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
    /// Get the effective volume for a category (category volume * master volume)
    /// </summary>
    /// <param name="category">Volume category to calculate</param>
    /// <returns>Effective volume level including master volume</returns>
    public float GetEffectiveVolumeLevel(VolumeCategory category)
    {
        return GetCurrentVolumeLevel(category) * masterVolume;
    }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
        ValidateConfiguration();
    }

    private void Start()
    {
        SetupAudioSystem();
        EnableDebugLogs = true;
    }

    private void Update()
    {
        PerformCleanup();
        UpdateBackgroundMusic();
        UpdateWaveComposition();
    }

    private void OnDestroy()
    {
        CleanupAudioSystem();
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
            return;
        }
    }

    private void ValidateConfiguration()
    {
        if (audioSourcePrefab == null)
        {
            DebugLog("AudioSource prefab is not assigned! Creating default AudioSource component.");
            audioSourcePrefab = CreateDefaultAudioSource();
        }
        
        // Validate cube audio configuration
        if (cubeAudioConfiguration != null)
        {
            bool configValid = cubeAudioConfiguration.ValidateConfiguration();
            if (!configValid)
            {
                DebugLog("CubeAudioConfiguration validation failed. Some cube types may not have audio assigned.");
            }
        }
        else
        {
            DebugLog("CubeAudioConfiguration is not assigned. Cube-specific audio will not be available.");
        }

        // Ensure reasonable values
        audioSourcePoolSize = Mathf.Max(5, audioSourcePoolSize);
        maxSimultaneousSounds = Mathf.Max(1, maxSimultaneousSounds);
        soundCleanupInterval = Mathf.Max(1f, soundCleanupInterval);
        
        // Clamp all volume values to ensure they stay within valid ranges
        masterVolume = Mathf.Clamp01(masterVolume);
        soundEffectsVolume = Mathf.Clamp01(soundEffectsVolume);
        cubeImpactVolume = Mathf.Clamp01(cubeImpactVolume);
        cubeDestructionVolume = Mathf.Clamp01(cubeDestructionVolume);
        backgroundAudioVolume = Mathf.Clamp01(backgroundAudioVolume);
        systemAudioVolume = Mathf.Clamp01(systemAudioVolume);
        waveCompositionVolume = Mathf.Clamp01(waveCompositionVolume);
        
        if (EnableDebugLogs)
        {
            DebugLog($"Volume validation complete - Master: {masterVolume:F2}, SFX: {soundEffectsVolume:F2}, CubeImpact: {cubeImpactVolume:F2}, CubeDestruction: {cubeDestructionVolume:F2}, Background: {backgroundAudioVolume:F2}, System: {systemAudioVolume:F2}, WaveComposition: {waveCompositionVolume:F2}");
        }
    }

    private AudioSource CreateDefaultAudioSource()
    {
        GameObject defaultAudioSourceObj = new GameObject("DefaultAudioSource");
        AudioSource audioSource = defaultAudioSourceObj.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound by default
        return audioSource;
    }

    private void SetupAudioSystem()
    {
        DebugLog($"Setting up audio system with pool size: {audioSourcePoolSize}");
        
        if (useAudioSourcePooling)
        {
            InitializeAudioSourcePool();
        }
        
        SetupBackgroundAudio();
        InitializeWaveComposition();
        
        IsInitialized = true;
        DebugLog("Audio system initialization complete");
    }

    private void InitializeAudioSourcePool()
    {
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            AudioSource pooledAudioSource = CreatePooledAudioSource();
            audioSourcePool.Enqueue(pooledAudioSource);
        }
        
        DebugLog($"Audio source pool initialized with {audioSourcePoolSize} sources");
    }

    /// <summary>
    /// Enhanced method to get an available audio source with proper 3D spatial configuration
    /// </summary>
    public AudioSource GetAvailableAudioSource()
    {
        AudioSource audioSource = GetAudioSource();
        if (audioSource != null)
        {
            Configure3DAudioSource(audioSource);
        }
        return audioSource;
    }

    /// <summary>
    /// Returns an audio source to the pool after playback completion
    /// </summary>
    public void ReturnAudioSource(AudioSource audioSource)
    {
        if (audioSource == null) return;
        
        ReturnAudioSourceToPool(audioSource);
        activeAudioSources.Remove(audioSource);
    }

    /// <summary>
    /// Configures audio source for 3D spatial sound with optimized settings
    /// </summary>
    private void Configure3DAudioSource(AudioSource audioSource)
    {
        audioSource.spatialBlend = 1.0f; // Full 3D spatial sound
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.maxDistance = 50f;
        audioSource.minDistance = 1f;
        audioSource.spread = 0f; // Directional sound
        audioSource.dopplerLevel = 0.1f; // Minimal doppler effect
        audioSource.volume = soundEffectsVolume * masterVolume;
        audioSource.pitch = 1f;
        audioSource.playOnAwake = false;
    }

    private AudioSource CreatePooledAudioSource()
    {
        GameObject audioSourceObj = Instantiate(audioSourcePrefab.gameObject);
        audioSourceObj.name = $"PooledAudioSource_{audioSourcePool.Count}";
        audioSourceObj.transform.SetParent(transform);
        audioSourceObj.SetActive(false);
        
        AudioSource audioSource = audioSourceObj.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = audioSourceObj.AddComponent<AudioSource>();
        }
        
        ConfigureAudioSource(audioSource);
        return audioSource;
    }

    private void ConfigureAudioSource(AudioSource audioSource)
    {
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound for UI/game events (will be overridden for 3D sounds)
        audioSource.volume = soundEffectsVolume * masterVolume;
        audioSource.pitch = 1f;
        
        // Set reasonable defaults for 3D audio (will be configured properly when used for spatial audio)
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.maxDistance = 50f;
        audioSource.minDistance = 1f;
    }
    #endregion

    #region Wave Composition System
    
    /// <summary>
    /// Initializes the wave composition system
    /// </summary>
    private void InitializeWaveComposition()
    {
        if (!enableWaveComposition) return;
        
        activeCompositionLayers.Clear();
        lastPlayedCubeSounds.Clear();
        lastWaveStepTime = 0f;
        
        // Ensure volume falloff curve has reasonable values
        if (volumeFalloffCurve == null || volumeFalloffCurve.length < 2)
        {
            volumeFalloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);
        }
        
        DebugLog("Wave composition system initialized");
    }
    
    /// <summary>
    /// Updates the wave composition system each frame
    /// </summary>
    private void UpdateWaveComposition()
    {
        if (!enableWaveComposition) return;
        
        UpdateActiveCompositionLayers();
    }
    
    /// <summary>
    /// Called when WaveManager detects a wave step
    /// </summary>
    /// <param name="stepNumber">The current wave step number</param>
    public void OnWaveStepDetected(int stepNumber)
    {
        if (!enableWaveComposition) return;
        
        lastWaveStepTime = Time.time;
        CreateWaveComposition(stepNumber);
        
        if (EnableDebugLogs)
        {
            DebugLog($"Wave step {stepNumber} detected - creating composition with {lastPlayedCubeSounds.Count} cube types");
        }
    }
    
    /// <summary>
    /// Creates a wave composition based on recently played cube sounds
    /// </summary>
    /// <param name="stepNumber">The current wave step number</param>
    private void CreateWaveComposition(int stepNumber)
    {
        if (lastPlayedCubeSounds.Count == 0) return;
        
        List<AudioClip> clipsToLayer = new List<AudioClip>();
        List<Vector3> positions = new List<Vector3>();
        List<float> volumes = new List<float>();
        
        int layerIndex = 0;
        foreach (var kvp in lastPlayedCubeSounds)
        {
            if (layerIndex >= maxCompositionLayers) break;
            
            if (kvp.Value != null)
            {
                clipsToLayer.Add(kvp.Value);
                positions.Add(Vector3.zero); // Will be set in the coroutine based on cube positions
                volumes.Add(CalculateCompositionLayerVolume(layerIndex));
                layerIndex++;
            }
        }
        
        if (clipsToLayer.Count > 0)
        {
            if (waveCompositionCoroutine != null)
            {
                StopCoroutine(waveCompositionCoroutine);
            }
            waveCompositionCoroutine = StartCoroutine(PlayWaveComposition(clipsToLayer, positions, volumes));
        }
    }
    
    /// <summary>
    /// Gets a cube audio clip for wave composition
    /// </summary>
    /// <param name="cubeType">The cube type to get audio for</param>
    /// <returns>Audio clip for the cube type, or null if none available</returns>
    private AudioClip GetCubeAudioForComposition(Enumerations.CubeType cubeType)
    {
        AudioClip clip = null;
        
        // Try to get clip from cube audio configuration
        if (cubeAudioConfiguration != null)
        {
            clip = cubeAudioConfiguration.GetRandomClip(cubeType, SoundCategory.Landing);
        }
        
        // Fallback to legacy cube impact sounds
        if (clip == null && cubeImpactSounds != null && cubeImpactSounds.Length > 0)
        {
            clip = GetRandomAudioClip(cubeImpactSounds);
        }
        
        return clip;
    }
    
    /// <summary>
    /// Calculates the volume for a composition layer based on its index
    /// </summary>
    /// <param name="layerIndex">The index of the layer (0 = first/loudest)</param>
    /// <returns>Volume multiplier for the layer</returns>
    private float CalculateCompositionLayerVolume(int layerIndex)
    {
        if (layerIndex >= maxCompositionLayers) return 0f;
        
        float normalizedIndex = (float)layerIndex / (maxCompositionLayers - 1);
        float falloffVolume = volumeFalloffCurve.Evaluate(normalizedIndex);
        
        return falloffVolume * waveCompositionVolume;
    }
    
    /// <summary>
    /// Updates and cleans up active composition layers
    /// </summary>
    private void UpdateActiveCompositionLayers()
    {
        for (int i = activeCompositionLayers.Count - 1; i >= 0; i--)
        {
            var layer = activeCompositionLayers[i];
            
            if (layer.IsFinished || layer.audioSource == null || !layer.audioSource.isPlaying)
            {
                if (layer.audioSource != null)
                {
                    ReturnAudioSource(layer.audioSource);
                }
                activeCompositionLayers.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Cleans up the wave composition system
    /// </summary>
    private void CleanupWaveComposition()
    {
        if (waveCompositionCoroutine != null)
        {
            StopCoroutine(waveCompositionCoroutine);
            waveCompositionCoroutine = null;
        }
        
        // Stop and return all active composition layers
        foreach (var layer in activeCompositionLayers)
        {
            if (layer.audioSource != null)
            {
                layer.audioSource.Stop();
                ReturnAudioSource(layer.audioSource);
            }
        }
        
        activeCompositionLayers.Clear();
        lastPlayedCubeSounds.Clear();
    }
    
    /// <summary>
    /// Sets the volume for wave composition
    /// </summary>
    /// <param name="volume">New volume level (0-1)</param>
    public void SetWaveCompositionVolume(float volume)
    {
        waveCompositionVolume = Mathf.Clamp01(volume);
        
        // Update active layers
        for (int i = 0; i < activeCompositionLayers.Count; i++)
        {
            var layer = activeCompositionLayers[i];
            if (layer.audioSource != null)
            {
                float layerVolume = CalculateCompositionLayerVolume(i);
                layer.audioSource.volume = layerVolume * masterVolume;
            }
        }
        
        DebugLog($"Wave composition volume set to: {waveCompositionVolume:F2}");
    }
    
    /// <summary>
    /// Coroutine to play layered wave composition
    /// </summary>
    /// <param name="clips">List of audio clips to layer</param>
    /// <param name="positions">List of positions for spatial audio</param>
    /// <param name="volumes">List of volume levels for each layer</param>
    private IEnumerator PlayWaveComposition(List<AudioClip> clips, List<Vector3> positions, List<float> volumes)
    {
        for (int i = 0; i < clips.Count && i < maxCompositionLayers; i++)
        {
            if (clips[i] == null) continue;
            
            AudioSource audioSource = GetAvailableAudioSource();
            if (audioSource == null)
            {
                DebugLog("Failed to get audio source for wave composition layer");
                continue;
            }
            
            // Configure the audio source for composition
            audioSource.clip = clips[i];
            audioSource.volume = volumes[i] * masterVolume;
            audioSource.pitch = 1f + (i * 0.05f); // Slight pitch variation for layers
            
            // Set position for spatial audio if available
            if (i < positions.Count && positions[i] != Vector3.zero)
            {
                audioSource.transform.position = positions[i];
                Configure3DAudioSource(audioSource);
            }
            else
            {
                // Default to 2D audio for composition
                audioSource.spatialBlend = 0f;
            }
            
            audioSource.Play();
            
            // Create and track the composition layer
            var layer = new WaveCompositionLayer(
                audioSource,
                Time.time,
                clips[i].length,
                lastPlayedCubeSounds.Keys.ToArray()[Mathf.Min(i, lastPlayedCubeSounds.Count - 1)],
                positions.Count > i ? positions[i] : Vector3.zero
            );
            
            activeCompositionLayers.Add(layer);
            
            if (EnableDebugLogs)
            {
                DebugLog($"Wave composition layer {i + 1} started: {clips[i].name} at volume {volumes[i]:F2}");
            }
            
            // Wait before starting the next layer
            if (i < clips.Count - 1)
            {
                yield return new WaitForSeconds(compositionLayerDelay);
            }
        }
    }
    
    #endregion

    #region Background Music System
    private void SetupBackgroundAudio()
    {
        if (backgroundAmbientTracks == null || backgroundAmbientTracks.Length == 0)
        {
            DebugLog("No background ambient tracks assigned");
            return;
        }
        
        // Create dedicated audio source for background music
        GameObject backgroundObj = new GameObject("BackgroundAudioSource");
        backgroundObj.transform.SetParent(transform);
        backgroundAudioSource = backgroundObj.AddComponent<AudioSource>();
        backgroundAudioSource.playOnAwake = false;
        backgroundAudioSource.loop = false; // We handle looping manually for transitions
        backgroundAudioSource.spatialBlend = 0f; // 2D audio
        backgroundAudioSource.volume = backgroundAudioVolume;
        
        // Initialize playlist
        InitializePlaylist();
        
        if (enableBackgroundMusic)
        {
            StartBackgroundMusic();
        }
        
        DebugLog($"Background audio system initialized with {backgroundAmbientTracks.Length} tracks");
    }

    private void InitializePlaylist()
    {
        shuffledTrackIndices.Clear();
        for (int i = 0; i < backgroundAmbientTracks.Length; i++)
        {
            shuffledTrackIndices.Add(i);
        }
        
        if (shufflePlaylist)
        {
            ShufflePlaylist();
        }
        
        currentTrackIndex = 0;
    }

    private void ShufflePlaylist()
    {
        for (int i = 0; i < shuffledTrackIndices.Count; i++)
        {
            int temp = shuffledTrackIndices[i];
            int randomIndex = Random.Range(i, shuffledTrackIndices.Count);
            shuffledTrackIndices[i] = shuffledTrackIndices[randomIndex];
            shuffledTrackIndices[randomIndex] = temp;
        }
        
        DebugLog("Background playlist shuffled");
    }

    public void StartBackgroundMusic()
    {
        if (backgroundAmbientTracks == null || backgroundAmbientTracks.Length == 0 || !enableBackgroundMusic)
        {
            DebugLog("Cannot start background music: no tracks available or disabled");
            return;
        }
        
        if (backgroundPlaybackCoroutine != null)
        {
            StopCoroutine(backgroundPlaybackCoroutine);
        }
        
        backgroundPlaybackCoroutine = StartCoroutine(BackgroundMusicCoroutine());
        DebugLog("Background music started");
    }

    public void StopBackgroundMusic()
    {
        if (backgroundPlaybackCoroutine != null)
        {
            StopCoroutine(backgroundPlaybackCoroutine);
            backgroundPlaybackCoroutine = null;
        }
        
        if (backgroundAudioSource != null && backgroundAudioSource.isPlaying)
        {
            backgroundAudioSource.Stop();
        }
        
        DebugLog("Background music stopped");
    }

    public void NextTrack()
    {
        if (!enableBackgroundMusic || backgroundAmbientTracks.Length <= 1) return;
        
        currentTrackIndex = (currentTrackIndex + 1) % shuffledTrackIndices.Count;
        
        if (currentTrackIndex == 0 && shufflePlaylist)
        {
            ShufflePlaylist(); // Re-shuffle when we complete a cycle
        }
        
        StartCoroutine(TransitionToTrack(GetCurrentTrack()));
        DebugLog($"Transitioning to next track: {GetCurrentTrack()?.name ?? "null"}");
    }

    public void PreviousTrack()
    {
        if (!enableBackgroundMusic || backgroundAmbientTracks.Length <= 1) return;
        
        currentTrackIndex = (currentTrackIndex - 1 + shuffledTrackIndices.Count) % shuffledTrackIndices.Count;
        StartCoroutine(TransitionToTrack(GetCurrentTrack()));
        DebugLog($"Transitioning to previous track: {GetCurrentTrack()?.name ?? "null"}");
    }

    public void SetBackgroundVolume(float volume)
    {
        backgroundAudioVolume = Mathf.Clamp01(volume);
        if (backgroundAudioSource != null)
        {
            backgroundAudioSource.volume = backgroundAudioVolume;
        }
        DebugLog($"Background volume set to: {backgroundAudioVolume:F2}");
    }

    private IEnumerator BackgroundMusicCoroutine()
    {
        while (enableBackgroundMusic && backgroundAmbientTracks.Length > 0)
        {
            AudioClip currentClip = GetCurrentTrack();
            if (currentClip != null && backgroundAudioSource != null)
            {
                yield return StartCoroutine(PlayTrackWithFade(currentClip));
                
                // Move to next track
                currentTrackIndex = (currentTrackIndex + 1) % shuffledTrackIndices.Count;
                
                // Re-shuffle playlist if we've completed a cycle
                if (currentTrackIndex == 0 && shufflePlaylist)
                {
                    ShufflePlaylist();
                }
            }
            else
            {
                yield return new WaitForSeconds(1f); // Wait before retrying if no valid track
            }
        }
    }

    private IEnumerator PlayTrackWithFade(AudioClip track)
    {
        if (track == null || backgroundAudioSource == null) yield break;
        
        // Fade in
        backgroundAudioSource.clip = track;
        backgroundAudioSource.volume = 0f;
        backgroundAudioSource.Play();
        
        float fadeTime = Mathf.Min(trackTransitionTime, track.length * 0.1f); // Max 10% of track length
        yield return StartCoroutine(FadeVolume(backgroundAudioSource, 0f, backgroundAudioVolume, fadeTime));
        
        // Play full track (minus fade times)
        float playTime = track.length - (fadeTime * 2f);
        if (playTime > 0f)
        {
            yield return new WaitForSeconds(playTime);
        }
        
        // Fade out
        yield return StartCoroutine(FadeVolume(backgroundAudioSource, backgroundAudioVolume, 0f, fadeTime));
        backgroundAudioSource.Stop();
    }

    private IEnumerator TransitionToTrack(AudioClip newTrack)
    {
        if (newTrack == null || isTransitioning) yield break;
        
        isTransitioning = true;
        
        // Fade out current track
        if (backgroundAudioSource != null && backgroundAudioSource.isPlaying)
        {
            yield return StartCoroutine(FadeVolume(backgroundAudioSource, backgroundAudioSource.volume, 0f, trackTransitionTime * 0.5f));
            backgroundAudioSource.Stop();
        }
        
        // Start new track
        if (backgroundAudioSource != null)
        {
            backgroundAudioSource.clip = newTrack;
            backgroundAudioSource.volume = 0f;
            backgroundAudioSource.Play();
            yield return StartCoroutine(FadeVolume(backgroundAudioSource, 0f, backgroundAudioVolume, trackTransitionTime * 0.5f));
        }
        
        isTransitioning = false;
    }

    private IEnumerator FadeVolume(AudioSource audioSource, float startVolume, float targetVolume, float fadeTime)
    {
        if (audioSource == null || fadeTime <= 0f)
        {
            if (audioSource != null) audioSource.volume = targetVolume;
            yield break;
        }
        
        float elapsedTime = 0f;
        audioSource.volume = startVolume;
        
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeTime;
            audioSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }
        
        audioSource.volume = targetVolume;
    }

    private AudioClip GetCurrentTrack()
    {
        if (backgroundAmbientTracks == null || backgroundAmbientTracks.Length == 0 || 
            currentTrackIndex >= shuffledTrackIndices.Count) return null;
        
        int trackIndex = shuffledTrackIndices[currentTrackIndex];
        return (trackIndex >= 0 && trackIndex < backgroundAmbientTracks.Length) ? backgroundAmbientTracks[trackIndex] : null;
    }

    private void UpdateBackgroundMusic()
    {
        if (backgroundAudioSource != null)
        {
            backgroundAudioSource.volume = backgroundAudioVolume; // Ensure volume stays synced
        }
    }

    [ContextMenu("Test Background Music")]
    public void TestBackgroundMusic()
    {
        if (backgroundAmbientTracks == null || backgroundAmbientTracks.Length == 0)
        {
            DebugLog("No background tracks available for testing");
            return;
        }
        
        if (IsPlayingBackground)
        {
            StopBackgroundMusic();
            DebugLog("Background music stopped");
        }
        else
        {
            StartBackgroundMusic();
            DebugLog("Background music started");
        }
    }
    
    [ContextMenu("Test Wave Composition")]
    public void TestWaveCompositionSystem()
    {
        if (!enableWaveComposition)
        {
            DebugLog("Wave composition is disabled");
            return;
        }
        
        DebugLog("Testing wave composition system...");
        
        // Simulate some cube landing sounds to populate the tracking dictionary
        var cubeTypes = new[] { Enumerations.CubeType.Unit, Enumerations.CubeType.Prime, Enumerations.CubeType.Recursion };
        Vector3 testPosition = transform.position;
        
        foreach (var cubeType in cubeTypes)
        {
            PlayCubeLandingSound(cubeType, testPosition + Vector3.right * UnityEngine.Random.Range(-2f, 2f));
            testPosition += Vector3.forward;
        }
        
        // Wait a bit then trigger wave composition
        StartCoroutine(DelayedWaveCompositionTest());
    }
    
    [ContextMenu("Test System Feedback")]
    public void TestSystemFeedback()
    {
        DebugLog("Testing system feedback audio...");
        
        PlayWaveStartSound();
        
        // Test marker placement sounds with delays
        StartCoroutine(TestSystemAudioSequence());
    }
    #endregion

    #region System Feedback Audio
    public void PlayWaveStartSound()
    {
        if (waveStartSound != null)
        {
            PlaySystemFeedbackSound(waveStartSound, systemAudioVolume);
            DebugLog("Wave start sound played");
        }
        else if (logAudioEvents)
        {
            DebugLog("Wave start sound requested but no clip assigned");
        }
    }

    public void PlayWaveCompleteSound()
    {
        if (waveCompleteSound != null)
        {
            PlaySystemFeedbackSound(waveCompleteSound, systemAudioVolume);
            DebugLog("Wave complete sound played");
        }
        else if (logAudioEvents)
        {
            DebugLog("Wave complete sound requested but no clip assigned");
        }
    }

    public void PlayMarkerPlaceSound(Enumerations.MarkerType markerType, Vector3 position = default)
    {
        AudioClip clipToPlay = null;
        string markerTypeName = "";
        
        switch (markerType)
        {
            case Enumerations.MarkerType.Light:
                clipToPlay = lightMarkerPlaceSound;
                markerTypeName = "Light";
                break;
            case Enumerations.MarkerType.Heavy:
                clipToPlay = heavyMarkerPlaceSound;
                markerTypeName = "Heavy";
                break;
            case Enumerations.MarkerType.Prime:
                clipToPlay = primeMarkerPlaceSound;
                markerTypeName = "Prime";
                break;
        }
        
        if (clipToPlay != null)
        {
            PlaySystemFeedbackSound(clipToPlay, systemAudioVolume, position);
            DebugLog($"{markerTypeName} marker place sound played at position {position}");
        }
        else if (logAudioEvents)
        {
            DebugLog($"{markerTypeName} marker place sound requested but no clip assigned");
        }
    }
    
    public void PlayMarkerTriggerSound(Enumerations.MarkerType markerType, Vector3 position = default)
    {
        AudioClip clipToPlay = null;
        string markerTypeName = "";
        
        switch (markerType)
        {
            case Enumerations.MarkerType.Light:
                clipToPlay = lightMarkerTriggerSound;
                markerTypeName = "Light";
                break;
            case Enumerations.MarkerType.Heavy:
                clipToPlay = heavyMarkerTriggerSound;
                markerTypeName = "Heavy";
                break;
            case Enumerations.MarkerType.Prime:
                clipToPlay = primeMarkerTriggerSound;
                markerTypeName = "Prime";
                break;
        }
        
        if (clipToPlay != null)
        {
            PlaySystemFeedbackSound(clipToPlay, systemAudioVolume * 1.2f, position); // Slightly louder for triggers
            DebugLog($"{markerTypeName} marker trigger sound played at position {position}");
        }
        else if (logAudioEvents)
        {
            DebugLog($"{markerTypeName} marker trigger sound requested but no clip assigned");
        }
    }

    public void PlayUIClickSound()
    {
        if (uiClickSound != null)
        {
            PlaySystemFeedbackSound(uiClickSound, systemAudioVolume * 0.8f); // Slightly quieter for UI
            DebugLog("UI click sound played");
        }
        else if (logAudioEvents)
        {
            DebugLog("UI click sound requested but no clip assigned");
        }
    }

    private void PlaySystemFeedbackSound(AudioClip clip, float volume, Vector3 position = default)
    {
        if (clip == null) return;
        
        // Use existing PlayAudioClip method with system audio volume
        float finalVolume = volume * masterVolume;
        PlayAudioClip(clip, finalVolume, position, 1f);
        
        if (EnableDebugLogs && logAudioEvents)
        {
            DebugLog($"System feedback sound played: {clip.name} at volume {finalVolume:F2} at position {position}");
        }
    }

    public void SetSystemAudioVolume(float volume)
    {
        systemAudioVolume = Mathf.Clamp01(volume);
        DebugLog($"System audio volume set to: {systemAudioVolume:F2}");
    }

    /// <summary>
    /// Integration point for WaveManager wave start events
    /// </summary>
    public void OnWaveStarted()
    {
        PlayWaveStartSound();
        DebugLog("Wave started event processed");
    }

    /// <summary>
    /// Integration point for WaveManager wave complete events
    /// </summary>
    public void OnWaveCompleted()
    {
        PlayWaveCompleteSound();
        DebugLog("Wave completed event processed");
    }

    /// <summary>
    /// Integration point for marker placement events
    /// </summary>
    public void OnMarkerPlaced(Enumerations.MarkerType markerType, Vector3 worldPosition)
    {
        PlayMarkerPlaceSound(markerType, worldPosition);
        DebugLog($"{markerType} marker placed event processed at position {worldPosition}");
    }

    /// <summary>
    /// Integration point for UI interaction events
    /// </summary>
    public void OnUIInteraction()
    {
        PlayUIClickSound();
    }



    private IEnumerator TestSystemAudioSequence()
    {
        yield return new WaitForSeconds(0.5f);
        PlayMarkerPlaceSound(Enumerations.MarkerType.Light, transform.position);
        
        yield return new WaitForSeconds(0.5f);
        PlayMarkerPlaceSound(Enumerations.MarkerType.Heavy, transform.position + Vector3.right);
        
        yield return new WaitForSeconds(0.5f);
        PlayMarkerPlaceSound(Enumerations.MarkerType.Prime, transform.position + Vector3.left);
        
        yield return new WaitForSeconds(0.5f);
        PlayMarkerTriggerSound(Enumerations.MarkerType.Light, transform.position);
        
        yield return new WaitForSeconds(0.5f);
        PlayMarkerTriggerSound(Enumerations.MarkerType.Heavy, transform.position + Vector3.right);
        
        yield return new WaitForSeconds(0.5f);
        PlayMarkerTriggerSound(Enumerations.MarkerType.Prime, transform.position + Vector3.left);
        
        yield return new WaitForSeconds(0.5f);
        PlayUIClickSound();
        
        yield return new WaitForSeconds(1f);
        PlayWaveCompleteSound();
        
        DebugLog("System feedback audio test complete");
    }
    

    
    private IEnumerator DelayedWaveCompositionTest()
    {
        yield return new WaitForSeconds(0.5f);
        
        // Trigger wave step to create composition
        OnWaveStepDetected(1);
        
        DebugLog("Wave composition test triggered");
    }
    
    [ContextMenu("Test Volume Controls")]
    public void TestVolumeControls()
    {
        DebugLog("=== VOLUME CONTROL SYSTEM TEST STARTED ===");
        
        // Test getting current volume levels for all categories
        foreach (VolumeCategory category in System.Enum.GetValues(typeof(VolumeCategory)))
        {
            float currentVolume = GetCurrentVolumeLevel(category);
            float effectiveVolume = GetEffectiveVolumeLevel(category);
            DebugLog($"{category}: Current={currentVolume:F2}, Effective={effectiveVolume:F2}");
        }
        
        // Test setting volumes
        DebugLog("Testing SetVolumeForCategory method...");
        
        float originalMaster = masterVolume;
        SetVolumeForCategory(VolumeCategory.Master, 0.5f);
        
        SetVolumeForCategory(VolumeCategory.SoundEffects, 0.7f);
        SetVolumeForCategory(VolumeCategory.BackgroundAudio, 0.3f);
        SetVolumeForCategory(VolumeCategory.SystemAudio, 0.8f);
        
        // Test a sound with the new volumes
        if (cubeImpactSounds != null && cubeImpactSounds.Length > 0)
        {
            PlayAudioClip(cubeImpactSounds[0], soundEffectsVolume, transform.position);
        }
        
        // Restore original master volume
        SetMasterVolume(originalMaster);
        
        DebugLog("=== VOLUME CONTROL SYSTEM TEST COMPLETED ===");
    }
    #endregion

    #region Testing and Validation Tools
    
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
        
        // Check legacy audio arrays
        if (cubeImpactSounds == null || cubeImpactSounds.Length == 0)
        {
            warnings.Add("No legacy cube impact sounds assigned");
        }
        else
        {
            DebugLog($"✓ Legacy cube impact sounds: {cubeImpactSounds.Length} clips");
        }
        
        if (cubeDestructionSounds == null || cubeDestructionSounds.Length == 0)
        {
            warnings.Add("No legacy cube destruction sounds assigned");
        }
        else
        {
            DebugLog($"✓ Legacy cube destruction sounds: {cubeDestructionSounds.Length} clips");
        }
        
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
        
        // Check AudioSource prefab
        if (audioSourcePrefab == null)
        {
            warnings.Add("AudioSource prefab not assigned - using default AudioSource component");
        }
        else
        {
            DebugLog("✓ AudioSource prefab assigned");
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
    /// Tests the entire audio system by playing sounds for all cube types
    /// </summary>
    [ContextMenu("Test Audio System")]
    public void TestAudioSystem()
    {
        if (!IsInitialized)
        {
            DebugLog("AudioManager not initialized! Cannot perform audio system test.");
            return;
        }
        
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
    /// <param name="cubeType">The cube type to test</param>
    public void TestCubeLandingSound(Enumerations.CubeType cubeType)
    {
        if (!IsInitialized)
        {
            DebugLog($"Cannot test {cubeType} - AudioManager not initialized");
            return;
        }
        
        Vector3 testPosition = transform.position + Vector3.right * UnityEngine.Random.Range(-2f, 2f);
        
        // Store original volume and use testing volume
        float originalImpactVolume = cubeImpactVolume;
        cubeImpactVolume = testingVolume;
        
        DebugLog($"Testing {cubeType} landing sound at position {testPosition} with volume {testingVolume:F2}");
        
        try
        {
            PlayCubeLandingSound(cubeType, testPosition);
        }
        catch (System.Exception ex)
        {
            DebugLog($"Error testing {cubeType} landing sound: {ex.Message}");
        }
        finally
        {
            // Restore original volume
            cubeImpactVolume = originalImpactVolume;
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
        DebugLog("4. Assign it to the 'cubeAudioConfiguration' field in this AudioManager");
        DebugLog("5. Configure audio clips for each cube type in the ScriptableObject");
        
        #if UNITY_EDITOR
        // In editor, we can help by selecting the AudioManager so the field is visible
        UnityEditor.Selection.activeObject = this;
        #endif
    }
    
    /// <summary>
    /// Tests real-time volume adjustment by playing a test sound
    /// </summary>
    [ContextMenu("Test Volume Adjustment")]
    public void TestVolumeAdjustment()
    {
        if (!IsInitialized)
        {
            DebugLog("AudioManager not initialized! Cannot test volume adjustment.");
            return;
        }
        
        // Play a test sound using the testing volume slider
        if (cubeImpactSounds != null && cubeImpactSounds.Length > 0)
        {
            AudioClip testClip = cubeImpactSounds[0];
            Vector3 testPosition = transform.position;
            
            DebugLog($"Testing volume adjustment: {testingVolume:F2} volume");
            PlayAudioClip(testClip, testingVolume, testPosition);
        }
        else
        {
            DebugLog("No cube impact sounds available for volume testing");
        }
    }
    
    /// <summary>
    /// Tests the audio event system by triggering different event types
    /// </summary>
    [ContextMenu("Test Audio Event System")]
    public void TestAudioEventSystem()
    {
        if (!IsInitialized)
        {
            DebugLog("AudioManager not initialized! Cannot test audio event system.");
            return;
        }
        
        DebugLog("=== AUDIO EVENT SYSTEM TEST STARTED ===");
        
        Vector3 testPosition = transform.position;
        
        // Test basic cube events with different cube types
        System.Array cubeTypes = System.Enum.GetValues(typeof(Enumerations.CubeType));
        foreach (Enumerations.CubeType cubeType in cubeTypes)
        {
            // Test cube landing event (this should play actual audio)
            TriggerCubeAudioEvent(Enumerations.GameAudioEvent.CubeLanded, cubeType, testPosition + Vector3.right * UnityEngine.Random.Range(-2f, 2f), testingVolume);
            
            // Small delay between tests
            System.Threading.Thread.Sleep(150);
            
            // Test cube capture event
            TriggerCubeAudioEvent(Enumerations.GameAudioEvent.CubeCaptured, cubeType, testPosition + Vector3.forward * UnityEngine.Random.Range(-2f, 2f), testingVolume);
            
            System.Threading.Thread.Sleep(150);
        }
        
        // Test other event types (these will log but not play audio yet)
        TriggerAudioEvent(Enumerations.GameAudioEvent.PlayerMoved, testPosition);
        TriggerAudioEvent(Enumerations.GameAudioEvent.LightMarkerPlaced, testPosition + Vector3.left);
        TriggerAudioEvent(Enumerations.GameAudioEvent.PrimeMarkerPlaced, testPosition + Vector3.back);
        TriggerAudioEvent(Enumerations.GameAudioEvent.HeavyMarkerPlaced, testPosition + Vector3.forward);
        TriggerAudioEvent(Enumerations.GameAudioEvent.MarkerTriggered, testPosition);
        TriggerAudioEvent(Enumerations.GameAudioEvent.WaveStarted, Vector3.zero);
        TriggerAudioEvent(Enumerations.GameAudioEvent.WaveCompleted, Vector3.zero);
        TriggerAudioEvent(Enumerations.GameAudioEvent.ResourceRegeneration, testPosition);
        
        DebugLog("=== AUDIO EVENT SYSTEM TEST COMPLETED ===");
        DebugPrintAudioInfo();
    }
    
    #endregion

    #region Core Audio Methods
    
    /// <summary>
    /// Gets a random audio clip from an array of clips for variation
    /// </summary>
    /// <param name="clips">Array of audio clips to choose from</param>
    /// <returns>Random audio clip from the array, or null if array is empty</returns>
    public AudioClip GetRandomAudioClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            if (EnableDebugLogs)
            {
                DebugLog("GetRandomAudioClip called with null or empty clips array");
            }
            return null;
        }
        
        AudioClip selectedClip = clips[Random.Range(0, clips.Length)];
        if (EnableDebugLogs && logAudioEvents)
        {
            DebugLog($"Selected random audio clip: {selectedClip?.name ?? "null"} from array of {clips.Length} clips");
        }
        
        return selectedClip;
    }
    
    #endregion
    
    #region Audio Playback
    /// <summary>
    /// Plays cube landing sound for a specific cube type with cube type and position parameters,
    /// volume control, random variation selection, and proper spatial positioning
    /// </summary>
    /// <param name="cubeType">The type of cube that landed</param>
    /// <param name="position">World position for 3D spatial audio positioning</param>
    public void PlayCubeLandingSound(Enumerations.CubeType cubeType, Vector3 position)
    {
        if (!IsInitialized)
        {
            DebugLog("AudioManager not initialized! Cannot play cube landing sound.");
            return;
        }
        
        // Check if we've exceeded max simultaneous sounds
        if (activeAudioSources.Count >= maxSimultaneousSounds)
        {
            if (EnableDebugLogs)
            {
                DebugLog($"Maximum simultaneous sounds ({maxSimultaneousSounds}) reached. Skipping cube landing sound for {cubeType}");
            }
            return;
        }
        
        AudioClip selectedClip = null;
        float volume = cubeImpactVolume * masterVolume * sfxVolume;
        float pitch = 1f;
        
        // Try to get clip from cube audio configuration first
        if (cubeAudioConfiguration != null)
        {
            selectedClip = cubeAudioConfiguration.GetRandomClip(cubeType, SoundCategory.Landing);
            if (selectedClip != null)
            {
                AudioPlaybackSettings settings = cubeAudioConfiguration.GetPlaybackSettings(cubeType, SoundCategory.Landing);
                volume = settings.volume * masterVolume * sfxVolume;
                pitch = settings.pitch;
                
                if (EnableDebugLogs && logAudioEvents)
                {
                    DebugLog($"Using configured cube landing sound for {cubeType}: {selectedClip.name} at position {position} (Volume: {volume:F2}, Pitch: {pitch:F2})");
                }
            }
        }
        
        // Fallback to legacy cube impact sounds with random variation
        if (selectedClip == null)
        {
            selectedClip = GetRandomAudioClip(cubeImpactSounds);
            if (EnableDebugLogs && logAudioEvents)
            {
                DebugLog($"Using fallback cube impact sound for {cubeType}: {selectedClip?.name ?? "null"} at position {position}");
            }
        }
        
        // Play the selected clip with proper error handling
        if (selectedClip != null)
        {
            PlayAudioClip(selectedClip, volume, position, pitch);
            
            // Track the clip for wave composition
            if (enableWaveComposition)
            {
                lastPlayedCubeSounds[cubeType] = selectedClip;
            }
            
            if (EnableDebugLogs && logAudioEvents)
            {
                DebugLog($"Successfully played cube landing sound for {cubeType} at position {position}");
            }
        }
        else
        {
            if (EnableDebugLogs)
            {
                DebugLog($"No audio clip available for cube landing sound (type: {cubeType})");
            }
        }
    }
    
    
    /// <summary>
    /// Plays cube capture sound for a specific cube type
    /// </summary>
    /// <param name="cubeType">The type of cube that was captured</param>
    /// <param name="position">World position for 3D audio (optional)</param>
    public void PlayCubeCaptureSound(Enumerations.CubeType cubeType, Vector3 position = default)
    {
        if (cubeAudioConfiguration != null)
        {
            AudioClip clip = cubeAudioConfiguration.GetRandomClip(cubeType, SoundCategory.Capture);
            if (clip != null)
            {
                AudioPlaybackSettings settings = cubeAudioConfiguration.GetPlaybackSettings(cubeType, SoundCategory.Capture);
                PlayAudioClip(clip, settings.volume, position, settings.pitch);
                
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
    /// <param name="cubeType">The type of cube that was destroyed</param>
    /// <param name="position">World position for 3D audio (optional)</param>
    public void PlayCubeDestructionSound(Enumerations.CubeType cubeType, Vector3 position = default)
    {
        if (cubeAudioConfiguration != null)
        {
            AudioClip clip = cubeAudioConfiguration.GetRandomClip(cubeType, SoundCategory.Destruction);
            if (clip != null)
            {
                AudioPlaybackSettings settings = cubeAudioConfiguration.GetPlaybackSettings(cubeType, SoundCategory.Destruction);
                PlayAudioClip(clip, settings.volume, position, settings.pitch);
                
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
    /// Plays cube special effect sound for a specific cube type
    /// </summary>
    /// <param name="cubeType">The type of cube triggering the special effect</param>
    /// <param name="position">World position for 3D audio (optional)</param>
    public void PlayCubeSpecialEffectSound(Enumerations.CubeType cubeType, Vector3 position = default)
    {
        if (cubeAudioConfiguration != null)
        {
            AudioClip clip = cubeAudioConfiguration.GetRandomClip(cubeType, SoundCategory.SpecialEffect);
            if (clip != null)
            {
                AudioPlaybackSettings settings = cubeAudioConfiguration.GetPlaybackSettings(cubeType, SoundCategory.SpecialEffect);
                PlayAudioClip(clip, settings.volume, position, settings.pitch);
                
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
    /// Plays a specific audio clip by name from the special effects collection
    /// Designed for specific cube types that need particular sound effects
    /// </summary>
    /// <param name="clipName">Name of the audio clip to play</param>
    /// <param name="position">World position for 3D spatial audio</param>
    /// <param name="volume">Volume override (uses soundEffectsVolume if not specified)</param>
    public void PlayNamedSpecialEffect(string clipName, Vector3 position = default, float volume = -1f)
    {
        if (string.IsNullOrEmpty(clipName))
        {
            if (EnableDebugLogs)
            {
                DebugLog("PlayNamedSpecialEffect called with null or empty clip name");
            }
            return;
        }
        
        AudioClip foundClip = null;
        
        // Search through special effect sounds for the named clip
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
            float playVolume = volume >= 0f ? volume : soundEffectsVolume;
            PlayAudioClip(foundClip, playVolume, position);
            
            if (EnableDebugLogs && logAudioEvents)
            {
                DebugLog($"Played named special effect: {clipName} at position {position} with volume {playVolume:F2}");
            }
        }
        else
        {
            if (EnableDebugLogs)
            {
                DebugLog($"Named special effect sound '{clipName}' not found in specialEffectSounds array");
            }
        }
    }
    
    public void PlayCubeImpactSound(Vector3 position = default)
    {
        if (cubeImpactSounds == null || cubeImpactSounds.Length == 0)
        {
            DebugLog("No cube impact sounds assigned!");
            return;
        }

        AudioClip randomClip = cubeImpactSounds[Random.Range(0, cubeImpactSounds.Length)];
        float volume = cubeImpactVolume * masterVolume;
        
        PlaySpatialAudioClip(randomClip, position, volume);
        
        if (logAudioEvents)
        {
            DebugLog($"Played cube impact sound: {randomClip.name} at position {position} with volume {volume}");
        }
    }

    /// <summary>
    /// Plays an audio clip with 3D spatial positioning using the enhanced pooling system
    /// </summary>
    public void PlaySpatialAudioClip(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
    {
        if (clip == null)
        {
            DebugLog("Attempted to play null AudioClip!");
            return;
        }

        if (!IsInitialized)
        {
            DebugLog("AudioManager not initialized! Cannot play sound.");
            return;
        }

        // Check if we've exceeded max simultaneous sounds
        if (activeAudioSources.Count >= maxSimultaneousSounds)
        {
            DebugLog($"Maximum simultaneous sounds ({maxSimultaneousSounds}) reached. Skipping sound: {clip.name}");
            return;
        }

        // Check for rapid-fire prevention
        if (IsClipPlayedTooRecently(clip))
        {
            return;
        }

        AudioSource audioSource = GetAvailableAudioSource();
        if (audioSource == null)
        {
            DebugLog("Failed to get AudioSource for spatial playback!");
            return;
        }

        // Set up spatial audio
        audioSource.transform.position = position;
        audioSource.clip = clip;
        audioSource.volume = volume * masterVolume;
        audioSource.pitch = pitch;
        audioSource.Play();
        
        // Track the playback
        UpdatePlaybackTracking(clip, audioSource);
        
        // Schedule return to pool after clip finishes
        StartCoroutine(ReturnAudioSourceAfterPlayback(audioSource, clip.length / pitch));
    }

    /// <summary>
    /// Coroutine to automatically return audio source to pool after playback
    /// </summary>
    private System.Collections.IEnumerator ReturnAudioSourceAfterPlayback(AudioSource audioSource, float delay)
    {
        yield return new WaitForSeconds(delay + 0.1f); // Small buffer for safety
        
        if (audioSource != null && !audioSource.isPlaying)
        {
            ReturnAudioSource(audioSource);
        }
    }

    public void PlayCubeDestructionSound(Vector3 position = default)
    {
        if (cubeDestructionSounds == null || cubeDestructionSounds.Length == 0)
        {
            DebugLog("No cube destruction sounds assigned!");
            return;
        }

        AudioClip randomClip = cubeDestructionSounds[Random.Range(0, cubeDestructionSounds.Length)];
        float volume = cubeDestructionVolume * masterVolume;
        
        PlayAudioClip(randomClip, volume, position);
        
        if (logAudioEvents)
        {
            DebugLog($"Played cube destruction sound: {randomClip.name} at volume {volume}");
        }
    }

    public void PlaySpecialEffectSound(int soundIndex, Vector3 position = default)
    {
        if (specialEffectSounds == null || specialEffectSounds.Length == 0)
        {
            DebugLog("No special effect sounds assigned!");
            return;
        }

        if (soundIndex < 0 || soundIndex >= specialEffectSounds.Length)
        {
            DebugLog($"Invalid special effect sound index: {soundIndex}");
            return;
        }

        AudioClip clip = specialEffectSounds[soundIndex];
        float volume = soundEffectsVolume * masterVolume;
        
        PlayAudioClip(clip, volume, position);
        
        if (logAudioEvents)
        {
            DebugLog($"Played special effect sound: {clip.name} at volume {volume}");
        }
    }

    /// <summary>
    /// Plays an audio clip with volume control, spatial positioning, and automatic audio source return to pool
    /// </summary>
    /// <param name="clip">Audio clip to play</param>
    /// <param name="volume">Volume level (0-1)</param>
    /// <param name="position">World position for 3D spatial audio</param>
    /// <param name="pitch">Pitch adjustment (default 1.0)</param>
    public void PlayAudioClip(AudioClip clip, float volume = 1f, Vector3 position = default, float pitch = 1f)
    {
        if (clip == null)
        {
            if (EnableDebugLogs)
            {
                DebugLog("Attempted to play null AudioClip!");
            }
            return;
        }

        if (!IsInitialized)
        {
            if (EnableDebugLogs)
            {
                DebugLog("AudioManager not initialized! Cannot play sound.");
            }
            return;
        }

        // Check if we've exceeded max simultaneous sounds
        if (activeAudioSources.Count >= maxSimultaneousSounds)
        {
            if (EnableDebugLogs)
            {
                DebugLog($"Maximum simultaneous sounds ({maxSimultaneousSounds}) reached. Skipping sound: {clip.name}");
            }
            return;
        }

        // Check for rapid-fire prevention
        if (IsClipPlayedTooRecently(clip))
        {
            //return;
        }

        AudioSource audioSource = GetAudioSource();
        if (audioSource == null)
        {
            if (EnableDebugLogs)
            {
                DebugLog("Failed to get AudioSource for playback!");
            }
            return;
        }

        // Apply distance-based volume falloff for spatial audio
        float finalVolume = volume * masterVolume;
        //if (position != default)
        //{
        //    // Calculate distance-based volume falloff
        //    float distance = Vector3.Distance(position, Camera.main?.transform.position ?? Vector3.zero);
        //    float volumeFalloff = Mathf.Clamp01(1f - (distance / 50f)); // 50f is max distance from Configure3DAudioSource
        //    finalVolume *= volumeFalloff;
        //}

        SetupAudioSourceForPlayback(audioSource, clip, finalVolume, position, pitch);
        audioSource.Play();
        
        // Track the playback
        UpdatePlaybackTracking(clip, audioSource);
        
        // Schedule return to pool after clip finishes playing
        StartCoroutine(ReturnAudioSourceAfterPlayback(audioSource, clip.length / pitch));
        
        if (EnableDebugLogs && logAudioEvents)
        {
            DebugLog($"Playing audio clip: {clip.name} at position {position} with volume {finalVolume:F2} and pitch {pitch:F2}");
        }
    }
    
    /// <summary>
    /// Helper method to play audio clip with enhanced error handling specifically for cube landing events
    /// This method is designed to be called from CubeManager.AnimateMove() during cube landing
    /// </summary>
    /// <param name="clip">Audio clip to play</param>
    /// <param name="position">World position where cube landed</param>
    /// <param name="volume">Volume level for the audio</param>
    public void PlayAudioClip(AudioClip clip, Vector3 position, float volume)
    {
        PlayAudioClip(clip, volume, position, 1f);
    }
    
    /// <summary>
    /// Convenience method for playing cube landing sounds at specific positions
    /// Integrates with CubeManager for seamless audio playback during cube movement
    /// </summary>
    /// <param name="cubeType">Type of cube that landed</param>
    /// <param name="worldPosition">World position where the cube landed</param>
    /// <param name="customVolume">Optional custom volume override</param>
    public void PlayCubeLandingSoundAtPosition(Enumerations.CubeType cubeType, Vector3 worldPosition, float customVolume = -1f)
    {
        if (customVolume >= 0f)
        {
            // Use custom volume if provided
            float originalVolume = cubeImpactVolume;
            cubeImpactVolume = customVolume;
            PlayCubeLandingSound(cubeType, worldPosition);
            cubeImpactVolume = originalVolume;
        }
        else
        {
            PlayCubeLandingSound(cubeType, worldPosition);
        }
    }

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

    private AudioSource GetAudioSource()
    {
        if (useAudioSourcePooling && audioSourcePool.Count > 0)
        {
            AudioSource pooledSource = audioSourcePool.Dequeue();
            pooledSource.gameObject.SetActive(true);
            pooledSourcesUsed++;
            return pooledSource;
        }
        else
        {
            // Create a temporary AudioSource
            GameObject tempAudioObj = new GameObject("TempAudioSource");
            tempAudioObj.transform.SetParent(transform);
            AudioSource tempAudioSource = tempAudioObj.AddComponent<AudioSource>();
            ConfigureAudioSource(tempAudioSource);
            instantiatedSources++;
            return tempAudioSource;
        }
    }

    /// <summary>
    /// Sets up audio source for playback with proper spatial positioning
    /// </summary>
    /// <param name="audioSource">Audio source to configure</param>
    /// <param name="clip">Audio clip to assign</param>
    /// <param name="volume">Volume level</param>
    /// <param name="position">World position for spatial audio</param>
    /// <param name="pitch">Pitch adjustment</param>
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

    private void UpdatePlaybackTracking(AudioClip clip, AudioSource audioSource)
    {
        lastPlayedTimes[clip] = Time.time;
        activeAudioSources.Add(audioSource);
        totalSoundsPlayed++;
    }
    #endregion

    #region Volume Management
    /// <summary>
    /// Enhanced master volume control that proportionally affects all categories
    /// </summary>
    /// <param name="volume">New master volume level (0-1)</param>
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateActiveSourceVolumes();
        
        // Update background music volume
        if (backgroundAudioSource != null)
        {
            backgroundAudioSource.volume = backgroundAudioVolume * masterVolume;
        }
        
        // Update wave composition layers
        UpdateWaveCompositionVolumes();
        
        DebugLog($"Master volume set to: {masterVolume:F2} - all categories updated proportionally");
    }

    public void SetSoundEffectsVolume(float volume)
    {
        soundEffectsVolume = Mathf.Clamp01(volume);
        UpdateActiveSourceVolumes();
        DebugLog($"Sound effects volume set to: {soundEffectsVolume:F2}");
    }

    public void SetCubeImpactVolume(float volume)
    {
        cubeImpactVolume = Mathf.Clamp01(volume);
        DebugLog($"Cube impact volume set to: {cubeImpactVolume:F2}");
    }

    public void SetCubeDestructionVolume(float volume)
    {
        cubeDestructionVolume = Mathf.Clamp01(volume);
        DebugLog($"Cube destruction volume set to: {cubeDestructionVolume:F2}");
    }

    /// <summary>
    /// Sets volume for a specific category with validation
    /// </summary>
    /// <param name="category">Volume category to set</param>
    /// <param name="volume">New volume level (0-1)</param>
    public void SetVolumeForCategory(VolumeCategory category, float volume)
    {
        volume = Mathf.Clamp01(volume);
        
        switch (category)
        {
            case VolumeCategory.Master:
                SetMasterVolume(volume);
                break;
            case VolumeCategory.SoundEffects:
                SetSoundEffectsVolume(volume);
                break;
            case VolumeCategory.CubeImpact:
                SetCubeImpactVolume(volume);
                break;
            case VolumeCategory.CubeDestruction:
                SetCubeDestructionVolume(volume);
                break;
            case VolumeCategory.BackgroundAudio:
                SetBackgroundVolume(volume);
                break;
            case VolumeCategory.SystemAudio:
                SetSystemAudioVolume(volume);
                break;
            case VolumeCategory.WaveComposition:
                SetWaveCompositionVolume(volume);
                break;
            default:
                DebugLog($"Unknown volume category: {category}");
                break;
        }
    }

    /// <summary>
    /// Enhanced method to update all active audio sources with proper volume categories
    /// </summary>
    private void UpdateActiveSourceVolumes()
    {
        foreach (AudioSource audioSource in activeAudioSources)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                // Apply master volume to all active sources
                // Note: Individual category volumes are applied at playback time
                audioSource.volume = audioSource.volume * masterVolume / (audioSource.volume > 0 ? audioSource.volume : 1f);
            }
        }
    }
    
    /// <summary>
    /// Updates wave composition layer volumes when master volume changes
    /// </summary>
    private void UpdateWaveCompositionVolumes()
    {
        if (!enableWaveComposition || activeCompositionLayers == null) return;
        
        for (int i = 0; i < activeCompositionLayers.Count; i++)
        {
            var layer = activeCompositionLayers[i];
            if (layer.audioSource != null && layer.audioSource.isPlaying)
            {
                float layerVolume = CalculateCompositionLayerVolume(i);
                layer.audioSource.volume = layerVolume * masterVolume;
            }
        }
        
        if (EnableDebugLogs && activeCompositionLayers.Count > 0)
        {
            DebugLog($"Updated {activeCompositionLayers.Count} wave composition layer volumes");
        }
    }
    #endregion

    #region Cleanup
    private void PerformCleanup()
    {
        if (Time.time - lastCleanupTime >= soundCleanupInterval)
        {
            CleanupFinishedAudioSources();
            lastCleanupTime = Time.time;
        }
    }

    private void CleanupFinishedAudioSources()
    {
        List<AudioSource> sourcesToRemove = new List<AudioSource>();
        
        foreach (AudioSource audioSource in activeAudioSources)
        {
            if (audioSource == null || !audioSource.isPlaying)
            {
                sourcesToRemove.Add(audioSource);
            }
        }

        foreach (AudioSource sourceToRemove in sourcesToRemove)
        {
            activeAudioSources.Remove(sourceToRemove);
            
            if (sourceToRemove != null)
            {
                ReturnAudioSourceToPool(sourceToRemove);
            }
        }

        if (sourcesToRemove.Count > 0)
        {
            DebugLog($"Cleaned up {sourcesToRemove.Count} finished audio sources");
        }
    }

    private void ReturnAudioSourceToPool(AudioSource audioSource)
    {
        if (useAudioSourcePooling && audioSource.gameObject.name.StartsWith("PooledAudioSource"))
        {
            audioSource.Stop();
            audioSource.clip = null;
            audioSource.gameObject.SetActive(false);
            audioSourcePool.Enqueue(audioSource);
        }
        else
        {
            // Destroy temporary audio sources
            if (audioSource.gameObject != null)
            {
                Destroy(audioSource.gameObject);
            }
        }
    }

    public void StopAllAudio()
    {
        foreach (AudioSource audioSource in activeAudioSources)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
        
        CleanupFinishedAudioSources();
        DebugLog("All audio stopped");
    }

    private void CleanupAudioSystem()
    {
        StopAllAudio();
        StopBackgroundMusic();
        CleanupWaveComposition();
        
        // Ensure all pooled audio sources are properly deactivated
        if (useAudioSourcePooling)
        {
            while (audioSourcePool.Count > 0)
            {
                AudioSource pooledSource = audioSourcePool.Dequeue();
                if (pooledSource != null && pooledSource.gameObject != null)
                {
                    pooledSource.Stop();
                    pooledSource.clip = null;
                    pooledSource.gameObject.SetActive(false);
                }
            }
        }
        
        // Cleanup background audio source
        if (backgroundAudioSource != null)
        {
            Destroy(backgroundAudioSource.gameObject);
        }
        
        // Clear collections
        activeAudioSources.Clear();
        audioSourcePool.Clear();
        lastPlayedTimes.Clear();
        
        DebugLog("Audio system cleanup complete");
    }
    #endregion

    #region Debug & Utility
    private void DebugLog(string message)
    {
        if (EnableDebugLogs)
        {
            this.Log(message, EnableDebugLogs);
        }
    }

    public void DebugPrintAudioInfo()
    {
        DebugLog("=== AUDIO DEBUG INFO ===");
        DebugLog($"Master Volume: {masterVolume}");
        DebugLog($"Sound Effects Volume: {soundEffectsVolume}");
        DebugLog($"Active Audio Sources: {activeAudioSources.Count}");
        DebugLog($"Available Pool Sources: {audioSourcePool.Count}");
        DebugLog($"Total Sounds Played: {totalSoundsPlayed}");
        DebugLog($"Pooled Sources Used: {pooledSourcesUsed}");
        DebugLog($"Instantiated Sources: {instantiatedSources}");
        DebugLog($"Use Audio Source Pooling: {useAudioSourcePooling}");
        
        // Debug enhanced pool status
        DebugPrintPoolStatus();
        
        // Debug cube audio configuration
        if (cubeAudioConfiguration != null)
        {
            DebugLog("=== CUBE AUDIO CONFIGURATION ===");
            DebugLog(cubeAudioConfiguration.GetDiagnosticInfo());
        }
        else
        {
            DebugLog("Cube Audio Configuration: Not Assigned");
        }
    }

    /// <summary>
    /// Debug method to display current audio source pool status
    /// </summary>
    public void DebugPrintPoolStatus()
    {
        DebugLog("=== AUDIO SOURCE POOL STATUS ===");
        DebugLog($"Pool Size: {audioSourcePoolSize}");
        DebugLog($"Available Sources: {audioSourcePool.Count}");
        DebugLog($"Active Sources: {activeAudioSources.Count}");
        DebugLog($"Pool Utilization: {(audioSourcePoolSize > 0 ? (float)(audioSourcePoolSize - audioSourcePool.Count) / audioSourcePoolSize * 100f : 0f):F1}%");
        DebugLog($"3D Spatial Audio: Enabled");
        DebugLog($"Max Distance: 50f units");
        DebugLog($"Rolloff Mode: Logarithmic");
    }

    private void OnDrawGizmosSelected()
    {
        if (showAudioGizmos && activeAudioSources != null)
        {
            Gizmos.color = Color.yellow;
            foreach (AudioSource audioSource in activeAudioSources)
            {
                if (audioSource != null && audioSource.isPlaying)
                {
                    Gizmos.DrawWireSphere(audioSource.transform.position, 1f);
                }
            }
        }
    }
    #endregion

    #region Audio Event System
    
    /// <summary>
    /// Triggers an audio event using comprehensive event data
    /// </summary>
    /// <param name="eventData">Complete audio event data structure</param>
    public void TriggerAudioEvent(AudioEventData eventData)
    {
        if (!IsInitialized)
        {
            DebugLog("AudioManager not initialized. Skipping audio event.");
            return;
        }
        
        if (!eventData.IsValid())
        {
            DebugLog("AudioEvent data is not valid. Skipping audio event.");
            return;
        }
        
        if (EnableDebugLogs && logAudioEvents)
        {
            DebugLog($"Processing audio event: {eventData}");
        }
        
        ProcessAudioEvent(eventData);
    }
    
    /// <summary>
    /// Triggers an audio event with basic parameters
    /// </summary>
    /// <param name="eventType">Type of audio event to trigger</param>
    /// <param name="worldPosition">World position for spatial audio</param>
    /// <param name="intensity">Audio intensity/volume multiplier</param>
    public void TriggerAudioEvent(Enumerations.GameAudioEvent eventType, Vector3 worldPosition, float intensity = 1f)
    {
        AudioEventData eventData = new AudioEventData(eventType, worldPosition, intensity);
        TriggerAudioEvent(eventData);
    }
    
    /// <summary>
    /// Triggers a cube-specific audio event with cube type information
    /// </summary>
    /// <param name="eventType">Type of audio event to trigger</param>
    /// <param name="cubeType">Type of cube involved in the event</param>
    /// <param name="worldPosition">World position for spatial audio</param>
    /// <param name="intensity">Audio intensity/volume multiplier</param>
    public void TriggerCubeAudioEvent(Enumerations.GameAudioEvent eventType, Enumerations.CubeType cubeType, Vector3 worldPosition, float intensity = 1f)
    {
        AudioEventData eventData = new AudioEventData(eventType, cubeType, worldPosition, intensity);
        TriggerAudioEvent(eventData);
    }
    
    /// <summary>
    /// Internal method to process audio events and trigger appropriate sounds
    /// </summary>
    /// <param name="eventData">Audio event data to process</param>
    private void ProcessAudioEvent(AudioEventData eventData)
    {
        switch (eventData.eventType)
        {
            case Enumerations.GameAudioEvent.CubeLanded:
                PlayCubeLandingSound(eventData.cubeType, eventData.worldPosition);
                break;
                
            case Enumerations.GameAudioEvent.CubeCaptured:
                PlayCubeCaptureSound(eventData.cubeType, eventData.worldPosition);
                break;
                
            case Enumerations.GameAudioEvent.CubeEscaped:
                // TODO: Implement cube escape sound when audio clips are available
                if (EnableDebugLogs && logAudioEvents)
                {
                    DebugLog($"Cube escape event triggered for {eventData.cubeType} at {eventData.worldPosition} (no audio implementation yet)");
                }
                break;
                
            case Enumerations.GameAudioEvent.PlayerMoved:
                // TODO: Implement player movement sound when audio clips are available
                if (EnableDebugLogs && logAudioEvents)
                {
                    DebugLog($"Player moved event triggered at {eventData.worldPosition} (no audio implementation yet)");
                }
                break;
                
            case Enumerations.GameAudioEvent.LightMarkerPlaced:
                PlayMarkerPlaceSound(Enumerations.MarkerType.Light, eventData.worldPosition);
                break;
            case Enumerations.GameAudioEvent.HeavyMarkerPlaced:
                PlayMarkerPlaceSound(Enumerations.MarkerType.Heavy, eventData.worldPosition);
                break;
            case Enumerations.GameAudioEvent.PrimeMarkerPlaced:
                PlayMarkerPlaceSound(Enumerations.MarkerType.Prime, eventData.worldPosition);
                break;
                
            case Enumerations.GameAudioEvent.MarkerTriggered:
                // For now, play a generic marker trigger sound - could be enhanced with marker type info
                PlayUIClickSound(); // Placeholder until we have marker trigger event with type info
                if (EnableDebugLogs && logAudioEvents)
                {
                    DebugLog($"Marker triggered event at {eventData.worldPosition}");
                }
                break;
                
            case Enumerations.GameAudioEvent.WaveStarted:
                PlayWaveStartSound();
                break;
                
            case Enumerations.GameAudioEvent.WaveCompleted:
                PlayWaveCompleteSound();
                break;
                
            case Enumerations.GameAudioEvent.ResourceRegeneration:
                // TODO: Implement resource regeneration sound when audio clips are available
                if (EnableDebugLogs && logAudioEvents)
                {
                    DebugLog($"Resource regeneration event triggered at {eventData.worldPosition} (no audio implementation yet)");
                }
                break;
                
            case Enumerations.GameAudioEvent.MessageShow:
                // Play message show audio feedback
                PlayUIClickSound(); // Placeholder - could be enhanced with message-specific sounds
                if (EnableDebugLogs && logAudioEvents)
                {
                    DebugLog($"Message show event triggered at {eventData.worldPosition}");
                }
                break;
                
            case Enumerations.GameAudioEvent.MessageHide:
                // Play message hide audio feedback (subtle)
                if (EnableDebugLogs && logAudioEvents)
                {
                    DebugLog($"Message hide event triggered at {eventData.worldPosition}");
                }
                // Note: Intentionally subtle/silent for normal message dismissal
                break;
                
            case Enumerations.GameAudioEvent.MessageSkip:
                // Play message skip audio feedback
                PlayUIClickSound(); // Placeholder - could be distinct skip sound
                if (EnableDebugLogs && logAudioEvents)
                {
                    DebugLog($"Message skip event triggered at {eventData.worldPosition}");
                }
                break;
                
            default:
                if (EnableDebugLogs)
                {
                    DebugLog( $"Unknown audio event type: {eventData.eventType}");
                }
                break;
        }
    }

    #endregion

    #region IManagerDebugInterface Implementation
    public bool EnableDebugLogs { get; set; } = true;

    public string GetDebugStatus()
    {
        return $"Audio: Master:{masterVolume:F2} Cube:{cubeImpactVolume:F2} BG:{backgroundAudioVolume:F2} Sys:{systemAudioVolume:F2} Comp:{waveCompositionVolume:F2} Active:{activeAudioSources.Count}/{maxSimultaneousSounds} Pool:{audioSourcePool.Count} Track:{GetCurrentTrack()?.name ?? "None"} CompLayers:{activeCompositionLayers.Count}";
    }

    public Dictionary<string, object> GetDebugData()
    {
        var debugData = new Dictionary<string, object>
        {
            ["Is Initialized"] = IsInitialized,
            ["Master Volume"] = masterVolume,
            ["Sound Effects Volume"] = soundEffectsVolume,
            ["Cube Impact Volume"] = cubeImpactVolume,
            ["Cube Destruction Volume"] = cubeDestructionVolume,
            ["Active Audio Sources"] = activeAudioSources.Count,
            ["Max Simultaneous Sounds"] = maxSimultaneousSounds,
            ["Available Pool Sources"] = audioSourcePool.Count,
            ["Audio Source Pool Size"] = audioSourcePoolSize,
            ["Pool Utilization %"] = audioSourcePoolSize > 0 ? (float)(audioSourcePoolSize - audioSourcePool.Count) / audioSourcePoolSize * 100f : 0f,
            ["Use Audio Source Pooling"] = useAudioSourcePooling,
            ["3D Spatial Audio Enabled"] = true,
            ["Total Sounds Played"] = totalSoundsPlayed,
            ["Pooled Sources Used"] = pooledSourcesUsed,
            ["Instantiated Sources"] = instantiatedSources,
            ["Cube Impact Sounds Count"] = cubeImpactSounds?.Length ?? 0,
            ["Cube Destruction Sounds Count"] = cubeDestructionSounds?.Length ?? 0,
            ["Special Effect Sounds Count"] = specialEffectSounds?.Length ?? 0,
            ["Audio Source Prefab Assigned"] = audioSourcePrefab != null,
            ["Log Audio Events"] = logAudioEvents,
            ["Show Audio Gizmos"] = showAudioGizmos,
            ["Sound Cleanup Interval"] = soundCleanupInterval,
            ["Testing Volume"] = testingVolume,
            ["Show Testing Instructions"] = showTestingInstructions,
            
            // Background music information
            ["Background Music Enabled"] = enableBackgroundMusic,
            ["Background Audio Volume"] = backgroundAudioVolume,
            ["Background Music Playing"] = IsPlayingBackground,
            ["Current Track"] = GetCurrentTrack()?.name ?? "None",
            ["Total Background Tracks"] = backgroundAmbientTracks?.Length ?? 0,
            ["Shuffle Playlist"] = shufflePlaylist,
            ["Track Transition Time"] = trackTransitionTime,
            ["Current Track Index"] = currentTrackIndex,
            ["Is Transitioning"] = isTransitioning,
            
            // System feedback audio information
            ["System Audio Volume"] = systemAudioVolume,
            ["Wave Start Sound Assigned"] = waveStartSound != null,
            ["Wave Complete Sound Assigned"] = waveCompleteSound != null,
            ["UI Click Sound Assigned"] = uiClickSound != null,
            ["Light Marker Place Sound Assigned"] = lightMarkerPlaceSound != null,
            ["Heavy Marker Place Sound Assigned"] = heavyMarkerPlaceSound != null,
            ["Prime Marker Place Sound Assigned"] = primeMarkerPlaceSound != null,
            ["Light Marker Trigger Sound Assigned"] = lightMarkerTriggerSound != null,
            ["Heavy Marker Trigger Sound Assigned"] = heavyMarkerTriggerSound != null,
            ["Prime Marker Trigger Sound Assigned"] = primeMarkerTriggerSound != null,
            
            // Audio event system information
            ["Audio Event System Enabled"] = true,
            ["Supported Event Types"] = System.Enum.GetNames(typeof(Enumerations.GameAudioEvent)).Length,
            ["Event Data Validation"] = "Enabled",
            
            // Wave composition system information
            ["Wave Composition Enabled"] = enableWaveComposition,
            ["Wave Composition Volume"] = waveCompositionVolume,
            ["Composition Layer Delay"] = compositionLayerDelay,
            ["Max Composition Layers"] = maxCompositionLayers,
            ["Active Composition Layers"] = activeCompositionLayers.Count,
            ["Last Wave Step Time"] = lastWaveStepTime,
            ["Tracked Cube Sounds"] = lastPlayedCubeSounds.Count,
            ["Wave Composition Coroutine Active"] = waveCompositionCoroutine != null
        };
        
        // Add enhanced audio clip assignment validation
        debugData["Cube Audio Configuration Assigned"] = cubeAudioConfiguration != null;
        
        if (cubeAudioConfiguration != null)
        {
            debugData["Cube Audio Global Volume"] = cubeAudioConfiguration.globalCubeAudioVolume;
            debugData["Cube Audio Debug Logs"] = cubeAudioConfiguration.enableAudioDebugLogs;
            
            // Validate configuration and add results to debug data
            bool configurationValid = cubeAudioConfiguration.ValidateConfiguration();
            debugData["Configuration Valid"] = configurationValid;
            
            // Count configured cube types
            int configuredCubeTypes = 0;
            int totalCubeTypes = System.Enum.GetValues(typeof(Enumerations.CubeType)).Length;
            
            foreach (Enumerations.CubeType cubeType in System.Enum.GetValues(typeof(Enumerations.CubeType)))
            {
                var audioData = cubeAudioConfiguration.GetAudioData(cubeType);
                if (audioData != null && audioData.HasAnyAudioClips())
                {
                    configuredCubeTypes++;
                }
            }
            
            debugData["Configured Cube Types"] = $"{configuredCubeTypes}/{totalCubeTypes}";
            debugData["Configuration Coverage %"] = totalCubeTypes > 0 ? (float)configuredCubeTypes / totalCubeTypes * 100f : 0f;
            
            // Check fallback audio availability
            debugData["Fallback Landing Clips"] = cubeAudioConfiguration.fallbackLandingSounds?.GetValidClipCount() ?? 0;
            debugData["Fallback Capture Clips"] = cubeAudioConfiguration.fallbackCaptureSounds?.GetValidClipCount() ?? 0;
            debugData["Fallback Destruction Clips"] = cubeAudioConfiguration.fallbackDestructionSounds?.GetValidClipCount() ?? 0;
            debugData["Fallback Special Effect Clips"] = cubeAudioConfiguration.fallbackSpecialEffectSounds?.GetValidClipCount() ?? 0;
        }
        else
        {
            debugData["Cube Audio Global Volume"] = 0f;
            debugData["Cube Audio Debug Logs"] = false;
            debugData["Configuration Valid"] = false;
            debugData["Configured Cube Types"] = "0/0 (No Configuration)";
            debugData["Configuration Coverage %"] = 0f;
            debugData["Configuration Validation Error"] = "CubeAudioConfiguration not assigned";
        }
        
        // Add audio folder structure validation results
        bool audioFolderExists = System.IO.Directory.Exists("Assets/Audio");
        debugData["Audio Folder Exists"] = audioFolderExists;
        
        if (audioFolderExists)
        {
            string[] recommendedFolders = { "CubeLanding", "CubeCapture", "CubeDestruction", "SpecialEffects", "Fallback" };
            int existingSubfolders = 0;
            
            foreach (string folder in recommendedFolders)
            {
                string fullPath = System.IO.Path.Combine("Assets/Audio", folder);
                if (System.IO.Directory.Exists(fullPath))
                {
                    existingSubfolders++;
                }
            }
            
            debugData["Audio Subfolders"] = $"{existingSubfolders}/{recommendedFolders.Length}";
            debugData["Audio Organization %"] = (float)existingSubfolders / recommendedFolders.Length * 100f;
        }
        else
        {
            debugData["Audio Subfolders"] = "0/0 (No Audio Folder)";
            debugData["Audio Organization %"] = 0f;
        }
        
        return debugData;
    }

    public void ResetToDefaults()
    {
        // Stop all current audio
        StopAllAudio();
        StopBackgroundMusic();
        CleanupWaveComposition();
        
        // Reset volume settings
        masterVolume = 1f;
        soundEffectsVolume = 0.8f;
        cubeImpactVolume = 0.7f;
        cubeDestructionVolume = 0.6f;
        backgroundAudioVolume = 0.3f;
        systemAudioVolume = 0.7f;
        
        // Reset background music settings
        enableBackgroundMusic = true;
        shufflePlaylist = true;
        trackTransitionTime = 2f;
        currentTrackIndex = 0;
        isTransitioning = false;
        
        // Reset performance settings
        audioSourcePoolSize = 10;
        maxSimultaneousSounds = 8;
        useAudioSourcePooling = true;
        soundCleanupInterval = 5f;
        
        // Reset debug settings
        EnableDebugLogs = true;
        showAudioGizmos = false;
        logAudioEvents = false;
        
        // Reset wave composition settings
        enableWaveComposition = true;
        compositionLayerDelay = 0.3f;
        maxCompositionLayers = 5;
        waveCompositionVolume = 0.6f;
        if (volumeFalloffCurve == null || volumeFalloffCurve.length < 2)
        {
            volumeFalloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f);
        }
        
        // Reset all audio categories properly
        InitializeWaveComposition();
        
        // Reinitialize background music if needed
        if (backgroundAmbientTracks != null && backgroundAmbientTracks.Length > 0)
        {
            InitializePlaylist();
            if (enableBackgroundMusic)
            {
                StartBackgroundMusic();
            }
        }
        
        // Reset tracking variables
        totalSoundsPlayed = 0;
        pooledSourcesUsed = 0;
        instantiatedSources = 0;
        lastCleanupTime = 0f;
        
        // Clear collections
        lastPlayedTimes.Clear();
        shuffledTrackIndices.Clear();
        
        // Reinitialize if needed
        if (IsInitialized)
        {
            CleanupAudioSystem();
            SetupAudioSystem();
        }
        
        if (EnableDebugLogs)
        {
            this.Log("Reset to defaults completed", EnableDebugLogs);
        }
    }

    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading for audio settings
        if (EnableDebugLogs)
        {
            this.Log($"Loading configuration: {configName} (not yet implemented)", EnableDebugLogs);
        }
    }

    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving for audio settings
        if (EnableDebugLogs)
        {
            this.Log($"Saving configuration: {configName} (not yet implemented)", EnableDebugLogs);
        }
    }
    #endregion
}

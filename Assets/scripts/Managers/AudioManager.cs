using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour, IManagerDebugInterface
{
    #region Inspector Configuration
    [Header("Audio Configuration")]
    public AudioSource audioSourcePrefab;
    public CubeAudioConfiguration cubeAudioConfiguration;
    public AudioClip[] cubeImpactSounds;
    public AudioClip[] cubeDestructionSounds;
    public AudioClip[] specialEffectSounds;

    [Header("Volume Controls")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float soundEffectsVolume = 0.8f;
    [Range(0f, 1f)] public float cubeImpactVolume = 0.7f;
    [Range(0f, 1f)] public float cubeDestructionVolume = 0.6f;

    [Header("Performance Settings")]
    public int audioSourcePoolSize = 10;
    public int maxSimultaneousSounds = 8;
    public bool useAudioSourcePooling = true;
    public float soundCleanupInterval = 5f;

    [Header("Debug Options")]
    public bool enableDebugLogs = true;
    public bool showAudioGizmos = false;
    public bool logAudioEvents = false;
    #endregion

    #region Runtime State
    private Queue<AudioSource> audioSourcePool = new Queue<AudioSource>();
    private List<AudioSource> activeAudioSources = new List<AudioSource>();
    private Dictionary<AudioClip, float> lastPlayedTimes = new Dictionary<AudioClip, float>();
    private float lastCleanupTime = 0f;
    
    // Performance tracking
    private int totalSoundsPlayed = 0;
    private int pooledSourcesUsed = 0;
    private int instantiatedSources = 0;
    #endregion

    #region Properties
    public static AudioManager Instance { get; private set; }
    public bool IsInitialized { get; private set; }
    public int ActiveSources => activeAudioSources.Count;
    public int AvailablePoolSources => audioSourcePool.Count;
    public float CurrentMasterVolume => masterVolume;
    public float sfxVolume 
    { 
        get => soundEffectsVolume; 
        set => soundEffectsVolume = Mathf.Clamp01(value); 
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
    }

    private void Update()
    {
        PerformCleanup();
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
        
        // Clamp volume values
        masterVolume = Mathf.Clamp01(masterVolume);
        soundEffectsVolume = Mathf.Clamp01(soundEffectsVolume);
        cubeImpactVolume = Mathf.Clamp01(cubeImpactVolume);
        cubeDestructionVolume = Mathf.Clamp01(cubeDestructionVolume);
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
            if (enableDebugLogs)
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
                
                if (enableDebugLogs && logAudioEvents)
                {
                    DebugLog($"Using configured cube landing sound for {cubeType}: {selectedClip.name} at position {position} (Volume: {volume:F2}, Pitch: {pitch:F2})");
                }
            }
        }
        
        // Fallback to legacy cube impact sounds with random variation
        if (selectedClip == null)
        {
            selectedClip = GetRandomAudioClip(cubeImpactSounds);
            if (enableDebugLogs && logAudioEvents)
            {
                DebugLog($"Using fallback cube impact sound for {cubeType}: {selectedClip?.name ?? "null"} at position {position}");
            }
        }
        
        // Play the selected clip with proper error handling
        if (selectedClip != null)
        {
            PlayAudioClip(selectedClip, volume, position, pitch);
            
            if (enableDebugLogs && logAudioEvents)
            {
                DebugLog($"Successfully played cube landing sound for {cubeType} at position {position}");
            }
        }
        else
        {
            if (enableDebugLogs)
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
            if (enableDebugLogs)
            {
                DebugLog("Attempted to play null AudioClip!");
            }
            return;
        }

        if (!IsInitialized)
        {
            if (enableDebugLogs)
            {
                DebugLog("AudioManager not initialized! Cannot play sound.");
            }
            return;
        }

        // Check if we've exceeded max simultaneous sounds
        if (activeAudioSources.Count >= maxSimultaneousSounds)
        {
            if (enableDebugLogs)
            {
                DebugLog($"Maximum simultaneous sounds ({maxSimultaneousSounds}) reached. Skipping sound: {clip.name}");
            }
            return;
        }

        // Check for rapid-fire prevention
        if (IsClipPlayedTooRecently(clip))
        {
            return;
        }

        AudioSource audioSource = GetAudioSource();
        if (audioSource == null)
        {
            if (enableDebugLogs)
            {
                DebugLog("Failed to get AudioSource for playback!");
            }
            return;
        }

        // Apply distance-based volume falloff for spatial audio
        float finalVolume = volume * masterVolume;
        if (position != default)
        {
            // Calculate distance-based volume falloff
            float distance = Vector3.Distance(position, Camera.main?.transform.position ?? Vector3.zero);
            float volumeFalloff = Mathf.Clamp01(1f - (distance / 50f)); // 50f is max distance from Configure3DAudioSource
            finalVolume *= volumeFalloff;
        }

        SetupAudioSourceForPlayback(audioSource, clip, finalVolume, position, pitch);
        audioSource.Play();
        
        // Track the playback
        UpdatePlaybackTracking(clip, audioSource);
        
        // Schedule return to pool after clip finishes playing
        StartCoroutine(ReturnAudioSourceAfterPlayback(audioSource, clip.length / pitch));
        
        if (enableDebugLogs && logAudioEvents)
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
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateActiveSourceVolumes();
        DebugLog($"Master volume set to: {masterVolume}");
    }

    public void SetSoundEffectsVolume(float volume)
    {
        soundEffectsVolume = Mathf.Clamp01(volume);
        UpdateActiveSourceVolumes();
        DebugLog($"Sound effects volume set to: {soundEffectsVolume}");
    }

    public void SetCubeImpactVolume(float volume)
    {
        cubeImpactVolume = Mathf.Clamp01(volume);
        DebugLog($"Cube impact volume set to: {cubeImpactVolume}");
    }

    public void SetCubeDestructionVolume(float volume)
    {
        cubeDestructionVolume = Mathf.Clamp01(volume);
        DebugLog($"Cube destruction volume set to: {cubeDestructionVolume}");
    }

    private void UpdateActiveSourceVolumes()
    {
        foreach (AudioSource audioSource in activeAudioSources)
        {
            if (audioSource != null && audioSource.isPlaying)
            {
                // Adjust volume based on the type of sound (this is a simplified approach)
                audioSource.volume = soundEffectsVolume * masterVolume;
            }
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
        if (enableDebugLogs)
        {
            Debug.Log($"[AudioManager] {message}");
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

    #region IManagerDebugInterface Implementation
    public bool EnableDebugLogs 
    { 
        get => enableDebugLogs; 
        set => enableDebugLogs = value; 
    }

    public string GetDebugStatus()
    {
        return $"Audio: Master:{masterVolume:F2} SFX:{soundEffectsVolume:F2} Active:{activeAudioSources.Count}/{maxSimultaneousSounds} Pool:{audioSourcePool.Count}/{audioSourcePoolSize} 3D:Enabled Total:{totalSoundsPlayed}";
    }

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
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
            ["Cube Audio Configuration Assigned"] = cubeAudioConfiguration != null,
            ["Cube Audio Global Volume"] = cubeAudioConfiguration?.globalCubeAudioVolume ?? 0f,
            ["Cube Audio Debug Logs"] = cubeAudioConfiguration?.enableAudioDebugLogs ?? false
        };
    }

    public void ResetToDefaults()
    {
        // Stop all current audio
        StopAllAudio();
        
        // Reset volume settings
        masterVolume = 1f;
        soundEffectsVolume = 0.8f;
        cubeImpactVolume = 0.7f;
        cubeDestructionVolume = 0.6f;
        
        // Reset performance settings
        audioSourcePoolSize = 10;
        maxSimultaneousSounds = 8;
        useAudioSourcePooling = true;
        soundCleanupInterval = 5f;
        
        // Reset debug settings
        enableDebugLogs = true;
        showAudioGizmos = false;
        logAudioEvents = false;
        
        // Reset tracking variables
        totalSoundsPlayed = 0;
        pooledSourcesUsed = 0;
        instantiatedSources = 0;
        lastCleanupTime = 0f;
        
        // Clear collections
        lastPlayedTimes.Clear();
        
        // Reinitialize if needed
        if (IsInitialized)
        {
            CleanupAudioSystem();
            SetupAudioSystem();
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
    #endregion
}

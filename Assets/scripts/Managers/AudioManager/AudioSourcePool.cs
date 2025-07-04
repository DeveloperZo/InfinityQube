using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages audio source pooling and lifecycle for the AudioManager system.
/// Handles creation, reuse, and cleanup of AudioSource components to optimize performance.
/// </summary>
public class AudioSourcePool : MonoBehaviour
{
    #region Configuration
    private AudioSource audioSourcePrefab;
    private int poolSize;
    private int maxSimultaneousSounds;
    private float soundCleanupInterval;
    private bool usePooling;
    private Transform parentTransform;
    #endregion

    #region Runtime State
    private Queue<AudioSource> availablePool = new Queue<AudioSource>();
    private List<AudioSource> activeAudioSources = new List<AudioSource>();
    private Coroutine cleanupCoroutine;
    
    // Performance tracking
    private int pooledSourcesUsed = 0;
    private int instantiatedSources = 0;
    #endregion

    #region Properties
    public int AvailableCount => availablePool.Count;
    public int ActiveCount => activeAudioSources.Count;
    public int PooledSourcesUsed => pooledSourcesUsed;
    public int InstantiatedSources => instantiatedSources;
    public bool IsInitialized { get; private set; }
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the audio source pool with specified configuration
    /// </summary>
    public void Initialize(AudioSource prefab, int size, int maxSounds, float cleanupInterval, bool enablePooling, Transform parent)
    {
        audioSourcePrefab = prefab;
        poolSize = size;
        maxSimultaneousSounds = maxSounds;
        soundCleanupInterval = cleanupInterval;
        usePooling = enablePooling;
        parentTransform = parent;

        if (usePooling)
        {
            CreateInitialPool();
        }

        // Start cleanup coroutine
        if (cleanupCoroutine != null)
        {
            StopCoroutine(cleanupCoroutine);
        }
        cleanupCoroutine = StartCoroutine(CleanupRoutine());

        IsInitialized = true;
        DebugLog($"AudioSourcePool initialized with {poolSize} sources");
    }

    private void CreateInitialPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource pooledSource = CreatePooledAudioSource();
            availablePool.Enqueue(pooledSource);
        }
    }

    private AudioSource CreatePooledAudioSource()
    {
        GameObject audioSourceObj = Instantiate(audioSourcePrefab.gameObject);
        audioSourceObj.name = $"PooledAudioSource_{availablePool.Count}";
        audioSourceObj.transform.SetParent(parentTransform);
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
        audioSource.spatialBlend = 0f; // 2D sound by default
        audioSource.volume = 1f;
        audioSource.pitch = 1f;
        
        // Set reasonable defaults for 3D audio
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.maxDistance = 50f;
        audioSource.minDistance = 1f;
    }
    #endregion

    #region Audio Source Management
    /// <summary>
    /// Gets an available audio source from the pool or creates a new one
    /// </summary>
    public AudioSource GetAudioSource()
    {
        // Check if we've exceeded max simultaneous sounds
        if (activeAudioSources.Count >= maxSimultaneousSounds)
        {
            DebugLog($"Maximum simultaneous sounds ({maxSimultaneousSounds}) reached");
            return null;
        }

        AudioSource audioSource = null;

        if (usePooling && availablePool.Count > 0)
        {
            audioSource = availablePool.Dequeue();
            audioSource.gameObject.SetActive(true);
            pooledSourcesUsed++;
        }
        else
        {
            // Create a temporary AudioSource
            GameObject tempAudioObj = new GameObject("TempAudioSource");
            tempAudioObj.transform.SetParent(parentTransform);
            AudioSource tempAudioSource = tempAudioObj.AddComponent<AudioSource>();
            ConfigureAudioSource(tempAudioSource);
            audioSource = tempAudioSource;
            instantiatedSources++;
        }

        if (audioSource != null)
        {
            activeAudioSources.Add(audioSource);
        }

        return audioSource;
    }

    /// <summary>
    /// Gets an audio source configured for 3D spatial audio
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
    /// Returns an audio source to the pool
    /// </summary>
    public void ReturnAudioSource(AudioSource audioSource)
    {
        if (audioSource == null) return;
        
        activeAudioSources.Remove(audioSource);
        
        if (usePooling)
        {
            ReturnToPool(audioSource);
        }
        else
        {
            // Destroy temporary audio sources
            Destroy(audioSource.gameObject);
        }
    }

    private void ReturnToPool(AudioSource audioSource)
    {
        if (audioSource == null) return;
        
        // Reset audio source to default state
        audioSource.Stop();
        audioSource.clip = null;
        audioSource.volume = 1f;
        audioSource.pitch = 1f;
        audioSource.spatialBlend = 0f;
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        
        // Reset position
        audioSource.transform.position = Vector3.zero;
        audioSource.transform.rotation = Quaternion.identity;
        
        // Deactivate and return to pool
        audioSource.gameObject.SetActive(false);
        availablePool.Enqueue(audioSource);
    }

    /// <summary>
    /// Configures an audio source for 3D spatial sound
    /// </summary>
    public void Configure3DAudioSource(AudioSource audioSource)
    {
        audioSource.spatialBlend = 1.0f; // Full 3D spatial sound
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.maxDistance = 50f;
        audioSource.minDistance = 1f;
        audioSource.spread = 0f; // Directional sound
        audioSource.dopplerLevel = 0.1f; // Minimal doppler effect
        audioSource.playOnAwake = false;
    }
    #endregion

    #region Cleanup
    /// <summary>
    /// Coroutine for periodic cleanup of inactive audio sources
    /// </summary>
    private IEnumerator CleanupRoutine()
    {
        WaitForSeconds waitInterval = new WaitForSeconds(soundCleanupInterval);

        while (true)
        {
            yield return waitInterval;
            PerformCleanup();
        }
    }

    private void PerformCleanup()
    {
        List<AudioSource> sourcesToReturn = new List<AudioSource>();

        // Find inactive audio sources
        foreach (AudioSource source in activeAudioSources)
        {
            if (source != null && !source.isPlaying)
            {
                sourcesToReturn.Add(source);
            }
        }

        // Return inactive sources to pool
        foreach (AudioSource source in sourcesToReturn)
        {
            ReturnAudioSource(source);
        }

        if (sourcesToReturn.Count > 0)
        {
            DebugLog($"Cleaned up {sourcesToReturn.Count} inactive audio sources");
        }
    }

    /// <summary>
    /// Cleans up all audio sources and stops the cleanup coroutine
    /// </summary>
    public void Cleanup()
    {
        if (cleanupCoroutine != null)
        {
            StopCoroutine(cleanupCoroutine);
            cleanupCoroutine = null;
        }

        // Return all active sources to pool
        List<AudioSource> sourcesToReturn = new List<AudioSource>(activeAudioSources);
        foreach (AudioSource source in sourcesToReturn)
        {
            if (source != null)
            {
                source.Stop();
                ReturnAudioSource(source);
            }
        }

        activeAudioSources.Clear();

        // Destroy all pooled sources
        while (availablePool.Count > 0)
        {
            AudioSource source = availablePool.Dequeue();
            if (source != null && source.gameObject != null)
            {
                Destroy(source.gameObject);
            }
        }

        IsInitialized = false;
        DebugLog("AudioSourcePool cleaned up");
    }
    #endregion

    #region Debug
    /// <summary>
    /// Gets debug information about the pool state
    /// </summary>
    public Dictionary<string, object> GetDebugInfo()
    {
        return new Dictionary<string, object>
        {
            ["Available Sources"] = availablePool.Count,
            ["Active Sources"] = activeAudioSources.Count,
            ["Total Pooled Used"] = pooledSourcesUsed,
            ["Total Instantiated"] = instantiatedSources,
            ["Pool Efficiency"] = pooledSourcesUsed > 0 ? 
                $"{(float)pooledSourcesUsed / (pooledSourcesUsed + instantiatedSources) * 100:F1}%" : "0%"
        };
    }

    private void DebugLog(string message)
    {
        // Use AudioManager's debug logging when integrated
        Debug.Log($"[AudioSourcePool] {message}");
    }
    #endregion
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static Enumerations;

public class CubeManager : MonoBehaviour, IManagerDebugInterface
{
    [Header("Cube Properties")]
    [SerializeField] public int level = 1;
    [SerializeField] public Vector2Int position;
    [SerializeField] public CubeType type;
    [SerializeField] public Material material;
    [SerializeField] public GameObject prefab;
    [SerializeField] public float spawnHeight;
    [SerializeField] public int currentHitPoints = 3;
    [SerializeField] public int maxHitPoints = 3;
    [SerializeField] public int moveCount = 0;
    [System.NonSerialized] private CubeData cubeData;

    [Header("Audio Configuration")]
    [SerializeField] 
    [Tooltip("ScriptableObject containing cube-specific audio configuration. Will use AudioManager's configuration if not assigned.")]
    private CubeAudioConfiguration cubeAudioConfig;
    
    [SerializeField]
    [Tooltip("AudioSource component for playing cube-specific audio. Will be created automatically if not assigned.")]
    private AudioSource cubeAudioSource;

    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private float squashDuration = 0.25f;
    public bool isRainingCube = false;

    [Header("Physics")]
    [SerializeField] private bool usePhysics = true;
    [SerializeField] private Rigidbody cubeRigidbody;
    [SerializeField] private Collider cubeCollider;

    [Header("Face Painting System")]
    [SerializeField] private FaceStatus[] faceStatuses = new FaceStatus[4]; // 4 cube faces
    [SerializeField] private Color[] faceColors = new Color[4]; // Visual colors for each face
    [SerializeField] private int[] faceDurations = new int[4]; // Remaining duration for each face (-1 = permanent)
    [SerializeField] private GameObject[] faceIndicators = new GameObject[4]; // Visual indicators
    [SerializeField] private bool showFaceIndicators = true;
    private CubeFace[] currentFaceMapping = new CubeFace[4];

    private GridManager grid;
    private PlayerActionManager playerActionManager;
    public bool isMoving = false;
    public bool isDestroyed = false;
    public float rainSpeed = 3f;
    public float rainHeight = 5f;
    public int targetRow = -1;
    private bool isRainAnimating = false;
    public int moveCountRemaining = 0;
    private float tileScale = 3f;
    private float tileSize = 1f;

    public void Init(GridManager gridManager, CubeData cubeData, float spawnHeight = 2f)
    {
        EnableDebugLogs = true;
        grid = gridManager;
        tileSize = grid.TileSize;

        name = cubeData.Definition?.name ?? cubeData.type.ToString();
        type = cubeData.type;
        position = cubeData.position;
        level = cubeData.level;
        isRainingCube = cubeData.isRainingCube;
        moveCountRemaining = cubeData.moveCountRemaining;
        currentHitPoints = cubeData.Definition.maxHitPoints;
        maxHitPoints = cubeData.Definition.maxHitPoints;

        material = cubeData.Definition?.material;
        prefab = cubeData.Definition?.prefab;

        // Add fallback material assignment for debug-spawned cubes
        if (material == null && grid != null)
        {
            material = grid.GetCubeTypeMaterial(type);
            this.Log($"Used fallback material for {type} cube from GridManager", EnableDebugLogs);
        }
        
        // Final check and warning if material is still null
        if (material == null)
        {
            this.LogWarning($"No material found for {type} cube - visual effects may not work correctly", EnableDebugLogs);
        }

        transform.localScale = new Vector3(tileSize, tileSize, tileSize);

        Vector3 worldPos = grid.GridToWorldPosition(position.x, position.y, spawnHeight);
        transform.position = worldPos;

        this.Log($"Cube {type} initialized at grid ({position.x}, {position.y}) -> world {worldPos}, HP: {currentHitPoints}/{maxHitPoints}", EnableDebugLogs);

        playerActionManager = FindAnyObjectByType<PlayerActionManager>();
        gameObject.name = name;

        InitializeFaceSystem();
        InitializeFaceMapping();
        SetupPhysics();
        SetupAudioSystem();
        UpdateDamageVisual();
        
        // Fire spawn event
        GameEvents.FireCubeSpawn(position, type);
        this.Log($"Fired GameEvents.OnCubeSpawn for {type} cube at ({position.x}, {position.y})", EnableDebugLogs);
    }

    public bool TakeDamage(int damage = 1)
    {
        if (isDestroyed) return false;

        currentHitPoints -= damage;
        this.Log($"{type} cube at ({position.x}, {position.y}) took {damage} damage. HP: {currentHitPoints}/{maxHitPoints}", EnableDebugLogs);

        UpdateDamageVisual();

        if (currentHitPoints <= 0)
        {
            // Play destruction sound when cube is destroyed by damage
            PlayDestructionSound();
            return true;
        }

        return false;
    }

    public void UpdateDamageVisual()
    {
        // Only apply damage visuals to recursion cubes
        if (type != CubeType.Recursion) return;

        // Calculate damage ratio (1.0 = full health, 0.0 = destroyed)
        float damageRatio = maxHitPoints > 0 ? (float)currentHitPoints / maxHitPoints : 1.0f;

        // Apply scale effect - cube shrinks as it takes damage
        float scaleMultiplier = Mathf.Lerp(0.85f, 1.0f, damageRatio);
        Vector3 targetScale = Vector3.one * tileSize * scaleMultiplier;
        
        // Only modify scale if not currently animating movement
        if (!isMoving)
        {
            transform.localScale = targetScale;
        }

        // Apply damage material effect only when damaged
        if (damageRatio < 1.0f)
        {
            Renderer cubeRenderer = GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                // Create new material based on original
                Material damagedMaterial = new Material(material != null ? material : cubeRenderer.material);
                
                // Lerp between gray (damaged) and original color based on damage ratio
                Color originalColor = material != null ? material.color : Color.white;
                Color damagedColor = Color.Lerp(Color.gray, originalColor, damageRatio);
                damagedMaterial.color = damagedColor;
                
                // Slightly reduce metallic and smoothness to show wear
                damagedMaterial.SetFloat("_Metallic", Mathf.Lerp(0.1f, 0.5f, damageRatio));
                damagedMaterial.SetFloat("_Smoothness", Mathf.Lerp(0.2f, 0.8f, damageRatio));
                
                cubeRenderer.material = damagedMaterial;
                
                this.Log($"Recursion cube at ({position.x}, {position.y}) visual damage updated: {damageRatio:F2} health ratio", EnableDebugLogs);
            }
        }
        else if (material != null)
        {
            // Restore original material when at full health
            Renderer cubeRenderer = GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                cubeRenderer.material = material;
            }
        }
    }

    private void SetupPhysics()
    {
        if (!usePhysics) return;

        cubeCollider = GetComponent<Collider>();
        if (cubeCollider == null)
        {
            cubeCollider = gameObject.AddComponent<BoxCollider>();
        }

        cubeCollider.isTrigger = false;
        this.Log($"Collider setup complete for {name} cube", EnableDebugLogs);
    }

    /// <summary>
    /// Sets up the audio system for this cube by configuring AudioSource and CubeAudioConfiguration
    /// </summary>
    private void SetupAudioSystem()
    {
        // Set up AudioSource component
        if (cubeAudioSource == null)
        {
            cubeAudioSource = GetComponent<AudioSource>();
        }
        
        if (cubeAudioSource == null)
        {
            cubeAudioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure AudioSource for cube-specific audio playback
        ConfigureCubeAudioSource();
        
        // Set up audio configuration
        if (cubeAudioConfig == null)
        {
            // Try to get from AudioManager if not assigned locally
            if (AudioManager.Instance != null && AudioManager.Instance.cubeAudioConfiguration != null)
            {
                cubeAudioConfig = AudioManager.Instance.cubeAudioConfiguration;
                this.Log($"Using CubeAudioConfiguration from AudioManager for {type} cube", EnableDebugLogs);
            }
            else
            {
                this.LogWarning($"No CubeAudioConfiguration available for {type} cube - audio will not play", EnableDebugLogs);
            }
        }
        
        // Validate audio configuration
        if (cubeAudioConfig != null)
        {
            var audioData = cubeAudioConfig.GetAudioData(type);
            if (audioData == null || !audioData.HasAnyAudioClips())
            {
                this.LogWarning($"No audio clips configured for cube type {type} in CubeAudioConfiguration", EnableDebugLogs);
            }
        }
        
        this.Log($"Audio system setup complete for {type} cube (AudioSource: {cubeAudioSource != null}, Config: {cubeAudioConfig != null})", EnableDebugLogs);
    }
    
    /// <summary>
    /// Configures the AudioSource component for optimal cube audio playback
    /// </summary>
    private void ConfigureCubeAudioSource()
    {
        if (cubeAudioSource == null) return;
        
        // Configure for 3D spatial audio
        cubeAudioSource.playOnAwake = false;
        cubeAudioSource.loop = false;
        cubeAudioSource.spatialBlend = 1f; // Full 3D spatial sound
        cubeAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        cubeAudioSource.maxDistance = 30f; // Reasonable distance for cube sounds
        cubeAudioSource.minDistance = 1f;
        cubeAudioSource.spread = 0f; // Directional sound
        cubeAudioSource.dopplerLevel = 0.1f; // Minimal doppler effect
        
        // Set default volume and pitch
        cubeAudioSource.volume = 0.8f;
        cubeAudioSource.pitch = 1f;
    }

    public void ResetMovementState()
    {
        isMoving = false;
    }

    #region Cube Audio Playback
    
    /// <summary>
    /// Plays cube audio for the specified sound category using CubeAudioConfiguration
    /// </summary>
    /// <param name="soundCategory">Type of sound to play (Landing, Capture, Destruction, SpecialEffect)</param>
    /// <param name="volumeMultiplier">Optional volume multiplier (default 1.0)</param>
    private void PlayCubeAudio(SoundCategory soundCategory, float volumeMultiplier = 1f)
    {
        if (cubeAudioConfig == null || cubeAudioSource == null)
        {
            // Fallback to AudioManager if no local configuration
            if (AudioManager.Instance != null)
            {
                switch (soundCategory)
                {
                    case SoundCategory.Landing:
                        AudioManager.Instance.PlayCubeLandingSound(GetEffectiveType(), transform.position);
                        break;
                    case SoundCategory.Capture:
                        AudioManager.Instance.PlayCubeCaptureSound(GetEffectiveType(), transform.position);
                        break;
                    case SoundCategory.Destruction:
                        AudioManager.Instance.PlayCubeDestructionSound(GetEffectiveType(), transform.position);
                        break;
                    case SoundCategory.SpecialEffect:
                        AudioManager.Instance.PlayCubeSpecialEffectSound(GetEffectiveType(), transform.position);
                        break;
                }
            }
            return;
        }
        
        // Get audio clip using effective type (considers face painting)
        CubeType effectiveType = GetEffectiveType();
        AudioClip audioClip = cubeAudioConfig.GetRandomClip(effectiveType, soundCategory);
        
        if (audioClip == null)
        {
            // Try with original type if effective type has no audio
            audioClip = cubeAudioConfig.GetRandomClip(type, soundCategory);
        }
        
        if (audioClip != null)
        {
            // Get playback settings from configuration
            AudioPlaybackSettings settings = cubeAudioConfig.GetPlaybackSettings(effectiveType, soundCategory);
            
            // Apply volume multiplier and ensure reasonable values
            float finalVolume = Mathf.Clamp01(settings.volume * volumeMultiplier);
            float finalPitch = Mathf.Clamp(settings.pitch, 0.5f, 2f);
            
            // Configure and play audio
            cubeAudioSource.clip = audioClip;
            cubeAudioSource.volume = finalVolume;
            cubeAudioSource.pitch = finalPitch;
            cubeAudioSource.Play();
            
            this.Log($"Played {soundCategory} audio for {effectiveType} cube: {audioClip.name} (Vol: {finalVolume:F2}, Pitch: {finalPitch:F2})", EnableDebugLogs);
        }
        else
        {
            this.Log($"No {soundCategory} audio available for {effectiveType} cube (fallback also checked)", EnableDebugLogs);
        }
    }
    
    /// <summary>
    /// Plays cube landing sound when cube lands on a tile
    /// </summary>
    public void PlayLandingSound()
    {
        PlayCubeAudio(SoundCategory.Landing);
    }
    
    /// <summary>
    /// Plays cube capture sound when cube is captured by player
    /// </summary>
    public void PlayCaptureSound()
    {
        PlayCubeAudio(SoundCategory.Capture);
    }
    
    /// <summary>
    /// Plays cube destruction sound when cube is destroyed
    /// </summary>
    public void PlayDestructionSound()
    {
        PlayCubeAudio(SoundCategory.Destruction);
    }
    
    /// <summary>
    /// Plays cube special effect sound for special cube interactions
    /// </summary>
    public void PlaySpecialEffectSound()
    {
        PlayCubeAudio(SoundCategory.SpecialEffect);
    }
    
    /// <summary>
    /// Called when this cube is captured - plays capture sound and any additional effects
    /// This method should be called by external systems (like Tile.cs) when cube capture occurs
    /// </summary>
    public void OnCubeCapture()
    {
        PlayCaptureSound();
        
        // Fire captured event
        GameEvents.FireCubeCaptured(position, GetEffectiveType());
        this.Log($"Fired GameEvents.OnCubeCaptured for {GetEffectiveType()} cube at ({position.x}, {position.y})", EnableDebugLogs);
        
        // Add any additional capture effects here if needed
        this.Log($"Cube {GetEffectiveType()} captured at ({position.x}, {position.y}) - capture audio triggered", EnableDebugLogs);
    }
    
    /// <summary>
    /// Validates that audio system is properly configured for this cube
    /// </summary>
    /// <returns>True if audio system is configured and ready</returns>
    public bool IsAudioSystemReady()
    {
        bool hasAudioSource = cubeAudioSource != null;
        bool hasAudioConfig = cubeAudioConfig != null;
        bool hasAudioManager = AudioManager.Instance != null;
        
        return hasAudioSource && (hasAudioConfig || hasAudioManager);
    }
    
    /// <summary>
    /// Gets diagnostic information about the cube's audio system
    /// </summary>
    /// <returns>String containing audio diagnostic information</returns>
    public string GetAudioDiagnostics()
    {
        var diagnostics = new System.Text.StringBuilder();
        diagnostics.AppendLine($"=== Audio Diagnostics for {type} Cube ===");
        diagnostics.AppendLine($"AudioSource: {(cubeAudioSource != null ? "Configured" : "Missing")}");
        diagnostics.AppendLine($"CubeAudioConfig: {(cubeAudioConfig != null ? "Assigned" : "Not Assigned")}");
        diagnostics.AppendLine($"AudioManager Available: {(AudioManager.Instance != null ? "Yes" : "No")}");
        diagnostics.AppendLine($"Audio System Ready: {IsAudioSystemReady()}");
        
        if (cubeAudioConfig != null)
        {
            var audioData = cubeAudioConfig.GetAudioData(type);
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
                diagnostics.AppendLine($"No audio data found for cube type: {type}");
            }
        }
        
        return diagnostics.ToString();
    }
    
    #endregion

    private void OnDestroy()
    {
        isDestroyed = true;
        StopAllCoroutines();
        
        // Play destruction sound before cleanup
        if (cubeAudioSource != null && !cubeAudioSource.isPlaying)
        {
            PlayDestructionSound();
        }

        // Notify FacePaintingManager that cube is leaving
        FacePaintingManager facePaintingManager = FindObjectOfType<FacePaintingManager>();
        if (facePaintingManager != null)
        {
            facePaintingManager.OnCubeLeft(position);
        }

        // Clean up face indicators
        for (int i = 0; i < faceIndicators.Length; i++)
        {
            if (faceIndicators[i] != null)
            {
                Destroy(faceIndicators[i]);
                faceIndicators[i] = null;
            }
        }
        
        // Clean up audio source if it exists
        if (cubeAudioSource != null)
        {
            cubeAudioSource.Stop();
        }
    }

    /// <summary>
    /// Moves the cube forward (down) by one position. This is the core movement logic.
    /// CUBE ESCAPE MECHANIC: When a cube moves below grid bounds (position.y < 0), it "escapes".
    /// Escape triggers failure conditions and affects stage progression.
    /// </summary>
    /// <returns>True if cube continues moving, false if cube escapes or is destroyed</returns>
    public bool MoveForward()
    {
        if (isMoving || isDestroyed) return true;

        this.Log($"Moving cube {GetEffectiveType()} from ({position.x}, {position.y}) forward", EnableDebugLogs);

        // CUBE ESCAPE CONDITION: Check if cube is outside grid bounds
        // Primary escape condition: position.y < 0 (below grid)
        // Secondary escape conditions: position.x < 0 or position.x >= grid.Width (outside horizontal bounds)
        if (position.y < 0 || position.x < 0 || position.x >= grid.Width)
        {
            this.Log($"🚨 CUBE ESCAPE: {GetEffectiveType()} at ({position.x}, {position.y}) is off-grid. Grid bounds: {grid.Width}x{grid.Height}", EnableDebugLogs);

            if (!isRainingCube || moveCountRemaining <= 0)
            {
                CubeType effectiveType = GetEffectiveType();

                if (effectiveType == CubeType.Infinity)
                {
                    this.Log("🌟 Infinity cube escaped (special behavior - no penalty)", EnableDebugLogs);
                }
                else
                {
                    // CUBE ESCAPE PROCESSING: Notify Wave system of cube escape
                    // Wave handles escape counting and failure conditions
                    WaveManager waveManager = FindObjectOfType<WaveManager>();
                    if (waveManager != null)
                    {
                        waveManager.OnNonBlackCubeProcessed(effectiveType, false);
                        waveManager.OnCubeEscaped(effectiveType); // Wave handles escape logic
                    }
                    
                    // Fire escaped event for general game systems (audio, effects, statistics)
                    GameEvents.FireCubeEscaped(position, effectiveType);
                    this.Log($"🔥 Fired GameEvents.OnCubeEscaped for {effectiveType} cube at ({position.x}, {position.y})", EnableDebugLogs);
                    
                    this.Log($"❌ CUBE ESCAPED: {effectiveType} cube has left the play area", EnableDebugLogs);
                }

                // [POC] Spawn simple escape visual effect
                SpawnEscapeEffect();
                
                // Destroy the escaped cube
                Destroy(gameObject);
                return false; // Cube has escaped - movement chain broken
            }
        }

        Vector2Int oldPosition = position;
        position.y -= 1;
        moveCount++;

        // Fire move event
        GameEvents.FireCubeMove(oldPosition, position, type);
        
        RotateFaceMapping();
        ProcessFaceDurations();
        UpdateFaceRotationTracking(); // Enhanced face rotation tracking

        oldPosition = new Vector2Int(position.x, position.y + 1); // Previous position for face painting
        
        this.Log($"Cube moved to ({position.x}, {position.y}), move count: {moveCount}", EnableDebugLogs);

        // Notify FacePaintingManager of movement
        FacePaintingManager facePaintingManager = FindObjectOfType<FacePaintingManager>();
        if (facePaintingManager != null)
        {
            facePaintingManager.OnCubeMoved(this, oldPosition, position);
        }

        StartCoroutine(AnimateMove(position));

        if (position.y >= 0 && position.x >= 0 && position.x < grid.Width)
        {
            Tile landingTile = grid.tiles[position.x, position.y];
            if (landingTile != null && !isDestroyed)
            {
                landingTile.HandleCubeLanding(this);
            }
        }

        return true;
    }

    private IEnumerator AnimateMove(Vector2Int newPos)
    {
        isMoving = true;

        Vector3 start = transform.position;
        Vector3 end = grid.GridToWorldPosition(newPos.x, newPos.y, 2f);

        this.Log($"Animating cube from {start} to {end} (grid pos {newPos})", EnableDebugLogs);

        WaveManager waveManager = FindObjectOfType<WaveManager>();
        float actualMoveDuration = moveDuration;

        if (waveManager != null)
        {
            float currentInterval = waveManager.isSpeedingUp ? waveManager.fastMoveInterval :
                                   (waveManager.CurrentWave?.moveInterval ?? waveManager.normalMoveInterval);
            actualMoveDuration = Mathf.Min(moveDuration, currentInterval * 0.8f);
        }

        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(-90f, 0f, 0f);

        while (elapsed < actualMoveDuration)
        {
            if (isDestroyed) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / actualMoveDuration);

            transform.position = Vector3.Lerp(start, end, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);

            yield return null;
        }

        if (!isDestroyed)
        {
            transform.position = end;
            transform.rotation = Quaternion.identity;
            PlayLandingSound();
        }

        if (isDestroyed) yield break;

        transform.localScale = new Vector3(tileSize * 1.05f, tileSize * 0.9f, tileSize * 1.05f);

        float squashTime = Mathf.Min(squashDuration, actualMoveDuration * 0.3f);
        yield return new WaitForSeconds(squashTime);

        if (isDestroyed) yield break;

        transform.localScale = new Vector3(tileSize, tileSize, tileSize);

        if (grid != null && newPos.x >= 0 && newPos.x < grid.Width &&
            newPos.y >= 0 && newPos.y < grid.Height)
        {
            Tile tile = grid.tiles[newPos.x, newPos.y];
            if (tile != null && tile.HasMarker)
            {
                tile.ProcessCubeInteraction(this);
            }
        }

        isMoving = false;
    }


    #region Face Painting System - FIXED

    public CubeFace GetCurrentDownFace()
    {
        return currentFaceMapping[0];
    }

    public FaceStatus GetActiveFaceStatus()
    {
        CubeFace downFace = GetCurrentDownFace();
        return faceStatuses[(int)downFace];
    }

    public bool HasActiveFaceStatus(FaceStatus status)
    {
        return GetActiveFaceStatus() == status;
    }

    public CubeType GetEffectiveType()
    {
        FaceStatus activeStatus = GetActiveFaceStatus();

        switch (activeStatus)
        {
            case FaceStatus.Corrupted:
                return CubeType.Infinity;
            default:
                return type;
        }
    }

    public bool CanBeCaptured()
    {
        FaceStatus activeStatus = GetActiveFaceStatus();

        switch (activeStatus)
        {
            case FaceStatus.Corrupted:
                return false;
            default:
                return type != CubeType.Infinity;
        }
    }

    public bool ShouldCreateDetonation()
    {
        FaceStatus activeStatus = GetActiveFaceStatus();
        return activeStatus == FaceStatus.Enhanced || type == CubeType.Prime;
    }

    public void PaintFace(CubeFace face, FaceStatus status, Color color, int duration = -1)
    {
        int faceIndex = (int)face;
        faceStatuses[faceIndex] = status;
        faceColors[faceIndex] = color;
        faceDurations[faceIndex] = duration;
        faceIndicators[faceIndex].SetActive(true);
        UpdateFaceVisuals();
        
        // Notify PlayerStatisticsManager of face painting
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnFacePainted(position, face, status);
        }
        
        this.Log($"Painted {face} of cube at ({position.x}, {position.y}) with {status} status, duration: {duration}", EnableDebugLogs);
    }

    public void PaintCurrentDownFace(FaceStatus status, Color color, int duration = -1)
    {
        CubeFace downFace = GetCurrentDownFace();
        PaintFace(downFace, status, color, duration);
    }

    private void ProcessFaceDurations()
    {
        bool anyChanged = false;
        for (int i = 0; i < 4; i++)
        {
            if (faceDurations[i] > 0)
            {
                faceDurations[i]--;
                if (faceDurations[i] == 0)
                {
                    faceStatuses[i] = FaceStatus.None;
                    faceColors[i] = Color.white;
                    anyChanged = true;
                    faceIndicators[i].SetActive(false);
                    this.Log($"Face {(CubeFace)i} paint status expired on cube at ({position.x}, {position.y})", EnableDebugLogs);
                }
            }
        }

        if (anyChanged)
        {
            UpdateFaceVisuals();
        }
    }

    private void InitializeFaceSystem()
    {
        for (int i = 0; i < 4; i++)
        {
            faceStatuses[i] = FaceStatus.None;
            faceColors[i] = Color.white;
            faceDurations[i] = 0;
        }

        if (showFaceIndicators)
        {
            CreateFaceIndicators();
        }
    }

    private void CreateFaceIndicators()
    {
        for (int i = 0; i < 4; i++)
        {
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Quad);
            indicator.name = $"FaceIndicator_{(CubeFace)i}_{position.x}_{position.y}";
            indicator.transform.SetParent(transform);

            // Position and orient the face indicator correctly
            PositionFaceIndicator(indicator, (CubeFace)i);

            // Set up renderer with proper material
            Renderer renderer = indicator.GetComponent<Renderer>();
            Material mat = CreateFaceIndicatorMaterial();
            renderer.material = mat;

            // Remove collider
            Destroy(indicator.GetComponent<Collider>());

            indicator.SetActive(false); // Hidden by default
            faceIndicators[i] = indicator;
        }
    }

    private void PositionFaceIndicator(GameObject indicator, CubeFace originalFace)
    {
        float offset = (0.55f); // Very close to cube surface, just barely hovering
        Vector3 scale = new Vector3( 1f, 1f, 1f); // Larger indicators for better visibility

        // Position based on the ORIGINAL face position on the cube
        switch (originalFace)
        {
            case CubeFace.Bottom: // Original bottom face (Y-)
                indicator.transform.localPosition = new Vector3(0, -offset, 0);
                indicator.transform.localRotation = Quaternion.Euler(90, 180, 0); // Face up (outward from bottom)
                break;

            case CubeFace.Top: // Original top face (Y+)
                indicator.transform.localPosition = new Vector3(0, offset, 0);
                indicator.transform.localRotation = Quaternion.Euler(-90, 180, 0); // Face down (outward from top)
                break;

            case CubeFace.Front: // Original front face (Z+)
                indicator.transform.localPosition = new Vector3(0, 0, offset);
                indicator.transform.localRotation = Quaternion.Euler(0, 180, 0); // Face toward camera (outward from front)
                break;

            case CubeFace.Back: // Original back face (Z-)
                indicator.transform.localPosition = new Vector3(0, 0, -offset);
                indicator.transform.localRotation = Quaternion.Euler(0, 0, 0); // Face toward camera (outward from back)
                break;
        }

        indicator.transform.localScale = scale;
    }

    private Material CreateFaceIndicatorMaterial()
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1, 1, 1, 0.8f);

        // Set up for transparency
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;

        return mat;
    }

    private void UpdateFaceVisuals()
    {

        UpdateFaceIndicatorPositions();
        
    }

    private void UpdateFaceIndicatorPositions()
    {
        if (!showFaceIndicators || faceIndicators == null) return;

        float offset = (0.55f);

        for (int i = 0; i < 4; i++)
        {
            if (faceIndicators[i] == null) continue;

            CubeFace originalFace = (CubeFace)i;

            // Find where this original face is currently positioned
            FacePosition currentPosition = GetFacePosition(originalFace);

            // Position the indicator based on the current FacePosition
            Vector3 newPosition = Vector3.one;
            Quaternion newRotation = Quaternion.identity;

            switch (currentPosition)
            {
                case FacePosition.Down:
                    newPosition = new Vector3(0, -offset, 0);
                    newRotation = Quaternion.Euler(90, 180, 0);
                    break;
                case FacePosition.Up:
                    newPosition = new Vector3(0, offset, 0);
                    newRotation = Quaternion.Euler(-270, 180, 0);
                    break;
                case FacePosition.Forward:
                    newPosition = new Vector3(0, 0, offset);
                    newRotation = Quaternion.Euler(0, 180, 0);
                    break;
                case FacePosition.Back:
                    newPosition = new Vector3(0, 0, -offset);
                    newRotation = Quaternion.Euler(0, 0, 0);
                    break;
            }

            faceIndicators[i].transform.localPosition = newPosition;
            faceIndicators[i].transform.localRotation = newRotation;
            faceIndicators[i].GetComponent<Renderer>().material.color = faceColors[i];
        }
    }

    private FacePosition GetFacePosition(CubeFace originalFace)
    {
        // Find where this original face is currently positioned
        for (int i = 0; i < 4; i++)
        {
            if (currentFaceMapping[i] == originalFace)
            {
                switch (i)
                {
                    case 0: return FacePosition.Down;
                    case 1: return FacePosition.Up;
                    case 2: return FacePosition.Forward;
                    case 3: return FacePosition.Back;
                    default: return FacePosition.Down;
                }
            }
        }
        return FacePosition.Down; // Fallback
    }
    private void EnsureFaceIndicatorOrientation(GameObject indicator, CubeFace originalFace)
    {
        if (indicator == null) return;

        // Reset the indicator's rotation to always face outward from its assigned face
        // This ensures that no matter how the cube has rotated, the painted surface is visible
        switch (originalFace)
        {
            case CubeFace.Bottom:
                // For bottom face, the quad should face upward (away from cube bottom)
                indicator.transform.localRotation = Quaternion.Euler(90, 180, 0);
                break;

            case CubeFace.Top:
                // For top face, the quad should face downward (away from cube top)
                indicator.transform.localRotation = Quaternion.Euler(-90, 180, 0);
                break;

            case CubeFace.Front:
                // For front face, the quad should face forward (away from cube front)
                indicator.transform.localRotation = Quaternion.Euler(0, 180, 0);
                break;

            case CubeFace.Back:
                // For back face, the quad should face backward (away from cube back)
                indicator.transform.localRotation = Quaternion.Euler(0, 0, 0);
                break;
        }
    }

    private System.Collections.IEnumerator PulseFaceIndicator(GameObject indicator, int faceIndex)
    {
        if (indicator == null || isDestroyed) yield break;

        // Only pulse if this is still the active face
        CubeFace activeFace = GetCurrentDownFace();
        if ((int)activeFace != faceIndex) yield break;

        Vector3 originalScale = indicator.transform.localScale;
        Vector3 pulseScale = originalScale * 1.2f;

        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration && indicator != null && indicator.activeInHierarchy && !isDestroyed)
        {
            // Check if still the active face
            if ((int)GetCurrentDownFace() != faceIndex) break;

            elapsed += Time.deltaTime;
            float t = Mathf.PingPong(elapsed * 2f, 1f); // Pulse twice per duration
            if (indicator != null)
            {
                indicator.transform.localScale = Vector3.Lerp(originalScale, pulseScale, t * 0.3f);
            }
            yield return null;
        }

        if (indicator != null && !isDestroyed)
        {
            indicator.transform.localScale = originalScale;
        }
    }

    // Debug and testing methods
    public void TestPaintFace(CubeFace face, FaceStatus status)
    {
        Color color = status == FaceStatus.Corrupted ? Color.black : Color.blue;
        PaintFace(face, status, color, 5);
        this.Log($"Test painted {face} with {status} status", EnableDebugLogs);
    }

    public void DebugShowAllFaces()
    {
        if (!showFaceIndicators) return;

        Color[] testColors = { Color.red, Color.green, Color.blue, Color.yellow };
        FaceStatus[] testStatuses = { FaceStatus.Corrupted, FaceStatus.Enhanced, FaceStatus.Corrupted, FaceStatus.Enhanced };

        for (int i = 0; i < 4; i++)
        {
            PaintFace((CubeFace)i, testStatuses[i], testColors[i], -1);
        }

        this.Log($"Debug: All faces painted with different colors. Current down face: {GetCurrentDownFace()}", EnableDebugLogs);
    }

    public void DebugPrintFaceMapping()
    {
        this.Log($"Face Mapping for cube at ({position.x}, {position.y}):", EnableDebugLogs);
        this.Log($"  Bottom position: {currentFaceMapping[0]}", EnableDebugLogs);
        this.Log($"  Top position: {currentFaceMapping[1]}", EnableDebugLogs);
        this.Log($"  Front position: {currentFaceMapping[2]}", EnableDebugLogs);
        this.Log($"  Back position: {currentFaceMapping[3]}", EnableDebugLogs);
        this.Log($"  Current down face: {GetCurrentDownFace()}", EnableDebugLogs);
        this.Log($"  Active face status: {GetActiveFaceStatus()}", EnableDebugLogs);
    }

    private void InitializeFaceMapping()
    {
        // Initially, faces are in their original positions
        currentFaceMapping[0] = CubeFace.Bottom;  // Bottom position has original bottom face
        currentFaceMapping[1] = CubeFace.Top;     // Top position has original top face  
        currentFaceMapping[2] = CubeFace.Front;   // Front position has original front face
        currentFaceMapping[3] = CubeFace.Back;    // Back position has original back face

        this.Log($"Face mapping initialized for cube at ({position.x}, {position.y})", EnableDebugLogs);
    }

    private void RotateFaceMapping()
    {
        // Forward roll rotation: Bottom->Front, Front->Top, Top->Back, Back->Bottom
        CubeFace temp = currentFaceMapping[0]; // Store current bottom
        currentFaceMapping[0] = currentFaceMapping[3]; // Back moves to Bottom
        currentFaceMapping[3] = currentFaceMapping[1]; // Top moves to Back  
        currentFaceMapping[1] = currentFaceMapping[2]; // Front moves to Top
        currentFaceMapping[2] = temp;                  // Bottom moves to Front

        this.Log($"Face mapping rotated: Bottom={currentFaceMapping[0]}, Top={currentFaceMapping[1]}, Front={currentFaceMapping[2]}, Back={currentFaceMapping[3]}", EnableDebugLogs);

        // Update visuals immediately after rotation to ensure proper orientation
        UpdateFaceVisuals();
        
        // Enhanced face rotation tracking for corruption mechanics
        UpdateFaceRotationTracking();
    }

    // Public methods for external testing
    public void SetFaceStatus(CubeFace face, FaceStatus status, int duration = -1)
    {
        Color color = status == FaceStatus.Corrupted ? Color.red :
                     status == FaceStatus.Enhanced ? Color.blue : Color.white;
        PaintFace(face, status, color, duration);
    }

    public FaceStatus GetFaceStatus(CubeFace face)
    {
        return faceStatuses[(int)face];
    }

    public int GetFaceDuration(CubeFace face)
    {
        return faceDurations[(int)face];
    }

    public void ClearAllFaces()
    {
        for (int i = 0; i < 4; i++)
        {
            faceStatuses[i] = FaceStatus.None;
            faceColors[i] = Color.white;
            faceDurations[i] = 0;
        }
        UpdateFaceVisuals();
        this.Log($"Cleared all face statuses on cube at ({position.x}, {position.y})", EnableDebugLogs);
    }

    #endregion

    #region Marker Interaction and Corruption System

    /// <summary>
    /// Called when a marker hits this cube. Handles infinity cube face painting.
    /// </summary>
    public void OnMarkerHit()
    {
        if (type == CubeType.Infinity)
        {
            CubeFace topFace = GetTopFace();
            PaintFace(topFace, FaceStatus.Corrupted, Color.black, -1);
            CreateMarkerHitEffect();
            this.Log($"Infinity cube at ({position.x}, {position.y}) hit by marker - top face painted for corruption", EnableDebugLogs);
        }
    }

    /// <summary>
    /// Gets the current top face of the cube based on face mapping
    /// </summary>
    /// <returns>The face currently positioned at the top</returns>
    public CubeFace GetTopFace()
    {
        return currentFaceMapping[1]; // Index 1 is the top position
    }

    /// <summary>
    /// Creates visual effect when marker hits cube
    /// </summary>
    private void CreateMarkerHitEffect()
    {
        // Create a temporary visual effect for marker hit
        GameObject hitEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        hitEffect.name = "MarkerHitEffect";
        hitEffect.transform.position = transform.position + Vector3.up * 0.6f;
        hitEffect.transform.localScale = Vector3.one * 0.3f;
        
        // Set up the effect material
        Renderer renderer = hitEffect.GetComponent<Renderer>();
        Material effectMaterial = new Material(Shader.Find("Standard"));
        effectMaterial.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange color
        effectMaterial.SetFloat("_Mode", 3); // Transparent mode
        effectMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        effectMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        effectMaterial.SetInt("_ZWrite", 0);
        effectMaterial.EnableKeyword("_ALPHABLEND_ON");
        effectMaterial.renderQueue = 3000;
        renderer.material = effectMaterial;
        
        // Remove collider to avoid interference
        Destroy(hitEffect.GetComponent<Collider>());
        
        // Destroy the effect after a short time
        Destroy(hitEffect, 1f);
    }

    /// <summary>
    /// Checks if this cube should trigger corruption based on its current state
    /// </summary>
    public void CheckForCorruption()
    {
        if (type == CubeType.Infinity && HasCorruptedDownFace())
        {
            // Coordinate with tile for corruption
            Vector2Int currentPos = position;
            if (grid != null && currentPos.x >= 0 && currentPos.x < grid.Width && 
                currentPos.y >= 0 && currentPos.y < grid.Height)
            {
                Tile currentTile = grid.tiles[currentPos.x, currentPos.y];
                if (currentTile != null)
                {
                    // Signal to tile that corruption should occur
                    // This provides the coordination point for the Tile.cs task
                    PrepareCorruptionForTile(currentTile);
                }
            }
        }
    }

    /// <summary>
    /// Checks if the current down face has corrupted status
    /// </summary>
    /// <returns>True if the down face is corrupted</returns>
    private bool HasCorruptedDownFace()
    {
        CubeFace downFace = GetCurrentDownFace();
        return faceStatuses[(int)downFace] == FaceStatus.Corrupted;
    }

    /// <summary>
    /// Prepares corruption coordination with tile system
    /// </summary>
    /// <param name="tile">The tile to coordinate corruption with</param>
    private void PrepareCorruptionForTile(Tile tile)
    {
        // This method provides the coordination point for tile corruption
        // The actual tile corruption logic will be implemented in the Tile.cs task
        this.Log($"Infinity cube at ({position.x}, {position.y}) preparing corruption for tile - ready for Tile.cs implementation", EnableDebugLogs);
        
        // Mark cube as having triggered corruption
        // Additional corruption state tracking can be added here if needed
    }

    /// <summary>
    /// Enhanced face rotation tracking for corruption mechanics
    /// </summary>
    private void UpdateFaceRotationTracking()
    {
        // Enhanced tracking of face rotation for corruption preparation
        // This ensures accurate face state during cube movement
        if (type == CubeType.Infinity)
        {
            // Track which face was painted and is now in the down position
            CheckForCorruption();
        }
    }

    #endregion

    #region IManagerDebugInterface Implementation

    public bool EnableDebugLogs { get; set; } = true;

    /// <summary>
    /// [POC] Spawns a simple visual effect when cube escapes
    /// </summary>
    private void SpawnEscapeEffect()
    {
        
        this.Log("[POC] Spawned escape visual effect", EnableDebugLogs);
    }
    
    public string GetDebugStatus()
    {
        string status = isDestroyed ? "DESTROYED" : (isMoving ? "MOVING" : "IDLE");
        string effectiveType = GetEffectiveType().ToString();
        string audioStatus = IsAudioSystemReady() ? "AudioOK" : "NoAudio";
        return $"Cube {type}->{effectiveType}: @({position.x},{position.y}) HP:{currentHitPoints}/{maxHitPoints} ({status}) Face:{GetCurrentDownFace()} Audio:{audioStatus}";
    }

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Cube Type"] = type.ToString(),
            ["Effective Type"] = GetEffectiveType().ToString(),
            ["Position"] = $"({position.x}, {position.y})",
            ["World Position"] = transform.position,
            ["Level"] = level,
            ["Hit Points"] = $"{currentHitPoints}/{maxHitPoints}",
            ["Move Count"] = moveCount,
            ["Is Moving"] = isMoving,
            ["Is Destroyed"] = isDestroyed,
            ["Is Raining Cube"] = isRainingCube,
            ["Move Count Remaining"] = moveCountRemaining,
            ["Current Down Face"] = GetCurrentDownFace(),
            ["Active Face Status"] = GetActiveFaceStatus(),
            ["Can Be Captured"] = CanBeCaptured(),
            ["Should Create Detonation"] = ShouldCreateDetonation(),
            ["Use Physics"] = usePhysics,
            ["Show Face Indicators"] = showFaceIndicators,
            ["Material Assigned"] = material != null,
            ["Prefab Assigned"] = prefab != null,
            ["Move Duration"] = moveDuration,
            ["Squash Duration"] = squashDuration,
            ["Rain Speed"] = rainSpeed,
            ["Rain Height"] = rainHeight,
            ["Target Row"] = targetRow,
            ["Tile Size"] = tileSize,
            
            // Audio system debug information
            ["Audio System Ready"] = IsAudioSystemReady(),
            ["AudioSource Configured"] = cubeAudioSource != null,
            ["CubeAudioConfig Assigned"] = cubeAudioConfig != null,
            ["Audio Currently Playing"] = cubeAudioSource?.isPlaying ?? false,
            ["Audio Volume"] = cubeAudioSource?.volume ?? 0f,
            ["Audio Pitch"] = cubeAudioSource?.pitch ?? 1f
        };
    }

    public void ResetToDefaults()
    {
        // Reset cube state to initial values
        isMoving = false;
        isDestroyed = false;
        moveCount = 0;
        moveCountRemaining = 0;
        isRainingCube = false;
        targetRow = -1;
        
        // Reset health to maximum
        currentHitPoints = maxHitPoints;
        
        // Clear all face paintings
        ClearAllFaces();
        
        // Reset physics state
        if (cubeRigidbody != null)
        {
            cubeRigidbody.velocity = Vector3.zero;
            cubeRigidbody.angularVelocity = Vector3.zero;
        }
        
        // Reset scale and rotation
        transform.localScale = new Vector3(tileSize, tileSize, tileSize);
        transform.rotation = Quaternion.identity;
        
        // Reset audio system
        if (cubeAudioSource != null)
        {
            cubeAudioSource.Stop();
            cubeAudioSource.clip = null;
            ConfigureCubeAudioSource(); // Reset to default audio settings
        }
        
        // Reset face mapping to original state
        InitializeFaceMapping();
        
        // Update visuals
        UpdateDamageVisual();
        UpdateFaceVisuals();
        
        if (EnableDebugLogs)
            this.Log($"Cube at ({position.x}, {position.y}) reset to defaults", EnableDebugLogs);
    }

    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading for cube settings
        if (EnableDebugLogs)
            this.Log($"Loading configuration: {configName} (not yet implemented)", EnableDebugLogs);
    }

    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving for cube settings
        if (EnableDebugLogs)
            this.Log($"Saving configuration: {configName} (not yet implemented)", EnableDebugLogs);
    }

    #endregion
}
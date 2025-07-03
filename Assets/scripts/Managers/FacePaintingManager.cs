using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Centralizes face painting coordination between tiles and cubes.
/// Manages face painting patterns, batch operations, and preview functionality.
/// </summary>
public class FacePaintingManager : MonoBehaviour, IManagerDebugInterface
{
    #region Inspector Configuration
    [Header("Face Painting Settings")]
    [SerializeField] private bool enableFacePainting = true;
    [SerializeField] private bool showPreviewEffects = true;
    [SerializeField] private Color defaultPaintColor = Color.red;
    [SerializeField] private int defaultPaintDuration = 3;

    [Header("Pattern Settings")]
    [SerializeField] private bool enablePatternPreviews = true;
    [SerializeField] private Material previewMaterial;
    [SerializeField] private float previewAlpha = 0.5f;

    #endregion

    #region Runtime State
    private GridManager gridManager;
    private List<FacePaintingPattern> activePatterns = new List<FacePaintingPattern>();
    private Dictionary<Vector2Int, FacePaintingPreview> activePreviews = new Dictionary<Vector2Int, FacePaintingPreview>();
    
    // Coordination tracking
    private Dictionary<Vector2Int, CubeManager> tilesWithCubes = new Dictionary<Vector2Int, CubeManager>();
    private List<Tile> facePaintingTiles = new List<Tile>();
    #endregion

    #region Properties
    public static FacePaintingManager Instance { get; private set; }
    public bool IsFacePaintingEnabled => enableFacePainting;
    public int ActivePatternsCount => activePatterns.Count;
    public int FacePaintingTilesCount => facePaintingTiles.Count;

    // IManagerDebugInterface Properties
    public bool EnableDebugLogs { get; set; } = true;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
        InitializePreviewMaterial();
        EnableDebugLogs = true;
    }

    private void Start()
    {
        InitializeManager();
    }

    private void OnDestroy()
    {
        CleanupManager();
    }
    #endregion

    #region Initialization
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning("Multiple FacePaintingManagers found! Destroying duplicate.");
            Destroy(gameObject);
            return;
        }
    }

    private void InitializeManager()
    {
        // Get reference to GridManager
        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("FacePaintingManager requires GridManager to function!");
            enabled = false;
            return;
        }

        DebugLog("FacePaintingManager initialized successfully");
    }

    private void InitializePreviewMaterial()
    {
        if (previewMaterial == null)
        {
            previewMaterial = new Material(Shader.Find("Standard"));
            previewMaterial.color = new Color(defaultPaintColor.r, defaultPaintColor.g, defaultPaintColor.b, previewAlpha);
            previewMaterial.SetFloat("_Mode", 3); // Transparent mode
            previewMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            previewMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            previewMaterial.SetInt("_ZWrite", 0);
            previewMaterial.DisableKeyword("_ALPHATEST_ON");
            previewMaterial.EnableKeyword("_ALPHABLEND_ON");
            previewMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            previewMaterial.renderQueue = 3000;
        }
    }

    private void CleanupManager()
    {
        ClearAllPreviews();
        activePatterns.Clear();
        facePaintingTiles.Clear();
        tilesWithCubes.Clear();
    }
    #endregion

    #region Core Face Painting Coordination
    /// <summary>
    /// Registers a tile as capable of face painting
    /// </summary>
    public void RegisterFacePaintingTile(Tile tile)
    {
        if (tile == null || facePaintingTiles.Contains(tile)) return;

        facePaintingTiles.Add(tile);
        DebugLog($"Registered face painting tile at ({tile.x}, {tile.y})");
    }

    /// <summary>
    /// Unregisters a tile from face painting
    /// </summary>
    public void UnregisterFacePaintingTile(Tile tile)
    {
        if (tile == null) return;

        facePaintingTiles.Remove(tile);
        Vector2Int pos = new Vector2Int(tile.x, tile.y);
        RemovePreview(pos);
        DebugLog($"Unregistered face painting tile at ({tile.x}, {tile.y})");
    }

    /// <summary>
    /// Coordinates face painting between a tile and cube
    /// </summary>
    public void CoordinateFacePainting(Tile tile, CubeManager cube, FaceStatus status, Color color, int duration = -1)
    {
        if (!enableFacePainting || tile == null || cube == null) return;

        // Update tracking
        Vector2Int pos = new Vector2Int(tile.x, tile.y);
        tilesWithCubes[pos] = cube;

        // Coordinate between tile setup and cube painting
        tile.SetupFacePainting(status, color, duration);
        
        // Paint the cube's current down face
        cube.PaintCurrentDownFace(status, color, duration);

        // Create visual effect
        CreatePaintingEffect(pos, color);

        DebugLog($"Coordinated face painting at ({tile.x}, {tile.y}): {status} status");
    }

    /// <summary>
    /// Removes cube tracking when cube leaves or is destroyed
    /// </summary>
    public void OnCubeLeft(Vector2Int position)
    {
        tilesWithCubes.Remove(position);
        DebugLog($"Cube left position ({position.x}, {position.y})");
    }

    /// <summary>
    /// Updates cube tracking when cube moves
    /// </summary>
    public void OnCubeMoved(CubeManager cube, Vector2Int oldPos, Vector2Int newPos)
    {
        if (cube == null) return;

        tilesWithCubes.Remove(oldPos);
        tilesWithCubes[newPos] = cube;
        DebugLog($"Tracked cube movement from ({oldPos.x}, {oldPos.y}) to ({newPos.x}, {newPos.y})");
    }
    #endregion

    #region Pattern Management
    /// <summary>
    /// Applies a face painting pattern to multiple tiles
    /// </summary>
    public void ApplyFacePaintingPattern(FacePaintingPattern pattern)
    {
        if (pattern == null || !enableFacePainting) return;

        foreach (var entry in pattern.entries)
        {
            Vector2Int pos = pattern.basePosition + entry.offset;
            Tile tile = gridManager.GetTileAt(pos);
            
            if (tile != null && tile.IsPlayable)
            {
                tile.SetupFacePainting(entry.status, entry.color, entry.duration, entry.paintOnLanding, entry.paintOnExit);
                RegisterFacePaintingTile(tile);
            }
        }

        activePatterns.Add(pattern);
        DebugLog($"Applied face painting pattern '{pattern.name}' with {pattern.entries.Count} entries");
    }

    /// <summary>
    /// Applies face painting to multiple tiles with the same settings
    /// </summary>
    public void BatchSetFacePainting(List<Vector2Int> positions, FaceStatus status, Color color, int duration = -1, bool paintOnLanding = true, bool paintOnExit = false)
    {
        if (!enableFacePainting || positions == null) return;

        int appliedCount = 0;
        foreach (var pos in positions)
        {
            Tile tile = gridManager.GetTileAt(pos);
            if (tile != null && tile.IsPlayable)
            {
                tile.SetupFacePainting(status, color, duration, paintOnLanding, paintOnExit);
                RegisterFacePaintingTile(tile);
                appliedCount++;
            }
        }

        DebugLog($"Batch applied face painting to {appliedCount}/{positions.Count} tiles with {status} status");
    }

    /// <summary>
    /// Clears face painting from all registered tiles
    /// </summary>
    public void ClearAllFacePainting()
    {
        foreach (var tile in facePaintingTiles.ToArray())
        {
            if (tile != null)
            {
                tile.DisableFacePainting();
            }
        }

        facePaintingTiles.Clear();
        activePatterns.Clear();
        ClearAllPreviews();
        tilesWithCubes.Clear();

        DebugLog("Cleared all face painting settings");
    }

    /// <summary>
    /// Creates a face painting pattern from current tile states
    /// </summary>
    public FacePaintingPattern CreatePatternFromTiles(string patternName, Vector2Int basePosition, List<Vector2Int> positions)
    {
        FacePaintingPattern pattern = new FacePaintingPattern
        {
            name = patternName,
            basePosition = basePosition,
            entries = new List<FacePaintingEntry>()
        };

        foreach (var pos in positions)
        {
            Tile tile = gridManager.GetTileAt(pos);
            if (tile != null && tile.CanPaintCubes)
            {
                pattern.entries.Add(new FacePaintingEntry
                {
                    offset = pos - basePosition,
                    status = tile.PaintStatus,
                    color = tile.PaintColor,
                    duration = tile.PaintDuration,
                    paintOnLanding = true,
                    paintOnExit = false
                });
            }
        }

        DebugLog($"Created pattern '{patternName}' with {pattern.entries.Count} entries");
        return pattern;
    }
    #endregion

    #region Preview System
    /// <summary>
    /// Shows preview of face painting pattern
    /// </summary>
    public void ShowPatternPreview(FacePaintingPattern pattern)
    {
        if (!enablePatternPreviews || pattern == null) return;

        ClearAllPreviews();

        foreach (var entry in pattern.entries)
        {
            Vector2Int pos = pattern.basePosition + entry.offset;
            ShowPreview(pos, entry.color);
        }

        DebugLog($"Showing preview for pattern '{pattern.name}'");
    }

    /// <summary>
    /// Shows preview for batch face painting
    /// </summary>
    public void ShowBatchPreview(List<Vector2Int> positions, Color color)
    {
        if (!enablePatternPreviews || positions == null) return;

        ClearAllPreviews();

        foreach (var pos in positions)
        {
            ShowPreview(pos, color);
        }

        DebugLog($"Showing batch preview for {positions.Count} positions");
    }

    private void ShowPreview(Vector2Int position, Color color)
    {
        Tile tile = gridManager.GetTileAt(position);
        if (tile == null || !tile.IsPlayable) return;

        // Create preview object
        GameObject previewObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        previewObj.name = $"FacePaintPreview_{position.x}_{position.y}";
        previewObj.transform.SetParent(tile.transform);
        previewObj.transform.localPosition = new Vector3(0, 0.52f, 0); // Just above tile
        previewObj.transform.localRotation = Quaternion.Euler(90, 0, 0);
        previewObj.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        // Remove collider
        Destroy(previewObj.GetComponent<Collider>());

        // Apply preview material with color
        Renderer renderer = previewObj.GetComponent<Renderer>();
        Material mat = new Material(previewMaterial);
        mat.color = new Color(color.r, color.g, color.b, previewAlpha);
        renderer.material = mat;

        // Store preview
        FacePaintingPreview preview = new FacePaintingPreview
        {
            gameObject = previewObj,
            position = position,
            color = color
        };

        activePreviews[position] = preview;
    }

    /// <summary>
    /// Clears all preview objects
    /// </summary>
    public void ClearAllPreviews()
    {
        foreach (var preview in activePreviews.Values)
        {
            if (preview.gameObject != null)
            {
                Destroy(preview.gameObject);
            }
        }
        activePreviews.Clear();
    }

    private void RemovePreview(Vector2Int position)
    {
        if (activePreviews.TryGetValue(position, out FacePaintingPreview preview))
        {
            if (preview.gameObject != null)
            {
                Destroy(preview.gameObject);
            }
            activePreviews.Remove(position);
        }
    }
    #endregion

    #region Quick Setup Methods
    /// <summary>
    /// Quick setup for corruption painting (black face)
    /// </summary>
    public void SetupCorruptionPattern(List<Vector2Int> positions, int duration = 3)
    {
        BatchSetFacePainting(positions, FaceStatus.Corrupted, Color.black, duration);
    }

    /// <summary>
    /// Quick setup for enhancement painting (blue face)
    /// </summary>
    public void SetupEnhancementPattern(List<Vector2Int> positions, int duration = 3)
    {
        BatchSetFacePainting(positions, FaceStatus.Enhanced, Color.blue, duration);
    }

    /// <summary>
    /// Sets up a single tile for face painting
    /// </summary>
    public void SetupSingleTilePainting(Vector2Int position, FaceStatus status, Color color, int duration = -1)
    {
        Tile tile = gridManager.GetTileAt(position);
        if (tile != null && tile.IsPlayable)
        {
            tile.SetupFacePainting(status, color, duration);
            RegisterFacePaintingTile(tile);
            CreatePaintingEffect(position, color);
            DebugLog($"Set up single tile face painting at ({position.x}, {position.y})");
        }
    }
    #endregion

    #region Visual Effects
    private void CreatePaintingEffect(Vector2Int position, Color color)
    {
        if (!showPreviewEffects) return;

        Vector3 worldPos = gridManager.GridToWorldPosition(position.x, position.y, 0.5f);
        
        // Create simple effect object
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "FacePaintingEffect";
        effect.transform.position = worldPos;
        effect.transform.localScale = Vector3.one * 0.2f;

        // Remove collider
        Destroy(effect.GetComponent<Collider>());

        // Apply effect material
        Renderer renderer = effect.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 0.5f);
        renderer.material = mat;

        // Animate and destroy
        StartCoroutine(AnimatePaintingEffect(effect));
    }

    private System.Collections.IEnumerator AnimatePaintingEffect(GameObject effect)
    {
        float duration = 1f;
        float elapsed = 0f;
        Vector3 startScale = effect.transform.localScale;
        Vector3 startPos = effect.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Scale up and move up slightly
            effect.transform.localScale = Vector3.Lerp(startScale, startScale * 3f, t);
            effect.transform.position = Vector3.Lerp(startPos, startPos + Vector3.up * 0.5f, t);

            // Fade material
            Renderer renderer = effect.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = 1f - t;
                renderer.material.color = color;
            }

            yield return null;
        }

        Destroy(effect);
    }
    #endregion

    #region State Queries
    /// <summary>
    /// Gets all tiles currently set up for face painting
    /// </summary>
    public List<Tile> GetFacePaintingTiles()
    {
        return new List<Tile>(facePaintingTiles);
    }

    /// <summary>
    /// Gets cube at specific position if tracked
    /// </summary>
    public CubeManager GetCubeAtPosition(Vector2Int position)
    {
        return tilesWithCubes.TryGetValue(position, out CubeManager cube) ? cube : null;
    }

    /// <summary>
    /// Checks if position has face painting enabled
    /// </summary>
    public bool HasFacePaintingAt(Vector2Int position)
    {
        Tile tile = gridManager.GetTileAt(position);
        return tile != null && tile.CanPaintCubes;
    }

    /// <summary>
    /// Gets face painting status at position
    /// </summary>
    public FaceStatus GetFacePaintingStatusAt(Vector2Int position)
    {
        Tile tile = gridManager.GetTileAt(position);
        return tile != null ? tile.PaintStatus : FaceStatus.None;
    }
    #endregion

    #region Enhanced Debug Interface (IManagerDebugInterface)
    
    /// <summary>
    /// Enhanced debug status output including pattern details and tile states
    /// </summary>
    public void DebugPrintStatus()
    {
        DebugLog("=== FACE PAINTING MANAGER STATUS ===");
        DebugLog($"Face painting enabled: {enableFacePainting}");
        DebugLog($"Active patterns: {activePatterns.Count}");
        DebugLog($"Face painting tiles: {facePaintingTiles.Count}");
        DebugLog($"Tracked cubes: {tilesWithCubes.Count}");
        DebugLog($"Active previews: {activePreviews.Count}");
        DebugLog($"Preview effects enabled: {showPreviewEffects}");
        DebugLog($"Pattern previews enabled: {enablePatternPreviews}");
        
        // Pattern details
        if (activePatterns.Count > 0)
        {
            DebugLog("--- Active Patterns ---");
            for (int i = 0; i < activePatterns.Count; i++)
            {
                var pattern = activePatterns[i];
                DebugLog($"  [{i}] '{pattern.name}' at ({pattern.basePosition.x}, {pattern.basePosition.y}) with {pattern.entries.Count} entries");
            }
        }
        
        // Tile details
        if (facePaintingTiles.Count > 0)
        {
            DebugLog("--- Face Painting Tiles ---");
            foreach (var tile in facePaintingTiles)
            {
                if (tile != null)
                {
                    DebugLog($"  Tile ({tile.x}, {tile.y}): {tile.PaintStatus} - {tile.PaintColor} (duration: {tile.PaintDuration})");
                }
            }
        }
    }
    
    /// <summary>
    /// Gets human-readable debug status string
    /// </summary>
    public string GetDebugStatus()
    {
        return $"FacePaintingManager: {(enableFacePainting ? "ENABLED" : "DISABLED")} | " +
               $"Patterns: {activePatterns.Count} | " +
               $"Tiles: {facePaintingTiles.Count} | " +
               $"Cubes: {tilesWithCubes.Count} | " +
               $"Previews: {activePreviews.Count}";
    }
    
    /// <summary>
    /// Gets structured debug data for debug panels
    /// </summary>
    public Dictionary<string, object> GetDebugData()
    {
        var debugData = new Dictionary<string, object>
        {
            ["Manager Enabled"] = enableFacePainting,
            ["Debug Logs Enabled"] = EnableDebugLogs,
            ["Show Preview Effects"] = showPreviewEffects,
            ["Enable Pattern Previews"] = enablePatternPreviews,
            ["Default Paint Color"] = defaultPaintColor,
            ["Default Paint Duration"] = defaultPaintDuration,
            ["Preview Alpha"] = previewAlpha,
            ["Active Patterns Count"] = activePatterns.Count,
            ["Face Painting Tiles Count"] = facePaintingTiles.Count,
            ["Tracked Cubes Count"] = tilesWithCubes.Count,
            ["Active Previews Count"] = activePreviews.Count
        };
        
        // Pattern data
        if (activePatterns.Count > 0)
        {
            var patternNames = new List<string>();
            var patternEntries = new List<int>();
            foreach (var pattern in activePatterns)
            {
                patternNames.Add(pattern.name);
                patternEntries.Add(pattern.entries.Count);
            }
            debugData["Pattern Names"] = patternNames;
            debugData["Pattern Entry Counts"] = patternEntries;
        }
        
        // Tile status breakdown
        var statusCounts = new Dictionary<string, int>();
        foreach (var tile in facePaintingTiles)
        {
            if (tile != null)
            {
                string status = tile.PaintStatus.ToString();
                statusCounts[status] = statusCounts.GetValueOrDefault(status, 0) + 1;
            }
        }
        debugData["Tile Status Counts"] = statusCounts;
        
        return debugData;
    }
    
    /// <summary>
    /// Resets all patterns and settings to defaults
    /// </summary>
    public void ResetToDefaults()
    {
        DebugLog("Resetting FacePaintingManager to defaults...");
        
        // Clear all active state
        ClearAllFacePainting();
        
        // Reset to default values
        enableFacePainting = true;
        showPreviewEffects = true;
        defaultPaintColor = Color.red;
        defaultPaintDuration = 3;
        enablePatternPreviews = true;
        previewAlpha = 0.5f;
        EnableDebugLogs = true;
        
        // Reinitialize preview material with defaults
        InitializePreviewMaterial();
        
        DebugLog("Reset to defaults completed");
    }
    
    /// <summary>
    /// Loads configuration - placeholder for future ScriptableObject integration
    /// </summary>
    public void LoadConfiguration(string configName)
    {
        DebugLog($"LoadConfiguration called with '{configName}' - not yet implemented (future ScriptableObject integration)");
        // Future implementation will load from ScriptableObject or JSON
        // For now, provide feedback that this is a future feature
    }
    
    /// <summary>
    /// Saves configuration - placeholder for future ScriptableObject integration
    /// </summary>
    public void SaveConfiguration(string configName)
    {
        DebugLog($"SaveConfiguration called with '{configName}' - not yet implemented (future ScriptableObject integration)");
        // Future implementation will save to ScriptableObject or JSON
        // For now, provide feedback that this is a future feature
    }
    
    #endregion
    
    #region Batch Debug Operations
    
    /// <summary>
    /// Resets all active patterns to their default states
    /// </summary>
    public void ResetAllPatternsToDefaults()
    {
        DebugLog("Resetting all patterns to defaults...");
        
        int resetCount = 0;
        foreach (var pattern in activePatterns.ToArray())
        {
            if (pattern != null)
            {
                // Reset each pattern entry to default values
                foreach (var entry in pattern.entries)
                {
                    entry.status = FaceStatus.None;
                    entry.color = defaultPaintColor;
                    entry.duration = defaultPaintDuration;
                    entry.paintOnLanding = true;
                    entry.paintOnExit = false;
                }
                resetCount++;
            }
        }
        
        // Reapply patterns with default values
        var patternsToReapply = new List<FacePaintingPattern>(activePatterns);
        ClearAllFacePainting();
        
        foreach (var pattern in patternsToReapply)
        {
            ApplyFacePaintingPattern(pattern);
        }
        
        DebugLog($"Reset {resetCount} patterns to default values");
    }
    
    /// <summary>
    /// Completely clears and reloads the face painting system
    /// </summary>
    public void ClearAndReloadFacePaintingSystem()
    {
        DebugLog("Clearing and reloading face painting system...");
        
        // Store current configuration
        bool wasEnabled = enableFacePainting;
        var storedPatterns = new List<FacePaintingPattern>(activePatterns);
        
        // Complete cleanup
        CleanupManager();
        
        // Reinitialize
        InitializePreviewMaterial();
        
        // Restore configuration
        enableFacePainting = wasEnabled;
        
        // Restore patterns if system is enabled
        if (enableFacePainting)
        {
            foreach (var pattern in storedPatterns)
            {
                ApplyFacePaintingPattern(pattern);
            }
        }
        
        DebugLog($"System reload completed. Restored {storedPatterns.Count} patterns.");
    }
    
    /// <summary>
    /// Advanced debug method to validate all face painting states
    /// </summary>
    public void ValidateAllFacePaintingStates()
    {
        DebugLog("Validating all face painting states...");
        
        int validTiles = 0;
        int invalidTiles = 0;
        int orphanedCubes = 0;
        int orphanedPreviews = 0;
        
        // Validate registered tiles
        foreach (var tile in facePaintingTiles.ToArray())
        {
            if (tile == null)
            {
                invalidTiles++;
                facePaintingTiles.Remove(tile);
            }
            else if (!tile.CanPaintCubes)
            {
                DebugLog($"Warning: Tile ({tile.x}, {tile.y}) is registered but cannot paint cubes");
                invalidTiles++;
            }
            else
            {
                validTiles++;
            }
        }
        
        // Validate cube tracking
        foreach (var kvp in tilesWithCubes.ToArray())
        {
            if (kvp.Value == null)
            {
                tilesWithCubes.Remove(kvp.Key);
                orphanedCubes++;
            }
        }
        
        // Validate previews
        foreach (var kvp in activePreviews.ToArray())
        {
            if (kvp.Value.gameObject == null)
            {
                activePreviews.Remove(kvp.Key);
                orphanedPreviews++;
            }
        }
        
        DebugLog($"Validation completed: {validTiles} valid tiles, {invalidTiles} invalid tiles cleaned up");
        DebugLog($"Cleaned up {orphanedCubes} orphaned cube references and {orphanedPreviews} orphaned previews");
    }
    
    /// <summary>
    /// Generates a comprehensive system report for debugging
    /// </summary>
    public void GenerateSystemReport()
    {
        DebugLog("=== COMPREHENSIVE FACE PAINTING SYSTEM REPORT ===");
        
        // System health
        ValidateAllFacePaintingStates();
        
        // Detailed status
        DebugPrintStatus();
        
        // Performance metrics
        DebugLog("--- Performance Metrics ---");
        DebugLog($"Total patterns memory: ~{activePatterns.Count * 200} bytes (estimated)");
        DebugLog($"Total preview objects: {activePreviews.Count}");
        DebugLog($"GridManager reference: {(gridManager != null ? "Valid" : "NULL - CRITICAL ERROR!")}");
        
        // Configuration summary
        DebugLog("--- Configuration Summary ---");
        var debugData = GetDebugData();
        foreach (var kvp in debugData)
        {
            DebugLog($"  {kvp.Key}: {kvp.Value}");
        }
        
        DebugLog("=== END SYSTEM REPORT ===");
    }
    
    #endregion
    
    #region Debug Utilities
    
    private void DebugLog(string message)
    {
        if (EnableDebugLogs)
            Debug.Log($"[FacePaintingManager] {message}");
    }
    
    #endregion
}

#region Data Structures
[System.Serializable]
public class FacePaintingPattern
{
    public string name;
    public Vector2Int basePosition;
    public List<FacePaintingEntry> entries = new List<FacePaintingEntry>();
}

[System.Serializable]
public class FacePaintingEntry
{
    public Vector2Int offset;
    public FaceStatus status;
    public Color color;
    public int duration = -1;
    public bool paintOnLanding = true;
    public bool paintOnExit = false;
}

[System.Serializable]
public class FacePaintingPreview
{
    public GameObject gameObject;
    public Vector2Int position;
    public Color color;
}
#endregion
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Manages all visual aspects of markers including creation, tile highlighting, 
/// countdown text displays, and visual effects.
/// </summary>
public class MarkerVisualManager : MonoBehaviour, IManagerDebugInterface
{
    #region Inspector Configuration

    [Header("Visual Settings")]
    #pragma warning disable CS0414 // Reserved for future flash effect implementation
    [SerializeField] private float flashDuration = 0.3f;
    #pragma warning restore CS0414
    [SerializeField] private Color areaPreviewColor = new Color(1f, 0.5f, 0f, 0.7f);

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    #endregion

    #region Manager References

    private GridManager gridManager;

    #endregion

    #region Runtime State

    // Temporary marker overlays for tile highlighting
    private Dictionary<Vector2Int, GameObject> temporaryMarkerOverlays = new Dictionary<Vector2Int, GameObject>();

    // Countdown text objects for auto-capture markers
    private Dictionary<Vector2Int, TextMesh> markerCountdownTexts = new Dictionary<Vector2Int, TextMesh>();

    #endregion

    #region Properties

    public bool EnableDebugLogs { get; set; } = true;

    #endregion

    #region Unity Lifecycle

    private void OnDestroy()
    {
        // Clean up all temporary overlays
        var overlaysToRemove = temporaryMarkerOverlays.Keys.ToList();
        foreach (var pos in overlaysToRemove)
        {
            ClearTileHighlight(pos);
        }

        // Clean up all countdown texts
        ClearAllMarkerCountdownTexts();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Initializes the visual manager with required references
    /// </summary>
    public void Initialize(GridManager grid)
    {
        gridManager = grid;
        EnableDebugLogs = enableDebugLogs;
        DebugLog("Initialize", "MarkerVisualManager initialized");
    }

    #endregion

    #region Visual Creation Methods

    /// <summary>
    /// Creates visual marker for Unit marker placement
    /// </summary>
    public GameObject CreateUnitMarkerVisual(Vector2Int position)
    {
        Tile tile = gridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            // Unit = Blue-gray (lighter variant for marker visibility)
            SetTileHighlight(tile, new Color(0.5f, 0.6f, 0.7f, 1f), "Unit");
        }

        GameObject dummy = new GameObject($"UnitMarker_{position.x}_{position.y}");
        dummy.transform.position = gridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    /// <summary>
    /// Creates visual marker for Recursion marker placement
    /// </summary>
    public GameObject CreateRecursionMarkerVisual(Vector2Int position)
    {
        Tile tile = gridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            // Recursion = Deep amber brown (warm brown-orange)
            SetTileHighlight(tile, new Color(0.8f, 0.5f, 0.2f, 1f), "Recursion");
        }

        GameObject dummy = new GameObject($"RecursionMarker_{position.x}_{position.y}");
        dummy.transform.position = gridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    /// <summary>
    /// Creates visual marker for Matrix marker placement
    /// </summary>
    public GameObject CreateMatrixMarkerVisual(Vector2Int position)
    {
        Tile tile = gridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            // Matrix = Vibrant light blue
            SetTileHighlight(tile, new Color(0.3f, 0.7f, 1f, 1f), "Matrix");
        }

        GameObject dummy = new GameObject($"MatrixMarker_{position.x}_{position.y}");
        dummy.transform.position = gridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    /// <summary>
    /// Creates visual marker for Infinity marker placement
    /// </summary>
    public GameObject CreateInfinityMarkerVisual(Vector2Int position)
    {
        Tile tile = gridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            // Infinity = Deep black (dark charcoal for visibility)
            SetTileHighlight(tile, new Color(0.15f, 0.15f, 0.18f, 1f), "Infinity");
        }

        GameObject dummy = new GameObject($"InfinityMarker_{position.x}_{position.y}");
        dummy.transform.position = gridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    /// <summary>
    /// Creates visual marker for Cube marker placement
    /// </summary>
    public GameObject CreateCubeMarkerVisual(Vector2Int position, PlayerMarkerSystem.CubeMarkerType type)
    {
        Tile tile = gridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            Color highlightColor = type switch
            {
                PlayerMarkerSystem.CubeMarkerType.Unit => Color.magenta,
                PlayerMarkerSystem.CubeMarkerType.Recursion => new Color(0.7f, 0.2f, 0.7f, 1f), // Dark magenta
                PlayerMarkerSystem.CubeMarkerType.Matrix => Color.cyan,
                PlayerMarkerSystem.CubeMarkerType.Cube => Color.yellow,
                _ => Color.white
            };
            string markerName = $"Cube{type}";
            SetTileHighlight(tile, highlightColor, markerName);
        }

        GameObject dummy = new GameObject($"CubeMarker_{type}_{position.x}_{position.y}");
        dummy.transform.position = gridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    /// <summary>
    /// Creates visual marker for powered-up Cube marker
    /// </summary>
    public GameObject CreatePoweredCubeMarkerVisual(Vector2Int position, PlayerMarkerSystem.CubeMarkerType type)
    {
        Tile tile = gridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            Color baseColor = type switch
            {
                PlayerMarkerSystem.CubeMarkerType.Unit => Color.magenta,
                PlayerMarkerSystem.CubeMarkerType.Recursion => new Color(0.7f, 0.2f, 0.7f, 1f),
                PlayerMarkerSystem.CubeMarkerType.Matrix => Color.cyan,
                PlayerMarkerSystem.CubeMarkerType.Cube => Color.yellow,
                _ => Color.white
            };
            Color poweredColor = new Color(baseColor.r * 1.5f, baseColor.g * 1.5f, baseColor.b * 1.5f, baseColor.a);
            string markerName = $"PoweredCube{type}";
            SetTileHighlight(tile, poweredColor, markerName);
        }

        GameObject dummy = new GameObject($"PoweredCubeMarker_{type}_{position.x}_{position.y}");
        dummy.transform.position = gridManager.GridToWorldPosition(position.x, position.y, 0f);
        return dummy;
    }

    /// <summary>
    /// Destroys a marker visual and clears associated highlights
    /// </summary>
    public void DestroyMarkerVisual(GameObject visual)
    {
        if (visual != null)
        {
            // Extract position from the visual object name
            string[] nameParts = visual.name.Split('_');
            if (nameParts.Length >= 3)
            {
                if (int.TryParse(nameParts[nameParts.Length - 2], out int x) &&
                    int.TryParse(nameParts[nameParts.Length - 1], out int y))
                {
                    ClearTileHighlight(new Vector2Int(x, y));
                    DebugLog("DestroyMarkerVisual", $"Cleared tile highlight at ({x}, {y}) after marker removal");
                }
            }

            Destroy(visual);
        }
    }

    #endregion

    #region Tile Highlighting

    /// <summary>
    /// Sets a tile highlight with specified color and marker type
    /// </summary>
    public void SetTileHighlight(Tile tile, Color color, string markerType)
    {
        if (tile == null) return;

        Vector2Int pos = new Vector2Int(tile.x, tile.y);

        // Remove existing overlay if present
        ClearTileHighlight(tile);

        // Create temporary overlay object
        GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
        overlay.name = $"ActionMarker_{markerType}_{tile.x}_{tile.y}";
        overlay.transform.SetParent(tile.transform);
        overlay.transform.localPosition = new Vector3(0, 0.52f, 0); // Slightly above tile overlay
        overlay.transform.localScale = new Vector3(0.95f, 0.08f, 0.95f);

        // Remove collider to avoid physics issues
        Destroy(overlay.GetComponent<Collider>());

        // Create material with highlight color
        Renderer overlayRenderer = overlay.GetComponent<Renderer>();
        if (overlayRenderer != null)
        {
            Material highlightMaterial = new Material(Shader.Find("Standard"));
            highlightMaterial.color = color;
            highlightMaterial.SetFloat("_Metallic", 0.3f);
            highlightMaterial.SetFloat("_Smoothness", 0.7f);

            // Enable emission for glow effect
            highlightMaterial.EnableKeyword("_EMISSION");
            highlightMaterial.SetColor("_EmissionColor", color * 0.3f);

            overlayRenderer.material = highlightMaterial;
        }

        // Store overlay for cleanup
        temporaryMarkerOverlays[pos] = overlay;

        DebugLog("SetTileHighlight", $"Created {markerType} highlight overlay at ({tile.x}, {tile.y}) with color {color}");
    }

    /// <summary>
    /// Clears tile highlight by Tile reference
    /// </summary>
    public void ClearTileHighlight(Tile tile)
    {
        if (tile == null) return;
        Vector2Int pos = new Vector2Int(tile.x, tile.y);
        ClearTileHighlight(pos);
    }

    /// <summary>
    /// Clears tile highlight by position
    /// </summary>
    public void ClearTileHighlight(Vector2Int position)
    {
        if (temporaryMarkerOverlays.TryGetValue(position, out GameObject overlay))
        {
            if (overlay != null)
            {
                Destroy(overlay);
            }
            temporaryMarkerOverlays.Remove(position);
            DebugLog("ClearTileHighlight", $"Cleared highlight overlay at ({position.x}, {position.y})");
        }
    }

    /// <summary>
    /// Clears all temporary marker overlays
    /// </summary>
    public void ClearAllTileHighlights()
    {
        var overlaysToRemove = temporaryMarkerOverlays.Keys.ToList();
        foreach (var pos in overlaysToRemove)
        {
            ClearTileHighlight(pos);
        }
    }

    /// <summary>
    /// Gets count of active tile highlights
    /// </summary>
    public int GetActiveHighlightCount() => temporaryMarkerOverlays.Count;

    #endregion

    #region Marker Countdown Text

    /// <summary>
    /// Creates a countdown text display on a tile for auto-capture markers
    /// </summary>
    public void CreateMarkerCountdownText(Vector2Int position, int remainingMoves, Color textColor)
    {
        if (markerCountdownTexts.ContainsKey(position))
        {
            // Already exists, just update it
            UpdateMarkerCountdownText(position, remainingMoves);
            return;
        }

        Tile tile = gridManager?.GetTileAt(position.x, position.y);
        if (tile == null) return;

        // Create text object
        GameObject textObj = new GameObject($"MarkerCountdown_{position.x}_{position.y}");
        textObj.transform.SetParent(tile.transform);
        textObj.transform.localPosition = new Vector3(0, 1.0f, 0); // Above the tile overlay

        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = remainingMoves.ToString();
        textMesh.fontSize = 12;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = textColor;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.15f;

        // Make text face camera (billboard style)
        if (Camera.main != null)
        {
            textObj.transform.LookAt(Camera.main.transform);
            textObj.transform.Rotate(0, 180, 0);
        }

        markerCountdownTexts[position] = textMesh;
        DebugLog("CreateMarkerCountdownText", $"Created countdown text at ({position.x}, {position.y}) showing {remainingMoves}");
    }

    /// <summary>
    /// Updates the countdown text for a marker position
    /// </summary>
    public void UpdateMarkerCountdownText(Vector2Int position, int remainingMoves)
    {
        if (!markerCountdownTexts.TryGetValue(position, out TextMesh textMesh)) return;
        if (textMesh == null)
        {
            markerCountdownTexts.Remove(position);
            return;
        }

        textMesh.text = remainingMoves.ToString();

        // Update color based on remaining moves (visual urgency)
        if (remainingMoves <= 1)
            textMesh.color = Color.red;
        else if (remainingMoves <= 2)
            textMesh.color = Color.yellow;
        else
            textMesh.color = Color.white;

        // Re-orient to face camera each update
        if (Camera.main != null && textMesh.gameObject != null)
        {
            textMesh.transform.LookAt(Camera.main.transform);
            textMesh.transform.Rotate(0, 180, 0);
        }
    }

    /// <summary>
    /// Removes the countdown text for a marker position
    /// </summary>
    public void ClearMarkerCountdownText(Vector2Int position)
    {
        if (markerCountdownTexts.TryGetValue(position, out TextMesh textMesh))
        {
            if (textMesh != null)
            {
                Destroy(textMesh.gameObject);
            }
            markerCountdownTexts.Remove(position);
            DebugLog("ClearMarkerCountdownText", $"Cleared countdown text at ({position.x}, {position.y})");
        }
    }

    /// <summary>
    /// Clears all marker countdown texts
    /// </summary>
    public void ClearAllMarkerCountdownTexts()
    {
        foreach (var kvp in markerCountdownTexts)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value.gameObject);
            }
        }
        markerCountdownTexts.Clear();
    }

    /// <summary>
    /// Gets count of active countdown texts
    /// </summary>
    public int GetActiveCountdownTextCount() => markerCountdownTexts.Count;

    #endregion

    #region Effects

    /// <summary>
    /// Shows a trigger effect at the specified position
    /// </summary>
    public void ShowMarkerTriggerEffect(Vector2Int position)
    {
        StartCoroutine(MarkerTriggerEffectCoroutine(position));
    }

    private IEnumerator MarkerTriggerEffectCoroutine(Vector2Int position)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(position.x, position.y, 0.1f);

        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = $"TriggerEffect_{position.x}_{position.y}";
        effect.transform.position = worldPos;
        effect.transform.localScale = Vector3.zero;

        Destroy(effect.GetComponent<Collider>());
        Renderer renderer = effect.GetComponent<Renderer>();

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            effect.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one * 2f, t);

            Color color = Color.white;
            color.a = 1f - t;
            renderer.material.color = color;

            yield return null;
        }

        Destroy(effect);
    }
    
    /// <summary>
    /// Shows a success flash effect when a cube is captured
    /// </summary>
    public void ShowCaptureSuccessEffect(Vector2Int position, CubeType cubeType)
    {
        StartCoroutine(CaptureSuccessEffectCoroutine(position, cubeType));
    }
    
    private IEnumerator CaptureSuccessEffectCoroutine(Vector2Int position, CubeType cubeType)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(position.x, position.y, 1.5f);
        
        // Create flash effect
        GameObject flash = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flash.name = $"CaptureFlash_{position.x}_{position.y}";
        flash.transform.position = worldPos;
        flash.transform.localScale = Vector3.zero;
        
        Destroy(flash.GetComponent<Collider>());
        Renderer flashRenderer = flash.GetComponent<Renderer>();
        
        // Success color based on cube type
        Color successColor = cubeType switch
        {
            CubeType.Unit => new Color(0.3f, 0.8f, 0.3f, 1f), // Green
            CubeType.Matrix => new Color(0.3f, 0.7f, 1f, 1f), // Blue
            CubeType.Recursion => new Color(0.8f, 0.5f, 0.2f, 1f), // Orange
            _ => Color.green
        };
        
        Material flashMat = new Material(Shader.Find("Standard"));
        flashMat.EnableKeyword("_EMISSION");
        flashMat.SetColor("_EmissionColor", successColor * 2f);
        flashRenderer.material = flashMat;
        
        // Flash animation
        float duration = 0.4f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Expand then fade
            float scale = t < 0.5f 
                ? Mathf.Lerp(0f, 3f, t * 2f) 
                : Mathf.Lerp(3f, 4f, (t - 0.5f) * 2f);
            
            flash.transform.localScale = Vector3.one * scale;
            
            // Fade out
            Color color = successColor;
            color.a = 1f - t;
            flashMat.SetColor("_EmissionColor", color * (2f * (1f - t)));
            flashRenderer.material.color = color;
            
            yield return null;
        }
        
        Destroy(flash);
    }

    /// <summary>
    /// Clears area expansion highlights after delay
    /// </summary>
    public void ClearAreaExpansionAfterDelay(List<Vector2Int> positions, Vector2Int centerPos, float delay)
    {
        StartCoroutine(ClearAreaExpansionCoroutine(positions, centerPos, delay));
    }

    private IEnumerator ClearAreaExpansionCoroutine(List<Vector2Int> positions, Vector2Int centerPos, float delay)
    {
        yield return new WaitForSeconds(delay);

        foreach (var pos in positions)
        {
            ClearTileHighlight(pos);
        }
    }

    #endregion

    #region Debug

    private void DebugLog(string methodName, string message)
    {
        if (EnableDebugLogs)
            Debug.Log($"[MarkerVisualManager] {methodName}: {message}");
    }

    private void DebugWarning(string methodName, string message)
    {
        if (EnableDebugLogs)
            Debug.LogWarning($"[MarkerVisualManager] {methodName}: {message}");
    }

    private void DebugError(string methodName, string message)
    {
        Debug.LogError($"[MarkerVisualManager] {methodName}: {message}");
    }

    public string GetDebugStatus()
    {
        return $"MarkerVisualManager: {temporaryMarkerOverlays.Count} highlights, {markerCountdownTexts.Count} countdown texts";
    }

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Active Highlights"] = temporaryMarkerOverlays.Count,
            ["Active Countdown Texts"] = markerCountdownTexts.Count,
            ["GridManager Set"] = gridManager != null
        };
    }

    public void ResetToDefaults()
    {
        ClearAllTileHighlights();
        ClearAllMarkerCountdownTexts();
    }

    public void LoadConfiguration(string configName)
    {
        DebugLog("LoadConfiguration", $"Loading configuration: {configName} (not yet implemented)");
    }

    public void SaveConfiguration(string configName)
    {
        DebugLog("SaveConfiguration", $"Saving configuration: {configName} (not yet implemented)");
    }

    #endregion
}

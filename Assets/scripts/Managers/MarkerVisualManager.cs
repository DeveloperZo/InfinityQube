using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static Enumerations;

/// <summary>
/// Manages all marker visual effects including tile highlights, countdown text, and effects.
/// Extracted from PlayerMarkerSystem for better organization and maintainability.
/// </summary>
public class MarkerVisualManager : MonoBehaviour
{
    #region Serialized Fields
    
    [Header("Visual Effects Settings")]
    [SerializeField] private float flashDuration = 0.3f;
    [SerializeField] private Color areaPreviewColor = new Color(1f, 0.5f, 0f, 0.7f);
    
    [Header("Cube Marker Materials")]
    [SerializeField] private Material cubeMarkerMaterial;
    [SerializeField] private Material poweredCubeMarkerMaterial;
    
    #endregion
    
    #region Private Fields
    
    // Temporary overlay tracking
    private Dictionary<Vector2Int, GameObject> temporaryMarkerOverlays = new Dictionary<Vector2Int, GameObject>();
    
    // Countdown text objects for auto-capture markers
    private Dictionary<Vector2Int, TextMesh> markerCountdownTexts = new Dictionary<Vector2Int, TextMesh>();
    
    // Preview system
    private List<GameObject> previewObjects = new List<GameObject>();
    private bool showingPreview = false;
    
    // Manager references
    private GridManager gridManager;
    
    #endregion
    
    #region Properties
    
    public bool ShowingPreview => showingPreview;
    
    #endregion
    
    #region Initialization
    
    public void Initialize(GridManager grid)
    {
        gridManager = grid;
    }
    
    void OnDestroy()
    {
        ClearAllOverlays();
        ClearAllMarkerCountdownTexts();
    }
    
    #endregion
    
    #region Marker Visual Creation
    
    /// <summary>
    /// Creates visual for Unit marker placement
    /// </summary>
    public GameObject CreateUnitMarkerVisual(Vector2Int position)
    {
        Tile tile = gridManager?.GetTileAt(position.x, position.y);
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
    /// Creates visual for Recursion marker placement
    /// </summary>
    public GameObject CreateRecursionMarkerVisual(Vector2Int position)
    {
        Tile tile = gridManager?.GetTileAt(position.x, position.y);
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
    /// Creates visual for Matrix marker placement
    /// </summary>
    public GameObject CreateMatrixMarkerVisual(Vector2Int position)
    {
        Tile tile = gridManager?.GetTileAt(position.x, position.y);
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
    /// Creates visual for Cube marker (generated from collisions)
    /// </summary>
    public GameObject CreateCubeMarkerVisual(Vector2Int position, PlayerMarkerSystem.CubeMarkerType type)
    {
        Tile tile = gridManager?.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            Color highlightColor = type switch
            {
                PlayerMarkerSystem.CubeMarkerType.Unit => Color.magenta,
                PlayerMarkerSystem.CubeMarkerType.Recursion => new Color(0.7f, 0.2f, 0.7f, 1f),
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
    /// Creates visual for powered-up Cube marker
    /// </summary>
    public GameObject CreatePoweredCubeMarkerVisual(Vector2Int position, PlayerMarkerSystem.CubeMarkerType type)
    {
        Tile tile = gridManager?.GetTileAt(position.x, position.y);
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
    
    #endregion
    
    #region Tile Highlighting
    
    /// <summary>
    /// Sets a highlight overlay on a tile with the specified color
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
        overlay.transform.localPosition = new Vector3(0, 0.52f, 0);
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

        Debug.Log($"[MarkerVisualManager] Created {markerType} highlight at ({tile.x}, {tile.y})");
    }

    /// <summary>
    /// Clears highlight from a tile
    /// </summary>
    public void ClearTileHighlight(Tile tile)
    {
        if (tile == null) return;
        
        Vector2Int pos = new Vector2Int(tile.x, tile.y);

        if (temporaryMarkerOverlays.TryGetValue(pos, out GameObject overlay))
        {
            if (overlay != null)
            {
                Destroy(overlay);
            }
            temporaryMarkerOverlays.Remove(pos);
        }
    }

    /// <summary>
    /// Clears highlight at a position
    /// </summary>
    public void ClearTileHighlight(Vector2Int position)
    {
        Tile tile = gridManager?.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            ClearTileHighlight(tile);
        }
    }
    
    /// <summary>
    /// Clears all overlay highlights
    /// </summary>
    public void ClearAllOverlays()
    {
        foreach (var kvp in temporaryMarkerOverlays)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        temporaryMarkerOverlays.Clear();
    }
    
    #endregion
    
    #region Marker Countdown Text
    
    /// <summary>
    /// Creates a countdown text display on a tile for auto-capture markers
    /// </summary>
    public void CreateMarkerCountdownText(Vector2Int position, int remainingMoves, Color textColor)
    {
        if (markerCountdownTexts.ContainsKey(position))
        {
            UpdateMarkerCountdownText(position, remainingMoves);
            return;
        }
        
        Tile tile = gridManager?.GetTileAt(position.x, position.y);
        if (tile == null) return;
        
        // Create text object
        GameObject textObj = new GameObject($"MarkerCountdown_{position.x}_{position.y}");
        textObj.transform.SetParent(tile.transform);
        textObj.transform.localPosition = new Vector3(0, 1.0f, 0);
        
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = remainingMoves.ToString();
        textMesh.fontSize = 12;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = textColor;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.characterSize = 0.15f;
        
        // Make text face camera
        if (Camera.main != null)
        {
            textObj.transform.LookAt(Camera.main.transform);
            textObj.transform.Rotate(0, 180, 0);
        }
        
        markerCountdownTexts[position] = textMesh;
        Debug.Log($"[MarkerVisualManager] Created countdown text at ({position.x}, {position.y}) showing {remainingMoves}");
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
        
        // Re-orient to face camera
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
    
    #endregion
    
    #region Area Preview
    
    /// <summary>
    /// Shows preview of area effect
    /// </summary>
    public void ShowAreaPreview(List<Vector2Int> positions)
    {
        HideAreaPreview();
        
        foreach (var pos in positions)
        {
            Tile tile = gridManager?.GetTileAt(pos.x, pos.y);
            if (tile != null)
            {
                GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                preview.name = $"AreaPreview_{pos.x}_{pos.y}";
                preview.transform.SetParent(tile.transform);
                preview.transform.localPosition = new Vector3(0, 0.55f, 0);
                preview.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
                
                Destroy(preview.GetComponent<Collider>());
                
                Renderer renderer = preview.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Material previewMat = new Material(Shader.Find("Standard"));
                    previewMat.color = areaPreviewColor;
                    renderer.material = previewMat;
                }
                
                previewObjects.Add(preview);
            }
        }
        
        showingPreview = true;
    }
    
    /// <summary>
    /// Hides area preview
    /// </summary>
    public void HideAreaPreview()
    {
        foreach (GameObject preview in previewObjects)
        {
            if (preview != null)
            {
                Destroy(preview);
            }
        }
        previewObjects.Clear();
        showingPreview = false;
    }
    
    #endregion
    
    #region Marker Visual Destruction
    
    /// <summary>
    /// Destroys a marker visual and cleans up associated highlight
    /// </summary>
    public void DestroyMarkerVisual(GameObject visual)
    {
        if (visual == null) return;
        
        // Extract position from the visual object name
        string[] nameParts = visual.name.Split('_');
        if (nameParts.Length >= 3)
        {
            if (int.TryParse(nameParts[nameParts.Length - 2], out int x) &&
                int.TryParse(nameParts[nameParts.Length - 1], out int y))
            {
                ClearTileHighlight(new Vector2Int(x, y));
            }
        }

        Destroy(visual);
    }
    
    #endregion
    
    #region Trigger Effects
    
    /// <summary>
    /// Shows visual effect when a marker is triggered
    /// </summary>
    public IEnumerator ShowMarkerTriggerEffect(Vector2Int position)
    {
        Tile tile = gridManager?.GetTileAt(position.x, position.y);
        if (tile == null) yield break;

        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = $"TriggerEffect_{position.x}_{position.y}";
        effect.transform.position = gridManager.GridToWorldPosition(position.x, position.y, 0.5f);
        effect.transform.localScale = Vector3.one * 0.5f;

        Destroy(effect.GetComponent<Collider>());

        Renderer renderer = effect.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material effectMat = new Material(Shader.Find("Standard"));
            effectMat.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            effectMat.EnableKeyword("_EMISSION");
            effectMat.SetColor("_EmissionColor", Color.yellow * 2f);
            renderer.material = effectMat;
        }

        // Expand and fade out
        float elapsed = 0f;
        Vector3 startScale = effect.transform.localScale;
        Vector3 endScale = startScale * 2f;

        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            
            effect.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            
            if (renderer != null)
            {
                Color c = renderer.material.color;
                c.a = Mathf.Lerp(0.8f, 0f, t);
                renderer.material.color = c;
            }
            
            yield return null;
        }

        Destroy(effect);
    }
    
    /// <summary>
    /// Shows visual effect for area capture
    /// </summary>
    public IEnumerator ShowAreaCaptureEffect(List<Vector2Int> positions, Color effectColor)
    {
        List<GameObject> effects = new List<GameObject>();
        
        foreach (var pos in positions)
        {
            GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Cube);
            effect.name = $"AreaEffect_{pos.x}_{pos.y}";
            effect.transform.position = gridManager.GridToWorldPosition(pos.x, pos.y, 0.5f);
            effect.transform.localScale = Vector3.one * 0.8f;

            Destroy(effect.GetComponent<Collider>());

            Renderer renderer = effect.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material effectMat = new Material(Shader.Find("Standard"));
                effectMat.color = effectColor;
                effectMat.EnableKeyword("_EMISSION");
                effectMat.SetColor("_EmissionColor", effectColor * 1.5f);
                renderer.material = effectMat;
            }
            
            effects.Add(effect);
        }

        // Animate all effects
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashDuration;
            
            foreach (var effect in effects)
            {
                if (effect == null) continue;
                
                effect.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one * 1.2f, t);
                
                Renderer renderer = effect.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color c = renderer.material.color;
                    c.a = Mathf.Lerp(0.8f, 0f, t);
                    renderer.material.color = c;
                }
            }
            
            yield return null;
        }

        foreach (var effect in effects)
        {
            if (effect != null)
            {
                Destroy(effect);
            }
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    /// <summary>
    /// Gets color for face painting based on cube type
    /// </summary>
    public Color GetFaceColorForType(CubeType type)
    {
        return type switch
        {
            CubeType.Unit => Color.gray,
            CubeType.Matrix => Color.cyan,
            CubeType.Recursion => new Color(0.8f, 0.5f, 0.2f),
            CubeType.Infinity => Color.black,
            _ => Color.white
        };
    }
    
    #endregion
}


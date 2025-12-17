using UnityEngine;

/// <summary>
/// Handles all visual state management for tiles including overlays and state visualization
/// </summary>
public class TileVisuals
{
    #region Configuration
    private readonly float overlayHeight = 0.51f;
    private readonly Vector3 overlayScale = new Vector3(0.9f, 0.05f, 0.9f);
    
    // Colors for different states
    private readonly Color markerColor = Color.red;
    private readonly Color corruptedColor = Color.black;
    private readonly Color matrixedColor = Color.blue;
    #endregion

    #region Runtime State
    private GameObject stateOverlay;
    private Transform parentTransform;
    private bool enableDebugLogs;
    #endregion

    #region Constructor
    public TileVisuals(Transform tileTransform, bool debugLogs = false)
    {
        parentTransform = tileTransform;
        enableDebugLogs = debugLogs;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Updates the visual state of the tile based on current state flags
    /// </summary>
    public void UpdateStateOverlay(bool hasMarker, bool isCorrupted, bool isBlackened, bool hasDetonationPoint)
    {
        // Determine if we need an overlay and what color
        (bool needsOverlay, Color overlayColor) = DetermineOverlayState(hasMarker, isCorrupted, isBlackened, hasDetonationPoint);

        if (needsOverlay)
        {
            CreateOrUpdateOverlay(overlayColor);
        }
        else
        {
            RemoveOverlay();
        }
    }

    /// <summary>
    /// Removes all visual overlays
    /// </summary>
    public void RemoveOverlay()
    {
        if (stateOverlay != null)
        {
            Object.Destroy(stateOverlay);
            stateOverlay = null;
        }
    }

    /// <summary>
    /// Cleanup when tile is destroyed
    /// </summary>
    public void OnDestroy()
    {
        RemoveOverlay();
    }
    #endregion

    #region Private Methods
    private (bool needsOverlay, Color color) DetermineOverlayState(bool hasMarker, bool isCorrupted, bool isBlackened, bool hasDetonationPoint)
    {
        // Priority order - return first match
        if (hasMarker)
            return (true, markerColor);

        if (isCorrupted)
            return (true, new Color(0.5f, 0f, 0.5f, 1f)); // Dark purple for corruption

        if (isBlackened)
            return (true, corruptedColor);

        if (hasDetonationPoint)
            return (true, matrixedColor);

        return (false, Color.white); // No overlay needed
    }

    private void CreateOrUpdateOverlay(Color color)
    {
        if (stateOverlay == null)
        {
            // Create overlay object
            stateOverlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stateOverlay.name = $"StateOverlay_{parentTransform.name}";
            stateOverlay.transform.SetParent(parentTransform);

            // Position on top of tile
            stateOverlay.transform.localPosition = new Vector3(0, overlayHeight, 0);
            stateOverlay.transform.localScale = overlayScale;

            // Remove collider to avoid physics issues
            Object.Destroy(stateOverlay.GetComponent<Collider>());

            DebugLog($"Created state overlay for tile {parentTransform.name}");
        }

        // Update overlay color
        Renderer overlayRenderer = stateOverlay.GetComponent<Renderer>();
        if (overlayRenderer != null)
        {
            // Create or update material
            if (overlayRenderer.material.name.Contains("Default"))
            {
                Material overlayMaterial = new Material(Shader.Find("Standard"));
                overlayMaterial.color = color;
                overlayMaterial.SetFloat("_Metallic", 0.2f);
                overlayMaterial.SetFloat("_Smoothness", 0.8f);

                // Add slight emission for better visibility
                overlayMaterial.EnableKeyword("_EMISSION");
                overlayMaterial.SetColor("_EmissionColor", color * 0.2f);

                overlayRenderer.material = overlayMaterial;
            }
            else
            {
                // Just update color
                overlayRenderer.material.color = color;
                overlayRenderer.material.SetColor("_EmissionColor", color * 0.2f);
            }
        }
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[TileVisuals] {message}");
        }
    }
    #endregion
}

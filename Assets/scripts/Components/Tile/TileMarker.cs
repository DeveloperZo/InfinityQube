using UnityEngine;
using System.Collections;

/// <summary>
/// Handles marker placement, clearing, and visual feedback for tiles
/// </summary>
public class TileMarker
{
    #region Configuration
    private float markerHeight = 0.5f;
    private float markerScale = 0.8f;
    #endregion

    #region Runtime State
    private bool hasMarker = false;
    private GameObject markerObj;
    private Transform parentTransform;
    private TileVisuals tileVisuals;
    private Tile parentTile;
    private bool enableDebugLogs;
    #endregion

    #region Properties
    public bool HasMarker => hasMarker;
    #endregion

    #region Constructor
    public TileMarker(Transform tileTransform, TileVisuals visuals, Tile tile, bool debugLogs = false)
    {
        parentTransform = tileTransform;
        tileVisuals = visuals;
        parentTile = tile;
        enableDebugLogs = debugLogs;
    }
    #endregion

    #region Public Methods
    public void PlaceMarker(bool canBeMarked, bool canAcceptMarkers)
    {
        if (hasMarker || !canBeMarked || !canAcceptMarkers) return;

        hasMarker = true;

        // Create marker object if it doesn't exist
        if (markerObj == null)
        {
            markerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerObj.transform.SetParent(parentTransform);
            markerObj.transform.localPosition = new Vector3(0, markerHeight, 0);
            markerObj.transform.localScale = new Vector3(markerScale, 0.3f, markerScale);
            markerObj.name = $"Marker_{parentTransform.name}";

            // Remove collider to avoid physics interference
            Collider markerCollider = markerObj.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Object.Destroy(markerCollider);
            }

            // Set marker color (bright red/orange for visibility)
            Renderer markerRenderer = markerObj.GetComponent<Renderer>();
            if (markerRenderer != null)
            {
                Material markerMaterial = new Material(Shader.Find("Standard"));
                markerMaterial.color = Color.red;
                markerMaterial.SetFloat("_Metallic", 0.2f);
                markerMaterial.SetFloat("_Smoothness", 0.8f);
                markerRenderer.material = markerMaterial;
            }
        }

        DebugLog($"Marked tile {parentTransform.name}");
    }

    public void ClearMarker()
    {
        if (!hasMarker) return;

        DebugLog($"Clearing marker on {parentTransform.name}");

        hasMarker = false;

        // Destroy marker object
        if (markerObj != null)
        {
            Object.Destroy(markerObj);
            markerObj = null;
        }

        DebugLog($"Marker cleared successfully on {parentTransform.name}");
    }

    public void ToggleMarker(bool canBeMarked, bool canAcceptMarkers)
    {
        if (hasMarker)
        {
            ClearMarker();
        }
        else
        {
            PlaceMarker(canBeMarked, canAcceptMarkers);
        }
    }

    /// <summary>
    /// Activates the marker visual feedback when triggered
    /// </summary>
    public void ActivateMarker()
    {
        DebugLog($"ActivateMarker called on {parentTransform.name}");

        // Hide the marker object temporarily
        if (markerObj != null)
        {
            markerObj.SetActive(false);
            DebugLog($"Marker object hidden on {parentTransform.name}");
        }

        // Start flash effect
        parentTile.StartCoroutine(FlashOverlay());
    }

    /// <summary>
    /// Resets marker after a delay (called from Tile's coroutine)
    /// </summary>
    public void ResetMarkerAfterDelay()
    {
        DebugLog($"Resetting marker after delay on {parentTransform.name}");

        // Clear the marker state
        hasMarker = false;

        if (markerObj != null)
        {
            Object.Destroy(markerObj);
            markerObj = null;
            DebugLog($"Marker object destroyed on {parentTransform.name}");
        }
    }

    /// <summary>
    /// Cleanup when tile is destroyed
    /// </summary>
    public void OnDestroy()
    {
        if (markerObj != null)
        {
            Object.Destroy(markerObj);
            markerObj = null;
        }
    }

    /// <summary>
    /// Update marker configuration
    /// </summary>
    public void UpdateConfiguration(float height, float scale)
    {
        markerHeight = height;
        markerScale = scale;
        
        // Update existing marker if present
        if (markerObj != null)
        {
            markerObj.transform.localPosition = new Vector3(0, markerHeight, 0);
            markerObj.transform.localScale = new Vector3(markerScale, 0.3f, markerScale);
        }
    }
    #endregion

    #region Private Methods
    private IEnumerator FlashOverlay()
    {
        // Create a temporary flash overlay
        GameObject flashOverlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flashOverlay.name = $"FlashOverlay_{parentTransform.name}";
        flashOverlay.transform.SetParent(parentTransform);
        flashOverlay.transform.localPosition = new Vector3(0, 0.52f, 0); // Just above tile
        flashOverlay.transform.localScale = new Vector3(0.95f, 0.02f, 0.95f);

        // Remove collider
        Object.Destroy(flashOverlay.GetComponent<Collider>());

        // Set flash material
        Renderer flashRenderer = flashOverlay.GetComponent<Renderer>();
        if (flashRenderer != null)
        {
            Material flashMaterial = new Material(Shader.Find("Standard"));
            flashMaterial.color = Color.white;
            flashMaterial.EnableKeyword("_EMISSION");
            flashMaterial.SetColor("_EmissionColor", Color.white * 2f);
            flashRenderer.material = flashMaterial;
        }

        // Flash for a brief moment
        yield return new WaitForSeconds(0.1f);

        Object.Destroy(flashOverlay);
    }

    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[TileMarker] {message}");
        }
    }
    #endregion
}

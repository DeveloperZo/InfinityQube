using System;
using System.Collections;
using UnityEngine;
using static Enumerations;

public class Tile : MonoBehaviour
{
    [Header("Tile Properties")]
    [SerializeField] public int x, y;
    [SerializeField] private bool hasMarker = false;
    [SerializeField] private float markerHeight = 0.5f;
    [SerializeField] private float markerScale = 0.8f;

    private const float TRANSFORMED_HEIGHT = -0.25f;  // Lower by 0.25 for transformed tiles
    private const float MARKED_HEIGHT = 0.25f;        // Raise by 0.25 for marked tiles
    private const float NORMAL_HEIGHT = 0f;           // Normal baseline height

    [Header("Detonation Point")]
    [SerializeField] private bool hasDetonationPoint = false;
    public bool HasDetonationPoint => hasDetonationPoint;

    [Header("Enhanced Blue Tile")]
    [SerializeField] private int detonationCharges = 0;
    [SerializeField] private int maxCharges = 3;

    [Header("Player Hover Effect")]
    [SerializeField] private bool isPlayerOnTile = false;
    [SerializeField] private GameObject softHighlightObject;
    [SerializeField] private Material playerHoverMaterial;

    [Header("Materials")]
    [SerializeField] private Material markedTileMaterial;
    [SerializeField] private Material forbiddenMaterial;
    [SerializeField] private Material activateMarkerMaterial;
    [SerializeField] private Material originalMaterial;
    [SerializeField] private Material[] chargeMaterials;

    // Properties to access charge information
    public int DetonationCharges => detonationCharges;
    public bool HasCharges => detonationCharges > 0;
    public bool IsBlackened => isBlackened;
    public bool IsAdvantaged => isAdvantaged;

    public bool IsPrimed => hasDetonationPoint;
    public bool HasMarker => hasMarker;
    public TileState currentState = TileState.Normal;

    public bool CanBeMarked => currentState == TileState.Normal && !isBlackened;

    private GameObject markerObj;
    private Renderer tileRenderer;
    public CubeBehavior currentCube;
    private bool isInitialized = false;
    private bool isBlackened = false;
    private bool isAdvantaged = false;

    public bool isPhasedZone { get; private set; }
    private TextMesh countdownText;

    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer != null)
        {
            originalMaterial = tileRenderer.material;
        }
    }

    public void Init(int xPos, int yPos)
    {
        x = xPos;
        y = yPos;
        isInitialized = true;

        // Initialize hover material
        if (playerHoverMaterial == null)
        {
            playerHoverMaterial = new Material(Shader.Find("Standard"));
            playerHoverMaterial.color = new Color(0.3f, 0.8f, 1f, 0.5f);
        }
    }

    private void OnDestroy()
    {
        if (markerObj != null)
        {
            Destroy(markerObj);
            markerObj = null;
        }

        if (softHighlightObject != null)
        {
            Destroy(softHighlightObject);
            softHighlightObject = null;
        }
    }

    public void PlaceMarker()
    {
        if (!isInitialized || hasMarker || !CanBeMarked) return;

        hasMarker = true;

        // Create marker object if it doesn't exist
        if (markerObj == null)
        {
            markerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerObj.transform.SetParent(transform);
            markerObj.transform.localPosition = new Vector3(0, markerHeight, 0);
            markerObj.transform.localScale = new Vector3(markerScale, 0.3f, markerScale);
            markerObj.name = $"Marker_{x}_{y}";

            // Remove collider to avoid physics interference
            Collider markerCollider = markerObj.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
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

        // Update visuals through central system
        UpdateTileVisuals();

        // Hide soft highlight when marked (marker takes precedence)
        if (softHighlightObject != null)
        {
            Destroy(softHighlightObject);
        }

        Debug.Log($"Marked tile at ({x}, {y})");
    }

    public void ClearMarker()
    {
        if (!hasMarker) return;

        Debug.Log($"Tile ({x},{y}): ClearMarker called");

        hasMarker = false;

        // Destroy marker object
        if (markerObj != null)
        {
            Destroy(markerObj);
            markerObj = null;
        }

        // Update visuals through central system
        UpdateTileVisuals();

        // Restore soft highlight if player is still on this tile
        if (isPlayerOnTile)
        {
            Debug.Log($"Tile ({x},{y}): After clearing marker, restoring soft highlight");
            ShowSoftHighlight();
        }

        Debug.Log($"Tile ({x},{y}): Marker cleared successfully");
    }

    public void SetDetonationPoint(bool hasPoint)
    {
        hasDetonationPoint = hasPoint;
        UpdateTileVisuals();
        Debug.Log($"Tile ({x},{y}): Detonation point set to {hasPoint}");
    }

    public void BlackenTile()
    {
        isBlackened = true;
        isAdvantaged = false;
        detonationCharges = 0;
        ClearMarker(); // Remove any existing marker

        UpdateTileVisuals();
        HideSoftHighlight();
    }

    public void PrimeTile()
    {
        if (isBlackened) return;
        hasDetonationPoint = true;

        ClearMarker();
        UpdateTileVisuals();

        // Register with PlayerActionManager
        PlayerActionManager playerActionManager = FindObjectOfType<PlayerActionManager>();
        if (playerActionManager != null)
        {
            playerActionManager.CreateCubeMarker(new Vector2Int(x, y));
        }

        Debug.Log($"Tile ({x},{y}): Primed for detonation and registered with PlayerActionManager");
    }

    public void AdvantageTile(int charges = 3)
    {
        if (isBlackened) return;

        isAdvantaged = true;
        detonationCharges = charges > maxCharges ? maxCharges : charges;
        ClearMarker();

        UpdateTileVisuals();

        Debug.Log($"Blue tile at ({x}, {y}) enhanced to charge level {detonationCharges}");
    }

    public void ResetTile()
    {
        currentState = TileState.Normal;
        isBlackened = false;
        isAdvantaged = false;
        detonationCharges = 0;
        hasDetonationPoint = false;
        ClearMarker();

        UpdateTileVisuals();
    }

    public void ResetTileAppearance()
    {
        // Public method for external systems to force a visual update
        UpdateTileVisuals();
    }


    public void ToggleMarker()
    {
        if (hasMarker)
        {
            ClearMarker();
        }
        else
        {
            PlaceMarker();
        }
    }

    public void TriggerMarker()
    {
        if (!hasMarker) return;

        // Store reference to cube before changing marker state
        CubeBehavior cubeToProcess = currentCube;

        // Change visual state to "activated"
        ActivateMarker();

        // Start a coroutine to reset the material after a delay
        StartCoroutine(ResetMarkerAfterDelay(0.5f));

        if (cubeToProcess == null)
        {
            Debug.LogWarning($"No cube to process on marker trigger at ({x}, {y}).");
            return;
        }

        // Handle cube type-specific behavior
        switch (cubeToProcess.type)
        {
            case Enumerations.CubeType.Black:
                // Black cube captured = immediate corruption
                BlackenTile();
                NotifyPlayerCubeCapture(Enumerations.CubeType.Black);
                // The black cube remains (not destroyed)
                break;

            case Enumerations.CubeType.Blue:
                // Blue cube captured = create detonation point
                Debug.Log($"Blue cube captured at ({x}, {y}) - Priming tile for detonation");
                NotifyPlayerCubeCapture(Enumerations.CubeType.Blue);
                PrimeTile(); // This will register with DetonationManager
                             // Consume the blue cube
                Destroy(cubeToProcess.gameObject);
                break;

            case Enumerations.CubeType.Normal:
                NotifyPlayerCubeCapture(Enumerations.CubeType.Normal);
                // Normal cubes are simply consumed
                Destroy(cubeToProcess.gameObject);
                break;
        }

        // Clear cube reference after processing
        currentCube = null;
    }

    private IEnumerator ResetMarkerAfterDelay(float delay)
    {
        Debug.Log($"Tile ({x},{y}): Starting marker reset delay of {delay} seconds");

        yield return new WaitForSeconds(delay);

        Debug.Log($"Tile ({x},{y}): Resetting marker after delay - isPlayerOnTile: {isPlayerOnTile}");

        // Now clear the marker state completely
        hasMarker = false;

        if (markerObj != null)
        {
            Destroy(markerObj);
            markerObj = null;
            Debug.Log($"Tile ({x},{y}): Marker object destroyed");
        }

        // CRITICAL: Update visuals through central system
        UpdateTileVisuals();

        // IMPORTANT: Restore soft highlight if player is still on this tile
        if (isPlayerOnTile)
        {
            Debug.Log($"Tile ({x},{y}): Player still on tile, restoring soft highlight");
            ShowSoftHighlight();
        }
        else
        {
            Debug.Log($"Tile ({x},{y}): Player not on tile, no highlight needed");
        }
    }


    private void ActivateMarker()
    {
        Debug.Log($"Tile ({x},{y}): ActivateMarker called - hasMarker before: {hasMarker}");

        // Hide the marker object temporarily but don't change hasMarker state yet
        if (markerObj != null)
        {
            markerObj.SetActive(false);
            Debug.Log($"Tile ({x},{y}): Marker object hidden");
        }

        if (tileRenderer != null && activateMarkerMaterial != null)
        {
            tileRenderer.material = activateMarkerMaterial;
            Debug.Log($"Tile ({x},{y}): Applied activation material");
        }
    }

    private void NotifyPlayerCubeCapture(Enumerations.CubeType cubeType)
    {

    }

    public void SetPlayerHover(bool isHovering)
    {
        if (isPlayerOnTile == isHovering) return; // No change needed

        Debug.Log($"Tile ({x},{y}): SetPlayerHover({isHovering}) - hasMarker={hasMarker}, isBlackened={isBlackened}, hasDetonationPoint={hasDetonationPoint}");

        isPlayerOnTile = isHovering;

        // Only show highlight if not marked, not blackened, and not a detonation point
        if (isHovering && !hasMarker && !isBlackened && !hasDetonationPoint)
        {
            ShowSoftHighlight();
        }
        else
        {
            HideSoftHighlight();
        }
    }

    private void ShowSoftHighlight()
    {
        if (isBlackened || hasMarker || hasDetonationPoint)
        {
            Debug.Log($"Tile ({x},{y}): Cannot show highlight - isBlackened={isBlackened}, hasMarker={hasMarker}, hasDetonationPoint={hasDetonationPoint}");
            return; // Don't highlight blackened, marked, or detonation point tiles
        }

        Debug.Log($"Tile ({x},{y}): Showing soft highlight");

        if (softHighlightObject == null)
        {
            // Create soft highlight object
            softHighlightObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            softHighlightObject.transform.SetParent(transform);
            softHighlightObject.transform.localPosition = new Vector3(0, 0.51f, 0); // Just above the tile
            softHighlightObject.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
            softHighlightObject.name = $"SoftHighlight_{x}_{y}";

            // Remove collider
            Collider highlightCollider = softHighlightObject.GetComponent<Collider>();
            if (highlightCollider != null)
            {
                Destroy(highlightCollider);
            }

            // Apply hover material
            Renderer highlightRenderer = softHighlightObject.GetComponent<Renderer>();
            if (highlightRenderer != null && playerHoverMaterial != null)
            {
                highlightRenderer.material = playerHoverMaterial;
            }

            Debug.Log($"Tile ({x},{y}): Created new soft highlight object");
        }

        // Activate the highlight object
        if (softHighlightObject != null)
        {
            softHighlightObject.SetActive(true);
            Debug.Log($"Tile ({x},{y}): Activated soft highlight object");
        }
    }

    private void HideSoftHighlight()
    {
        Debug.Log($"Tile ({x},{y}): Hiding soft highlight");

        if (softHighlightObject != null)
        {
            softHighlightObject.SetActive(false);
        }
    }

    // Tile state management methods
    public void TransformTile(Enumerations.CubeType cubeType)
    {
        if (currentState != Enumerations.TileState.Transformed)
        {
            currentState = Enumerations.TileState.Transformed;

            switch (cubeType)
            {
                case Enumerations.CubeType.Black:
                    BlackenTile();
                    break;
                case Enumerations.CubeType.Blue:
                    AdvantageTile();
                    break;
            }
        }
    }

    private void UpdateChargeVisuals()
    {
        if (tileRenderer != null && detonationCharges > 0 &&
            chargeMaterials != null && detonationCharges <= chargeMaterials.Length)
        {
            tileRenderer.material = chargeMaterials[detonationCharges - 1];
        }
        else if (tileRenderer != null && detonationCharges == 0)
        {
            tileRenderer.material = originalMaterial;
        }
    }

    public void ReduceCharge()
    {
        if (detonationCharges <= 0)
        {
            ResetToNormalState();
            return;
        }

        detonationCharges = isAdvantaged ? detonationCharges - 1 : 0;

        if (detonationCharges > 0)
        {
            UpdateChargeVisuals();
        }
        else
        {
            ResetToNormalState();
        }
    }

    private void ResetToNormalState()
    {
        currentState = TileState.Normal;
        isAdvantaged = false;
        detonationCharges = 0;

        if (tileRenderer != null)
        {
            tileRenderer.material = originalMaterial;
        }

    }

    public void ProcessCubeInteraction(CubeBehavior cube)
    {
        if (cube != null)
        {
            currentCube = cube;
        }
    }

    public void HandleCubeLanding(CubeBehavior cube)
    {
        if (cube == null || currentState != Enumerations.TileState.Transformed)
            return;

        if (IsBlackened)
        {
            // Black tiles have no effect
            return;
        }

        if (IsAdvantaged)
        {
            if (cube.type == Enumerations.CubeType.Black)
            {
                Debug.Log("Black cube landed on an advantaged tile. Charge Reduced.");
            }
            ReduceCharge();
        }

    }

    #region Material Management - Centralized System

    private void UpdateTileVisuals()
    {
        if (tileRenderer == null) return;

        Material targetMaterial = DetermineTileMaterial();

        if (targetMaterial != null && tileRenderer.material != targetMaterial)
        {
            tileRenderer.material = targetMaterial;
            Debug.Log($"Tile ({x},{y}): Material updated to {targetMaterial.name} - State: Blackened:{isBlackened},  Advantaged:{isAdvantaged}, HasMarker:{hasMarker}, HasDetonationPoint:{hasDetonationPoint}");
        }
    }

    private Material DetermineTileMaterial()
    {
        // Priority order for material selection

        // 1. Active marker state (highest priority - during trigger animation)
        if (hasMarker && activateMarkerMaterial != null)
        {
            return activateMarkerMaterial;
        }

        // 2. Marked state (player placed marker)
        if (hasMarker && markedTileMaterial != null)
        {
            return markedTileMaterial;
        }

        // 3. Blackened state
        if (isBlackened && forbiddenMaterial != null)
        {
            return forbiddenMaterial;
        }

        // 4. Advantaged state with charges
        if (isAdvantaged && detonationCharges > 0)
        {
            return GetChargeMaterial(detonationCharges);
        }

        // 5. Cube marker (detonation point)
        if (hasDetonationPoint && !isBlackened && !isAdvantaged)
        {
            return GetCubeMarkerMaterial();
        }

        // 6. Default state
        return originalMaterial;
    }

    private Material GetCubeMarkerMaterial()
    {
        // Create or return a blue material for cube markers
        Material blueMaterial = new Material(Shader.Find("Standard"));
        blueMaterial.color = Color.blue;
        blueMaterial.SetFloat("_Metallic", 0.3f);
        blueMaterial.SetFloat("_Smoothness", 0.7f);
        return blueMaterial;
    }

    private Material GetChargeMaterial(int charges)
    {
        if (chargeMaterials != null && charges > 0 && charges <= chargeMaterials.Length)
        {
            return chargeMaterials[charges - 1];
        }
        return originalMaterial;
    }

    public void ForceUpdateVisuals()
    {
        UpdateTileVisuals();
    }

    #endregion
}
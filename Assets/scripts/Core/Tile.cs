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

    [Header("Tile State")]
    [SerializeField] private bool hasFallen = false;
    public bool HasFallen => hasFallen;
    public bool IsPlayable => !hasFallen;

    [Header("Face Painting")]
    [SerializeField] public bool canPaintCubes = false;
    [SerializeField] private FaceStatus paintStatus = FaceStatus.None;
    [SerializeField] private Color paintColor = Color.red;
    [SerializeField] private int paintDuration = 3; // -1 for permanent
    [SerializeField] private bool paintOnLanding = true;
    [SerializeField] private bool paintOnExit = false;

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

    [Header("State Overlay System")]
    [SerializeField] private GameObject stateOverlay;
    [SerializeField] private float overlayHeight = 0.51f;
    [SerializeField] private Vector3 overlayScale = new Vector3(0.9f, 0.05f, 0.9f);

    // Colors for different states
    private readonly Color markerColor = Color.red;
    private readonly Color corruptedColor = Color.black;
    private readonly Color primedColor = Color.blue;
    private readonly Color enhancedColor = Color.yellow;

    // Properties to access charge information
    public int DetonationCharges => detonationCharges;
    public bool HasCharges => detonationCharges > 0;
    public bool IsBlackened => isBlackened;
    public bool IsAdvantaged => isAdvantaged;
    public bool IsPrimed => hasDetonationPoint;
    public bool HasMarker => hasMarker;
    public TileState currentState = TileState.Normal;
    public bool CanBeMarked => currentState == TileState.Normal && !isBlackened;

    // Face painting properties for external access
    public bool CanPaintCubes => canPaintCubes;
    public FaceStatus PaintStatus => paintStatus;
    public Color PaintColor => paintColor;
    public int PaintDuration => paintDuration;

    private GameObject markerObj;
    private Renderer tileRenderer;
    public CubeManager currentCube;
    private bool isInitialized = false;
    private bool isBlackened = false;
    private bool isAdvantaged = false;

    public bool isPhasedZone { get; private set; }
    private TextMesh countdownText;

    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
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

        RemoveOverlay();
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

        // Update visuals through overlay system
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

        // Update visuals through overlay system
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

    public void MakeTileFall()
    {
        if (hasFallen) return;

        hasFallen = true;

        // Clear any existing state
        ClearMarker();
        ResetTile();

        // Visual indication - make tile disappear or look destroyed
        MakeTileVisuallyFallen();

        Debug.Log($"Tile ({x},{y}) has fallen");
    }

    private void MakeTileVisuallyFallen()
    {
        // Option 1: Hide the tile completely
        gameObject.SetActive(false);
    }

    public void RestoreTile()
    {
        if (!hasFallen) return;

        hasFallen = false;
        gameObject.SetActive(true);
        ResetTile();

        Debug.Log($"Tile ({x},{y}) has been restored");
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
        RemoveOverlay();

        Debug.Log($"Tile ({x}, {y}) reset to normal state");
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
        CubeManager cubeToProcess = currentCube;

        // Change visual state to "activated"
        ActivateMarker();

        // Start a coroutine to reset the material after a delay
        StartCoroutine(ResetMarkerAfterDelay(0.5f));

        if (cubeToProcess == null)
        {
            Debug.LogWarning($"No cube to process on marker trigger at ({x}, {y}).");
            return;
        }

        // Use effective type instead of base type
        CubeType effectiveType = cubeToProcess.GetEffectiveType();
        bool canCapture = cubeToProcess.CanBeCaptured();

        Debug.Log($"Processing {cubeToProcess.type} cube (effective: {effectiveType}, can capture: {canCapture}) at ({x}, {y})");

        if (!canCapture)
        {
            Debug.Log($"Cube with {cubeToProcess.GetActiveFaceStatus()} face status cannot be captured");
            return;
        }

        // Handle cube type-specific behavior using effective type
        switch (effectiveType)
        {
            case Enumerations.CubeType.Black:
                // Cube is acting as black due to corrupted face
                Debug.Log($"Cube acting as black due to face status at ({x}, {y})");
                NotifyPlayerCubeCapture(Enumerations.CubeType.Black);
                // Don't destroy the cube - black cubes can't be captured
                break;

            case Enumerations.CubeType.Blue:
                // Cube is acting as blue (enhanced face or naturally blue)
                Debug.Log($"Cube acting as blue captured at ({x}, {y}) - Priming tile for detonation");
                NotifyPlayerCubeCapture(Enumerations.CubeType.Blue);
                PrimeTile();

                Destroy(cubeToProcess.gameObject);
                break;

            case Enumerations.CubeType.Normal:
                NotifyPlayerCubeCapture(Enumerations.CubeType.Normal);

                // Check if it should create detonation despite being normal
                if (cubeToProcess.ShouldCreateDetonation())
                {
                    Debug.Log("Normal cube creating detonation due to face status!");
                    PrimeTile();
                }

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

        // Update visuals through overlay system
        UpdateTileVisuals();

        // Restore soft highlight if player is still on this tile
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

        // Flash the overlay instead of changing tile material
        StartCoroutine(FlashOverlay());
    }

    private IEnumerator FlashOverlay()
    {
        // Create a temporary flash overlay
        GameObject flashOverlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
        flashOverlay.name = $"FlashOverlay_{x}_{y}";
        flashOverlay.transform.SetParent(transform);
        flashOverlay.transform.localPosition = new Vector3(0, overlayHeight + 0.01f, 0);
        flashOverlay.transform.localScale = new Vector3(0.95f, 0.02f, 0.95f);

        // Remove collider
        Destroy(flashOverlay.GetComponent<Collider>());

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

        Destroy(flashOverlay);
    }

    private void NotifyPlayerCubeCapture(Enumerations.CubeType cubeType)
    {
        // Implementation for notifying capture events
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

    public void TransformToPaintingTile(FaceStatus status, Color color, int duration = -1)
    {
        // Transform the tile visually
        TransformTile(Enumerations.CubeType.Blue); // Use blue transformation as base

        // Set up face painting
        SetupFacePainting(status, color, duration);

        Debug.Log($"Tile ({x},{y}) transformed to paint cubes with {status} status");
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
            UpdateTileVisuals(); // Update overlay for new charge level
        }
        else
        {
            ResetToNormalState();
        }
    }

    private void ResetToNormalState()
    {
        currentState = TileState.Normal;
        canPaintCubes = false;
        paintColor = Color.clear;
        paintStatus = FaceStatus.None;
        isAdvantaged = false;
        detonationCharges = 0;
        RemoveOverlay();
    }

    public void ProcessCubeInteraction(CubeManager cube)
    {
        if (cube != null)
        {
            currentCube = cube;
        }
    }

    public void HandleCubeLanding(CubeManager cube)
    {
        if (cube == null || currentState != Enumerations.TileState.Transformed)
            return;
        

        if (IsBlackened)
        {
            // Black tiles have no effect
            paintColor = Color.black;
            paintStatus = FaceStatus.Corrupted;
            ReduceCharge();
        }

        if (IsAdvantaged)
        {
            if (cube.type == Enumerations.CubeType.Black)
            {
                Debug.Log("Black cube landed on an advantaged tile. Charge Reduced.");
            }
            ReduceCharge();
        }

        TryPaintCube(cube);

    }

    #region Overlay System - Replaces Material Management

    private void UpdateTileVisuals()
    {
        UpdateStateOverlay();
    }

    private void UpdateStateOverlay()
    {
        // Determine if we need an overlay and what color
        (bool needsOverlay, Color overlayColor) = DetermineOverlayState();

        if (needsOverlay)
        {
            CreateOrUpdateOverlay(overlayColor);
        }
        else
        {
            RemoveOverlay();
        }
    }

    private (bool needsOverlay, Color color) DetermineOverlayState()
    {
        // Priority order - return first match
        if (hasMarker)
            return (true, markerColor);

        if (isBlackened)
            return (true, corruptedColor);

        if (hasDetonationPoint)
            return (true, primedColor);

        if (isAdvantaged && detonationCharges > 0)
        {
            // Different shades of yellow based on charge level
            float intensity = (float)detonationCharges / maxCharges;
            Color chargedColor = Color.Lerp(new Color(1f, 1f, 0.3f), enhancedColor, intensity);
            return (true, chargedColor);
        }

        return (false, Color.white); // No overlay needed
    }

    private void CreateOrUpdateOverlay(Color color)
    {
        if (stateOverlay == null)
        {
            // Create overlay object
            stateOverlay = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stateOverlay.name = $"StateOverlay_{x}_{y}";
            stateOverlay.transform.SetParent(transform);

            // Position on top of tile
            stateOverlay.transform.localPosition = new Vector3(0, overlayHeight, 0);
            stateOverlay.transform.localScale = overlayScale;

            // Remove collider to avoid physics issues
            Destroy(stateOverlay.GetComponent<Collider>());

            Debug.Log($"Created state overlay for tile ({x}, {y})");
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

    private void RemoveOverlay()
    {
        if (stateOverlay != null)
        {
            Destroy(stateOverlay);
            stateOverlay = null;
        }
    }

    public void ForceUpdateVisuals()
    {
        UpdateTileVisuals();
    }

    #endregion

    #region Face Painting System

    public void SetupFacePainting(FaceStatus status, Color color, int duration = -1, bool onLanding = true, bool onExit = false)
    {
        canPaintCubes = true;
        paintStatus = status;
        paintColor = color;
        paintDuration = duration;
        paintOnLanding = onLanding;
        paintOnExit = onExit;

        Debug.Log($"Tile ({x},{y}) set up to paint cubes with {status} status");
    }

    public void TryPaintCube(CubeManager cube)
    {
        if (!canPaintCubes || cube == null || paintStatus == FaceStatus.None) return;

        if (paintOnLanding)
        {
            PaintCube(cube);
        }
    }

    public void TryPaintCubeOnExit(CubeManager cube)
    {
        if (!canPaintCubes || cube == null || paintStatus == FaceStatus.None) return;

        if (paintOnExit)
        {
            PaintCube(cube);
        }
    }

    private void PaintCube(CubeManager cube)
    {
        // Paint the cube's currently down-facing face
        cube.PaintCurrentDownFace(paintStatus, paintColor, paintDuration);

        // Visual feedback effect
        CreatePaintEffect(cube.transform.position);

        Debug.Log($"Tile ({x},{y}) painted {cube.name} with {paintStatus} status");
    }

    private void CreatePaintEffect(Vector3 position)
    {
        // Create a simple particle effect or visual feedback
        GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        effect.name = "PaintEffect";
        effect.transform.position = position + Vector3.up * 0.5f;
        effect.transform.localScale = Vector3.one * 0.3f;

        // Remove collider
        Destroy(effect.GetComponent<Collider>());

        // Set color and make it fade
        Renderer renderer = effect.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = paintColor;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", paintColor * 0.5f);
        renderer.material = mat;

        // Animate and destroy
        StartCoroutine(AnimatePaintEffect(effect));
    }

    private IEnumerator AnimatePaintEffect(GameObject effect)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 startScale = effect.transform.localScale;
        Vector3 startPos = effect.transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Scale up and fade out
            effect.transform.localScale = Vector3.Lerp(startScale, startScale * 2f, t);
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

    // Quick setup methods for common painting scenarios
    public void SetupCorruptionPainting(int duration = 3)
    {
        SetupFacePainting(FaceStatus.Corrupted, Color.black, duration);
    }

    public void SetupEnhancementPainting(int duration = 3)
    {
        SetupFacePainting(FaceStatus.Enhanced, Color.blue, duration);
    }

    public void DisableFacePainting()
    {
        canPaintCubes = false;
        paintStatus = FaceStatus.None;
    }

    #endregion
}
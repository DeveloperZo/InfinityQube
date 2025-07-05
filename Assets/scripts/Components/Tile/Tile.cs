using System;
using System.Collections;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Main tile class that acts as a facade for tile functionality.
/// Delegates specific responsibilities to specialized components.
/// </summary>
public class Tile : MonoBehaviour
{
    #region Inspector Fields
    [Header("Tile Properties")]
    [SerializeField] public int x, y;
    [SerializeField] private float markerHeight = 0.5f;
    [SerializeField] private float markerScale = 0.8f;

    [Header("Tile State")]
    [SerializeField] private bool hasFallen = false;

    [Header("Face Painting")]
    [SerializeField] private FaceStatus paintStatus = FaceStatus.None;
    [SerializeField] private Color paintColor = Color.red;
    [SerializeField] private int paintDuration = 3; // -1 for permanent
    [SerializeField] private bool paintOnLanding = true;
    [SerializeField] private bool paintOnExit = false;

    [Header("Corruption System")]
    [SerializeField] private int corruptionDuration = 5;
    [SerializeField] private int maxCorruptionInteractions = 3;
    [SerializeField] private bool showCorruptionCountdown = true;

    [Header("Detonation Point")]
    [SerializeField] private bool hasDetonationPoint = false;

    [Header("Player Hover Effect")]
    [SerializeField] private bool isPlayerOnTile = false;
    [SerializeField] private GameObject softHighlightObject;
    [SerializeField] private Material playerHoverMaterial;
    #endregion

    #region Component Delegates
    private TileVisuals tileVisuals;
    private TileMarker tileMarker;
    private TileCorruption tileCorruption;
    private TileFacePainting tileFacePainting;
    #endregion

    #region Private Fields
    private Renderer tileRenderer;
    private bool isInitialized = false;
    private bool isBlackened = false;
    #endregion

    #region Public Properties
    // Tile state properties
    public bool HasFallen => hasFallen;
    public bool IsPlayable => !hasFallen;
    public bool IsBlackened => isBlackened;
    public bool IsPrimed => hasDetonationPoint;
    public bool HasDetonationPoint => hasDetonationPoint;
    public bool isPhasedZone { get; private set; }
    public TileState currentState = TileState.Normal;
    public CubeManager currentCube;

    // Delegated properties
    public bool HasMarker => tileMarker?.HasMarker ?? false;
    public bool IsCorrupted => tileCorruption?.IsCorrupted ?? false;
    public bool CanBeMarked => currentState == TileState.Normal && !isBlackened && !IsCorrupted;
    public bool CanAcceptMarkers => !IsCorrupted;

    // Face painting properties
    public bool CanPaintCubes => tileFacePainting?.CanPaintCubes ?? false;
    public FaceStatus PaintStatus => tileFacePainting?.PaintStatus ?? FaceStatus.None;
    public Color PaintColor => tileFacePainting?.PaintColor ?? Color.clear;
    public int PaintDuration => tileFacePainting?.PaintDuration ?? 0;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        
        // Initialize components
        tileVisuals = new TileVisuals(transform);
        tileMarker = new TileMarker(transform, tileVisuals, this);
        tileCorruption = new TileCorruption(transform, this, showCorruptionCountdown);
        tileFacePainting = new TileFacePainting(transform, this);
        
        // Set initial face painting configuration if needed
        if (paintStatus != FaceStatus.None)
        {
            tileFacePainting.SetupFacePainting(paintStatus, paintColor, paintDuration, paintOnLanding, paintOnExit);
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
        
        // Update marker configuration if needed
        tileMarker?.UpdateConfiguration(markerHeight, markerScale);
    }

    private void OnDestroy()
    {
        // Clean up components
        tileMarker?.OnDestroy();
        tileCorruption?.OnDestroy();
        tileVisuals?.OnDestroy();

        if (softHighlightObject != null)
        {
            Destroy(softHighlightObject);
            softHighlightObject = null;
        }
    }
    #endregion

    #region Marker Management
    public void PlaceMarker()
    {
        if (!isInitialized) return;

        tileMarker?.PlaceMarker(CanBeMarked, CanAcceptMarkers);
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
        if (!HasMarker) return;

        Debug.Log($"Tile ({x},{y}): ClearMarker called");
        tileMarker?.ClearMarker();
        UpdateTileVisuals();

        // Restore soft highlight if player is still on this tile
        if (isPlayerOnTile)
        {
            Debug.Log($"Tile ({x},{y}): After clearing marker, restoring soft highlight");
            ShowSoftHighlight();
        }

        Debug.Log($"Tile ({x},{y}): Marker cleared successfully");
    }

    public void ToggleMarker()
    {
        if (HasMarker)
            ClearMarker();
        else
            PlaceMarker();
    }

    public void TriggerMarker()
    {
        if (!HasMarker) return;

        CubeManager cubeToProcess = currentCube;
        ActivateMarker();
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

        // Handle cube type-specific behavior
        switch (effectiveType)
        {
            case Enumerations.CubeType.Infinity:
                Debug.Log($"Cube acting as black due to face status at ({x}, {y})");
                NotifyPlayerCubeCapture(Enumerations.CubeType.Infinity);
                break;

            case Enumerations.CubeType.Prime:
                Debug.Log($"Prime cube captured at ({x}, {y}) - Creating prime cube marker");
                NotifyPlayerCubeCapture(Enumerations.CubeType.Prime);
                PrimeTile();
                Destroy(cubeToProcess.gameObject);
                break;

            case Enumerations.CubeType.Unit:
                NotifyPlayerCubeCapture(Enumerations.CubeType.Unit);
                if (cubeToProcess.ShouldCreateDetonation())
                {
                    Debug.Log("Normal cube creating detonation due to face status!");
                    PrimeTile();
                }
                Destroy(cubeToProcess.gameObject);
                break;

            case Enumerations.CubeType.Recursion:
                HandleReinforcedCube(cubeToProcess);
                break;
        }

        currentCube = null;
    }

    private void ActivateMarker()
    {
        Debug.Log($"Tile ({x},{y}): ActivateMarker called - hasMarker before: {HasMarker}");
        tileMarker?.ActivateMarker();
    }

    private IEnumerator ResetMarkerAfterDelay(float delay)
    {
        Debug.Log($"Tile ({x},{y}): Starting marker reset delay of {delay} seconds");
        yield return new WaitForSeconds(delay);
        Debug.Log($"Tile ({x},{y}): Resetting marker after delay - isPlayerOnTile: {isPlayerOnTile}");

        tileMarker?.ResetMarkerAfterDelay();
        UpdateTileVisuals();

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
    #endregion

    #region Tile State Management
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
        ClearMarker();
        ResetTile();

        // Visual indication - make tile disappear
        gameObject.SetActive(false);
        Debug.Log($"Tile ({x},{y}) has fallen");
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
        ClearMarker();
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

    public void ResetTile()
    {
        currentState = TileState.Normal;
        isBlackened = false;
        hasDetonationPoint = false;
        ClearMarker();
        CleanseCorruption();
        tileVisuals?.RemoveOverlay();

        Debug.Log($"Tile ({x}, {y}) reset to normal state");
    }

    public void ResetTileAppearance()
    {
        UpdateTileVisuals();
    }

    public void TransformTile(Enumerations.CubeType cubeType)
    {
        if (currentState != Enumerations.TileState.Transformed)
        {
            currentState = Enumerations.TileState.Transformed;

            if (cubeType == Enumerations.CubeType.Infinity)
            {
                BlackenTile();
            }
        }
    }

    public void TransformToPaintingTile(FaceStatus status, Color color, int duration = -1)
    {
        currentState = Enumerations.TileState.Transformed;
        SetupFacePainting(status, color, duration);
        Debug.Log($"Tile ({x},{y}) transformed to paint cubes with {status} status");
    }

    private void ResetToNormalState()
    {
        currentState = TileState.Normal;
        tileFacePainting?.DisableFacePainting();
        tileVisuals?.RemoveOverlay();
    }
    #endregion

    #region Player Hover Effects
    public void SetPlayerHover(bool isHovering)
    {
        if (isPlayerOnTile == isHovering) return;

        Debug.Log($"Tile ({x},{y}): SetPlayerHover({isHovering}) - hasMarker={HasMarker}, isBlackened={isBlackened}, hasDetonationPoint={hasDetonationPoint}");

        isPlayerOnTile = isHovering;

        if (isHovering && !HasMarker && !isBlackened && !hasDetonationPoint && !IsCorrupted)
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
        if (isBlackened || HasMarker || hasDetonationPoint || IsCorrupted)
        {
            Debug.Log($"Tile ({x},{y}): Cannot show highlight - isBlackened={isBlackened}, hasMarker={HasMarker}, hasDetonationPoint={hasDetonationPoint}, isCorrupted={IsCorrupted}");
            return;
        }

        Debug.Log($"Tile ({x},{y}): Showing soft highlight");

        if (softHighlightObject == null)
        {
            softHighlightObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            softHighlightObject.transform.SetParent(transform);
            softHighlightObject.transform.localPosition = new Vector3(0, 0.51f, 0);
            softHighlightObject.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
            softHighlightObject.name = $"SoftHighlight_{x}_{y}";

            Collider highlightCollider = softHighlightObject.GetComponent<Collider>();
            if (highlightCollider != null)
            {
                Destroy(highlightCollider);
            }

            Renderer highlightRenderer = softHighlightObject.GetComponent<Renderer>();
            if (highlightRenderer != null && playerHoverMaterial != null)
            {
                highlightRenderer.material = playerHoverMaterial;
            }

            Debug.Log($"Tile ({x},{y}): Created new soft highlight object");
        }

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
    #endregion

    #region Cube Interactions
    public void ProcessCubeInteraction(CubeManager cube)
    {
        if (cube != null)
        {
            currentCube = cube;
        }
    }

    public void HandleCubeLanding(CubeManager cube)
    {
        if (cube == null) return;

        currentCube = cube;
        
        // Check for corruption from infinity cubes with painted faces
        if (cube.type == CubeType.Infinity && cube.GetActiveFaceStatus() == FaceStatus.Corrupted)
        {
            CorruptTile(corruptionDuration, maxCorruptionInteractions);
            Debug.Log($"Tile ({x},{y}) corrupted by infinity cube with painted face");
        }
        
        // Handle corrupted tile behavior
        if (IsCorrupted && cube.type != CubeType.Infinity)
        {
            cube.PaintCurrentDownFace(FaceStatus.Corrupted, Color.black, -1);
            tileCorruption.IncrementInteraction();
            Debug.Log($"Corrupted tile painted {cube.type} cube at ({x},{y}). Interactions: {tileCorruption.CorruptionInteractions}/{tileCorruption.MaxCorruptionInteractions}");
            
            if (tileCorruption.ShouldCleanseFromInteractions())
            {
                CleanseCorruption();
                Debug.Log($"Tile ({x},{y}) corruption cleansed due to interaction limit");
            }
        }
        
        // Handle transformed tile behavior
        if (currentState == Enumerations.TileState.Transformed && IsBlackened)
        {
            tileFacePainting?.UpdateForTransformedState(IsBlackened);
        }

        // Handle face painting
        TryPaintCube(cube);
        NotifyFacePaintingManager(cube);
    }

    private void HandleReinforcedCube(CubeManager cube)
    {
        if (cube == null) return;

        bool wasDestroyed = cube.TakeDamage();
        
        if (wasDestroyed)
        {
            Debug.Log($"Reinforced cube destroyed at ({x}, {y}) after taking damage");
            NotifyPlayerCubeCapture(Enumerations.CubeType.Recursion);
            
            WaveManager waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.OnNonBlackCubeProcessed(Enumerations.CubeType.Recursion, true);
            }
            
            Destroy(cube.gameObject);
        }
        else
        {
            Debug.Log($"Reinforced cube damaged at ({x}, {y}), HP: {cube.currentHitPoints}/{cube.maxHitPoints}");
        }
    }

    private void NotifyPlayerCubeCapture(Enumerations.CubeType cubeType)
    {
        WaveManager waveManager = FindObjectOfType<WaveManager>();
        if (waveManager != null)
        {
            waveManager.OnCubeCaptured(cubeType);
            Debug.Log($"Tile ({x},{y}): Notified cube capture of type {cubeType}");
        }
        else
        {
            Debug.LogWarning($"Tile ({x},{y}): WaveManager not found for cube capture notification");
        }
    }

    private void NotifyFacePaintingManager(CubeManager cube)
    {
        if (CanPaintCubes)
        {
            tileFacePainting?.NotifyFacePaintingManager(cube, new Vector2Int(x, y));
        }
    }
    #endregion

    #region Visual Management
    private void UpdateTileVisuals()
    {
        tileVisuals?.UpdateStateOverlay(HasMarker, IsCorrupted, isBlackened, hasDetonationPoint);
    }

    public void ForceUpdateVisuals()
    {
        UpdateTileVisuals();
    }
    #endregion

    #region Face Painting System
    public void SetupFacePainting(FaceStatus status, Color color, int duration = -1, bool onLanding = true, bool onExit = false)
    {
        tileFacePainting?.SetupFacePainting(status, color, duration, onLanding, onExit);
    }

    public void TryPaintCube(CubeManager cube)
    {
        tileFacePainting?.TryPaintCube(cube);
    }

    public void TryPaintCubeOnExit(CubeManager cube)
    {
        tileFacePainting?.TryPaintCubeOnExit(cube);
    }

    public void SetupCorruptionPainting(int duration = 3)
    {
        tileFacePainting?.SetupCorruptionPainting(duration);
    }

    public void SetupEnhancementPainting(int duration = 3)
    {
        tileFacePainting?.SetupEnhancementPainting(duration);
    }

    public void DisableFacePainting()
    {
        tileFacePainting?.DisableFacePainting();
    }
    #endregion

    #region Corruption System
    public void CorruptTile(int duration = 5, int maxInteractions = 3)
    {
        tileCorruption?.CorruptTile(duration, maxInteractions);
        ClearMarker();
        SetupCorruptionPainting(duration);
        UpdateTileVisuals();
    }
    
    public void CleanseCorruption()
    {
        tileCorruption?.CleanseCorruption();
        DisableFacePainting();
        UpdateTileVisuals();
    }
    
    public void ProcessCorruptionDecay()
    {
        tileCorruption?.ProcessCorruptionDecay();
        
        if (!IsCorrupted)
        {
            DisableFacePainting();
            UpdateTileVisuals();
        }
    }
    
    public string GetCorruptionStatus()
    {
        return tileCorruption?.GetCorruptionStatus() ?? "Clean";
    }
    #endregion
}

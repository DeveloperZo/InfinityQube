using System;
using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Color currentHoverColor = new Color(0.5f, 0.6f, 0.7f, 0.5f); // Default: Unit blue-gray
    #endregion

    #region Component Delegates
    private TileVisuals tileVisuals;
    private TileMarker tileMarker;
    private TileCorruption tileCorruption;
    #endregion

    #region Private Fields
    private Renderer tileRenderer;
    private bool isInitialized = false;
    private bool isBlackened = false;
    
    // Cached manager references
    private GridManager cachedGridManager;
    private WaveManager cachedWaveManager;
    private PlayerActionManager cachedPlayerActionManager;
    #endregion

    #region Public Properties
    // Tile state properties
    public bool HasFallen => hasFallen;
    public bool IsPlayable => !hasFallen;
    public bool IsBlackened => isBlackened;
    public bool IsMatrixd => hasDetonationPoint;
    public bool HasDetonationPoint => hasDetonationPoint;
    public bool isPhasedZone { get; private set; }
    public TileState currentState = TileState.Normal;
    public CubeManager currentCube;

    // Delegated properties
    public bool HasMarker => tileMarker?.HasMarker ?? false;
    public bool IsCorrupted => tileCorruption?.IsCorrupted ?? false;
    public bool CanBeMarked => currentState == TileState.Normal && !isBlackened && !IsCorrupted;
    public bool CanAcceptMarkers => !IsCorrupted;

    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        
        // Initialize components
        tileVisuals = new TileVisuals(transform);
        tileMarker = new TileMarker(transform, tileVisuals, this);
        tileCorruption = new TileCorruption(transform, this, showCorruptionCountdown);
    }

    public void Init(int xPos, int yPos)
    {
        x = xPos;
        y = yPos;
        isInitialized = true;
        
        // Cache manager references (Task 6/7/8) - done once per tile
        cachedGridManager = GridManager.Instance;
        cachedWaveManager = FindFirstObjectByType<WaveManager>();
        cachedPlayerActionManager = FindFirstObjectByType<PlayerActionManager>();

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
            case CubeType.Infinity:
                Debug.Log($"Cube acting as black due to face status at ({x}, {y})");
                NotifyPlayerCubeCapture(CubeType.Infinity);
                break;

            case CubeType.Matrix:
                Debug.Log($"Matrix cube captured at ({x}, {y}) - Creating matrix cube marker");
                NotifyPlayerCubeCapture(CubeType.Matrix);
                MatrixTile();
                Destroy(cubeToProcess.gameObject);
                break;

            case CubeType.Unit:
                NotifyPlayerCubeCapture(CubeType.Unit);
                if (cubeToProcess.ShouldCreateDetonation())
                {
                    Debug.Log("Normal cube creating detonation due to face status!");
                    MatrixTile();
                }
                Destroy(cubeToProcess.gameObject);
                break;

            case CubeType.Recursion:
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

    /// <summary>
    /// Event fired when tile starts falling (for animation hooks)
    /// </summary>
    public System.Action<Tile> OnTileFallStarted;
    
    /// <summary>
    /// Event fired when tile fall completes (for animation hooks)
    /// </summary>
    public System.Action<Tile> OnTileFallCompleted;
    
    public void MakeTileFall()
    {
        if (hasFallen) return;

        // Fire start event (for future animation systems)
        OnTileFallStarted?.Invoke(this);
        
        hasFallen = true;
        ClearMarker();
        ResetTile();

        // Visual indication - make tile disappear
        // Note: Actual visual transition is handled by GridManager.RemoveBottomRowCoroutine()
        // This method is called after the transition completes
        gameObject.SetActive(false);
        
        // Fire completion event (for future animation systems)
        OnTileFallCompleted?.Invoke(this);
        
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

    public void MatrixTile()
    {
        if (isBlackened) return;
        hasDetonationPoint = true;

        ClearMarker();
        UpdateTileVisuals();

        // Register with PlayerActionManager
        // Default size is 2x2 for Matrix tile detonation (from marker capture)
        PlayerActionManager playerActionManager = FindFirstObjectByType<PlayerActionManager>();
        if (playerActionManager != null)
        {
            playerActionManager.CreateCubeMarker(new Vector2Int(x, y), PlayerMarkerSystem.CubeMarkerType.Matrix, 2);
        }

        Debug.Log($"Tile ({x},{y}): Matrixd for detonation and registered with PlayerActionManager");
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

    public void TransformTile(CubeType cubeType)
    {
        if (currentState != TileState.Transformed)
        {
            currentState = TileState.Transformed;

            if (cubeType == CubeType.Infinity)
            {
                BlackenTile();
            }
        }
    }

    private void ResetToNormalState()
    {
        currentState = TileState.Normal;
        tileVisuals?.RemoveOverlay();
    }
    #endregion

    #region Player Hover Effects
    public void SetPlayerHover(bool isHovering)
    {
        if (isPlayerOnTile == isHovering) return;

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

    /// <summary>
    /// Updates the hover highlight color based on current marker mode.
    /// Call this when the player's selected marker mode changes.
    /// </summary>
    public void SetHoverColor(Color color)
    {
        currentHoverColor = color;
        
        // Update existing highlight if visible
        if (softHighlightObject != null && softHighlightObject.activeSelf)
        {
            Renderer highlightRenderer = softHighlightObject.GetComponent<Renderer>();
            if (highlightRenderer != null)
            {
                highlightRenderer.material.color = color;
            }
        }
    }

    private void ShowSoftHighlight()
    {
        if (isBlackened || HasMarker || hasDetonationPoint || IsCorrupted)
        {
            return;
        }

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
            if (highlightRenderer != null)
            {
                // Create material with current hover color
                Material hoverMat = new Material(Shader.Find("Standard"));
                hoverMat.color = currentHoverColor;
                highlightRenderer.material = hoverMat;
            }
        }
        else
        {
            // Update color on existing highlight
            Renderer highlightRenderer = softHighlightObject.GetComponent<Renderer>();
            if (highlightRenderer != null)
            {
                highlightRenderer.material.color = currentHoverColor;
            }
        }

        if (softHighlightObject != null)
        {
            softHighlightObject.SetActive(true);
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
        
        // Handle corrupted tile behavior - corrupted tiles affect non-Infinity cubes
        if (IsCorrupted && cube.type != CubeType.Infinity)
        {
            cube.PaintCurrentDownFace(FaceStatus.InfinityFace, Color.black, -1);
            tileCorruption.IncrementInteraction();
            Debug.Log($"Corrupted tile painted {cube.type} cube at ({x},{y}). Interactions: {tileCorruption.CorruptionInteractions}/{tileCorruption.MaxCorruptionInteractions}");
            
            if (tileCorruption.ShouldCleanseFromInteractions())
            {
                CleanseCorruption();
                Debug.Log($"Tile ({x},{y}) corruption cleansed due to interaction limit");
            }
        }
        
        // Note: Face painting system has been removed (Jan 2026)
        // Infinity cubes are now truly immutable - resonance triggers immediately on Infinity+Infinity collision
    }
    #endregion
    
    #region Cube Capture Helpers
    private void HandleReinforcedCube(CubeManager cube)
    {
        if (cube == null) return;

        bool wasDestroyed = cube.TakeDamage();
        
        if (wasDestroyed)
        {
            Debug.Log($"Reinforced cube destroyed at ({x}, {y}) after taking damage");
            NotifyPlayerCubeCapture(CubeType.Recursion);
            
            WaveManager waveManager = FindFirstObjectByType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.OnNonBlackCubeProcessed(CubeType.Recursion, true);
            }
            
            Destroy(cube.gameObject);
        }
        else
        {
            Debug.Log($"Reinforced cube damaged at ({x}, {y}), HP: {cube.currentHitPoints}/{cube.maxHitPoints}");
        }
    }

    private void NotifyPlayerCubeCapture(CubeType cubeType)
    {
        WaveManager waveManager = FindFirstObjectByType<WaveManager>();
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

    #region Corruption System
    public void CorruptTile(int duration = 5, int maxInteractions = 3)
    {
        tileCorruption?.CorruptTile(duration, maxInteractions);
        ClearMarker();
        UpdateTileVisuals();
    }
    
    public void CleanseCorruption()
    {
        tileCorruption?.CleanseCorruption();
        UpdateTileVisuals();
    }
    
    public void ProcessCorruptionDecay()
    {
        tileCorruption?.ProcessCorruptionDecay();
        
        if (!IsCorrupted)
        {
            UpdateTileVisuals();
        }
    }
    
    public string GetCorruptionStatus()
    {
        return tileCorruption?.GetCorruptionStatus() ?? "Clean";
    }
    #endregion
}

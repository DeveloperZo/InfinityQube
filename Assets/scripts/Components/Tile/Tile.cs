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
    
    [Header("Task 8: Face Painting Telegraph")]
    [SerializeField] private GameObject telegraphObject; // Visual indicator for painted face grid touch
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
    
    // Cached manager references (Task 6/7/8)
    private GridManager cachedGridManager;
    private WaveManager cachedWaveManager;
    private PlayerActionManager cachedPlayerActionManager;
    private Coroutine telegraphPulseCoroutine;
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
        
        // Cache manager references (Task 6/7/8) - done once per tile
        cachedGridManager = GridManager.Instance;
        cachedWaveManager = FindObjectOfType<WaveManager>();
        cachedPlayerActionManager = FindObjectOfType<PlayerActionManager>();

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

    public void MatrixTile()
    {
        if (isBlackened) return;
        hasDetonationPoint = true;

        ClearMarker();
        UpdateTileVisuals();

        // Register with PlayerActionManager
        // Default size is 2x2 for Matrix tile detonation (from marker capture)
        PlayerActionManager playerActionManager = FindObjectOfType<PlayerActionManager>();
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

    public void TransformToPaintingTile(FaceStatus status, Color color, int duration = -1)
    {
        currentState = TileState.Transformed;
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
        
        // Task 8: Grid touch detection for painted faces
        // Check if a painted face has just touched the grid (become the down face)
        FaceStatus activeFaceStatus = cube.GetActiveFaceStatus();
        if (activeFaceStatus != FaceStatus.None)
        {
            HandlePaintedFaceGridTouch(cube, activeFaceStatus);
        }
        
        // Check for corruption from infinity cubes with painted faces
        if (cube.type == CubeType.Infinity && cube.GetActiveFaceStatus() == FaceStatus.InfinityFace)
        {
            CorruptTile(corruptionDuration, maxCorruptionInteractions);
            Debug.Log($"Tile ({x},{y}) corrupted by infinity cube with painted face");
        }
        
        // Handle corrupted tile behavior
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
        
        // Handle transformed tile behavior
        if (currentState == TileState.Transformed && IsBlackened)
        {
            tileFacePainting?.UpdateForTransformedState(IsBlackened);
        }

        // Handle face painting
        TryPaintCube(cube);
        NotifyFacePaintingManager(cube);
        
        // Task 8: Update telegraph for painted faces that will touch grid soon
        UpdatePaintedFaceTelegraph(cube);
    }
    
    /// <summary>
    /// Task 8: Updates telegraph visualization for painted faces that will touch grid in 1 turn
    /// </summary>
    private void UpdatePaintedFaceTelegraph(CubeManager cube)
    {
        if (cube == null) return;
        
        // Check if a painted face will touch grid in 1 move
        if (cube.WillPaintedFaceTouchGrid(1))
        {
            FaceStatus predictedStatus = cube.GetPredictedFaceStatus(1);
            ShowTelegraphEffect(predictedStatus);
            Debug.Log($"[Task 8] Telegraph: Painted face ({predictedStatus}) will touch grid at ({x},{y}) in 1 move");
        }
        else
        {
            HideTelegraphEffect();
        }
    }
    
    /// <summary>
    /// Task 8: Shows telegraph pulse effect on tile
    /// </summary>
    private void ShowTelegraphEffect(FaceStatus faceStatus)
    {
        // Create telegraph visual if needed
        if (telegraphObject == null)
        {
            telegraphObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            telegraphObject.name = $"Telegraph_{x}_{y}";
            telegraphObject.transform.SetParent(transform);
            telegraphObject.transform.localPosition = new Vector3(0, 0.52f, 0);
            telegraphObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
            telegraphObject.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            
            // Remove collider
            Destroy(telegraphObject.GetComponent<Collider>());
            
            // Set up transparent material
            Renderer rend = telegraphObject.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            rend.material = mat;
        }
        
        // Set color based on face status
        Color telegraphColor = GetTelegraphColor(faceStatus);
        telegraphColor.a = 0.6f;
        
        Renderer renderer = telegraphObject.GetComponent<Renderer>();
        if (renderer != null && renderer.material != null)
        {
            renderer.material.color = telegraphColor;
        }
        
        telegraphObject.SetActive(true);
        
        // Stop existing pulse and start new one
        if (telegraphPulseCoroutine != null)
        {
            StopCoroutine(telegraphPulseCoroutine);
        }
        telegraphPulseCoroutine = StartCoroutine(PulseTelegraph());
    }
    
    /// <summary>
    /// Task 8: Hides telegraph effect
    /// </summary>
    private void HideTelegraphEffect()
    {
        if (telegraphObject != null)
        {
            telegraphObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Task 8: Gets telegraph color based on face status
    /// </summary>
    private Color GetTelegraphColor(FaceStatus faceStatus)
    {
        switch (faceStatus)
        {
            case FaceStatus.MatrixFace:
                return new Color(0.3f, 0.7f, 1f); // Light blue for Matrix
            case FaceStatus.RecursionFace:
                return new Color(0.8f, 0.5f, 0.2f); // Amber for Recursion
            case FaceStatus.InfinityFace:
                return new Color(0.2f, 0.2f, 0.2f); // Dark for Infinity
            default:
                return Color.white;
        }
    }
    
    /// <summary>
    /// Task 8: Pulses the telegraph effect
    /// </summary>
    private System.Collections.IEnumerator PulseTelegraph()
    {
        if (telegraphObject == null) yield break;
        
        float pulseSpeed = 2f;
        float minAlpha = 0.3f;
        float maxAlpha = 0.8f;
        float elapsed = 0f;
        
        Renderer renderer = telegraphObject.GetComponent<Renderer>();
        if (renderer == null || renderer.material == null) yield break;
        
        while (telegraphObject != null && telegraphObject.activeSelf)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(elapsed * pulseSpeed) + 1f) / 2f);
            
            Color color = renderer.material.color;
            color.a = alpha;
            renderer.material.color = color;
            
            yield return null;
        }
    }
    
    /// <summary>
    /// Task 8: Handles painted face grid touch detection and triggers appropriate effects
    /// </summary>
    private void HandlePaintedFaceGridTouch(CubeManager cube, FaceStatus faceStatus)
    {
        if (cachedPlayerActionManager == null) return;
        
        Vector2Int centerPosition = new Vector2Int(x, y);
        
        switch (faceStatus)
        {
            case FaceStatus.MatrixFace:
                Debug.Log($"[Task 8] MatrixFace touched grid at ({x},{y}) - creating 3x3 detonation marker");
                cachedPlayerActionManager.CreateCubeMarker(centerPosition, PlayerMarkerSystem.CubeMarkerType.Matrix, 3);
                ApplyLineDividerReward(1); // Task 6: Reward for painted face trigger
                break;
                
            case FaceStatus.RecursionFace:
                Debug.Log($"[Task 8] RecursionFace touched grid at ({x},{y}) - creating 5 tile cross marker");
                CreateRecursionCrossMarker(centerPosition);
                ApplyLineDividerReward(1); // Task 6: Reward for painted face trigger
                break;
                
            case FaceStatus.InfinityFace:
                Debug.Log($"[Task 8] InfinityFace touched grid at ({x},{y}) - triggering resonance system");
                TriggerResonanceEffect();
                ApplyLineDividerReward(2); // Task 6: Reward for resonance trigger (2 rows)
                break;
        }
    }
    
    /// <summary>
    /// Task 8: Creates a 5-tile cross marker pattern for RecursionFace
    /// Creates an auto-capture area marker (doesn't consume player marker charges)
    /// </summary>
    private void CreateRecursionCrossMarker(Vector2Int centerPosition)
    {
        if (cachedPlayerActionManager == null || cachedGridManager == null) return;
        
        // Build cross pattern: center + 4 adjacent tiles
        List<Vector2Int> crossPositions = new List<Vector2Int>();
        Vector2Int[] offsets = { Vector2Int.zero, Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        
        foreach (var offset in offsets)
        {
            Vector2Int pos = centerPosition + offset;
            if (cachedGridManager.IsValidGridPosition(pos))
            {
                crossPositions.Add(pos);
            }
        }
        
        // Create auto-capture cross marker via PlayerMarkerSystem
        // This creates a visual marker that auto-captures cubes passing through
        var markerSystem = cachedPlayerActionManager.GetMarkerSystem();
        if (markerSystem != null)
        {
            markerSystem.CreateAutoCaptureAreaMarker(crossPositions, "RecursionFaceCross", 
                new Color(0.8f, 0.5f, 0.2f, 0.8f), 3, 2); // 3 moves expiration, 2 charges
            Debug.Log($"[Task 8] Created RecursionFace auto-capture cross marker at ({centerPosition.x},{centerPosition.y}) with {crossPositions.Count} tiles");
        }
        else
        {
            Debug.LogWarning("[Task 8] MarkerSystem not available for RecursionFace cross marker");
        }
    }
    
    /// <summary>
    /// Task 7: Triggers resonance effect when InfinityFace touches grid
    /// Makes all Infinity cubes on grid phaseable for 2-4 moves
    /// </summary>
    private void TriggerResonanceEffect()
    {
        if (cachedWaveManager == null || cachedWaveManager.activeCubes == null)
        {
            Debug.LogWarning("[Task 7] WaveManager not found - cannot trigger resonance");
            return;
        }
        
        int phaseableMoves = UnityEngine.Random.Range(2, 5); // 2-4 moves
        int infinityCubesAffected = 0;
        
        foreach (var activeCube in cachedWaveManager.activeCubes)
        {
            if (activeCube != null && !activeCube.isDestroyed && activeCube.type == CubeType.Infinity)
            {
                activeCube.SetPhaseable(phaseableMoves);
                infinityCubesAffected++;
            }
        }
        
        Debug.Log($"[Task 7] Resonance: {infinityCubesAffected} Infinity cubes phaseable for {phaseableMoves} moves");
    }
    
    /// <summary>
    /// Task 6: Applies line divider reward (silently skips if disabled)
    /// </summary>
    private void ApplyLineDividerReward(int rows)
    {
        cachedGridManager?.MoveLineDivider(rows, true);
    }

    private void HandleReinforcedCube(CubeManager cube)
    {
        if (cube == null) return;

        bool wasDestroyed = cube.TakeDamage();
        
        if (wasDestroyed)
        {
            Debug.Log($"Reinforced cube destroyed at ({x}, {y}) after taking damage");
            NotifyPlayerCubeCapture(CubeType.Recursion);
            
            WaveManager waveManager = FindObjectOfType<WaveManager>();
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

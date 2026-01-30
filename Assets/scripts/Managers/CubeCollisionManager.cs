using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Manages collision detection and handling between player cubes and wave cubes.
/// Implements the comprehensive 16-combination collision matrix.
/// Handles active area markers that auto-capture cubes.
/// </summary>
public class CubeCollisionManager : MonoBehaviour, IManagerDebugInterface
{
    #region Inspector Configuration

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false; // Default OFF - high noise from collision checks

    #endregion

    #region Manager References

    private PlayerMarkerSystem markerSystem;
    private PlayerActionManager actionManager;
    private GridManager gridManager;
    private MarkerVisualManager visualManager;
    private AttunementManager attunementManager;

    #endregion

    #region Runtime State

    // Active area markers that auto-capture cubes and expire after N move forwards
    private List<ActiveAreaMarker> activeAreaMarkers = new List<ActiveAreaMarker>();

    #endregion

    #region Properties

    public bool EnableDebugLogs 
    { 
        get => enableDebugLogs; 
        set => enableDebugLogs = value; 
    }

    #endregion

    #region Data Structures

    /// <summary>
    /// Tracks an active area marker that can auto-capture cubes
    /// </summary>
    public class ActiveAreaMarker
    {
        public List<Vector2Int> positions;
        public int createdAtMoveStep;
        public int expiresAfterMoves; // Expires after this many move forwards
        public int remainingCharges; // Number of captures remaining (default 2)
        public int maxCharges; // Original max charges for display
        public Color markerColor;
        public string markerType;
        public bool autoCapture; // If true, automatically captures cubes entering the area

        public ActiveAreaMarker(List<Vector2Int> pos, int currentMoveStep, int duration, Color color, string type, bool autoCap = true, int charges = 2)
        {
            positions = new List<Vector2Int>(pos);
            createdAtMoveStep = currentMoveStep;
            expiresAfterMoves = duration;
            remainingCharges = charges;
            maxCharges = charges;
            markerColor = color;
            markerType = type;
            autoCapture = autoCap;
        }

        /// <summary>
        /// Checks if marker is expired (either by moves or by charges exhausted)
        /// </summary>
        public bool IsExpired(int currentMoveStep)
        {
            return remainingCharges <= 0 || (currentMoveStep - createdAtMoveStep) >= expiresAfterMoves;
        }

        /// <summary>
        /// Uses one charge. Returns true if capture should proceed, false if no charges left.
        /// </summary>
        public bool UseCharge()
        {
            if (remainingCharges <= 0) return false;
            remainingCharges--;
            return true;
        }

        /// <summary>
        /// Gets remaining move forwards before marker expires
        /// </summary>
        public int GetRemainingMoves(int currentMoveStep)
        {
            return Mathf.Max(0, expiresAfterMoves - (currentMoveStep - createdAtMoveStep));
        }

        /// <summary>
        /// Gets display text showing charges/moves remaining
        /// </summary>
        public string GetDisplayText(int currentMoveStep)
        {
            int movesLeft = GetRemainingMoves(currentMoveStep);
            return $"{remainingCharges}";  // Show charges - can be enhanced to show both
        }
    }

    /// <summary>
    /// Result of collision handling
    /// </summary>
    public struct CollisionResult
    {
        public bool handled;
        public bool destroyPlayerCube;

        public CollisionResult(bool handled, bool destroyPlayerCube = true)
        {
            this.handled = handled;
            this.destroyPlayerCube = destroyPlayerCube;
        }
    }

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        ValidateReferences();
        
        // Subscribe to wave step events for area marker processing
        GameEvents.OnWaveStep += OnWaveStep;
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        GameEvents.OnWaveStep -= OnWaveStep;
        
        // Clear active area markers
        activeAreaMarkers.Clear();
    }

    #endregion

    #region Public API

    /// <summary>
    /// Initializes the collision manager with required references
    /// </summary>
    public void Initialize(PlayerMarkerSystem system, PlayerActionManager action, GridManager grid, MarkerVisualManager visual)
    {
        markerSystem = system;
        actionManager = action;
        gridManager = grid;
        visualManager = visual;
        
        EnableDebugLogs = enableDebugLogs;
        DebugLog("Initialize", "CubeCollisionManager initialized");
    }

    /// <summary>
    /// Checks for collisions between player cubes and wave cubes.
    /// Handles both same-tile collisions and adjacent cubes moving toward each other.
    /// IMPORTANT: Pass-through collisions are checked FIRST to ensure the player cube
    /// collides with the leading wave cube when multiple adjacent cubes are present.
    /// </summary>
    public void CheckPlayerCubeCollisions(List<CubeManager> playerCubes)
    {
        if (playerCubes == null || playerCubes.Count == 0) return;
        if (gridManager == null) return;

        int collisionCount = 0;

        // Iterate through all player cubes
        for (int i = playerCubes.Count - 1; i >= 0; i--)
        {
            if (i >= playerCubes.Count) continue;

            var playerCube = playerCubes[i];
            if (playerCube == null || playerCube.isDestroyed)
            {
                playerCubes.RemoveAt(i);
                continue;
            }

            Vector2Int playerPos = playerCube.position;

            // Validate position bounds
            if (!IsValidPosition(playerPos))
            {
                continue;
            }

            // PRIORITY 1: Check pass-through collision FIRST (adjacent cubes that moved past each other)
            // This ensures we hit the LEADING cube when two adjacent wave cubes are present,
            // not the TRAILING cube that moved into our destination position.
            Vector2Int playerPreviousPos = new Vector2Int(playerPos.x, playerPos.y - 1);
            if (IsValidPosition(playerPreviousPos))
            {
                if (ProcessPassThroughCollision(playerCube, playerPos, playerPreviousPos, playerCubes, ref collisionCount, ref i))
                {
                    continue; // Pass-through collision handled, move to next player cube
                }
            }

            // PRIORITY 2: Check collision at current position (normal same-tile collision)
            if (ProcessCollisionAtPosition(playerCube, playerPos, playerCubes, ref collisionCount, ref i))
            {
                continue; // Collision handled, move to next player cube
            }
        }

        if (collisionCount > 0)
        {
            DebugLog("CheckPlayerCubeCollisions", $"Processed {collisionCount} collisions");
        }
    }

    /// <summary>
    /// Process all active area markers - check for auto-captures and expiration
    /// SEQUENTIAL CAPTURE: Processes positions sorted by Y (lowest first, closest to player)
    /// to ensure cubes are captured in proper order when multiple markers exist in same column.
    /// </summary>
    public void ProcessActiveAreaMarkers(int currentMoveStep)
    {
        if (activeAreaMarkers.Count == 0) return;

        var markersToRemove = new List<ActiveAreaMarker>();
        
        // Track which columns have already captured this step to prevent simultaneous same-column captures
        var columnsCapturedThisStep = new HashSet<int>();

        // Collect all marker positions with their associated markers, sorted by Y (ascending)
        // This ensures we process cubes closest to the player first
        var sortedPositions = new List<(Vector2Int pos, ActiveAreaMarker marker)>();
        foreach (var marker in activeAreaMarkers)
        {
            if (marker.autoCapture && marker.remainingCharges > 0)
            {
                foreach (var pos in marker.positions)
                {
                    sortedPositions.Add((pos, marker));
                }
            }
        }
        
        // Sort by Y position (ascending - lower Y = closer to player = process first)
        sortedPositions.Sort((a, b) => a.pos.y.CompareTo(b.pos.y));
        
        // Process positions in sorted order
        foreach (var (pos, marker) in sortedPositions)
        {
            // Skip if this column already captured a cube this step (sequential capture)
            if (columnsCapturedThisStep.Contains(pos.x))
            {
                continue;
            }
            
            // Skip if marker is out of charges (may have been used earlier in this loop)
            if (marker.remainingCharges <= 0)
            {
                continue;
            }
            
            var cubesAtPos = markerSystem.FindAllCubesAt(pos);
            foreach (var cube in cubesAtPos)
            {
                if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;

                // Try to use a charge and capture
                if (marker.UseCharge())
                {
                    if (markerSystem.ProcessCubeCapture(cube, pos, PlayerMarkerSystem.MarkerType.Recursion, null, false))
                    {
                        DebugLog("ProcessActiveAreaMarkers", $"Auto-captured {cube.type} at ({pos.x}, {pos.y}) by {marker.markerType} marker (charges left: {marker.remainingCharges})");
                        
                        // Mark this column as captured for this step (sequential capture)
                        columnsCapturedThisStep.Add(pos.x);
                    }
                }

                // Only capture one cube per position per step
                break;
            }
        }

        // Check for marker expiration and update displays
        foreach (var marker in activeAreaMarkers)
        {
            // Check if marker should be removed (charges exhausted OR moves expired)
            if (marker.IsExpired(currentMoveStep))
            {
                string reason = marker.remainingCharges <= 0 ? "charges exhausted" : "moves expired";
                DebugLog("ProcessActiveAreaMarkers", $"{marker.markerType} marker removed ({reason})");

                // Clear the visual markers and countdown text
                foreach (var pos in marker.positions)
                {
                    visualManager?.ClearTileHighlight(pos);
                    visualManager?.ClearMarkerCountdownText(pos);
                }
                markersToRemove.Add(marker);
            }
            else
            {
                // Update countdown display - show remaining charges
                foreach (var pos in marker.positions)
                {
                    visualManager?.UpdateMarkerCountdownText(pos, marker.remainingCharges);
                }
            }
        }

        // Remove expired markers
        foreach (var marker in markersToRemove)
        {
            activeAreaMarkers.Remove(marker);
        }
    }

    /// <summary>
    /// Creates an auto-capture area marker from external sources.
    /// </summary>
    public void CreateAutoCaptureAreaMarker(List<Vector2Int> positions, string markerType, Color markerColor, int expiresAfterMoves = 3, int charges = 2)
    {
        if (positions == null || positions.Count == 0) return;
        if (gridManager == null) return;

        // Create visual highlights for each position
        foreach (var pos in positions)
        {
            Tile tile = gridManager.GetTileAt(pos.x, pos.y);
            if (tile != null)
            {
                visualManager?.SetTileHighlight(tile, markerColor, markerType);
                visualManager?.CreateMarkerCountdownText(pos, charges, Color.white);
            }
        }

        // Get current move step from WaveManager
        int currentMoveStep = actionManager?.WaveManager?.MoveStep ?? 0;

        // Register as active area marker with charge tracking
        var areaMarker = new ActiveAreaMarker(positions, currentMoveStep, expiresAfterMoves, markerColor, markerType, true, charges);
        activeAreaMarkers.Add(areaMarker);

        DebugLog("CreateAutoCaptureAreaMarker", $"Created {markerType} auto-capture marker with {positions.Count} tiles ({charges} charges, expires in {expiresAfterMoves} moves)");
    }

    /// <summary>
    /// Clears all active area markers
    /// </summary>
    public void ClearActiveAreaMarkers()
    {
        foreach (var marker in activeAreaMarkers)
        {
            foreach (var pos in marker.positions)
            {
                visualManager?.ClearTileHighlight(pos);
                visualManager?.ClearMarkerCountdownText(pos);
            }
        }
        activeAreaMarkers.Clear();
    }

    /// <summary>
    /// Gets the count of active area markers
    /// </summary>
    public int GetActiveAreaMarkerCount() => activeAreaMarkers.Count;

    #endregion

    #region Private Methods - Collision Detection

    /// <summary>
    /// Called on each wave step to process active area markers
    /// </summary>
    private void OnWaveStep(int waveIndex, int stepNumber)
    {
        ProcessActiveAreaMarkers(stepNumber);
    }

    private void ValidateReferences()
    {
        if (markerSystem == null)
            DebugLog("ValidateReferences", "MarkerSystem not set - call Initialize()");
        if (actionManager == null)
            DebugLog("ValidateReferences", "ActionManager not set - call Initialize()");
        if (gridManager == null)
            DebugLog("ValidateReferences", "GridManager not set - call Initialize()");
    }

    /// <summary>
    /// Checks for collision at a specific position and processes it if found.
    /// </summary>
    private bool ProcessCollisionAtPosition(CubeManager playerCube, Vector2Int position, List<CubeManager> playerCubes, ref int collisionCount, ref int playerCubeIndex)
    {
        var cubesAtPosition = markerSystem.FindAllCubesAt(position);

        foreach (var waveCube in cubesAtPosition)
        {
            if (waveCube == null || waveCube.isDestroyed || waveCube.isPlayerCube) continue;

            // Task 7: Check if wave cube is phaseable - if so, player cube passes through
            if (waveCube.type == CubeType.Infinity && waveCube.IsPhaseable())
            {
                DebugLog("ProcessCollisionAtPosition", $"[Task 7] Player cube passing through phaseable Infinity cube at ({position.x}, {position.y})");
                continue; // Skip collision, allow passing through
            }

            // Route to appropriate collision handler based on collision matrix
            CollisionResult result = HandleCollision(playerCube, waveCube, position);

            if (result.handled)
            {
                // Only destroy player cube if it should be destroyed
                if (result.destroyPlayerCube)
                {
                    HandlePlayerCubeDestruction(playerCube, playerCubes, ref collisionCount, ref playerCubeIndex);
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Handles collision detection for adjacent cubes moving toward each other.
    /// This detects the leading cube that passed through the player's path.
    /// </summary>
    /// <returns>True if a pass-through collision was handled, false otherwise</returns>
    private bool ProcessPassThroughCollision(CubeManager playerCube, Vector2Int playerPos, Vector2Int playerPreviousPos,
        List<CubeManager> playerCubes, ref int collisionCount, ref int playerCubeIndex)
    {
        var cubesAtPreviousPos = markerSystem.FindAllCubesAt(playerPreviousPos);

        foreach (var waveCube in cubesAtPreviousPos)
        {
            if (waveCube == null || waveCube.isDestroyed || waveCube.isPlayerCube) continue;

            // Task 7: Check if wave cube is phaseable
            if (waveCube.type == CubeType.Infinity && waveCube.IsPhaseable())
            {
                DebugLog("ProcessPassThroughCollision", $"[Task 7] Player cube passing through phaseable Infinity cube at ({playerPreviousPos.x}, {playerPreviousPos.y})");
                continue;
            }

            // Verify wave cube came from player's current position (confirms they passed through)
            // Wave moves down (Y decreases), so wave's previous position is Y+1
            // Player moves up (Y increases), so player's previous position is Y-1
            // If wave cube's source position equals player's current position, they passed through each other
            Vector2Int waveCubeSourcePos = new Vector2Int(waveCube.position.x, waveCube.position.y + 1);
            if (waveCubeSourcePos == playerPos)
            {
                DebugLog("ProcessPassThroughCollision", $"Pass-through detected: Player at ({playerPos.x}, {playerPos.y}), Wave at ({waveCube.position.x}, {waveCube.position.y}) came from ({waveCubeSourcePos.x}, {waveCubeSourcePos.y})");
                
                CollisionResult result = HandleCollision(playerCube, waveCube, playerPreviousPos);

                if (result.handled)
                {
                    if (result.destroyPlayerCube)
                    {
                        HandlePlayerCubeDestruction(playerCube, playerCubes, ref collisionCount, ref playerCubeIndex);
                    }
                    return true; // Collision was handled
                }
            }
        }

        return false; // No pass-through collision found
    }

    /// <summary>
    /// Destroys player cube and removes it from tracking list.
    /// </summary>
    private void HandlePlayerCubeDestruction(CubeManager playerCube, List<CubeManager> playerCubes, ref int collisionCount, ref int playerCubeIndex)
    {
        collisionCount++;
        if (playerCube != null && playerCube.gameObject != null)
        {
            Destroy(playerCube.gameObject);
        }
        playerCubes.RemoveAt(playerCubeIndex);
    }

    /// <summary>
    /// Validates if a position is within grid bounds.
    /// </summary>
    private bool IsValidPosition(Vector2Int position)
    {
        if (gridManager == null) return false;
        return position.x >= 0 && position.x < gridManager.Width &&
               position.y >= 0 && position.y < gridManager.Height;
    }

    #endregion

    #region Private Methods - Collision Matrix Router

    /// <summary>
    /// Central collision matrix handler. Routes all 16 collision combinations to their specific behaviors.
    /// </summary>
    private CollisionResult HandleCollision(CubeManager playerCube, CubeManager waveCube, Vector2Int collisionPosition)
    {
        CubeType playerType = playerCube.type;
        CubeType waveType = waveCube.type;

        // Route to specific collision handler based on collision matrix
        switch (playerType)
        {
            case CubeType.Unit:
                return HandleUnitCollision(playerCube, waveCube, collisionPosition);

            case CubeType.Matrix:
                return HandleMatrixCollision(playerCube, waveCube, collisionPosition);

            case CubeType.Recursion:
                return HandleRecursionCollision(playerCube, waveCube, collisionPosition);

            case CubeType.Infinity:
                return HandleInfinityCollision(playerCube, waveCube, collisionPosition);

            default:
                DebugWarning("HandleCollision", $"Unknown player cube type: {playerType}");
                return new CollisionResult(false);
        }
    }

    /// <summary>
    /// Handles Unit cube collisions (Unit, Matrix, Recursion, Infinity)
    /// </summary>
    private CollisionResult HandleUnitCollision(CubeManager playerCube, CubeManager waveCube, Vector2Int position)
    {
        switch (waveCube.type)
        {
            case CubeType.Unit:
                // Unit + Unit: Standard capture
                return new CollisionResult(markerSystem.ProcessCubeCapture(waveCube, position, PlayerMarkerSystem.MarkerType.Unit, null, false));

            case CubeType.Matrix:
                // Unit + Matrix: 2x2 area capture centered on collision point
                return new CollisionResult(HandleUnitMatrixCollision(position));

            case CubeType.Recursion:
                // Unit + Recursion: Creates swap marker (1 charge, player selects direction)
                return new CollisionResult(HandleUnitRecursionSwap(position));

            case CubeType.Infinity:
                // Unit + Infinity: Unit destroyed, no face painting
                // Unit cubes do NOT paint Infinity cubes - they are simply destroyed on collision
                DebugLog("HandleUnitCollision", $"Unit cube destroyed by Infinity cube at ({position.x}, {position.y}) - no face painting");
                return new CollisionResult(true, true); // Collision handled, destroy player cube

            default:
                return new CollisionResult(false);
        }
    }

    /// <summary>
    /// Handles Matrix cube collisions (Unit, Matrix, Recursion, Infinity)
    /// </summary>
    private CollisionResult HandleMatrixCollision(CubeManager playerCube, CubeManager waveCube, Vector2Int position)
    {
        switch (waveCube.type)
        {
            case CubeType.Unit:
                // Matrix + Unit: Area capture from Matrix position (2x2 base, 3x3 with Expanded Expansion)
                int matrixAreaSize = AttunementManager.IsInitialized ? AttunementManager.Instance.GetMatrixAreaSize() : 2;
                return new CollisionResult(HandleMatrixAreaCapture(position, matrixAreaSize));

            case CubeType.Matrix:
                // Matrix + Matrix: 3x3 triggerable marker (enhanced reward)
                // Phaseable Expansion attunement may paint wave cube face
                DebugLog("HandleMatrixCollision", $"Matrix+Matrix collision detected at ({position.x}, {position.y})");
                return new CollisionResult(HandleMatrixMatrixCollision(position, waveCube));

            case CubeType.Recursion:
                // Matrix + Recursion: Degrading 2x2 marker
                return new CollisionResult(HandleMatrixRecursionCollision(position));

            case CubeType.Infinity:
                // Matrix + Infinity: Matrix destroyed - Infinity is immutable, nothing paints it
                DebugLog("HandleMatrixCollision", $"Matrix+Infinity collision at ({position.x}, {position.y}) - Matrix destroyed (Infinity immutable)");
                return new CollisionResult(true, true); // Collision handled, destroy player cube

            default:
                return new CollisionResult(false);
        }
    }

    /// <summary>
    /// Handles Recursion cube collisions (Unit, Matrix, Recursion, Infinity)
    /// </summary>
    private CollisionResult HandleRecursionCollision(CubeManager playerCube, CubeManager waveCube, Vector2Int position)
    {
        switch (waveCube.type)
        {
            case CubeType.Unit:
                // Recursion + Unit: Creates swap marker (1 charge, player selects direction)
                return new CollisionResult(HandleRecursionUnitSwap(position));

            case CubeType.Matrix:
                // Recursion + Matrix: Creates 2x2 marker (Wave Matrix determines area effect)
                return new CollisionResult(HandleRecursionMatrixSwap(position));

            case CubeType.Recursion:
                // Recursion + Recursion: Instant capture + empowered swap marker (2 charges, swap + capture axes)
                return new CollisionResult(HandleRecursionRecursionEmpoweredSwap(position, waveCube));

            case CubeType.Infinity:
                // Recursion + Infinity: Recursion destroyed - Infinity is immutable, nothing paints it
                DebugLog("HandleRecursionCollision", $"Recursion+Infinity collision at ({position.x}, {position.y}) - Recursion destroyed (Infinity immutable)");
                return new CollisionResult(true, true); // Collision handled, destroy player cube

            default:
                return new CollisionResult(false);
        }
    }

    /// <summary>
    /// Handles Infinity cube collisions (Unit, Matrix, Recursion, Infinity)
    /// When Player Infinity hits wave cubes, the PLAYER Infinity gets painted
    /// </summary>
    private CollisionResult HandleInfinityCollision(CubeManager playerCube, CubeManager waveCube, Vector2Int position)
    {
        switch (waveCube.type)
        {
            case CubeType.Unit:
                // Infinity + Unit: Wave join (removes Unit, Infinity takes position, moves with wave)
                // With Untethered attunement: Destroy Unit and continue up (no wave join)
                if (AttunementManager.IsInitialized && AttunementManager.Instance.ShouldInfinitySkipWaveJoin())
                {
                    return HandleInfinityUntethered(playerCube, waveCube, position);
                }
                return HandleInfinityWaveJoin(playerCube, waveCube, position);

            case CubeType.Matrix:
                // Infinity + Matrix: Capture Matrix, player Infinity continues - Infinity is immutable
                markerSystem.ProcessCubeCapture(waveCube, position, PlayerMarkerSystem.MarkerType.Infinity, null, false);
                DebugLog("HandleInfinityCollision", $"Infinity+Matrix collision at ({position.x}, {position.y}) - Matrix captured, Infinity continues (immutable)");
                return new CollisionResult(true, false); // Collision handled, player cube continues

            case CubeType.Recursion:
                // Infinity + Recursion: Capture Recursion, player Infinity continues - Infinity is immutable
                markerSystem.ProcessCubeCapture(waveCube, position, PlayerMarkerSystem.MarkerType.Infinity, null, false);
                DebugLog("HandleInfinityCollision", $"Infinity+Recursion collision at ({position.x}, {position.y}) - Recursion captured, Infinity continues (immutable)");
                return new CollisionResult(true, false); // Collision handled, player cube continues

            case CubeType.Infinity:
                // Infinity + Infinity: Paint PLAYER Infinity's face with Infinity status, resonance
                return HandleInfinityInfinityCollision(playerCube, waveCube, position);

            default:
                return new CollisionResult(false);
        }
    }

    #endregion

    #region Private Methods - Specific Collision Behaviors

    /// <summary>
    /// Unit + Matrix: Creates 2x2 manual marker (player triggers with R)
    /// Changed to manual trigger only to give player agency.
    /// </summary>
    private bool HandleUnitMatrixCollision(Vector2Int centerPosition)
    {
        // Capture the Matrix cube at collision point first
        var cubesAtPosition = markerSystem.FindAllCubesAt(centerPosition);
        bool capturedMatrix = false;

        foreach (var cube in cubesAtPosition)
        {
            if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
            if (cube.type == CubeType.Matrix)
            {
                if (markerSystem.ProcessCubeCapture(cube, centerPosition, PlayerMarkerSystem.MarkerType.Matrix, null, false))
                {
                    capturedMatrix = true;
                    break;
                }
            }
        }

        // Create a 2x2 cube marker for manual triggering
        int matrixAreaSize = AttunementManager.IsInitialized ? AttunementManager.Instance.GetMatrixAreaSize() : 2;
        markerSystem.CreateCubeMarker(centerPosition, PlayerMarkerSystem.CubeMarkerType.Matrix, matrixAreaSize);
        DebugLog("HandleUnitMatrixCollision", $"Unit+Matrix collision - created {matrixAreaSize}x{matrixAreaSize} manual cube marker at ({centerPosition.x}, {centerPosition.y})");

        return capturedMatrix || true;
    }

    /// <summary>
    /// Matrix + Unit: Creates 2x2 manual marker (player triggers with R)
    /// </summary>
    private bool HandleMatrixAreaCapture(Vector2Int centerPosition, int areaSize)
    {
        // Capture the Unit cube at collision point first
        var cubesAtPosition = markerSystem.FindAllCubesAt(centerPosition);
        bool capturedUnit = false;

        foreach (var cube in cubesAtPosition)
        {
            if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
            if (cube.type == CubeType.Unit)
            {
                if (markerSystem.ProcessCubeCapture(cube, centerPosition, PlayerMarkerSystem.MarkerType.Matrix, null, false))
                {
                    capturedUnit = true;
                    break;
                }
            }
        }

        // Create a 2x2 cube marker for manual triggering
        markerSystem.CreateCubeMarker(centerPosition, PlayerMarkerSystem.CubeMarkerType.Matrix, areaSize);
        DebugLog("HandleMatrixAreaCapture", $"Matrix+Unit collision - created {areaSize}x{areaSize} manual cube marker at ({centerPosition.x}, {centerPosition.y})");

        return capturedUnit || true;
    }

    /// <summary>
    /// Matrix + Matrix: 3x3 triggerable marker (enhanced reward)
    /// With Phaseable Expansion attunement: Also paints wave Matrix cube face
    /// </summary>
    private bool HandleMatrixMatrixCollision(Vector2Int centerPosition, CubeManager waveMatrixCube = null)
    {
        // Capture the Matrix cube at collision point first
        var cubesAtPosition = markerSystem.FindAllCubesAt(centerPosition);
        bool capturedMatrix = false;

        foreach (var cube in cubesAtPosition)
        {
            if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
            if (cube.type == CubeType.Matrix)
            {
                DebugLog("HandleMatrixMatrixCollision", $"Found Matrix wave cube at ({centerPosition.x}, {centerPosition.y}), calling ProcessCubeCapture with isSameTypeMatch=true");
                // Matrix + Matrix is a same-type match, so ProcessCubeCapture will create 3x3 marker
                if (markerSystem.ProcessCubeCapture(cube, centerPosition, PlayerMarkerSystem.MarkerType.Matrix, null, true))
                {
                    capturedMatrix = true;
                    DebugLog("HandleMatrixMatrixCollision", $"Matrix+Matrix collision - ProcessCubeCapture created 3x3 manual cube marker at ({centerPosition.x}, {centerPosition.y})");
                    break;
                }
            }
        }

        // If ProcessCubeCapture didn't create a marker (shouldn't happen), create one as fallback
        if (!capturedMatrix)
        {
            markerSystem.CreateCubeMarker(centerPosition, PlayerMarkerSystem.CubeMarkerType.Matrix, 3);
            DebugLog("HandleMatrixMatrixCollision", $"Matrix+Matrix collision - fallback: created 3x3 manual cube marker at ({centerPosition.x}, {centerPosition.y})");
        }

        return capturedMatrix || true;
    }

    /// <summary>
    /// Matrix + Recursion: Creates 2x2 degrading manual marker
    /// </summary>
    private bool HandleMatrixRecursionCollision(Vector2Int centerPosition)
    {
        var cubesAtPosition = markerSystem.FindAllCubesAt(centerPosition);
        bool capturedRecursion = false;

        foreach (var cube in cubesAtPosition)
        {
            if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
            if (cube.type == CubeType.Recursion)
            {
                if (markerSystem.ProcessCubeCapture(cube, centerPosition, PlayerMarkerSystem.MarkerType.Matrix, null, false))
                {
                    capturedRecursion = true;
                    break;
                }
            }
        }

        markerSystem.CreateCubeMarker(centerPosition, PlayerMarkerSystem.CubeMarkerType.Matrix, 2);
        DebugLog("HandleMatrixRecursionCollision", $"Matrix+Recursion collision - created 2x2 manual cube marker at ({centerPosition.x}, {centerPosition.y})");

        return capturedRecursion || true;
    }

    /// <summary>
    /// Unit + Recursion: Creates swap marker (1 charge, player selects direction)
    /// </summary>
    private bool HandleUnitRecursionSwap(Vector2Int position)
    {
        GridSegmentController segment = actionManager?.WaveManager?.CurrentSegmentController;
        markerSystem.CreateSwapMarker(position, 1, segment);
        DebugLog("HandleUnitRecursionSwap", $"Unit+Recursion collision - created swap marker at ({position.x}, {position.y})");
        return true;
    }

    /// <summary>
    /// Recursion + Unit: Creates swap marker (1 charge, player selects direction)
    /// </summary>
    private bool HandleRecursionUnitSwap(Vector2Int position)
    {
        GridSegmentController segment = actionManager?.WaveManager?.CurrentSegmentController;
        markerSystem.CreateSwapMarker(position, 1, segment);
        DebugLog("HandleRecursionUnitSwap", $"Recursion+Unit collision - created swap marker at ({position.x}, {position.y})");
        return true;
    }

    /// <summary>
    /// Recursion + Matrix: Creates 2x2 marker (Wave Matrix determines area effect)
    /// </summary>
    private bool HandleRecursionMatrixSwap(Vector2Int position)
    {
        // For now, create a 2x2 cube marker (similar to Matrix+Recursion)
        // This may need adjustment based on design requirements
        int matrixAreaSize = AttunementManager.IsInitialized ? AttunementManager.Instance.GetMatrixAreaSize() : 2;
        markerSystem.CreateCubeMarker(position, PlayerMarkerSystem.CubeMarkerType.Matrix, matrixAreaSize);
        DebugLog("HandleRecursionMatrixSwap", $"Recursion+Matrix collision - created {matrixAreaSize}x{matrixAreaSize} marker at ({position.x}, {position.y})");
        return true;
    }

    /// <summary>
    /// Recursion + Recursion: Instant capture + empowered swap marker (2 charges, swap + capture axes)
    /// </summary>
    private bool HandleRecursionRecursionEmpoweredSwap(Vector2Int position, CubeManager waveRecursionCube)
    {
        // First, capture the Recursion cube immediately (instant capture)
        bool captured = markerSystem.ProcessCubeCapture(waveRecursionCube, position, PlayerMarkerSystem.MarkerType.Recursion, null, true);
        
        if (captured)
        {
            DebugLog("HandleRecursionRecursionEmpoweredSwap", $"Recursion+Recursion - instant capture at ({position.x}, {position.y})");
        }

        // Then create empowered swap marker (2 charges = swap axis + capture axis)
        GridSegmentController segment = actionManager?.WaveManager?.CurrentSegmentController;
        markerSystem.CreateSwapMarker(position, 2, segment);
        
        // Mark as empowered (has both swap and capture axes)
        var swapMarkers = markerSystem.swapMarkers;
        if (swapMarkers.Count > 0)
        {
            var lastMarker = swapMarkers[swapMarkers.Count - 1];
            lastMarker.isEmpowered = true;
        }

        DebugLog("HandleRecursionRecursionEmpoweredSwap", $"Recursion+Recursion - created empowered swap marker (2 charges) at ({position.x}, {position.y})");
        return true;
    }

    /// <summary>
    /// Recursion capture: Creates a single recursion marker at collision point
    /// Base: 3 charges. With Concentrated Concentration attunement: 5 charges.
    /// Recursion+Unit and Unit+Recursion behavior
    /// DEPRECATED: Replaced by swap markers
    /// </summary>
    private bool HandleColumnCapture(Vector2Int position, int charges = 3)
    {
        int expiresAfterMoves = 5;
        
        // Apply Concentrated Concentration attunement (+2 charges)
        int finalCharges = AttunementManager.IsInitialized ? AttunementManager.Instance.GetRecursionCharges() : charges;

        // Create single marker with charges at collision point
        CreateRecursionMarker(position, expiresAfterMoves, finalCharges);

        // Try to capture cube at collision point immediately
        var cubesAtPosition = markerSystem.FindAllCubesAt(position);
        bool capturedImmediately = false;

        foreach (var cube in cubesAtPosition)
        {
            if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;

            if (markerSystem.ProcessCubeCapture(cube, position, PlayerMarkerSystem.MarkerType.Recursion, null, false))
            {
                // Find the marker and use a charge
                foreach (var marker in activeAreaMarkers)
                {
                    if (marker.positions.Contains(position) && marker.markerType == "RecursionCapture")
                    {
                        marker.UseCharge();
                        break;
                    }
                }
                capturedImmediately = true;
                DebugLog("HandleColumnCapture", $"Recursion capture - immediate capture at ({position.x}, {position.y})");
                break;
            }
        }

        if (!capturedImmediately)
        {
            DebugLog("HandleColumnCapture", $"Recursion capture - created marker with {charges} charges, expires in {expiresAfterMoves} moves");
        }

        return true;
    }

    /// <summary>
    /// Creates a recursion marker at position with specified charges.
    /// With Expanded Concentration attunement: Creates 2-tile vertical pattern.
    /// </summary>
    private void CreateRecursionMarker(Vector2Int position, int expiresAfterMoves = 5, int charges = 3)
    {
        Color markerColor = new Color(0.9f, 0.6f, 0.2f, 0.8f); // Amber/orange
        int currentMoveStep = actionManager?.WaveManager?.MoveStep ?? 0;

        // Check for Expanded Concentration attunement (+1 tile to pattern)
        int patternTiles = AttunementManager.IsInitialized ? AttunementManager.Instance.GetRecursionPatternTiles() : 1;
        
        List<Vector2Int> positions = new List<Vector2Int>();
        
        // Base position
        positions.Add(position);
        Tile tile = gridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            visualManager?.SetTileHighlight(tile, markerColor, "RecursionCapture");
            visualManager?.CreateMarkerCountdownText(position, charges, Color.white);
        }
        
        // Expanded Concentration: Add additional tile above (vertical pattern)
        if (patternTiles > 1)
        {
            Vector2Int abovePos = new Vector2Int(position.x, position.y + 1);
            if (IsValidPosition(abovePos))
            {
                positions.Add(abovePos);
                Tile aboveTile = gridManager.GetTileAt(abovePos.x, abovePos.y);
                if (aboveTile != null)
                {
                    visualManager?.SetTileHighlight(aboveTile, markerColor, "RecursionCapture");
                    visualManager?.CreateMarkerCountdownText(abovePos, charges, Color.white);
                }
            }
            DebugLog("CreateRecursionMarker", $"[Expanded Concentration] Extended to {positions.Count}-tile vertical pattern");
        }

        var areaMarker = new ActiveAreaMarker(positions, currentMoveStep, expiresAfterMoves, markerColor, "RecursionCapture", true, charges);
        activeAreaMarkers.Add(areaMarker);

        DebugLog("CreateRecursionMarker", $"Created recursion marker at ({position.x}, {position.y}) with {charges} charges, {positions.Count} tiles, expires in {expiresAfterMoves} moves");
    }

    /// <summary>
    /// Recursion + Matrix: Auto 3x1 horizontal marker with 2 charges
    /// Horizontally strong, captures cubes as wave passes
    /// </summary>
    private bool HandleRecursionMatrixCollision(Vector2Int centerPosition)
    {
        int charges = 2;
        int expiresAfterMoves = 5;

        CreateHorizontalMarker(centerPosition, 3, expiresAfterMoves, charges);

        var cubesAtPosition = markerSystem.FindAllCubesAt(centerPosition);
        bool capturedImmediately = false;

        foreach (var cube in cubesAtPosition)
        {
            if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
            if (markerSystem.ProcessCubeCapture(cube, centerPosition, PlayerMarkerSystem.MarkerType.Recursion, null, false))
            {
                var marker = activeAreaMarkers[activeAreaMarkers.Count - 1];
                marker.UseCharge();
                capturedImmediately = true;
                DebugLog("HandleRecursionMatrixCollision", $"Recursion+Matrix - immediate capture, {marker.remainingCharges} charges left");
                break;
            }
        }

        return true;
    }

    /// <summary>
    /// Creates a visual horizontal marker (3x1) - spans left/center/right
    /// </summary>
    private void CreateHorizontalMarker(Vector2Int centerPosition, int width, int expiresAfterMoves = 5, int charges = 2)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        Color markerColor = new Color(0.3f, 0.8f, 0.9f, 0.8f); // Cyan/blue

        // Horizontal: left, center, right
        for (int x = -1; x <= 1; x++)
        {
            Vector2Int pos = new Vector2Int(centerPosition.x + x, centerPosition.y);
            if (IsValidPosition(pos))
            {
                positions.Add(pos);
                Tile tile = gridManager.GetTileAt(pos.x, pos.y);
                if (tile != null)
                {
                    visualManager?.SetTileHighlight(tile, markerColor, "HorizontalCapture");
                    visualManager?.CreateMarkerCountdownText(pos, charges, Color.white);
                }
            }
        }

        int currentMoveStep = actionManager?.WaveManager?.MoveStep ?? 0;
        var areaMarker = new ActiveAreaMarker(positions, currentMoveStep, expiresAfterMoves, markerColor, "HorizontalCapture", true, charges);
        activeAreaMarkers.Add(areaMarker);

        DebugLog("CreateHorizontalMarker", $"Created 3x1 horizontal marker at ({centerPosition.x}, {centerPosition.y}) - {charges} charges, expires in {expiresAfterMoves} moves");
    }

    /// <summary>
    /// Recursion + Recursion: Cross marker (5 tiles) with charges
    /// With Phaseable Concentration attunement: Also paints wave Recursion cube face
    /// </summary>
    private bool HandleRecursionRecursionCollision(Vector2Int centerPosition, CubeManager waveRecursionCube = null)
    {
        int charges = 2;
        int expiresAfterMoves = 3;

        List<Vector2Int> crossPositions = new List<Vector2Int>();

        // Vertical line (3 tiles)
        for (int y = -1; y <= 1; y++)
        {
            Vector2Int pos = new Vector2Int(centerPosition.x, centerPosition.y + y);
            if (IsValidPosition(pos) && !crossPositions.Contains(pos))
            {
                crossPositions.Add(pos);
            }
        }

        // Horizontal line (3 tiles, center overlaps)
        for (int x = -1; x <= 1; x++)
        {
            Vector2Int pos = new Vector2Int(centerPosition.x + x, centerPosition.y);
            if (IsValidPosition(pos) && !crossPositions.Contains(pos))
            {
                crossPositions.Add(pos);
            }
        }

        CreateCrossMarker(crossPositions, expiresAfterMoves, charges);

        var cubesAtCenter = markerSystem.FindAllCubesAt(centerPosition);
        bool capturedImmediately = false;

        foreach (var cube in cubesAtCenter)
        {
            if (cube == null || cube.isDestroyed || cube.isPlayerCube) continue;
            if (markerSystem.ProcessCubeCapture(cube, centerPosition, PlayerMarkerSystem.MarkerType.Recursion, null, true))
            {
                var marker = activeAreaMarkers[activeAreaMarkers.Count - 1];
                marker.UseCharge();
                capturedImmediately = true;
                DebugLog("HandleRecursionRecursionCollision", $"Recursion+Recursion - immediate capture, {marker.remainingCharges} charges left");
                break;
            }
        }

        return true;
    }

    /// <summary>
    /// Creates a visual cross marker (5 tiles)
    /// </summary>
    private void CreateCrossMarker(List<Vector2Int> positions, int expiresAfterMoves = 3, int charges = 2)
    {
        Color markerColor = new Color(0.7f, 0.3f, 0.8f, 0.8f); // Purple

        foreach (var pos in positions)
        {
            Tile tile = gridManager.GetTileAt(pos.x, pos.y);
            if (tile != null)
            {
                visualManager?.SetTileHighlight(tile, markerColor, "CrossCapture");
                visualManager?.CreateMarkerCountdownText(pos, charges, Color.white);
            }
        }

        int currentMoveStep = actionManager?.WaveManager?.MoveStep ?? 0;
        var areaMarker = new ActiveAreaMarker(positions, currentMoveStep, expiresAfterMoves, markerColor, "CrossCapture", true, charges);
        activeAreaMarkers.Add(areaMarker);

        DebugLog("CreateCrossMarker", $"Created cross marker with {positions.Count} tiles - expires in {expiresAfterMoves} moves or on capture");
    }

    /// <summary>
    /// Recursion + Infinity: Paint Wave Infinity's face AND leave a recursion marker at collision point
    /// </summary>
    private CollisionResult HandleRecursionInfinityCollision(CubeManager waveInfinity, CubeManager playerRecursion, Vector2Int position)
    {
        // Paint the Wave Infinity's face with Recursion status
        CollisionResult paintResult = HandleWaveInfinityFacePaint(waveInfinity, playerRecursion, CubeType.Recursion, position, true);
        
        // Also leave a recursion marker at the collision point (1 charge by default)
        int charges = 1; // Per user request: "per the number of charges of the painted face (default 1)"
        int expiresAfterMoves = 5;
        
        List<Vector2Int> positions = new List<Vector2Int> { position };
        Color markerColor = new Color(0.8f, 0.5f, 0.2f, 0.8f); // Amber for Recursion
        
        Tile tile = gridManager.GetTileAt(position.x, position.y);
        if (tile != null)
        {
            visualManager?.SetTileHighlight(tile, markerColor, "RecursionMarker");
            visualManager?.CreateMarkerCountdownText(position, charges, Color.white);
        }
        
        int currentMoveStep = actionManager?.WaveManager?.MoveStep ?? 0;
        var areaMarker = new ActiveAreaMarker(positions, currentMoveStep, expiresAfterMoves, markerColor, "RecursionMarker", true, charges);
        activeAreaMarkers.Add(areaMarker);
        
        DebugLog("HandleRecursionInfinityCollision", $"Recursion+Infinity - painted Wave Infinity, left recursion marker with {charges} charge at ({position.x}, {position.y})");
        
        return paintResult;
    }

    /// <summary>
    /// Infinity + Unit: Wave join - Player Infinity destroys Unit, takes its place, moves with wave
    /// </summary>
    private CollisionResult HandleInfinityWaveJoin(CubeManager playerInfinity, CubeManager waveUnit, Vector2Int position)
    {
        // Capture/destroy the Unit cube
        if (markerSystem.ProcessCubeCapture(waveUnit, position, PlayerMarkerSystem.MarkerType.Unit, null, false))
        {
            // Player Infinity takes the Unit's position
            playerInfinity.position = position;
            
            // Convert to wave cube - no longer player-controlled, moves with wave
            playerInfinity.isPlayerCube = false;
            
            // Apply wave cube material (opaque instead of translucent)
            playerInfinity.ApplyWaveCubeMaterial();
            
            // CRITICAL: Remove from player cubes list so it doesn't get moved backward
            if (markerSystem.playerCubes.Contains(playerInfinity))
            {
                markerSystem.playerCubes.Remove(playerInfinity);
            }
            
            // Add to wave's active cubes so it moves with the wave
            if (actionManager?.WaveManager != null && !actionManager.WaveManager.activeCubes.Contains(playerInfinity))
            {
                actionManager.WaveManager.activeCubes.Add(playerInfinity);
            }
            
            DebugLog("HandleInfinityWaveJoin", $"Player Infinity joined wave at ({position.x}, {position.y}) - removed from player cubes, now moves with wave");
            return new CollisionResult(true, false); // Don't destroy - it joined wave
        }

        return new CollisionResult(false);
    }

    /// <summary>
    /// Untethered attunement: Infinity destroys Unit and continues moving up (no wave join)
    /// </summary>
    private CollisionResult HandleInfinityUntethered(CubeManager playerInfinity, CubeManager waveUnit, Vector2Int position)
    {
        // Capture/destroy the Unit cube
        if (markerSystem.ProcessCubeCapture(waveUnit, position, PlayerMarkerSystem.MarkerType.Unit, null, false))
        {
            DebugLog("HandleInfinityUntethered", $"[Untethered] Player Infinity destroyed Unit at ({position.x}, {position.y}) and continues up");
            // Player Infinity continues moving - don't join wave, don't destroy
            return new CollisionResult(true, false); // Collision handled, don't destroy player cube
        }

        return new CollisionResult(false);
    }

    /// <summary>
    /// Handles face painting when non-Infinity player cubes hit Wave Infinity cubes.
    /// Paints the WAVE Infinity cube's face with the player cube's type.
    /// </summary>
    private CollisionResult HandleWaveInfinityFacePaint(CubeManager waveInfinity, CubeManager playerCube, CubeType paintedType, Vector2Int position, bool destroyPlayerCube = true)
    {
        if (waveInfinity.type != CubeType.Infinity)
            return new CollisionResult(false);

        CubeFace collisionFace = CubeFace.Front; // Face that was hit

        FaceStatus faceStatus = paintedType switch
        {
            CubeType.Unit => FaceStatus.None, // Unit doesn't paint a useful face
            CubeType.Matrix => FaceStatus.MatrixFace,
            CubeType.Recursion => FaceStatus.RecursionFace,
            CubeType.Infinity => FaceStatus.InfinityFace,
            _ => FaceStatus.None
        };

        waveInfinity.PaintFace(collisionFace, faceStatus, GetFaceColorForType(paintedType), -1);

        DebugLog("HandleWaveInfinityFacePaint", $"Painted Wave Infinity's {collisionFace} face at ({position.x}, {position.y}) with {paintedType} type");

        return new CollisionResult(true, destroyPlayerCube);
    }

    /// <summary>
    /// Handles face painting when Player Infinity hits non-Infinity wave cubes.
    /// Paints the PLAYER Infinity cube's face with the wave cube's type, player continues moving.
    /// </summary>
    private CollisionResult HandlePlayerInfinityFacePaint(CubeManager playerInfinity, CubeManager waveCube, CubeType paintedType, Vector2Int position)
    {
        if (playerInfinity.type != CubeType.Infinity)
            return new CollisionResult(false);

        CubeFace collisionFace = CubeFace.Front; // Face that hit the wave cube

        FaceStatus faceStatus = paintedType switch
        {
            CubeType.Unit => FaceStatus.None,
            CubeType.Matrix => FaceStatus.MatrixFace,
            CubeType.Recursion => FaceStatus.RecursionFace,
            CubeType.Infinity => FaceStatus.InfinityFace,
            _ => FaceStatus.None
        };

        // Base: 1 charge. Apply Potent Paint attunements for bonus charges.
        int charges = 1;
        if (AttunementManager.IsInitialized)
        {
            if (paintedType == CubeType.Matrix)
            {
                charges += AttunementManager.Instance.GetMatrixPaintBonusCharges();
            }
            else if (paintedType == CubeType.Recursion)
            {
                charges += AttunementManager.Instance.GetRecursionPaintBonusCharges();
            }
        }

        // Paint the PLAYER Infinity cube's face with appropriate charges
        playerInfinity.PaintFace(collisionFace, faceStatus, GetFaceColorForType(paintedType), -1, charges);

        // Capture the wave cube
        markerSystem.ProcessCubeCapture(waveCube, position, PlayerMarkerSystem.MarkerType.Infinity, null, false);

        string attunementNote = charges > 1 ? $" [Potent {paintedType} Paint]" : "";
        DebugLog("HandlePlayerInfinityFacePaint", $"Painted Player Infinity's {collisionFace} face with {paintedType} type ({charges} charges){attunementNote}, captured wave cube, continuing up");

        return new CollisionResult(true, false); // Player Infinity continues, not destroyed
    }

    /// <summary>
    /// Infinity + Infinity: Trigger resonance immediately, destroy Player Infinity (cost)
    /// All Infinity cubes on grid become phaseable - the only positive Infinity interaction
    /// </summary>
    private CollisionResult HandleInfinityInfinityCollision(CubeManager playerInfinity, CubeManager waveInfinity, Vector2Int position)
    {
        // Trigger resonance immediately - all Infinity cubes become phaseable
        TriggerResonanceEffect(position);

        DebugLog("HandleInfinityInfinityCollision", $"Infinity+Infinity collision at ({position.x}, {position.y}) - Resonance triggered, Player Infinity destroyed (cost)");

        return new CollisionResult(true, true); // Player Infinity destroyed as cost of resonance
    }

    /// <summary>
    /// Triggers resonance effect - all Infinity cubes become phaseable for 2-4 moves
    /// </summary>
    private void TriggerResonanceEffect(Vector2Int triggerPosition)
    {
        if (actionManager?.WaveManager == null || actionManager.WaveManager.activeCubes == null)
        {
            DebugWarning("TriggerResonanceEffect", "WaveManager not found - cannot trigger resonance");
            return;
        }

        int phaseableMoves = UnityEngine.Random.Range(2, 5); // 2-4 moves
        int infinityCubesAffected = 0;

        foreach (var activeCube in actionManager.WaveManager.activeCubes)
        {
            if (activeCube != null && !activeCube.isDestroyed && activeCube.type == CubeType.Infinity)
            {
                activeCube.SetPhaseable(phaseableMoves);
                infinityCubesAffected++;
            }
        }

        DebugLog("TriggerResonanceEffect", $"Resonance triggered at ({triggerPosition.x}, {triggerPosition.y}) - {infinityCubesAffected} Infinity cubes now phaseable for {phaseableMoves} moves");
    }

    /// <summary>
    /// Gets color for face painting based on cube type
    /// </summary>
    private Color GetFaceColorForType(CubeType type)
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

    #region Debug

    private void DebugLog(string methodName, string message)
    {
        if (EnableDebugLogs)
            Debug.Log($"[CubeCollisionManager] {methodName}: {message}");
    }

    private void DebugWarning(string methodName, string message)
    {
        if (EnableDebugLogs)
            Debug.LogWarning($"[CubeCollisionManager] {methodName}: {message}");
    }

    private void DebugError(string methodName, string message)
    {
        Debug.LogError($"[CubeCollisionManager] {methodName}: {message}");
    }

    public string GetDebugStatus()
    {
        return $"CubeCollisionManager: {activeAreaMarkers.Count} active area markers";
    }

    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Active Area Markers"] = activeAreaMarkers.Count,
            ["MarkerSystem Set"] = markerSystem != null,
            ["ActionManager Set"] = actionManager != null,
            ["GridManager Set"] = gridManager != null,
            ["VisualManager Set"] = visualManager != null
        };
    }

    public void ResetToDefaults()
    {
        ClearActiveAreaMarkers();
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


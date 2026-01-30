using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Handles segment-to-segment transitions for multi-segment grid layouts.
/// Extracted from WaveManager as part of SRP refactoring.
/// WaveManager maintains facade methods that delegate to this controller.
/// </summary>
public class WaveSegmentController : MonoBehaviour
{
    #region References
    private WaveManager waveManager;
    private GridManager grid;
    private PlayerManager player;
    private GameObject[] cubePrefabs;
    
    // Logging
    private bool enableDebugLogs;
    #endregion

    #region Segment State
    // Track the MoveStep when first cube reached edge (for row offset calculation)
    private int transitionStartMoveStep = -1;
    
    // Current segment tracking
    private int currentSegmentIndex = 0;
    private bool isTransitioning = false;
    private bool isInLateralPhase = false; // Legacy - kept for compatibility
    private bool waveStoppedAtEdge = false; // True when entire wave has stopped at segment edge
    private List<CubeData> transitionCubeData = new List<CubeData>(); // Cubes to respawn after transition
    
    // Wave containment at segment edge
    private List<CubeData> originalWaveFormation = new List<CubeData>(); // Original wave for respawn
    private int originalWaveDepth = 0; // Number of rows in original wave
    private int segmentStartMoveStep = 0; // MoveStep when wave started on current segment
    private int movesUntilEdge = 0; // Calculated moves until front row reaches edge
    private HashSet<CubeManager> stoppedAtEdge = new HashSet<CubeManager>(); // Legacy - kept for compatibility
    private bool waveContainedAtEdge = false; // Legacy - kept for compatibility
    
    // Track if this is the first move on segment 1 (for camera rotation)
    private bool firstMoveOnSegment1 = false;
    #endregion

    #region Properties
    public int CurrentSegmentIndex => currentSegmentIndex;
    public bool IsTransitioning => isTransitioning;
    public bool WaveStoppedAtEdge => waveStoppedAtEdge;
    public List<CubeData> OriginalWaveFormation => originalWaveFormation;
    public int OriginalWaveDepth => originalWaveDepth;
    public int SegmentStartMoveStep => segmentStartMoveStep;
    public int MovesUntilEdge => movesUntilEdge;
    
    // Multi-segment properties (delegate to grid/waveManager)
    public bool HasSegmentControllers => grid != null && grid.HasSegmentControllers;
    private int SegmentControllerCount => grid != null ? grid.SegmentControllerCount : 0;
    private bool IsOnTerminalSegment => currentSegmentIndex >= SegmentControllerCount - 1;
    public GridSegmentController CurrentSegmentController => 
        grid != null && currentSegmentIndex < grid.SegmentControllerCount ? 
        grid.GetSegmentController(currentSegmentIndex) : null;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the segment controller with references to parent manager and dependencies.
    /// </summary>
    public void Initialize(WaveManager manager, GridManager gridManager, PlayerManager playerManager, GameObject[] prefabs, bool debugLogs)
    {
        waveManager = manager;
        grid = gridManager;
        player = playerManager;
        cubePrefabs = prefabs;
        enableDebugLogs = debugLogs;
        
        DebugLog("WaveSegmentController initialized");
    }
    
    /// <summary>
    /// Updates debug logging state from parent manager.
    /// </summary>
    public void SetDebugLogs(bool enabled)
    {
        enableDebugLogs = enabled;
    }
    #endregion

    #region Public API (Called by WaveManager facades)
    
    /// <summary>
    /// SEGMENT CONTROLLER: Called when a cube reaches the edge of the current segment.
    /// Returns true if the cube should be queued for transition (not terminal segment).
    /// Returns false if this is the terminal segment (cube truly escapes).
    /// </summary>
    public bool HandleCubeAtSegmentEdge(CubeManager cube)
    {
        if (!HasSegmentControllers)
            return false; // Not using segment controllers, use legacy escape
        
        // If we're on the terminal segment, this is a real escape
        if (IsOnTerminalSegment)
        {
            DebugLog($"🚨 TERMINAL ESCAPE: {cube.type} escaped from terminal segment {currentSegmentIndex}");
            return false;
        }
        
        // Queue this cube for segment transition
        DebugLog($"🔄 SEGMENT EDGE: {cube.type} at edge of segment {currentSegmentIndex}, queuing for transition");
        
        // Track when first cube reaches edge to calculate row offsets
        if (transitionStartMoveStep < 0)
        {
            transitionStartMoveStep = waveManager.MoveStep;
        }
        
        // Calculate row offset: cubes from row N of the wave reach the edge N steps after front row
        int rowOffset = waveManager.MoveStep - transitionStartMoveStep;
        
        // Store cube data for respawn (only if not already captured/destroyed)
        if (!cube.isDestroyed)
        {
            transitionCubeData.Add(new CubeData
            {
                type = cube.type,
                position = new Vector2Int(cube.position.x, rowOffset), // Store X column and row offset
                level = cube.level
            });
        }
        
        return true; // Cube will be transitioned, not escaped
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Called when a cube reaches the segment edge and STOPS (doesn't escape).
    /// </summary>
    public void HandleCubeStoppedAtEdge(CubeManager cube)
    {
        if (cube == null || cube.isDestroyed) return;
        
        stoppedAtEdge.Add(cube);
        DebugLog($"🛑 EDGE STOP: {cube.type} stopped at edge ({cube.position.x}, {cube.position.y}), direction: {cube.CurrentDirection}");
        
        // Check if wave is ready for transition
        var currentSegment = CurrentSegmentController;
        if (currentSegment != null && cube.CurrentDirection != currentSegment.localDirection)
        {
            // Cube is moving laterally (toward next segment) - check for segment transition
            CheckLateralSegmentTransition();
        }
        else
        {
            // Cube is moving in primary direction (down) - check for containment
            CheckWaveContainmentAtEdge();
        }
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Checks if cube should stop at edge instead of escaping.
    /// Returns true if cube is at the segment edge (next move would escape).
    /// Only applies to non-terminal segments.
    /// </summary>
    public bool ShouldCubeStopAtEdge(CubeManager cube)
    {
        if (cube == null) return false;
        
        // If wave is flagged as stopped at edge, ALL cubes stop
        if (waveStoppedAtEdge) return true;
        
        // Check if there's a next segment - if not, allow escape (terminal segment)
        var nextSegment = grid?.GetSegmentController(currentSegmentIndex + 1);
        if (nextSegment == null) return false;
        
        // Get segment for bounds check
        var segment = cube.CurrentSegment ?? CurrentSegmentController;
        if (segment == null) return false;
        
        // Check if cube is at the edge position (next move would escape)
        bool atEdge = false;
        switch (cube.CurrentDirection)
        {
            case MovementDirection.Down:
                atEdge = cube.position.y <= 0;
                break;
            case MovementDirection.Up:
                atEdge = cube.position.y >= segment.height - 1;
                break;
            case MovementDirection.Left:
                atEdge = cube.position.x <= 0;
                break;
            case MovementDirection.Right:
                atEdge = cube.position.x >= segment.width - 1;
                break;
        }
        
        if (atEdge)
        {
            waveStoppedAtEdge = true; // Flag entire wave to stop
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Checks if all cubes have reached the segment edge and transition should occur.
    /// </summary>
    public void CheckSegmentTransitionReady()
    {
        if (!HasSegmentControllers || IsOnTerminalSegment || isTransitioning)
            return;
        
        // With new edge containment logic, this is handled by CheckWaveContainmentAtEdge
        // Keep for backwards compatibility but the new flow uses HandleCubeStoppedAtEdge
        
        // Count ALL active cubes still on the grid (including Infinity)
        // For segment transitions, ALL cubes must reach the edge before transitioning
        var activeCubes = waveManager.activeCubes;
        int activeCubesOnGrid = activeCubes.Count(c => c != null && !c.isDestroyed);
        
        // If all cubes have reached the edge (either queued for transition or captured)
        if (activeCubesOnGrid == 0 && transitionCubeData.Count > 0)
        {
            DebugLog($"🔄 SEGMENT TRANSITION READY: {transitionCubeData.Count} cubes ready to transition to segment {currentSegmentIndex + 1}");
            StartCoroutine(PerformSegmentControllerTransition());
        }
        else if (activeCubesOnGrid == 0 && transitionCubeData.Count == 0)
        {
            // All cubes captured - wave complete!
            DebugLog("✅ All cubes captured on segment - wave complete!");
        }
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Checks if a grid position is occupied by another cube.
    /// Used for wave containment - cubes stop behind other cubes.
    /// </summary>
    public bool IsPositionOccupiedByCube(Vector2Int position, CubeManager excludeCube = null)
    {
        foreach (var cube in waveManager.activeCubes)
        {
            if (cube == null || cube.isDestroyed) continue;
            if (cube == excludeCube) continue;
            
            if (cube.position == position)
                return true;
        }
        return false;
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Gets a cube at a position ONLY if it has stopped at the edge.
    /// Returns null if the position is empty or contains a cube that's still moving.
    /// This allows normal wave movement while enabling containment stacking.
    /// </summary>
    public CubeManager GetStoppedCubeAtPosition(Vector2Int position, CubeManager excludeCube = null)
    {
        foreach (var cube in waveManager.activeCubes)
        {
            if (cube == null || cube.isDestroyed) continue;
            if (cube == excludeCube) continue;
            
            // Only return cube if it's at this position AND stopped at edge
            if (cube.position == position && cube.stoppedAtEdge)
                return cube;
        }
        return null;
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Resets segment tracking to initial state.
    /// </summary>
    public void ResetSegmentState()
    {
        currentSegmentIndex = 0;
        isTransitioning = false;
        isInLateralPhase = false;
        waveStoppedAtEdge = false;
        transitionCubeData.Clear();
        transitionStartMoveStep = -1;
        
        // Reset edge containment tracking
        originalWaveFormation.Clear();
        stoppedAtEdge.Clear();
        waveContainedAtEdge = false;
        originalWaveDepth = 0;
        
        DebugLog("🔄 Segment state reset to segment 0");
    }
    
    /// <summary>
    /// ADVANCED GRID: Resets segment tracking (call when wave/stage resets).
    /// </summary>
    public void ResetSegmentTracking()
    {
        currentSegmentIndex = 0;
        isTransitioning = false;
        waveStoppedAtEdge = false; // FIX: Reset edge flag so single-segment maps allow escape
        transitionCubeData.Clear();
        transitionStartMoveStep = -1; // Reset row offset tracking
        
        if (grid != null)
        {
            grid.SetActiveSegment(0);
        }
        
        // Reset camera to default/segment 0 settings
        var cameraFollow = FindFirstObjectByType<CameraFollow>();
        if (cameraFollow != null)
        {
            if (HasSegmentControllers && grid.SegmentControllerCount > 0)
            {
                var primarySegment = grid.GetSegmentController(0);
                cameraFollow.SetSegmentInstant(primarySegment);
            }
            else
            {
                cameraFollow.ResetToDefault();
            }
        }
        
        DebugLog("🔄 Segment tracking reset");
    }
    
    /// <summary>
    /// Pre-checks if ANY cube in the wave is at the segment edge.
    /// Called BEFORE processing cube movements to prevent race conditions.
    /// </summary>
    public void PreCheckWaveAtEdge()
    {
        var segment = CurrentSegmentController;
        if (segment == null) return;
        
        // Check if there's a next segment - if not, this is terminal
        var nextSegment = grid?.GetSegmentController(currentSegmentIndex + 1);
        if (nextSegment == null) return;
        
        // Check each cube to see if any is at the edge
        foreach (var cube in waveManager.activeCubes)
        {
            if (cube == null || cube.isDestroyed) continue;
            
            bool atEdge = false;
            switch (cube.CurrentDirection)
            {
                case MovementDirection.Down:
                    atEdge = cube.position.y <= 0;
                    break;
                case MovementDirection.Up:
                    atEdge = cube.position.y >= segment.height - 1;
                    break;
                case MovementDirection.Left:
                    atEdge = cube.position.x <= 0;
                    break;
                case MovementDirection.Right:
                    atEdge = cube.position.x >= segment.width - 1;
                    break;
            }
            
            if (atEdge)
            {
                waveStoppedAtEdge = true;
                DebugLog($"🛑 PRE-CHECK: Cube at ({cube.position.x},{cube.position.y}) is at edge - stopping entire wave");
                return;
            }
        }
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Checks if the wave has reached the segment edge.
    /// Triggered when waveStoppedAtEdge flag is set by ShouldCubeStopAtEdge.
    /// </summary>
    public void CheckWaveAtSegmentEdge()
    {
        if (isTransitioning) return;
        
        // Check if there's a next segment - if not, this is terminal segment
        var nextSegment = grid?.GetSegmentController(currentSegmentIndex + 1);
        if (nextSegment == null) return;
        
        // Debug: Log every 5 moves
        int movesSinceStart = waveManager.MoveStep - segmentStartMoveStep;
        if (movesSinceStart % 5 == 0)
        {
            DebugLog($"📍 Edge check: MoveStep={waveManager.MoveStep}, waveStoppedAtEdge={waveStoppedAtEdge}");
        }
        
        // Check if wave has been flagged as stopped at edge
        if (!waveStoppedAtEdge)
        {
            return; // Not at edge yet
        }
        
        // ENTIRE WAVE has reached the edge - trigger transition
        DebugLog($"✅ WAVE AT EDGE: MoveStep={waveManager.MoveStep}, triggering transition");
        StartCoroutine(PerformEdgeTransitionToNextSegment());
    }
    
    /// <summary>
    /// Records the original wave formation for respawn at segment edge.
    /// Calculates the wave depth and how many moves until the front row reaches the edge.
    /// </summary>
    public void TrackOriginalWaveFormation()
    {
        originalWaveFormation.Clear();
        
        if (waveManager.activeCubes.Count == 0) return;
        
        int minRow = int.MaxValue;
        int maxRow = int.MinValue;
        
        foreach (var cube in waveManager.activeCubes)
        {
            if (cube == null || cube.isDestroyed) continue;
            
            // Store original cube data
            originalWaveFormation.Add(new CubeData
            {
                type = cube.type,
                position = cube.position,
                level = cube.level
            });
            
            // Track row range
            minRow = Mathf.Min(minRow, cube.position.y);
            maxRow = Mathf.Max(maxRow, cube.position.y);
        }
        
        originalWaveDepth = (maxRow - minRow) + 1;
        
        // Record starting move step and calculate moves until edge
        segmentStartMoveStep = waveManager.MoveStep;
        movesUntilEdge = maxRow; // Front row at maxRow needs maxRow moves to reach row 0
        
        DebugLog($"📊 Wave tracked: {originalWaveFormation.Count} cubes, depth={originalWaveDepth}, front at row {maxRow}");
        DebugLog($"📊 Segment starts at MoveStep={segmentStartMoveStep}, edge in {movesUntilEdge} moves (MoveStep={segmentStartMoveStep + movesUntilEdge})");
    }
    
    /// <summary>
    /// Clears segment edge tracking for new wave.
    /// </summary>
    public void ClearEdgeTracking()
    {
        originalWaveFormation.Clear();
        stoppedAtEdge.Clear();
        waveContainedAtEdge = false;
    }
    #endregion

    #region Private Methods - Transition Logic
    
    /// <summary>
    /// SEGMENT CONTROLLER: Checks if the wave's leading edge has reached the segment boundary
    /// while moving laterally toward the next segment.
    /// </summary>
    private void CheckLateralSegmentTransition()
    {
        // Now handled by CheckWaveAtSegmentEdge which is called from MoveCubesForward
        CheckWaveAtSegmentEdge();
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Legacy method - kept for compatibility but now uses CheckWaveAtSegmentEdge.
    /// </summary>
    private void CheckWaveContainmentAtEdge()
    {
        CheckWaveAtSegmentEdge();
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Transitions cubes from current segment to next segment
    /// when they've reached the lateral boundary.
    /// </summary>
    private IEnumerator PerformLateralSegmentTransition()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        
        DebugLog($"🔄 LATERAL TRANSITION: Moving from segment {currentSegmentIndex} to {currentSegmentIndex + 1}");
        
        // Store original wave for respawn on new segment
        // Convert actual positions to (column, rowOffset) format
        transitionCubeData.Clear();
        
        // Find the front row (highest Y) to calculate offsets
        int frontRow = 0;
        foreach (var cubeData in originalWaveFormation)
        {
            frontRow = Mathf.Max(frontRow, cubeData.position.y);
        }
        
        foreach (var cubeData in originalWaveFormation)
        {
            // Convert to (column, rowOffset) format
            int rowOffset = frontRow - cubeData.position.y;
            
            transitionCubeData.Add(new CubeData
            {
                type = cubeData.type,
                position = new Vector2Int(cubeData.position.x, rowOffset),
                level = cubeData.level
            });
        }
        
        // Destroy current cubes
        var activeCubes = waveManager.activeCubes;
        var currentCubes = activeCubes.Where(c => c != null && !c.isDestroyed).ToList();
        foreach (var cube in currentCubes)
        {
            if (cube.gameObject != null) Destroy(cube.gameObject);
        }
        activeCubes.Clear();
        stoppedAtEdge.Clear();
        
        // Advance to next segment
        currentSegmentIndex++;
        DebugLog($"🔄 Advanced to segment {currentSegmentIndex}");
        
        // Grant player invulnerability
        if (player != null)
        {
            player.GrantBriefInvulnerability(1.0f);
        }
        
        // Respawn full wave on new segment
        RespawnCubesAtSegmentController();
        
        // Reset containment tracking for new segment
        waveContainedAtEdge = false;
        originalWaveFormation.Clear();
        TrackOriginalWaveFormation();
        
        yield return new WaitForSeconds(0.3f);
        
        isTransitioning = false;
        DebugLog($"🔄 LATERAL TRANSITION COMPLETE: Now on segment {currentSegmentIndex}");
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Performs transition when wave reaches segment edge.
    /// </summary>
    private IEnumerator PerformEdgeTransitionToNextSegment()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        
        var currentSegment = CurrentSegmentController;
        var nextSegment = grid.GetSegmentController(currentSegmentIndex + 1);
        
        if (nextSegment == null)
        {
            DebugLog("❌ Cannot transition: no next segment");
            isTransitioning = false;
            yield break;
        }
        
        DebugLog($"🔄 EDGE TRANSITION: Wave stopped at segment {currentSegmentIndex} edge");
        
        // Step 1: Identify and respawn missing NON-infinity cubes
        RespawnMissingCubesAtEdge(currentSegment);
        
        // Step 2: Transition all cubes to segment 1's coordinate system
        TransitionCubesToNextSegment(currentSegment, nextSegment);
        
        // Step 3: Advance to next segment and reset flags
        currentSegmentIndex++;
        isInLateralPhase = false;
        waveStoppedAtEdge = false; // Allow wave to move again
        
        // Step 4: Calculate moves until cubes reach segment 1's edge
        int oldWaveWidth = currentSegment.width;
        var activeCubes = waveManager.activeCubes;
        if (activeCubes.Count > 0)
        {
            int maxY = activeCubes.Where(c => c != null && !c.isDestroyed).Max(c => c.position.y);
            oldWaveWidth = maxY - nextSegment.height + 1;
        }
        movesUntilEdge = nextSegment.height + oldWaveWidth;
        segmentStartMoveStep = waveManager.MoveStep;
        
        DebugLog($"🔄 Now on segment {currentSegmentIndex}, wave at y={nextSegment.height} (above grid)");
        DebugLog($"🔄 {movesUntilEdge} moves to reach segment {currentSegmentIndex}'s edge");
        
        // Update original wave formation for this segment
        originalWaveFormation.Clear();
        TrackOriginalWaveFormation();
        
        // Grant player invulnerability
        if (player != null)
        {
            player.GrantBriefInvulnerability(1.0f);
        }
        
        yield return new WaitForSeconds(0.3f);
        
        isTransitioning = false;
        DebugLog($"🔄 EDGE TRANSITION COMPLETE: Wave moving down on segment {currentSegmentIndex}");
    }
    
    /// <summary>
    /// Respawns missing NON-infinity cubes at the edge to restore full wave formation.
    /// </summary>
    private void RespawnMissingCubesAtEdge(GridSegmentController segment)
    {
        if (segment == null) return;
        
        var activeCubes = waveManager.activeCubes;
        
        // Build set of current cube positions (column, rowOffset from front)
        var currentPositions = new HashSet<string>();
        foreach (var cube in activeCubes)
        {
            if (cube != null && !cube.isDestroyed)
            {
                currentPositions.Add($"{cube.position.x},{cube.position.y}");
            }
        }
        
        int respawnCount = 0;
        
        // Check each cube in original formation
        foreach (var originalCube in originalWaveFormation)
        {
            // Skip infinity cubes - they can't be captured so should always be present
            if (originalCube.type == CubeType.Infinity) continue;
            
            // Calculate expected position at edge
            int maxY = originalWaveFormation.Max(c => c.position.y);
            int rowOffset = maxY - originalCube.position.y;
            string posKey = $"{originalCube.position.x},{rowOffset}";
            
            // Check if cube exists at this position
            if (!currentPositions.Contains(posKey))
            {
                // Respawn this cube at the edge
                Vector2Int localPos = new Vector2Int(originalCube.position.x, rowOffset);
                Vector3 worldPos = segment.LocalToWorldPosition(localPos.x, localPos.y, 2f);
                
                int prefabIndex = (int)originalCube.type;
                if (prefabIndex >= 0 && prefabIndex < cubePrefabs.Length && cubePrefabs[prefabIndex] != null)
                {
                    GameObject cubeObj = Instantiate(cubePrefabs[prefabIndex], worldPos, segment.WorldRotation);
                    var cube = cubeObj.GetComponent<CubeManager>();
                    if (cube == null) cube = cubeObj.AddComponent<CubeManager>();
                    
                    var spawnData = new CubeData
                    {
                        type = originalCube.type,
                        position = localPos,
                        level = originalCube.level
                    };
                    
                    cube.Init(grid, spawnData, 2f);
                    cube.transform.position = worldPos;
                    cube.transform.rotation = segment.WorldRotation;
                    cube.SetSegmentController(segment);
                    
                    activeCubes.Add(cube);
                    currentPositions.Add(posKey);
                    respawnCount++;
                }
            }
        }
        
        if (respawnCount > 0)
        {
            DebugLog($"🔄 Respawned {respawnCount} missing non-infinity cubes at edge");
        }
    }
    
    /// <summary>
    /// Transitions all cubes from current segment to next segment's coordinate system.
    /// TRANSPOSE: Since direction changes 90°, we swap rows and columns.
    /// </summary>
    private void TransitionCubesToNextSegment(GridSegmentController fromSegment, GridSegmentController toSegment)
    {
        int toHeight = toSegment.height;
        int maxColumn = fromSegment.width - 1;
        
        var activeCubes = waveManager.activeCubes;
        
        DebugLog($"🔄 Transitioning {activeCubes.Count} cubes to segment {currentSegmentIndex + 1} (TRANSPOSE)");
        DebugLog($"🔄 maxColumn={maxColumn}, toHeight={toHeight}");
        
        foreach (var cube in activeCubes)
        {
            if (cube == null || cube.isDestroyed) continue;
            
            int oldColumn = cube.position.x;
            int oldRow = cube.position.y;
            
            // TRANSPOSE for 90° direction change
            int newX = oldRow;
            int newY = toHeight + (maxColumn - oldColumn);
            
            DebugLog($"  Cube {cube.type}: ({oldColumn},{oldRow}) -> ({newX},{newY}) [transpose]");
            
            // Update cube position
            cube.position = new Vector2Int(newX, newY);
            
            // Assign to new segment
            cube.SetSegmentController(toSegment);
            cube.stoppedAtEdge = false;
            
            // Calculate world position
            Vector3 worldPos = CalculateWorldPositionAboveGrid(toSegment, newX, newY);
            cube.transform.position = worldPos;
            cube.transform.rotation = toSegment.WorldRotation;
        }
    }
    
    /// <summary>
    /// Calculates world position for a cube that may be above the grid (y >= height).
    /// </summary>
    private Vector3 CalculateWorldPositionAboveGrid(GridSegmentController segment, int x, int y)
    {
        // If within grid bounds, use normal calculation
        if (y < segment.height)
        {
            return segment.LocalToWorldPosition(x, y, 2f);
        }
        
        // For positions above the grid, extrapolate based on grid spacing
        Vector3 topRowPos = segment.LocalToWorldPosition(x, segment.height - 1, 2f);
        Vector3 prevRowPos = segment.LocalToWorldPosition(x, segment.height - 2, 2f);
        
        Vector3 rowDirection = (topRowPos - prevRowPos).normalized;
        int rowsAbove = y - (segment.height - 1);
        
        return topRowPos + (rowDirection * rowsAbove * segment.tileSize);
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Performs the full segment transition sequence.
    /// </summary>
    private IEnumerator PerformSegmentControllerTransition()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        
        DebugLog($"🔄 SEGMENT TRANSITION: Starting transition from segment {currentSegmentIndex} to {currentSegmentIndex + 1}");
        
        var activeCubes = waveManager.activeCubes;
        
        // Clean up remaining infinity cubes (they fall off)
        var infinityCubes = activeCubes.Where(c => c != null && !c.isDestroyed && c.type == CubeType.Infinity).ToList();
        if (infinityCubes.Count > 0)
        {
            DebugLog($"🔄 Removing {infinityCubes.Count} infinity cubes from previous segment");
            yield return StartCoroutine(PlayFallOverEffect(infinityCubes));
            
            foreach (var cube in infinityCubes)
            {
                activeCubes.Remove(cube);
                if (cube != null && cube.gameObject != null)
                {
                    Destroy(cube.gameObject);
                }
            }
        }
        
        // Advance to next segment
        currentSegmentIndex++;
        DebugLog($"🔄 Advanced to segment {currentSegmentIndex}");
        
        yield return new WaitForSeconds(0.3f);
        
        // Grant player brief invulnerability
        if (player != null)
        {
            player.GrantBriefInvulnerability(1.0f);
        }
        
        // Respawn cubes at new segment
        RespawnCubesAtSegmentController();
        
        yield return new WaitForSeconds(0.5f);
        
        isTransitioning = false;
        DebugLog($"🔄 SEGMENT TRANSITION COMPLETE: Now on segment {currentSegmentIndex}");
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Respawns queued cubes at the new segment.
    /// </summary>
    private void RespawnCubesAtSegmentController()
    {
        var currentSegment = CurrentSegmentController;
        if (currentSegment == null)
        {
            DebugLog($"❌ Cannot respawn: No segment controller for index {currentSegmentIndex}");
            return;
        }
        
        var activeCubes = waveManager.activeCubes;
        
        // Spawn at segment's spawn row (entry point)
        int baseSpawnRow = currentSegment.SpawnRow;
        
        DebugLog($"🔄 Respawning {transitionCubeData.Count} cubes at segment {currentSegmentIndex}");
        DebugLog($"   Base spawn row: {baseSpawnRow}, segment: {currentSegment.width}x{currentSegment.height}");
        DebugLog($"   Movement direction: {currentSegment.localDirection}");
        
        foreach (var cubeData in transitionCubeData)
        {
            int column = cubeData.position.x;
            int rowOffset = cubeData.position.y;
            
            int spawnRow = baseSpawnRow - rowOffset;
            spawnRow = Mathf.Clamp(spawnRow, 0, currentSegment.height - 1);
            column = Mathf.Clamp(column, 0, currentSegment.width - 1);
            
            Vector2Int localPos = new Vector2Int(column, spawnRow);
            Vector3 spawnWorldPos = currentSegment.LocalToWorldPosition(localPos.x, localPos.y, 2f);
            Quaternion cubeRotation = currentSegment.WorldRotation;
            
            int prefabIndex = (int)cubeData.type;
            if (prefabIndex >= 0 && prefabIndex < cubePrefabs.Length && cubePrefabs[prefabIndex] != null)
            {
                GameObject cubeObj = Instantiate(cubePrefabs[prefabIndex], Vector3.zero, Quaternion.identity);
                
                var cube = cubeObj.GetComponent<CubeManager>();
                if (cube == null) cube = cubeObj.AddComponent<CubeManager>();
                
                var spawnData = new CubeData
                {
                    type = cubeData.type,
                    position = localPos,
                    level = cubeData.level
                };
                
                cube.Init(grid, spawnData, 2f);
                cube.transform.position = spawnWorldPos;
                cube.transform.rotation = cubeRotation;
                cube.SetSegmentController(currentSegment);
                activeCubes.Add(cube);
                
                DebugLog($"  ✅ Respawned {cubeData.type} at local ({localPos.x}, {localPos.y}) world {spawnWorldPos}");
            }
        }
        
        transitionCubeData.Clear();
        transitionStartMoveStep = -1;
    }
    
    /// <summary>
    /// Plays fall-off effect for cubes leaving the segment.
    /// </summary>
    private IEnumerator PlayFallOverEffect(List<CubeManager> cubes)
    {
        // Simple fall effect - can be enhanced later
        float duration = 0.5f;
        float elapsed = 0f;
        
        var startPositions = cubes.Select(c => c != null ? c.transform.position : Vector3.zero).ToList();
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            for (int i = 0; i < cubes.Count; i++)
            {
                if (cubes[i] != null && cubes[i].gameObject != null)
                {
                    Vector3 fallOffset = Vector3.down * (t * t * 5f); // Accelerating fall
                    cubes[i].transform.position = startPositions[i] + fallOffset;
                }
            }
            
            yield return null;
        }
    }
    
    /// <summary>
    /// SEGMENT CONTROLLER: Repositions player to the lowest row of the current segment.
    /// </summary>
    private void RepositionPlayerForSegment()
    {
        var currentSegment = CurrentSegmentController;
        if (currentSegment == null || player == null)
        {
            DebugLog("❌ Cannot reposition player: missing segment controller or player");
            return;
        }
        
        int centerX = currentSegment.width / 2;
        int bottomY = 0;
        
        player.SetPositionOnSegment(currentSegment, centerX, bottomY);
        
        DebugLog($"🎮 Player repositioned to segment {currentSegmentIndex} at local ({centerX}, {bottomY})");
    }
    
    /// <summary>
    /// Determines the movement direction to reach the next segment from current segment.
    /// </summary>
    private MovementDirection GetDirectionTowardSegment(GridSegmentController from, GridSegmentController to)
    {
        Vector3 fromCenter = from.transform.position;
        Vector3 toCenter = to.transform.position;
        Vector3 direction = (toCenter - fromCenter).normalized;
        
        Vector3 localDir = from.transform.InverseTransformDirection(direction);
        
        if (Mathf.Abs(localDir.x) > Mathf.Abs(localDir.z))
        {
            return localDir.x > 0 ? MovementDirection.Right : MovementDirection.Left;
        }
        else
        {
            return localDir.z > 0 ? MovementDirection.Up : MovementDirection.Down;
        }
    }
    #endregion

    #region Debug
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[WaveSegmentController] {message}");
        }
    }
    #endregion
}

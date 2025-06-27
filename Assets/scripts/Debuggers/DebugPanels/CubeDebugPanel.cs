using static Enumerations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CubeDebugPanel : DebugPanelBase
{
    public override string PanelName => "Cube Faces";
    public override DebugPanelGroup Group => DebugPanelGroup.Cube;

    private GridManager gridManager;
    private PlayerManager playerManager;

    // UI State
    private bool showFacePainter = true;
    private bool showActiveCubes = true;
    private bool showCubeInspector = false;
    private bool showRecursionTests = false;
    private Vector2 activeCubesScroll;
    private Vector2 inspectorScroll;

    // Face painting controls
    private int selectedFaceStatus = 1; // 1=Corrupted, 2=Enhanced
    private int paintDuration = 3;
    private Vector2Int targetPosition = new Vector2Int(0, 0);
    private bool autoTrackPlayer = true;

    // Selection
    private CubeManager selectedCube = null;
    private int maxCubesToShow = 8;

    public override void Initialize()
    {
        base.Initialize(); // Initialize theme and performance systems
        
        gridManager = GridManager.Instance;
        playerManager = Object.FindObjectOfType<PlayerManager>();

        if (playerManager != null)
        {
            targetPosition = playerManager.currentTilePosition;
        }
    }

    public override void Update()
    {
        if (autoTrackPlayer && playerManager != null)
        {
            targetPosition = playerManager.currentTilePosition;
        }

        // Auto-clear destroyed cube selection
        if (selectedCube != null && (selectedCube.isDestroyed || selectedCube == null))
        {
            selectedCube = null;
        }
    }

    protected override void DrawPanelContent()
    {
        DrawSectionToggles();
        GUILayout.Space(5);

        if (showFacePainter) DrawFacePainterSection();
        if (showActiveCubes) DrawActiveCubesSection();
        if (showCubeInspector && selectedCube != null) DrawCubeInspectorSection();
        if (showRecursionTests) DrawRecursionTestsSection();
    }

    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        showFacePainter = DebugUIHelpers.DrawToggleButton("Face Painter", showFacePainter);
        showActiveCubes = DebugUIHelpers.DrawToggleButton("Active Cubes", showActiveCubes);
        showCubeInspector = DebugUIHelpers.DrawToggleButton("Inspector", showCubeInspector);
        showRecursionTests = DebugUIHelpers.DrawToggleButton("Recursion", showRecursionTests);
        GUILayout.EndHorizontal();
    }

    private void DrawFacePainterSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("FACE PAINTER", GUI.skin.box);

        // Target position controls
        DrawTargetPositionControls();

        // Face painting settings
        DrawFacePaintingSettings();

        // Quick actions
        DrawFacePaintingActions();

        GUILayout.EndVertical();
    }

    private void DrawTargetPositionControls()
    {
        GUILayout.BeginHorizontal();
        autoTrackPlayer = GUILayout.Toggle(autoTrackPlayer, "Track Player");
        GUILayout.EndHorizontal();

        if (!autoTrackPlayer)
        {
            targetPosition = DebugUIHelpers.DrawVector2IntField("Position:", targetPosition, 
                0, gridManager?.Width - 1 ?? 10, 0, gridManager?.Height - 1 ?? 20);
        }
        else
        {
            GUILayout.Label($"Following: ({targetPosition.x}, {targetPosition.y})");
        }
    }

    private void DrawFacePaintingSettings()
    {
        // Face status selector
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = selectedFaceStatus == 1 ? Color.red : Color.white;
        if (GUILayout.Button("Corrupted")) selectedFaceStatus = 1;
        GUI.backgroundColor = selectedFaceStatus == 2 ? Color.blue : Color.white;
        if (GUILayout.Button("Enhanced")) selectedFaceStatus = 2;
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Duration control
        paintDuration = DebugUIHelpers.DrawIntField("Duration:", paintDuration, -1, 20);
        GUILayout.Label("(-1 = permanent)");
    }

    private void DrawFacePaintingActions()
    {
        // Tile painting setup
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Setup Tile Painting"))
        {
            SetupTilePainting(targetPosition);
        }
        if (GUILayout.Button("Clear Tile Painting"))
        {
            ClearTilePainting(targetPosition);
        }
        GUILayout.EndHorizontal();

        // Cube face painting (direct)
        var cubesAtPosition = FindCubesAt(targetPosition);
        if (cubesAtPosition.Count > 0)
        {
            GUILayout.Label($"Cubes at ({targetPosition.x}, {targetPosition.y}):");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Paint All Faces"))
            {
                foreach (var cube in cubesAtPosition)
                {
                    PaintCubeFace(cube);
                }
            }
            if (GUILayout.Button("Clear All Faces"))
            {
                foreach (var cube in cubesAtPosition)
                {
                    cube.ClearAllFaces();
                }
            }
            GUILayout.EndHorizontal();

            // Individual cube controls
            foreach (var cube in cubesAtPosition.Take(3)) // Show max 3 cubes
            {
            GUILayout.BeginHorizontal();
            string cubeTypeName = GetCubeTypeDisplayName(cube.type);
            GUILayout.Label($"{cubeTypeName}:", GUILayout.Width(60));

                if (GUILayout.Button("Paint", GUILayout.Width(50)))
                {
                    PaintCubeFace(cube);
                }
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    cube.ClearAllFaces();
                }
                if (GUILayout.Button("Select", GUILayout.Width(50)))
                {
                    selectedCube = cube;
                    showCubeInspector = true;
                }
                GUILayout.EndHorizontal();
            }

            if (cubesAtPosition.Count > 3)
            {
                GUILayout.Label($"... and {cubesAtPosition.Count - 3} more");
            }
        }
        else
        {
            GUILayout.Label($"No cubes at ({targetPosition.x}, {targetPosition.y})");
        }
    }

    private void DrawActiveCubesSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("ACTIVE CUBES", GUI.skin.box);

        var allCubes = Object.FindObjectsOfType<CubeManager>()
            .Where(c => c != null && !c.isDestroyed)
            .OrderBy(c => c.position.y)
            .ThenBy(c => c.position.x)
            .ToList();

        if (allCubes.Count == 0)
        {
            GUILayout.Label("No active cubes found");
            GUILayout.EndVertical();
            return;
        }

        // Controls
        GUILayout.Label($"Found: {allCubes.Count} cubes");
        maxCubesToShow = DebugUIHelpers.DrawIntField("Show:", maxCubesToShow, 1, 20);
        
        if (GUILayout.Button("Clear All Faces"))
        {
            foreach (var cube in allCubes)
            {
                cube.ClearAllFaces();
            }
        }

        // Cube list
        activeCubesScroll = GUILayout.BeginScrollView(activeCubesScroll, GUILayout.MinHeight(300));

        int shown = 0;
        foreach (var cube in allCubes)
        {
            if (shown >= maxCubesToShow) break;

            DrawCubeListItem(cube);
            shown++;
        }

        if (allCubes.Count > maxCubesToShow)
        {
            GUILayout.Label($"... and {allCubes.Count - maxCubesToShow} more (increase limit to show)");
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawCubeListItem(CubeManager cube)
    {
        bool isSelected = cube == selectedCube;
        GUI.backgroundColor = isSelected ? Color.yellow : GetCubeColor(cube.type);

        GUILayout.BeginVertical(GUI.skin.box);

        // Header line
        GUILayout.BeginHorizontal();
        string cubeTypeName = GetCubeTypeDisplayName(cube.type);
        GUILayout.Label($"{cubeTypeName} at ({cube.position.x},{cube.position.y})", GUILayout.Width(140));

        if (GUILayout.Button("Select", GUILayout.Width(50)))
        {
            selectedCube = cube;
            showCubeInspector = true;
        }
        GUILayout.EndHorizontal();

        // Status line
        var activeFace = cube.GetCurrentDownFace();
        var activeStatus = cube.GetActiveFaceStatus();
        var effectiveType = cube.GetEffectiveType();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Face: {activeFace} ({activeStatus})", GUILayout.Width(120));

        if (effectiveType != cube.type)
        {
            GUI.color = Color.yellow;
            GUILayout.Label($"→ {effectiveType}");
            GUI.color = Color.white;
        }
        GUILayout.EndHorizontal();

        // Quick actions
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("C", GUILayout.Width(25)))
        {
            cube.SetFaceStatus(activeFace, FaceStatus.Corrupted, paintDuration);
        }
        if (GUILayout.Button("E", GUILayout.Width(25)))
        {
            cube.SetFaceStatus(activeFace, FaceStatus.Enhanced, paintDuration);
        }
        if (GUILayout.Button("X", GUILayout.Width(25)))
        {
            cube.SetFaceStatus(activeFace, FaceStatus.None, 0);
        }
        if (GUILayout.Button("Debug", GUILayout.Width(50)))
        {
            cube.DebugPrintFaceMapping();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUI.backgroundColor = Color.white;
        GUILayout.Space(2);
    }

    private void DrawCubeInspectorSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CUBE INSPECTOR", GUI.skin.box);

        if (selectedCube == null || selectedCube.isDestroyed)
        {
            GUILayout.Label("No cube selected or cube destroyed");
            selectedCube = null;
            GUILayout.EndVertical();
            return;
        }

        inspectorScroll = GUILayout.BeginScrollView(inspectorScroll, GUILayout.MinHeight(400));

        // Basic info
        GUILayout.Label($"Type: {selectedCube.type} | HP: {selectedCube.currentHitPoints}/{selectedCube.maxHitPoints}");
        GUILayout.Label($"Position: ({selectedCube.position.x}, {selectedCube.position.y})");
        GUILayout.Label($"Move Count: {selectedCube.moveCount}");

        GUILayout.Space(5);

        // Face status details
        DrawDetailedFaceInfo();

        GUILayout.Space(5);

        // Face manipulation
        DrawFaceManipulation();

        GUILayout.Space(5);

        // Testing functions
        DrawTestingFunctions();

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawDetailedFaceInfo()
    {
        GUILayout.Label("Face Status Details:", GUI.skin.box);

        var downFace = selectedCube.GetCurrentDownFace();
        var activeStatus = selectedCube.GetActiveFaceStatus();
        var effectiveType = selectedCube.GetEffectiveType();

        GUILayout.Label($"Current Down Face: {downFace}");
        GUILayout.Label($"Active Status: {activeStatus}");
        GUILayout.Label($"Effective Type: {effectiveType}");
        GUILayout.Label($"Can Be Captured: {selectedCube.CanBeCaptured()}");
        GUILayout.Label($"Should Create Detonation: {selectedCube.ShouldCreateDetonation()}");

        GUILayout.Space(3);

        // Show ALL face statuses in a clear table format
        GUILayout.Label("All Face Statuses:", GUI.skin.box);

        // Table header
        GUILayout.BeginHorizontal();
        GUILayout.Label("Face", GUILayout.Width(60));
        GUILayout.Label("Status", GUILayout.Width(80));
        GUILayout.Label("Duration", GUILayout.Width(60));
        GUILayout.Label("Active", GUILayout.Width(50));
        GUILayout.EndHorizontal();

        // Draw each face with full details
        for (int i = 0; i < 4; i++)
        {
            var face = (CubeFace)i;
            var status = selectedCube.GetFaceStatus(face);
            var duration = selectedCube.GetFaceDuration(face);
            bool isCurrentDown = face == downFace;

            // Highlight the current down face
            if (isCurrentDown)
            {
                GUI.backgroundColor = Color.yellow;
            }
            else if (status != FaceStatus.None)
            {
                GUI.backgroundColor = status == FaceStatus.Corrupted ? new Color(1f, 0.5f, 0.5f) : new Color(0.5f, 0.5f, 1f);
            }

            GUILayout.BeginHorizontal(GUI.skin.box);

            GUILayout.Label($"{face}", GUILayout.Width(60));

            string statusText = status == FaceStatus.None ? "None" : status.ToString();
            GUILayout.Label(statusText, GUILayout.Width(80));

            string durationText = status == FaceStatus.None ? "-" : (duration == -1 ? "∞" : duration.ToString());
            GUILayout.Label(durationText, GUILayout.Width(60));

            GUILayout.Label(isCurrentDown ? "YES" : "", GUILayout.Width(50));

            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }
    }

    private void DrawFaceManipulation()
    {
        GUILayout.Label("Face Manipulation:", GUI.skin.box);

        // Paint current down face
        GUILayout.Label("Paint Current Down Face:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Corrupt", GUILayout.Width(60)))
            selectedCube.PaintCurrentDownFace(FaceStatus.Corrupted, Color.red, paintDuration);
        if (GUILayout.Button("Enhance", GUILayout.Width(60)))
            selectedCube.PaintCurrentDownFace(FaceStatus.Enhanced, Color.blue, paintDuration);
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
            selectedCube.PaintCurrentDownFace(FaceStatus.None, Color.white, 0);
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // Paint ALL faces - Multiple face painting
        GUILayout.Label("Paint Multiple Faces:", GUI.skin.box);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("All Corrupt"))
        {
            for (int i = 0; i < 4; i++)
            {
                selectedCube.SetFaceStatus((CubeFace)i, FaceStatus.Corrupted, paintDuration);
            }
        }
        if (GUILayout.Button("All Enhance"))
        {
            for (int i = 0; i < 4; i++)
            {
                selectedCube.SetFaceStatus((CubeFace)i, FaceStatus.Enhanced, paintDuration);
            }
        }
        if (GUILayout.Button("Clear All"))
        {
            selectedCube.ClearAllFaces();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Alternating Pattern"))
        {
            for (int i = 0; i < 4; i++)
            {
                var status = i % 2 == 0 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
                selectedCube.SetFaceStatus((CubeFace)i, status, paintDuration);
            }
        }
        if (GUILayout.Button("Test All Faces"))
        {
            selectedCube.DebugShowAllFaces();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // Paint specific faces - Individual face control
        GUILayout.Label("Individual Face Control:", GUI.skin.box);

        for (int i = 0; i < 4; i++)
        {
            var face = (CubeFace)i;
            var currentStatus = selectedCube.GetFaceStatus(face);
            var currentDuration = selectedCube.GetFaceDuration(face);
            bool isCurrentDown = face == selectedCube.GetCurrentDownFace();

            // Highlight current down face
            if (isCurrentDown)
            {
                GUI.backgroundColor = Color.yellow;
            }

            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{face}:", GUILayout.Width(60));

            if (GUILayout.Button("C", GUILayout.Width(25)))
                selectedCube.SetFaceStatus(face, FaceStatus.Corrupted, paintDuration);
            if (GUILayout.Button("E", GUILayout.Width(25)))
                selectedCube.SetFaceStatus(face, FaceStatus.Enhanced, paintDuration);
            if (GUILayout.Button("X", GUILayout.Width(25)))
                selectedCube.SetFaceStatus(face, FaceStatus.None, 0);

            // Show current status with color coding
            if (currentStatus != FaceStatus.None)
            {
                GUI.color = currentStatus == FaceStatus.Corrupted ? Color.red : Color.cyan;
                string durationText = currentDuration == -1 ? "∞" : currentDuration.ToString();
                GUILayout.Label($"{currentStatus} ({durationText})");
                GUI.color = Color.white;
            }
            else
            {
                GUILayout.Label("None");
            }

            if (isCurrentDown)
            {
                GUI.color = Color.yellow;
                GUILayout.Label("ACTIVE");
                GUI.color = Color.white;
            }

            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.backgroundColor = Color.white;
        }

        GUILayout.Space(5);

        // Duration controls for batch operations
        GUILayout.Label("Batch Duration Control:");
        GUILayout.BeginHorizontal();
        GUILayout.Label("Duration:", GUILayout.Width(60));
        string durationStr = GUILayout.TextField(paintDuration.ToString(), GUILayout.Width(40));
        if (int.TryParse(durationStr, out int newDuration))
            paintDuration = Mathf.Clamp(newDuration, -1, 50);

        if (GUILayout.Button("Set All Durations"))
        {
            for (int i = 0; i < 4; i++)
            {
                var face = (CubeFace)i;
                var status = selectedCube.GetFaceStatus(face);
                if (status != FaceStatus.None)
                {
                    selectedCube.SetFaceStatus(face, status, paintDuration);
                }
            }
        }
        GUILayout.EndHorizontal();
    }

    private void DrawTestingFunctions()
    {
        GUILayout.Label("Testing Functions:", GUI.skin.box);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Force Move"))
        {
            selectedCube.MoveForward();
        }
        if (GUILayout.Button("Take Damage"))
        {
            bool destroyed = selectedCube.TakeDamage(1);
            if (destroyed)
            {
                selectedCube = null;
            }
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Print Face Mapping"))
        {
            selectedCube.DebugPrintFaceMapping();
        }
        if (GUILayout.Button("Test All Face Paints"))
        {
            for (int i = 0; i < 4; i++)
            {
                var face = (CubeFace)i;
                var status = i % 2 == 0 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
                selectedCube.TestPaintFace(face, status);
            }
        }
        GUILayout.EndHorizontal();

        // Recursion cube specific controls
        if (selectedCube.type == CubeType.Recursion || selectedCube.type == CubeType.Recursion) // Backward compatibility
        {
            DrawRecursionCubeControls();
        }
    }

    // Helper methods
    private void SetupTilePainting(Vector2Int position)
    {
        FaceStatus status = selectedFaceStatus == 1 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
        Color color = selectedFaceStatus == 1 ? Color.red : Color.blue;
        DebugTileHelper.SetupTilePainting(position, status, color, paintDuration, gridManager, true, false);
    }

    private void ClearTilePainting(Vector2Int position)
    {
        DebugTileHelper.ClearTilePainting(position, gridManager);
    }

    private void PaintCubeFace(CubeManager cube)
    {
        if (cube == null) return;

        FaceStatus status = selectedFaceStatus == 1 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
        Color color = selectedFaceStatus == 1 ? Color.red : Color.blue;

        CubeFace currentFace = cube.GetCurrentDownFace();
        cube.SetFaceStatus(currentFace, status, paintDuration);

        Debug.Log($"Painted {currentFace} face of {cube.type} cube with {status} status");
    }

    private List<CubeManager> FindCubesAt(Vector2Int position)
    {
        return DebugCubeSpawnHelper.FindCubesAt(position);
    }

    private void DrawRecursionTestsSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("DENSE CUBE TESTING", GUI.skin.box);

        // Quick spawning controls
        GUILayout.Label("Quick Spawn Controls:", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn at Player"))
        {
            SpawnRecursionCubeAtPlayer();
        }
        if (GUILayout.Button("Spawn Multiple"))
        {
            SpawnMultipleRecursionCubes();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // Multi-hit simulation
        GUILayout.Label("Multi-Hit Simulation:", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("1 Hit Test"))
        {
            SimulateMultiHitSequence(1);
        }
        if (GUILayout.Button("2 Hit Test"))
        {
            SimulateMultiHitSequence(2);
        }
        if (GUILayout.Button("3 Hit Test"))
        {
            SimulateMultiHitSequence(3);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // Visual feedback testing
        GUILayout.Label("Visual Feedback Testing:", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Test All Damage States"))
        {
            TestAllDamageStates();
        }
        if (GUILayout.Button("Force Visual Update"))
        {
            ForceVisualUpdateOnAll();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(5);

        // Show current recursion cubes
        DrawCurrentRecursionCubes();

        GUILayout.EndVertical();
    }

    private void DrawRecursionCubeControls()
    {
        GUILayout.Space(5);
        GUILayout.Label("Recursion Cube Controls:", GUI.skin.box);

        // Display current HP with color coding
        float hpRatio = (float)selectedCube.currentHitPoints / selectedCube.maxHitPoints;
        Color hpColor = Color.green;
        if (hpRatio <= 0.33f) hpColor = Color.red;
        else if (hpRatio <= 0.66f) hpColor = Color.yellow;

        GUI.color = hpColor;
        GUILayout.Label($"HP: {selectedCube.currentHitPoints}/{selectedCube.maxHitPoints}");
        GUI.color = Color.white;

        // Direct HP manipulation
        GUILayout.Label("Set HP Directly:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("1 HP", GUILayout.Width(50)))
        {
            SetCubeHP(selectedCube, 1);
        }
        if (GUILayout.Button("2 HP", GUILayout.Width(50)))
        {
            SetCubeHP(selectedCube, 2);
        }
        if (GUILayout.Button("3 HP", GUILayout.Width(50)))
        {
            SetCubeHP(selectedCube, 3);
        }
        if (GUILayout.Button("Full", GUILayout.Width(50)))
        {
            SetCubeHP(selectedCube, selectedCube.maxHitPoints);
        }
        GUILayout.EndHorizontal();

        // Force visual update
        if (GUILayout.Button("Force Visual Update"))
        {
            ForceVisualUpdate(selectedCube);
        }
    }

    private void DrawCurrentRecursionCubes()
    {
        GUILayout.Label("Current Recursion Cubes:", GUI.skin.box);

        var recursionCubes = Object.FindObjectsOfType<CubeManager>()
            .Where(c => c != null && !c.isDestroyed && (c.type == CubeType.Recursion || c.type == CubeType.Recursion))
            .OrderBy(c => c.position.y)
            .ThenBy(c => c.position.x)
            .ToList();

        if (recursionCubes.Count == 0)
        {
            GUILayout.Label("No recursion cubes found");
            return;
        }

        foreach (var cube in recursionCubes.Take(3)) // Show max 3 for UI space
        {
            GUILayout.BeginVertical(GUI.skin.box);

            // HP display with color coding
            float hpRatio = (float)cube.currentHitPoints / cube.maxHitPoints;
            Color hpColor = Color.green;
            if (hpRatio <= 0.33f) hpColor = Color.red;
            else if (hpRatio <= 0.66f) hpColor = Color.yellow;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"({cube.position.x},{cube.position.y})", GUILayout.Width(60));
            
            GUI.color = hpColor;
            GUILayout.Label($"HP: {cube.currentHitPoints}/{cube.maxHitPoints}", GUILayout.Width(70));
            GUI.color = Color.white;

            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                selectedCube = cube;
                showCubeInspector = true;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("-1", GUILayout.Width(30)))
            {
                cube.TakeDamage(1);
            }
            if (GUILayout.Button("1HP", GUILayout.Width(35)))
            {
                SetCubeHP(cube, 1);
            }
            if (GUILayout.Button("2HP", GUILayout.Width(35)))
            {
                SetCubeHP(cube, 2);
            }
            if (GUILayout.Button("3HP", GUILayout.Width(35)))
            {
                SetCubeHP(cube, 3);
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        if (recursionCubes.Count > 3)
        {
            GUILayout.Label($"... and {recursionCubes.Count - 3} more");
        }
    }

    private void SpawnRecursionCubeAtPlayer()
    {
        if (playerManager == null) return;

        Vector2Int playerPos = playerManager.currentTilePosition;
        Vector2Int spawnPos = new Vector2Int(playerPos.x, playerPos.y + 2); // Spawn slightly ahead
        
        SpawnCubeAt(spawnPos, CubeType.Recursion);
        Debug.Log($"Spawned recursion cube at ({spawnPos.x}, {spawnPos.y})");
    }

    private void SpawnMultipleRecursionCubes()
    {
        if (playerManager == null) return;

        Vector2Int playerPos = playerManager.currentTilePosition;
        
        // Spawn 3 recursion cubes in a line
        for (int i = 0; i < 3; i++)
        {
            Vector2Int spawnPos = new Vector2Int(playerPos.x + i - 1, playerPos.y + 3);
            SpawnCubeAt(spawnPos, CubeType.Recursion);
        }
        
        Debug.Log($"Spawned 3 recursion cubes near player position");
    }

    private void SimulateMultiHitSequence(int hitCount)
    {
        var recursionCubes = Object.FindObjectsOfType<CubeManager>()
            .Where(c => c != null && !c.isDestroyed && (c.type == CubeType.Recursion || c.type == CubeType.Recursion))
            .ToList();

        if (recursionCubes.Count == 0)
        {
            Debug.Log("No recursion cubes found for multi-hit simulation");
            return;
        }

        foreach (var cube in recursionCubes)
        {
            // Reset to full HP first
            SetCubeHP(cube, cube.maxHitPoints);
            
            // Apply specified number of hits
            for (int i = 0; i < hitCount; i++)
            {
                bool destroyed = cube.TakeDamage(1);
                if (destroyed) break;
            }
            
            Debug.Log($"Applied {hitCount} hits to recursion cube at ({cube.position.x},{cube.position.y})");
        }
    }

    private void TestAllDamageStates()
    {
        var recursionCubes = Object.FindObjectsOfType<CubeManager>()
            .Where(c => c != null && !c.isDestroyed && (c.type == CubeType.Recursion || c.type == CubeType.Recursion))
            .ToList();

        if (recursionCubes.Count == 0)
        {
            Debug.Log("No recursion cubes found for damage state testing");
            return;
        }

        // Test different damage states
        for (int i = 0; i < recursionCubes.Count && i < 3; i++)
        {
            var cube = recursionCubes[i];
            int targetHP = 3 - i; // 3, 2, 1 HP respectively
            SetCubeHP(cube, targetHP);
            ForceVisualUpdate(cube);
        }
        
        Debug.Log("Set recursion cubes to different damage states for visual testing");
    }

    private void ForceVisualUpdateOnAll()
    {
        var recursionCubes = Object.FindObjectsOfType<CubeManager>()
            .Where(c => c != null && !c.isDestroyed && (c.type == CubeType.Recursion || c.type == CubeType.Recursion))
            .ToList();

        foreach (var cube in recursionCubes)
        {
            ForceVisualUpdate(cube);
        }
        
        Debug.Log($"Forced visual update on {recursionCubes.Count} recursion cubes");
    }

    private void SetCubeHP(CubeManager cube, int hp)
    {
        if (cube == null || (cube.type != CubeType.Recursion && cube.type != CubeType.Recursion)) return;
        
        cube.currentHitPoints = Mathf.Clamp(hp, 1, cube.maxHitPoints);
        ForceVisualUpdate(cube);
        Debug.Log($"Set recursion cube HP to {cube.currentHitPoints}/{cube.maxHitPoints}");
    }

    private void ForceVisualUpdate(CubeManager cube)
    {
        if (cube == null || (cube.type != CubeType.Recursion && cube.type != CubeType.Recursion)) return;
        
        // Force call to UpdateDamageVisual
        cube.UpdateDamageVisual();
    }

    private void SpawnCubeAt(Vector2Int position, CubeType cubeType)
    {
        var waveManager = Object.FindObjectOfType<WaveManager>();
        DebugCubeSpawnHelper.SpawnCubeAt(position, cubeType, gridManager, waveManager);
    }

    private Color GetCubeColor(CubeType type)
    {
        return DebugUIHelpers.GetCubeDisplayColor(type);
    }
    
    private string GetCubeTypeDisplayName(CubeType type)
    {
        switch (type)
        {
            case CubeType.Unit:
                return "Unit";
            case CubeType.Prime:
                return "Prime";
            case CubeType.Infinity:
                return "Infinity";
            case CubeType.Recursion:
                return "Recursion";
            default:
                return type.ToString();
        }
    }
}
using static Enumerations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Individual cube management panel - spawn, select, and manipulate single cubes with full functionality.
/// Focuses on testing individual cube behaviors including face painting, tile status effects, and movement.
/// </summary>
public class CubeIndividualPanel : DebugPanelBase
{
    public override string PanelName => "Individual Cube";
    public override DebugPanelGroup Group => DebugPanelGroup.Cube;

    private GridManager gridManager;
    private PlayerManager playerManager;
    private WaveManager waveManager;

    // UI State
    private bool showCubeSpawner = true;
    private bool showCubeEditor = true;
    private bool showMovementTest = true;
    private bool showTileInteraction = true;
    private Vector2 editorScroll;

    // Spawning controls
    private int selectedCubeType = 0; // CubeType index
    private Vector2Int spawnPosition = new Vector2Int(2, 10);
    private bool autoTrackPlayer = true;

    // Selected cube for editing
    private CubeManager selectedCube = null;

    // Face painting controls
    private int selectedFaceIndex = 0; // 0-3 for North, East, South, West
    private int selectedFaceStatus = 1; // 1=Corrupted, 2=Enhanced
    private int paintDuration = 3;

    // Movement test controls
    private bool autoStep = false;
    private float stepInterval = 1.0f;
    private float lastStepTime = 0f;

    public override void Initialize()
    {
        base.Initialize();
        
        gridManager = GridManager.Instance;
        playerManager = Object.FindObjectOfType<PlayerManager>();
        waveManager = Object.FindObjectOfType<WaveManager>();

        if (playerManager != null)
        {
            spawnPosition = new Vector2Int(playerManager.currentTilePosition.x, playerManager.currentTilePosition.y + 3);
        }
    }

    public override void Update()
    {
        if (autoTrackPlayer && playerManager != null)
        {
            spawnPosition = new Vector2Int(playerManager.currentTilePosition.x, playerManager.currentTilePosition.y + 3);
        }

        // Auto-clear destroyed cube selection
        if (selectedCube != null && (selectedCube.isDestroyed || selectedCube == null))
        {
            selectedCube = null;
        }

        // Auto stepping for movement tests
        if (autoStep && selectedCube != null && !selectedCube.isDestroyed)
        {
            if (Time.time - lastStepTime >= stepInterval)
            {
                selectedCube.MoveForward();
                lastStepTime = Time.time;
                MarkDirty();
            }
        }
    }

    protected override void DrawPanelContent()
    {
        DrawSectionToggles();
        GUILayout.Space(5);

        if (showCubeSpawner) DrawCubeSpawnerSection();
        if (showCubeEditor && selectedCube != null) DrawCubeEditorSection();
        if (showMovementTest && selectedCube != null) DrawMovementTestSection();
        if (showTileInteraction) DrawTileInteractionSection();
    }

    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        showCubeSpawner = DebugUIHelpers.DrawToggleButton("Spawner", showCubeSpawner);
        showCubeEditor = DebugUIHelpers.DrawToggleButton("Editor", showCubeEditor);
        showMovementTest = DebugUIHelpers.DrawToggleButton("Movement", showMovementTest);
        showTileInteraction = DebugUIHelpers.DrawToggleButton("Tile Effects", showTileInteraction);
        GUILayout.EndHorizontal();
    }

    private void DrawCubeSpawnerSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CUBE SPAWNER", GUI.skin.box);

        // Position controls
        DrawPositionControls();

        // Cube type selection
        DrawCubeTypeSelection();

        // Spawn controls
        DrawSpawnControls();

        // Quick selection of nearby cubes
        DrawQuickSelection();

        GUILayout.EndVertical();
    }

    private void DrawPositionControls()
    {
        GUILayout.BeginHorizontal();
        autoTrackPlayer = GUILayout.Toggle(autoTrackPlayer, "Follow Player (+3Y)");
        GUILayout.EndHorizontal();

        if (!autoTrackPlayer)
        {
            spawnPosition = DebugUIHelpers.DrawVector2IntField("Spawn Position:", spawnPosition, 
                0, gridManager?.Width - 1 ?? 10, 0, gridManager?.Height - 1 ?? 20);
        }
        else
        {
            GUILayout.Label($"Auto Position: ({spawnPosition.x}, {spawnPosition.y})");
        }

        // Quick position buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Player+1", GUILayout.Width(70)) && playerManager != null)
        {
            spawnPosition = new Vector2Int(playerManager.currentTilePosition.x, playerManager.currentTilePosition.y + 1);
            autoTrackPlayer = false;
        }
        if (GUILayout.Button("Player+3", GUILayout.Width(70)) && playerManager != null)
        {
            spawnPosition = new Vector2Int(playerManager.currentTilePosition.x, playerManager.currentTilePosition.y + 3);
            autoTrackPlayer = false;
        }
        if (GUILayout.Button("Top Row", GUILayout.Width(70)) && gridManager != null)
        {
            spawnPosition = new Vector2Int(spawnPosition.x, gridManager.Height - 1);
            autoTrackPlayer = false;
        }
        GUILayout.EndHorizontal();
    }

    private void DrawCubeTypeSelection()
    {
        GUILayout.Label("Cube Type:", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        
        // Normal cube
        GUI.backgroundColor = selectedCubeType == 0 ? DebugUIHelpers.GetCubeDisplayColor(CubeType.Unit) : Color.white;
        if (GUILayout.Button("Normal", GUILayout.Width(60)))
            selectedCubeType = 0;

        // Blue cube
        GUI.backgroundColor = selectedCubeType == 1 ? DebugUIHelpers.GetCubeDisplayColor(CubeType.Prime) : Color.white;
        if (GUILayout.Button("Blue", GUILayout.Width(60)))
            selectedCubeType = 1;

        // Black cube
        GUI.backgroundColor = selectedCubeType == 2 ? DebugUIHelpers.GetCubeDisplayColor(CubeType.Infinity) : Color.white;
        if (GUILayout.Button("Black", GUILayout.Width(60)))
            selectedCubeType = 2;

        // Reinforced cube
        GUI.backgroundColor = selectedCubeType == 3 ? DebugUIHelpers.GetCubeDisplayColor(CubeType.Recursion) : Color.white;
        if (GUILayout.Button("Reinforced", GUILayout.Width(80)))
            selectedCubeType = 3;

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Display selected type with description
        CubeType currentType = (CubeType)selectedCubeType;
        GUILayout.Label($"Selected: {currentType} - {GetCubeDescription(currentType)}");
    }

    private void DrawSpawnControls()
    {
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Spawn Cube", GUILayout.Height(30)))
        {
            SpawnCubeAtPosition(spawnPosition, (CubeType)selectedCubeType);
        }

        if (GUILayout.Button("Spawn & Select", GUILayout.Height(30)))
        {
            var newCube = SpawnCubeAtPosition(spawnPosition, (CubeType)selectedCubeType);
            if (newCube != null)
            {
                selectedCube = newCube;
                showCubeEditor = true;
                MarkDirty();
            }
        }
        
        GUILayout.EndHorizontal();

        // Batch spawning
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn Line (3)", GUILayout.Width(100)))
        {
            SpawnLineOfCubes(3);
        }
        if (GUILayout.Button("Clear Area", GUILayout.Width(80)))
        {
            ClearCubesInArea(spawnPosition, 2);
        }
        GUILayout.EndHorizontal();
    }

    private void DrawQuickSelection()
    {
        GUILayout.Label("Quick Select Nearby:", GUI.skin.box);
        
        var nearbyCubes = GetNearbyCubes(spawnPosition, 3);
        if (nearbyCubes.Count == 0)
        {
            GUILayout.Label("No cubes nearby");
            return;
        }

        foreach (var cube in nearbyCubes.Take(4)) // Show max 4 for UI space
        {
            bool isSelected = cube == selectedCube;
            GUI.backgroundColor = isSelected ? Color.yellow : DebugUIHelpers.GetCubeDisplayColor(cube.type);
            
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"{cube.type} at ({cube.position.x},{cube.position.y})", GUILayout.Width(140));
            
            if (GUILayout.Button("Select", GUILayout.Width(50)))
            {
                selectedCube = cube;
                showCubeEditor = true;
                MarkDirty();
            }
            
            if (GUILayout.Button("Delete", GUILayout.Width(50)))
            {
                if (cube == selectedCube) selectedCube = null;
                DestroyCube(cube);
            }
            
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }

        if (nearbyCubes.Count > 4)
        {
            GUILayout.Label($"... and {nearbyCubes.Count - 4} more");
        }
    }

    private void DrawCubeEditorSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CUBE EDITOR", GUI.skin.box);

        if (selectedCube == null || selectedCube.isDestroyed)
        {
            GUILayout.Label("No cube selected or cube destroyed");
            selectedCube = null;
            GUILayout.EndVertical();
            return;
        }

        editorScroll = GUILayout.BeginScrollView(editorScroll, GUILayout.MinHeight(200));

        // Basic info display
        DrawCubeBasicInfo();

        GUILayout.Space(5);

        // Face painting controls
        DrawFacePaintingControls();

        GUILayout.Space(5);

        // Health management (for reinforced cubes)
        if (selectedCube.type == CubeType.Recursion)
        {
            DrawHealthManagement();
            GUILayout.Space(5);
        }

        // Position and movement controls
        DrawCubePositionControls();

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawCubeBasicInfo()
    {
        GUILayout.Label("Basic Information:", GUI.skin.box);
        
        GUILayout.Label($"Type: {selectedCube.type}");
        GUILayout.Label($"Position: ({selectedCube.position.x}, {selectedCube.position.y})");
        GUILayout.Label($"Move Count: {selectedCube.moveCount}");
        
        if (selectedCube.type == CubeType.Recursion)
        {
            float hpRatio = (float)selectedCube.currentHitPoints / selectedCube.maxHitPoints;
            Color hpColor = hpRatio > 0.66f ? Color.green : (hpRatio > 0.33f ? Color.yellow : Color.red);
            
            GUI.color = hpColor;
            GUILayout.Label($"Health: {selectedCube.currentHitPoints}/{selectedCube.maxHitPoints}");
            GUI.color = Color.white;
        }

        // Current status
        var downFace = selectedCube.GetCurrentDownFace();
        var activeStatus = selectedCube.GetActiveFaceStatus();
        var effectiveType = selectedCube.GetEffectiveType();

        GUILayout.Label($"Down Face: {downFace}");
        GUILayout.Label($"Active Status: {activeStatus}");
        
        if (effectiveType != selectedCube.type)
        {
            GUI.color = Color.yellow;
            GUILayout.Label($"Effective Type: {effectiveType}");
            GUI.color = Color.white;
        }

        GUILayout.Label($"Can Be Captured: {selectedCube.CanBeCaptured()}");
        GUILayout.Label($"Creates Detonation: {selectedCube.ShouldCreateDetonation()}");
    }

    private void DrawFacePaintingControls()
    {
        GUILayout.Label("Face Painting:", GUI.skin.box);

        // Face selection
        GUILayout.Label("Select Face:");
        GUILayout.BeginHorizontal();
        for (int i = 0; i < 4; i++)
        {
            var face = (CubeFace)i;
            var currentStatus = selectedCube.GetFaceStatus(face);
            bool isCurrentDown = face == selectedCube.GetCurrentDownFace();
            
            Color buttonColor = Color.white;
            if (selectedFaceIndex == i) buttonColor = Color.cyan;
            else if (isCurrentDown) buttonColor = Color.yellow;
            else if (currentStatus != FaceStatus.None) 
                buttonColor = currentStatus == FaceStatus.Corrupted ? new Color(1f, 0.7f, 0.7f) : new Color(0.7f, 0.7f, 1f);

            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button($"{face}", GUILayout.Width(50)))
            {
                selectedFaceIndex = i;
                MarkDirty();
            }
            GUI.backgroundColor = Color.white;
        }
        GUILayout.EndHorizontal();

        // Status selection
        GUILayout.Label("Paint Type:");
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = selectedFaceStatus == 1 ? Color.red : Color.white;
        if (GUILayout.Button("Corrupted", GUILayout.Width(80)))
            selectedFaceStatus = 1;
        
        GUI.backgroundColor = selectedFaceStatus == 2 ? Color.blue : Color.white;
        if (GUILayout.Button("Enhanced", GUILayout.Width(80)))
            selectedFaceStatus = 2;
        
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Duration control
        paintDuration = DebugUIHelpers.DrawIntField("Duration:", paintDuration, -1, 20);
        GUILayout.Label("(-1 = permanent, 0 = clear)");

        // Paint actions
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Paint Selected Face"))
        {
            var face = (CubeFace)selectedFaceIndex;
            var status = selectedFaceStatus == 1 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
            var color = selectedFaceStatus == 1 ? Color.red : Color.blue;
            selectedCube.PaintFace(face, status, color, paintDuration);
            MarkDirty();
        }

        if (GUILayout.Button("Clear Selected Face"))
        {
            var face = (CubeFace)selectedFaceIndex;
            selectedCube.SetFaceStatus(face, FaceStatus.None, 0);
            MarkDirty();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Paint Current Down"))
        {
            var status = selectedFaceStatus == 1 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
            var color = selectedFaceStatus == 1 ? Color.red : Color.blue;
            selectedCube.PaintCurrentDownFace(status, color, paintDuration);
            MarkDirty();
        }

        if (GUILayout.Button("Clear All Faces"))
        {
            selectedCube.ClearAllFaces();
            MarkDirty();
        }
        GUILayout.EndHorizontal();

        // Face status display
        DrawFaceStatusDisplay();
    }

    private void DrawFaceStatusDisplay()
    {
        GUILayout.Label("Face Status Overview:");
        
        for (int i = 0; i < 4; i++)
        {
            var face = (CubeFace)i;
            var status = selectedCube.GetFaceStatus(face);
            var duration = selectedCube.GetFaceDuration(face);
            bool isCurrentDown = face == selectedCube.GetCurrentDownFace();
            bool isSelected = i == selectedFaceIndex;

            Color bgColor = Color.white;
            if (isSelected) bgColor = Color.cyan;
            else if (isCurrentDown) bgColor = Color.yellow;
            else if (status != FaceStatus.None) 
                bgColor = status == FaceStatus.Corrupted ? new Color(1f, 0.8f, 0.8f) : new Color(0.8f, 0.8f, 1f);

            GUI.backgroundColor = bgColor;
            GUILayout.BeginHorizontal(GUI.skin.box);
            
            GUILayout.Label($"{face}:", GUILayout.Width(60));
            
            string statusText = status == FaceStatus.None ? "None" : status.ToString();
            GUILayout.Label(statusText, GUILayout.Width(80));
            
            string durationText = status == FaceStatus.None ? "-" : (duration == -1 ? "∞" : duration.ToString());
            GUILayout.Label(durationText, GUILayout.Width(40));
            
            if (isCurrentDown) GUILayout.Label("ACTIVE", GUILayout.Width(50));
            
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
        }
    }

    private void DrawHealthManagement()
    {
        GUILayout.Label("Health Management (Reinforced):", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Damage -1", GUILayout.Width(70)))
        {
            bool destroyed = selectedCube.TakeDamage(1);
            if (destroyed) selectedCube = null;
        }
        
        if (GUILayout.Button("Set 1 HP", GUILayout.Width(60)))
        {
            SetCubeHP(1);
        }
        
        if (GUILayout.Button("Set 2 HP", GUILayout.Width(60)))
        {
            SetCubeHP(2);
        }
        
        if (GUILayout.Button("Full HP", GUILayout.Width(60)))
        {
            SetCubeHP(selectedCube.maxHitPoints);
        }
        GUILayout.EndHorizontal();
    }

    private void DrawMovementTestSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("MOVEMENT TESTING", GUI.skin.box);

        if (selectedCube == null || selectedCube.isDestroyed)
        {
            GUILayout.Label("No cube selected for movement testing");
            GUILayout.EndVertical();
            return;
        }

        // Manual movement controls
        GUILayout.Label("Manual Movement:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Step Forward"))
        {
            selectedCube.MoveForward();
            MarkDirty();
        }
        
        if (GUILayout.Button("Force Position"))
        {
            // Move cube to specific position (for testing)
            Vector2Int newPos = new Vector2Int(selectedCube.position.x, selectedCube.position.y - 1);
            if (gridManager.IsValidGridPosition(newPos))
            {
                selectedCube.position = newPos;
                Vector3 worldPos = gridManager.GridToWorldPosition(newPos.x, newPos.y, 2f);
                selectedCube.transform.position = worldPos;
                MarkDirty();
            }
        }
        GUILayout.EndHorizontal();

        // Auto stepping controls
        GUILayout.Space(5);
        GUILayout.Label("Auto Movement:");
        
        GUILayout.BeginHorizontal();
        autoStep = GUILayout.Toggle(autoStep, "Auto Step");
        GUILayout.Label("Interval:", GUILayout.Width(50));
        string intervalStr = GUILayout.TextField(stepInterval.ToString("F1"), GUILayout.Width(40));
        if (float.TryParse(intervalStr, out float newInterval))
            stepInterval = Mathf.Clamp(newInterval, 0.1f, 5.0f);
        GUILayout.Label("sec");
        GUILayout.EndHorizontal();

        if (autoStep)
        {
            float timeLeft = stepInterval - (Time.time - lastStepTime);
            GUILayout.Label($"Next step in: {timeLeft:F1}s");
        }

        // Movement information
        GUILayout.Space(5);
        GUILayout.Label("Movement Info:");
        GUILayout.Label($"Current Position: ({selectedCube.position.x}, {selectedCube.position.y})");
        GUILayout.Label($"Move Count: {selectedCube.moveCount}");
        GUILayout.Label($"Is Moving: {selectedCube.isMoving}");

        GUILayout.EndVertical();
    }

    private void DrawTileInteractionSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("TILE INTERACTION TESTING", GUI.skin.box);

        // Tile status setup
        GUILayout.Label("Setup Tile Effects:", GUI.skin.box);
        
        Vector2Int testPosition = selectedCube != null ? selectedCube.position : spawnPosition;
        testPosition = DebugUIHelpers.DrawVector2IntField("Test Position:", testPosition, 
            0, gridManager?.Width - 1 ?? 10, 0, gridManager?.Height - 1 ?? 20);

        // Tile painting setup
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Corrupted Tile"))
        {
            SetupTileWithStatus(testPosition, FaceStatus.Corrupted, Color.red, paintDuration);
        }
        
        if (GUILayout.Button("Enhanced Tile"))
        {
            SetupTileWithStatus(testPosition, FaceStatus.Enhanced, Color.blue, paintDuration);
        }
        
        if (GUILayout.Button("Clear Tile"))
        {
            ClearTilePainting(testPosition);
        }
        GUILayout.EndHorizontal();

        // Test area setup
        GUILayout.Label("Test Scenarios:", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Paint Path"))
        {
            SetupPaintedPath();
        }
        
        if (GUILayout.Button("Mixed Tiles"))
        {
            SetupMixedTilePattern();
        }
        
        if (GUILayout.Button("Clear All Tiles"))
        {
            ClearAllTilePainting();
        }
        GUILayout.EndHorizontal();

        // Tile status display
        if (gridManager != null)
        {
            var tile = gridManager.GetTileAt(testPosition);
            if (tile != null)
            {
                GUILayout.Space(5);
                GUILayout.Label($"Tile at ({testPosition.x}, {testPosition.y}):");
                GUILayout.Label($"Can Paint Cubes: {tile.CanPaintCubes}");
                if (tile.CanPaintCubes)
                {
                    GUILayout.Label($"Paint Status: {tile.PaintStatus}");
                    GUILayout.Label($"Paint Duration: {tile.PaintDuration}");
                }
            }
        }

        GUILayout.EndVertical();
    }

    // Helper methods
    private CubeManager SpawnCubeAtPosition(Vector2Int position, CubeType cubeType)
    {
        if (waveManager?.cubePrefabs == null || (int)cubeType >= waveManager.cubePrefabs.Length)
        {
            Debug.LogWarning($"Cannot spawn cube type {cubeType}");
            return null;
        }

        if (!gridManager.IsValidGridPosition(position))
        {
            Debug.LogWarning($"Invalid spawn position ({position.x}, {position.y})");
            return null;
        }

        // Check if position is occupied
        if (FindCubeAt(position) != null)
        {
            Debug.LogWarning($"Position ({position.x}, {position.y}) is already occupied");
            return null;
        }

        Vector3 worldPos = gridManager.GridToWorldPosition(position.x, position.y, 2f);
        GameObject cubeObj = Object.Instantiate(waveManager.cubePrefabs[(int)cubeType], worldPos, Quaternion.identity);

        var cube = cubeObj.GetComponent<CubeManager>();
        if (cube == null) cube = cubeObj.AddComponent<CubeManager>();

        var cubeData = new CubeData
        {
            type = cubeType,
            position = position,
            level = 1
        };

        cube.Init(gridManager, cubeData, 2f);
        waveManager?.activeCubes.Add(cube);

        Debug.Log($"Spawned {cubeType} cube at ({position.x}, {position.y})");
        return cube;
    }

    private void SpawnLineOfCubes(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = new Vector2Int(spawnPosition.x + i - count/2, spawnPosition.y);
            SpawnCubeAtPosition(pos, (CubeType)selectedCubeType);
        }
    }

    private void ClearCubesInArea(Vector2Int center, int radius)
    {
        var cubes = Object.FindObjectsOfType<CubeManager>();
        foreach (var cube in cubes)
        {
            if (cube != null && !cube.isDestroyed)
            {
                float distance = Vector2Int.Distance(cube.position, center);
                if (distance <= radius)
                {
                    if (cube == selectedCube) selectedCube = null;
                    DestroyCube(cube);
                }
            }
        }
    }

    private void DestroyCube(CubeManager cube)
    {
        if (cube != null)
        {
            waveManager?.activeCubes.Remove(cube);
            Object.DestroyImmediate(cube.gameObject);
        }
    }

    private CubeManager FindCubeAt(Vector2Int position)
    {
        var cubes = Object.FindObjectsOfType<CubeManager>();
        return cubes.FirstOrDefault(c => c != null && !c.isDestroyed && c.position == position);
    }

    private List<CubeManager> GetNearbyCubes(Vector2Int center, int maxDistance)
    {
        var cubes = Object.FindObjectsOfType<CubeManager>();
        return cubes.Where(c => c != null && !c.isDestroyed && 
                               Vector2Int.Distance(c.position, center) <= maxDistance)
                   .OrderBy(c => Vector2Int.Distance(c.position, center))
                   .ToList();
    }

    private void DrawCubePositionControls()
    {
        GUILayout.Label("Position Management:", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("↑", GUILayout.Width(30)))
        {
            MoveCubeBy(0, 1);
        }
        if (GUILayout.Button("↓", GUILayout.Width(30)))
        {
            MoveCubeBy(0, -1);
        }
        if (GUILayout.Button("←", GUILayout.Width(30)))
        {
            MoveCubeBy(-1, 0);
        }
        if (GUILayout.Button("→", GUILayout.Width(30)))
        {
            MoveCubeBy(1, 0);
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("To Top", GUILayout.Width(60)))
        {
            MoveCubeTo(selectedCube.position.x, gridManager.Height - 1);
        }
        if (GUILayout.Button("Center X", GUILayout.Width(60)))
        {
            MoveCubeTo(gridManager.Width / 2, selectedCube.position.y);
        }
        if (GUILayout.Button("To Player", GUILayout.Width(70)) && playerManager != null)
        {
            MoveCubeTo(playerManager.currentTilePosition.x, playerManager.currentTilePosition.y + 2);
        }
        GUILayout.EndHorizontal();
    }
    
    private void MoveCubeBy(int deltaX, int deltaY)
    {
        if (selectedCube == null || selectedCube.isDestroyed) return;
        
        Vector2Int newPos = new Vector2Int(
            Mathf.Clamp(selectedCube.position.x + deltaX, 0, gridManager.Width - 1),
            Mathf.Clamp(selectedCube.position.y + deltaY, 0, gridManager.Height - 1)
        );
        
        MoveCubeTo(newPos.x, newPos.y);
    }
    
    private void MoveCubeTo(int x, int y)
    {
        if (selectedCube == null || selectedCube.isDestroyed) return;
        
        Vector2Int newPos = new Vector2Int(x, y);
        if (!gridManager.IsValidGridPosition(newPos)) return;
        
        // Check if position is occupied
        if (FindCubeAt(newPos) != null && FindCubeAt(newPos) != selectedCube)
        {
            Debug.LogWarning($"Position ({newPos.x}, {newPos.y}) is occupied");
            return;
        }
        
        selectedCube.position = newPos;
        Vector3 worldPos = gridManager.GridToWorldPosition(newPos.x, newPos.y, 2f);
        selectedCube.transform.position = worldPos;
        MarkDirty();
    }

    private void SetCubeHP(int hp)
    {
        if (selectedCube == null || selectedCube.type != CubeType.Recursion) return;
        
        selectedCube.currentHitPoints = Mathf.Clamp(hp, 1, selectedCube.maxHitPoints);
        selectedCube.UpdateDamageVisual();
        MarkDirty();
    }

    private void SetupTileWithStatus(Vector2Int position, FaceStatus status, Color color, int duration)
    {
        if (!gridManager.IsValidGridPosition(position)) return;

        var tile = gridManager.GetTileAt(position);
        if (tile != null)
        {
            tile.SetupFacePainting(status, color, duration, true, false);
            Debug.Log($"Setup tile at ({position.x}, {position.y}) to paint cubes with {status} status");
        }
    }

    public void SetupTilePainting(Vector2Int position)
    {
        FaceStatus status = selectedFaceStatus == 1 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
        Color color = selectedFaceStatus == 1 ? Color.red : Color.blue;
        SetupTileWithStatus(position, status, color, paintDuration);
    }

    private void ClearTilePainting(Vector2Int position)
    {
        if (!gridManager.IsValidGridPosition(position)) return;

        var tile = gridManager.GetTileAt(position);
        if (tile != null)
        {
            tile.DisableFacePainting();
            Debug.Log($"Cleared face painting from tile at ({position.x}, {position.y})");
        }
    }

    private void SetupPaintedPath()
    {
        Vector2Int startPos = selectedCube != null ? selectedCube.position : spawnPosition;
        
        // Create a path of alternating corrupted and enhanced tiles
        for (int i = 0; i < 5; i++)
        {
            Vector2Int pos = new Vector2Int(startPos.x, startPos.y - i);
            if (gridManager.IsValidGridPosition(pos))
            {
                var status = i % 2 == 0 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
                var color = i % 2 == 0 ? Color.red : Color.blue;
                SetupTileWithStatus(pos, status, color, paintDuration);
            }
        }
    }

    private void SetupMixedTilePattern()
    {
        Vector2Int center = selectedCube != null ? selectedCube.position : spawnPosition;
        
        // Create a 3x3 pattern of different tile effects
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int pos = new Vector2Int(center.x + x, center.y + y);
                if (gridManager.IsValidGridPosition(pos))
                {
                    if ((x + y) % 2 == 0)
                    {
                        SetupTileWithStatus(pos, FaceStatus.Corrupted, Color.red, paintDuration);
                    }
                    else if (x == 0 || y == 0)
                    {
                        SetupTileWithStatus(pos, FaceStatus.Enhanced, Color.blue, paintDuration);
                    }
                }
            }
        }
    }

    private void ClearAllTilePainting()
    {
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                ClearTilePainting(new Vector2Int(x, y));
            }
        }
    }

    private string GetCubeDescription(CubeType type)
    {
        switch (type)
        {
            case CubeType.Unit: return "Standard cube, can be painted";
            case CubeType.Prime: return "Valuable cube, creates detonations";
            case CubeType.Infinity: return "Dangerous cube, avoid capture";
            case CubeType.Recursion: return "Multi-hit cube, requires damage";
            default: return "Unknown cube type";
        }
    }
}

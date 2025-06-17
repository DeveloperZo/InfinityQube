using static Enumerations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerActionDebugPanel : DebugPanelBase
{
    public override string PanelName => "Player Actions";
    public override DebugPanelGroup Group => DebugPanelGroup.Gameplay;

    private PlayerActionManager actionManager;
    private PlayerManager playerManager;
    private GridManager gridManager;

    // UI State
    private bool showMarkerManagement = true;
    private bool showActionInspection = true;
    private bool showMarkerTesting = true;
    private bool showActionOperations = false;
    private Vector2 markerListScroll;

    // Marker placement controls
    private int selectedMarkerType = 0; // 0=Individual, 1=Area, 2=Cube
    private Vector2Int targetPosition = new Vector2Int(0, 0);
    private bool autoTrackPlayer = true;
    private int areaMarkerSize = 2;
    private int cubeMarkerType = 0; // 0=Individual, 1=Area

    // Testing settings
    private int testMarkerCount = 5;
    private float testTriggerDelay = 1f;
    private bool showCooldownTimers = true;

    public override void Initialize()
    {
        actionManager = Object.FindObjectOfType<PlayerActionManager>();
        playerManager = Object.FindObjectOfType<PlayerManager>();
        gridManager = GridManager.Instance;

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
    }

    public override void DrawPanel()
    {
        DrawSectionToggles();
        GUILayout.Space(5);

        if (showMarkerManagement) DrawMarkerManagementSection();
        if (showActionInspection) DrawActionInspectionSection();
        if (showMarkerTesting) DrawMarkerTestingSection();
        if (showActionOperations) DrawActionOperationsSection();
    }

    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        showMarkerManagement = DrawToggleButton("Markers", showMarkerManagement);
        showActionInspection = DrawToggleButton("Inspection", showActionInspection);
        showMarkerTesting = DrawToggleButton("Testing", showMarkerTesting);
        showActionOperations = DrawToggleButton("Operations", showActionOperations);
        GUILayout.EndHorizontal();
    }

    private bool DrawToggleButton(string label, bool current)
    {
        GUI.backgroundColor = current ? Color.cyan : Color.white;
        bool result = GUILayout.Button(label, GUILayout.Height(25));
        GUI.backgroundColor = Color.white;
        return result ? !current : current;
    }

    private void DrawMarkerManagementSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("MARKER MANAGEMENT", GUI.skin.box);

        if (actionManager != null)
        {
            // Current marker counts
            DrawMarkerCounts();

            GUILayout.Space(5);

            // Target position controls
            DrawTargetPositionControls();

            // Marker type selector
            DrawMarkerTypeSelector();

            // Placement controls
            DrawPlacementControls();

            // Quick trigger controls
            DrawQuickTriggerControls();
        }
        else
        {
            GUILayout.Label("PlayerActionManager not found!");
        }

        GUILayout.EndVertical();
    }

    private void DrawMarkerCounts()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Current Markers:");

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Individual: {actionManager.GetCurrentIndividualMarkers()}/3", GUILayout.Width(100));
        GUILayout.Label($"Area: {actionManager.GetCurrentAreaMarkers()}/2", GUILayout.Width(80));
        GUILayout.Label($"Cube: {actionManager.GetCurrentCubeMarkers()}", GUILayout.Width(60));
        GUILayout.EndHorizontal();

        // Cooldown information
        if (showCooldownTimers)
        {
            float individualCD = actionManager.GetIndividualMarkerCooldownRemaining();
            float areaCD = actionManager.GetAreaMarkerCooldownRemaining();

            if (individualCD > 0 || areaCD > 0)
            {
                GUILayout.BeginHorizontal();
                if (individualCD > 0)
                    GUILayout.Label($"Individual CD: {individualCD:F1}s", GUILayout.Width(120));
                if (areaCD > 0)
                    GUILayout.Label($"Area CD: {areaCD:F1}s", GUILayout.Width(100));
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndVertical();
    }

    private void DrawTargetPositionControls()
    {
        GUILayout.BeginHorizontal();
        autoTrackPlayer = GUILayout.Toggle(autoTrackPlayer, "Track Player");

        if (!autoTrackPlayer)
        {
            GUILayout.Label("X:", GUILayout.Width(15));
            if (GUILayout.Button("-", GUILayout.Width(20)) && targetPosition.x > 0)
                targetPosition.x--;
            GUILayout.Label($"{targetPosition.x}", GUILayout.Width(20));
            if (GUILayout.Button("+", GUILayout.Width(20)) && targetPosition.x < (gridManager?.Width - 1 ?? 10))
                targetPosition.x++;

            GUILayout.Label("Y:", GUILayout.Width(15));
            if (GUILayout.Button("-", GUILayout.Width(20)) && targetPosition.y > 0)
                targetPosition.y--;
            GUILayout.Label($"{targetPosition.y}", GUILayout.Width(20));
            if (GUILayout.Button("+", GUILayout.Width(20)) && targetPosition.y < (gridManager?.Height - 1 ?? 20))
                targetPosition.y++;
        }
        else
        {
            GUILayout.Label($"Target: ({targetPosition.x}, {targetPosition.y})");
        }
        GUILayout.EndHorizontal();

        // Show target position status
        if (gridManager != null && gridManager.IsValidGridPosition(targetPosition))
        {
            bool hasIndividual = actionManager.HasIndividualMarkerAt(targetPosition);
            bool hasArea = actionManager.HasAreaMarkerAt(targetPosition);

            string status = "Empty";
            if (hasIndividual && hasArea) status = "Individual + Area";
            else if (hasIndividual) status = "Individual Marker";
            else if (hasArea) status = "Area Marker";

            GUILayout.Label($"Target Status: {status}");
        }
    }

    private void DrawMarkerTypeSelector()
    {
        GUILayout.Label("Marker Type:");
        GUILayout.BeginHorizontal();

        GUI.backgroundColor = selectedMarkerType == 0 ? Color.red : Color.white;
        if (GUILayout.Button("Individual")) selectedMarkerType = 0;

        GUI.backgroundColor = selectedMarkerType == 1 ? Color.green : Color.white;
        if (GUILayout.Button("Area")) selectedMarkerType = 1;

        GUI.backgroundColor = selectedMarkerType == 2 ? Color.magenta : Color.white;
        if (GUILayout.Button("Cube")) selectedMarkerType = 2;

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Type-specific settings
        if (selectedMarkerType == 1) // Area
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Size:", GUILayout.Width(40));
            if (GUILayout.Button("-", GUILayout.Width(20)) && areaMarkerSize > 1)
                areaMarkerSize--;
            GUILayout.Label($"{areaMarkerSize}x{areaMarkerSize}", GUILayout.Width(40));
            if (GUILayout.Button("+", GUILayout.Width(20)) && areaMarkerSize < 5)
                areaMarkerSize++;
            GUILayout.EndHorizontal();
        }
        else if (selectedMarkerType == 2) // Cube
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type:", GUILayout.Width(40));
            GUI.backgroundColor = cubeMarkerType == 0 ? Color.magenta : Color.white;
            if (GUILayout.Button("Individual (1x1)", GUILayout.Width(90))) cubeMarkerType = 0;
            GUI.backgroundColor = cubeMarkerType == 1 ? Color.cyan : Color.white;
            if (GUILayout.Button("Area (3x3)", GUILayout.Width(80))) cubeMarkerType = 1;
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Label("Individual marker + Blue cube = Individual cube marker (1x1)");
            GUILayout.Label("Area marker + Blue cube = Area cube marker (3x3)");
        }
    }

    private void DrawPlacementControls()
    {
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Place at Target"))
        {
            PlaceMarkerAtTarget();
        }

        if (GUILayout.Button("Remove at Target"))
        {
            RemoveMarkerAtTarget();
        }

        if (GUILayout.Button("Toggle at Target"))
        {
            ToggleMarkerAtTarget();
        }

        GUILayout.EndHorizontal();

        // Capability indicators
        GUILayout.BeginHorizontal();

        bool canPlaceIndividual = actionManager.CanPlaceIndividualMarkerCheck();
        bool canPlaceArea = actionManager.CanPlaceAreaMarkerCheck();

        GUI.color = canPlaceIndividual ? Color.green : Color.red;
        GUILayout.Label("Individual", GUILayout.Width(70));

        GUI.color = canPlaceArea ? Color.green : Color.red;
        GUILayout.Label("Area", GUILayout.Width(50));

        GUI.color = Color.white;
        GUILayout.EndHorizontal();
    }

    private void DrawQuickTriggerControls()
    {
        GUILayout.Label("Quick Triggers:");
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Trigger Individual"))
        {
            actionManager.TriggerNextIndividualMarker();
        }

        if (GUILayout.Button("Trigger Area"))
        {
            actionManager.TriggerNextAreaMarker();
        }

        if (GUILayout.Button("Trigger Cube"))
        {
            actionManager.TriggerNextCubeMarker();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Trigger All Individual"))
        {
            TriggerAllIndividualMarkers();
        }

        if (GUILayout.Button("Trigger All Area"))
        {
            TriggerAllAreaMarkers();
        }

        if (GUILayout.Button("Clear All"))
        {
            actionManager.ClearAllActions();
        }
        GUILayout.EndHorizontal();
    }

    private void DrawActionInspectionSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("ACTION INSPECTION", GUI.skin.box);

        if (actionManager != null)
        {
            // Statistics overview
            DrawActionStatistics();

            GUILayout.Space(5);

            // Active markers list
            DrawActiveMarkersList();

            GUILayout.Space(5);

            // Settings inspection
            DrawActionSettings();
        }

        GUILayout.EndVertical();
    }

    private void DrawActionStatistics()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Statistics:");

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Individual Placed: {actionManager.GetIndividualMarkersPlaced()}", GUILayout.Width(130));
        GUILayout.Label($"Area Placed: {actionManager.GetAreaMarkersPlaced()}", GUILayout.Width(100));
        GUILayout.Label($"Cube Triggered: {actionManager.GetCubeMarkersTriggered()}", GUILayout.Width(110));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Perfect Timing: {actionManager.GetPerfectTimingHits()}", GUILayout.Width(120));
        if (GUILayout.Button("Reset Stats", GUILayout.Width(80)))
        {
            actionManager.ResetStatistics();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawActiveMarkersList()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Active Markers:");

        markerListScroll = GUILayout.BeginScrollView(markerListScroll, GUILayout.MaxHeight(200));

        // Individual markers
        var individualMarkers = actionManager.individualMarkers.ToArray();
        if (individualMarkers.Length > 0)
        {
            GUILayout.Label("Individual Markers:", GUI.skin.box);
            foreach (var marker in individualMarkers.Take(10))
            {
                DrawIndividualMarkerItem(marker);
            }
        }

        // Area markers
        var areaMarkers = actionManager.areaMarkers.ToArray();
        if (areaMarkers.Length > 0)
        {
            GUILayout.Label("Area Markers:", GUI.skin.box);
            foreach (var marker in areaMarkers.Take(5))
            {
                DrawAreaMarkerItem(marker);
            }
        }

        // Cube markers (if accessible)
        int cubeMarkerCount = actionManager.GetCurrentCubeMarkers();
        if (cubeMarkerCount > 0)
        {
            GUILayout.Label($"Cube Markers: {cubeMarkerCount} active", GUI.skin.box);
            var nextCubeMarker = actionManager.GetNextCubeMarker();
            if (nextCubeMarker.x >= 0)
            {
                GUILayout.Label($"Next: ({nextCubeMarker.x}, {nextCubeMarker.y})");
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawIndividualMarkerItem(PlayerActionManager.IndividualMarker marker)
    {
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label($"({marker.position.x},{marker.position.y})", GUILayout.Width(60));

        float age = Time.time - marker.placementTime;
        GUILayout.Label($"Age: {age:F1}s", GUILayout.Width(70));

        if (marker.isPerfectTiming)
        {
            GUI.color = Color.yellow;
            GUILayout.Label("PERFECT", GUILayout.Width(60));
            GUI.color = Color.white;
        }

        if (GUILayout.Button("Go To", GUILayout.Width(50)))
        {
            targetPosition = marker.position;
            autoTrackPlayer = false;
        }
        GUILayout.EndHorizontal();
    }

    private void DrawAreaMarkerItem(PlayerActionManager.AreaMarker marker)
    {
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label($"Center: ({marker.centerPosition.x},{marker.centerPosition.y})", GUILayout.Width(100));
        GUILayout.Label($"Size: {marker.size}x{marker.size}", GUILayout.Width(60));
        GUILayout.Label($"Tiles: {marker.affectedPositions.Count}", GUILayout.Width(60));

        if (GUILayout.Button("Go To", GUILayout.Width(50)))
        {
            targetPosition = marker.centerPosition;
            autoTrackPlayer = false;
        }
        GUILayout.EndHorizontal();
    }

    private void DrawActionSettings()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Action Settings:");

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Individual Max: {actionManager.maxIndividualMarkers}", GUILayout.Width(110));
        GUILayout.Label($"Area Max: {actionManager.maxAreaMarkers}", GUILayout.Width(80));
        GUILayout.Label($"Area Size: {actionManager.areaMarkerSize}", GUILayout.Width(80));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Individual CD: {actionManager.individualMarkerCooldown:F1}s", GUILayout.Width(120));
        GUILayout.Label($"Area CD: {actionManager.areaMarkerCooldown:F1}s", GUILayout.Width(100));
        GUILayout.EndHorizontal();

        showCooldownTimers = GUILayout.Toggle(showCooldownTimers, "Show Cooldown Timers");

        GUILayout.EndVertical();
    }

    private void DrawMarkerTestingSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("MARKER TESTING", GUI.skin.box);

        // Test settings
        DrawTestSettings();

        GUILayout.Space(5);

        // Test patterns
        DrawTestPatterns();

        GUILayout.Space(5);

        // Performance tests
        DrawPerformanceTests();

        GUILayout.EndVertical();
    }

    private void DrawTestSettings()
    {
        GUILayout.Label("Test Settings:");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Count:", GUILayout.Width(50));
        if (GUILayout.Button("-", GUILayout.Width(20)) && testMarkerCount > 1)
            testMarkerCount--;
        GUILayout.Label($"{testMarkerCount}", GUILayout.Width(20));
        if (GUILayout.Button("+", GUILayout.Width(20)) && testMarkerCount < 20)
            testMarkerCount++;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Delay:", GUILayout.Width(50));
        if (GUILayout.Button("-", GUILayout.Width(20)) && testTriggerDelay > 0.1f)
            testTriggerDelay -= 0.1f;
        GUILayout.Label($"{testTriggerDelay:F1}s", GUILayout.Width(40));
        if (GUILayout.Button("+", GUILayout.Width(20)) && testTriggerDelay < 5f)
            testTriggerDelay += 0.1f;
        GUILayout.EndHorizontal();
    }

    private void DrawTestPatterns()
    {
        GUILayout.Label("Test Patterns:");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Line Pattern"))
        {
            CreateLinePattern();
        }
        if (GUILayout.Button("Grid Pattern"))
        {
            CreateGridPattern();
        }
        if (GUILayout.Button("Random Pattern"))
        {
            CreateRandomPattern();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Around Player"))
        {
            CreatePatternAroundPlayer();
        }
        if (GUILayout.Button("Top Row"))
        {
            CreateTopRowPattern();
        }
        if (GUILayout.Button("Corners"))
        {
            CreateCornerPattern();
        }
        GUILayout.EndHorizontal();
    }

    private void DrawPerformanceTests()
    {
        GUILayout.Label("Performance Tests:");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Rapid Place/Remove"))
        {
            StartCoroutine(RapidPlaceRemoveTest());
        }
        if (GUILayout.Button("Trigger Stress Test"))
        {
            StartCoroutine(TriggerStressTest());
        }
        GUILayout.EndHorizontal();
    }

    private void DrawActionOperationsSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("ACTION OPERATIONS", GUI.skin.box);

        // Batch operations
        DrawBatchOperations();

        GUILayout.Space(5);

        // System operations
        DrawSystemOperations();

        GUILayout.Space(5);

        // Debug operations
        DrawDebugOperations();

        GUILayout.EndVertical();
    }

    private void DrawBatchOperations()
    {
        GUILayout.Label("Batch Operations:");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill Individual"))
        {
            FillIndividualMarkers();
        }
        if (GUILayout.Button("Fill Area"))
        {
            FillAreaMarkers();
        }
        if (GUILayout.Button("Clear All"))
        {
            actionManager.ClearAllActions();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Trigger All"))
        {
            TriggerAllMarkers();
        }
        if (GUILayout.Button("Remove All Individual"))
        {
            RemoveAllIndividualMarkers();
        }
        if (GUILayout.Button("Remove All Area"))
        {
            RemoveAllAreaMarkers();
        }
        GUILayout.EndHorizontal();
    }

    private void DrawSystemOperations()
    {
        GUILayout.Label("System Operations:");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset Cooldowns"))
        {
            ResetCooldowns();
        }
        if (GUILayout.Button("Max Charges"))
        {
            SetMaxCharges();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Test Input Handling"))
        {
            TestInputHandling();
        }
        if (GUILayout.Button("Validate System"))
        {
            ValidateActionSystem();
        }
        GUILayout.EndHorizontal();
    }

    private void DrawDebugOperations()
    {
        GUILayout.Label("Debug Operations:");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Print Action State"))
        {
            PrintActionSystemState();
        }
        if (GUILayout.Button("Log Marker Positions"))
        {
            LogAllMarkerPositions();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Simulate Perfect Timing"))
        {
            SimulatePerfectTiming();
        }
        if (GUILayout.Button("Test Edge Cases"))
        {
            TestEdgeCases();
        }
        GUILayout.EndHorizontal();
    }

    // Implementation methods
    private void PlaceMarkerAtTarget()
    {
        switch (selectedMarkerType)
        {
            case 0: // Individual
                actionManager.PlaceIndividualMarker(targetPosition);
                break;
            case 1: // Area
                actionManager.PlaceAreaMarker(targetPosition, areaMarkerSize);
                break;
            case 2: // Cube
                var cubeMarkerType = (PlayerActionManager.CubeMarkerType)this.cubeMarkerType;
                actionManager.CreateCubeMarker(targetPosition, cubeMarkerType);
                break;
        }
    }

    private void RemoveMarkerAtTarget()
    {
        if (actionManager.HasIndividualMarkerAt(targetPosition))
        {
            actionManager.RemoveIndividualMarkerAt(targetPosition);
        }
        if (actionManager.HasAreaMarkerAt(targetPosition))
        {
            actionManager.RemoveAreaMarkerAt(targetPosition);
        }
    }

    private void ToggleMarkerAtTarget()
    {
        bool hasMarker = actionManager.HasIndividualMarkerAt(targetPosition) ||
                        actionManager.HasAreaMarkerAt(targetPosition);

        if (hasMarker)
        {
            RemoveMarkerAtTarget();
        }
        else
        {
            PlaceMarkerAtTarget();
        }
    }

    private void TriggerAllIndividualMarkers()
    {
        int count = actionManager.GetCurrentIndividualMarkers();
        for (int i = 0; i < count; i++)
        {
            actionManager.TriggerNextIndividualMarker();
        }
        Debug.Log($"Triggered {count} individual markers");
    }

    private void TriggerAllAreaMarkers()
    {
        int count = actionManager.GetCurrentAreaMarkers();
        for (int i = 0; i < count; i++)
        {
            actionManager.TriggerNextAreaMarker();
        }
        Debug.Log($"Triggered {count} area markers");
    }

    private void CreateLinePattern()
    {
        if (gridManager == null) return;

        actionManager.ClearAllActions();
        int centerY = gridManager.Height / 2;

        for (int i = 0; i < testMarkerCount && i < gridManager.Width; i++)
        {
            actionManager.PlaceIndividualMarker(new Vector2Int(i, centerY));
        }
        Debug.Log($"Created line pattern with {testMarkerCount} markers");
    }

    private void CreateGridPattern()
    {
        if (gridManager == null) return;

        actionManager.ClearAllActions();
        int placed = 0;

        for (int x = 1; x < gridManager.Width - 1 && placed < testMarkerCount; x += 2)
        {
            for (int y = 1; y < gridManager.Height - 1 && placed < testMarkerCount; y += 2)
            {
                actionManager.PlaceIndividualMarker(new Vector2Int(x, y));
                placed++;
            }
        }
        Debug.Log($"Created grid pattern with {placed} markers");
    }

    private void CreateRandomPattern()
    {
        if (gridManager == null) return;

        actionManager.ClearAllActions();

        for (int i = 0; i < testMarkerCount; i++)
        {
            Vector2Int randomPos = new Vector2Int(
                Random.Range(0, gridManager.Width),
                Random.Range(0, gridManager.Height)
            );
            actionManager.PlaceIndividualMarker(randomPos);
        }
        Debug.Log($"Created random pattern with {testMarkerCount} markers");
    }

    private void CreatePatternAroundPlayer()
    {
        if (playerManager == null || gridManager == null) return;

        Vector2Int center = playerManager.currentTilePosition;
        actionManager.ClearAllActions();

        for (int radius = 1; radius <= 3; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) == radius)
                    {
                        Vector2Int pos = center + new Vector2Int(x, y);
                        if (gridManager.IsValidGridPosition(pos))
                        {
                            actionManager.PlaceIndividualMarker(pos);
                        }
                    }
                }
            }
        }
        Debug.Log("Created pattern around player");
    }

    private void CreateTopRowPattern()
    {
        if (gridManager == null) return;

        actionManager.ClearAllActions();
        int topRow = gridManager.Height - 1;

        for (int x = 0; x < gridManager.Width && x < testMarkerCount; x++)
        {
            actionManager.PlaceIndividualMarker(new Vector2Int(x, topRow));
        }
        Debug.Log($"Created top row pattern");
    }

    private void CreateCornerPattern()
    {
        if (gridManager == null) return;

        actionManager.ClearAllActions();

        Vector2Int[] corners = {
            new Vector2Int(0, 0),
            new Vector2Int(gridManager.Width - 1, 0),
            new Vector2Int(0, gridManager.Height - 1),
            new Vector2Int(gridManager.Width - 1, gridManager.Height - 1)
        };

        foreach (var corner in corners)
        {
            actionManager.PlaceAreaMarker(corner, 2);
        }
        Debug.Log("Created corner pattern with area markers");
    }

    private System.Collections.IEnumerator RapidPlaceRemoveTest()
    {
        Vector2Int testPos = targetPosition;

        for (int i = 0; i < 10; i++)
        {
            actionManager.PlaceIndividualMarker(testPos);
            yield return new WaitForSeconds(0.1f);
            actionManager.RemoveIndividualMarkerAt(testPos);
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("Rapid place/remove test completed");
    }

    private System.Collections.IEnumerator TriggerStressTest()
    {
        CreateRandomPattern();
        yield return new WaitForSeconds(0.5f);

        while (actionManager.GetCurrentIndividualMarkers() > 0)
        {
            actionManager.TriggerNextIndividualMarker();
            yield return new WaitForSeconds(testTriggerDelay);
        }
        Debug.Log("Trigger stress test completed");
    }

    private void FillIndividualMarkers()
    {
        while (actionManager.CanPlaceIndividualMarkerCheck() && actionManager.GetCurrentIndividualMarkers() < actionManager.maxIndividualMarkers)
        {
            Vector2Int randomPos = new Vector2Int(
                Random.Range(0, gridManager.Width),
                Random.Range(0, gridManager.Height)
            );
            if (!actionManager.PlaceIndividualMarker(randomPos))
                break;
        }
        Debug.Log("Filled individual markers to maximum");
    }

    private void FillAreaMarkers()
    {
        while (actionManager.CanPlaceAreaMarkerCheck() && actionManager.GetCurrentAreaMarkers() < actionManager.maxAreaMarkers)
        {
            Vector2Int randomPos = new Vector2Int(
                Random.Range(0, gridManager.Width),
                Random.Range(0, gridManager.Height)
            );
            if (!actionManager.PlaceAreaMarker(randomPos, areaMarkerSize))
                break;
        }
        Debug.Log("Filled area markers to maximum");
    }

    private void TriggerAllMarkers()
    {
        TriggerAllIndividualMarkers();
        TriggerAllAreaMarkers();

        while (actionManager.GetCurrentCubeMarkers() > 0)
        {
            actionManager.TriggerNextCubeMarker();
        }
        Debug.Log("Triggered all markers");
    }

    private void RemoveAllIndividualMarkers()
    {
        var markers = actionManager.individualMarkers.ToArray();
        foreach (var marker in markers)
        {
            actionManager.RemoveIndividualMarkerAt(marker.position);
        }
        Debug.Log($"Removed {markers.Length} individual markers");
    }

    private void RemoveAllAreaMarkers()
    {
        var markers = actionManager.areaMarkers.ToArray();
        foreach (var marker in markers)
        {
            actionManager.RemoveAreaMarkerAt(marker.centerPosition);
        }
        Debug.Log($"Removed {markers.Length} area markers");
    }

    private void ResetCooldowns()
    {
        // This would need to be implemented in PlayerActionManager
        Debug.Log("Reset cooldowns (would need PlayerActionManager method)");
    }

    private void SetMaxCharges()
    {
        // This would need to be implemented in PlayerActionManager
        Debug.Log("Set max charges (would need PlayerActionManager method)");
    }

    private void TestInputHandling()
    {
        Debug.Log("=== INPUT HANDLING TEST ===");
        Debug.Log("Simulating input handling - check PlayerActionManager.HandleInput()");
        // Could simulate key presses if needed
    }

    private void ValidateActionSystem()
    {
        Debug.Log("=== ACTION SYSTEM VALIDATION ===");
        Debug.Log($"Individual Markers: {actionManager.GetCurrentIndividualMarkers()}/{actionManager.maxIndividualMarkers}");
        Debug.Log($"Area Markers: {actionManager.GetCurrentAreaMarkers()}/{actionManager.maxAreaMarkers}");
        Debug.Log($"Cube Markers: {actionManager.GetCurrentCubeMarkers()}");
        Debug.Log($"Can Place Individual: {actionManager.CanPlaceIndividualMarkerCheck()}");
        Debug.Log($"Can Place Area: {actionManager.CanPlaceAreaMarkerCheck()}");
        Debug.Log("Action system validation complete");
    }

    private void PrintActionSystemState()
    {
        Debug.Log("=== ACTION SYSTEM STATE ===");
        Debug.Log($"PlayerActionManager found: {actionManager != null}");
        if (actionManager != null)
        {
            Debug.Log($"Individual: {actionManager.GetCurrentIndividualMarkers()}/{actionManager.maxIndividualMarkers}");
            Debug.Log($"Area: {actionManager.GetCurrentAreaMarkers()}/{actionManager.maxAreaMarkers}");
            Debug.Log($"Cube: {actionManager.GetCurrentCubeMarkers()}");
            Debug.Log($"Statistics - Individual Placed: {actionManager.GetIndividualMarkersPlaced()}");
            Debug.Log($"Statistics - Area Placed: {actionManager.GetAreaMarkersPlaced()}");
            Debug.Log($"Statistics - Perfect Timing: {actionManager.GetPerfectTimingHits()}");
        }
    }

    private void LogAllMarkerPositions()
    {
        Debug.Log("=== ALL MARKER POSITIONS ===");

        var individual = actionManager.individualMarkers.ToArray();
        Debug.Log($"Individual Markers ({individual.Length}):");
        foreach (var marker in individual)
        {
            Debug.Log($"  ({marker.position.x}, {marker.position.y}) - Age: {Time.time - marker.placementTime:F1}s");
        }

        var area = actionManager.areaMarkers.ToArray();
        Debug.Log($"Area Markers ({area.Length}):");
        foreach (var marker in area)
        {
            Debug.Log($"  Center: ({marker.centerPosition.x}, {marker.centerPosition.y}) - Size: {marker.size}x{marker.size}");
        }
    }

    private void SimulatePerfectTiming()
    {
        Vector2Int testPos = targetPosition;

        // Place marker and trigger immediately for perfect timing
        if (actionManager.PlaceIndividualMarker(testPos))
        {
            actionManager.TriggerNextIndividualMarker();
            Debug.Log("Simulated perfect timing trigger");
        }
    }

    private void TestEdgeCases()
    {
        Debug.Log("=== TESTING EDGE CASES ===");

        // Test invalid positions
        Vector2Int[] invalidPositions = {
            new Vector2Int(-1, -1),
            new Vector2Int(gridManager.Width, gridManager.Height),
            new Vector2Int(1000, 1000)
        };

        foreach (var pos in invalidPositions)
        {
            bool result = actionManager.PlaceIndividualMarker(pos);
            Debug.Log($"Place marker at invalid position ({pos.x}, {pos.y}): {result}");
        }

        // Test when at maximum capacity
        Debug.Log("Testing maximum capacity behavior...");
        FillIndividualMarkers();

        bool overLimitResult = actionManager.PlaceIndividualMarker(targetPosition);
        Debug.Log($"Place marker when at limit: {overLimitResult}");

        Debug.Log("Edge case testing complete");
    }

    private System.Collections.IEnumerator StartCoroutine(System.Collections.IEnumerator routine)
    {
        // Simple coroutine starter for debug panel
        var runner = new GameObject("DebugCoroutineRunner");
        var mb = runner.AddComponent<CoroutineRunner>();
        mb.StartCoroutine(routine);
        return routine;
    }

    private class CoroutineRunner : MonoBehaviour
    {
        void Start()
        {
            Destroy(gameObject, 30f); // Auto cleanup after 30 seconds
        }
    }
}
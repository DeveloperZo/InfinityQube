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
    private int selectedMarkerType = 0; // 0=Light, 1=Heavy, 2=Prime, 3=Cube
    private Vector2Int targetPosition = new Vector2Int(0, 0);
    private bool autoTrackPlayer = true;
    private int PrimeMarkerSize = 2;
    private int cubeMarkerType = 0; // 0=Light, 1=Prime

    // Testing settings
    private int testMarkerCount = 5;
    private float testTriggerDelay = 1f;
    private bool showCooldownTimers = true;

    // Enhanced statistics tracking
    private Dictionary<string, int> patternUsageStats = new Dictionary<string, int>();
    private Dictionary<string, float> performanceMetrics = new Dictionary<string, float>();
    private List<float> operationTimes = new List<float>();
    private float totalTestTime = 0f;
    private int totalOperations = 0;

    // Enhanced batch operation settings
    private int batchSize = 10;
    private float batchDelay = 0.1f;
    private bool showBatchProgress = true;
    private string currentBatchOperation = "";
    private float batchProgressPercentage = 0f;

    // Pattern presets
    private readonly string[] patternPresets = {
        "Cross Pattern", "Diamond Pattern", "Spiral Pattern", "Checkerboard",
        "Border Pattern", "Wave Pattern", "Cluster Pattern", "Custom Pattern"
    };
    private int selectedPreset = 0;
    private bool showPatternPreview = true;

    // Enhanced visualization settings
    private bool showMarkerAges = true;
    private bool showMarkerEfficiencyStats = true;
    private bool highlightPerfectTimingMarkers = true;
    private bool showMarkerHeatmap = false;

    // Performance testing
    private bool isPerformanceTestRunning = false;
    private string currentPerformanceTest = "";
    private float performanceTestStartTime = 0f;
    private int performanceTestOperations = 0;

    public override void Initialize()
    {
        base.Initialize(); // Initialize theme and performance systems
        
        actionManager = Object.FindObjectOfType<PlayerActionManager>();
        playerManager = Object.FindObjectOfType<PlayerManager>();
        gridManager = GridManager.Instance;

        if (playerManager != null)
        {
            targetPosition = playerManager.currentTilePosition;
        }

        // Initialize enhanced statistics
        InitializeStatistics();
    }

    public override void Update()
    {
        if (autoTrackPlayer && playerManager != null)
        {
            targetPosition = playerManager.currentTilePosition;
        }

        // Update performance test timing
        if (isPerformanceTestRunning)
        {
            UpdatePerformanceTestMetrics();
        }
    }

    protected override void DrawPanelContent()
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
        showMarkerManagement = DebugUIHelpers.DrawToggleButton("Markers", showMarkerManagement);
        showActionInspection = DebugUIHelpers.DrawToggleButton("Inspection", showActionInspection);
        showMarkerTesting = DebugUIHelpers.DrawToggleButton("Testing", showMarkerTesting);
        showActionOperations = DebugUIHelpers.DrawToggleButton("Operations", showActionOperations);
        GUILayout.EndHorizontal();
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
        GUILayout.Label($"Light: {actionManager.GetCurrentLightMarkers()}/{actionManager.maxLightMarkers}", GUILayout.Width(80));
        GUILayout.Label($"Heavy: {actionManager.GetCurrentHeavyMarkers()}/{actionManager.maxHeavyMarkers}", GUILayout.Width(80));
        GUILayout.Label($"Prime: {actionManager.GetCurrentPrimeMarkers()}/{actionManager.maxPrimeMarkers}", GUILayout.Width(80));
        GUILayout.Label($"Cube: {actionManager.GetCurrentCubeMarkers()}", GUILayout.Width(60));
        GUILayout.EndHorizontal();

        // Cooldown information
        if (showCooldownTimers)
        {
            float lightCD = actionManager.GetLightMarkerCooldownRemaining();
            float heavyCD = actionManager.GetHeavyMarkerCooldownRemaining();
            float primeCD = actionManager.GetPrimeMarkerCooldownRemaining();

            if (lightCD > 0 || heavyCD > 0 || primeCD > 0)
            {
                GUILayout.BeginHorizontal();
                if (lightCD > 0)
                    GUILayout.Label($"Light CD: {lightCD:F1}s", GUILayout.Width(100));
                if (heavyCD > 0)
                    GUILayout.Label($"Heavy CD: {heavyCD:F1}s", GUILayout.Width(100));
                if (primeCD > 0)
                    GUILayout.Label($"Prime CD: {primeCD:F1}s", GUILayout.Width(100));
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
            bool hasLight = actionManager.HasLightMarkerAt(targetPosition);
            bool hasHeavy = actionManager.HasHeavyMarkerAt(targetPosition);
            bool hasPrime = actionManager.HasPrimeMarkerAt(targetPosition);

            string status = "Empty";
            List<string> markers = new List<string>();
            if (hasLight) markers.Add("Light");
            if (hasHeavy) markers.Add("Heavy");
            if (hasPrime) markers.Add("Prime");
            
            if (markers.Count > 0)
                status = string.Join(" + ", markers) + " Marker" + (markers.Count > 1 ? "s" : "");

            GUILayout.Label($"Target Status: {status}");
        }
    }

    private void DrawMarkerTypeSelector()
    {
        GUILayout.Label("Marker Type:");
        GUILayout.BeginHorizontal();

        GUI.backgroundColor = selectedMarkerType == 0 ? Color.cyan : Color.white;
        if (GUILayout.Button("Light")) selectedMarkerType = 0;

        GUI.backgroundColor = selectedMarkerType == 1 ? Color.yellow : Color.white;
        if (GUILayout.Button("Heavy")) selectedMarkerType = 1;

        GUI.backgroundColor = selectedMarkerType == 2 ? Color.green : Color.white;
        if (GUILayout.Button("Prime")) selectedMarkerType = 2;

        GUI.backgroundColor = selectedMarkerType == 3 ? Color.magenta : Color.white;
        if (GUILayout.Button("Cube")) selectedMarkerType = 3;

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Type-specific settings
        if (selectedMarkerType == 1) // Heavy
        {
            GUILayout.Label("Heavy markers work best with recursion cubes", GUI.skin.label);
            GUILayout.Label("Provides enhanced effectiveness vs recursion cube types");
        }
        else if (selectedMarkerType == 2) // Prime
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Size:", GUILayout.Width(40));
            if (GUILayout.Button("-", GUILayout.Width(20)) && PrimeMarkerSize > 1)
                PrimeMarkerSize--;
            GUILayout.Label($"{PrimeMarkerSize}x{PrimeMarkerSize}", GUILayout.Width(40));
            if (GUILayout.Button("+", GUILayout.Width(20)) && PrimeMarkerSize < 5)
                PrimeMarkerSize++;
            GUILayout.EndHorizontal();
        }
        else if (selectedMarkerType == 3) // Cube
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type:", GUILayout.Width(40));
            GUI.backgroundColor = cubeMarkerType == 0 ? Color.cyan : Color.white;
            if (GUILayout.Button("Light (1x1)", GUILayout.Width(80))) cubeMarkerType = 0;
            GUI.backgroundColor = cubeMarkerType == 1 ? Color.green : Color.white;
            if (GUILayout.Button("Prime (3x3)", GUILayout.Width(80))) cubeMarkerType = 1;
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Label("Light marker + Prime cube = Light cube marker (1x1)");
            GUILayout.Label("Prime marker + Prime cube = Prime cube marker (3x3)");
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

        bool canPlaceLight = actionManager.CanPlaceLightMarkerCheck();
        bool canPlaceHeavy = actionManager.CanPlaceHeavyMarkerCheck();
        bool canPlacePrime = actionManager.CanPlacePrimeMarkerCheck();

        GUI.color = canPlaceLight ? Color.green : Color.red;
        GUILayout.Label("Light", GUILayout.Width(50));

        GUI.color = canPlaceHeavy ? Color.green : Color.red;
        GUILayout.Label("Heavy", GUILayout.Width(50));

        GUI.color = canPlacePrime ? Color.green : Color.red;
        GUILayout.Label("Prime", GUILayout.Width(50));

        GUI.color = Color.white;
        GUILayout.EndHorizontal();
    }

    private void DrawQuickTriggerControls()
    {
        GUILayout.Label("Quick Triggers:");
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Trigger Light"))
        {
            actionManager.TriggerNextLightMarker();
        }

        if (GUILayout.Button("Trigger Heavy"))
        {
            actionManager.TriggerNextHeavyMarker();
        }

        if (GUILayout.Button("Trigger Prime"))
        {
            actionManager.TriggerNextPrimeMarker();
        }

        if (GUILayout.Button("Trigger Cube"))
        {
            actionManager.TriggerNextCubeMarker();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Trigger All Light"))
        {
            TriggerAllLightMarkers();
        }

        if (GUILayout.Button("Trigger All Heavy"))
        {
            TriggerAllHeavyMarkers();
        }

        if (GUILayout.Button("Trigger All Prime"))
        {
            TriggerAllPrimeMarkers();
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
        GUILayout.Label("Enhanced Statistics:");

        // Basic statistics
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Light Placed: {actionManager.GetLightMarkersPlaced()}", GUILayout.Width(100));
        GUILayout.Label($"Heavy Placed: {actionManager.GetHeavyMarkersPlaced()}", GUILayout.Width(100));
        GUILayout.Label($"Prime Placed: {actionManager.GetPrimeMarkersPlaced()}", GUILayout.Width(100));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Cube Triggered: {actionManager.GetCubeMarkersTriggered()}", GUILayout.Width(110));
        GUILayout.Label($"Perfect Timing: {actionManager.GetPerfectTimingHits()}", GUILayout.Width(120));
        GUILayout.Label($"Total Operations: {totalOperations}", GUILayout.Width(120));
        GUILayout.EndHorizontal();

        // Enhanced efficiency metrics
        if (showMarkerEfficiencyStats)
        {
            DrawEfficiencyMetrics();
        }

        // Pattern usage statistics
        DrawPatternUsageStats();

        // Performance metrics
        DrawPerformanceMetrics();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset Stats", GUILayout.Width(80)))
        {
            ResetEnhancedStatistics();
        }
        showMarkerEfficiencyStats = GUILayout.Toggle(showMarkerEfficiencyStats, "Show Efficiency");
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawActiveMarkersList()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("Active Markers:");

        markerListScroll = GUILayout.BeginScrollView(markerListScroll, GUILayout.MinHeight(200));

        // Light markers
        var lightMarkers = actionManager.lightMarkers.ToArray();
        if (lightMarkers.Length > 0)
        {
            GUILayout.Label("Light Markers:", GUI.skin.box);
            foreach (var marker in lightMarkers.Take(10))
            {
                DrawLightMarkerItem(marker);
            }
        }

        // Heavy markers
        var heavyMarkers = actionManager.heavyMarkers.ToArray();
        if (heavyMarkers.Length > 0)
        {
            GUILayout.Label("Heavy Markers:", GUI.skin.box);
            foreach (var marker in heavyMarkers.Take(5))
            {
                DrawHeavyMarkerItem(marker);
            }
        }

        // Prime markers
        var primeMarkers = actionManager.primeMarkers.ToArray();
        if (primeMarkers.Length > 0)
        {
            GUILayout.Label("Prime Markers:", GUI.skin.box);
            foreach (var marker in primeMarkers.Take(5))
            {
                DrawPrimeMarkerItem(marker);
            }
        }

        // Cube markers (if accessible)
        int cubeMarkerCount = actionManager.GetCurrentCubeMarkers();
        if (cubeMarkerCount > 0)
        {
            GUILayout.Label($"Cube Markers: {cubeMarkerCount} active", GUI.skin.box);
            //var nextCubeMarker = actionManager.GetNextCubeMarker();
            //if (nextCubeMarker.x >= 0)
            //{
            //    GUILayout.Label($"Next: ({nextCubeMarker.x}, {nextCubeMarker.y})");
            //}
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawLightMarkerItem(LightMarker marker)
    {
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label($"({marker.position.x},{marker.position.y})", GUILayout.Width(60));

        if (showMarkerAges)
        {
            float age = Time.time - marker.placementTime;
            Color ageColor = age > 5f ? Color.red : (age > 2f ? Color.yellow : Color.white);
            GUI.color = ageColor;
            GUILayout.Label($"Age: {age:F1}s", GUILayout.Width(70));
            GUI.color = Color.white;
        }

        if (marker.isPerfectTiming && highlightPerfectTimingMarkers)
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

    private void DrawHeavyMarkerItem(HeavyMarker marker)
    {
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUILayout.Label($"({marker.position.x},{marker.position.y})", GUILayout.Width(60));

        if (showMarkerAges)
        {
            float age = Time.time - marker.placementTime;
            Color ageColor = age > 5f ? Color.red : (age > 2f ? Color.yellow : Color.white);
            GUI.color = ageColor;
            GUILayout.Label($"Age: {age:F1}s", GUILayout.Width(70));
            GUI.color = Color.white;
        }

        // Heavy marker indicator
        GUI.color = Color.yellow;
        GUILayout.Label("HEAVY", GUILayout.Width(50));
        GUI.color = Color.white;

        if (marker.isPerfectTiming && highlightPerfectTimingMarkers)
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

    private void DrawPrimeMarkerItem(PrimeMarker marker)
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
        GUILayout.Label($"Light Max: {actionManager.maxLightMarkers}", GUILayout.Width(80));
        GUILayout.Label($"Heavy Max: {actionManager.maxHeavyMarkers}", GUILayout.Width(80));
        GUILayout.Label($"Prime Max: {actionManager.maxPrimeMarkers}", GUILayout.Width(80));
        GUILayout.Label($"Prime Size: {actionManager.primeMarkerSize}", GUILayout.Width(80));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Light CD: {actionManager.lightMarkerCooldown:F1}s", GUILayout.Width(100));
        GUILayout.Label($"Heavy CD: {actionManager.heavyMarkerCooldown:F1}s", GUILayout.Width(100));
        GUILayout.Label($"Prime CD: {actionManager.primeMarkerCooldown:F1}s", GUILayout.Width(100));
        GUILayout.EndHorizontal();

        showCooldownTimers = GUILayout.Toggle(showCooldownTimers, "Show Cooldown Timers");

        GUILayout.EndVertical();
    }

    private void DrawMarkerTestingSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("ENHANCED MARKER TESTING", GUI.skin.box);

        // Enhanced test settings
        DrawEnhancedTestSettings();

        GUILayout.Space(5);

        // Pattern presets
        DrawPatternPresets();

        GUILayout.Space(5);

        // Test patterns
        DrawTestPatterns();

        GUILayout.Space(5);

        // Enhanced performance tests
        DrawEnhancedPerformanceTests();

        // Batch operation progress
        if (showBatchProgress && !string.IsNullOrEmpty(currentBatchOperation))
        {
            DrawBatchProgress();
        }

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
        
        // Heavy marker testing
        GUILayout.Space(5);
        GUILayout.Label("Heavy Marker Tests:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Test Heavy + Recursion"))
        {
            TestHeavyMarkerRecursionCube();
        }
        if (GUILayout.Button("All Heavy Combos"))
        {
            TestAllHeavyMarkerCombos();
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
        GUILayout.Label("ENHANCED ACTION OPERATIONS", GUI.skin.box);

        // Enhanced batch operations
        DrawEnhancedBatchOperations();

        GUILayout.Space(5);

        // Enhanced cooldown and charge management
        DrawEnhancedCooldownManagement();

        GUILayout.Space(5);

        // System operations
        DrawSystemOperations();

        GUILayout.Space(5);

        // Enhanced debug operations
        DrawEnhancedDebugOperations();

        GUILayout.Space(5);

        // Marker visualization controls
        DrawVisualizationControls();

        GUILayout.EndVertical();
    }

    private void DrawBatchOperations()
    {
        GUILayout.Label("Batch Operations:");

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill Light"))
        {
            FillLightMarkers();
        }
        if (GUILayout.Button("Fill Prime"))
        {
            FillPrimeMarkers();
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
        if (GUILayout.Button("Remove All Light"))
        {
            RemoveAllLightMarkers();
        }
        if (GUILayout.Button("Remove All Prime"))
        {
            RemoveAllPrimeMarkers();
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

    // Enhanced Implementation Methods
    
    private void InitializeStatistics()
    {
        patternUsageStats.Clear();
        performanceMetrics.Clear();
        operationTimes.Clear();
        totalTestTime = 0f;
        totalOperations = 0;
        
        foreach (string pattern in patternPresets)
        {
            patternUsageStats[pattern] = 0;
        }
        
        performanceMetrics["Average Operation Time"] = 0f;
        performanceMetrics["Peak Performance"] = 0f;
        performanceMetrics["Efficiency Rating"] = 100f;
    }
    
    private void UpdatePerformanceTestMetrics()
    {
        if (isPerformanceTestRunning)
        {
            float testDuration = Time.time - performanceTestStartTime;
            if (testDuration > 0)
            {
                float operationsPerSecond = performanceTestOperations / testDuration;
                performanceMetrics["Current OPS"] = operationsPerSecond;
                
                if (operationsPerSecond > performanceMetrics["Peak Performance"])
                {
                    performanceMetrics["Peak Performance"] = operationsPerSecond;
                }
            }
        }
    }
    
    private void DrawEfficiencyMetrics()
    {
        GUILayout.Label("Efficiency Metrics:", GUI.skin.box);
        
        float averageTime = operationTimes.Count > 0 ? operationTimes.Average() : 0f;
        float efficiency = totalOperations > 0 ? (actionManager.GetPerfectTimingHits() * 100f) / totalOperations : 100f;
        
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Avg Time: {averageTime:F3}s", GUILayout.Width(100));
        GUILayout.Label($"Efficiency: {efficiency:F1}%", GUILayout.Width(100));
        GUILayout.Label($"Success Rate: {CalculateSuccessRate():F1}%", GUILayout.Width(120));
        GUILayout.EndHorizontal();
    }
    
    private void DrawPatternUsageStats()
    {
        GUILayout.Label("Pattern Usage:", GUI.skin.box);
        
        int totalPatternUse = patternUsageStats.Values.Sum();
        if (totalPatternUse > 0)
        {
            GUILayout.BeginHorizontal();
            foreach (var kvp in patternUsageStats.Take(3))
            {
                float percentage = (kvp.Value * 100f) / totalPatternUse;
                GUILayout.Label($"{kvp.Key}: {percentage:F0}%", GUILayout.Width(80));
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("No patterns used yet");
        }
    }
    
    private void DrawPerformanceMetrics()
    {
        GUILayout.Label("Performance:", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        if (performanceMetrics.ContainsKey("Current OPS"))
        {
            GUILayout.Label($"OPS: {performanceMetrics["Current OPS"]:F1}", GUILayout.Width(80));
        }
        if (performanceMetrics.ContainsKey("Peak Performance"))
        {
            GUILayout.Label($"Peak: {performanceMetrics["Peak Performance"]:F1}", GUILayout.Width(80));
        }
        GUILayout.Label($"Test Time: {totalTestTime:F1}s", GUILayout.Width(100));
        GUILayout.EndHorizontal();
    }
    
    private void DrawEnhancedTestSettings()
    {
        GUILayout.Label("Enhanced Test Settings:");
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Count:", GUILayout.Width(50));
        if (GUILayout.Button("-", GUILayout.Width(20)) && testMarkerCount > 1)
            testMarkerCount--;
        GUILayout.Label($"{testMarkerCount}", GUILayout.Width(20));
        if (GUILayout.Button("+", GUILayout.Width(20)) && testMarkerCount < 50)
            testMarkerCount++;
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Delay:", GUILayout.Width(50));
        if (GUILayout.Button("-", GUILayout.Width(20)) && testTriggerDelay > 0.05f)
            testTriggerDelay -= 0.05f;
        GUILayout.Label($"{testTriggerDelay:F2}s", GUILayout.Width(40));
        if (GUILayout.Button("+", GUILayout.Width(20)) && testTriggerDelay < 5f)
            testTriggerDelay += 0.05f;
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Batch Size:", GUILayout.Width(70));
        if (GUILayout.Button("-", GUILayout.Width(20)) && batchSize > 1)
            batchSize--;
        GUILayout.Label($"{batchSize}", GUILayout.Width(20));
        if (GUILayout.Button("+", GUILayout.Width(20)) && batchSize < 100)
            batchSize++;
        GUILayout.EndHorizontal();
        
        showBatchProgress = GUILayout.Toggle(showBatchProgress, "Show Batch Progress");
        showPatternPreview = GUILayout.Toggle(showPatternPreview, "Show Pattern Preview");
    }
    
    private void DrawPatternPresets()
    {
        GUILayout.Label("Pattern Presets:");
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<", GUILayout.Width(20)))
        {
            selectedPreset = (selectedPreset - 1 + patternPresets.Length) % patternPresets.Length;
        }
        
        GUILayout.Label(patternPresets[selectedPreset], GUILayout.Width(120));
        
        if (GUILayout.Button(">", GUILayout.Width(20)))
        {
            selectedPreset = (selectedPreset + 1) % patternPresets.Length;
        }
        
        if (GUILayout.Button("Apply", GUILayout.Width(60)))
        {
            ApplyPatternPreset(selectedPreset);
        }
        GUILayout.EndHorizontal();
        
        if (showPatternPreview)
        {
            GUILayout.Label($"Preview: {GetPatternDescription(selectedPreset)}");
        }
    }
    
    private void DrawEnhancedPerformanceTests()
    {
        GUILayout.Label("Enhanced Performance Tests:");
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Stress Test"))
        {
            StartEnhancedStressTest();
        }
        if (GUILayout.Button("Efficiency Test"))
        {
            StartEfficiencyTest();
        }
        if (GUILayout.Button("Endurance Test"))
        {
            StartEnduranceTest();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Memory Test"))
        {
            StartMemoryTest();
        }
        if (GUILayout.Button("Precision Test"))
        {
            StartPrecisionTest();
        }
        if (isPerformanceTestRunning && GUILayout.Button("Stop Test"))
        {
            StopPerformanceTest();
        }
        GUILayout.EndHorizontal();
        
        if (isPerformanceTestRunning)
        {
            GUILayout.Label($"Running: {currentPerformanceTest}");
            GUILayout.Label($"Operations: {performanceTestOperations}");
        }
    }
    
    private void DrawBatchProgress()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label($"Batch Operation: {currentBatchOperation}");
        
        // Progress bar
        Rect progressRect = GUILayoutUtility.GetRect(200, 20);
        GUI.DrawTexture(progressRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.gray, 0, 0);
        
        Rect fillRect = progressRect;
        fillRect.width *= batchProgressPercentage / 100f;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.green, 0, 0);
        
        GUILayout.Label($"Progress: {batchProgressPercentage:F1}%");
        GUILayout.EndVertical();
    }
    
    private void DrawEnhancedBatchOperations()
    {
        GUILayout.Label("Enhanced Batch Operations:");
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Smart Fill"))
        {
            StartCoroutine(SmartFillBatch());
        }
        if (GUILayout.Button("Pattern Fill"))
        {
            StartCoroutine(PatternFillBatch());
        }
        if (GUILayout.Button("Optimized Clear"))
        {
            StartCoroutine(OptimizedClearBatch());
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cascade Trigger"))
        {
            StartCoroutine(CascadeTriggerBatch());
        }
        if (GUILayout.Button("Wave Trigger"))
        {
            StartCoroutine(WaveTriggerBatch());
        }
        if (GUILayout.Button("Random Burst"))
        {
            StartCoroutine(RandomBurstBatch());
        }
        GUILayout.EndHorizontal();
    }
    
    private void DrawEnhancedCooldownManagement()
    {
        GUILayout.Label("Enhanced Cooldown & Charge Management:");
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset All CDs"))
        {
            ResetAllCooldowns();
        }
        if (GUILayout.Button("Half CDs"))
        {
            ReduceCooldowns(0.5f);
        }
        if (GUILayout.Button("Zero CDs"))
        {
            ReduceCooldowns(0f);
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Max Charges"))
        {
            SetMaxCharges();
        }
        if (GUILayout.Button("Test Charges"))
        {
            SetTestCharges();
        }
        if (GUILayout.Button("Unlimited Mode"))
        {
            ToggleUnlimitedMode();
        }
        GUILayout.EndHorizontal();
        
        // Cooldown visualization
        DrawCooldownVisualization();
    }
    
    private void DrawEnhancedDebugOperations()
    {
        GUILayout.Label("Enhanced Debug Operations:");
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Detailed State"))
        {
            PrintDetailedSystemState();
        }
        if (GUILayout.Button("Export Metrics"))
        {
            ExportPerformanceMetrics();
        }
        if (GUILayout.Button("Memory Usage"))
        {
            PrintMemoryUsage();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Validate Integrity"))
        {
            ValidateSystemIntegrity();
        }
        if (GUILayout.Button("Benchmark"))
        {
            RunBenchmarkSuite();
        }
        if (GUILayout.Button("Simulate Errors"))
        {
            SimulateErrorConditions();
        }
        GUILayout.EndHorizontal();
    }
    
    private void DrawVisualizationControls()
    {
        GUILayout.Label("Visualization Controls:");
        
        showMarkerAges = GUILayout.Toggle(showMarkerAges, "Show Marker Ages");
        highlightPerfectTimingMarkers = GUILayout.Toggle(highlightPerfectTimingMarkers, "Highlight Perfect Timing");
        showMarkerHeatmap = GUILayout.Toggle(showMarkerHeatmap, "Show Heatmap");
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Toggle All Visual"))
        {
            ToggleAllVisualization();
        }
        if (GUILayout.Button("Reset Visual"))
        {
            ResetVisualizationSettings();
        }
        GUILayout.EndHorizontal();
    }
    
    private void DrawCooldownVisualization()
    {
        if (!showCooldownTimers) return;
        
        float LightCD = actionManager.GetLightMarkerCooldownRemaining();
        float PrimeCD = actionManager.GetPrimeMarkerCooldownRemaining();
        
        if (LightCD > 0 || PrimeCD > 0)
        {
            GUILayout.Label("Cooldown Status:");
            
            if (LightCD > 0)
            {
                DrawCooldownBar("Light", LightCD, actionManager.lightMarkerCooldown);
            }
            
            if (PrimeCD > 0)
            {
                DrawCooldownBar("Prime", PrimeCD, actionManager.primeMarkerCooldown);
            }
        }
    }
    
    private void DrawCooldownBar(string label, float current, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}:", GUILayout.Width(80));
        
        Rect barRect = GUILayoutUtility.GetRect(100, 16);
        GUI.DrawTexture(barRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.gray, 0, 0);
        
        float fillAmount = 1f - (current / max);
        Rect fillRect = barRect;
        fillRect.width *= fillAmount;
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0, Color.cyan, 0, 0);
        
        GUILayout.Label($"{current:F1}s", GUILayout.Width(40));
        GUILayout.EndHorizontal();
    }
    
    private float CalculateSuccessRate()
    {
        if (totalOperations == 0) return 100f;
        
        int successfulOperations = actionManager.GetLightMarkersPlaced() + 
                                  actionManager.GetPrimeMarkersPlaced() + 
                                  actionManager.GetCubeMarkersTriggered();
        
        return (successfulOperations * 100f) / totalOperations;
    }
    
    private string GetPatternDescription(int presetIndex)
    {
        switch (presetIndex)
        {
            case 0: return "Cross-shaped marker placement";
            case 1: return "Diamond pattern with center focus";
            case 2: return "Spiral from center outward";
            case 3: return "Checkerboard alternating pattern";
            case 4: return "Border edge placement";
            case 5: return "Wave-like sinusoidal pattern";
            case 6: return "Clustered groupings";
            case 7: return "User-defined custom pattern";
            default: return "Unknown pattern";
        }
    }
    
    private void ResetEnhancedStatistics()
    {
        InitializeStatistics();
        //actionManager.ResetStatistics(); // If this method exists
        Debug.Log("Enhanced statistics reset");
    }
    
    // Enhanced Pattern and Performance Methods
    
    private void ApplyPatternPreset(int presetIndex)
    {
        string patternName = patternPresets[presetIndex];
        patternUsageStats[patternName]++;
        
        float startTime = Time.time;
        
        switch (presetIndex)
        {
            case 0: CreateCrossPattern(); break;
            case 1: CreateDiamondPattern(); break;
            case 2: CreateSpiralPattern(); break;
            case 3: CreateCheckerboardPattern(); break;
            case 4: CreateBorderPattern(); break;
            case 5: CreateWavePattern(); break;
            case 6: CreateClusterPattern(); break;
            case 7: CreateCustomPattern(); break;
        }
        
        float operationTime = Time.time - startTime;
        operationTimes.Add(operationTime);
        totalOperations++;
        
        Debug.Log($"Applied {patternName} in {operationTime:F3}s");
    }
    
    private void CreateCrossPattern()
    {
        if (gridManager == null) return;
        
        actionManager.ClearAllActions();
        Vector2Int center = new Vector2Int(gridManager.Width / 2, gridManager.Height / 2);
        
        // Horizontal line
        for (int x = center.x - 3; x <= center.x + 3; x++)
        {
            if (gridManager.IsValidGridPosition(new Vector2Int(x, center.y)))
                actionManager.PlaceLightMarker(new Vector2Int(x, center.y));
        }
        
        // Vertical line
        for (int y = center.y - 3; y <= center.y + 3; y++)
        {
            if (gridManager.IsValidGridPosition(new Vector2Int(center.x, y)))
                actionManager.PlaceLightMarker(new Vector2Int(center.x, y));
        }
    }
    
    private void CreateDiamondPattern()
    {
        if (gridManager == null) return;
        
        actionManager.ClearAllActions();
        Vector2Int center = new Vector2Int(gridManager.Width / 2, gridManager.Height / 2);
        
        for (int radius = 1; radius <= 3; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                int y1 = radius - Mathf.Abs(x);
                int y2 = -(radius - Mathf.Abs(x));
                
                Vector2Int pos1 = center + new Vector2Int(x, y1);
                Vector2Int pos2 = center + new Vector2Int(x, y2);
                
                if (gridManager.IsValidGridPosition(pos1))
                    actionManager.PlaceLightMarker(pos1);
                if (y2 != y1 && gridManager.IsValidGridPosition(pos2))
                    actionManager.PlaceLightMarker(pos2);
            }
        }
    }
    
    private void CreateSpiralPattern()
    {
        if (gridManager == null) return;
        
        actionManager.ClearAllActions();
        Vector2Int center = new Vector2Int(gridManager.Width / 2, gridManager.Height / 2);
        
        int[] dx = {0, 1, 0, -1};
        int[] dy = {1, 0, -1, 0};
        
        Vector2Int current = center;
        int direction = 0;
        int steps = 1;
        int placed = 0;
        
        actionManager.PlaceLightMarker(current);
        placed++;
        
        while (placed < testMarkerCount)
        {
            for (int step = 0; step < steps && placed < testMarkerCount; step++)
            {
                current += new Vector2Int(dx[direction], dy[direction]);
                if (gridManager.IsValidGridPosition(current))
                {
                    actionManager.PlaceLightMarker(current);
                    placed++;
                }
            }
            
            direction = (direction + 1) % 4;
            if (direction % 2 == 0) steps++;
        }
    }
    
    private void CreateCheckerboardPattern()
    {
        if (gridManager == null) return;
        
        actionManager.ClearAllActions();
        int placed = 0;
        
        for (int x = 0; x < gridManager.Width && placed < testMarkerCount; x++)
        {
            for (int y = 0; y < gridManager.Height && placed < testMarkerCount; y++)
            {
                if ((x + y) % 2 == 0)
                {
                    actionManager.PlaceLightMarker(new Vector2Int(x, y));
                    placed++;
                }
            }
        }
    }
    
    private void CreateBorderPattern()
    {
        if (gridManager == null) return;
        
        actionManager.ClearAllActions();
        
        // Top and bottom borders
        for (int x = 0; x < gridManager.Width; x++)
        {
            actionManager.PlaceLightMarker(new Vector2Int(x, 0));
            actionManager.PlaceLightMarker(new Vector2Int(x, gridManager.Height - 1));
        }
        
        // Left and right borders
        for (int y = 1; y < gridManager.Height - 1; y++)
        {
            actionManager.PlaceLightMarker(new Vector2Int(0, y));
            actionManager.PlaceLightMarker(new Vector2Int(gridManager.Width - 1, y));
        }
    }
    
    private void CreateWavePattern()
    {
        if (gridManager == null) return;
        
        actionManager.ClearAllActions();
        
        for (int x = 0; x < gridManager.Width; x++)
        {
            float wave = Mathf.Sin(x * 0.5f) * 3f + gridManager.Height / 2f;
            int y = Mathf.RoundToInt(wave);
            
            if (gridManager.IsValidGridPosition(new Vector2Int(x, y)))
            {
                actionManager.PlaceLightMarker(new Vector2Int(x, y));
            }
        }
    }
    
    private void CreateClusterPattern()
    {
        if (gridManager == null) return;
        
        actionManager.ClearAllActions();
        
        // Create 3-4 clusters randomly
        for (int cluster = 0; cluster < 4; cluster++)
        {
            Vector2Int clusterCenter = new Vector2Int(
                Random.Range(2, gridManager.Width - 2),
                Random.Range(2, gridManager.Height - 2)
            );
            
            // Place markers around cluster center
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector2Int pos = clusterCenter + new Vector2Int(x, y);
                    if (gridManager.IsValidGridPosition(pos))
                    {
                        actionManager.PlaceLightMarker(pos);
                    }
                }
            }
        }
    }
    
    private void CreateCustomPattern()
    {
        // For now, create a simple custom pattern - could be expanded to load from file
        CreateLinePattern();
        Debug.Log("Custom pattern applied (currently using line pattern)");
    }
    
    // Enhanced Performance Test Methods
    
    private void StartEnhancedStressTest()
    {
        StartPerformanceTest("Enhanced Stress Test");
        StartCoroutine(EnhancedStressTestCoroutine());
    }
    
    private void StartEfficiencyTest()
    {
        StartPerformanceTest("Efficiency Test");
        StartCoroutine(EfficiencyTestCoroutine());
    }
    
    private void StartEnduranceTest()
    {
        StartPerformanceTest("Endurance Test");
        StartCoroutine(EnduranceTestCoroutine());
    }
    
    private void StartMemoryTest()
    {
        StartPerformanceTest("Memory Test");
        StartCoroutine(MemoryTestCoroutine());
    }
    
    private void StartPrecisionTest()
    {
        StartPerformanceTest("Precision Test");
        StartCoroutine(PrecisionTestCoroutine());
    }
    
    private void StartPerformanceTest(string testName)
    {
        currentPerformanceTest = testName;
        isPerformanceTestRunning = true;
        performanceTestStartTime = Time.time;
        performanceTestOperations = 0;
        Debug.Log($"Started {testName}");
    }
    
    private void StopPerformanceTest()
    {
        isPerformanceTestRunning = false;
        float duration = Time.time - performanceTestStartTime;
        totalTestTime += duration;
        
        Debug.Log($"{currentPerformanceTest} completed in {duration:F2}s with {performanceTestOperations} operations");
        currentPerformanceTest = "";
    }
    
    private System.Collections.IEnumerator EnhancedStressTestCoroutine()
    {
        // Rapid place/remove operations
        for (int i = 0; i < 100; i++)
        {
            Vector2Int randomPos = new Vector2Int(Random.Range(0, gridManager.Width), Random.Range(0, gridManager.Height));
            actionManager.PlaceLightMarker(randomPos);
            performanceTestOperations++;
            
            yield return new WaitForSeconds(0.01f);
            
            actionManager.RemoveLightMarkerAt(randomPos);
            performanceTestOperations++;
            
            yield return new WaitForSeconds(0.01f);
        }
        
        StopPerformanceTest();
    }
    
    private System.Collections.IEnumerator EfficiencyTestCoroutine()
    {
        // Test different patterns for efficiency
        for (int preset = 0; preset < patternPresets.Length - 1; preset++)
        {
            ApplyPatternPreset(preset);
            performanceTestOperations++;
            yield return new WaitForSeconds(0.5f);
            
            TriggerAllMarkers();
            performanceTestOperations++;
            yield return new WaitForSeconds(0.5f);
        }
        
        StopPerformanceTest();
    }
    
    private System.Collections.IEnumerator EnduranceTestCoroutine()
    {
        // Long-running test with many operations
        for (int cycle = 0; cycle < 50; cycle++)
        {
            CreateRandomPattern();
            performanceTestOperations++;
            yield return new WaitForSeconds(0.1f);
            
            TriggerAllMarkers();
            performanceTestOperations++;
            yield return new WaitForSeconds(0.1f);
        }
        
        StopPerformanceTest();
    }
    
    private System.Collections.IEnumerator MemoryTestCoroutine()
    {
        // Test memory usage patterns
        for (int i = 0; i < 20; i++)
        {
            FillLightMarkers();
            FillPrimeMarkers();
            performanceTestOperations += 2;
            yield return new WaitForSeconds(0.2f);
            
            actionManager.ClearAllActions();
            performanceTestOperations++;
            yield return new WaitForSeconds(0.1f);
        }
        
        StopPerformanceTest();
    }
    
    private System.Collections.IEnumerator PrecisionTestCoroutine()
    {
        // Test precision timing
        for (int i = 0; i < 10; i++)
        {
            Vector2Int testPos = new Vector2Int(i, gridManager.Height / 2);
            actionManager.PlaceLightMarker(testPos);
            
            // Immediate trigger for perfect timing
            yield return null; // Wait one frame
            actionManager.TriggerNextLightMarker();
            performanceTestOperations++;
            
            yield return new WaitForSeconds(0.1f);
        }
        
        StopPerformanceTest();
    }
    
    // Enhanced Batch Operation Coroutines
    
    private System.Collections.IEnumerator SmartFillBatch()
    {
        currentBatchOperation = "Smart Fill";
        batchProgressPercentage = 0f;
        
        // Fill based on current game state and optimal positions
        int totalOperations = batchSize;
        
        for (int i = 0; i < totalOperations; i++)
        {
            // Smart positioning logic
            Vector2Int smartPos = CalculateOptimalPosition();
            
            if (actionManager.CanPlaceLightMarkerCheck())
            {
                actionManager.PlaceLightMarker(smartPos);
            }
            else if (actionManager.CanPlacePrimeMarkerCheck())
            {
                actionManager.PlacePrimeMarker(smartPos, 2);
            }
            
            batchProgressPercentage = ((float)(i + 1) / totalOperations) * 100f;
            yield return new WaitForSeconds(batchDelay);
        }
        
        currentBatchOperation = "";
        Debug.Log("Smart Fill batch completed");
    }
    
    private System.Collections.IEnumerator PatternFillBatch()
    {
        currentBatchOperation = "Pattern Fill";
        batchProgressPercentage = 0f;
        
        ApplyPatternPreset(selectedPreset);
        batchProgressPercentage = 100f;
        yield return new WaitForSeconds(0.5f);
        
        currentBatchOperation = "";
        Debug.Log("Pattern Fill batch completed");
    }
    
    private System.Collections.IEnumerator OptimizedClearBatch()
    {
        currentBatchOperation = "Optimized Clear";
        batchProgressPercentage = 0f;
        
        int totalMarkers = actionManager.GetCurrentLightMarkers() + actionManager.GetCurrentPrimeMarkers();
        int cleared = 0;
        
        // Clear in order of placement time (oldest first)
        var LightMarkers = actionManager.lightMarkers.OrderBy(m => m.placementTime).ToArray();
        foreach (var marker in LightMarkers)
        {
            actionManager.RemoveLightMarkerAt(marker.position);
            cleared++;
            batchProgressPercentage = ((float)cleared / totalMarkers) * 100f;
            yield return new WaitForSeconds(batchDelay * 0.5f);
        }
        
        var PrimeMarkers = actionManager.primeMarkers.ToArray();
        foreach (var marker in PrimeMarkers)
        {
            actionManager.RemovePrimeMarkerAt(marker.centerPosition);
            cleared++;
            batchProgressPercentage = ((float)cleared / totalMarkers) * 100f;
            yield return new WaitForSeconds(batchDelay * 0.5f);
        }
        
        currentBatchOperation = "";
        Debug.Log("Optimized Clear batch completed");
    }
    
    private System.Collections.IEnumerator CascadeTriggerBatch()
    {
        currentBatchOperation = "Cascade Trigger";
        batchProgressPercentage = 0f;
        
        int totalMarkers = actionManager.GetCurrentLightMarkers();
        
        for (int i = 0; i < totalMarkers; i++)
        {
            actionManager.TriggerNextLightMarker();
            batchProgressPercentage = ((float)(i + 1) / totalMarkers) * 100f;
            yield return new WaitForSeconds(batchDelay);
        }
        
        currentBatchOperation = "";
        Debug.Log("Cascade Trigger batch completed");
    }
    
    private System.Collections.IEnumerator WaveTriggerBatch()
    {
        currentBatchOperation = "Wave Trigger";
        batchProgressPercentage = 0f;
        
        // Trigger in wave pattern from left to right
        for (int x = 0; x < gridManager.Width; x++)
        {
            var markersAtX = actionManager.lightMarkers.Where(m => m.position.x == x).ToArray();
            
            foreach (var marker in markersAtX)
            {
                actionManager.RemoveLightMarkerAt(marker.position);
            }
            
            batchProgressPercentage = ((float)(x + 1) / gridManager.Width) * 100f;
            yield return new WaitForSeconds(batchDelay);
        }
        
        currentBatchOperation = "";
        Debug.Log("Wave Trigger batch completed");
    }
    
    private System.Collections.IEnumerator RandomBurstBatch()
    {
        currentBatchOperation = "Random Burst";
        batchProgressPercentage = 0f;
        
        var allMarkers = actionManager.lightMarkers.ToList();
        int totalMarkers = allMarkers.Count;
        
        for (int i = 0; i < totalMarkers; i++)
        {
            if (allMarkers.Count > 0)
            {
                int randomIndex = Random.Range(0, allMarkers.Count);
                var randomMarker = allMarkers[randomIndex];
                actionManager.RemoveLightMarkerAt(randomMarker.position);
                allMarkers.RemoveAt(randomIndex);
            }
            
            batchProgressPercentage = ((float)(i + 1) / totalMarkers) * 100f;
            yield return new WaitForSeconds(batchDelay * 0.2f);
        }
        
        currentBatchOperation = "";
        Debug.Log("Random Burst batch completed");
    }
    
    private Vector2Int CalculateOptimalPosition()
    {
        // Simple optimal position calculation - could be enhanced
        Vector2Int playerPos = playerManager != null ? playerManager.currentTilePosition : new Vector2Int(gridManager.Width / 2, gridManager.Height / 2);
        
        // Find position near player but not occupied
        for (int radius = 1; radius <= 5; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) + Mathf.Abs(y) == radius)
                    {
                        Vector2Int pos = playerPos + new Vector2Int(x, y);
                        if (gridManager.IsValidGridPosition(pos) && 
                            !actionManager.HasLightMarkerAt(pos) && 
                            !actionManager.HasPrimeMarkerAt(pos))
                        {
                            return pos;
                        }
                    }
                }
            }
        }
        
        // Fallback to random position
        return new Vector2Int(Random.Range(0, gridManager.Width), Random.Range(0, gridManager.Height));
    }
    
    // Enhanced System Management Methods
    
    private void ResetAllCooldowns()
    {
        // Would need PlayerActionManager method to reset cooldowns
        Debug.Log("Reset all cooldowns (would need PlayerActionManager implementation)");
    }
    
    private void ReduceCooldowns(float factor)
    {
        // Would need PlayerActionManager method to modify cooldowns
        Debug.Log($"Reduce cooldowns by factor {factor} (would need PlayerActionManager implementation)");
    }
    
    private void SetTestCharges()
    {
        // Set charges to specific test values
        Debug.Log("Set test charges (would need PlayerActionManager implementation)");
    }
    
    private void ToggleUnlimitedMode()
    {
        // Toggle unlimited mode for testing
        Debug.Log("Toggle unlimited mode (would need PlayerActionManager implementation)");
    }
    
    private void ToggleAllVisualization()
    {
        bool newState = !showMarkerAges;
        showMarkerAges = newState;
        showMarkerEfficiencyStats = newState;
        highlightPerfectTimingMarkers = newState;
        showMarkerHeatmap = newState;
        
        Debug.Log($"All visualization toggled to {newState}");
    }
    
    private void ResetVisualizationSettings()
    {
        showMarkerAges = true;
        showMarkerEfficiencyStats = true;
        highlightPerfectTimingMarkers = true;
        showMarkerHeatmap = false;
        
        Debug.Log("Visualization settings reset to defaults");
    }
    
    // Enhanced Debug Methods
    
    private void PrintDetailedSystemState()
    {
        Debug.Log("=== DETAILED SYSTEM STATE ===");
        Debug.Log($"PlayerActionManager: {(actionManager != null ? "Found" : "Not Found")}");
        Debug.Log($"GridManager: {(gridManager != null ? "Found" : "Not Found")}");
        Debug.Log($"PlayerManager: {(playerManager != null ? "Found" : "Not Found")}");
        
        if (actionManager != null)
        {
            Debug.Log($"Light Markers: {actionManager.GetCurrentLightMarkers()}/{actionManager.maxLightMarkers}");
            Debug.Log($"Prime Markers: {actionManager.GetCurrentPrimeMarkers()}/{actionManager.maxPrimeMarkers}");
            Debug.Log($"Cube Markers: {actionManager.GetCurrentCubeMarkers()}");
            Debug.Log($"Light CD Remaining: {actionManager.GetLightMarkerCooldownRemaining():F2}s");
            Debug.Log($"Prime CD Remaining: {actionManager.GetPrimeMarkerCooldownRemaining():F2}s");
        }
        
        Debug.Log($"Enhanced Stats - Total Operations: {totalOperations}");
        Debug.Log($"Enhanced Stats - Total Test Time: {totalTestTime:F2}s");
        Debug.Log($"Enhanced Stats - Average Operation Time: {(operationTimes.Count > 0 ? operationTimes.Average() : 0):F4}s");
    }
    
    private void ExportPerformanceMetrics()
    {
        Debug.Log("=== PERFORMANCE METRICS EXPORT ===");
        
        foreach (var metric in performanceMetrics)
        {
            Debug.Log($"{metric.Key}: {metric.Value:F3}");
        }
        
        Debug.Log("Pattern Usage Statistics:");
        foreach (var pattern in patternUsageStats)
        {
            Debug.Log($"{pattern.Key}: {pattern.Value} uses");
        }
        
        Debug.Log($"Operation Times (last 10): {string.Join(", ", operationTimes.TakeLast(10).Select(t => t.ToString("F3")))}");
    }
    
    private void PrintMemoryUsage()
    {
        Debug.Log("=== MEMORY USAGE ===");
        Debug.Log($"Operation Times List: {operationTimes.Count} entries");
        Debug.Log($"Pattern Usage Stats: {patternUsageStats.Count} entries");
        Debug.Log($"Performance Metrics: {performanceMetrics.Count} entries");
        Debug.Log($"GC Total Memory: {System.GC.GetTotalMemory(false) / (1024 * 1024):F2} MB");
    }
    
    private void ValidateSystemIntegrity()
    {
        Debug.Log("=== SYSTEM INTEGRITY VALIDATION ===");
        
        bool isValid = true;
        
        if (actionManager == null)
        {
            Debug.LogError("PlayerActionManager is null!");
            isValid = false;
        }
        
        if (gridManager == null)
        {
            Debug.LogError("GridManager is null!");
            isValid = false;
        }
        
        if (actionManager != null)
        {
            int LightCount = actionManager.GetCurrentLightMarkers();
            int PrimeCount = actionManager.GetCurrentPrimeMarkers();
            
            if (LightCount > actionManager.maxLightMarkers)
            {
                Debug.LogError($"Light marker count ({LightCount}) exceeds maximum ({actionManager.maxLightMarkers})!");
                isValid = false;
            }
            
            if (PrimeCount > actionManager.maxPrimeMarkers)
            {
                Debug.LogError($"Prime marker count ({PrimeCount}) exceeds maximum ({actionManager.maxPrimeMarkers})!");
                isValid = false;
            }
        }
        
        Debug.Log($"System integrity: {(isValid ? "VALID" : "INVALID")}");
    }
    
    private void RunBenchmarkSuite()
    {
        Debug.Log("=== BENCHMARK SUITE ===");
        StartCoroutine(BenchmarkSuiteCoroutine());
    }
    
    private System.Collections.IEnumerator BenchmarkSuiteCoroutine()
    {
        Debug.Log("Starting benchmark suite...");
        
        // Benchmark 1: Pattern creation speed
        float startTime = Time.time;
        for (int i = 0; i < patternPresets.Length - 1; i++)
        {
            ApplyPatternPreset(i);
            yield return new WaitForSeconds(0.1f);
            actionManager.ClearAllActions();
        }
        float patternTime = Time.time - startTime;
        Debug.Log($"Pattern Creation Benchmark: {patternTime:F3}s");
        
        // Benchmark 2: Rapid operations
        startTime = Time.time;
        for (int i = 0; i < 50; i++)
        {
            Vector2Int pos = new Vector2Int(Random.Range(0, gridManager.Width), Random.Range(0, gridManager.Height));
            actionManager.PlaceLightMarker(pos);
            yield return null;
        }
        float rapidTime = Time.time - startTime;
        Debug.Log($"Rapid Operations Benchmark: {rapidTime:F3}s for 50 operations");
        
        actionManager.ClearAllActions();
        Debug.Log("Benchmark suite completed");
    }
    
    private void SimulateErrorConditions()
    {
        Debug.Log("=== SIMULATING ERROR CONDITIONS ===");
        
        // Test invalid positions
        Vector2Int[] invalidPositions = {
            new Vector2Int(-1, -1),
            new Vector2Int(gridManager.Width + 10, gridManager.Height + 10),
            new Vector2Int(9999, 9999)
        };
        
        foreach (var pos in invalidPositions)
        {
            bool result = actionManager.PlaceLightMarker(pos);
            Debug.Log($"Place marker at invalid position {pos}: {result} (should be false)");
        }
        
        // Test capacity limits
        Debug.Log("Testing capacity limits...");
        FillLightMarkers();
        bool overLimitResult = actionManager.PlaceLightMarker(targetPosition);
        Debug.Log($"Place marker over Light limit: {overLimitResult} (should be false)");
        
        FillPrimeMarkers();
        overLimitResult = actionManager.PlacePrimeMarker(targetPosition, 2);
        Debug.Log($"Place Prime marker over limit: {overLimitResult} (should be false)");
        
        Debug.Log("Error condition simulation completed");
    }

    // Implementation methods
    private void PlaceMarkerAtTarget()
    {
        switch (selectedMarkerType)
        {
            case 0: // Light
                actionManager.PlaceLightMarker(targetPosition);
                break;
            case 1: // Heavy
                actionManager.PlaceHeavyMarker(targetPosition);
                break;
            case 2: // Prime
                actionManager.PlacePrimeMarker(targetPosition, PrimeMarkerSize);
                break;
            case 3: // Cube
                var cubeMarkerType = (PlayerMarkerSystem.CubeMarkerType)this.cubeMarkerType;
                actionManager.CreateCubeMarker(targetPosition, cubeMarkerType);
                break;
        }
    }

    private void RemoveMarkerAtTarget()
    {
        if (actionManager.HasLightMarkerAt(targetPosition))
        {
            actionManager.RemoveLightMarkerAt(targetPosition);
        }
        if (actionManager.HasHeavyMarkerAt(targetPosition))
        {
            actionManager.RemoveHeavyMarkerAt(targetPosition);
        }
        if (actionManager.HasPrimeMarkerAt(targetPosition))
        {
            actionManager.RemovePrimeMarkerAt(targetPosition);
        }
    }

    private void ToggleMarkerAtTarget()
    {
        bool hasMarker = actionManager.HasLightMarkerAt(targetPosition) ||
                        actionManager.HasHeavyMarkerAt(targetPosition) ||
                        actionManager.HasPrimeMarkerAt(targetPosition);

        if (hasMarker)
        {
            RemoveMarkerAtTarget();
        }
        else
        {
            PlaceMarkerAtTarget();
        }
    }

    private void TriggerAllLightMarkers()
    {
        int count = actionManager.GetCurrentLightMarkers();
        for (int i = 0; i < count; i++)
        {
            actionManager.TriggerNextLightMarker();
        }
        Debug.Log($"Triggered {count} light markers");
    }

    private void TriggerAllHeavyMarkers()
    {
        int count = actionManager.GetCurrentHeavyMarkers();
        for (int i = 0; i < count; i++)
        {
            actionManager.TriggerNextHeavyMarker();
        }
        Debug.Log($"Triggered {count} heavy markers");
    }

    private void TriggerAllPrimeMarkers()
    {
        int count = actionManager.GetCurrentPrimeMarkers();
        for (int i = 0; i < count; i++)
        {
            actionManager.TriggerNextPrimeMarker();
        }
        Debug.Log($"Triggered {count} prime markers");
    }

    private void CreateLinePattern()
    {
        if (gridManager == null) return;

        actionManager.ClearAllActions();
        int centerY = gridManager.Height / 2;

        for (int i = 0; i < testMarkerCount && i < gridManager.Width; i++)
        {
            actionManager.PlaceLightMarker(new Vector2Int(i, centerY));
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
                actionManager.PlaceLightMarker(new Vector2Int(x, y));
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
            actionManager.PlaceLightMarker(randomPos);
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
                            actionManager.PlaceLightMarker(pos);
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
            actionManager.PlaceLightMarker(new Vector2Int(x, topRow));
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
            actionManager.PlacePrimeMarker(corner, 2);
        }
        Debug.Log("Created corner pattern with Prime markers");
    }

    private System.Collections.IEnumerator RapidPlaceRemoveTest()
    {
        Vector2Int testPos = targetPosition;

        for (int i = 0; i < 10; i++)
        {
            actionManager.PlaceLightMarker(testPos);
            yield return new WaitForSeconds(0.1f);
            actionManager.RemoveLightMarkerAt(testPos);
            yield return new WaitForSeconds(0.1f);
        }
        Debug.Log("Rapid place/remove test completed");
    }

    private System.Collections.IEnumerator TriggerStressTest()
    {
        CreateRandomPattern();
        yield return new WaitForSeconds(0.5f);

        while (actionManager.GetCurrentLightMarkers() > 0)
        {
            actionManager.TriggerNextLightMarker();
            yield return new WaitForSeconds(testTriggerDelay);
        }
        Debug.Log("Trigger stress test completed");
    }

    private void FillLightMarkers()
    {
        while (actionManager.CanPlaceLightMarkerCheck() && actionManager.GetCurrentLightMarkers() < actionManager.maxLightMarkers)
        {
            Vector2Int randomPos = new Vector2Int(
                Random.Range(0, gridManager.Width),
                Random.Range(0, gridManager.Height)
            );
            if (!actionManager.PlaceLightMarker(randomPos))
                break;
        }
        Debug.Log("Filled Light markers to maximum");
    }

    private void FillPrimeMarkers()
    {
        while (actionManager.CanPlacePrimeMarkerCheck() && actionManager.GetCurrentPrimeMarkers() < actionManager.maxPrimeMarkers)
        {
            Vector2Int randomPos = new Vector2Int(
                Random.Range(0, gridManager.Width),
                Random.Range(0, gridManager.Height)
            );
            if (!actionManager.PlacePrimeMarker(randomPos, PrimeMarkerSize))
                break;
        }
        Debug.Log("Filled Prime markers to maximum");
    }

    private void TriggerAllMarkers()
    {
        TriggerAllHeavyMarkers();
        TriggerAllLightMarkers();
        TriggerAllPrimeMarkers();

        Debug.Log("Triggered all markers");
    }

    private void RemoveAllLightMarkers()
    {
        var markers = actionManager.lightMarkers.ToArray();
        foreach (var marker in markers)
        {
            actionManager.RemoveLightMarkerAt(marker.position);
        }
        Debug.Log($"Removed {markers.Length} Light markers");
    }

    private void RemoveAllPrimeMarkers()
    {
        var markers = actionManager.primeMarkers.ToArray();
        foreach (var marker in markers)
        {
            actionManager.RemovePrimeMarkerAt(marker.centerPosition);
        }
        Debug.Log($"Removed {markers.Length} Prime markers");
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
        Debug.Log($"Light Markers: {actionManager.GetCurrentLightMarkers()}/{actionManager.maxLightMarkers}");
        Debug.Log($"Prime Markers: {actionManager.GetCurrentPrimeMarkers()}/{actionManager.maxPrimeMarkers}");
        Debug.Log($"Cube Markers: {actionManager.GetCurrentCubeMarkers()}");
        Debug.Log($"Can Place Light: {actionManager.CanPlaceLightMarkerCheck()}");
        Debug.Log($"Can Place Prime: {actionManager.CanPlacePrimeMarkerCheck()}");
        Debug.Log("Action system validation complete");
    }

    private void PrintActionSystemState()
    {
        Debug.Log("=== ACTION SYSTEM STATE ===");
        Debug.Log($"PlayerActionManager found: {actionManager != null}");
        if (actionManager != null)
        {
            Debug.Log($"Light: {actionManager.GetCurrentLightMarkers()}/{actionManager.maxLightMarkers}");
            Debug.Log($"Prime: {actionManager.GetCurrentPrimeMarkers()}/{actionManager.maxPrimeMarkers}");
            Debug.Log($"Cube: {actionManager.GetCurrentCubeMarkers()}");
            Debug.Log($"Statistics - Light Placed: {actionManager.GetLightMarkersPlaced()}");
            Debug.Log($"Statistics - Prime Placed: {actionManager.GetPrimeMarkersPlaced()}");
            Debug.Log($"Statistics - Perfect Timing: {actionManager.GetPerfectTimingHits()}");
        }
    }

    private void LogAllMarkerPositions()
    {
        Debug.Log("=== ALL MARKER POSITIONS ===");

        var Light = actionManager.lightMarkers.ToArray();
        Debug.Log($"Light Markers ({Light.Length}):");
        foreach (var marker in Light)
        {
            Debug.Log($"  ({marker.position.x}, {marker.position.y}) - Age: {Time.time - marker.placementTime:F1}s");
        }

        var Prime = actionManager.primeMarkers.ToArray();
        Debug.Log($"Prime Markers ({Prime.Length}):");
        foreach (var marker in Prime)
        {
            Debug.Log($"  Center: ({marker.centerPosition.x}, {marker.centerPosition.y}) - Size: {marker.size}x{marker.size}");
        }
    }

    private void SimulatePerfectTiming()
    {
        Vector2Int testPos = targetPosition;

        // Place marker and trigger immediately for perfect timing
        if (actionManager.PlaceLightMarker(testPos))
        {
            actionManager.TriggerNextLightMarker();
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
            bool result = actionManager.PlaceLightMarker(pos);
            Debug.Log($"Place marker at invalid position ({pos.x}, {pos.y}): {result}");
        }

        // Test when at maximum capacity
        Debug.Log("Testing maximum capacity behavior...");
        FillLightMarkers();

        bool overLimitResult = actionManager.PlaceLightMarker(targetPosition);
        Debug.Log($"Place marker when at limit: {overLimitResult}");

        Debug.Log("Edge case testing complete");
    }

    // Heavy marker testing methods
    private void TestHeavyMarkerRecursionCube()
    {
        Debug.Log("=== TESTING HEAVY MARKER + DENSE CUBE ====");
        
        Vector2Int testPos = targetPosition;
        
        // Place a heavy marker at the target position
        if (actionManager.PlaceHeavyMarker(testPos))
        {
            Debug.Log($"Placed heavy marker at ({testPos.x}, {testPos.y})");
            
            // TODO: Spawn a recursion cube at or near the position
            // This would require access to cube spawning functionality
            Debug.Log("Recursion cube interaction testing - requires cube spawning system integration");
            
            // Trigger the heavy marker for testing
            actionManager.TriggerNextHeavyMarker();
            Debug.Log("Triggered heavy marker for recursion cube interaction test");
        }
        else
        {
            Debug.LogWarning("Could not place heavy marker - check charges and cooldowns");
        }
    }
    
    private void TestAllHeavyMarkerCombos()
    {
        Debug.Log("=== TESTING ALL HEAVY MARKER COMBINATIONS ====");
        
        // Test heavy marker effectiveness against all cube types
        string[] cubeTypes = { "Unit", "Prime", "Infinity", "Recursion" };
        
        for (int i = 0; i < cubeTypes.Length; i++)
        {
            Vector2Int testPos = new Vector2Int(targetPosition.x + i, targetPosition.y);
            
            if (actionManager.CanPlaceHeavyMarkerCheck())
            {
                actionManager.PlaceHeavyMarker(testPos);
                Debug.Log($"Placed heavy marker at ({testPos.x}, {testPos.y}) for {cubeTypes[i]} cube test");
                
                // TODO: Spawn appropriate cube type at position
                Debug.Log($"TODO: Spawn {cubeTypes[i]} cube at ({testPos.x}, {testPos.y})");
            }
            else
            {
                Debug.LogWarning($"Cannot place heavy marker for {cubeTypes[i]} test - insufficient charges");
                break;
            }
        }
        
        Debug.Log("Heavy marker combination testing setup complete");
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
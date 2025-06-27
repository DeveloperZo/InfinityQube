using static Enumerations;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Dedicated debug panel for testing individual cube and tile interactions.
/// Focuses on one-on-one testing scenarios, cube lifecycle testing, detailed face painting operations,
/// and individual cube spawning/manipulation. Absorbed functionality from CubeDebugPanel for individual entity testing.
/// </summary>
public class CubeTileIndividualPanel : DebugPanelBase
{
    public override string PanelName => "Cube-Tile Individual";
    public override DebugPanelGroup Group => DebugPanelGroup.Cube;

    #region Manager References
    private GridManager gridManager;
    private PlayerManager playerManager;
    private WaveManager waveManager;
    private AudioManager audioManager;
    #endregion

    #region Audio Testing Implementation
         
   
    private int DrawAudioCubeTypeSelector(int currentType)
    {
        GUILayout.BeginHorizontal();
        
        for (int i = 0; i < System.Enum.GetValues(typeof(CubeType)).Length; i++)
        {
            var cubeType = (CubeType)i;
            Color bgColor = currentType == i ? DebugUIHelpers.GetCubeDisplayColor(cubeType) : Color.white;
            
            DebugUIHelpers.WithBackgroundColor(bgColor, () =>
            {
                if (GUILayout.Button(cubeType.ToString(), GUILayout.Width(60)))
                {
                    currentType = i;
                    if (enableAudioPreview && audioManager != null)
                    {
                        // Preview audio for the newly selected cube type
                        LogAudioTest($"Selected {cubeType} for audio testing");
                        TestCubeTypeAudio(cubeType, selectedSoundCategory);
                    }
                }
            });
        }
        
        GUILayout.EndHorizontal();
        return currentType;
    }
    
    private SoundCategory DrawSoundCategorySelector(SoundCategory currentCategory)
    {
        GUILayout.BeginHorizontal();
        
        var categories = System.Enum.GetValues(typeof(SoundCategory));
        foreach (SoundCategory category in categories)
        {
            Color bgColor = currentCategory == category ? Color.cyan : Color.white;
            
            DebugUIHelpers.WithBackgroundColor(bgColor, () =>
            {
                if (GUILayout.Button(category.ToString(), GUILayout.Width(80)))
                {
                    currentCategory = category;
                    if (enableAudioPreview && audioManager != null)
                    {
                        // Preview audio for the newly selected category
                        LogAudioTest($"Selected {category} sound category");
                        TestCubeTypeAudio(selectedAudioCubeType, category);
                    }
                }
            });
        }
        
        GUILayout.EndHorizontal();
        return currentCategory;
    }
    
    private void TestQuickAudioPreview()
    {
        if (audioManager == null || !audioManager.IsInitialized) return;
        
        Vector3 testPos = GetAudioTestPosition();
        TestCubeTypeAudio(selectedAudioCubeType, SoundCategory.Landing, true);
    }
    
    private void TestSelectedCubeAudio()
    {
        if (audioManager == null)
        {
            LogAudioTest("AudioManager not available");
            return;
        }
        
        Vector3 testPos = GetAudioTestPosition();
        LogAudioTest($"Testing {selectedAudioCubeType} {selectedSoundCategory} audio at {testPos}");
        TestCubeTypeAudio(selectedAudioCubeType, selectedSoundCategory);
    }
    
    private void TestAllCubeTypesAudio()
    {
        if (audioManager == null)
        {
            LogAudioTest("AudioManager not available for full test");
            return;
        }
        
        LogAudioTest($"Testing all cube types with {selectedSoundCategory} sounds");
        
        var cubeTypes = System.Enum.GetValues(typeof(CubeType));
        int delay = 0;
        
        foreach (CubeType cubeType in cubeTypes)
        {
            // Use delayed testing to avoid overwhelming the audio system
            UnityEngine.Object.FindObjectOfType<MonoBehaviour>().StartCoroutine(DelayedAudioTest(cubeType, selectedSoundCategory, delay));
            delay += 500; // 500ms delay between tests
        }
    }
    
    private void TestCurrentTestCubeAudio()
    {
        if (testCube == null)
        {
            LogAudioTest("No test cube available for audio testing");
            return;
        }
        
        if (audioManager == null)
        {
            LogAudioTest("AudioManager not available");
            return;
        }
        
        Vector3 cubePosition = new Vector3(testCube.position.x, 0, testCube.position.y);
        LogAudioTest($"Testing {testCube.type} cube audio at its position {cubePosition}");
        
        switch (selectedSoundCategory)
        {
            case SoundCategory.Landing:
                audioManager.PlayCubeLandingSound(testCube.type, cubePosition);
                break;
            case SoundCategory.Capture:
                audioManager.PlayCubeCaptureSound(testCube.type, cubePosition);
                break;
            case SoundCategory.Destruction:
                audioManager.PlayCubeDestructionSound(testCube.type, cubePosition);
                break;
            case SoundCategory.SpecialEffect:
                audioManager.PlayCubeSpecialEffectSound(testCube.type, cubePosition);
                break;
        }
    }
    
    private void TestAudioAtPlayerPosition()
    {
        if (playerManager == null)
        {
            LogAudioTest("PlayerManager not available");
            return;
        }
        
        Vector3 playerPos = new Vector3(playerManager.currentTilePosition.x, 0, playerManager.currentTilePosition.y);
        LogAudioTest($"Testing {selectedAudioCubeType} {selectedSoundCategory} audio at player position {playerPos}");
        TestCubeTypeAudio(selectedAudioCubeType, selectedSoundCategory, false, playerPos);
    }
    
    private void ValidateAudioConfiguration()
    {
        if (audioManager == null)
        {
            LogAudioTest("AudioManager not available for configuration validation");
            return;
        }
        
        LogAudioTest("Validating audio configuration...");
        audioManager.ValidateAudioClipAssignments();
        audioManager.ValidateAudioFolderStructure();
        
        var debugData = audioManager.GetDebugData();
        LogAudioTest($"Configuration valid: {debugData.GetValueOrDefault("Configuration Valid", false)}");
        LogAudioTest($"Configured cube types: {debugData.GetValueOrDefault("Configured Cube Types", "0/0")}");
    }
    
    private void TestEntireAudioSystem()
    {
        if (audioManager == null)
        {
            LogAudioTest("AudioManager not available for system test");
            return;
        }
        
        LogAudioTest("Running complete audio system test...");
        audioManager.TestAudioSystem();
        LogAudioTest("Audio system test completed - check console for detailed results");
    }
    
    private void TestVolumeControls()
    {
        if (audioManager == null)
        {
            LogAudioTest("AudioManager not available for volume testing");
            return;
        }
        
        LogAudioTest($"Testing volume controls at {testAudioVolume:F2} volume");
        audioManager.TestVolumeAdjustment();
        
        // Also test the AudioManager's testing volume control
        float originalTestingVolume = audioManager.testingVolume;
        audioManager.testingVolume = testAudioVolume;
        audioManager.TestCubeLandingSound(selectedAudioCubeType);
        audioManager.testingVolume = originalTestingVolume;
        
        LogAudioTest("Volume control test completed");
    }
    
    private void TestCubeTypeAudio(CubeType cubeType, SoundCategory category, bool isPreview = false, Vector3? customPosition = null)
    {
        if (audioManager == null || !audioManager.IsInitialized) return;
        
        Vector3 testPos = customPosition ?? GetAudioTestPosition();
        
        // Store original volume and apply test volume
        float originalImpactVolume = audioManager.cubeImpactVolume;
        audioManager.cubeImpactVolume = testAudioVolume;
        
        try
        {
            switch (category)
            {
                case SoundCategory.Landing:
                    audioManager.PlayCubeLandingSound(cubeType, testPos);
                    break;
                case SoundCategory.Capture:
                    audioManager.PlayCubeCaptureSound(cubeType, testPos);
                    break;
                case SoundCategory.Destruction:
                    audioManager.PlayCubeDestructionSound(cubeType, testPos);
                    break;
                case SoundCategory.SpecialEffect:
                    audioManager.PlayCubeSpecialEffectSound(cubeType, testPos);
                    break;
            }
            
            if (!isPreview)
            {
                LogAudioTest($"Played {cubeType} {category} sound at {testPos}");
            }
        }
        catch (System.Exception ex)
        {
            LogAudioTest($"Error playing {cubeType} {category} sound: {ex.Message}");
        }
        finally
        {
            // Restore original volume
            audioManager.cubeImpactVolume = originalImpactVolume;
        }
    }
    
    private Vector3 GetAudioTestPosition()
    {
        if (useRandomPositions)
        {
            return new Vector3(
                UnityEngine.Random.Range(-5f, 5f),
                0f,
                UnityEngine.Random.Range(-5f, 5f)
            );
        }
        else
        {
            return new Vector3(testPosition.x, 0f, testPosition.y);
        }
    }
    
    private System.Collections.IEnumerator DelayedAudioTest(CubeType cubeType, SoundCategory category, int delayMs)
    {
        yield return new WaitForSeconds(delayMs / 1000f);
        TestCubeTypeAudio(cubeType, category);
    }
    
    private void LogAudioTest(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string logEntry = $"[{timestamp}] {message}";
        
        audioTestHistory.Add(logEntry);
        
        // Limit history size
        while (audioTestHistory.Count > maxAudioHistoryEntries)
        {
            audioTestHistory.RemoveAt(0);
        }
        
        // Also log to console for debugging
        Debug.Log($"[AudioTesting] {message}");
    }
    
    private void ClearAudioTestHistory()
    {
        audioTestHistory.Clear();
        LogAudioTest("Audio test history cleared");
    }
    
    #endregion

    #region UI State
    private bool showCubeSpawning = true;
    private bool showFacePainter = true;
    private bool showCubeInspector = false;
    private bool showLifecycleTesting = true;
    private bool showInteractionTesting = true;
    private bool showReinforcedTests = false;
    private bool showAudioTesting = true;
    private Vector2 spawningScroll;
    private Vector2 facePainterScroll;
    private Vector2 inspectorScroll;
    private Vector2 lifecycleScroll;
    private Vector2 interactionScroll;
    private Vector2 audioTestingScroll;
    #endregion

    #region Individual Entity Testing State
    private CubeManager testCube = null;
    private CubeManager selectedCube = null; // For inspector
    private Tile testTile = null;
    private Vector2Int testPosition = new Vector2Int(0, 0);
    private bool autoTrackPlayer = true;
    private CubeType spawnCubeType = CubeType.Unit;
    private int testPaintDuration = 3;
    private int selectedFaceStatus = 1; // 1=Corrupted, 2=Enhanced
    private int lifecycleStepIndex = 0;
    private bool isLifecycleRunning = false;
    private float lifecycleStartTime;
    #region Audio Testing State
    private float testAudioVolume = 0.8f;
    private bool enableAudioPreview = true;
    private CubeType selectedAudioCubeType = CubeType.Unit;
    private SoundCategory selectedSoundCategory = SoundCategory.Landing;
    private Vector3 audioTestPosition = Vector3.zero;
    private bool useRandomPositions = true;
    private List<string> audioTestHistory = new List<string>();
    private int maxAudioHistoryEntries = 8;
    #endregion
    private bool trackInteractionHistory = true;
    private List<string> interactionHistory = new List<string>();
    private int maxHistoryEntries = 10;
    private bool autoTestOnSpawn = false;
    private bool paintTileBeforeTest = false;
    #endregion

    public override void Initialize()
    {
        base.Initialize();
        
        gridManager = Object.FindObjectOfType<GridManager>();
        playerManager = Object.FindObjectOfType<PlayerManager>();
        waveManager = Object.FindObjectOfType<WaveManager>();
        audioManager = AudioManager.Instance;

        if (playerManager != null)
        {
            testPosition = playerManager.currentTilePosition;
        }

        ClearInteractionHistory();
        ClearAudioTestHistory();
        LogInteraction("Panel initialized");
        LogAudioTest("Audio testing panel initialized");
    }

    public override void Update()
    {
        // Auto-track player position
        if (autoTrackPlayer && playerManager != null)
        {
            testPosition = playerManager.currentTilePosition;
        }

        // Auto-clear destroyed cubes
        if (testCube != null && (testCube.isDestroyed || testCube == null))
        {
            LogInteraction($"Test cube was destroyed at step {lifecycleStepIndex}");
            testCube = null;
            isLifecycleRunning = false;
        }

        if (selectedCube != null && (selectedCube.isDestroyed || selectedCube == null))
        {
            selectedCube = null;
        }

        // Update test tile reference
        if (gridManager != null)
        {
            testTile = gridManager.GetTileAt(testPosition);
        }

        // Handle running lifecycle test
        if (isLifecycleRunning && testCube != null)
        {
            HandleLifecycleProgression();
        }
    }

    protected override void DrawPanelContent()
    {
        DrawSectionToggles();
        DebugUIHelpers.Space(5);

        DrawTestEntityStatus();
        DebugUIHelpers.Space(5);

        if (showCubeSpawning) DrawCubeSpawningSection();
        if (showFacePainter) DrawFacePainterSection();
        if (showLifecycleTesting) DrawLifecycleTestingSection();
        if (showInteractionTesting) DrawInteractionTestingSection();
        if (showAudioTesting) DrawAudioTestingSection();
    }

    #region UI Drawing Methods

    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        showCubeSpawning = DebugUIHelpers.DrawToggleButton("Spawning", showCubeSpawning);
        showFacePainter = DebugUIHelpers.DrawToggleButton("Face Painter", showFacePainter);
        showCubeInspector = DebugUIHelpers.DrawToggleButton("Inspector", showCubeInspector);
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        showLifecycleTesting = DebugUIHelpers.DrawToggleButton("Lifecycle", showLifecycleTesting);
        showInteractionTesting = DebugUIHelpers.DrawToggleButton("Interactions", showInteractionTesting);
        showAudioTesting = DebugUIHelpers.DrawToggleButton("Audio Testing", showAudioTesting);
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        showReinforcedTests = DebugUIHelpers.DrawToggleButton("Reinforced", showReinforcedTests);
        GUILayout.EndHorizontal();
    }

    private void DrawCubeSpawningSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CUBE SPAWNING & SELECTION", GUI.skin.box);

        spawningScroll = GUILayout.BeginScrollView(spawningScroll, GUILayout.MinHeight(200));

        // Cube type selection
        GUILayout.Label("Cube Type:", GUI.skin.box);
        spawnCubeType = (CubeType)DrawCubeTypeSelector((int)spawnCubeType);

        DebugUIHelpers.Space(5);

        // Spawning controls
        GUILayout.Label("Spawning Controls:", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn at Test Position"))
        {
            SpawnCubeAtTestPosition();
        }
        if (GUILayout.Button("Spawn at Player"))
        {
            SpawnCubeAtPlayer();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn Multiple (Line)"))
        {
            SpawnMultipleCubesLine();
        }
        if (GUILayout.Button("Spawn Multiple (Pattern)"))
        {
            SpawnMultipleCubesPattern();
        }
        GUILayout.EndHorizontal();

        DebugUIHelpers.Space(5);

        // Quick selection from existing cubes
        GUILayout.Label("Quick Selection:", GUI.skin.box);
        var cubesAtPosition = DebugCubeSpawnHelper.FindCubesAt(testPosition);
        if (cubesAtPosition.Count > 0)
        {
            GUILayout.Label($"Cubes at test position ({testPosition.x}, {testPosition.y}):");
            foreach (var cube in cubesAtPosition.Take(3))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{cube.type}:", GUILayout.Width(60));
                if (GUILayout.Button("Select as Test", GUILayout.Width(80)))
                {
                    testCube = cube;
                    LogInteraction($"Selected {cube.type} cube as test cube");
                }
                if (GUILayout.Button("Select for Inspector", GUILayout.Width(120)))
                {
                    selectedCube = cube;
                    showCubeInspector = true;
                    LogInteraction($"Selected {cube.type} cube for inspection");
                }
                GUILayout.EndHorizontal();
            }
        }
        else
        {
            GUILayout.Label($"No cubes at test position ({testPosition.x}, {testPosition.y})");
        }

        DebugUIHelpers.Space(5);

        // Clear controls
        GUILayout.Label("Clear Controls:", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Test Cube"))
        {
            ClearTestCube();
        }
        if (GUILayout.Button("Clear Selected Cube"))
        {
            selectedCube = null;
            LogInteraction("Cleared selected cube");
        }
        GUILayout.EndHorizontal();

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }
    
    private void DrawAudioTestingSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CUBE AUDIO TESTING", GUI.skin.box);
        
        audioTestingScroll = GUILayout.BeginScrollView(audioTestingScroll, GUILayout.MinHeight(250));
        
        // Audio system status
        if (audioManager == null)
        {
            DebugUIHelpers.WithColor(DebugUIHelpers.ErrorColor, () =>
            {
                GUILayout.Label("⚠ AudioManager not found! Audio testing unavailable.");
            });
        }
        else if (!audioManager.IsInitialized)
        {
            DebugUIHelpers.WithColor(DebugUIHelpers.WarningColor, () =>
            {
                GUILayout.Label("⚠ AudioManager not initialized.");
            });
        }
        else
        {
            DebugUIHelpers.WithColor(DebugUIHelpers.SuccessColor, () =>
            {
                GUILayout.Label($"✓ AudioManager ready - {audioManager.ActiveSources}/{audioManager.GetDebugData()["Max Simultaneous Sounds"]} sources active");
            });
        }
        
        DebugUIHelpers.Space(5);
        
        // Audio testing configuration
        GUILayout.Label("Audio Testing Configuration:", GUI.skin.box);
        
        // Volume control with real-time testing
        GUILayout.BeginHorizontal();
        GUILayout.Label("Test Volume:", GUILayout.Width(80));
        float newVolume = GUILayout.HorizontalSlider(testAudioVolume, 0f, 1f, GUILayout.Width(100));
        if (Mathf.Abs(newVolume - testAudioVolume) > 0.01f)
        {
            testAudioVolume = newVolume;
            if (enableAudioPreview && audioManager != null)
            {
                // Play a quick preview sound when volume changes
                TestQuickAudioPreview();
            }
        }
        GUILayout.Label($"{testAudioVolume:F2}", GUILayout.Width(40));
        GUILayout.EndHorizontal();
        
        enableAudioPreview = GUILayout.Toggle(enableAudioPreview, "Enable real-time audio preview");
        useRandomPositions = GUILayout.Toggle(useRandomPositions, "Use random 3D positions for testing");
        
        DebugUIHelpers.Space(5);
        
        // Cube type selection for audio testing
        GUILayout.Label("Cube Type for Audio Testing:", GUI.skin.box);
        selectedAudioCubeType = (CubeType)DrawAudioCubeTypeSelector((int)selectedAudioCubeType);
        
        DebugUIHelpers.Space(3);
        
        // Sound category selection
        GUILayout.Label("Sound Category:", GUI.skin.box);
        selectedSoundCategory = DrawSoundCategorySelector(selectedSoundCategory);
        
        DebugUIHelpers.Space(5);
        
        // Quick audio tests
        GUILayout.Label("Quick Audio Tests:", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Test Selected Cube Type"))
        {
            TestSelectedCubeAudio();
        }
        if (GUILayout.Button("Test All Cube Types"))
        {
            TestAllCubeTypesAudio();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Test Current Test Cube"))
        {
            TestCurrentTestCubeAudio();
        }
        if (GUILayout.Button("Test at Player Position"))
        {
            TestAudioAtPlayerPosition();
        }
        GUILayout.EndHorizontal();
        
        DebugUIHelpers.Space(5);
        
        // Comprehensive audio system testing
        GUILayout.Label("System Audio Tests:", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Validate Audio Configuration"))
        {
            ValidateAudioConfiguration();
        }
        if (GUILayout.Button("Test Audio System"))
        {
            TestEntireAudioSystem();
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Test Volume Controls"))
        {
            TestVolumeControls();
        }
        if (GUILayout.Button("Clear Audio History"))
        {
            ClearAudioTestHistory();
        }
        GUILayout.EndHorizontal();
        
        DebugUIHelpers.Space(5);
        
        // Audio test history
        if (audioTestHistory.Count > 0)
        {
            GUILayout.Label("Audio Test History:", GUI.skin.box);
            
            foreach (var entry in audioTestHistory.TakeLast(6))
            {
                GUILayout.Label($"• {entry}");
            }
            
            if (audioTestHistory.Count > 6)
            {
                GUILayout.Label($"... and {audioTestHistory.Count - 6} more entries");
            }
        }
        
        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawTestEntityStatus()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("TEST ENTITY STATUS", GUI.skin.box);

        // Position controls
        var (newPosition, newAutoTrack) = DebugUIHelpers.DrawTargetPositionControls(
            "Test Position:", testPosition, autoTrackPlayer, playerManager, gridManager);
        testPosition = newPosition;
        autoTrackPlayer = newAutoTrack;

        // Entity status display
        GUILayout.BeginHorizontal();
        
        // Cube status
        GUILayout.BeginVertical(GUILayout.Width(150));
        GUILayout.Label("Test Cube:", GUI.skin.box);
        if (testCube != null)
        {
            DebugUIHelpers.WithColor(DebugUIHelpers.GetCubeDisplayColor(testCube.type), () =>
            {
                GUILayout.Label($"{testCube.type} cube");
                GUILayout.Label($"HP: {testCube.currentHitPoints}/{testCube.maxHitPoints}");
                GUILayout.Label($"Position: ({testCube.position.x},{testCube.position.y})");
                var activeFace = testCube.GetCurrentDownFace();
                var faceStatus = testCube.GetActiveFaceStatus();
                GUILayout.Label($"Active: {activeFace} ({faceStatus})");
            });
        }
        else
        {
            GUILayout.Label("No test cube");
        }
        GUILayout.EndVertical();

        // Tile status
        GUILayout.BeginVertical(GUILayout.Width(150));
        GUILayout.Label("Test Tile:", GUI.skin.box);
        if (testTile != null)
        {
            DebugUIHelpers.WithColor(DebugTileHelper.GetTileDisplayColor(testTile), () =>
            {
                GUILayout.Label(DebugTileHelper.GetTileStateDescription(testTile));
                if (testTile.CanPaintCubes)
                {
                    GUILayout.Label($"Paint: {testTile.PaintStatus}");
                    GUILayout.Label($"Duration: {testTile.PaintDuration}");
                }
                // Enhanced tile features have been removed
                // if (testTile.IsAdvantaged)
                // {
                //     GUILayout.Label($"Charges: {testTile.DetonationCharges}");
                // }
            });
        }
        else
        {
            GUILayout.Label("No tile at position");
        }
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        // Quick entity manipulation
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Select Cube Here"))
        {
            SelectCubeAtTestPosition();
        }
        if (GUILayout.Button("Clear Test Entities"))
        {
            ClearTestEntities();
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawLifecycleTestingSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CUBE LIFECYCLE TESTING", GUI.skin.box);

        lifecycleScroll = GUILayout.BeginScrollView(lifecycleScroll, GUILayout.MinHeight(200));

        // Lifecycle configuration
        GUILayout.Label("Lifecycle Configuration:", GUI.skin.box);
        spawnCubeType = (CubeType)DrawCubeTypeSelector((int)spawnCubeType);
        testPaintDuration = DebugUIHelpers.DrawDurationControl("Paint Duration:", testPaintDuration);
        selectedFaceStatus = DebugUIHelpers.DrawFaceStatusSelector(selectedFaceStatus);

        DebugUIHelpers.Space(5);

        // Lifecycle control
        GUILayout.Label("Lifecycle Control:", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Full Auto Test") && !isLifecycleRunning)
        {
            StartAutomatedLifecycleTest();
        }
        if (GUILayout.Button("Step-by-Step") && !isLifecycleRunning)
        {
            StartStepByStepLifecycleTest();
        }
        if (GUILayout.Button("Stop Test") && isLifecycleRunning)
        {
            StopLifecycleTest();
        }
        GUILayout.EndHorizontal();

        // Manual step controls
        if (!isLifecycleRunning)
        {
            GUILayout.Label("Manual Lifecycle Steps:", GUI.skin.box);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("1. Spawn"))
            {
                PerformLifecycleStep_Spawn();
            }
            if (GUILayout.Button("2. Paint"))
            {
                PerformLifecycleStep_Paint();
            }
            if (GUILayout.Button("3. Move"))
            {
                PerformLifecycleStep_Move();
            }
            if (GUILayout.Button("4. Capture"))
            {
                PerformLifecycleStep_Capture();
            }
            GUILayout.EndHorizontal();
        }

        // Lifecycle status
        if (isLifecycleRunning)
        {
            DebugUIHelpers.WithColor(DebugUIHelpers.SuccessColor, () =>
            {
                GUILayout.Label($"Running lifecycle test - Step {lifecycleStepIndex}/4");
                float elapsed = Time.time - lifecycleStartTime;
                GUILayout.Label($"Elapsed: {elapsed:F1}s");
            });
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawInteractionTestingSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CUBE-TILE INTERACTION TESTING", GUI.skin.box);

        interactionScroll = GUILayout.BeginScrollView(interactionScroll, GUILayout.MinHeight(200));

        // Interaction configuration
        GUILayout.Label("Interaction Configuration:", GUI.skin.box);
        GUILayout.BeginHorizontal();
        autoTestOnSpawn = GUILayout.Toggle(autoTestOnSpawn, "Auto-test on spawn");
        paintTileBeforeTest = GUILayout.Toggle(paintTileBeforeTest, "Paint tile first");
        GUILayout.EndHorizontal();

        trackInteractionHistory = GUILayout.Toggle(trackInteractionHistory, "Track interaction history");
        maxHistoryEntries = DebugUIHelpers.DrawIntField("History limit:", maxHistoryEntries, 5, 50);

        DebugUIHelpers.Space(5);

        // Specific interaction tests
        GUILayout.Label("Specific Interaction Tests:", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cube -> Tile"))
        {
            TestCubeToTileInteraction();
        }
        if (GUILayout.Button("Tile -> Cube"))
        {
            TestTileToCubeInteraction();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Landing Test"))
        {
            TestCubeLandingInteraction();
        }
        if (GUILayout.Button("Exit Test"))
        {
            TestCubeExitInteraction();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Duration Test"))
        {
            TestPaintDurationInteraction();
        }
        if (GUILayout.Button("Clear History"))
        {
            ClearInteractionHistory();
        }
        GUILayout.EndHorizontal();

        DebugUIHelpers.Space(5);

        // Interaction history display
        if (trackInteractionHistory && interactionHistory.Count > 0)
        {
            GUILayout.Label("Interaction History:", GUI.skin.box);
            
            foreach (var entry in interactionHistory.TakeLast(5))
            {
                GUILayout.Label($"• {entry}");
            }
            
            if (interactionHistory.Count > 5)
            {
                GUILayout.Label($"... and {interactionHistory.Count - 5} more entries");
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    private void DrawFacePainterSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("FACE PAINTER", GUI.skin.box);

        facePainterScroll = GUILayout.BeginScrollView(facePainterScroll, GUILayout.MinHeight(250));

        // Face painting settings
        GUILayout.Label("Face Painting Settings:", GUI.skin.box);
        selectedFaceStatus = DebugUIHelpers.DrawFaceStatusSelector(selectedFaceStatus);
        testPaintDuration = DebugUIHelpers.DrawDurationControl("Duration:", testPaintDuration);

        DebugUIHelpers.Space(5);

        // Tile painting setup
        GUILayout.Label("Tile Painting Setup:", GUI.skin.box);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Setup Tile Painting"))
        {
            SetupTilePainting(testPosition);
        }
        if (GUILayout.Button("Clear Tile Painting"))
        {
            ClearTilePainting(testPosition);
        }
        GUILayout.EndHorizontal();

        DebugUIHelpers.Space(5);

        // Direct cube face painting
        GUILayout.Label("Direct Cube Face Painting:", GUI.skin.box);
        var cubesAtPosition = DebugCubeSpawnHelper.FindCubesAt(testPosition);
        if (cubesAtPosition.Count > 0)
        {
            GUILayout.Label($"Cubes at ({testPosition.x}, {testPosition.y}):");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Paint All Current Faces"))
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
            foreach (var cube in cubesAtPosition.Take(3))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{cube.type}:", GUILayout.Width(60));

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
            GUILayout.Label($"No cubes at ({testPosition.x}, {testPosition.y})");
        }

        DebugUIHelpers.Space(5);

        // Test cube specific painting
        if (testCube != null)
        {
            GUILayout.Label("Test Cube Face Painting:", GUI.skin.box);
            var currentFace = testCube.GetCurrentDownFace();
            var faceStatus = testCube.GetFaceStatus(currentFace);
            
            GUILayout.Label($"Current Down Face: {currentFace} ({faceStatus})");
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Paint Current Face"))
            {
                PaintCubeFace(testCube);
            }
            if (GUILayout.Button("Clear Current Face"))
            {
                testCube.SetFaceStatus(currentFace, FaceStatus.None, 0);
                LogInteraction($"Cleared {currentFace} face on test cube");
            }
            if (GUILayout.Button("Test All Faces"))
            {
                TestAllFaceConversions();
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    #endregion

    #region Lifecycle Testing Implementation

    private void StartAutomatedLifecycleTest()
    {
        LogInteraction("Starting automated lifecycle test");
        isLifecycleRunning = true;
        lifecycleStepIndex = 0;
        lifecycleStartTime = Time.time;
        
        // Start with spawning
        PerformLifecycleStep_Spawn();
    }

    private void StartStepByStepLifecycleTest()
    {
        LogInteraction("Starting step-by-step lifecycle test");
        isLifecycleRunning = true;
        lifecycleStepIndex = 0;
        lifecycleStartTime = Time.time;
        
        // Don't auto-advance in step mode
    }

    private void StopLifecycleTest()
    {
        LogInteraction($"Stopped lifecycle test at step {lifecycleStepIndex}");
        isLifecycleRunning = false;
        lifecycleStepIndex = 0;
    }

    private void HandleLifecycleProgression()
    {
        // Auto-advance lifecycle steps with timing
        float elapsed = Time.time - lifecycleStartTime;
        
        // Each step takes 2 seconds in auto mode
        int expectedStep = Mathf.FloorToInt(elapsed / 2f);
        
        if (expectedStep > lifecycleStepIndex && expectedStep < 4)
        {
            lifecycleStepIndex = expectedStep;
            
            switch (lifecycleStepIndex)
            {
                case 1:
                    PerformLifecycleStep_Paint();
                    break;
                case 2:
                    PerformLifecycleStep_Move();
                    break;
                case 3:
                    PerformLifecycleStep_Capture();
                    break;
            }
        }
        
        // Complete test after 8 seconds
        if (elapsed > 8f)
        {
            LogInteraction("Automated lifecycle test completed");
            isLifecycleRunning = false;
        }
    }

    private void PerformLifecycleStep_Spawn()
    {
        // Clear any existing test cube
        if (testCube != null)
        {
            Object.Destroy(testCube.gameObject);
        }

        // Spawn new cube at test position
        bool spawned = DebugCubeSpawnHelper.SpawnCubeAt(testPosition, spawnCubeType, gridManager, waveManager);
        
        if (spawned)
        {
            // Find the spawned cube
            var cubesAtPosition = DebugCubeSpawnHelper.FindCubesAt(testPosition);
            testCube = cubesAtPosition.LastOrDefault(); // Get the most recently spawned
            
            LogInteraction($"Step 1: Spawned {spawnCubeType} cube at ({testPosition.x},{testPosition.y})");
            
            if (autoTestOnSpawn)
            {
                TestCubeToTileInteraction();
            }
        }
        else
        {
            LogInteraction($"Step 1: Failed to spawn {spawnCubeType} cube at ({testPosition.x},{testPosition.y})");
        }
        
        lifecycleStepIndex = 1;
    }

    private void PerformLifecycleStep_Paint()
    {
        if (testCube == null)
        {
            LogInteraction("Step 2: No cube to paint");
            return;
        }

        FaceStatus status = selectedFaceStatus == 1 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
        CubeFace currentFace = testCube.GetCurrentDownFace();
        
        testCube.SetFaceStatus(currentFace, status, testPaintDuration);
        LogInteraction($"Step 2: Painted {currentFace} face with {status} (duration: {testPaintDuration})");
        
        // Test the painting immediately
        TestTileToCubeInteraction();
        
        lifecycleStepIndex = 2;
    }

    private void PerformLifecycleStep_Move()
    {
        if (testCube == null)
        {
            LogInteraction("Step 3: No cube to move");
            return;
        }

        Vector2Int oldPosition = testCube.position;
        testCube.MoveForward();
        Vector2Int newPosition = testCube.position;
        
        LogInteraction($"Step 3: Moved cube from ({oldPosition.x},{oldPosition.y}) to ({newPosition.x},{newPosition.y})");
        
        // Test interaction at new position
        TestCubeLandingInteraction();
        
        lifecycleStepIndex = 3;
    }

    private void PerformLifecycleStep_Capture()
    {
        if (testCube == null)
        {
            LogInteraction("Step 4: No cube to capture");
            return;
        }

        bool canBeCaptured = testCube.CanBeCaptured();
        bool shouldDetonate = testCube.ShouldCreateDetonation();
        
        LogInteraction($"Step 4: Capture test - Can capture: {canBeCaptured}, Should detonate: {shouldDetonate}");
        
        // Simulate capture by dealing damage
        bool destroyed = testCube.TakeDamage(testCube.currentHitPoints);
        
        if (destroyed)
        {
            LogInteraction("Step 4: Cube was destroyed (captured)");
            testCube = null;
        }
        else
        {
            LogInteraction("Step 4: Cube survived capture attempt");
        }
        
        lifecycleStepIndex = 4;
    }

    #endregion

    #region Interaction Testing Implementation

    private void TestCubeToTileInteraction()
    {
        if (testCube == null || testTile == null)
        {
            LogInteraction("Cannot test cube-to-tile: missing entities");
            return;
        }

        var faceBefore = testCube.GetCurrentDownFace();
        var statusBefore = testCube.GetActiveFaceStatus();
        
        // Force the cube to interact with the tile (simulate landing)
        if (testTile.CanPaintCubes)
        {
            testCube.SetFaceStatus(faceBefore, testTile.PaintStatus, testTile.PaintDuration);
            LogInteraction($"Cube-to-Tile: {faceBefore} face painted with {testTile.PaintStatus}");
        }
        else
        {
            LogInteraction("Cube-to-Tile: No tile painting configured");
        }
    }

    private void TestTileToCubeInteraction()
    {
        if (testCube == null || testTile == null)
        {
            LogInteraction("Cannot test tile-to-cube: missing entities");
            return;
        }

        // Setup tile painting if requested
        if (paintTileBeforeTest)
        {
            FaceStatus status = selectedFaceStatus == 1 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
            Color color = selectedFaceStatus == 1 ? Color.red : Color.blue;
            
            DebugTileHelper.SetupTilePainting(testPosition, status, color, testPaintDuration, gridManager);
            LogInteraction($"Tile-to-Cube: Setup tile painting with {status}");
        }

        var effectiveBefore = testCube.GetEffectiveType();
        var effectiveAfter = testCube.GetEffectiveType();
        
        LogInteraction($"Tile-to-Cube: Effective type {effectiveBefore} → {effectiveAfter}");
    }

    private void TestCubeLandingInteraction()
    {
        if (testCube == null)
        {
            LogInteraction("Cannot test cube landing: no test cube");
            return;
        }

        Vector2Int cubePos = testCube.position;
        Tile landingTile = gridManager?.GetTileAt(cubePos);
        
        if (landingTile != null)
        {
            string tileState = DebugTileHelper.GetTileStateDescription(landingTile);
            LogInteraction($"Landing: Cube landed on {tileState} tile at ({cubePos.x},{cubePos.y})");
            
            if (landingTile.CanPaintCubes)
            {
                var currentFace = testCube.GetCurrentDownFace();
                LogInteraction($"Landing: Face {currentFace} exposed to tile painter");
            }
        }
        else
        {
            LogInteraction($"Landing: No tile found at ({cubePos.x},{cubePos.y})");
        }
    }

    private void TestCubeExitInteraction()
    {
        if (testCube == null)
        {
            LogInteraction("Cannot test cube exit: no test cube");
            return;
        }

        LogInteraction($"Exit: Cube exiting from position ({testCube.position.x},{testCube.position.y})");
        
        // Move cube to trigger exit
        testCube.MoveForward();
        
        LogInteraction($"Exit: Cube moved to ({testCube.position.x},{testCube.position.y})");
    }

    private void TestPaintDurationInteraction()
    {
        if (testCube == null)
        {
            LogInteraction("Cannot test paint duration: no test cube");
            return;
        }

        var currentFace = testCube.GetCurrentDownFace();
        int currentDuration = testCube.GetFaceDuration(currentFace);
        
        LogInteraction($"Duration Test: {currentFace} face has {currentDuration} duration remaining");
        
        // Test different durations
        testCube.SetFaceStatus(currentFace, FaceStatus.Enhanced, 1);
        LogInteraction("Duration Test: Set 1-second paint duration");
        
        // Test permanent duration
        testCube.SetFaceStatus(currentFace, FaceStatus.Corrupted, -1);
        LogInteraction("Duration Test: Set permanent paint duration");
    }

    #endregion

    #region Face Painting Details Implementation

    private void DrawDetailedFaceStatus(CubeManager cube)
    {
        var downFace = cube.GetCurrentDownFace();
        var activeStatus = cube.GetActiveFaceStatus();
        var effectiveType = cube.GetEffectiveType();

        GUILayout.Label($"Current Down Face: {downFace}");
        GUILayout.Label($"Active Status: {activeStatus}");
        GUILayout.Label($"Effective Type: {effectiveType}");

        DebugUIHelpers.Space(3);

        // Detailed face table
        GUILayout.BeginHorizontal();
        GUILayout.Label("Face", GUILayout.Width(60));
        GUILayout.Label("Status", GUILayout.Width(80));
        GUILayout.Label("Duration", GUILayout.Width(60));
        GUILayout.Label("Active", GUILayout.Width(50));
        GUILayout.EndHorizontal();

        for (int i = 0; i < 4; i++)
        {
            var face = (CubeFace)i;
            var status = cube.GetFaceStatus(face);
            var duration = cube.GetFaceDuration(face);
            bool isCurrentDown = face == downFace;

            Color bgColor = Color.white;
            if (isCurrentDown) bgColor = Color.yellow;
            else if (status == FaceStatus.Corrupted) bgColor = new Color(1f, 0.5f, 0.5f);
            else if (status == FaceStatus.Enhanced) bgColor = new Color(0.5f, 0.5f, 1f);

            DebugUIHelpers.WithBackgroundColor(bgColor, () =>
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"{face}", GUILayout.Width(60));
                GUILayout.Label(status.ToString(), GUILayout.Width(80));
                GUILayout.Label(duration == -1 ? "∞" : duration.ToString(), GUILayout.Width(60));
                GUILayout.Label(isCurrentDown ? "YES" : "", GUILayout.Width(50));
                GUILayout.EndHorizontal();
            });
        }
    }

    private void DrawPreciseFaceControls(CubeManager cube)
    {
        var currentFace = cube.GetCurrentDownFace();
        
        // Current face manipulation
        GUILayout.Label($"Current Down Face ({currentFace}):");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Paint Corrupted"))
        {
            cube.SetFaceStatus(currentFace, FaceStatus.Corrupted, testPaintDuration);
            LogInteraction($"Painted {currentFace} face with Corrupted status");
        }
        if (GUILayout.Button("Paint Enhanced"))
        {
            cube.SetFaceStatus(currentFace, FaceStatus.Enhanced, testPaintDuration);
            LogInteraction($"Painted {currentFace} face with Enhanced status");
        }
        if (GUILayout.Button("Clear Face"))
        {
            cube.SetFaceStatus(currentFace, FaceStatus.None, 0);
            LogInteraction($"Cleared {currentFace} face");
        }
        GUILayout.EndHorizontal();

        DebugUIHelpers.Space(5);

        // Individual face controls
        for (int i = 0; i < 4; i++)
        {
            var face = (CubeFace)i;
            var status = cube.GetFaceStatus(face);
            bool isActive = face == currentFace;

            GUILayout.BeginHorizontal();
            
            DebugUIHelpers.WithBackgroundColor(isActive ? Color.yellow : Color.white, () =>
            {
                GUILayout.Label($"{face}:", GUILayout.Width(60));
            });

            if (GUILayout.Button("C", GUILayout.Width(25)))
            {
                cube.SetFaceStatus(face, FaceStatus.Corrupted, testPaintDuration);
                LogInteraction($"Painted {face} face with Corrupted");
            }
            if (GUILayout.Button("E", GUILayout.Width(25)))
            {
                cube.SetFaceStatus(face, FaceStatus.Enhanced, testPaintDuration);
                LogInteraction($"Painted {face} face with Enhanced");
            }
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                cube.SetFaceStatus(face, FaceStatus.None, 0);
                LogInteraction($"Cleared {face} face");
            }

            // Status display
            if (status != FaceStatus.None)
            {
                DebugUIHelpers.WithColor(
                    status == FaceStatus.Corrupted ? DebugUIHelpers.CorruptedColor : DebugUIHelpers.EnhancedColor,
                    () => GUILayout.Label(status.ToString())
                );
            }
            else
            {
                GUILayout.Label("None");
            }

            GUILayout.EndHorizontal();
        }
    }

    private void DrawDurationRotationTesting(CubeManager cube)
    {
        // Duration testing
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Test 1s Duration"))
        {
            var face = cube.GetCurrentDownFace();
            cube.SetFaceStatus(face, FaceStatus.Enhanced, 1);
            LogInteraction($"Testing 1-second duration on {face}");
        }
        if (GUILayout.Button("Test Permanent"))
        {
            var face = cube.GetCurrentDownFace();
            cube.SetFaceStatus(face, FaceStatus.Corrupted, -1);
            LogInteraction($"Testing permanent duration on {face}");
        }
        GUILayout.EndHorizontal();

        // Rotation testing
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Force Rotate"))
        {
            var oldFace = cube.GetCurrentDownFace();
            cube.MoveForward(); // This should trigger rotation
            var newFace = cube.GetCurrentDownFace();
            LogInteraction($"Rotation test: {oldFace} → {newFace}");
        }
        if (GUILayout.Button("Debug Face Mapping"))
        {
            cube.DebugPrintFaceMapping();
            LogInteraction("Debug face mapping printed to console");
        }
        GUILayout.EndHorizontal();
    }

    #endregion

    #region Type Conversion Testing Implementation

    private void TestTypeConversion(CubeType fromType, FaceStatus paintStatus)
    {
        if (testCube == null)
        {
            LogInteraction($"Cannot test {fromType} → {paintStatus}: no test cube");
            return;
        }

        if (testCube.type != fromType)
        {
            LogInteraction($"Type mismatch: expected {fromType}, got {testCube.type}");
            return;
        }

        var currentFace = testCube.GetCurrentDownFace();
        var typeBefore = testCube.GetEffectiveType();
        
        testCube.SetFaceStatus(currentFace, paintStatus, testPaintDuration);
        
        var typeAfter = testCube.GetEffectiveType();
        
        LogInteraction($"Conversion test: {fromType} cube with {paintStatus} paint");
        LogInteraction($"Effective type: {typeBefore} → {typeAfter}");
        LogInteraction($"Can be captured: {testCube.CanBeCaptured()}");
        LogInteraction($"Should detonate: {testCube.ShouldCreateDetonation()}");
    }

    private void TestAllFaceConversions()
    {
        if (testCube == null)
        {
            LogInteraction("Cannot test all face conversions: no test cube");
            return;
        }

        LogInteraction($"Testing all face conversions on {testCube.type} cube");

        // Paint each face with alternating status
        for (int i = 0; i < 4; i++)
        {
            var face = (CubeFace)i;
            var status = i % 2 == 0 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
            testCube.SetFaceStatus(face, status, testPaintDuration);
            LogInteraction($"Face {face}: painted with {status}");
        }

        // Test effective type after all paintings
        var effectiveType = testCube.GetEffectiveType();
        LogInteraction($"All faces painted - Effective type: {effectiveType}");
    }

    #endregion

    #region Utility Methods

    private void SelectCubeAtTestPosition()
    {
        var cubesAtPosition = DebugCubeSpawnHelper.FindCubesAt(testPosition);
        
        if (cubesAtPosition.Count > 0)
        {
            testCube = cubesAtPosition.First();
            LogInteraction($"Selected {testCube.type} cube at ({testPosition.x},{testPosition.y})");
        }
        else
        {
            LogInteraction($"No cubes found at ({testPosition.x},{testPosition.y})");
        }
    }

    private void ClearTestEntities()
    {
        if (testCube != null)
        {
            Object.Destroy(testCube.gameObject);
            testCube = null;
            LogInteraction("Cleared test cube");
        }

        if (testTile != null && testTile.CanPaintCubes)
        {
            DebugTileHelper.ClearTilePainting(testPosition, gridManager);
            LogInteraction("Cleared test tile painting");
        }

        isLifecycleRunning = false;
        lifecycleStepIndex = 0;
    }

    private int DrawCubeTypeSelector(int currentType)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Type:", GUILayout.Width(40));
        
        for (int i = 0; i < System.Enum.GetValues(typeof(CubeType)).Length; i++)
        {
            var cubeType = (CubeType)i;
            Color bgColor = currentType == i ? DebugUIHelpers.GetCubeDisplayColor(cubeType) : Color.white;
            
            DebugUIHelpers.WithBackgroundColor(bgColor, () =>
            {
                if (GUILayout.Button(cubeType.ToString(), GUILayout.Width(70)))
                {
                    currentType = i;
                }
            });
        }
        
        GUILayout.EndHorizontal();
        return currentType;
    }

    private void LogInteraction(string message)
    {
        if (!trackInteractionHistory) return;

        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string logEntry = $"[{timestamp}] {message}";
        
        interactionHistory.Add(logEntry);
        
        // Limit history size
        while (interactionHistory.Count > maxHistoryEntries)
        {
            interactionHistory.RemoveAt(0);
        }

        // Also log to console for debugging
        Debug.Log($"[CubeTileIndividual] {message}");
    }

    private void ClearInteractionHistory()
    {
        interactionHistory.Clear();
        LogInteraction("Interaction history cleared");
    }

    #endregion

    #region Utility Methods (Absorbed from CubeDebugPanel)

    private void SpawnCubeAtTestPosition()
    {
        bool spawned = DebugCubeSpawnHelper.SpawnCubeAt(testPosition, spawnCubeType, gridManager, waveManager);
        if (spawned)
        {
            var cubesAtPosition = DebugCubeSpawnHelper.FindCubesAt(testPosition);
            testCube = cubesAtPosition.LastOrDefault();
            LogInteraction($"Spawned {spawnCubeType} cube at test position");
        }
        else
        {
            LogInteraction($"Failed to spawn {spawnCubeType} cube at test position");
        }
    }

    private void SpawnCubeAtPlayer()
    {
        if (playerManager == null) return;
        Vector2Int playerPos = playerManager.currentTilePosition;
        Vector2Int spawnPos = new Vector2Int(playerPos.x, playerPos.y + 1);
        
        bool spawned = DebugCubeSpawnHelper.SpawnCubeAt(spawnPos, spawnCubeType, gridManager, waveManager);
        if (spawned)
        {
            LogInteraction($"Spawned {spawnCubeType} cube at player position + 1");
        }
    }

    private void SpawnMultipleCubesLine()
    {
        int spawned = DebugCubeSpawnHelper.SpawnCubeLinePattern(
            testPosition, spawnCubeType, 3, Vector2Int.right, gridManager, waveManager);
        LogInteraction($"Spawned {spawned} cubes in line pattern");
    }

    private void SpawnMultipleCubesPattern()
    {
        if (playerManager == null) return;
        Vector2Int playerPos = playerManager.currentTilePosition;
        
        for (int i = 0; i < 3; i++)
        {
            Vector2Int spawnPos = new Vector2Int(playerPos.x + i - 1, playerPos.y + 2);
            DebugCubeSpawnHelper.SpawnCubeAt(spawnPos, spawnCubeType, gridManager, waveManager);
        }
        LogInteraction($"Spawned 3 {spawnCubeType} cubes in pattern");
    }

    private void ClearTestCube()
    {
        if (testCube != null)
        {
            Object.Destroy(testCube.gameObject);
            testCube = null;
            LogInteraction("Cleared test cube");
        }
        isLifecycleRunning = false;
    }

    private void SetupTilePainting(Vector2Int position)
    {
        FaceStatus status = selectedFaceStatus == 1 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
        Color color = selectedFaceStatus == 1 ? Color.red : Color.blue;
        DebugTileHelper.SetupTilePainting(position, status, color, testPaintDuration, gridManager, true, false);
        LogInteraction($"Setup tile painting at ({position.x},{position.y}) with {status}");
    }

    private void ClearTilePainting(Vector2Int position)
    {
        DebugTileHelper.ClearTilePainting(position, gridManager);
        LogInteraction($"Cleared tile painting at ({position.x},{position.y})");
    }

    private void PaintCubeFace(CubeManager cube)
    {
        if (cube == null) return;

        FaceStatus status = selectedFaceStatus == 1 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
        CubeFace currentFace = cube.GetCurrentDownFace();
        cube.SetFaceStatus(currentFace, status, testPaintDuration);

        LogInteraction($"Painted {currentFace} face of {cube.type} cube with {status} status");
    }

    #endregion
}

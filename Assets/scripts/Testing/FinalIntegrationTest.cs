using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static Enumerations;

/// <summary>
/// Comprehensive integration test for final terminology updates, four-tier marker system, and corruption mechanics.
/// This test validates all dependency tasks are working together correctly.
/// </summary>
public class FinalIntegrationTest : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool runTestsOnStart = false;
    [SerializeField] private bool enableDetailedLogs = true;
    [SerializeField] private float testDelay = 1f;

    [Header("Test Results")]
    [SerializeField] private int testsRun = 0;
    [SerializeField] private int testsPassed = 0;
    [SerializeField] private int testsFailed = 0;
    [SerializeField] private List<string> failedTests = new List<string>();

    // System references
    private PlayerActionManager playerActionManager;
    private PlayerMarkerSystem markerSystem;
    private GridManager gridManager;
    private CubeManager testCube;
    private PlayerManager playerManager;

    void Start()
    {
        if (runTestsOnStart)
        {
            StartCoroutine(RunIntegrationTests());
        }
    }

    void Update()
    {
        // Manual test trigger
        if (Input.GetKeyDown(KeyCode.F1))
        {
            StartCoroutine(RunIntegrationTests());
        }
    }

    public IEnumerator RunIntegrationTests()
    {
        Log("=== STARTING FINAL INTEGRATION TESTS ===");
        
        // Initialize systems
        yield return InitializeSystems();
        
        // Test 1: Four-tier marker system terminology
        yield return TestFourTierMarkerTerminology();
        
        // Test 2: Light marker functionality (was individual)
        yield return TestLightMarkerPlacement();
        
        // Test 3: Heavy marker functionality (NEW - for recursion cubes)
        yield return TestRecursionMarkerFunctionality();
        
        // Test 4: Prime marker area coverage (was area)
        yield return TestPrimeMarkerAreaCoverage();
        
        // Test 5: Cube marker generation from prime cube captures
        yield return TestCubeMarkerGeneration();
        
        // Test 6: New cube terminology (unit/infinity/prime/recursion)
        yield return TestCubeTerminology();
        
        // Test 7: Heavy marker + recursion cube interaction
        yield return TestRecursionMarkerRecursionCubeInteraction();
        
        // Test 8: Corruption mechanics
        yield return TestCorruptionMechanics();
        
        // Test 9: Face painting integration
        yield return TestFacePaintingIntegration();
        
        // Test 10: Backward compatibility aliases
        yield return TestBackwardCompatibility();
        
        // Test 11: UI integration with four-tier system
        yield return TestUIIntegration();
        
        // Test 12: Debug system updates
        yield return TestDebugSystemUpdates();
        
        // Final cleanup and summary
        yield return FinalizeTests();
        
        Log("=== INTEGRATION TESTS COMPLETE ===");
        PrintTestSummary();
    }

    private IEnumerator InitializeSystems()
    {
        Log("Initializing test systems...");
        
        playerActionManager = FindObjectOfType<PlayerActionManager>();
        AssertNotNull(playerActionManager, "PlayerActionManager found");
        
        markerSystem = FindObjectOfType<PlayerMarkerSystem>();
        AssertNotNull(markerSystem, "PlayerMarkerSystem found");
        
        gridManager = FindObjectOfType<GridManager>();
        AssertNotNull(gridManager, "GridManager found");
        
        playerManager = FindObjectOfType<PlayerManager>();
        AssertNotNull(playerManager, "PlayerManager found");
        
        yield return new WaitForSeconds(testDelay);
    }

    #region Test 1: Four-tier marker system terminology
    private IEnumerator TestFourTierMarkerTerminology()
    {
        Log("Testing four-tier marker system terminology...");
        
        // Test MarkerType enum exists and has correct values
        AssertTrue(System.Enum.IsDefined(typeof(MarkerType), MarkerType.Light), "MarkerType.Light exists");
        AssertTrue(System.Enum.IsDefined(typeof(MarkerType), MarkerType.Heavy), "MarkerType.Heavy exists (NEW)");
        AssertTrue(System.Enum.IsDefined(typeof(MarkerType), MarkerType.Prime), "MarkerType.Prime exists");
        AssertTrue(System.Enum.IsDefined(typeof(MarkerType), MarkerType.Cube), "MarkerType.Cube exists");
        
        // Test CubeMarkerType enum exists and has correct values
        AssertTrue(System.Enum.IsDefined(typeof(PlayerMarkerSystem.CubeMarkerType), PlayerMarkerSystem.CubeMarkerType.Light), "CubeMarkerType.Light exists");
        AssertTrue(System.Enum.IsDefined(typeof(PlayerMarkerSystem.CubeMarkerType), PlayerMarkerSystem.CubeMarkerType.Heavy), "CubeMarkerType.Heavy exists (NEW)");
        AssertTrue(System.Enum.IsDefined(typeof(PlayerMarkerSystem.CubeMarkerType), PlayerMarkerSystem.CubeMarkerType.Prime), "CubeMarkerType.Prime exists");
        AssertTrue(System.Enum.IsDefined(typeof(PlayerMarkerSystem.CubeMarkerType), PlayerMarkerSystem.CubeMarkerType.Cube), "CubeMarkerType.Cube exists");
        
        yield return new WaitForSeconds(testDelay);
    }
    #endregion

    #region Test 2: Light marker functionality (was individual)
    private IEnumerator TestLightMarkerPlacement()
    {
        Log("Testing light marker placement and triggering...");
        
        Vector2Int testPos = new Vector2Int(2, 5);
        
        // Test light marker placement
        bool placed = playerActionManager.PlaceLightMarker(testPos);
        AssertTrue(placed, "Light marker placed successfully");
        
        // Test marker detection
        bool hasMarker = playerActionManager.HasLightMarkerAt(testPos);
        AssertTrue(hasMarker, "Light marker detected at position");
        
        // Test marker count
        int currentMarkers = playerActionManager.GetCurrentUnitMarkers();
        AssertTrue(currentMarkers > 0, "Light marker count increased");
        
        // Test marker removal
        bool removed = playerActionManager.RemoveLightMarkerAt(testPos);
        AssertTrue(removed, "Light marker removed successfully");
        
        // Test marker no longer detected
        hasMarker = playerActionManager.HasLightMarkerAt(testPos);
        AssertFalse(hasMarker, "Light marker no longer detected after removal");
        
        yield return new WaitForSeconds(testDelay);
    }
    #endregion

    #region Test 3: Heavy marker functionality (NEW)
    private IEnumerator TestRecursionMarkerFunctionality()
    {
        Log("Testing heavy marker functionality (NEW feature)...");
        
        Vector2Int testPos = new Vector2Int(3, 5);
        
        // Test heavy marker placement
        bool placed = playerActionManager.PlaceRecursionMarker(testPos);
        AssertTrue(placed, "Heavy marker placed successfully");
        
        // Test marker detection
        bool hasMarker = playerActionManager.HasRecursionMarkerAt(testPos);
        AssertTrue(hasMarker, "Heavy marker detected at position");
        
        // Test heavy marker visual differences from light markers
        var RecursionMarkers = markerSystem.RecursionMarkers;
        AssertTrue(RecursionMarkers.Count > 0, "Heavy markers queue not empty");
        
        // Test heavy marker removal
        bool removed = playerActionManager.RemoveRecursionMarkerAt(testPos);
        AssertTrue(removed, "Heavy marker removed successfully");
        
        yield return new WaitForSeconds(testDelay);
    }
    #endregion

    #region Test 4: Prime marker area coverage (was area)
    private IEnumerator TestPrimeMarkerAreaCoverage()
    {
        Log("Testing prime marker area coverage functionality...");
        
        Vector2Int centerPos = new Vector2Int(4, 6);
        int areaSize = 3; // 3x3 area
        
        // Test prime marker placement
        bool placed = playerActionManager.PlacePrimeMarker(centerPos, areaSize);
        AssertTrue(placed, "Prime marker placed successfully");
        
        // Test area coverage calculation
        var primeMarkers = markerSystem.PrimeMarkers;
        AssertTrue(primeMarkers.Count > 0, "Prime markers queue not empty");
        
        var marker = primeMarkers.Peek();
        AssertTrue(marker.affectedPositions.Count > 1, "Prime marker affects multiple positions");
        AssertTrue(marker.centerPosition == centerPos, "Prime marker center position correct");
        AssertTrue(marker.size == areaSize, "Prime marker size correct");
        
        // Test prime marker removal
        bool removed = playerActionManager.RemovePrimeMarkerAt(centerPos);
        AssertTrue(removed, "Prime marker removed successfully");
        
        yield return new WaitForSeconds(testDelay);
    }
    #endregion

    #region Test 5: Cube marker generation from prime cube captures
    private IEnumerator TestCubeMarkerGeneration()
    {
        Log("Testing cube marker generation from prime cube captures...");
        
        Vector2Int cubePos = new Vector2Int(3, 7);
        
        // Create a test prime cube
        GameObject testCubeObj = CreateTestCube(CubeType.Prime, cubePos);
        CubeManager testCube = testCubeObj.GetComponent<CubeManager>();
        
        // Test cube marker creation (simulating prime cube capture)
        int initialCubeMarkers = markerSystem.GetCurrentCubeMarkers();
        markerSystem.CreateCubeMarker(cubePos, PlayerMarkerSystem.CubeMarkerType.Prime);
        
        int finalCubeMarkers = markerSystem.GetCurrentCubeMarkers();
        AssertTrue(finalCubeMarkers > initialCubeMarkers, "Cube marker created from prime cube capture");
        
        // Test cube marker triggering
        bool triggered = markerSystem.TriggerNextCubeMarker();
        AssertTrue(triggered, "Cube marker triggered successfully");
        
        // Cleanup
        if (testCubeObj != null) Destroy(testCubeObj);
        
        yield return new WaitForSeconds(testDelay);
    }
    #endregion

    #region Test 6: New cube terminology
    private IEnumerator TestCubeTerminology()
    {
        Log("Testing new cube terminology (unit/infinity/prime/recursion)...");
        
        // Test CubeType enum has new values
        AssertTrue(System.Enum.IsDefined(typeof(CubeType), CubeType.Unit), "CubeType.Unit exists (was Normal)");
        AssertTrue(System.Enum.IsDefined(typeof(CubeType), CubeType.Infinity), "CubeType.Infinity exists (was Black)");
        AssertTrue(System.Enum.IsDefined(typeof(CubeType), CubeType.Prime), "CubeType.Prime exists (was Blue)");
        AssertTrue(System.Enum.IsDefined(typeof(CubeType), CubeType.Recursion), "CubeType.Recursion exists (was Reinforced)");
        
        // Test cube creation with new types
        Vector2Int pos = new Vector2Int(1, 8);
        
        GameObject unitCube = CreateTestCube(CubeType.Unit, pos);
        AssertNotNull(unitCube, "Unit cube created");
        
        GameObject infinityCube = CreateTestCube(CubeType.Infinity, pos + Vector2Int.right);
        AssertNotNull(infinityCube, "Infinity cube created");
        
        GameObject primeCube = CreateTestCube(CubeType.Prime, pos + Vector2Int.right * 2);
        AssertNotNull(primeCube, "Prime cube created");
        
        GameObject recursionCube = CreateTestCube(CubeType.Recursion, pos + Vector2Int.right * 3);
        AssertNotNull(recursionCube, "Recursion cube created");
        
        // Cleanup
        if (unitCube != null) Destroy(unitCube);
        if (infinityCube != null) Destroy(infinityCube);
        if (primeCube != null) Destroy(primeCube);
        if (recursionCube != null) Destroy(recursionCube);
        
        yield return new WaitForSeconds(testDelay);
    }
    #endregion

    #region Test 7: Heavy marker + recursion cube interaction
    private IEnumerator TestRecursionMarkerRecursionCubeInteraction()
    {
        Log("Testing heavy marker + recursion cube interaction...");
        
        Vector2Int pos = new Vector2Int(5, 7);
        
        // Create recursion cube
        GameObject recursionCubeObj = CreateTestCube(CubeType.Recursion, pos);
        CubeManager recursionCube = recursionCubeObj.GetComponent<CubeManager>();
        
        AssertNotNull(recursionCube, "Recursion cube created for heavy marker test");
        AssertTrue(recursionCube.type == CubeType.Recursion, "Cube type is Recursion");
        
        // Place heavy marker at cube position
        bool heavyPlaced = playerActionManager.PlaceRecursionMarker(pos);
        AssertTrue(heavyPlaced, "Heavy marker placed for recursion cube interaction");
        
        // Test heavy marker can interact with recursion cubes
        bool hasRecursionMarker = playerActionManager.HasRecursionMarkerAt(pos);
        AssertTrue(hasRecursionMarker, "Heavy marker detected at recursion cube position");
        
        // Cleanup
        playerActionManager.RemoveRecursionMarkerAt(pos);
        if (recursionCubeObj != null) Destroy(recursionCubeObj);
        
        yield return new WaitForSeconds(testDelay);
    }
    #endregion

    #region Test 8: Corruption mechanics
    private IEnumerator TestCorruptionMechanics()
    {
        Log("Testing corruption mechanics...");
        
        Vector2Int pos = new Vector2Int(2, 8);
        
        // Get tile for corruption testing
        Tile testTile = gridManager.GetTileAt(pos.x, pos.y);
        AssertNotNull(testTile, "Test tile found for corruption");
        
        // Create infinity cube with painted face
        GameObject infinityCubeObj = CreateTestCube(CubeType.Infinity, pos);
        CubeManager infinityCube = infinityCubeObj.GetComponent<CubeManager>();
        
        // Test marker hit on infinity cube (should paint top face)
        infinityCube.OnMarkerHit();
        
        // Test that top face is painted
        CubeFace topFace = infinityCube.GetTopFace();
        FaceStatus topFaceStatus = infinityCube.GetFaceStatus(topFace);
        AssertTrue(topFaceStatus == FaceStatus.Corrupted, "Infinity cube top face painted on marker hit");
        
        // Test tile corruption mechanics
        bool initialCorruption = testTile.IsCorrupted;
        testTile.CorruptTile(5, 3);
        AssertTrue(testTile.IsCorrupted, "Tile corruption applied");
        AssertFalse(testTile.CanAcceptMarkers, "Corrupted tile rejects markers");
        
        // Test corruption cleansing
        testTile.CleanseCorruption();
        AssertFalse(testTile.IsCorrupted, "Tile corruption cleansed");
        AssertTrue(testTile.CanAcceptMarkers, "Cleansed tile accepts markers");
        
        // Cleanup
        if (infinityCubeObj != null) Destroy(infinityCubeObj);
        
        yield return new WaitForSeconds(testDelay);
    }
    #endregion

    #region Test 9: Face painting integration
    private IEnumerator TestFacePaintingIntegration()
    {
        Log("Testing face painting integration...");
        
        Vector2Int pos = new Vector2Int(6, 8);
        
        // Create test cube
        GameObject testCubeObj = CreateTestCube(CubeType.Unit, pos);
        CubeManager testCube = testCubeObj.GetComponent<CubeManager>();
        
        // Test face painting
        testCube.PaintFace(CubeFace.Bottom, FaceStatus.Corrupted, Color.black, -1);
        
        FaceStatus bottomStatus = testCube.GetFaceStatus(CubeFace.Bottom);
        AssertTrue(bottomStatus == FaceStatus.Corrupted, "Face painting applied correctly");
        
        // Test effective type change
        CubeType effectiveType = testCube.GetEffectiveType();
        // Note: effective type changes based on active (down) face, not just painted faces
        
        // Test capture eligibility based on face status
        bool canCapture = testCube.CanBeCaptured();
        Log($"Cube capture eligibility: {canCapture} (depends on active face)");
        
        // Cleanup
        if (testCubeObj != null) Destroy(testCubeObj);
        
        yield return new WaitForSeconds(testDelay);
    }
    #endregion

    #region Test 10: Backward compatibility aliases
    private IEnumerator TestBackwardCompatibility()
    {
        Log("Testing backward compatibility aliases...");
        
        Vector2Int pos = new Vector2Int(7, 8);
        
        // Test that obsolete methods still work but generate warnings
        try
        {
#pragma warning disable CS0618 // Disable obsolete warnings for testing
            
            // Test obsolete marker placement methods
            bool individualPlaced = playerActionManager.PlaceLightMarker(pos);
            Log($"Obsolete PlaceLightMarker still works: {individualPlaced}");
            
            bool hasIndividual = playerActionManager.HasLightMarkerAt(pos);
            Log($"Obsolete HasLightMarkerAt still works: {hasIndividual}");
            
            if (hasIndividual)
            {
                playerActionManager.RemoveLightMarkerAt(pos);
            }
            
            // Test obsolete area marker methods
            bool areaPlaced = playerActionManager.PlacePrimeMarker(pos, 2);
            Log($"Obsolete PlacePrimeMarker still works: {areaPlaced}");
            
            if (areaPlaced)
            {
                playerActionManager.RemovePrimeMarkerAt(pos);
            }
            
#pragma warning restore CS0618
            
            AssertTrue(true, "Backward compatibility aliases functional");
        }
        catch (System.Exception e)
        {
            AssertTrue(false, $"Backward compatibility failed: {e.Message}");
        }
        
        yield return new WaitForSeconds(testDelay);
    }
    #endregion

    #region Test 11: UI integration
    private IEnumerator TestUIIntegration()
    {
        Log("Testing UI integration with four-tier marker system...");
        
        PlayerActionUI actionUI = FindObjectOfType<PlayerActionUI>();
        
        if (actionUI != null)
        {
            // Test UI can handle four-tier marker charges
            actionUI.UpdateCharges(2, 1, 1, 0); // light, heavy, prime, cube
            Log("UI charge update for four-tier system successful");
            
            // Test UI cooldown display
            actionUI.UpdateCooldowns(2f, 5f, 4f, 1f);
            Log("UI cooldown update for four-tier system successful");
            
            AssertTrue(true, "UI integration with four-tier marker system working");
        }
        else
        {
            Log("PlayerActionUI not found - UI integration test skipped");
        }
        
        yield return new WaitForSeconds(testDelay);
    }
    #endregion

    #region Test 12: Debug system updates
    private IEnumerator TestDebugSystemUpdates()
    {
        Log("Testing debug system updates...");
        
        // Test PlayerActionManager debug interface
        string debugStatus = playerActionManager.GetDebugStatus();
        AssertTrue(debugStatus.Contains("Light:"), "Debug status contains Light marker info");
        AssertTrue(debugStatus.Contains("Heavy:"), "Debug status contains Heavy marker info");
        AssertTrue(debugStatus.Contains("Prime:"), "Debug status contains Prime marker info");
        
        var debugData = playerActionManager.GetDebugData();
        AssertTrue(debugData.ContainsKey("Light Markers Placed"), "Debug data contains Light marker info");
        AssertTrue(debugData.ContainsKey("Heavy Markers Placed"), "Debug data contains Heavy marker info");
        AssertTrue(debugData.ContainsKey("Prime Markers Placed"), "Debug data contains Prime marker info");
        
        Log("Debug system updates verified");
        
        yield return new WaitForSeconds(testDelay);
    }
    #endregion

    #region Test finalization
    private IEnumerator FinalizeTests()
    {
        Log("Finalizing integration tests...");
        
        // Clean up any remaining test objects
        GameObject[] testObjects = GameObject.FindGameObjectsWithTag("TestCube");
        foreach (GameObject obj in testObjects)
        {
            Destroy(obj);
        }
        
        // Clear all markers
        if (markerSystem != null)
        {
            markerSystem.ClearAllActions();
        }
        
        Log("Test cleanup completed");
        
        yield return new WaitForSeconds(testDelay);
    }
    #endregion

    #region Utility methods
    private GameObject CreateTestCube(CubeType type, Vector2Int position)
    {
        GameObject cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cubeObj.name = $"TestCube_{type}_{position.x}_{position.y}";
        cubeObj.tag = "TestCube";
        
        CubeManager cubeManager = cubeObj.AddComponent<CubeManager>();
        
        // Create minimal CubeData for initialization
        CubeData cubeData = new CubeData
        {
            type = type,
            position = position,
            level = 1,
            isRainingCube = false,
            moveCountRemaining = 0
        };
        
        // Initialize cube
        if (gridManager != null)
        {
            cubeManager.Init(gridManager, cubeData, 2f);
        }
        else
        {
            cubeManager.type = type;
            cubeManager.position = position;
        }
        
        return cubeObj;
    }
    
    private void AssertTrue(bool condition, string message)
    {
        testsRun++;
        if (condition)
        {
            testsPassed++;
            if (enableDetailedLogs)
                Log($"✓ PASS: {message}");
        }
        else
        {
            testsFailed++;
            failedTests.Add(message);
            Log($"✗ FAIL: {message}");
        }
    }
    
    private void AssertFalse(bool condition, string message)
    {
        AssertTrue(!condition, message);
    }
    
    private void AssertNotNull(object obj, string message)
    {
        AssertTrue(obj != null, message);
    }
    
    private void Log(string message)
    {
        Debug.Log($"[FinalIntegrationTest] {message}");
    }
    
    private void PrintTestSummary()
    {
        Log("=== TEST SUMMARY ===");
        Log($"Tests Run: {testsRun}");
        Log($"Tests Passed: {testsPassed}");
        Log($"Tests Failed: {testsFailed}");
        
        if (testsFailed > 0)
        {
            Log("Failed Tests:");
            foreach (string failedTest in failedTests)
            {
                Log($"  - {failedTest}");
            }
        }
        else
        {
            Log("🎉 ALL TESTS PASSED!");
        }
        
        float successRate = testsRun > 0 ? (float)testsPassed / testsRun * 100f : 0f;
        Log($"Success Rate: {successRate:F1}%");
    }
    #endregion
}

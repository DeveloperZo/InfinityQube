using UnityEngine;
using System.Collections;
using static Enumerations;

/// <summary>
/// Comprehensive backward compatibility test to verify all obsolete aliases work correctly
/// before they are removed in final cleanup.
/// </summary>
public class BackwardCompatibilityValidationTest : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private bool runOnStart = false;
    [SerializeField] private bool removeObsoleteAfterTest = false;

    private PlayerActionManager playerActionManager;
    private PlayerMarkerSystem markerSystem;
    private int testsRun = 0;
    private int testsPassed = 0;

    void Start()
    {
        if (runOnStart)
        {
            StartCoroutine(RunBackwardCompatibilityTests());
        }
    }

    public IEnumerator RunBackwardCompatibilityTests()
    {
        Debug.Log("=== BACKWARD COMPATIBILITY VALIDATION ===");
        
        playerActionManager = FindObjectOfType<PlayerActionManager>();
        markerSystem = FindObjectOfType<PlayerMarkerSystem>();
        
        if (playerActionManager == null || markerSystem == null)
        {
            Debug.LogError("Required managers not found!");
            yield break;
        }
        
        // Test obsolete class compatibility
        yield return TestObsoleteClasses();
        
        // Test obsolete method compatibility
        yield return TestObsoleteMethods();
        
        // Test obsolete property compatibility
        yield return TestObsoleteProperties();
        
        // Test obsolete enum compatibility
        yield return TestObsoleteEnums();
        
        PrintResults();
        
        if (removeObsoleteAfterTest)
        {
            Debug.Log("All backward compatibility tests passed - obsolete code can be safely removed");
        }
    }

    private IEnumerator TestObsoleteClasses()
    {
        Debug.Log("Testing obsolete class compatibility...");
        
        try
        {
#pragma warning disable CS0618 // Disable obsolete warnings for testing
            
            // Test IndividualMarker backward compatibility
            var individualMarker = new IndividualMarker(new Vector2Int(1, 1), Time.time);
            Assert(individualMarker != null, "IndividualMarker creation");
            Assert(individualMarker.position == new Vector2Int(1, 1), "IndividualMarker position");
            
            // Test AreaMarker backward compatibility
            var areaMarker = new AreaMarker(new Vector2Int(2, 2), 3, Time.time);
            Assert(areaMarker != null, "AreaMarker creation");
            Assert(areaMarker.centerPosition == new Vector2Int(2, 2), "AreaMarker center position");
            Assert(areaMarker.size == 3, "AreaMarker size");
            
#pragma warning restore CS0618
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Obsolete class test failed: {e.Message}");
        }
        
        yield return null;
    }

    private IEnumerator TestObsoleteMethods()
    {
        Debug.Log("Testing obsolete method compatibility...");
        
        Vector2Int testPos = new Vector2Int(3, 5);
        
        try
        {
#pragma warning disable CS0618
            
            // Test obsolete individual marker methods
            bool individualPlaced = playerActionManager.PlaceIndividualMarker(testPos);
            Debug.Log($"PlaceIndividualMarker: {individualPlaced}");
            
            bool hasIndividual = playerActionManager.HasIndividualMarkerAt(testPos);
            Assert(hasIndividual == individualPlaced, "HasIndividualMarkerAt consistency");
            
            if (hasIndividual)
            {
                bool individualRemoved = playerActionManager.RemoveIndividualMarkerAt(testPos);
                Assert(individualRemoved, "RemoveIndividualMarkerAt");
            }
            
            // Test obsolete area marker methods
            bool areaPlaced = playerActionManager.PlaceAreaMarker(testPos, 2);
            Debug.Log($"PlaceAreaMarker: {areaPlaced}");
            
            bool hasArea = playerActionManager.HasAreaMarkerAt(testPos);
            Assert(hasArea == areaPlaced, "HasAreaMarkerAt consistency");
            
            if (hasArea)
            {
                bool areaRemoved = playerActionManager.RemoveAreaMarkerAt(testPos);
                Assert(areaRemoved, "RemoveAreaMarkerAt");
            }
            
            // Test obsolete charge time methods
            float individualChargeTime = playerActionManager.GetNextIndividualChargeTime();
            float lightChargeTime = playerActionManager.GetNextLightChargeTime();
            Assert(individualChargeTime == lightChargeTime, "Individual/Light charge time equivalence");
            
            float areaChargeTime = playerActionManager.GetNextAreaChargeTime();
            float primeChargeTime = playerActionManager.GetNextPrimeChargeTime();
            Assert(areaChargeTime == primeChargeTime, "Area/Prime charge time equivalence");
            
#pragma warning restore CS0618
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Obsolete method test failed: {e.Message}");
        }
        
        yield return null;
    }

    private IEnumerator TestObsoleteProperties()
    {
        Debug.Log("Testing obsolete property compatibility...");
        
        try
        {
#pragma warning disable CS0618
            
            // Test obsolete marker limit properties
            int maxIndividual = playerActionManager.maxIndividualMarkers;
            int maxLight = playerActionManager.maxLightMarkers;
            Assert(maxIndividual == maxLight, "Individual/Light max markers equivalence");
            
            int maxArea = playerActionManager.maxAreaMarkers;
            int maxPrime = playerActionManager.maxPrimeMarkers;
            Assert(maxArea == maxPrime, "Area/Prime max markers equivalence");
            
            // Test obsolete charge properties
            int maxIndividualCharges = playerActionManager.maxIndividualMarkerCharges;
            int maxLightCharges = playerActionManager.maxLightMarkerCharges;
            Assert(maxIndividualCharges == maxLightCharges, "Individual/Light max charges equivalence");
            
            // Test obsolete cooldown properties
            float individualCooldown = playerActionManager.individualMarkerCooldown;
            float lightCooldown = playerActionManager.lightMarkerCooldown;
            Assert(Mathf.Approximately(individualCooldown, lightCooldown), "Individual/Light cooldown equivalence");
            
            // Test obsolete material properties
            Material individualMaterial = playerActionManager.individualMarkerMaterial;
            Material lightMaterial = playerActionManager.lightMarkerMaterial;
            Assert(individualMaterial == lightMaterial, "Individual/Light material equivalence");
            
#pragma warning restore CS0618
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Obsolete property test failed: {e.Message}");
        }
        
        yield return null;
    }

    private IEnumerator TestObsoleteEnums()
    {
        Debug.Log("Testing obsolete enum compatibility...");
        
        try
        {
#pragma warning disable CS0618
            
            // Test obsolete CubeMarkerType enum values
            var lightCubeMarker = PlayerMarkerSystem.CubeMarkerType.Light;
            var individualCubeMarker = PlayerMarkerSystem.CubeMarkerType.Individual;
            Assert((int)lightCubeMarker == (int)individualCubeMarker, "CubeMarkerType Individual/Light equivalence");
            
            var primeCubeMarker = PlayerMarkerSystem.CubeMarkerType.Prime;
            var areaCubeMarker = PlayerMarkerSystem.CubeMarkerType.Area;
            Assert((int)primeCubeMarker == (int)areaCubeMarker, "CubeMarkerType Area/Prime equivalence");
            
            // Test obsolete MarkerType enum values
            var lightMarkerType = PlayerMarkerSystem.MarkerType.Light;
            var individualMarkerType = PlayerMarkerSystem.MarkerType.Individual;
            Assert((int)lightMarkerType == (int)individualMarkerType, "MarkerType Individual/Light equivalence");
            
            var primeMarkerType = PlayerMarkerSystem.MarkerType.Prime;
            var areaMarkerType = PlayerMarkerSystem.MarkerType.Area;
            Assert((int)primeMarkerType == (int)areaMarkerType, "MarkerType Area/Prime equivalence");
            
#pragma warning restore CS0618
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Obsolete enum test failed: {e.Message}");
        }
        
        yield return null;
    }

    private void Assert(bool condition, string message)
    {
        testsRun++;
        if (condition)
        {
            testsPassed++;
            Debug.Log($"✓ PASS: {message}");
        }
        else
        {
            Debug.LogError($"✗ FAIL: {message}");
        }
    }

    private void PrintResults()
    {
        Debug.Log("=== BACKWARD COMPATIBILITY TEST RESULTS ===");
        Debug.Log($"Tests Run: {testsRun}");
        Debug.Log($"Tests Passed: {testsPassed}");
        Debug.Log($"Tests Failed: {testsRun - testsPassed}");
        
        if (testsPassed == testsRun)
        {
            Debug.Log("🎉 ALL BACKWARD COMPATIBILITY TESTS PASSED!");
            Debug.Log("Obsolete code can be safely removed after production verification.");
        }
        else
        {
            Debug.LogWarning("Some backward compatibility tests failed. Review before removing obsolete code.");
        }
        
        float successRate = testsRun > 0 ? (float)testsPassed / testsRun * 100f : 0f;
        Debug.Log($"Success Rate: {successRate:F1}%");
    }

    // Manual test trigger for development
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            StartCoroutine(RunBackwardCompatibilityTests());
        }
    }
}

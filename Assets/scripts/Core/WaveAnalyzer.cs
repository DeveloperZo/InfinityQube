using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Analyzes wave data to determine solvability, calculate minimum slack space,
/// and determine optimal marker usage strategies for InfinityQube.
/// </summary>
public class WaveAnalyzer
{
    #region Configuration
    private IQWaveGenerator generator;
    private bool enableDebugLogs = true;
    
    // Game mechanics constants
    private const int PRIME_AREA_SIZE = 3; // 3x3 area for prime cube captures
    private const int INFINITY_CORRUPTION_RADIUS = 1; // Adjacent tiles get corrupted
    private const int UNIT_MARKER_BASE_CHARGE = 3;
    private const int PRIME_MARKER_BASE_CHARGE = 1;
    private const int RECURSION_MARKER_BASE_CHARGE = 1;
    private const float MARKER_COOLDOWN = 1f; // 1 second cooldown between uses
    #endregion
    
    #region Constructor
    public WaveAnalyzer(IQWaveGenerator generator)
    {
        this.generator = generator;
    }
    #endregion
    
    #region Public Analysis Methods
    /// <summary>
    /// Performs comprehensive analysis of a wave
    /// </summary>
    public WaveAnalysisResult AnalyzeWave(WaveData waveData)
    {
        if (waveData == null || waveData.CubesData == null || waveData.CubesData.Count == 0)
        {
            return new WaveAnalysisResult
            {
                isSolvable = false,
                minimumSlackSpace = -1,
                requiredMarkers = 0,
                warnings = new List<string> { "Wave has no cubes to analyze" }
            };
        }
        
        DebugLog("AnalyzeWave", $"Analyzing wave with {waveData.CubesData.Count} cubes");
        
        // Step 1: Analyze cube positions and types
        CubeAnalysis cubeAnalysis = AnalyzeCubePositions(waveData);
        
        // Step 2: Calculate optimal marker strategy
        MarkerStrategy optimalStrategy = CalculateOptimalMarkerStrategy(cubeAnalysis, waveData);
        
        // Step 3: Calculate minimum slack space
        int minSlackSpace = CalculateMinimumSlackSpace(cubeAnalysis, optimalStrategy);
        
        // Step 4: Determine solvability
        bool isSolvable = DetermineWaveSolvability(cubeAnalysis, optimalStrategy, minSlackSpace);
        
        // Create result
        WaveAnalysisResult result = new WaveAnalysisResult
        {
            isSolvable = isSolvable,
            minimumSlackSpace = minSlackSpace,
            requiredMarkers = optimalStrategy.totalMarkersNeeded,
            warnings = GenerateWarnings(cubeAnalysis, optimalStrategy)
        };
        
        DebugLog("AnalyzeWave", $"Analysis complete - Solvable: {isSolvable}, Min Slack: {minSlackSpace}, Markers: {optimalStrategy.totalMarkersNeeded}");
        
        return result;
    }
    #endregion
    
    #region Cube Analysis
    /// <summary>
    /// Analyzes cube positions and interactions
    /// </summary>
    private CubeAnalysis AnalyzeCubePositions(WaveData waveData)
    {
        CubeAnalysis analysis = new CubeAnalysis();
        
        // Count cubes by type
        foreach (var cube in waveData.CubesData)
        {
            switch (cube.type)
            {
                case CubeType.Unit:
                    analysis.unitCubes.Add(cube);
                    break;
                case CubeType.Prime:
                    analysis.primeCubes.Add(cube);
                    break;
                case CubeType.Infinity:
                    analysis.infinityCubes.Add(cube);
                    break;
                case CubeType.Recursion:
                    analysis.recursionCubes.Add(cube);
                    break;
            }
        }
        
        // Analyze prime cube coverage
        analysis.primeCoverageMap = CalculatePrimeCoverage(analysis.primeCubes, waveData);
        
        // Analyze infinity cube danger zones
        analysis.infinityDangerZones = CalculateInfinityDangerZones(analysis.infinityCubes);
        
        // Calculate clearable cubes
        analysis.directlyClearableUnits = CountDirectlyClearableUnits(analysis.unitCubes, analysis.infinityDangerZones);
        analysis.primeClearableUnits = CountPrimeClearableUnits(analysis.unitCubes, analysis.primeCoverageMap);
        
        // Analyze recursion cube requirements
        analysis.recursionCubeInfo = AnalyzeRecursionCubes(analysis.recursionCubes);
        
        DebugLog("AnalyzeCubePositions", 
            $"Units: {analysis.unitCubes.Count}, Prime: {analysis.primeCubes.Count}, " +
            $"Infinity: {analysis.infinityCubes.Count}, Recursion: {analysis.recursionCubes.Count}");
        
        return analysis;
    }
    
    /// <summary>
    /// Calculates 3x3 coverage areas for prime cubes
    /// </summary>
    private Dictionary<Vector2Int, List<CubeData>> CalculatePrimeCoverage(List<CubeData> primeCubes, WaveData waveData)
    {
        Dictionary<Vector2Int, List<CubeData>> coverageMap = new Dictionary<Vector2Int, List<CubeData>>();
        
        foreach (var primeCube in primeCubes)
        {
            // Calculate 3x3 area around prime cube
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int coveragePos = primeCube.position + new Vector2Int(dx, dy);
                    
                    // Find cubes at this position
                    var cubesAtPos = waveData.CubesData.Where(c => c.position == coveragePos).ToList();
                    
                    if (cubesAtPos.Count > 0)
                    {
                        if (!coverageMap.ContainsKey(primeCube.position))
                        {
                            coverageMap[primeCube.position] = new List<CubeData>();
                        }
                        coverageMap[primeCube.position].AddRange(cubesAtPos);
                    }
                }
            }
        }
        
        return coverageMap;
    }
    
    /// <summary>
    /// Calculates danger zones around infinity cubes
    /// </summary>
    private List<Vector2Int> CalculateInfinityDangerZones(List<CubeData> infinityCubes)
    {
        HashSet<Vector2Int> dangerZones = new HashSet<Vector2Int>();
        
        foreach (var infinityCube in infinityCubes)
        {
            // Add the infinity cube position itself
            dangerZones.Add(infinityCube.position);
            
            // Add adjacent positions (4-way adjacency for corruption)
            dangerZones.Add(infinityCube.position + Vector2Int.up);
            dangerZones.Add(infinityCube.position + Vector2Int.down);
            dangerZones.Add(infinityCube.position + Vector2Int.left);
            dangerZones.Add(infinityCube.position + Vector2Int.right);
        }
        
        return dangerZones.ToList();
    }
    
    /// <summary>
    /// Counts unit cubes that can be cleared directly (not in danger zones)
    /// </summary>
    private int CountDirectlyClearableUnits(List<CubeData> unitCubes, List<Vector2Int> dangerZones)
    {
        return unitCubes.Count(cube => !dangerZones.Contains(cube.position));
    }
    
    /// <summary>
    /// Counts unit cubes that can be cleared by prime cube area effects
    /// </summary>
    private int CountPrimeClearableUnits(List<CubeData> unitCubes, Dictionary<Vector2Int, List<CubeData>> primeCoverageMap)
    {
        HashSet<CubeData> clearableUnits = new HashSet<CubeData>();
        
        foreach (var coverage in primeCoverageMap.Values)
        {
            foreach (var cube in coverage)
            {
                if (cube.type == CubeType.Unit)
                {
                    clearableUnits.Add(cube);
                }
            }
        }
        
        return clearableUnits.Count;
    }
    
    /// <summary>
    /// Analyzes recursion cube requirements
    /// </summary>
    private RecursionCubeInfo AnalyzeRecursionCubes(List<CubeData> recursionCubes)
    {
        RecursionCubeInfo info = new RecursionCubeInfo();
        
        foreach (var cube in recursionCubes)
        {
            info.totalHitsRequired += cube.level; // Each level requires one hit
            info.RecursionMarkersRequired += Mathf.CeilToInt(cube.level / 2f); // Recursion markers do 2 damage
        }
        
        return info;
    }
    #endregion
    
    #region Marker Strategy Calculation
    /// <summary>
    /// Calculates the optimal marker usage strategy
    /// </summary>
    private MarkerStrategy CalculateOptimalMarkerStrategy(CubeAnalysis analysis, WaveData waveData)
    {
        MarkerStrategy strategy = new MarkerStrategy();
        
        // Priority 1: Handle infinity cubes (they cause game over if they escape)
        foreach (var infinityCube in analysis.infinityCubes)
        {
            strategy.UnitMarkersForInfinity++;
        }
        
        // Priority 2: Optimize prime cube captures for area clearing
        // Check which prime cubes provide best coverage
        var sortedPrimeCubes = analysis.primeCoverageMap
            .OrderByDescending(kvp => kvp.Value.Count(c => c.type == CubeType.Unit))
            .ToList();
        
        foreach (var primeKvp in sortedPrimeCubes)
        {
            // Check if prime marker is more efficient than individual unit markers
            int unitsCovered = primeKvp.Value.Count(c => c.type == CubeType.Unit);
            if (unitsCovered >= 2) // Prime marker is worth it if it clears 2+ units
            {
                strategy.primeMarkersForArea++;
            }
            else
            {
                // Use unit marker on the prime cube itself
                strategy.UnitMarkersForPrime++;
            }
        }
        
        // Priority 3: Handle recursion cubes
        strategy.RecursionMarkersForRecursion = analysis.recursionCubeInfo.RecursionMarkersRequired;
        
        // Priority 4: Remaining unit cubes
        int unhandledUnits = analysis.unitCubes.Count - analysis.primeClearableUnits;
        strategy.UnitMarkersForUnits = Mathf.Max(0, unhandledUnits);
        
        // Calculate totals
        strategy.totalUnitMarkers = strategy.UnitMarkersForInfinity + 
                                    strategy.UnitMarkersForPrime + 
                                    strategy.UnitMarkersForUnits;
        strategy.totalPrimeMarkers = strategy.primeMarkersForArea;
        strategy.totalRecursionMarkers = strategy.RecursionMarkersForRecursion;
        strategy.totalMarkersNeeded = strategy.totalUnitMarkers + 
                                    strategy.totalPrimeMarkers + 
                                    strategy.totalRecursionMarkers;
        
        DebugLog("CalculateOptimalMarkerStrategy", 
            $"Strategy - Light: {strategy.totalUnitMarkers}, Prime: {strategy.totalPrimeMarkers}, Heavy: {strategy.totalRecursionMarkers}");
        
        return strategy;
    }
    #endregion
    
    #region Slack Space Calculation
    /// <summary>
    /// Calculates minimum slack space required for the wave
    /// </summary>
    private int CalculateMinimumSlackSpace(CubeAnalysis analysis, MarkerStrategy strategy)
    {
        // Slack space = Total cubes that must be prevented from escaping
        int totalCubes = analysis.unitCubes.Count + 
                        analysis.primeCubes.Count + 
                        analysis.infinityCubes.Count + 
                        analysis.recursionCubes.Count;
        
        // Calculate cubes that will be captured
        int capturedCubes = 0;
        
        // Units captured directly or by prime areas
        capturedCubes += analysis.directlyClearableUnits;
        capturedCubes += analysis.primeClearableUnits;
        
        // Prime cubes captured
        capturedCubes += strategy.UnitMarkersForPrime + strategy.primeMarkersForArea;
        
        // Infinity cubes captured (must use markers)
        capturedCubes += strategy.UnitMarkersForInfinity;
        
        // Recursion cubes captured
        capturedCubes += analysis.recursionCubes.Count; // Assuming all are handled
        
        // Minimum slack = cubes that might escape
        int minSlack = totalCubes - capturedCubes;
        
        // Add buffer for timing issues and player mistakes
        minSlack += 2; // POC: Simple buffer, could be more sophisticated
        
        DebugLog("CalculateMinimumSlackSpace", 
            $"Total: {totalCubes}, Captured: {capturedCubes}, Min Slack: {minSlack}");
        
        return Mathf.Max(0, minSlack);
    }
    #endregion
    
    #region Solvability Determination
    /// <summary>
    /// Determines if the wave is solvable with available resources
    /// </summary>
    private bool DetermineWaveSolvability(CubeAnalysis analysis, MarkerStrategy strategy, int minSlackSpace)
    {
        // Check if we have enough marker charges
        int availableUnitCharges = UNIT_MARKER_BASE_CHARGE * 3; // Assume 3 unit markers max
        int availablePrimeCharges = PRIME_MARKER_BASE_CHARGE * 2; // Assume 2 prime markers max
        int availableRecursionCharges = RECURSION_MARKER_BASE_CHARGE * 1; // Assume 1 recursion marker max
        
        if (strategy.totalUnitMarkers > availableUnitCharges)
        {
            DebugLog("DetermineWaveSolvability", "Not enough unit marker charges");
            return false;
        }
        
        if (strategy.totalPrimeMarkers > availablePrimeCharges)
        {
            DebugLog("DetermineWaveSolvability", "Not enough prime marker charges");
            return false;
        }
        
        if (strategy.totalRecursionMarkers > availableRecursionCharges)
        {
            DebugLog("DetermineWaveSolvability", "Not enough recursion marker charges");
            return false;
        }
        
        // Check if infinity cubes can be handled
        if (analysis.infinityCubes.Count > availableUnitCharges)
        {
            DebugLog("DetermineWaveSolvability", "Too many infinity cubes to handle");
            return false;
        }
        
        // Check if slack space is reasonable
        if (minSlackSpace > 10) // POC: Arbitrary limit
        {
            DebugLog("DetermineWaveSolvability", "Slack space requirement too high");
            return false;
        }
        
        return true;
    }
    #endregion
    
    #region Warning Generation
    /// <summary>
    /// Generates warnings about wave difficulty or special conditions
    /// </summary>
    private List<string> GenerateWarnings(CubeAnalysis analysis, MarkerStrategy strategy)
    {
        List<string> warnings = new List<string>();
        
        // Infinity cube warnings
        if (analysis.infinityCubes.Count >= 3)
        {
            warnings.Add($"High infinity cube count ({analysis.infinityCubes.Count}) - requires precise marker usage");
        }
        
        // Recursion cube warnings
        if (analysis.recursionCubeInfo.totalHitsRequired > 6)
        {
            warnings.Add($"High recursion cube durability (total hits: {analysis.recursionCubeInfo.totalHitsRequired})");
        }
        
        // Marker shortage warnings
        if (strategy.totalMarkersNeeded > 6)
        {
            warnings.Add($"High marker requirement ({strategy.totalMarkersNeeded} markers needed)");
        }
        
        // Prime cube clustering
        if (analysis.primeCoverageMap.Count >= 3)
        {
            bool hasOverlap = CheckPrimeCubeOverlap(analysis.primeCubes);
            if (hasOverlap)
            {
                warnings.Add("Prime cubes have overlapping coverage areas - optimize placement");
            }
        }
        
        // Danger zone warnings
        float dangerZonePercent = (float)analysis.infinityDangerZones.Count / (generator.GridManager.Width * generator.GridManager.Height);
        if (dangerZonePercent > 0.2f)
        {
            warnings.Add($"Large infinity danger zones ({dangerZonePercent:P0} of grid affected)");
        }
        
        return warnings;
    }
    
    /// <summary>
    /// Checks if prime cubes have overlapping coverage
    /// </summary>
    private bool CheckPrimeCubeOverlap(List<CubeData> primeCubes)
    {
        for (int i = 0; i < primeCubes.Count; i++)
        {
            for (int j = i + 1; j < primeCubes.Count; j++)
            {
                float distance = Vector2Int.Distance(primeCubes[i].position, primeCubes[j].position);
                if (distance < 3f) // Coverage areas overlap if distance < 3
                {
                    return true;
                }
            }
        }
        return false;
    }
    #endregion
    
    #region Utility Methods
    private void DebugLog(string methodName, string message)
    {
        if (enableDebugLogs && generator != null && generator.EnableDebugLogs)
        {
            Debug.Log($"[WaveAnalyzer] {methodName}: {message}");
        }
    }
    #endregion
    
    #region Helper Classes
    /// <summary>
    /// Contains analyzed cube data
    /// </summary>
    private class CubeAnalysis
    {
        public List<CubeData> unitCubes = new List<CubeData>();
        public List<CubeData> primeCubes = new List<CubeData>();
        public List<CubeData> infinityCubes = new List<CubeData>();
        public List<CubeData> recursionCubes = new List<CubeData>();
        
        public Dictionary<Vector2Int, List<CubeData>> primeCoverageMap = new Dictionary<Vector2Int, List<CubeData>>();
        public List<Vector2Int> infinityDangerZones = new List<Vector2Int>();
        
        public int directlyClearableUnits = 0;
        public int primeClearableUnits = 0;
        
        public RecursionCubeInfo recursionCubeInfo = new RecursionCubeInfo();
    }
    
    /// <summary>
    /// Information about recursion cubes
    /// </summary>
    private class RecursionCubeInfo
    {
        public int totalHitsRequired = 0;
        public int RecursionMarkersRequired = 0;
    }
    
    /// <summary>
    /// Optimal marker usage strategy
    /// </summary>
    private class MarkerStrategy
    {
        // Unit marker allocation
        public int UnitMarkersForUnits = 0;
        public int UnitMarkersForPrime = 0;
        public int UnitMarkersForInfinity = 0;
        
        // Prime marker allocation
        public int primeMarkersForArea = 0;
        
        // Recursion marker allocation
        public int RecursionMarkersForRecursion = 0;
        
        // Totals
        public int totalUnitMarkers = 0;
        public int totalPrimeMarkers = 0;
        public int totalRecursionMarkers = 0;
        public int totalMarkersNeeded = 0;
    }
    #endregion
}

/// <summary>
/// Wave analysis result data
/// </summary>
[System.Serializable]
public class WaveAnalysisResult
{
    public bool isSolvable;
    public int minimumSlackSpace;
    public int requiredMarkers;
    public int difficulty;
    public List<string> warnings = new List<string>();
}

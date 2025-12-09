using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Wave generator manager for InfinityQube that creates procedural waves with
/// configurable parameters, difficulty scaling, and solvability analysis.
/// Follows singleton pattern and integrates with existing manager architecture.
/// </summary>
public class IQWaveGenerator : MonoBehaviour, IManagerDebugInterface
{
    #region Inspector Configuration
    [Header("Wave Generation Configuration")]
    [SerializeField] private WaveGeneratorConfig defaultConfig;
    
    [Header("Generation Constraints")]
    [Range(3, 10)] public int minGridWidth = 3;
    [Range(3, 10)] public int maxGridWidth = 5;
    [Range(9, 20)] public int minGridHeight = 9;
    [Range(9, 20)] public int maxGridHeight = 20;
    
    [Header("Cube Distribution")]
    [Range(0f, 1f)] public float unitCubePercentage = 0.6f;
    [Range(0f, 1f)] public float matrixCubePercentage = 0.2f;
    [Range(0f, 1f)] public float infinityCubePercentage = 0.1f;
    [Range(0f, 1f)] public float recursionCubePercentage = 0.1f;
    
    [Header("Difficulty Scaling")]
    [Range(0.1f, 2f)] public float difficultyMultiplier = 1f;
    [Range(1, 10)] public int baseCubesPerWave = 5;
    [Range(0.5f, 3f)] public float cubeSpacingMin = 1f;
    [Range(1f, 5f)] public float cubeSpacingMax = 2f;
    
    [Header("Pattern Generation")]
    public bool usePatternGeneration = true;
    public List<WavePattern> availablePatterns = new List<WavePattern>();
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool showGenerationGizmos = false;
    public Color gizmoColor = Color.yellow;
    #endregion
    
    #region Runtime State
    private GridManager gridManager;
    private WaveManager waveManager;
    private WaveAnalyzer waveAnalyzer;
    
    private bool isInitialized = false;
    private WaveGeneratorConfig activeConfig;
    private System.Random randomGenerator;
    
    // Generation statistics
    private int totalWavesGenerated = 0;
    private int successfulGenerations = 0;
    private int failedGenerations = 0;
    private float lastGenerationTime = 0f;
    #endregion
    
    #region Properties
    public static IQWaveGenerator Instance { get; private set; }
    public bool IsInitialized => isInitialized && gridManager != null && waveManager != null;
    public WaveGeneratorConfig ActiveConfig => activeConfig;
    public int TotalWavesGenerated => totalWavesGenerated;
    public float SuccessRate => totalWavesGenerated > 0 ? (float)successfulGenerations / totalWavesGenerated : 0f;
    public GridManager GridManager => gridManager;
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
        InitializeRandomGenerator();
    }
    
    private void Start()
    {
        EnableDebugLogs = enableDebugLogs;
        CacheManagerReferences();
        InitializeConfiguration();
        InitializeAnalyzer();
        
        DebugLog("Start", "IQWaveGenerator initialized");
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (showGenerationGizmos && gridManager != null && gridManager.IsGridReady)
        {
            DrawGenerationGizmos();
        }
    }
    #endregion
    
    #region Initialization
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            DebugLog("InitializeSingleton", "Multiple IQWaveGenerators found! Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    
    private void InitializeRandomGenerator()
    {
        int seed = System.DateTime.Now.Millisecond;
        randomGenerator = new System.Random(seed);
        DebugLog("InitializeRandomGenerator", $"Random generator initialized with seed: {seed}");
    }
    
    private void CacheManagerReferences()
    {
        gridManager = GridManager.Instance;
        if (gridManager == null)
        {
            DebugLog("CacheManagerReferences", "GridManager not found - wave generation will be limited");
        }
        
        waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager == null)
        {
            DebugLog("CacheManagerReferences", "WaveManager not found - generation results cannot be played");
        }
        
        isInitialized = gridManager != null;
    }
    
    private void InitializeConfiguration()
    {
        if (defaultConfig != null)
        {
            activeConfig = Instantiate(defaultConfig);
            DebugLog("InitializeConfiguration", "Loaded default configuration");
        }
        else
        {
            activeConfig = ScriptableObject.CreateInstance<WaveGeneratorConfig>();
            ApplyInspectorSettingsToConfig();
            DebugLog("InitializeConfiguration", "Created configuration from inspector settings");
        }
    }
    
    private void InitializeAnalyzer()
    {
        waveAnalyzer = new WaveAnalyzer(this);
        DebugLog("InitializeAnalyzer", "Wave analyzer initialized");
    }
    
    private void ApplyInspectorSettingsToConfig()
    {
        if (activeConfig == null) return;
        
        activeConfig.minGridWidth = minGridWidth;
        activeConfig.maxGridWidth = maxGridWidth;
        activeConfig.minGridHeight = minGridHeight;
        activeConfig.maxGridHeight = maxGridHeight;
        
        activeConfig.unitCubePercentage = unitCubePercentage;
        activeConfig.matrixCubePercentage = matrixCubePercentage;
        activeConfig.infinityCubePercentage = infinityCubePercentage;
        activeConfig.recursionCubePercentage = recursionCubePercentage;
        
        activeConfig.difficultyMultiplier = difficultyMultiplier;
        activeConfig.baseCubesPerWave = baseCubesPerWave;
        activeConfig.cubeSpacingMin = cubeSpacingMin;
        activeConfig.cubeSpacingMax = cubeSpacingMax;
        
        activeConfig.usePatternGeneration = usePatternGeneration;
    }
    #endregion
    
    #region Wave Generation Core
    public WaveData GenerateWave(int difficulty = 1, GenerationStrategy strategy = GenerationStrategy.Random)
    {
        if (!IsInitialized)
        {
            DebugLog("GenerateWave", "Cannot generate wave - manager not initialized");
            return null;
        }
        
        float startTime = Time.realtimeSinceStartup;
        totalWavesGenerated++;
        
        DebugLog("GenerateWave", $"Generating wave {totalWavesGenerated} with difficulty {difficulty}, strategy {strategy}");
        
        WaveData waveData = ScriptableObject.CreateInstance<WaveData>();
        waveData.waveIndex = totalWavesGenerated;
        waveData.spawnWidth = gridManager.Width;
        waveData.spawnHeight = gridManager.Height;
        
        // Calculate wave parameters based on difficulty
        int cubeCount = CalculateCubeCount(difficulty);
        waveData.cubes = new List<CubeData>();
        
        bool success = false;
        
        switch (strategy)
        {
            case GenerationStrategy.Random:
                success = GenerateRandomWave(waveData, cubeCount, difficulty);
                break;
            case GenerationStrategy.Pattern:
                success = GeneratePatternWave(waveData, cubeCount, difficulty);
                break;
            case GenerationStrategy.DifficultyScaled:
                success = GenerateDifficultyScaledWave(waveData, cubeCount, difficulty);
                break;
        }
        
        if (success)
        {
            // Analyze the generated wave
            if (waveAnalyzer != null)
            {
                var analysisResult = waveAnalyzer.AnalyzeWave(waveData);
                
                // Store key analysis results in wave data messages for now
                if (analysisResult != null)
                {
                    // Add analysis info as debug messages
                    if (waveData.messages == null)
                        waveData.messages = new List<WaveMessage>();
                    
                    waveData.messages.Add(new WaveMessage
                    {
                        Message = $"Analysis: Solvable={analysisResult.isSolvable}, MinSlack={analysisResult.minimumSlackSpace}, Markers={analysisResult.requiredMarkers}",
                        AutoHideDelay = 0f,
                        
                    });
                    
                    // Add warnings
                    foreach (var warning in analysisResult.warnings)
                    {
                        waveData.messages.Add(new WaveMessage
                        {
                            Message = $"Warning: {warning}",
                            AutoHideDelay = 0f,
                            
                        });
                    }
                }
            }
            
            successfulGenerations++;
            lastGenerationTime = Time.realtimeSinceStartup - startTime;
            
            DebugLog("GenerateWave", $"Wave {totalWavesGenerated} generated successfully in {lastGenerationTime:F3}s with {cubeCount} cubes");
        }
        else
        {
            failedGenerations++;
            DebugLog("GenerateWave", $"Wave {totalWavesGenerated} generation failed");
            return null;
        }
        
        return waveData;
    }
    
    private int CalculateCubeCount(int difficulty)
    {
        float scaledCount = baseCubesPerWave + (difficulty - 1) * difficultyMultiplier * 2;
        return Mathf.RoundToInt(scaledCount);
    }
    
    private bool GenerateRandomWave(WaveData waveData, int cubeCount, int difficulty)
    {
        List<Vector2Int> usedPositions = new List<Vector2Int>();
        
        for (int i = 0; i < cubeCount; i++)
        {
            CubeData entry = GenerateRandomCubeEntry(usedPositions, difficulty);
            if (entry != null)
            {
                waveData.cubes.Add(entry);
                usedPositions.Add(entry.position);
            }
            else
            {
                DebugLog("GenerateRandomWave", $"Failed to place cube {i + 1}/{cubeCount}");
            }
        }
        
        return waveData.cubes.Count > 0;
    }
    
    private bool GeneratePatternWave(WaveData waveData, int cubeCount, int difficulty)
    {
        if (!usePatternGeneration || availablePatterns.Count == 0)
        {
            return GenerateRandomWave(waveData, cubeCount, difficulty);
        }
        
        // Select a random pattern
        WavePattern pattern = availablePatterns[randomGenerator.Next(availablePatterns.Count)];
        
        // Apply pattern with variations based on difficulty
        // POC: Simple pattern application - expand later
        return ApplyPattern(waveData, pattern, cubeCount, difficulty);
    }
    
    private bool GenerateDifficultyScaledWave(WaveData waveData, int cubeCount, int difficulty)
    {
        // Adjust cube type distribution based on difficulty
        float unitPercent = Mathf.Lerp(0.8f, 0.4f, (difficulty - 1) / 10f);
        float specialPercent = 1f - unitPercent;
        
        // Temporarily adjust percentages based on difficulty
        float originalUnit = unitCubePercentage;
        float originalMatrix = matrixCubePercentage;
        float originalInfinity = infinityCubePercentage;
        float originalRecursion = recursionCubePercentage;
        
        // Scale special cube percentages based on difficulty
        unitCubePercentage = unitPercent;
        float specialTotal = originalMatrix + originalInfinity + originalRecursion;
        if (specialTotal > 0)
        {
            matrixCubePercentage = (originalMatrix / specialTotal) * specialPercent;
            infinityCubePercentage = (originalInfinity / specialTotal) * specialPercent;
            recursionCubePercentage = (originalRecursion / specialTotal) * specialPercent;
        }
        
        // Use balanced generation with adjusted percentages
        bool success = GenerateBalancedWave(waveData, cubeCount, difficulty);
        
        // Restore original percentages
        unitCubePercentage = originalUnit;
        matrixCubePercentage = originalMatrix;
        infinityCubePercentage = originalInfinity;
        recursionCubePercentage = originalRecursion;
        
        return success;
    }
    #endregion
    
    #region Cube Generation Helpers
    private CubeData GenerateRandomCubeEntry(List<Vector2Int> usedPositions, int difficulty)
    {
        CubeType type = DetermineCubeType();
        return GenerateCubeEntry(type, usedPositions, difficulty);
    }
    
    private CubeData GenerateCubeEntry(CubeType type, List<Vector2Int> usedPositions, int difficulty)
    {
        Vector2Int position = FindValidPosition(usedPositions, difficulty);
        if (position.x < 0) return null;
        
        CubeData entry = new CubeData
        {
            type = type,
            position = position,
            level = Mathf.Max(1, difficulty / 3) // Scale level with difficulty
        };
        
        return entry;
    }
    
    private CubeType DetermineCubeType()
    {
        float rand = (float)randomGenerator.NextDouble();
        float cumulative = 0f;
        
        cumulative += unitCubePercentage;
        if (rand < cumulative) return CubeType.Unit;
        
        cumulative += matrixCubePercentage;
        if (rand < cumulative) return CubeType.Matrix;
        
        cumulative += infinityCubePercentage;
        if (rand < cumulative) return CubeType.Infinity;
        
        return CubeType.Recursion;
    }
    
    private CubeType DetermineCubeTypeByDifficulty(float normalPercent, float specialPercent)
    {
        float rand = (float)randomGenerator.NextDouble();
        
        if (rand < normalPercent)
        {
            return CubeType.Unit;
        }
        
        // Distribute special cubes based on their relative percentages
        float specialRand = (float)randomGenerator.NextDouble();
        float totalSpecial = matrixCubePercentage + infinityCubePercentage + recursionCubePercentage;
        
        if (totalSpecial <= 0) return CubeType.Unit;
        
        float matrixChance = matrixCubePercentage / totalSpecial;
        float infinityChance = infinityCubePercentage / totalSpecial;
        
        if (specialRand < matrixChance) return CubeType.Matrix;
        if (specialRand < matrixChance + infinityChance) return CubeType.Infinity;
        
        return CubeType.Recursion;
    }
    
    private Vector2Int FindValidPosition(List<Vector2Int> usedPositions, int difficulty)
    {
        if (gridManager == null || !gridManager.IsGridReady)
            return new Vector2Int(-1, -1);
        
        int maxAttempts = 50;
        float minSpacing = Mathf.Lerp(cubeSpacingMax, cubeSpacingMin, (difficulty - 1) / 10f);
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            int x = randomGenerator.Next(0, gridManager.Width);
            int y = randomGenerator.Next(gridManager.Height / 2, gridManager.Height);
            
            Vector2Int pos = new Vector2Int(x, y);
            
            if (IsPositionValid(pos, usedPositions, minSpacing))
            {
                return pos;
            }
        }
        
        // Fallback: find any valid position
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = gridManager.Height / 2; y < gridManager.Height; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!usedPositions.Contains(pos))
                {
                    return pos;
                }
            }
        }
        
        return new Vector2Int(-1, -1);
    }
    
    private bool IsPositionValid(Vector2Int pos, List<Vector2Int> usedPositions, float minSpacing)
    {
        if (usedPositions.Contains(pos)) return false;
        
        // Check grid bounds
        if (!gridManager.IsValidGridPosition(pos)) return false;
        
        // Check spacing from other cubes
        foreach (var usedPos in usedPositions)
        {
            float distance = Vector2Int.Distance(pos, usedPos);
            if (distance < minSpacing)
            {
                return false;
            }
        }
        
        return true;
    }
    
    private float CalculateSpawnTime(int cubeIndex, int difficulty)
    {
        float baseDelay = 0.5f;
        float delayReduction = Mathf.Lerp(0f, 0.3f, (difficulty - 1) / 10f);
        return cubeIndex * (baseDelay - delayReduction);
    }
    
    private float CalculateFallSpeed(CubeType type, int difficulty)
    {
        float baseSpeed = 1f;
        float speedIncrease = Mathf.Lerp(0f, 0.5f, (difficulty - 1) / 10f);
        
        // Adjust speed based on cube type
        switch (type)
        {
            case CubeType.Infinity:
                return (baseSpeed + speedIncrease) * 1.2f; // Infinity cubes fall slightly faster
            case CubeType.Recursion:
                return (baseSpeed + speedIncrease) * 0.8f; // Recursion cubes fall slightly slower
            default:
                return baseSpeed + speedIncrease;
        }
    }
    
    private bool ApplyPattern(WaveData waveData, WavePattern pattern, int cubeCount, int difficulty)
    {
        // POC: Pattern-based generation with variations
        if (pattern == null || pattern.entries.Count == 0)
        {
            return GenerateRandomWave(waveData, cubeCount, difficulty);
        }
        
        List<Vector2Int> usedPositions = new List<Vector2Int>();
        
        // Calculate pattern center point
        int centerX = gridManager.Width / 2;
        int centerY = gridManager.Height - (gridManager.Height / 4); // Upper portion of grid
        
        // Apply pattern entries with random variations
        foreach (var entry in pattern.entries)
        {
            Vector2Int basePos = new Vector2Int(centerX, centerY) + entry.relativePosition;
            
            // Add random variation based on difficulty
            int variance = Mathf.Max(1, 5 - difficulty / 2);
            basePos.x += randomGenerator.Next(-variance, variance + 1);
            basePos.y += randomGenerator.Next(-variance / 2, variance / 2 + 1);
            
            // Ensure within bounds
            basePos.x = Mathf.Clamp(basePos.x, 0, gridManager.Width - 1);
            basePos.y = Mathf.Clamp(basePos.y, gridManager.Height / 2, gridManager.Height - 1);
            
            if (!usedPositions.Contains(basePos))
            {
                CubeData cube = new CubeData
                {
                    type = entry.cubeType,
                    position = basePos,
                    level = Mathf.Max(1, difficulty / 3)
                };
                
                waveData.cubes.Add(cube);
                usedPositions.Add(basePos);
            }
        }
        
        // Fill remaining cube count with random cubes
        int remaining = cubeCount - waveData.cubes.Count;
        for (int i = 0; i < remaining; i++)
        {
            CubeData entry = GenerateRandomCubeEntry(usedPositions, difficulty);
            if (entry != null)
            {
                waveData.cubes.Add(entry);
                usedPositions.Add(entry.position);
            }
        }
        
        return waveData.cubes.Count > 0;
    }
    #endregion
    
    #region Placement Rules and Validation
    /// <summary>
    /// Validates cube placement considering special cube rules
    /// </summary>
    private bool ValidateCubePlacement(CubeType type, Vector2Int position, List<CubeData> existingCubes)
    {
        switch (type)
        {
            case CubeType.Infinity:
                return ValidateInfinityCubePlacement(position, existingCubes);
            case CubeType.Matrix:
                return ValidateMatrixCubePlacement(position, existingCubes);
            case CubeType.Recursion:
                return ValidateRecursionCubePlacement(position, existingCubes);
            default:
                return true; // Unit cubes have no special placement rules
        }
    }
    
    /// <summary>
    /// Ensures infinity cubes aren't too close to each other (creates danger zones)
    /// </summary>
    private bool ValidateInfinityCubePlacement(Vector2Int position, List<CubeData> existingCubes)
    {
        float minInfinitySpacing = 3f; // Infinity cubes need more space
        
        foreach (var cube in existingCubes)
        {
            if (cube.type == CubeType.Infinity)
            {
                float distance = Vector2Int.Distance(position, cube.position);
                if (distance < minInfinitySpacing)
                {
                    DebugLog("ValidateInfinityCubePlacement", $"Infinity cube too close to another at {cube.position}");
                    return false;
                }
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Validates matrix cube placement considering area effect overlaps
    /// </summary>
    private bool ValidateMatrixCubePlacement(Vector2Int position, List<CubeData> existingCubes)
    {
        // Matrix cubes should have some spacing to avoid overlapping area effects
        float minMatrixSpacing = 2.5f;
        int nearbyMatrixCount = 0;
        
        foreach (var cube in existingCubes)
        {
            if (cube.type == CubeType.Matrix)
            {
                float distance = Vector2Int.Distance(position, cube.position);
                if (distance < minMatrixSpacing)
                {
                    nearbyMatrixCount++;
                }
            }
        }
        
        // Allow some clustering but not too much
        return nearbyMatrixCount < 2;
    }
    
    /// <summary>
    /// Validates recursion cube placement
    /// </summary>
    private bool ValidateRecursionCubePlacement(Vector2Int position, List<CubeData> existingCubes)
    {
        // Recursion cubes can be placed more freely but avoid stacking
        foreach (var cube in existingCubes)
        {
            if (cube.type == CubeType.Recursion && cube.position == position)
            {
                return false; // No stacking recursion cubes
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Calculates danger zones around infinity cubes
    /// </summary>
    private List<Vector2Int> CalculateInfinityDangerZones(List<CubeData> cubes)
    {
        List<Vector2Int> dangerZones = new List<Vector2Int>();
        
        foreach (var cube in cubes)
        {
            if (cube.type == CubeType.Infinity)
            {
                // Add 3x3 area around infinity cube as danger zone
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        Vector2Int dangerPos = cube.position + new Vector2Int(dx, dy);
                        if (gridManager.IsValidGridPosition(dangerPos) && !dangerZones.Contains(dangerPos))
                        {
                            dangerZones.Add(dangerPos);
                        }
                    }
                }
            }
        }
        
        return dangerZones;
    }
    
    /// <summary>
    /// Detects clustering of advantage cubes (Matrix cubes)
    /// </summary>
    private bool DetectAdvantageCubeClustering(List<CubeData> cubes)
    {
        int clusterThreshold = 3; // More than 3 matrix cubes in close proximity
        float clusterRadius = 2.5f;
        
        foreach (var cube in cubes)
        {
            if (cube.type == CubeType.Matrix)
            {
                int nearbyMatrixs = 0;
                foreach (var other in cubes)
                {
                    if (other != cube && other.type == CubeType.Matrix)
                    {
                        float distance = Vector2Int.Distance(cube.position, other.position);
                        if (distance <= clusterRadius)
                        {
                            nearbyMatrixs++;
                        }
                    }
                }
                
                if (nearbyMatrixs >= clusterThreshold)
                {
                    DebugLog("DetectAdvantageCubeClustering", $"Detected matrix cube cluster at {cube.position}");
                    return true;
                }
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Validates the entire wave for minimum solvability
    /// </summary>
    private bool ValidateWaveSolvability(WaveData waveData)
    {
        // Basic solvability checks
        if (waveData.cubes.Count == 0)
        {
            DebugLog("ValidateWaveSolvability", "Wave has no cubes");
            return false;
        }
        
        // Check for impossible configurations
        int infinityCount = 0;
        int totalCubes = waveData.cubes.Count;
        
        foreach (var cube in waveData.cubes)
        {
            if (cube.type == CubeType.Infinity)
                infinityCount++;
        }
        
        // If more than 50% are infinity cubes, wave might be too difficult
        if (infinityCount > totalCubes * 0.5f)
        {
            DebugLog("ValidateWaveSolvability", "Too many infinity cubes for wave size");
            return false;
        }
        
        // Check for clustering issues
        if (DetectAdvantageCubeClustering(waveData.cubes))
        {
            // Clustering is okay but log it
            DebugLog("ValidateWaveSolvability", "Wave contains matrix cube clusters");
        }
        
        return true;
    }
    #endregion
    
    #region Enhanced Generation Methods
    /// <summary>
    /// Generates a balanced wave with proper cube distribution
    /// </summary>
    public bool GenerateBalancedWave(WaveData waveData, int cubeCount, int difficulty)
    {
        List<Vector2Int> usedPositions = new List<Vector2Int>();
        
        // Calculate cube type distribution based on percentages
        int unitCount = Mathf.RoundToInt(cubeCount * unitCubePercentage);
        int matrixCount = Mathf.RoundToInt(cubeCount * matrixCubePercentage);
        int infinityCount = Mathf.RoundToInt(cubeCount * infinityCubePercentage);
        int recursionCount = cubeCount - unitCount - matrixCount - infinityCount;
        
        // Place special cubes first (they have more constraints)
        bool success = true;
        success &= PlaceCubesOfType(waveData, CubeType.Infinity, infinityCount, usedPositions, difficulty);
        success &= PlaceCubesOfType(waveData, CubeType.Matrix, matrixCount, usedPositions, difficulty);
        success &= PlaceCubesOfType(waveData, CubeType.Recursion, recursionCount, usedPositions, difficulty);
        success &= PlaceCubesOfType(waveData, CubeType.Unit, unitCount, usedPositions, difficulty);
        
        return success && ValidateWaveSolvability(waveData);
    }
    
    /// <summary>
    /// Places specific number of cubes of a given type
    /// </summary>
    private bool PlaceCubesOfType(WaveData waveData, CubeType type, int count, List<Vector2Int> usedPositions, int difficulty)
    {
        int placed = 0;
        int maxAttempts = count * 10;
        int attempts = 0;
        
        while (placed < count && attempts < maxAttempts)
        {
            attempts++;
            Vector2Int position = FindValidPosition(usedPositions, difficulty);
            
            if (position.x >= 0)
            {
                // Additional validation for special cube types
                if (ValidateCubePlacement(type, position, waveData.cubes))
                {
                    CubeData cube = new CubeData
                    {
                        type = type,
                        position = position,
                        level = Mathf.Max(1, difficulty / 3)
                    };
                    
                    waveData.cubes.Add(cube);
                    usedPositions.Add(position);
                    placed++;
                }
            }
        }
        
        if (placed < count)
        {
            DebugLog("PlaceCubesOfType", $"Could only place {placed}/{count} {type} cubes");
        }
        
        return placed > 0;
    }
    #endregion
    
    #region Public API
    public List<WaveData> GenerateBatch(int count, int startDifficulty = 1, GenerationStrategy strategy = GenerationStrategy.Random)
    {
        DebugLog("GenerateBatch", $"Generating batch of {count} waves starting at difficulty {startDifficulty}");
        
        List<WaveData> batch = new List<WaveData>();
        
        for (int i = 0; i < count; i++)
        {
            int difficulty = startDifficulty + (i / 3); // Increase difficulty every 3 waves
            WaveData wave = GenerateWave(difficulty, strategy);
            
            if (wave != null)
            {
                batch.Add(wave);
            }
        }
        
        DebugLog("GenerateBatch", $"Batch generation complete: {batch.Count}/{count} waves generated successfully");
        return batch;
    }
    
    public void UpdateConfiguration(WaveGeneratorConfig newConfig)
    {
        if (newConfig == null) return;
        
        activeConfig = Instantiate(newConfig);
        ApplyConfigToInspector();
        
        DebugLog("UpdateConfiguration", "Configuration updated");
    }
    
    private void ApplyConfigToInspector()
    {
        if (activeConfig == null) return;
        
        minGridWidth = activeConfig.minGridWidth;
        maxGridWidth = activeConfig.maxGridWidth;
        minGridHeight = activeConfig.minGridHeight;
        maxGridHeight = activeConfig.maxGridHeight;
        
        unitCubePercentage = activeConfig.unitCubePercentage;
        matrixCubePercentage = activeConfig.matrixCubePercentage;
        infinityCubePercentage = activeConfig.infinityCubePercentage;
        recursionCubePercentage = activeConfig.recursionCubePercentage;
        
        difficultyMultiplier = activeConfig.difficultyMultiplier;
        baseCubesPerWave = activeConfig.baseCubesPerWave;
        cubeSpacingMin = activeConfig.cubeSpacingMin;
        cubeSpacingMax = activeConfig.cubeSpacingMax;
        
        usePatternGeneration = activeConfig.usePatternGeneration;
    }
    #endregion
    
    #region Debug Visualization
    private void DrawGenerationGizmos()
    {
        Gizmos.color = gizmoColor;
        
        // Draw generation bounds
        if (gridManager != null)
        {
            Vector3 center = gridManager.GridCenter;
            Vector3 size = new Vector3(
                gridManager.Width * gridManager.TileSize,
                10f,
                gridManager.Height * gridManager.TileSize
            );
            
            Gizmos.DrawWireCube(center + Vector3.up * 5f, size);
        }
    }
    #endregion
    
    #region IManagerDebugInterface Implementation
    public bool EnableDebugLogs { get; set; }
    
    public string GetDebugStatus()
    {
        string status = IsInitialized ? "READY" : "NOT_READY";
        return $"WaveGen: {status} | Generated: {totalWavesGenerated} | Success: {SuccessRate:P0} | LastTime: {lastGenerationTime:F3}s";
    }
    
    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Status"] = IsInitialized ? "Ready" : "Not Initialized",
            ["Total Waves Generated"] = totalWavesGenerated,
            ["Successful Generations"] = successfulGenerations,
            ["Failed Generations"] = failedGenerations,
            ["Success Rate"] = $"{SuccessRate:P0}",
            ["Last Generation Time"] = $"{lastGenerationTime:F3}s",
            ["Active Config"] = activeConfig != null ? activeConfig.name : "None",
            ["Grid Manager"] = gridManager != null ? "Connected" : "Missing",
            ["Wave Manager"] = waveManager != null ? "Connected" : "Missing",
            ["Analyzer"] = waveAnalyzer != null ? "Ready" : "Not Initialized",
            ["Pattern Generation"] = usePatternGeneration ? "Enabled" : "Disabled",
            ["Available Patterns"] = availablePatterns.Count,
            ["Difficulty Multiplier"] = difficultyMultiplier,
            ["Base Cubes Per Wave"] = baseCubesPerWave,
            ["Cube Distribution"] = $"Unit:{unitCubePercentage:P0} Matrix:{matrixCubePercentage:P0} Infinity:{infinityCubePercentage:P0} Recursion:{recursionCubePercentage:P0}"
        };
    }
    
    public void ResetToDefaults()
    {
        DebugLog("ResetToDefaults", "Resetting wave generator to defaults");
        
        // Reset statistics
        totalWavesGenerated = 0;
        successfulGenerations = 0;
        failedGenerations = 0;
        lastGenerationTime = 0f;
        
        // Reset configuration
        if (defaultConfig != null)
        {
            activeConfig = Instantiate(defaultConfig);
            ApplyConfigToInspector();
        }
        else
        {
            // Reset to hardcoded defaults
            minGridWidth = 3;
            maxGridWidth = 5;
            minGridHeight = 9;
            maxGridHeight = 20;
            
            unitCubePercentage = 0.6f;
            matrixCubePercentage = 0.2f;
            infinityCubePercentage = 0.1f;
            recursionCubePercentage = 0.1f;
            
            difficultyMultiplier = 1f;
            baseCubesPerWave = 5;
            cubeSpacingMin = 1f;
            cubeSpacingMax = 2f;
            
            usePatternGeneration = true;
            
            ApplyInspectorSettingsToConfig();
        }
        
        // Re-initialize random generator
        InitializeRandomGenerator();
    }
    
    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading from file or resources
        DebugLog("LoadConfiguration", $"Loading configuration: {configName} (not yet implemented)");
    }
    
    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving
        DebugLog("SaveConfiguration", $"Saving configuration: {configName} (not yet implemented)");
    }
    #endregion
    
    #region Utility Methods
    private void DebugLog(string methodName, string message)
    {
        if (EnableDebugLogs)
        {
            Debug.Log($"[IQWaveGenerator] {methodName}: {message}");
        }
    }
    #endregion
}

#region Supporting Classes
/// <summary>
/// Configuration data for wave generation parameters
/// </summary>
[System.Serializable]
[CreateAssetMenu(fileName = "WaveGeneratorConfig", menuName = "InfinityQube/Wave Generator Config")]
public class WaveGeneratorConfig : ScriptableObject
{
    [Header("Grid Constraints")]
    public int minGridWidth = 3;
    public int maxGridWidth = 5;
    public int minGridHeight = 9;
    public int maxGridHeight = 20;
    
    [Header("Cube Distribution")]
    public float unitCubePercentage = 0.6f;
    public float matrixCubePercentage = 0.2f;
    public float infinityCubePercentage = 0.1f;
    public float recursionCubePercentage = 0.1f;
    
    [Header("Difficulty")]
    public float difficultyMultiplier = 1f;
    public int baseCubesPerWave = 5;
    public float cubeSpacingMin = 1f;
    public float cubeSpacingMax = 2f;
    
    [Header("Patterns")]
    public bool usePatternGeneration = true;
}

/// <summary>
/// Wave pattern definition for pattern-based generation
/// </summary>
[System.Serializable]
public class WavePattern
{
    public string patternName;
    public List<PatternEntry> entries = new List<PatternEntry>();
    public int minDifficulty = 1;
    public int maxDifficulty = 10;
}

/// <summary>
/// Single entry in a wave pattern
/// </summary>
[System.Serializable]
public class PatternEntry
{
    public Vector2Int relativePosition;
    public CubeType cubeType;
    public float timeOffset;
}

/// <summary>
/// Generation strategies for wave creation
/// </summary>
public enum GenerationStrategy
{
    Random,
    Pattern,
    DifficultyScaled
}

// WaveAnalyzer has been moved to its own file: WaveAnalyzer.cs
#endregion

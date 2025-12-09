using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Manages score accumulation during gameplay and calculates final grades.
/// Subscribes to game events to track captures, escapes, markers, and deaths.
/// 
/// Usage:
/// - ScoreManager.Instance.CurrentWaveScore (live score during wave)
/// - ScoreManager.Instance.CalculateStageResult() (at stage end)
/// </summary>
public class ScoreManager : MonoBehaviour, IManagerDebugInterface
{
    #region Singleton
    
    private static ScoreManager _instance;
    public static ScoreManager Instance => _instance;
    public static bool IsInitialized => _instance != null;
    
    #endregion
    
    #region Inspector Configuration
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region Runtime State
    
    private WaveScoreData _currentWaveScore = new WaveScoreData();
    private List<WaveScoreData> _waveScores = new List<WaveScoreData>();
    private int _currentStageIndex = -1;
    private string _currentStageName = "";
    private int _stageMaxMoves = 20;
    
    #endregion
    
    #region Properties
    
    /// <summary>Current wave's live score data.</summary>
    public WaveScoreData CurrentWaveScore => _currentWaveScore;
    
    /// <summary>All completed wave scores for current stage.</summary>
    public IReadOnlyList<WaveScoreData> WaveScores => _waveScores;
    
    /// <summary>Running total of base score across all waves.</summary>
    public int RunningBaseScore
    {
        get
        {
            int total = _currentWaveScore.CalculateBaseScore();
            foreach (var wave in _waveScores)
            {
                total += wave.CalculateBaseScore();
            }
            return total;
        }
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void OnEnable()
    {
        // Subscribe to game events
        GameEvents.OnStageStart += HandleStageStart;
        GameEvents.OnWaveStart += HandleWaveStart;
        GameEvents.OnWaveComplete += HandleWaveComplete;
        GameEvents.OnCubeCaptured += HandleCubeCapture;
        GameEvents.OnCubeEscaped += HandleCubeEscape;
        
        DebugLog("Subscribed to game events");
    }
    
    private void OnDisable()
    {
        GameEvents.OnStageStart -= HandleStageStart;
        GameEvents.OnWaveStart -= HandleWaveStart;
        GameEvents.OnWaveComplete -= HandleWaveComplete;
        GameEvents.OnCubeCaptured -= HandleCubeCapture;
        GameEvents.OnCubeEscaped -= HandleCubeEscape;
    }
    
    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }
    
    #endregion
    
    #region Event Handlers
    
    private void HandleStageStart(int stageIndex, StageData stageData)
    {
        _currentStageIndex = stageIndex;
        _waveScores.Clear();
        _currentWaveScore.Reset();
        
        // Get stage info from event data
        if (stageData != null)
        {
            _currentStageName = stageData.stageName;
            _stageMaxMoves = stageData.gridHeight;
        }
        else
        {
            _currentStageName = $"Stage {stageIndex}";
            _stageMaxMoves = 20;
        }
        
        DebugLog($"Stage {stageIndex} ({_currentStageName}) started. Max moves: {_stageMaxMoves}");
    }
    
    private void HandleWaveStart(int waveIndex, WaveData waveData)
    {
        _currentWaveScore.Reset();
        _currentWaveScore.maxPossibleMoves = _stageMaxMoves;
        
        // Track total cubes in this wave for scoring
        if (waveData != null && waveData.cubes != null)
        {
            _currentWaveScore.totalCubesInWave = waveData.cubes.Count;
        }
        
        DebugLog($"Wave {waveIndex} started (cubes: {_currentWaveScore.totalCubesInWave})");
    }
    
    private void HandleWaveComplete(int waveIndex)
    {
        // Record final move count
        var waveManager = Object.FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
        {
            _currentWaveScore.RecordMove(waveManager.MoveStep);
        }
        
        // Store completed wave score
        var completedWave = new WaveScoreData();
        CopyWaveScore(_currentWaveScore, completedWave);
        _waveScores.Add(completedWave);
        
        int waveScore = completedWave.CalculateBaseScore();
        DebugLog($"Wave {waveIndex} complete. Wave score: {waveScore}, Moves: {completedWave.movesUsed}/{completedWave.maxPossibleMoves}");
    }
    
    private void HandleCubeCapture(Vector2Int position, CubeType cubeType)
    {
        _currentWaveScore.RecordCapture(cubeType);
        DebugLog($"Capture: {cubeType} at {position}. Running score: {RunningBaseScore}");
    }
    
    private void HandleCubeEscape(Vector2Int position, CubeType cubeType)
    {
        _currentWaveScore.RecordEscape(cubeType);
        
        string penalty = cubeType != CubeType.Infinity ? $" (-{ScoreConstants.PENALTY_ESCAPE})" : " (no penalty)";
        DebugLog($"Escape: {cubeType} at {position}{penalty}. Running score: {RunningBaseScore}");
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Record a marker placement (call from PlayerMarkerSystem).
    /// </summary>
    public void RecordMarkerPlaced()
    {
        _currentWaveScore.RecordMarkerPlaced();
    }
    
    /// <summary>
    /// Record a player death (call from PlayerManager).
    /// </summary>
    public void RecordPlayerDeath()
    {
        _currentWaveScore.RecordDeath();
        DebugLog("Player death recorded");
    }
    
    /// <summary>
    /// Update move count (call from WaveManager each move step).
    /// </summary>
    public void RecordMoveStep(int moveStep)
    {
        _currentWaveScore.RecordMove(moveStep);
    }
    
    /// <summary>
    /// Calculate final stage score result. Call at stage completion.
    /// </summary>
    public StageScoreResult CalculateStageResult(int baseShards = 100)
    {
        var result = new StageScoreResult
        {
            stageIndex = _currentStageIndex,
            stageName = _currentStageName,
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            waveCount = _waveScores.Count
        };
        
        // Aggregate stats from all waves
        int totalCaptures = 0;
        int totalPenalizedEscapes = 0;
        int totalEscapes = 0;
        int totalMoves = 0;
        int totalMaxMoves = 0;
        int totalMarkers = 0;
        int totalDeaths = 0;
        int baseScore = 0;
        
        foreach (var wave in _waveScores)
        {
            totalCaptures += wave.TotalCubesCaptured;
            totalPenalizedEscapes += wave.PenalizedEscapes;
            totalEscapes += wave.PenalizedEscapes + wave.infinityEscapes;
            totalMoves += wave.movesUsed;
            totalMaxMoves += wave.maxPossibleMoves;
            totalMarkers += wave.markersPlaced;
            totalDeaths += wave.playerDeaths;
            baseScore += wave.CalculateBaseScore();
        }
        
        result.totalCubesCaptured = totalCaptures;
        result.totalPenalizedEscapes = totalPenalizedEscapes;
        result.totalEscapes = totalEscapes;
        result.totalMovesUsed = totalMoves;
        result.totalMarkersPlaced = totalMarkers;
        result.totalDeaths = totalDeaths;
        result.baseScore = baseScore;
        
        // Calculate move efficiency (1.0 to 1.3)
        // Faster clear = higher multiplier. Clear at move 0 = 1.3x, clear at max moves = 1.0x
        float moveRatio = totalMaxMoves > 0 ? (float)totalMoves / totalMaxMoves : 1f;
        result.moveEfficiency = 1f + (ScoreConstants.MOVE_EFFICIENCY_MAX_BONUS * (1f - moveRatio));
        result.moveEfficiency = Mathf.Clamp(result.moveEfficiency, 1f, 1f + ScoreConstants.MOVE_EFFICIENCY_MAX_BONUS);
        
        // Calculate bonuses
        result.noDeathBonus = totalDeaths == 0 ? ScoreConstants.BONUS_NO_DEATH : 0;
        result.noEscapeBonus = totalPenalizedEscapes == 0 ? ScoreConstants.BONUS_NO_ESCAPE : 0;
        
        // Calculate final score: Base × MoveEfficiency + Bonuses
        // Note: Marker efficiency intentionally NOT scored (stage-controlled, not player-controlled)
        float multipliedScore = baseScore * result.moveEfficiency;
        result.finalScore = Mathf.RoundToInt(multipliedScore) + result.noDeathBonus + result.noEscapeBonus;
        
        // Calculate max possible score (perfect run)
        result.maxPossibleScore = CalculateMaxPossibleScore();
        
        // Determine grade
        result.gradePercentage = result.maxPossibleScore > 0 
            ? (float)result.finalScore / result.maxPossibleScore * 100f 
            : 100f;
        result.grade = ScoreConstants.GetGrade(result.gradePercentage / 100f);
        
        // Calculate shard reward
        result.shardMultiplier = ScoreConstants.GetShardMultiplier(result.grade);
        result.baseShards = baseShards;
        result.finalShards = Mathf.RoundToInt(baseShards * result.shardMultiplier);
        
        DebugLog($"Stage result: {result.grade} ({result.gradePercentage:F0}%) - Score: {result.finalScore}/{result.maxPossibleScore} - Shards: {result.finalShards}");
        
        return result;
    }
    
    /// <summary>
    /// Calculate maximum possible score for current stage (perfect run).
    /// Perfect = all cubes captured, no escapes, fastest clear, no deaths.
    /// </summary>
    private int CalculateMaxPossibleScore()
    {
        // Calculate based on what cubes existed in the stage
        int totalCubesInStage = 0;
        int perfectBaseScore = 0;
        
        foreach (var wave in _waveScores)
        {
            // Count all cubes that existed (captured + escaped)
            int waveTotal = wave.TotalCubesCaptured + wave.PenalizedEscapes + wave.infinityEscapes;
            totalCubesInStage += waveTotal;
            
            // Perfect base = all captured, weighted by what was actually captured
            // (approximation: use actual capture distribution as estimate)
            perfectBaseScore += wave.unitCubesCaptured * ScoreConstants.POINTS_UNIT;
            perfectBaseScore += wave.matrixCubesCaptured * ScoreConstants.POINTS_MATRIX;
            perfectBaseScore += wave.recursionCubesCaptured * ScoreConstants.POINTS_RECURSION;
            perfectBaseScore += wave.infinityCubesCaptured * ScoreConstants.POINTS_INFINITY;
            
            // Add back escaped cubes at average value (15 pts)
            perfectBaseScore += (wave.PenalizedEscapes + wave.infinityEscapes) * 15;
        }
        
        // Perfect run: base × max move efficiency + both bonuses
        float perfectMultiplied = perfectBaseScore * (1f + ScoreConstants.MOVE_EFFICIENCY_MAX_BONUS);
        
        return Mathf.RoundToInt(perfectMultiplied) + ScoreConstants.BONUS_NO_DEATH + ScoreConstants.BONUS_NO_ESCAPE;
    }
    
    /// <summary>
    /// Reset score tracking (call when returning to hub).
    /// </summary>
    public void ResetScores()
    {
        _currentWaveScore.Reset();
        _waveScores.Clear();
        _currentStageIndex = -1;
        _currentStageName = "";
    }
    
    #endregion
    
    #region Utility
    
    private void CopyWaveScore(WaveScoreData source, WaveScoreData dest)
    {
        dest.unitCubesCaptured = source.unitCubesCaptured;
        dest.matrixCubesCaptured = source.matrixCubesCaptured;
        dest.recursionCubesCaptured = source.recursionCubesCaptured;
        dest.infinityCubesCaptured = source.infinityCubesCaptured;
        dest.unitEscapes = source.unitEscapes;
        dest.matrixEscapes = source.matrixEscapes;
        dest.recursionEscapes = source.recursionEscapes;
        dest.infinityEscapes = source.infinityEscapes;
        dest.movesUsed = source.movesUsed;
        dest.maxPossibleMoves = source.maxPossibleMoves;
        dest.markersPlaced = source.markersPlaced;
        dest.playerDeaths = source.playerDeaths;
    }
    
    #endregion
    
    #region IManagerDebugInterface
    
    public bool EnableDebugLogs
    {
        get => enableDebugLogs;
        set => enableDebugLogs = value;
    }
    
    public string GetDebugStatus()
    {
        return $"Score: {RunningBaseScore} | Waves: {_waveScores.Count} | Deaths: {_currentWaveScore.playerDeaths}";
    }
    
    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Stage"] = _currentStageIndex,
            ["Running Score"] = RunningBaseScore,
            ["Waves Completed"] = _waveScores.Count,
            ["Current Wave Captures"] = _currentWaveScore.TotalCubesCaptured,
            ["Current Wave Escapes"] = _currentWaveScore.PenalizedEscapes,
            ["Current Wave Markers"] = _currentWaveScore.markersPlaced,
            ["Total Deaths"] = _currentWaveScore.playerDeaths
        };
    }
    
    public void ResetToDefaults()
    {
        ResetScores();
    }
    
    public void LoadConfiguration(string configName) { }
    public void SaveConfiguration(string configName) { }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[ScoreManager] {message}");
        }
    }
    
    #endregion
}


using TMPro;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Displays player score information during gameplay.
/// Shows grade (C/B/A/S) and numeric score, starting at C and progressing upward.
/// Calculates score directly from PlayerManager statistics.
/// </summary>
public class PlayerUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI playerLevel;
    [SerializeField] private TextMeshProUGUI scoreGradeText;
    [SerializeField] private TextMeshProUGUI scoreNumericText;
    
    [Header("Score Display Settings")]
    [Tooltip("Show numeric score alongside grade")]
    [SerializeField] private bool showNumericScore = true;
    [Tooltip("Color for each grade")]
    [SerializeField] private Color gradeCColor = new Color(1f, 0.3f, 0.3f); // Red
    [SerializeField] private Color gradeBColor = new Color(1f, 0.8f, 0.2f); // Yellow
    [SerializeField] private Color gradeAColor = new Color(0.2f, 0.6f, 1f); // Blue
    [SerializeField] private Color gradeSColor = new Color(1f, 0.8f, 0f); // Gold
    
    [Header("References")]
    [SerializeField] private PlayerManager playerManager;
    private ScoreManager scoreManager;
    private WaveManager waveManager;
    
    private int lastScore = -1;
    private ScoreGrade lastGrade = ScoreGrade.C;
    
    void Start()
    {
        playerManager = FindFirstObjectByType<PlayerManager>();
        scoreManager = ScoreManager.Instance;
        waveManager = FindFirstObjectByType<WaveManager>();
        
        // Subscribe to stage events to reset display on new stage
        GameEvents.OnStageStart += HandleStageStart;
        
        // Initialize display to C grade (starting state)
        ResetDisplay();
    }
    
    void OnDestroy()
    {
        GameEvents.OnStageStart -= HandleStageStart;
    }
    
    private void HandleStageStart(int stageIndex, StageData stageData)
    {
        // Reset display when new stage starts (statistics are reset by PlayerManager)
        ResetDisplay();
    }
    
    private void ResetDisplay()
    {
        if (scoreGradeText != null)
        {
            scoreGradeText.text = "C";
            scoreGradeText.color = gradeCColor;
        }
        if (scoreNumericText != null && showNumericScore)
        {
            scoreNumericText.text = "0";
        }
        
        // Force first update
        lastScore = -1;
        lastGrade = ScoreGrade.C;
        UpdateDisplay();
    }

    void Update()
    {
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        // Ensure references are available
        if (scoreManager == null)
        {
            scoreManager = ScoreManager.Instance;
        }
        if (waveManager == null)
        {
            waveManager = FindFirstObjectByType<WaveManager>();
        }
        
        // Get current score from ScoreManager
        int currentScore = CalculateScore();
        
        // Calculate grade based on current score
        ScoreGrade currentGrade = CalculateCurrentGrade(currentScore);
        
        // Update grade display if changed
        if (scoreGradeText != null && (currentGrade != lastGrade || currentScore != lastScore))
        {
            scoreGradeText.text = currentGrade.ToString();
            scoreGradeText.color = GetGradeColor(currentGrade);
            lastGrade = currentGrade;
        }
        
        // Update numeric score display
        if (scoreNumericText != null && showNumericScore)
        {
            if (currentScore != lastScore)
            {
                scoreNumericText.text = currentScore.ToString();
                lastScore = currentScore;
            }
        }
    }
    
    /// <summary>
    /// Calculates current score using ScoreManager.
    /// Formula: Base Score × Move Efficiency + Marker Bonus
    /// </summary>
    private int CalculateScore()
    {
        if (scoreManager == null || !ScoreManager.IsInitialized) return 0;
        
        // 1. Base score (captures by type - escape penalties)
        int baseScore = scoreManager.CalculateCurrentScore();
        
        // 2. Move efficiency (if wave active)
        float moveEfficiency = 1.0f;
        if (waveManager != null && waveManager.waveActive)
        {
            moveEfficiency = scoreManager.CalculateCurrentMoveEfficiency();
        }
        
        // 3. Marker efficiency bonus
        int markerBonus = scoreManager.CalculateMarkerEfficiencyBonus();
        
        // Simplified real-time score: Base × MoveEfficiency + MarkerBonus
        return Mathf.RoundToInt(baseScore * moveEfficiency) + markerBonus;
    }
    
    /// <summary>
    /// Calculates current grade based on score.
    /// Estimates max possible score based on current wave progress and completed waves.
    /// </summary>
    private ScoreGrade CalculateCurrentGrade(int currentScore)
    {
        if (currentScore <= 0) return ScoreGrade.C;
        
        // Estimate max possible score based on current progress
        int estimatedMaxScore = EstimateMaxPossibleScore();
        
        if (estimatedMaxScore <= 0)
        {
            // Fallback to fixed thresholds if we can't estimate
            if (currentScore >= 200) return ScoreGrade.S;
            if (currentScore >= 100) return ScoreGrade.A;
            if (currentScore >= 50) return ScoreGrade.B;
            return ScoreGrade.C;
        }
        
        // Calculate percentage of max score
        float percentage = (float)currentScore / estimatedMaxScore;
        
        // Use ScoreConstants thresholds
        return ScoreConstants.GetGrade(percentage);
    }
    
    /// <summary>
    /// Estimates max possible score based on current wave progress.
    /// </summary>
    private int EstimateMaxPossibleScore()
    {
        if (scoreManager == null || !ScoreManager.IsInitialized) return 0;
        
        int estimatedMax = 0;
        
        // Get completed waves' max scores
        var completedWaves = scoreManager.WaveScores;
        if (completedWaves != null)
        {
            foreach (var wave in completedWaves)
            {
                // Estimate: perfect capture of all cubes with max efficiency
                int waveMaxScore = EstimateWaveMaxScore(wave);
                estimatedMax += waveMaxScore;
            }
        }
        
        // Estimate current wave max score
        var currentWave = scoreManager.CurrentWaveScore;
        if (currentWave != null && currentWave.totalCubesInWave > 0)
        {
            int currentWaveMaxScore = EstimateWaveMaxScore(currentWave);
            estimatedMax += currentWaveMaxScore;
        }
        
        return estimatedMax;
    }
    
    /// <summary>
    /// Estimates max score for a wave (perfect run).
    /// </summary>
    private int EstimateWaveMaxScore(WaveScoreData wave)
    {
        // Perfect run: all cubes captured, no escapes, max efficiency
        // Estimate based on total cubes in wave (assume average Unit cube value for simplicity)
        int estimatedCaptures = wave.totalCubesInWave;
        if (estimatedCaptures == 0)
        {
            // Fallback: use actual captures if total not available
            estimatedCaptures = wave.TotalCubesCaptured + wave.PenalizedEscapes;
        }
        
        // Conservative estimate: assume all Unit cubes (10 pts each)
        int baseScore = estimatedCaptures * ScoreConstants.POINTS_UNIT;
        float maxEfficiency = 1f + ScoreConstants.MOVE_EFFICIENCY_MAX_BONUS; // 1.3x
        int markerBonus = estimatedCaptures * 5; // Max marker bonus (one per capture)
        
        return Mathf.RoundToInt(baseScore * maxEfficiency) + markerBonus;
    }
    
    /// <summary>
    /// Gets color for a grade.
    /// </summary>
    private Color GetGradeColor(ScoreGrade grade)
    {
        return grade switch
        {
            ScoreGrade.S => gradeSColor,
            ScoreGrade.A => gradeAColor,
            ScoreGrade.B => gradeBColor,
            ScoreGrade.C => gradeCColor,
            _ => gradeCColor
        };
    }
}
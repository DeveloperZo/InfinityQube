using System;
using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Runtime score tracking for a single wave.
/// Accumulates during gameplay, finalized at wave end.
/// </summary>
[Serializable]
public class WaveScoreData
{
    #region Cube Captures (Accumulated)
    
    public int unitCubesCaptured = 0;
    public int matrixCubesCaptured = 0;
    public int recursionCubesCaptured = 0;
    public int infinityCubesCaptured = 0;
    
    public int TotalCubesCaptured => unitCubesCaptured + matrixCubesCaptured + 
                                      recursionCubesCaptured + infinityCubesCaptured;
    
    #endregion
    
    #region Escapes (Penalties)
    
    public int unitEscapes = 0;
    public int matrixEscapes = 0;
    public int recursionEscapes = 0;
    public int infinityEscapes = 0; // Not penalized
    
    /// <summary>Non-Infinity escapes (penalized)</summary>
    public int PenalizedEscapes => unitEscapes + matrixEscapes + recursionEscapes;
    
    #endregion
    
    #region Efficiency Tracking
    
    public int movesUsed = 0;
    public int maxPossibleMoves = 20; // Set from grid height
    public int markersPlaced = 0;
    public int playerDeaths = 0;
    
    #endregion
    
    #region Score Calculation
    
    /// <summary>
    /// Calculate base score from captures (before multipliers).
    /// </summary>
    public int CalculateBaseScore()
    {
        int score = 0;
        score += unitCubesCaptured * ScoreConstants.POINTS_UNIT;
        score += matrixCubesCaptured * ScoreConstants.POINTS_MATRIX;
        score += recursionCubesCaptured * ScoreConstants.POINTS_RECURSION;
        score += infinityCubesCaptured * ScoreConstants.POINTS_INFINITY;
        
        // Subtract escape penalties (non-Infinity only)
        score -= PenalizedEscapes * ScoreConstants.PENALTY_ESCAPE;
        
        return Mathf.Max(0, score); // Don't go negative
    }
    
    #endregion
    
    #region Recording Methods
    
    public void RecordCapture(CubeType type)
    {
        switch (type)
        {
            case CubeType.Unit: unitCubesCaptured++; break;
            case CubeType.Matrix: matrixCubesCaptured++; break;
            case CubeType.Recursion: recursionCubesCaptured++; break;
            case CubeType.Infinity: infinityCubesCaptured++; break;
        }
    }
    
    public void RecordEscape(CubeType type)
    {
        switch (type)
        {
            case CubeType.Unit: unitEscapes++; break;
            case CubeType.Matrix: matrixEscapes++; break;
            case CubeType.Recursion: recursionEscapes++; break;
            case CubeType.Infinity: infinityEscapes++; break;
        }
    }
    
    public void RecordMarkerPlaced()
    {
        markersPlaced++;
    }
    
    public void RecordDeath()
    {
        playerDeaths++;
    }
    
    public void RecordMove(int currentMoveStep)
    {
        movesUsed = currentMoveStep;
    }
    
    #endregion
    
    #region Reset
    
    public void Reset()
    {
        unitCubesCaptured = matrixCubesCaptured = recursionCubesCaptured = infinityCubesCaptured = 0;
        unitEscapes = matrixEscapes = recursionEscapes = infinityEscapes = 0;
        movesUsed = markersPlaced = playerDeaths = 0;
    }
    
    #endregion
}

/// <summary>
/// Final score result for a completed stage.
/// </summary>
[Serializable]
public class StageScoreResult
{
    #region Identity
    
    public int stageIndex;
    public string stageName;
    public string timestamp;
    
    #endregion
    
    #region Raw Stats
    
    public int totalCubesCaptured;
    public int totalEscapes;
    public int totalPenalizedEscapes;
    public int totalMovesUsed;
    public int totalMarkersPlaced;
    public int totalDeaths;
    public int waveCount;
    
    #endregion
    
    #region Calculated Scores
    
    public int baseScore;
    public float moveEfficiency;      // 1.0 - 1.3 (faster = higher)
    public int noDeathBonus;
    public int noEscapeBonus;
    public int finalScore;
    public int maxPossibleScore;
    
    #endregion
    
    #region Grade
    
    public ScoreGrade grade;
    public float gradePercentage;
    public float shardMultiplier;
    public int baseShards;
    public int finalShards;
    
    #endregion
    
    #region Breakdown (for UI display)
    
    public List<string> GetScoreBreakdown()
    {
        var lines = new List<string>();
        
        lines.Add($"Cubes Captured: {totalCubesCaptured}");
        if (totalPenalizedEscapes > 0)
            lines.Add($"Escapes: -{totalPenalizedEscapes} × {ScoreConstants.PENALTY_ESCAPE}");
        lines.Add($"Base Score: {baseScore}");
        lines.Add($"");
        lines.Add($"Move Efficiency: ×{moveEfficiency:F2}");
        if (noDeathBonus > 0)
            lines.Add($"No Death Bonus: +{noDeathBonus}");
        if (noEscapeBonus > 0)
            lines.Add($"No Escape Bonus: +{noEscapeBonus}");
        lines.Add($"");
        lines.Add($"Final Score: {finalScore}");
        lines.Add($"Grade: {grade} ({gradePercentage:F0}%)");
        
        return lines;
    }
    
    #endregion
}

/// <summary>
/// Score grade levels.
/// </summary>
public enum ScoreGrade
{
    C,  // <50%
    B,  // 50-69%
    A,  // 70-89%
    S   // 90%+
}

/// <summary>
/// Score calculation constants (easily tunable).
/// 
/// Design Philosophy:
/// - Player controls execution (captures, speed, survival)
/// - Stage designer controls difficulty ceiling (marker economy)
/// - Marker efficiency intentionally NOT scored (stage-controlled, not player-controlled)
/// </summary>
public static class ScoreConstants
{
    // Points per cube type
    public const int POINTS_UNIT = 10;
    public const int POINTS_MATRIX = 15;
    public const int POINTS_RECURSION = 20;
    public const int POINTS_INFINITY = 25;
    
    // Penalties (non-Infinity escapes only)
    public const int PENALTY_ESCAPE = 15;
    
    // Bonuses
    public const int BONUS_NO_DEATH = 50;
    public const int BONUS_NO_ESCAPE = 30;
    
    // Move efficiency: faster clear = higher multiplier
    // Clear at move 0 = 1.3x, clear at max moves = 1.0x
    public const float MOVE_EFFICIENCY_MAX_BONUS = 0.3f;
    
    // Grade thresholds (percentage of max score)
    public const float GRADE_S_THRESHOLD = 0.90f;
    public const float GRADE_A_THRESHOLD = 0.70f;
    public const float GRADE_B_THRESHOLD = 0.50f;
    
    // Shard multipliers per grade
    public const float SHARD_MULTIPLIER_S = 1.50f;
    public const float SHARD_MULTIPLIER_A = 1.25f;
    public const float SHARD_MULTIPLIER_B = 1.00f;
    public const float SHARD_MULTIPLIER_C = 0.75f;
    
    /// <summary>
    /// Get shard multiplier for a grade.
    /// </summary>
    public static float GetShardMultiplier(ScoreGrade grade)
    {
        return grade switch
        {
            ScoreGrade.S => SHARD_MULTIPLIER_S,
            ScoreGrade.A => SHARD_MULTIPLIER_A,
            ScoreGrade.B => SHARD_MULTIPLIER_B,
            ScoreGrade.C => SHARD_MULTIPLIER_C,
            _ => SHARD_MULTIPLIER_B
        };
    }
    
    /// <summary>
    /// Get grade from percentage.
    /// </summary>
    public static ScoreGrade GetGrade(float percentage)
    {
        if (percentage >= GRADE_S_THRESHOLD) return ScoreGrade.S;
        if (percentage >= GRADE_A_THRESHOLD) return ScoreGrade.A;
        if (percentage >= GRADE_B_THRESHOLD) return ScoreGrade.B;
        return ScoreGrade.C;
    }
}


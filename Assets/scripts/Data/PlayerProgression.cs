using System;
using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Serializable data structure for player progression.
/// Used by SaveManager for persistence.
/// Steam Cloud Ready: Uses simple serializable types.
/// </summary>
[Serializable]
public class PlayerProgression
{
    #region Constants
    
    /// <summary>
    /// Attunement slot indices matching MarkerMode values.
    /// Unit (0) has no attunements but slot exists for index alignment.
    /// </summary>
    public const int SLOT_UNIT = 0;      // No attunements (placeholder)
    public const int SLOT_MATRIX = 1;    // MarkerMode.Matrix - 1
    public const int SLOT_RECURSION = 2; // MarkerMode.Recursion - 1  
    public const int SLOT_INFINITY = 3;  // MarkerMode.Infinity - 1
    
    #endregion
    
    #region Currency
    
    /// <summary>
    /// Primary progression currency earned from wave completions.
    /// </summary>
    public int axiomShards = 0;
    
    #endregion
    
    #region Attunements
    
    /// <summary>
    /// List of attunement IDs that have been permanently unlocked.
    /// </summary>
    public List<string> unlockedAttunements = new List<string>();
    
    /// <summary>
    /// Currently equipped attunement ID per marker type.
    /// Index: 0=Unit (always empty), 1=Matrix, 2=Recursion, 3=Infinity
    /// Empty string means no attunement equipped (default behavior).
    /// </summary>
    public List<string> equippedAttunements = new List<string> { "", "", "", "" };
    
    #endregion
    
    #region Stage Progress
    
    /// <summary>
    /// Stage indices that have been cleared at least once.
    /// Used for first-clear bonus and replay detection.
    /// </summary>
    public List<int> clearedStageIndices = new List<int>();
    
    /// <summary>
    /// Highest stage index the player has unlocked.
    /// </summary>
    public int highestStageUnlocked = 0;
    
    #endregion
    
    #region Hub Progress
    
    /// <summary>
    /// Whether the Resonance Alignment Chamber is unlocked.
    /// Unlocks after Stage 3 completion.
    /// </summary>
    public bool resonanceAlignmentUnlocked = false;
    
    /// <summary>
    /// Whether the Observation Chronicle is unlocked.
    /// Unlocks after Stage 3 completion.
    /// </summary>
    public bool observationChronicleUnlocked = false;
    
    #endregion
    
    #region Lifetime Statistics
    
    /// <summary>
    /// Total cubes captured across all playthroughs.
    /// </summary>
    public int lifetimeCubesCaptured = 0;
    
    /// <summary>
    /// Total cubes escaped across all playthroughs.
    /// </summary>
    public int lifetimeCubesEscaped = 0;
    
    /// <summary>
    /// Total play time in seconds.
    /// </summary>
    public float lifetimePlayTimeSeconds = 0f;
    
    /// <summary>
    /// Total stages completed (cumulative, includes replays).
    /// </summary>
    public int lifetimeStagesCompleted = 0;
    
    /// <summary>
    /// Total waves completed (cumulative, includes replays).
    /// </summary>
    public int lifetimeWavesCompleted = 0;
    
    /// <summary>
    /// Best completion time per stage (stage index → seconds).
    /// </summary>
    public List<StageBestTime> stageBestTimes = new List<StageBestTime>();
    
    #endregion
    
    #region Stage Methods
    
    /// <summary>
    /// Check if a stage has been cleared before.
    /// </summary>
    public bool HasClearedStage(int stageIndex)
    {
        return clearedStageIndices.Contains(stageIndex);
    }
    
    /// <summary>
    /// Mark a stage as cleared and update progression.
    /// </summary>
    public void MarkStageCleared(int stageIndex)
    {
        if (!clearedStageIndices.Contains(stageIndex))
        {
            clearedStageIndices.Add(stageIndex);
        }
        
        if (stageIndex > highestStageUnlocked)
        {
            highestStageUnlocked = stageIndex;
        }
        
        // Unlock hub areas after Stage 3
        if (stageIndex >= 3)
        {
            resonanceAlignmentUnlocked = true;
            observationChronicleUnlocked = true;
        }
    }
    
    #endregion
    
    #region Attunement Methods
    
    /// <summary>
    /// Get the equipped attunement ID for a marker mode.
    /// </summary>
    public string GetEquippedAttunement(MarkerMode mode)
    {
        int index = GetSlotIndex(mode);
        if (index >= 0 && index < equippedAttunements.Count)
        {
            return equippedAttunements[index];
        }
        return "";
    }
    
    /// <summary>
    /// Set the equipped attunement for a marker mode.
    /// </summary>
    public void SetEquippedAttunement(MarkerMode mode, string attunementId)
    {
        int index = GetSlotIndex(mode);
        if (index >= 0 && index < equippedAttunements.Count)
        {
            equippedAttunements[index] = attunementId ?? "";
        }
    }
    
    /// <summary>
    /// Check if an attunement is unlocked.
    /// </summary>
    public bool IsAttunementUnlocked(string attunementId)
    {
        return unlockedAttunements.Contains(attunementId);
    }
    
    /// <summary>
    /// Unlock an attunement permanently.
    /// </summary>
    public bool UnlockAttunement(string attunementId)
    {
        if (string.IsNullOrEmpty(attunementId)) return false;
        
        if (!unlockedAttunements.Contains(attunementId))
        {
            unlockedAttunements.Add(attunementId);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Convert MarkerMode to slot index.
    /// MarkerMode values are 1-4, slots are 0-3.
    /// </summary>
    private int GetSlotIndex(MarkerMode mode)
    {
        return (int)mode - 1;
    }
    
    #endregion
    
    #region Currency Methods
    
    /// <summary>
    /// Add Axiom Shards to the player's total.
    /// </summary>
    public void AddShards(int amount)
    {
        if (amount > 0)
        {
            axiomShards += amount;
        }
    }
    
    /// <summary>
    /// Try to spend Axiom Shards. Returns true if successful.
    /// </summary>
    public bool TrySpendShards(int amount)
    {
        if (amount <= 0) return false;
        
        if (axiomShards >= amount)
        {
            axiomShards -= amount;
            return true;
        }
        return false;
    }
    
    #endregion
    
    #region Lifetime Statistics Methods
    
    /// <summary>
    /// Record cubes captured in a session.
    /// </summary>
    public void RecordCubesCaptured(int count)
    {
        lifetimeCubesCaptured += count;
    }
    
    /// <summary>
    /// Record cubes escaped in a session.
    /// </summary>
    public void RecordCubesEscaped(int count)
    {
        lifetimeCubesEscaped += count;
    }
    
    /// <summary>
    /// Add play time from a session.
    /// </summary>
    public void AddPlayTime(float seconds)
    {
        lifetimePlayTimeSeconds += seconds;
    }
    
    /// <summary>
    /// Record a stage completion and check for best time.
    /// </summary>
    public bool RecordStageCompletion(int stageIndex, float completionTimeSeconds)
    {
        lifetimeStagesCompleted++;
        
        // Check/update best time
        var existing = stageBestTimes.Find(s => s.stageIndex == stageIndex);
        if (existing != null)
        {
            if (completionTimeSeconds < existing.bestTimeSeconds)
            {
                existing.bestTimeSeconds = completionTimeSeconds;
                existing.achievedDate = System.DateTime.Now.ToString("yyyy-MM-dd");
                return true; // New best time!
            }
        }
        else
        {
            stageBestTimes.Add(new StageBestTime
            {
                stageIndex = stageIndex,
                bestTimeSeconds = completionTimeSeconds,
                achievedDate = System.DateTime.Now.ToString("yyyy-MM-dd")
            });
            return true; // First completion
        }
        
        return false;
    }
    
    /// <summary>
    /// Get best time for a stage, or -1 if not completed.
    /// </summary>
    public float GetStageBestTime(int stageIndex)
    {
        var entry = stageBestTimes.Find(s => s.stageIndex == stageIndex);
        return entry?.bestTimeSeconds ?? -1f;
    }
    
    /// <summary>
    /// Record a wave completion.
    /// </summary>
    public void RecordWaveCompletion()
    {
        lifetimeWavesCompleted++;
    }
    
    /// <summary>
    /// Get formatted lifetime play time (e.g., "2h 30m").
    /// </summary>
    public string GetFormattedPlayTime()
    {
        int totalMinutes = (int)(lifetimePlayTimeSeconds / 60);
        int hours = totalMinutes / 60;
        int minutes = totalMinutes % 60;
        
        if (hours > 0)
            return $"{hours}h {minutes}m";
        else
            return $"{minutes}m";
    }
    
    #endregion
}

/// <summary>
/// Best completion time for a stage.
/// </summary>
[System.Serializable]
public class StageBestTime
{
    public int stageIndex;
    public float bestTimeSeconds;
    public string achievedDate;
}

/// <summary>
/// Root save data container with metadata.
/// </summary>
[Serializable]
public class SaveData
{
    #region Metadata
    
    /// <summary>
    /// Save format version for migration support.
    /// Increment when changing save structure.
    /// </summary>
    public int saveVersion = 1;
    
    /// <summary>
    /// ISO 8601 timestamp of last save.
    /// </summary>
    public string lastSaveTime;
    
    /// <summary>
    /// Platform identifier for debugging cross-platform issues.
    /// </summary>
    public string platform;
    
    #endregion
    
    #region Progression Data
    
    /// <summary>
    /// Player's progression data (currency, unlocks, equipped items).
    /// </summary>
    public PlayerProgression progression = new PlayerProgression();
    
    #endregion
}


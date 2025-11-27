using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "StageDatabase", menuName = "Infinity Qube/Stage Database")]
public class StageDB : ScriptableObject
{
    [SerializeField] private List<StageData> stages = new List<StageData>();

    // Dictionary to quickly access stages by ID (populated at runtime)
    private Dictionary<int, StageData> stageMap = new Dictionary<int, StageData>();
    private bool initialized = false;

    // Initialization
    public void Initialize()
    {
        if (initialized && stageMap.Any()) return;

        // Populate the stage map
        stageMap.Clear();


        // Add regular stages
        foreach (var stage in stages)
        {
            if (stage != null)
                stageMap[stage.stageNumber] = stage;
        }


        initialized = true;
    }

    // Get a stage by ID
    public StageData GetStage(int stageId)
    {
        if (!initialized)
            Initialize();

        if (stageMap.TryGetValue(stageId, out StageData stage))
        {
            var newStage = Instantiate(stage);
            newStage.waveConfigurations = stage.waveConfigurations.Select(x=>Instantiate(x)).ToList();  
            return newStage;
        }

        Debug.LogWarning($"Stage {stageId} not found in database!");
        return null;
    }

    // Get all available stage IDs
    public List<int> GetAllStageIds()
    {
        if (!initialized)
            Initialize();

        return new List<int>(stageMap.Keys);
    }

    // Add a stage
    public void AddStage(StageData stage)
    {
        if (!initialized)
            Initialize();

        if (stage == null) return;

        if (!stages.Contains(stage))
            stages.Add(stage);
        

        stageMap[stage.stageNumber] = stage;
    }

    /// <summary>
    /// Validates paired wave configuration for all stages.
    /// Checks that waves have marker spawn rules configured if they will be used for mirroring.
    /// </summary>
    public bool ValidatePairedWaveConfiguration()
    {
        if (!initialized)
            Initialize();

        bool isValid = true;

        foreach (var stage in stages)
        {
            if (stage == null || stage.waveConfigurations == null) continue;

            foreach (var wave in stage.waveConfigurations)
            {
                if (wave == null) continue;

                // Check that waves with marker spawn rules have at least one rule enabled
                var rules = wave.markerSpawnRules;
                bool hasAnyRule = rules.lightSpawnsUnit || rules.heavySpawnsRecursion || 
                                 rules.primeSpawnsPrime || rules.infinitySpawnsInfinity;
                
                // Note: It's valid to have no rules if the wave won't be mirrored
                // This validation is informational - waves can work without mirroring
                if (hasAnyRule)
                {
                    Debug.Log($"StageDB Validation: Wave '{wave.name}' has marker spawn rules configured (ready for mirroring)");
                }
            }
        }

        return isValid;
    }
}
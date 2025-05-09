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
}
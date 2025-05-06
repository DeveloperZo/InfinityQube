using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Stage", menuName = "Infinity Qube/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage Identity")]
    public int stageNumber;
    public string stageName;
    [TextArea(3, 5)]
    public string description;
    [TextArea(2, 3)]
    public string objective;

    [Header("Grid Configuration")]
    public int gridWidth = 6;
    public int gridHeight = 10;
    public Vector2Int playerStartPosition = new Vector2Int(2, 0);

    [Header("Wave Configuration")]
    public List<WaveData> waveConfigurations = new List<WaveData>();

    [Header("Settings")]
    public List<StageMessage> messages = new List<StageMessage>();
    public bool limitMarkers = false;
    public int maxMarkers = 2;
   

    [Header("Success Conditions")]
    public bool requireAllCubesDestroyed = false;
    public int requiredCaptureCount = 0;
    public int maxAllowedEscapes = 0;

    [Header("Player Statistics")]
    public StageStatistics playerStatistics;
}
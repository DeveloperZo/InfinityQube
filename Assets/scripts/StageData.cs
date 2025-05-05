using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class StageData
{
    public int stageNumber;
    public string stageName;
    public string description;
    public string objective;

    // Grid configuration
    public int gridWidth = 6;
    public int gridHeight = 10;

    // Wave settings
    public int waveSize = 1;  // Number of rows in each wave
    public int waveCount = 1; // Number of waves

    // Cube type spawning chances - sums should add up to 1.0
    public float normalCubeChance = 1.0f;  // Default to all normal cubes
    public float greenCubeChance = 0.0f;
    public float blackCubeChance = 0.0f;

    // Tutorial settings
    public bool isTutorial = false;
    public List<string> tutorialMessages = new List<string>();
    public bool limitMarkers = false;
    public int maxMarkers = 2;

    // Success conditions
    public bool requireAllCubesDestroyed = false;
    public int requiredCaptureCount = 0;
    public int maxAllowedEscapes = 0;

    // Specific cube placements for tutorials
    public List<CubePlacement> specificCubePlacements = new List<CubePlacement>();

    // Player starting position
    public Vector2Int playerStartPosition = new Vector2Int(2, 0);

    [System.Serializable]
    public class CubePlacement
    {
        public Enumerations.CubeType cubeType;
        public Vector2Int position;
        public int waveIndex = 0; // Which wave it belongs to
    }

    // Constructor for Tutorial -1: "Capture the Cube"
    public static StageData CreateTutorialMinus1()
    {
        StageData data = new StageData
        {
            stageNumber = -1,
            stageName = "First Steps",
            description = "Learn how to capture a cube before it falls off the edge.",
            objective = "Use a marker to capture the cube before it falls off the grid.",

            gridWidth = 5,
            gridHeight = 6,

            waveSize = 1,
            waveCount = 1,

            normalCubeChance = 1.0f,
            greenCubeChance = 0.0f,
            blackCubeChance = 0.0f,

            isTutorial = true,
            limitMarkers = true,
            maxMarkers = 1,

            requiredCaptureCount = 1,
            maxAllowedEscapes = 0,

            playerStartPosition = new Vector2Int(2, 0)
        };

        // Add tutorial messages
        data.tutorialMessages.Add("Welcome to Infinity Cube! Use the arrow keys to move the selector.");
        data.tutorialMessages.Add("Press SPACE to place a marker on a tile. The cube will be captured when it crosses the marked tile.");
        data.tutorialMessages.Add("Try to capture the gray cube before it falls off the edge!");

        // Add specific cube placement for the tutorial
        data.specificCubePlacements.Add(new CubePlacement
        {
            cubeType = Enumerations.CubeType.Normal,
            position = new Vector2Int(2, 5),
            waveIndex = 0
        });

        return data;
    }
}
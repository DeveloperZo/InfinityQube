using UnityEngine;

[System.Serializable]
public class PlayerStatistics
{
    [Header("Cube Statistics")]
    public int normalCubesCaptured;
    public int blueCubesCaptured;
    public int blackCubesCaptured;
    public int cubesEscaped;

    [Header("Action Statistics")]
    public int markersPlaced;
    public int markersTriggered;
    public int detonationsUsed;
    public int movesCount;

    [Header("Tile Statistics")]
    public int tilesCorrupted;
    public int tilesEnhanced;

    [Header("Player Statistics")]
    public int playerDeaths;
    public float timeAlive;
    public float totalPlayTime;

    [Header("Derived Statistics")]
    public float captureRate;
    public float deathRate;
    public float markersPerMove;

    // Helper properties
    public int TotalCubesCaptured => normalCubesCaptured + blueCubesCaptured + blackCubesCaptured;
    public int TotalCubesInteracted => TotalCubesCaptured + cubesEscaped;
    public float AverageTimeAlivePerLife => playerDeaths > 0 ? timeAlive / playerDeaths : timeAlive;
    public float MarkersTriggeredRate => markersPlaced > 0 ? (float)markersTriggered / markersPlaced : 0f;

    public override string ToString()
    {
        return $"Cubes: {TotalCubesCaptured} captured, {cubesEscaped} escaped | " +
               $"Markers: {markersPlaced} placed, {markersTriggered} triggered | " +
               $"Deaths: {playerDeaths} | Moves: {movesCount} | " +
               $"Time: {totalPlayTime:F1}s alive";
    }
}
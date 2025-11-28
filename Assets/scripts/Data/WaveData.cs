using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Wave", menuName = "Infinity Qube/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Wave Identity")]
    public int Index = 0;
    public int GridHeight = 3;
    public int GridWidth = 3;

    [Header("Cube Configuration")]
    public List<CubeData> CubesData = new List<CubeData>();

    [Header("Marker Settings")]
    [Tooltip("0 = Light markers disabled")]
    public int maxLightMarkerCharge;
    public int maxLightMarkerCount;

    [Tooltip("0 = Prime markers disabled")]
    public int maxPrimeMarkerCharge;
    public int maxPrimeMarkerCount;

    [Tooltip("0 = Heavy markers disabled")]
    public int maxRecursionMarkerCharge;
    public int maxRecursionMarkerCount;

    [Header("Wave Timing")]
    public float waveStartDelay;
    public float moveInterval;
    public float fastMoveInterval;

    [Header("Success Criteria")]
    public bool hasOwnSuccessCriteria = false;
    public int requiredCaptureCount;
    public int maxAllowedEscapes;

    [Header("Messages")]
    public List<WaveMessage> messages = new List<WaveMessage>();

    [Header("Statistics")]
    public int unitCubesCaptured;
    public int primeCubesCaptured;
    public int infinityCubesEscaped;
    public int recursionCubesCaptured;
    public int markersPlaced;
    public int detonationsUsed;

    [Header("Paired Wave System")]
    [Tooltip("Rules for how markers from the previous wave spawn cubes in this wave when mirrored. Only used when HasBeenMirrored is true.")]
    public MarkerSpawnRules markerSpawnRules = new MarkerSpawnRules();
    
    [Tooltip("Delay before inherited cubes spawn (in seconds). Allows base spawns to appear first.")]
    public float inheritanceDelay = 0f;
    
    // Runtime-only: Tracks if this wave instance has been mirrored (spawned with marker-based cubes)
    [System.NonSerialized]
    public bool HasBeenMirrored = false;
}

/// <summary>
/// Defines rules for how markers from the previous wave spawn cubes in the mirrored version of this wave
/// </summary>
[System.Serializable]
public class MarkerSpawnRules
{
    [Tooltip("If true, Light markers from the previous wave spawn Unit cubes in the mirrored wave")]
    public bool lightSpawnsUnit = true;
    
    [Tooltip("If true, Heavy markers from the previous wave spawn Recursion cubes in the mirrored wave")]
    public bool heavySpawnsRecursion = true;
    
    [Tooltip("If true, Prime markers from the previous wave spawn Prime cubes in the mirrored wave")]
    public bool primeSpawnsPrime = true;
    
    [Tooltip("If true, Infinity markers from the previous wave spawn Infinity cubes in the mirrored wave")]
    public bool infinitySpawnsInfinity = true;
}
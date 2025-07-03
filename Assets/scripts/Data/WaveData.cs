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
    public int maxHeavyMarkerCharge;
    public int maxHeavyMarkerCount;

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
}
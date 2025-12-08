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
    [Tooltip("0 = Unit markers disabled")]
    public int maxUnitMarkerCharge;
    public int maxUnitMarkerCount;

    [Tooltip("0 = Matrix markers disabled")]
    public int maxMatrixMarkerCharge;
    public int maxMatrixMarkerCount;

    [Tooltip("0 = Recursion markers disabled")]
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
    public int matrixCubesCaptured;
    public int infinityCubesEscaped;
    public int recursionCubesCaptured;
    public int markersPlaced;
    public int detonationsUsed;
}
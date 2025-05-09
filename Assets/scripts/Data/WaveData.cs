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

    [Header("Wave Settings")]
    public bool limitMarkers = false;
    public int maxMarkers = 2;
    public float waveStartDelay = 0.75f;
    public float moveInterval = 0.75f;
    public float fastMoveInterval = 0.1f;

    [Header("Success Criteria")]
    public bool hasOwnSuccessCriteria = false;
    public int requiredCaptureCount = 0;
    public int maxAllowedEscapes = 0;

    [Header("Messages")]
    public List<WaveMessage> messages = new List<WaveMessage>();

    [Header("Statistics")]
    public int normalCubesCaptured = 0;
    public int blueCubesCaptured = 0;
    public int cubesEscaped = 0;
    public int markersPlaced = 0;
    public int detonationsUsed = 0;
}
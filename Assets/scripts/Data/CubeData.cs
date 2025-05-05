using UnityEngine;
using static Enumerations;

[CreateAssetMenu(fileName = "Cube Data", menuName = "Infinity Qube/Cube Data")]
public class CubeData : ScriptableObject
{
    [Header("Cube Identity")]
    public string cubeName;
    public CubeType type;
    public int level;
    public Vector2Int position;


    [Header("Visual Settings")]
    public GameObject prefab;
    public Material material;

    [Header("Cube Level & Scoring")]
    public int cubeLevel = 1;
    public int capturePoints = 100;
    public int missPenaltyPoints = 0;

    [Header("Audio & FX")]
    public AudioClip spawnClip;
    public AudioClip captureClip;
    public ParticleSystem captureEffect;
}

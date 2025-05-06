using UnityEngine;
using static Enumerations;

[CreateAssetMenu(fileName = "New Cube Data", menuName = "Infinity Qube/Cube Data")]
public class CubeData : ScriptableObject
{
    [Header("Cube Identity")]
    public CubeType type = CubeType.Normal;
    public int level = 1;
    public Vector2Int position;

    [Header("Visual Settings")]
    public GameObject prefab;
    public Material material;

    [Header("Runtime State")]
    [HideInInspector] public bool isRainingCube = false;
    [HideInInspector] public int moveCountRemaining = 0;
    [HideInInspector] public bool isDestroyed = false;

    // Create a runtime copy of this data (for instances)
    public CubeData CreateRuntimeInstance()
    {
        CubeData instance = Instantiate(this);
        instance.name = this.name + "_Instance";
        return instance;
    }

    // Reset runtime values
    public void ResetRuntime()
    {
        isRainingCube = false;
        moveCountRemaining = 0;
        isDestroyed = false;
    }
}
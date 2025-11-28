using UnityEngine;
using static Enumerations;

[System.Serializable]
public class CubeData
{
    public CubeType type;
    public Vector2Int position;
    public int level = 1;

    // Runtime-only state (not serialized)
    [System.NonSerialized] public bool isRainingCube;
    [System.NonSerialized] public int moveCountRemaining;


    // This is a property, not a field - it gets the definition at runtime
    public CubeTypeDefinition Definition
    {
        get { return GridManager.Instance.GetCubeDefinition(type); }
    }

}
using System;
using System.Collections.Generic;
using UnityEngine;
using static Enumerations;


[CreateAssetMenu(fileName = "CubeTypes", menuName = "Infinity Qube/Cube Types")]
public class CubeTypeDefinitions : ScriptableObject
{
    // Array of definitions, in same order as Enumerations.CubeType
    [SerializeField] public List<CubeTypeDefinition> definitions = new List<CubeTypeDefinition>();

    // Get definition for a specific type
    public CubeTypeDefinition GetDefinition(Enumerations.CubeType type)
    {
        int index = (int)type;
        if (index >= 0 && index < definitions.Count)
            return definitions[index];
        return null;
    }

    // Validate the array has all types on load
    private void OnValidate()
    {
        // Ensure we have entry for each enum value
        int typeCount = System.Enum.GetValues(typeof(Enumerations.CubeType)).Length;
        while (definitions.Count < typeCount)
            definitions.Add(new CubeTypeDefinition());
    }
}

[Serializable]
public class CubeTypeDefinition
{
    public string name = "Default";
    public GameObject prefab;
    public Material material;
    public bool canBeMarked = true;
    public bool causesCorruption = false;
    public bool enablesDetonation = false;
    public int detonationRadius = 0;
}

[System.Serializable]
public class CubeData
{
    public Enumerations.CubeType type;
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
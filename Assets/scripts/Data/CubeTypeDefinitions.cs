using System.Collections.Generic;
using UnityEngine;
using static Enumerations;

[CreateAssetMenu(fileName = "CubeTypes", menuName = "Infinity Qube/Cube Types")]
public class CubeTypeDefinitions : ScriptableObject
{
    // Array of definitions, in same order as CubeType
    [SerializeField] public List<CubeTypeDefinition> definitions = new List<CubeTypeDefinition>();

    // Get definition for a specific type
    public CubeTypeDefinition GetDefinition(CubeType type)
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
        int typeCount = System.Enum.GetValues(typeof(CubeType)).Length;
        while (definitions.Count < typeCount)
            definitions.Add(new CubeTypeDefinition());
    }
}

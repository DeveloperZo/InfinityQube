using System;
using UnityEngine;

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

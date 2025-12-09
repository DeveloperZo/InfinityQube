using UnityEngine;
using static Enumerations;

/// <summary>
/// Serializable data for a single cube in a wave configuration.
/// Defines type, position, and optional pre-painted faces.
/// </summary>
[System.Serializable]
public class CubeData
{
    #region Core Properties
    
    [Tooltip("Type of cube (Unit, Matrix, Recursion, Infinity)")]
    public CubeType type;
    
    [Tooltip("Grid position within the wave spawn area")]
    public Vector2Int position;
    
    [Tooltip("Cube level/tier (affects behavior for some cube types)")]
    [Range(1, 5)] public int level = 1;
    
    #endregion
    
    #region Face Painting (Pre-configured)
    
    [Header("Pre-Painted Faces")]
    [Tooltip("If true, this cube spawns with pre-painted faces")]
    public bool hasPaintedFaces = false;
    
    [Tooltip("Status of the front face (None, InfinityFace, MatrixFace, RecursionFace)")]
    public FaceStatus frontFaceStatus = FaceStatus.None;
    
    [Tooltip("Charges on front face (if painted)")]
    [Range(0, 10)] public int frontFaceCharges = 0;
    
    #endregion
    
    #region Spawn Timing
    
    [Header("Spawn Timing")]
    [Tooltip("Delay before this cube spawns (0 = spawn immediately with wave)")]
    [Range(0f, 10f)] public float spawnDelay = 0f;
    
    #endregion
    
    #region Runtime State (Not Serialized)
    
    /// <summary>Runtime: Is this cube currently falling/raining down?</summary>
    [System.NonSerialized] public bool isRainingCube;
    
    /// <summary>Runtime: Moves remaining until cube reaches grid</summary>
    [System.NonSerialized] public int moveCountRemaining;
    
    /// <summary>Runtime: Reference to spawned CubeManager instance</summary>
    [System.NonSerialized] public CubeManager spawnedInstance;
    
    #endregion
    
    #region Properties
    
    /// <summary>
    /// Gets the CubeTypeDefinition from GridManager at runtime.
    /// </summary>
    public CubeTypeDefinition Definition
    {
        get 
        { 
            if (GridManager.Instance == null) return null;
            return GridManager.Instance.GetCubeDefinition(type); 
        }
    }
    
    #endregion
    
    #region Constructors
    
    public CubeData() { }
    
    public CubeData(CubeType cubeType, Vector2Int pos)
    {
        type = cubeType;
        position = pos;
        level = 1;
    }
    
    public CubeData(CubeType cubeType, int x, int y)
    {
        type = cubeType;
        position = new Vector2Int(x, y);
        level = 1;
    }
    
    #endregion
    
    #region Utility
    
    /// <summary>
    /// Creates a copy of this CubeData.
    /// </summary>
    public CubeData Clone()
    {
        return new CubeData
        {
            type = this.type,
            position = this.position,
            level = this.level,
            hasPaintedFaces = this.hasPaintedFaces,
            frontFaceStatus = this.frontFaceStatus,
            frontFaceCharges = this.frontFaceCharges,
            spawnDelay = this.spawnDelay
        };
    }
    
    public override string ToString()
    {
        string painted = hasPaintedFaces ? $" [{frontFaceStatus}:{frontFaceCharges}]" : "";
        return $"{type}@({position.x},{position.y}){painted}";
    }
    
    #endregion
}
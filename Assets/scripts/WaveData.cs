using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Wave", menuName = "Infinity Qube/Wave Data")]
public class WaveData : ScriptableObject
{
    public int Index = 0;
    public List<CubeData> CubesData = new List<CubeData>();
}
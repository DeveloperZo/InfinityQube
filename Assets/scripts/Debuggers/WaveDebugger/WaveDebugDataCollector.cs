using UnityEngine;
using System.Collections.Generic;

public class WaveDebugDataCollector : MonoBehaviour
{
    [SerializeField] public int TotalSpawned;
    [SerializeField] public int TotalRemoved;
    [SerializeField] public List<WaveMessage> CurrentWaveMessages;

    /// <summary>Call when a new cube appears in waveManager.activeCubes.</summary>
    public void RecordCubeSpawned(CubeBehavior cube)
    {
        TotalSpawned++;
    }

    /// <summary>Call when a cube is removed (captured or escaped) from activeCubes.</summary>
    public void RecordCubeRemoved(CubeBehavior cube)
    {
        TotalRemoved++;
    }

    /// <summary>Replace the in-memory message list to use for Save/Spawn debug info.</summary>
    public void SetWaveMessages(List<WaveMessage> msgs)
    {
        CurrentWaveMessages = new List<WaveMessage>(msgs);
    }

    /// <summary>Reset statistical counters between waves.</summary>
    public void Reset()
    {
        TotalSpawned = 0;
        TotalRemoved = 0;
        CurrentWaveMessages.Clear();
    }
}

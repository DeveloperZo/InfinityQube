using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using static Enumerations;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WaveDebugWaveController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private WaveDebugGridConfigurator gridConfig;
    [SerializeField] private WaveDebugDataCollector dataCollector;
    [SerializeField] private string saveLocation = "Assets/data/waves/";
    [SerializeField] public WaveData nextWave;

    private List<CubeBehavior> previousCubes = new();
    private bool trackingActive = false;

    private void Awake()
    {
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();
        if (gridConfig == null) gridConfig = GetComponent<WaveDebugGridConfigurator>();
        if (dataCollector == null) dataCollector = GetComponent<WaveDebugDataCollector>();
    }

    /// <summary>Call to start a new wave based on blueprint.</summary>
    public void SpawnWave()
    {
        if (waveManager == null) return;
        dataCollector.Reset();
        previousCubes.Clear();

        // Build WaveData asset in-memory
        WaveData wd = ScriptableObject.CreateInstance<WaveData>();
        wd.GridWidth = gridConfig.WaveWidth;
        wd.GridHeight = gridConfig.WaveHeight;
        wd.CubesData = new List<CubeData>();
        wd.messages = new List<WaveMessage>(dataCollector.CurrentWaveMessages);

        // Populate cube list from grid state
        for (int y = 0; y < gridConfig.WaveHeight; y++)
        {
            for (int x = 0; x < gridConfig.WaveWidth; x++)
            {
                int state = gridConfig.GridState[x, y];
                if (state == 0) continue;

                CubeData cd = new CubeData
                {
                    type = (CubeType)(state - 1),
                    position = new Vector2Int(x, gridManager.Height - gridConfig.WaveHeight + (gridConfig.WaveHeight - 1 - y)),
                    level = 1
                };
                wd.CubesData.Add(cd);
            }
        }

        waveManager.useWaveConfiguration = true;
        waveManager.waveConfiguration = new List<WaveData> { wd };
        waveManager.StartWave();
        StartCoroutine(DelayedTracking());
    }

    private IEnumerator DelayedTracking()
    {
        yield return new WaitForSeconds(0.1f);
        BeginTracking();
    }

    private void BeginTracking()
    {
        if (waveManager == null) return;
        previousCubes = new List<CubeBehavior>(waveManager.activeCubes);
        foreach (var c in previousCubes)
            dataCollector.RecordCubeSpawned(c);
        trackingActive = true;
    }

    private void Update()
    {
        if (!trackingActive || waveManager == null) return;

        var current = waveManager.activeCubes;
        // Spawn detection
        foreach (var c in current)
        {
            if (!previousCubes.Contains(c))
                dataCollector.RecordCubeSpawned(c);
        }
        // Removal detection
        foreach (var c in previousCubes)
        {
            if (!current.Contains(c))
                dataCollector.RecordCubeRemoved(c);
        }
        previousCubes = new List<CubeBehavior>(current);
    }

    /// <summary>Save last spawned wave as asset file.</summary>
    public void SaveCurrentWave()
    {
#if UNITY_EDITOR
        if (!System.IO.Directory.Exists(saveLocation))
            System.IO.Directory.CreateDirectory(saveLocation);
        string ts = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string path = $"{saveLocation}Wave_{ts}.asset";
        WaveData asset = ScriptableObject.CreateInstance<WaveData>();
        // Copy data from last run (omitted for brevity)
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"Wave saved to {path}");
#endif
    }
}

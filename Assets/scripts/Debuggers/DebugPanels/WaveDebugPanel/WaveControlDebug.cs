using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WaveDebugSystem
{
    public class WaveControlDebug
    {
        private WaveManager waveManager;
        private GridManager gridManager;
        private bool autoSyncToGrid;
        private List<WaveData> cachedLibraryWaves = new List<WaveData>();
        private int currentLibraryIndex = -1;

        public void Initialize(WaveManager waveManager, GridManager gridManager)
        {
            this.waveManager = waveManager;
            this.gridManager = gridManager;
            this.autoSyncToGrid = true;
            RefreshLibraryCache();
        }

        public void DrawPanel(System.Action<WaveData> onWaveChanged = null)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("WAVE CONTROLS", GUI.skin.box);

            if (waveManager != null)
            {
                DrawWaveStatus();
                DrawMainControls();
                DrawNavigationControls(onWaveChanged);
                DrawDebugControls();
                DrawCurrentWaveInfo();
            }
            else
            {
                GUILayout.Label("WaveManager not found");
            }

            GUILayout.EndVertical();
        }

        private void DrawWaveStatus()
        {
            string waveName = waveManager.CurrentWave != null ? waveManager.CurrentWave.name : "None";
            GUILayout.Label($"Current Wave: {waveName}");
            GUILayout.Label($"Library Index: {currentLibraryIndex + 1}/{cachedLibraryWaves.Count}");
            GUILayout.Label($"Step: {waveManager.MoveStep} | Active: {waveManager.waveActive}");
            GUILayout.Label($"Cubes: {waveManager.activeCubes.Count} | Speed: {(waveManager.isSpeedingUp ? "FAST" : "NORMAL")}");
        }

        private void DrawMainControls()
        {
            GUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Start/Reset"))
            {
                waveManager.StopWave();
                waveManager.StartWave();
            }

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Pause"))
                waveManager.PauseWave();

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Step ▶"))
                waveManager.ManualMoveWaveForward();

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear"))
            {
                waveManager.StopWave();
                waveManager.ClearAllCubes();
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }

        private void DrawNavigationControls(System.Action<WaveData> onWaveChanged)
        {
            GUILayout.BeginHorizontal();

            // Previous button
            if (GUILayout.Button("◀◀ Prev", GUILayout.Width(60)))
            {
                NavigateToPreviousWave(onWaveChanged);
            }

            // Current wave info
            string currentInfo = "No Wave";
            if (currentLibraryIndex >= 0 && currentLibraryIndex < cachedLibraryWaves.Count)
            {
                currentInfo = cachedLibraryWaves[currentLibraryIndex].name;
            }

            GUILayout.Label(currentInfo, GUI.skin.box, GUILayout.ExpandWidth(true));

            // Next button
            if (GUILayout.Button("Next ▶▶", GUILayout.Width(60)))
            {
                NavigateToNextWave(onWaveChanged);
            }

            // Refresh library
            if (GUILayout.Button("↻", GUILayout.Width(25)))
            {
                RefreshLibraryCache();
            }

            GUILayout.EndHorizontal();
        }

        private void RefreshLibraryCache()
        {
            cachedLibraryWaves.Clear();

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:WaveData", new[] { "Assets/data/waves" });
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                WaveData wave = UnityEditor.AssetDatabase.LoadAssetAtPath<WaveData>(path);
                if (wave != null)
                {
                    cachedLibraryWaves.Add(wave);
                }
            }
#endif

            // Sort by name for consistent ordering
            cachedLibraryWaves.Sort((a, b) => string.Compare(a.name, b.name));

            // Find current wave in library
            if (waveManager?.CurrentWave != null)
            {
                currentLibraryIndex = cachedLibraryWaves.FindIndex(w => w.name == waveManager.CurrentWave.name);
            }

            if (currentLibraryIndex < 0 && cachedLibraryWaves.Count > 0)
            {
                currentLibraryIndex = 0;
            }

            Debug.Log($"Library refreshed: {cachedLibraryWaves.Count} waves found");
        }

        private void NavigateToPreviousWave(System.Action<WaveData> onWaveChanged)
        {
            if (cachedLibraryWaves.Count == 0)
            {
                RefreshLibraryCache();
                if (cachedLibraryWaves.Count == 0) return;
            }

            // Move to previous with wrap-around
            currentLibraryIndex--;
            if (currentLibraryIndex < 0)
            {
                currentLibraryIndex = cachedLibraryWaves.Count - 1;
            }

            LoadWaveAtIndex(currentLibraryIndex, onWaveChanged);
        }

        private void NavigateToNextWave(System.Action<WaveData> onWaveChanged)
        {
            if (cachedLibraryWaves.Count == 0)
            {
                RefreshLibraryCache();
                if (cachedLibraryWaves.Count == 0) return;
            }

            // Move to next with wrap-around
            currentLibraryIndex++;
            if (currentLibraryIndex >= cachedLibraryWaves.Count)
            {
                currentLibraryIndex = 0;
            }

            LoadWaveAtIndex(currentLibraryIndex, onWaveChanged);
        }

        private void LoadWaveAtIndex(int index, System.Action<WaveData> onWaveChanged)
        {
            if (index < 0 || index >= cachedLibraryWaves.Count) return;

            var wave = cachedLibraryWaves[index];
            LoadWaveToManager(wave);
            onWaveChanged?.Invoke(wave);

            if (autoSyncToGrid)
            {
                SyncWaveToGrid(wave);
            }
        }

        private void LoadWaveToManager(WaveData wave)
        {
            if (waveManager == null || wave == null) return;

            // Clear existing configuration
            waveManager.waveConfiguration.Clear();
            waveManager.useWaveConfiguration = true;

            // Create a copy of the wave to avoid modifying the asset
            var waveCopy = Object.Instantiate(wave);
            waveCopy.name = wave.name; // Keep original name

            // Add the copy to wave manager
            waveManager.waveConfiguration.Add(waveCopy);
            waveManager.currentWaveIndex = 0;

            Debug.Log($"Loaded wave '{wave.name}' to wave manager");
        }

        public void SyncWaveToGrid(WaveData wave, int rowOverride = -1)
        {
            if (wave == null || waveManager == null || gridManager == null) return;

            // Clear current cubes
            waveManager.ClearAllCubes();

            // Spawn cubes from wave configuration at top of grid
            foreach (var cubeData in wave.CubesData)
            {
                cubeData.position.x = rowOverride > 0 ? rowOverride : cubeData.position.x;
                SpawnCubeAtGridTop(cubeData);
            }

            Debug.Log($"Synced wave '{wave.name}' to grid - spawned {wave.CubesData.Count} cubes at grid height");
        }

        private void SpawnCubeAtGridTop(CubeData cubeData)
        {
            if (waveManager?.cubePrefabs == null || (int)cubeData.type >= waveManager.cubePrefabs.Length)
            {
                Debug.LogWarning($"Cannot spawn cube type {cubeData.type} - prefab not available");
                return;
            }

            // Calculate spawn position at top of grid
            Vector2Int spawnPosition = new Vector2Int(
                cubeData.position.x,
                gridManager.Height - 1 - (cubeData.position.y % gridManager.Height)
            );

            if (!gridManager.IsValidGridPosition(spawnPosition))
            {
                Debug.LogWarning($"Cannot spawn cube at invalid position ({spawnPosition.x}, {spawnPosition.y})");
                return;
            }

            // Spawn at the calculated grid position
            Vector3 worldPos = gridManager.GridToWorldPosition(spawnPosition.x, spawnPosition.y, 2f);
            GameObject cubeObj = Object.Instantiate(waveManager.cubePrefabs[(int)cubeData.type], worldPos, Quaternion.identity);

            var cube = cubeObj.GetComponent<CubeManager>();
            if (cube == null) cube = cubeObj.AddComponent<CubeManager>();

            // Initialize with adjusted position
            var adjustedCubeData = new CubeData
            {
                type = cubeData.type,
                position = spawnPosition,
                level = cubeData.level
            };

            cube.Init(gridManager, adjustedCubeData, 2f);
            waveManager.activeCubes.Add(cube);

            Debug.Log($"Spawned {cubeData.type} cube at grid ({spawnPosition.x}, {spawnPosition.y})");
        }

        private void DrawDebugControls()
        {
            GUILayout.BeginHorizontal();

            bool newDebug = GUILayout.Toggle(waveManager.debugMode, "Debug Mode");
            if (newDebug != waveManager.debugMode)
            {
                if (newDebug)
                    waveManager.EnterDebugMode(true);
                else
                    waveManager.ExitDebugMode();
            }

            autoSyncToGrid = GUILayout.Toggle(autoSyncToGrid, "Auto Sync Grid");

            GUILayout.EndHorizontal();
        }

        private void DrawCurrentWaveInfo()
        {
            if (waveManager.CurrentWave != null)
            {
                var wave = waveManager.CurrentWave;
                GUILayout.Label($"Grid: {wave.GridWidth}x{wave.GridHeight} | Cubes: {wave.CubesData.Count}");
                GUILayout.Label($"Timing: {wave.moveInterval:F1}s / {wave.fastMoveInterval:F1}s");

                if (wave.limitMarkers)
                {
                    GUILayout.Label($"Marker Limit: {wave.maxMarkerCount} (charge: {wave.maxMarkerCharge})");
                }
            }
        }

        public bool GetAutoSyncToGrid() => autoSyncToGrid;
        public void SetAutoSyncToGrid(bool value) => autoSyncToGrid = value;
    }
}
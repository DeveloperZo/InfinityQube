using System.Collections.Generic;
using UnityEngine;

namespace WaveDebugSystem
{
    public class WaveLibraryDebug
    {
        private WaveManager waveManager;
        private GridManager gridManager;
        private List<WaveData> availableWaves = new List<WaveData>();
        private Vector2 waveListScroll;
        private bool needsWaveRefresh = true;

        public void Initialize(WaveManager waveManager, GridManager gridManager)
        {
            this.waveManager = waveManager;
            this.gridManager = gridManager;
            RefreshAvailableWaves();
        }

        public void Update()
        {
            if (needsWaveRefresh)
            {
                RefreshAvailableWaves();
                needsWaveRefresh = false;
            }
        }

        public void DrawPanel(WaveData currentEditingWave, System.Action<WaveData> onWaveChanged = null)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("WAVE LIBRARY", GUI.skin.box);

            DrawWaveList(currentEditingWave, onWaveChanged);
            DrawAssetManagement();

            GUILayout.EndVertical();
        }

        private void DrawWaveList(WaveData currentEditingWave, System.Action<WaveData> onWaveChanged)
        {
            waveListScroll = GUILayout.BeginScrollView(waveListScroll, GUILayout.Height(200));

            // Show waves from assets/data/waves
            foreach (var wave in availableWaves)
            {
                if (wave == null) continue;
                
                var waveCopy = ScriptableObject.CreateInstance<WaveData>();
                waveCopy.name = wave.name;
                waveCopy.GridWidth = wave.GridWidth;
                waveCopy.GridHeight = wave.GridHeight;
                waveCopy.moveInterval = wave.moveInterval;
                waveCopy.fastMoveInterval = wave.fastMoveInterval;
                waveCopy.waveStartDelay = wave.waveStartDelay;
                waveCopy.limitMarkers = wave.limitMarkers;
                waveCopy.maxIndividualMarkerCount = wave.maxIndividualMarkerCount;
                waveCopy.maxIndividualMarkerCharge = wave.maxIndividualMarkerCharge;
                waveCopy.maxAreaMarkerCount = wave.maxAreaMarkerCount;
                waveCopy.maxAreaMarkerCharge = wave.maxAreaMarkerCharge;

                foreach (var cube in wave.CubesData)
                {
                    var cubeCopy = new CubeData
                    {
                        type = cube.type,
                        position = new Vector2Int(cube.position.x, cube.position.y),
                        level = cube.level
                    };
                    waveCopy.CubesData.Add(cubeCopy);
                }

                bool isCurrent = currentEditingWave == waveCopy;
                bool isActive = waveManager != null && waveManager.CurrentWave != null &&
                               waveManager.CurrentWave.name == waveCopy.name;

                GUI.backgroundColor = isCurrent ? Color.yellow : (isActive ? Color.green : Color.white);
                GUILayout.BeginHorizontal(GUI.skin.box);

                GUILayout.Label(waveCopy.name, GUILayout.Width(120));
                GUILayout.Label($"{waveCopy.CubesData.Count}c", GUILayout.Width(30));
                GUILayout.Label($"{waveCopy.GridWidth}x{waveCopy.GridHeight}", GUILayout.Width(40));

                if (GUILayout.Button("Edit", GUILayout.Width(35)))
                {
                    onWaveChanged?.Invoke(waveCopy);
                }

                if (GUILayout.Button("Load", GUILayout.Width(35)))
                {
                    LoadWaveToManager(waveCopy);
                }

                if (GUILayout.Button("Copy", GUILayout.Width(35)))
                {
                    var copiedWave = CopyWave(waveCopy);
                    onWaveChanged?.Invoke(copiedWave);
                }

                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    DeleteWaveAsset(waveCopy);
                }

                GUILayout.EndHorizontal();
            }
            GUI.backgroundColor = Color.white;

            GUILayout.EndScrollView();
        }

        private void DrawAssetManagement()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh"))
                needsWaveRefresh = true;
            if (GUILayout.Button("Open Folder"))
                OpenWavesFolder();
            if (GUILayout.Button("Create Template"))
                CreateTemplateWaves();
            GUILayout.EndHorizontal();
            
            // Fast Testing Mode enhancements
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Load All to Manager", GUILayout.Width(120)))
                LoadAllWavesToManager();
            if (GUILayout.Button("Quick Save All", GUILayout.Width(100)))
                QuickSaveCurrentConfiguration();
            if (GUILayout.Button("Test Sequence", GUILayout.Width(100)))
                TestWaveSequence();
            GUILayout.EndHorizontal();
        }

        private void RefreshAvailableWaves()
        {
            availableWaves.Clear();

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:WaveData", new[] { "Assets/data/waves" });
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                WaveData wave = UnityEditor.AssetDatabase.LoadAssetAtPath<WaveData>(path);
                if (wave != null)
                {
                    availableWaves.Add(wave);
                }
            }
#endif

            availableWaves.Sort((a, b) => string.Compare(a.name, b.name));
            Debug.Log($"Found {availableWaves.Count} wave assets");
        }

        private void LoadWaveToManager(WaveData wave)
        {
            if (waveManager == null || wave == null) return;
            waveManager.useWaveConfiguration = true;
            // Add to wave manager configuration if not already there
            if (!waveManager.waveConfiguration.Contains(wave))
            {
                foreach(var cube in wave.CubesData)
                {
                    cube.position = new Vector2Int(cube.position.x, cube.position.y);
                }
                waveManager.waveConfiguration.Add(wave);
            }

            // Set as current wave
            waveManager.currentWaveIndex = waveManager.waveConfiguration.IndexOf(wave);
            Debug.Log($"Loaded wave '{wave.name}' to wave manager");
        }

        private WaveData CopyWave(WaveData original)
        {
            var copy = Object.Instantiate(original);
            copy.name = original.name + "_Copy";
            Debug.Log($"Created copy of wave '{original.name}'");
            return copy;
        }

        private void DeleteWaveAsset(WaveData wave)
        {
#if UNITY_EDITOR
            string path = UnityEditor.AssetDatabase.GetAssetPath(wave);
            if (!string.IsNullOrEmpty(path))
            {
                if (UnityEditor.EditorUtility.DisplayDialog("Delete Wave",
                    $"Are you sure you want to delete '{wave.name}'?", "Delete", "Cancel"))
                {
                    UnityEditor.AssetDatabase.DeleteAsset(path);
                    UnityEditor.AssetDatabase.Refresh();
                    needsWaveRefresh = true;
                    Debug.Log($"Deleted wave asset: {wave.name}");
                }
            }
#endif
        }

        private void OpenWavesFolder()
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.RevealInFinder("Assets/data/waves");
#endif
        }

        private void CreateTemplateWaves()
        {
#if UNITY_EDITOR
            string wavesPath = "Assets/data/waves";

            // Ensure directory exists
            if (!UnityEditor.AssetDatabase.IsValidFolder(wavesPath))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets/data", "waves");
            }

            // Create basic template wave
            var basicWave = ScriptableObject.CreateInstance<WaveData>();
            basicWave.name = "Template_Basic";
            basicWave.GridWidth = 5;
            basicWave.GridHeight = 3;
            basicWave.moveInterval = 2.0f;
            basicWave.fastMoveInterval = 0.1f;
            basicWave.waveStartDelay = 1.0f;
            basicWave.CubesData = new List<CubeData>();

            // Add some basic cubes at the top
            for (int x = 0; x < basicWave.GridWidth; x++)
            {
                if (x % 2 == 0) // Every other position
                {
                    basicWave.CubesData.Add(new CubeData
                    {
                        type = Enumerations.CubeType.Normal,
                        position = new Vector2Int(x, basicWave.GridHeight - 1),
                        level = 1
                    });
                }
            }

            // Create advanced template wave
            var advancedWave = ScriptableObject.CreateInstance<WaveData>();
            advancedWave.name = "Template_Advanced";
            advancedWave.GridWidth = 5;
            advancedWave.GridHeight = 5;
            advancedWave.moveInterval = 1.5f;
            advancedWave.fastMoveInterval = 0.1f;
            advancedWave.waveStartDelay = 0.75f;
            advancedWave.limitMarkers = true;
            advancedWave.maxIndividualMarkerCount = 3;
            advancedWave.maxIndividualMarkerCharge = 2;
            advancedWave.maxAreaMarkerCount = 3;
            advancedWave.maxAreaMarkerCharge = 2;
            advancedWave.CubesData = new List<CubeData>();

            // Add mixed cube types
            for (int y = advancedWave.GridHeight - 2; y < advancedWave.GridHeight; y++)
            {
                for (int x = 0; x < advancedWave.GridWidth; x++)
                {
                    Enumerations.CubeType cubeType = Enumerations.CubeType.Normal;
                    if (x == 1 || x == 3) cubeType = Enumerations.CubeType.Blue;
                    if (x == 2 && y == advancedWave.GridHeight - 1) cubeType = Enumerations.CubeType.Black;

                    advancedWave.CubesData.Add(new CubeData
                    {
                        type = cubeType,
                        position = new Vector2Int(x, y),
                        level = 1
                    });
                }
            }

            // Save the templates
            UnityEditor.AssetDatabase.CreateAsset(basicWave, $"{wavesPath}/Template_Basic.asset");
            UnityEditor.AssetDatabase.CreateAsset(advancedWave, $"{wavesPath}/Template_Advanced.asset");
            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();

            needsWaveRefresh = true;
            Debug.Log("Created template waves: Template_Basic and Template_Advanced");
#endif
        }

        public void ForceRefresh()
        {
            needsWaveRefresh = true;
        }

        public List<WaveData> GetAvailableWaves() => availableWaves;

        // Fast Testing Mode enhancements
        private void LoadAllWavesToManager()
        {
            if (waveManager == null || availableWaves.Count == 0)
            {
                Debug.LogWarning("Cannot load waves - missing manager or no waves available");
                return;
            }

            waveManager.waveConfiguration.Clear();
            
            foreach (var wave in availableWaves)
            {
                if (wave != null)
                {
                    var waveCopy = Object.Instantiate(wave);
                    waveManager.waveConfiguration.Add(waveCopy);
                }
            }

            waveManager.useWaveConfiguration = true;
            waveManager.currentWaveIndex = 0;
            
            Debug.Log($"🚀 Loaded {availableWaves.Count} waves to manager for batch testing");
        }

        private void QuickSaveCurrentConfiguration()
        {
            if (waveManager?.waveConfiguration == null || waveManager.waveConfiguration.Count == 0)
            {
                Debug.LogWarning("No wave configuration to save");
                return;
            }

#if UNITY_EDITOR
            string configPath = "Assets/data/waves/QuickSave_Config";
            
            // Ensure directory exists
            if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/data/waves"))
            {
                UnityEditor.AssetDatabase.CreateFolder("Assets/data", "waves");
            }

            int savedCount = 0;
            for (int i = 0; i < waveManager.waveConfiguration.Count; i++)
            {
                var wave = waveManager.waveConfiguration[i];
                if (wave != null)
                {
                    string assetPath = $"{configPath}_{i:D2}_{wave.name}.asset";
                    var saveWave = Object.Instantiate(wave);
                    saveWave.name = $"QuickSave_{i:D2}_{wave.name}";
                    
                    UnityEditor.AssetDatabase.CreateAsset(saveWave, assetPath);
                    savedCount++;
                }
            }

            UnityEditor.AssetDatabase.SaveAssets();
            UnityEditor.AssetDatabase.Refresh();
            needsWaveRefresh = true;
            
            Debug.Log($"💾 Quick saved {savedCount} waves from current configuration");
#endif
        }

        private void TestWaveSequence()
        {
            if (waveManager == null || availableWaves.Count == 0)
            {
                Debug.LogWarning("Cannot test sequence - missing manager or no waves available");
                return;
            }

            // Load all waves and start sequence testing
            LoadAllWavesToManager();
            
            // Start the first wave
            waveManager.currentWaveIndex = 0;
            waveManager.StartWave();
            
            Debug.Log($"🏁 Started sequence test with {availableWaves.Count} waves. Use wave controls to navigate.");
        }
    }
}
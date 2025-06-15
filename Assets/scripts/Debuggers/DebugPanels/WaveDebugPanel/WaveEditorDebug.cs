using System.Collections.Generic;
using UnityEngine;

namespace WaveDebugSystem
{

    namespace WaveDebugSystem
    {
        public class WaveEditorDebug
        {
            private WaveManager waveManager;
            private GridManager gridManager;
            private WaveData currentEditingWave;

            public WaveData CurrentEditingWave => currentEditingWave;

            public void Initialize(WaveManager waveManager, GridManager gridManager)
            {
                this.waveManager = waveManager;
                this.gridManager = gridManager;
            }

            public void DrawPanel(System.Action<WaveData> onWaveChanged = null, System.Action onSyncToGrid = null)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("WAVE EDITOR", GUI.skin.box);

                DrawQuickActions(onWaveChanged);

                if (currentEditingWave != null)
                {
                    GUILayout.Space(3);
                    DrawWaveNameEditor();
                    DrawDimensionControls();
                    DrawWavePropertiesEditor();
                    DrawTestAndSyncControls(onSyncToGrid);
                }

                GUILayout.EndVertical();
            }

            private void DrawQuickActions(System.Action<WaveData> onWaveChanged)
            {
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("New Wave"))
                    CreateNewWave();

                if (GUILayout.Button("Load Current"))
                    LoadCurrentWaveForEditing(onWaveChanged);

                if (GUILayout.Button("Save Wave") && currentEditingWave != null)
                    SaveCurrentWave();

                GUILayout.EndHorizontal();
            }

            private void DrawWaveNameEditor()
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name:", GUILayout.Width(40));
                string newName = GUILayout.TextField(currentEditingWave.name);
                if (newName != currentEditingWave.name)
                {
                    currentEditingWave.name = newName;
                }
                GUILayout.EndHorizontal();
            }

            private void DrawDimensionControls()
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Dimensions:");

                // Grid dimensions
                GUILayout.BeginHorizontal();
                GUILayout.Label("Grid:", GUILayout.Width(35));
                if (GUILayout.Button("-", GUILayout.Width(20)) && gridManager.Width > 3)
                {
                    gridManager.ResizeGrid(gridManager.Width - 1, gridManager.Height);
                }
                GUILayout.Label($"{gridManager.Width}", GUILayout.Width(25));
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    gridManager.ResizeGrid(gridManager.Width + 1, gridManager.Height);
                }

                GUILayout.Label("x", GUILayout.Width(10));

                if (GUILayout.Button("-", GUILayout.Width(20)) && gridManager.Height > 10)
                {
                    gridManager.ResizeGrid(gridManager.Width, gridManager.Height - 1);
                }
                GUILayout.Label($"{gridManager.Height}", GUILayout.Width(25));
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    gridManager.ResizeGrid(gridManager.Width, gridManager.Height + 1);
                }
                GUILayout.EndHorizontal();

                // Wave dimensions
                GUILayout.BeginHorizontal();
                GUILayout.Label("Wave:", GUILayout.Width(35));
                if (GUILayout.Button("-", GUILayout.Width(20)) && currentEditingWave.GridWidth > 1)
                {
                    currentEditingWave.GridWidth--;
                    ClampCubesToWaveBounds();
                }
                GUILayout.Label($"{currentEditingWave.GridWidth}", GUILayout.Width(25));
                if (GUILayout.Button("+", GUILayout.Width(20)) && currentEditingWave.GridWidth < gridManager.Width)
                {
                    currentEditingWave.GridWidth++;
                }

                GUILayout.Label("x", GUILayout.Width(10));

                if (GUILayout.Button("-", GUILayout.Width(20)) && currentEditingWave.GridHeight > 1)
                {
                    currentEditingWave.GridHeight--;
                    ClampCubesToWaveBounds();
                }
                GUILayout.Label($"{currentEditingWave.GridHeight}", GUILayout.Width(25));
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    currentEditingWave.GridHeight++;
                }
                GUILayout.EndHorizontal();

                // Validation
                if (currentEditingWave.GridWidth > gridManager.Width)
                {
                    GUI.color = Color.red;
                    GUILayout.Label("Wave width exceeds grid width!");
                    GUI.color = Color.white;
                }

                GUILayout.EndVertical();
            }

            private void DrawWavePropertiesEditor()
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Properties:");

                // Timing controls
                GUILayout.BeginHorizontal();
                GUILayout.Label("Move:", GUILayout.Width(40));
                string intervalStr = GUILayout.TextField(currentEditingWave.moveInterval.ToString("F1"), GUILayout.Width(40));
                if (float.TryParse(intervalStr, out float newInterval))
                    currentEditingWave.moveInterval = Mathf.Max(0.1f, newInterval);

                GUILayout.Label("Fast:", GUILayout.Width(30));
                string fastStr = GUILayout.TextField(currentEditingWave.fastMoveInterval.ToString("F1"), GUILayout.Width(40));
                if (float.TryParse(fastStr, out float newFast))
                    currentEditingWave.fastMoveInterval = Mathf.Max(0.05f, newFast);

                GUILayout.Label("Delay:", GUILayout.Width(35));
                string delayStr = GUILayout.TextField(currentEditingWave.waveStartDelay.ToString("F1"), GUILayout.Width(40));
                if (float.TryParse(delayStr, out float newDelay))
                    currentEditingWave.waveStartDelay = Mathf.Max(0f, newDelay);
                GUILayout.EndHorizontal();

                // Marker limits
                GUILayout.BeginHorizontal();
                currentEditingWave.limitMarkers = GUILayout.Toggle(currentEditingWave.limitMarkers, "Limit Markers");
                if (currentEditingWave.limitMarkers)
                {
                    GUILayout.Label("Max:", GUILayout.Width(30));
                    string maxStr = GUILayout.TextField(currentEditingWave.maxMarkerCount.ToString(), GUILayout.Width(30));
                    if (int.TryParse(maxStr, out int newMax))
                        currentEditingWave.maxMarkerCount = Mathf.Max(1, newMax);

                    GUILayout.Label("Charge:", GUILayout.Width(45));
                    string chargeStr = GUILayout.TextField(currentEditingWave.maxMarkerCharge.ToString(), GUILayout.Width(30));
                    if (int.TryParse(chargeStr, out int newCharge))
                        currentEditingWave.maxMarkerCharge = Mathf.Max(1, newCharge);
                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            private void DrawTestAndSyncControls(System.Action onSyncToGrid)
            {
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Test Wave"))
                    TestCurrentWave();

                if (GUILayout.Button("Sync to Grid"))
                    onSyncToGrid?.Invoke();

                if (GUILayout.Button("Clear All Cubes"))
                {
                    currentEditingWave.CubesData.Clear();
                    onSyncToGrid?.Invoke();
                }

                GUILayout.EndHorizontal();
            }

            public void CreateNewWave()
            {
                currentEditingWave = ScriptableObject.CreateInstance<WaveData>();
                currentEditingWave.name = $"NewWave_{System.DateTime.Now:HHmmss}";
                currentEditingWave.GridWidth = Mathf.Min(5, gridManager.Width);
                currentEditingWave.GridHeight = 3;
                currentEditingWave.moveInterval = 1.5f;
                currentEditingWave.fastMoveInterval = 0.1f;
                currentEditingWave.waveStartDelay = 0.75f;
                currentEditingWave.CubesData = new List<CubeData>();
                currentEditingWave.limitMarkers = false;
                currentEditingWave.maxMarkerCount = 3;
                currentEditingWave.maxMarkerCharge = 2;
            }

            public void LoadCurrentWaveForEditing(System.Action<WaveData> onWaveChanged = null)
            {
                if (waveManager?.CurrentWave != null)
                {
                    LoadWaveForEditing(waveManager.CurrentWave);
                    onWaveChanged?.Invoke(currentEditingWave);
                }
            }

            public void LoadWaveForEditing(WaveData wave)
            {
                if (wave == null) return;
                currentEditingWave = Object.Instantiate(wave);
            }

            public void SaveCurrentWave()
            {
                if (currentEditingWave == null) return;

                string wavesPath = "Assets/data/waves";

#if UNITY_EDITOR
                // Ensure directory exists
                if (!UnityEditor.AssetDatabase.IsValidFolder(wavesPath))
                {
                    UnityEditor.AssetDatabase.CreateFolder("Assets/data", "waves");
                }

                string assetPath = $"{wavesPath}/{currentEditingWave.name}.asset";

                // Check if asset already exists
                var existingAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<WaveData>(assetPath);
                if (existingAsset != null)
                {
                    // Update existing asset
                    UnityEditor.EditorUtility.CopySerialized(currentEditingWave, existingAsset);
                    UnityEditor.EditorUtility.SetDirty(existingAsset);
                }
                else
                {
                    // Create new asset
                    UnityEditor.AssetDatabase.CreateAsset(currentEditingWave, assetPath);
                }

                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();

                Debug.Log($"Wave '{currentEditingWave.name}' saved to {assetPath}");
#endif
            }

            private void TestCurrentWave()
            {
                if (currentEditingWave == null || waveManager == null) return;

                // Add to wave manager configuration if not already there
                if (!waveManager.waveConfiguration.Contains(currentEditingWave))
                {
                    waveManager.waveConfiguration.Add(currentEditingWave);
                }

                // Set as current wave
                waveManager.currentWaveIndex = waveManager.waveConfiguration.IndexOf(currentEditingWave);
                waveManager.StartWave();
            }

            private void ClampCubesToWaveBounds()
            {
                if (currentEditingWave == null) return;

                currentEditingWave.CubesData.RemoveAll(c =>
                    c.position.x >= currentEditingWave.GridWidth ||
                    c.position.y >= currentEditingWave.GridHeight);
            }
        }
    }

    }
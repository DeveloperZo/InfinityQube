using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Enumerations;

namespace WaveDebugSystem
{
    namespace WaveDebugSystem
    {
        public class WaveEditorDebug
        {
            private WaveManager waveManager;
            private GridManager gridManager;
            private WaveData currentEditingWave;
            private bool hasUnsavedChanges = false;

            public WaveData CurrentEditingWave => currentEditingWave;
            public bool HasUnsavedChanges => hasUnsavedChanges;

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
                    DrawUnsavedChangesWarning();
                    GUILayout.Space(3);
                    DrawWaveNameEditor();
                    DrawDimensionControls();
                    DrawWavePropertiesEditor();
                    DrawMessageToggle();
                    DrawWaveManagementControls(onSyncToGrid);
                    DrawCurrentWaveStats();
                }

                GUILayout.EndVertical();
            }

            private void DrawQuickActions(System.Action<WaveData> onWaveChanged)
            {
                GUILayout.BeginHorizontal();

                if (GUILayout.Button("New Wave"))
                {
                    if (ConfirmUnsavedChanges())
                    {
                        CreateNewWave();
                        onWaveChanged?.Invoke(currentEditingWave);
                    }
                }

                if (GUILayout.Button("Load Current"))
                {
                    if (ConfirmUnsavedChanges())
                    {
                        LoadCurrentWaveForEditing(onWaveChanged);
                    }
                }

                GUI.backgroundColor = hasUnsavedChanges ? Color.yellow : Color.white;
                if (GUILayout.Button("Save Wave") && currentEditingWave != null)
                    SaveCurrentWave();

                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }

            private void DrawUnsavedChangesWarning()
            {
                if (hasUnsavedChanges)
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label("⚠ Unsaved Changes", GUI.skin.box);
                    GUI.color = Color.white;
                }
            }

            private void DrawWaveNameEditor()
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name:", GUILayout.Width(40));
                string newName = GUILayout.TextField(currentEditingWave.name);
                if (newName != currentEditingWave.name)
                {
                    currentEditingWave.name = newName;
                    MarkAsChanged();
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
                    MarkAsChanged();
                }
                GUILayout.Label($"{currentEditingWave.GridWidth}", GUILayout.Width(25));
                if (GUILayout.Button("+", GUILayout.Width(20)) && currentEditingWave.GridWidth < gridManager.Width)
                {
                    currentEditingWave.GridWidth++;
                    MarkAsChanged();
                }

                GUILayout.Label("x", GUILayout.Width(10));

                if (GUILayout.Button("-", GUILayout.Width(20)) && currentEditingWave.GridHeight > 1)
                {
                    currentEditingWave.GridHeight--;
                    ClampCubesToWaveBounds();
                    MarkAsChanged();
                }
                GUILayout.Label($"{currentEditingWave.GridHeight}", GUILayout.Width(25));
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    currentEditingWave.GridHeight++;
                    MarkAsChanged();
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
                if (float.TryParse(intervalStr, out float newInterval) && newInterval != currentEditingWave.moveInterval)
                {
                    currentEditingWave.moveInterval = Mathf.Max(0.1f, newInterval);
                    MarkAsChanged();
                }

                GUILayout.Label("Fast:", GUILayout.Width(30));
                string fastStr = GUILayout.TextField(currentEditingWave.fastMoveInterval.ToString("F1"), GUILayout.Width(40));
                if (float.TryParse(fastStr, out float newFast) && newFast != currentEditingWave.fastMoveInterval)
                {
                    currentEditingWave.fastMoveInterval = Mathf.Max(0.05f, newFast);
                    MarkAsChanged();
                }

                GUILayout.Label("Delay:", GUILayout.Width(35));
                string delayStr = GUILayout.TextField(currentEditingWave.waveStartDelay.ToString("F1"), GUILayout.Width(40));
                if (float.TryParse(delayStr, out float newDelay) && newDelay != currentEditingWave.waveStartDelay)
                {
                    currentEditingWave.waveStartDelay = Mathf.Max(0f, newDelay);
                    MarkAsChanged();
                }
                GUILayout.EndHorizontal();

                // Marker limits
                GUILayout.BeginHorizontal();

                       
                GUILayout.Label("Light Max:", GUILayout.Width(30));
                string maxIndividualStr = GUILayout.TextField(currentEditingWave.maxLightMarkerCount.ToString(), GUILayout.Width(30));
                if (int.TryParse(maxIndividualStr, out int newIndividualMax) && newIndividualMax != currentEditingWave.maxLightMarkerCount)
                {
                    currentEditingWave.maxLightMarkerCount = Mathf.Max(1, newIndividualMax);
                    MarkAsChanged();
                }

                GUILayout.Label("Individual Charge:", GUILayout.Width(45));
                string chargeIndividualStr = GUILayout.TextField(currentEditingWave.maxLightMarkerCharge.ToString(), GUILayout.Width(30));
                if (int.TryParse(chargeIndividualStr, out int newIndividualCharge) && newIndividualCharge != currentEditingWave.maxLightMarkerCharge)
                {
                    currentEditingWave.maxLightMarkerCharge = Mathf.Max(1, newIndividualCharge);
                    MarkAsChanged();
                }

                GUILayout.Label("Prime Max:", GUILayout.Width(30));
                string maxAreaStr = GUILayout.TextField(currentEditingWave.maxPrimeMarkerCount.ToString(), GUILayout.Width(30));
                if (int.TryParse(maxAreaStr, out int newAreaMax) && newAreaMax != currentEditingWave.maxPrimeMarkerCount)
                {
                    currentEditingWave.maxPrimeMarkerCount = Mathf.Max(1, newAreaMax);
                    MarkAsChanged();
                }
                GUILayout.Label("Prime Charges:", GUILayout.Width(45));
                string chargeAreaStr = GUILayout.TextField(currentEditingWave.maxPrimeMarkerCharge.ToString(), GUILayout.Width(30));
                if (int.TryParse(chargeAreaStr, out int newAreaCharge) && newAreaCharge != currentEditingWave.maxPrimeMarkerCharge)
                {
                    currentEditingWave.maxPrimeMarkerCharge = Mathf.Max(1, newAreaCharge);
                    MarkAsChanged();
                }


                
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }

            private void DrawWaveManagementControls(System.Action onSyncToGrid)
            {
#if UNITY_EDITOR
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Wave Management:");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Sync to Grid"))
                {
                    onSyncToGrid?.Invoke();
                }

                if (GUILayout.Button("Capture from Grid"))
                {
                    CaptureCurrentGridState();
                }

                if (GUILayout.Button("Test Wave"))
                {
                    TestCurrentWave();
                }
                
                if (GUILayout.Button("Quick Test"))
                {
                    QuickTestCurrentWave();
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear Wave Data"))
                {
                    if (UnityEditor.EditorUtility.DisplayDialog("Clear Wave",
                        "Remove all cubes from this wave?", "Clear", "Cancel"))
                    {
                        currentEditingWave.CubesData.Clear();
                        MarkAsChanged();
                        onSyncToGrid?.Invoke();
                    }
                }

                if (GUILayout.Button("Reset Positions"))
                {
                    ResetCubePositionsToTop();
                    MarkAsChanged();
                }

                if (GUILayout.Button("Duplicate Wave"))
                {
                    DuplicateCurrentWave();
                }
                GUILayout.EndHorizontal();
                
                // Quick modification shortcuts
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Fill Grid", GUILayout.Width(60)))
                {
                    FillGridWithCubes();
                }
                if (GUILayout.Button("Pattern", GUILayout.Width(60)))
                {
                    GenerateTestPattern();
                }
                if (GUILayout.Button("Random", GUILayout.Width(60)))
                {
                    GenerateRandomWave();
                }
                if (GUILayout.Button("Optimize", GUILayout.Width(60)))
                {
                    OptimizeWave();
                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
#endif
            }

            private void DrawCurrentWaveStats()
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("Current Wave Stats:");

                if (currentEditingWave.CubesData.Count > 0)
                {
                    var stats = AnalyzeWaveComposition();
                    GUILayout.Label($"Total Cubes: {currentEditingWave.CubesData.Count}");
                    GUILayout.Label($"Normal: {stats.normalCount} | Blue: {stats.blueCount} | Black: {stats.blackCount} | Reinforced: {stats.reinforcedCount}");

                    if (stats.minY >= 0)
                    {
                        GUILayout.Label($"Y Range: {stats.minY} to {stats.maxY} (Height: {stats.maxY - stats.minY + 1})");
                        GUILayout.Label($"X Range: {stats.minX} to {stats.maxX} (Width: {stats.maxX - stats.minX + 1})");
                    }
                }
                else
                {
                    GUILayout.Label("No cubes configured");
                }

                GUILayout.EndVertical();
            }

            private void DrawMessageToggle()
            {
                GUILayout.BeginHorizontal();

                bool newShowMessages = GUILayout.Toggle(waveManager.showMessages, "Show Wave Messages");
                if (newShowMessages != waveManager.showMessages)
                {
                    waveManager.showMessages = newShowMessages;
                    Debug.Log($"Wave messages {(newShowMessages ? "enabled" : "disabled")}");
                }

                GUILayout.EndHorizontal();
            }

            // Public methods for external systems (like CubeToolsDebug)
            public void AddCubeToWave(Vector2Int gridPosition, CubeType type)
            {
                if (currentEditingWave == null) return;

                // Convert grid position to wave-relative position
                Vector2Int wavePosition = ConvertGridToWavePosition(gridPosition);

                // Remove any existing cube at this wave position
                currentEditingWave.CubesData.RemoveAll(c => c.position == wavePosition);

                // Add new cube
                var cubeData = new CubeData
                {
                    type = type,
                    position = wavePosition,
                    level = 1
                };
                currentEditingWave.CubesData.Add(cubeData);
                MarkAsChanged();

                Debug.Log($"Added {type} cube to wave at wave position ({wavePosition.x}, {wavePosition.y}) from grid ({gridPosition.x}, {gridPosition.y})");
            }

            public void RemoveCubeFromWave(Vector2Int gridPosition)
            {
                if (currentEditingWave == null) return;

                Vector2Int wavePosition = ConvertGridToWavePosition(gridPosition);
                int removed = currentEditingWave.CubesData.RemoveAll(c => c.position == wavePosition);

                if (removed > 0)
                {
                    MarkAsChanged();
                    Debug.Log($"Removed cube from wave at wave position ({wavePosition.x}, {wavePosition.y})");
                }
            }

            public void SyncWaveDataToGrid()
            {
                // Called when grid state changes - capture current grid state
                CaptureCurrentGridState();
            }

            // Core wave management methods
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
                currentEditingWave.maxLightMarkerCount = 3;
                currentEditingWave.maxLightMarkerCharge = 1;
                currentEditingWave.maxPrimeMarkerCount = 5;
                currentEditingWave.maxPrimeMarkerCharge = 1;
                
                hasUnsavedChanges = false;

                Debug.Log($"Created new wave: {currentEditingWave.name}");
            }

            public void LoadCurrentWaveForEditing(System.Action<WaveData> onWaveChanged = null)
            {
                if (waveManager?.CurrentWave != null)
                {
                    LoadWaveForEditing(waveManager.CurrentWave);
                    onWaveChanged?.Invoke(currentEditingWave);
                }
                else
                {
                    Debug.LogWarning("No current wave to load");
                }
            }

            public void LoadWaveForEditing(WaveData wave)
            {
                if (wave == null) return;
                // Deep copy: create a new instance and copy all fields, including a new list for CubesData
                currentEditingWave = ScriptableObject.CreateInstance<WaveData>();
                currentEditingWave.name = wave.name;
                currentEditingWave.GridWidth = wave.GridWidth;
                currentEditingWave.GridHeight = wave.GridHeight;
                currentEditingWave.moveInterval = wave.moveInterval;
                currentEditingWave.fastMoveInterval = wave.fastMoveInterval;
                currentEditingWave.waveStartDelay = wave.waveStartDelay;
                currentEditingWave.maxLightMarkerCharge = wave.maxLightMarkerCount;
                currentEditingWave.maxLightMarkerCharge = wave.maxLightMarkerCharge;
                currentEditingWave.maxPrimeMarkerCount = wave.maxPrimeMarkerCount;
                currentEditingWave.maxPrimeMarkerCharge = wave.maxPrimeMarkerCharge;

                // Deep copy CubesData
                currentEditingWave.CubesData = new List<CubeData>();
                if (wave.CubesData != null)
                {
                    foreach (var cube in wave.CubesData)
                    {
                        var cubeCopy = new CubeData
                        {
                            type = cube.type,
                            position = new Vector2Int(cube.position.x, cube.position.y),
                            level = cube.level
                        };
                        currentEditingWave.CubesData.Add(cubeCopy);
                    }
                }

                hasUnsavedChanges = false;
                Debug.Log($"Loaded wave for editing: {currentEditingWave.name} with {currentEditingWave.CubesData.Count} cubes");
                waveManager.waveConfiguration.Clear();
                waveManager.waveConfiguration.Add( currentEditingWave );
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
                    Debug.Log($"Updated existing wave '{currentEditingWave.name}' with {currentEditingWave.CubesData.Count} cubes");
                }
                else
                {
                    // Create new asset
                    var saveWave = Object.Instantiate(currentEditingWave);
                    UnityEditor.AssetDatabase.CreateAsset(saveWave, assetPath);
                    Debug.Log($"Created new wave '{currentEditingWave.name}' with {currentEditingWave.CubesData.Count} cubes");
                }

                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.Refresh();
                hasUnsavedChanges = false;

                Debug.Log($"Wave '{currentEditingWave.name}' saved to {assetPath}");
#endif
            }

            // Helper methods
            private void MarkAsChanged()
            {
                hasUnsavedChanges = true;
            }

            private bool ConfirmUnsavedChanges()
            {
                if (!hasUnsavedChanges) return true;

#if UNITY_EDITOR
                return UnityEditor.EditorUtility.DisplayDialog("Unsaved Changes",
                    "You have unsaved changes. Continue without saving?", "Continue", "Cancel");
#else
                return true;
#endif
            }

            private Vector2Int ConvertGridToWavePosition(Vector2Int gridPosition)
            {
                // Convert from absolute grid coordinates to wave-relative coordinates
                // Wave Y=0 should be at the top of the wave area (grid height - 1)
                int waveY = (gridManager.Height - 1) - gridPosition.y;
                return new Vector2Int(gridPosition.x, waveY);
            }

            private Vector2Int ConvertWaveToGridPosition(Vector2Int wavePosition)
            {
                // Convert from wave-relative coordinates to absolute grid coordinates
                int gridY = (gridManager.Height - 1) - wavePosition.y;
                return new Vector2Int(wavePosition.x, gridY);
            }

            private void CaptureCurrentGridState()
            {
                if (currentEditingWave == null) return;

                // Get all active cubes from the scene
                var activeCubes = Object.FindObjectsOfType<CubeManager>()
                    .Where(c => c != null && !c.isDestroyed).ToList();

                // Clear current wave data
                currentEditingWave.CubesData.Clear();

                // Convert each cube to wave data
                foreach (var cube in activeCubes)
                {
                    Vector2Int wavePosition = ConvertGridToWavePosition(cube.position);

                    var cubeData = new CubeData
                    {
                        type = cube.type,
                        position = wavePosition,
                        level = cube.level
                    };
                    currentEditingWave.CubesData.Add(cubeData);
                }

                MarkAsChanged();
                Debug.Log($"Captured {currentEditingWave.CubesData.Count} cubes from grid to wave '{currentEditingWave.name}'");
            }

            private void TestCurrentWave()
            {
                if (currentEditingWave == null || waveManager == null) return;

                if (currentEditingWave.CubesData.Count == 0)
                {
                    Debug.LogWarning("Wave has no cubes configured!");
                    return;
                }

                // Clear existing wave configuration
                waveManager.waveConfiguration.Clear();

                // Add current wave (make a copy to avoid modifying the editing wave)
                var testWave = Object.Instantiate(currentEditingWave);
                waveManager.waveConfiguration.Add(testWave);

                // Set up wave manager
                waveManager.useWaveConfiguration = true;
                waveManager.currentWaveIndex = 0;
                waveManager.StartWave();

                Debug.Log($"Testing wave '{currentEditingWave.name}' with {currentEditingWave.CubesData.Count} cubes");
            }

            private void DuplicateCurrentWave()
            {
                if (currentEditingWave == null) return;

                var duplicate = Object.Instantiate(currentEditingWave);
                duplicate.name = currentEditingWave.name + "_Copy";
                currentEditingWave = duplicate;
                hasUnsavedChanges = true;

                Debug.Log($"Duplicated wave as '{currentEditingWave.name}'");
            }

            private void ResetCubePositionsToTop()
            {
                if (currentEditingWave == null) return;

                foreach (var cube in currentEditingWave.CubesData)
                {
                    // Move all cubes to the top row of the wave (Y=0 in wave space)
                    cube.position.y = 0;
                }

                Debug.Log($"Reset {currentEditingWave.CubesData.Count} cubes to top of wave");
            }

            private void ClampCubesToWaveBounds()
            {
                if (currentEditingWave == null) return;

                int removed = currentEditingWave.CubesData.RemoveAll(c =>
                    c.position.x >= currentEditingWave.GridWidth ||
                    c.position.y >= currentEditingWave.GridHeight);

                if (removed > 0)
                {
                    Debug.Log($"Removed {removed} cubes that were outside wave bounds");
                }
            }

            private WaveComposition AnalyzeWaveComposition()
            {
                var stats = new WaveComposition();

                if (currentEditingWave.CubesData.Count == 0) return stats;

                stats.minX = currentEditingWave.CubesData.Min(c => c.position.x);
                stats.maxX = currentEditingWave.CubesData.Max(c => c.position.x);
                stats.minY = currentEditingWave.CubesData.Min(c => c.position.y);
                stats.maxY = currentEditingWave.CubesData.Max(c => c.position.y);

                foreach (var cube in currentEditingWave.CubesData)
                {
                    switch (cube.type)
                    {
                        case CubeType.Unit: stats.normalCount++; break;
                        case CubeType.Prime: stats.blueCount++; break;
                        case CubeType.Infinity: stats.blackCount++; break;
                        case CubeType.Recursion: stats.reinforcedCount++; break;
                    }
                }

                return stats;
            }

            private struct WaveComposition
            {
                public int normalCount, blueCount, blackCount, reinforcedCount;
                public int minX, maxX, minY, maxY;
            }

            // Fast Testing Mode enhancements
            private void QuickTestCurrentWave()
            {
                if (currentEditingWave == null || waveManager == null) return;

                // Store current message state
                bool originalShowMessages = waveManager.showMessages;
                
                try
                {
                    // Disable messages for quick test
                    waveManager.showMessages = false;
                    
                    // Test the wave
                    TestCurrentWave();
                    
                    Debug.Log($"🚀 Quick test started for wave '{currentEditingWave.name}' (messages disabled)");
                }
                finally
                {
                    // Restore message state
                    waveManager.showMessages = originalShowMessages;
                }
            }

            private void FillGridWithCubes()
            {
                if (currentEditingWave == null) return;

                currentEditingWave.CubesData.Clear();
                
                // Fill the entire wave grid with normal cubes
                for (int x = 0; x < currentEditingWave.GridWidth; x++)
                {
                    for (int y = 0; y < currentEditingWave.GridHeight; y++)
                    {
                        currentEditingWave.CubesData.Add(new CubeData
                        {
                            type = Enumerations.CubeType.Unit,
                            position = new Vector2Int(x, y),
                            level = 1
                        });
                    }
                }
                
                MarkAsChanged();
                Debug.Log($"🧩 Filled wave '{currentEditingWave.name}' with {currentEditingWave.CubesData.Count} normal cubes");
            }

            private void GenerateTestPattern()
            {
                if (currentEditingWave == null) return;

                currentEditingWave.CubesData.Clear();
                
                // Create a checkerboard pattern in the top rows
                int patternHeight = Mathf.Min(3, currentEditingWave.GridHeight);
                
                for (int y = currentEditingWave.GridHeight - patternHeight; y < currentEditingWave.GridHeight; y++)
                {
                    for (int x = 0; x < currentEditingWave.GridWidth; x++)
                    {
                        if ((x + y) % 2 == 0)
                        {
                            Enumerations.CubeType cubeType = Enumerations.CubeType.Unit;
                            if (x % 3 == 1) cubeType = Enumerations.CubeType.Prime;
                            else if (x % 3 == 2) cubeType = Enumerations.CubeType.Infinity;
                            
                            currentEditingWave.CubesData.Add(new CubeData
                            {
                                type = cubeType,
                                position = new Vector2Int(x, y - (currentEditingWave.GridHeight - patternHeight)),
                                level = 1
                            });
                        }
                    }
                }
                
                MarkAsChanged();
                Debug.Log($"🎨 Generated test pattern for wave '{currentEditingWave.name}' with {currentEditingWave.CubesData.Count} cubes");
            }

            private void GenerateRandomWave()
            {
                if (currentEditingWave == null) return;

                currentEditingWave.CubesData.Clear();
                
                // Generate random cubes (about 30-60% coverage)
                int targetCubes = Random.Range(
                    (currentEditingWave.GridWidth * currentEditingWave.GridHeight) / 3,
                    (currentEditingWave.GridWidth * currentEditingWave.GridHeight) * 2 / 3
                );
                
                var occupiedPositions = new HashSet<Vector2Int>();
                
                for (int i = 0; i < targetCubes; i++)
                {
                    Vector2Int position;
                    int attempts = 0;
                    do
                    {
                        position = new Vector2Int(
                            Random.Range(0, currentEditingWave.GridWidth),
                            Random.Range(0, currentEditingWave.GridHeight)
                        );
                        attempts++;
                    } while (occupiedPositions.Contains(position) && attempts < 100);
                    
                    if (!occupiedPositions.Contains(position))
                    {
                        occupiedPositions.Add(position);
                        
                        // Random cube type with weighted distribution
                        Enumerations.CubeType cubeType = Enumerations.CubeType.Unit;
                        float random = Random.value;
                        if (random < 0.7f) cubeType = Enumerations.CubeType.Unit;
                        else if (random < 0.9f) cubeType = Enumerations.CubeType.Prime;
                        else cubeType = Enumerations.CubeType.Infinity;
                        
                        currentEditingWave.CubesData.Add(new CubeData
                        {
                            type = cubeType,
                            position = position,
                            level = 1
                        });
                    }
                }
                
                MarkAsChanged();
                Debug.Log($"🎲 Generated random wave '{currentEditingWave.name}' with {currentEditingWave.CubesData.Count} cubes");
            }

            private void OptimizeWave()
            {
                if (currentEditingWave == null || currentEditingWave.CubesData.Count == 0) return;

                int originalCount = currentEditingWave.CubesData.Count;
                
                // Remove duplicate cubes at same position
                var uniqueCubes = new Dictionary<Vector2Int, CubeData>();
                foreach (var cube in currentEditingWave.CubesData)
                {
                    uniqueCubes[cube.position] = cube;
                }
                
                currentEditingWave.CubesData.Clear();
                currentEditingWave.CubesData.AddRange(uniqueCubes.Values);
                
                // Sort cubes by position for consistent ordering
                currentEditingWave.CubesData.Sort((a, b) => 
                {
                    int yCompare = a.position.y.CompareTo(b.position.y);
                    return yCompare != 0 ? yCompare : a.position.x.CompareTo(b.position.x);
                });
                
                // Optimize timing values to reasonable ranges
                currentEditingWave.moveInterval = Mathf.Clamp(currentEditingWave.moveInterval, 0.5f, 5.0f);
                currentEditingWave.fastMoveInterval = Mathf.Clamp(currentEditingWave.fastMoveInterval, 0.05f, 1.0f);
                currentEditingWave.waveStartDelay = Mathf.Clamp(currentEditingWave.waveStartDelay, 0f, 3.0f);
                
                MarkAsChanged();
                int removedDuplicates = originalCount - currentEditingWave.CubesData.Count;
                Debug.Log($"⚙️ Optimized wave '{currentEditingWave.name}' - removed {removedDuplicates} duplicates, sorted {currentEditingWave.CubesData.Count} cubes");
            }
        }
    }
}
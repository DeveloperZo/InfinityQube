using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Enumerations;

namespace WaveDebugSystem
{
    public class CubeToolsDebug
    {
        private WaveManager waveManager;
        private GridManager gridManager;

        // UI State
        private int selectedCubeType = 0; // Normal
        private bool isPlacementMode = false;
        private bool showActiveCubes = true;
        private bool liveEditMode = true;
        private Vector2 cubeListScroll;
        private Vector2 activeCubeScroll;

        // Selection state
        private CubeManager selectedCube = null;

        public void Initialize(WaveManager waveManager, GridManager gridManager)
        {
            this.waveManager = waveManager;
            this.gridManager = gridManager;
        }

        public void DrawPanel(WaveData currentEditingWave, System.Action onSyncToGrid = null)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("CUBE TOOLS", GUI.skin.box);

            if (currentEditingWave != null)
            {
                DrawCubeTypeSelector();
                DrawPlacementControls(currentEditingWave, onSyncToGrid);
                DrawCubeGrid(currentEditingWave, onSyncToGrid);
                GUILayout.Space(5);
                DrawCubeList(currentEditingWave, onSyncToGrid);
            }
            else
            {
                GUILayout.Label("Create or load a wave to edit cubes");
                if (GUILayout.Button("Quick New Wave"))
                {
                    // This would need to be handled by the parent panel
                    Debug.Log("Quick new wave requested");
                }
            }

            GUILayout.EndVertical();
        }

        private void DrawEditModeControls()
        {
            GUILayout.BeginHorizontal();
            liveEditMode = GUILayout.Toggle(liveEditMode, "Live Edit Mode");
            showActiveCubes = GUILayout.Toggle(showActiveCubes, "Show Active Cubes");
            GUILayout.EndHorizontal();

            if (liveEditMode)
            {
                GUILayout.Label("Live Mode: Changes apply immediately to active cubes");
            }
        }


        private void DrawCombinedGrid(WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            // Get both wave cubes and active cubes
            var waveCubes = currentEditingWave.CubesData;
            var activeCubes = GetActiveCubesAsData();

            GUILayout.Label($"Wave Grid ({currentEditingWave.GridWidth}x{currentEditingWave.GridHeight}) - Active Cubes: {activeCubes.Count}");

            // Show grid coordinates header
            GUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(25)); // Space for row labels
            for (int x = 0; x < currentEditingWave.GridWidth; x++)
            {
                GUILayout.Label($"{x}", GUILayout.Width(25));
            }
            GUILayout.EndHorizontal();

            // Grid representation (top to bottom) - Show actual grid layout
            for (int y = currentEditingWave.GridHeight - 1; y >= 0; y--)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{y}:", GUILayout.Width(25));

                for (int x = 0; x < currentEditingWave.GridWidth; x++)
                {
                    DrawGridCell(x, y, waveCubes, activeCubes, currentEditingWave, onSyncToGrid);
                }

                GUILayout.EndHorizontal();
            }

            // Show combined summary
            if (waveCubes.Count > 0 || activeCubes.Count > 0)
            {
                GUILayout.Space(3);
                GUILayout.Label($"Wave Cubes: {waveCubes.Count} | Active Cubes: {activeCubes.Count}");

                if (selectedCube != null)
                {
                    GUILayout.Label($"Selected: {selectedCube.type} at ({selectedCube.position.x}, {selectedCube.position.y})");
                }
            }
        }

        private void DrawGridCell(int x, int y, List<CubeData> waveCubes, List<CubeData> activeCubes,
                                 WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            var waveCube = waveCubes.FirstOrDefault(c => c.position.x == x && c.position.y == y);
            var activeCube = activeCubes.FirstOrDefault(c => c.position.x == x && c.position.y == y);

            Color buttonColor = Color.white;
            string buttonText = "·";

            // Prioritize active cubes over wave cubes
            if (activeCube != null)
            {
                buttonColor = GetCubeColor(activeCube.type);
                buttonText = GetCubeSymbol(activeCube.type);

                // Add border for active cubes
                if (showActiveCubes)
                {
                    buttonColor = Color.Lerp(buttonColor, Color.yellow, 0.3f);
                }
            }
            else if (waveCube != null)
            {
                buttonColor = GetCubeColor(waveCube.type);
                buttonText = GetCubeSymbol(waveCube.type);

                // Slightly faded for wave-only cubes
                buttonColor = Color.Lerp(buttonColor, Color.white, 0.4f);
            }

            GUI.backgroundColor = buttonColor;
            if (GUILayout.Button(buttonText, GUILayout.Width(25), GUILayout.Height(25)))
            {
                HandleGridClick(x, y, waveCube, activeCube, currentEditingWave, onSyncToGrid);
            }
            GUI.backgroundColor = Color.white;
        }

        private void HandleGridClick(int x, int y, CubeData waveCube, CubeData activeCube,
                                   WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            if (isPlacementMode)
            {
                // Placement mode - add/remove cubes
                if (waveCube != null)
                {
                    // Remove from wave
                    currentEditingWave.CubesData.Remove(waveCube);
                }
                else
                {
                    // Add to wave
                    var newCube = new CubeData
                    {
                        type = (CubeType)selectedCubeType,
                        position = new Vector2Int(x, y),
                        level = 1
                    };
                    currentEditingWave.CubesData.Add(newCube);
                }
                onSyncToGrid?.Invoke();
            }
            else
            {
                // Selection mode - select active cube for editing
                if (activeCube != null)
                {
                    var activeCubeManager = FindActiveCubeAt(x, y);
                    if (activeCubeManager != null)
                    {
                        selectedCube = activeCubeManager;
                        Debug.Log($"Selected {activeCubeManager.type} cube at ({x}, {y})");
                    }
                }
            }
        }

        private void DrawCubeInspector(WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("CUBE INSPECTOR", GUI.skin.box);

            if (selectedCube != null && !selectedCube.isDestroyed)
            {
                DrawSelectedCubeInspector();
            }
            else
            {
                selectedCube = null;
                GUILayout.Label("No cube selected. Click on an active cube to inspect/edit.");
            }

            GUILayout.Space(5);
            DrawCubeLists(currentEditingWave, onSyncToGrid);

            GUILayout.EndVertical();
        }

        private void DrawSelectedCubeInspector()
        {
            GUILayout.Label($"Selected: {selectedCube.type} at ({selectedCube.position.x}, {selectedCube.position.y})");

            // Cube type changer
            GUILayout.BeginHorizontal();
            GUILayout.Label("Change Type:");
            if (GUILayout.Button("Normal")) ChangeCubeType(selectedCube, CubeType.Normal);
            if (GUILayout.Button("Blue")) ChangeCubeType(selectedCube, CubeType.Blue);
            if (GUILayout.Button("Black")) ChangeCubeType(selectedCube, CubeType.Black);
            if (GUILayout.Button("Reinforced")) ChangeCubeType(selectedCube, CubeType.Reinforced);
            GUILayout.EndHorizontal();

            // Position controls
            GUILayout.BeginHorizontal();
            GUILayout.Label("Position:");
            if (GUILayout.Button("↑") && selectedCube.position.y < gridManager.Height - 1)
                MoveCube(selectedCube, 0, 1);
            if (GUILayout.Button("↓") && selectedCube.position.y > 0)
                MoveCube(selectedCube, 0, -1);
            if (GUILayout.Button("←") && selectedCube.position.x > 0)
                MoveCube(selectedCube, -1, 0);
            if (GUILayout.Button("→") && selectedCube.position.x < gridManager.Width - 1)
                MoveCube(selectedCube, 1, 0);
            GUILayout.EndHorizontal();

            // Face painting controls
            GUILayout.Label("Face Status:");
            var activeFaceStatus = selectedCube.GetActiveFaceStatus();
            GUILayout.Label($"Current Down Face: {selectedCube.GetCurrentDownFace()} ({activeFaceStatus})");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Paint Corrupt"))
                selectedCube.PaintCurrentDownFace(FaceStatus.Corrupted, Color.red, 5);
            if (GUILayout.Button("Paint Enhance"))
                selectedCube.PaintCurrentDownFace(FaceStatus.Enhanced, Color.blue, 5);
            if (GUILayout.Button("Clear Paint"))
                selectedCube.PaintCurrentDownFace(FaceStatus.None, Color.white, 0);
            GUILayout.EndHorizontal();

            // Debug buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Show All Faces")) selectedCube.DebugShowAllFaces();
            if (GUILayout.Button("Print Mapping")) selectedCube.DebugPrintFaceMapping();
            if (GUILayout.Button("Destroy")) DestroyCube(selectedCube);
            GUILayout.EndHorizontal();
        }

        private void DrawCubeLists(WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            GUILayout.BeginHorizontal();

            // Wave cubes list
            GUILayout.BeginVertical(GUILayout.Width(200));
            GUILayout.Label($"Wave Cubes ({currentEditingWave.CubesData.Count}):");
            cubeListScroll = GUILayout.BeginScrollView(cubeListScroll, GUILayout.Height(120));
            DrawWaveCubesList(currentEditingWave, onSyncToGrid);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            // Active cubes list
            GUILayout.BeginVertical(GUILayout.Width(200));
            var activeCubes = GetActiveCubes();
            GUILayout.Label($"Active Cubes ({activeCubes.Count}):");
            activeCubeScroll = GUILayout.BeginScrollView(activeCubeScroll, GUILayout.Height(120));
            DrawActiveCubesList(activeCubes);
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawWaveCubesList(WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            for (int i = currentEditingWave.CubesData.Count - 1; i >= 0; i--)
            {
                var cube = currentEditingWave.CubesData[i];
                GUILayout.BeginHorizontal();

                GUI.backgroundColor = GetCubeColor(cube.type);
                GUILayout.Label(GetCubeSymbol(cube.type), GUILayout.Width(20));
                GUI.backgroundColor = Color.white;

                GUILayout.Label($"{cube.type} ({cube.position.x},{cube.position.y})", GUILayout.Width(100));

                if (GUILayout.Button("Del", GUILayout.Width(30)))
                {
                    currentEditingWave.CubesData.RemoveAt(i);
                    onSyncToGrid?.Invoke();
                }

                GUILayout.EndHorizontal();
            }
        }

        private void DrawActiveCubesList(List<CubeManager> activeCubes)
        {
            foreach (var cube in activeCubes.Take(10)) // Limit display
            {
                if (cube == null || cube.isDestroyed) continue;

                GUILayout.BeginHorizontal();

                GUI.backgroundColor = cube == selectedCube ? Color.yellow : GetCubeColor(cube.type);
                if (GUILayout.Button(GetCubeSymbol(cube.type), GUILayout.Width(20)))
                {
                    selectedCube = cube;
                }
                GUI.backgroundColor = Color.white;

                string effectiveType = cube.GetEffectiveType().ToString();
                if (effectiveType != cube.type.ToString())
                {
                    effectiveType = $"{cube.type}→{effectiveType}";
                }

                GUILayout.Label($"{effectiveType} ({cube.position.x},{cube.position.y})", GUILayout.Width(120));

                GUILayout.EndHorizontal();
            }
        }

        // Cube manipulation methods
        private void ChangeCubeType(CubeManager cube, CubeType newType)
        {
            if (cube == null || cube.isDestroyed) return;

            // Store position and other data
            var position = cube.position;
            var level = cube.level;

            // Destroy old cube
            waveManager.activeCubes.Remove(cube);
            Object.Destroy(cube.gameObject);

            // Create new cube of different type
            if (waveManager.cubePrefabs != null && (int)newType < waveManager.cubePrefabs.Length)
            {
                Vector3 worldPos = gridManager.GridToWorldPosition(position.x, position.y, 2f);
                GameObject newCubeObj = Object.Instantiate(waveManager.cubePrefabs[(int)newType], worldPos, Quaternion.identity);

                var newCube = newCubeObj.GetComponent<CubeManager>();
                if (newCube == null) newCube = newCubeObj.AddComponent<CubeManager>();

                var cubeData = new CubeData { type = newType, position = position, level = level };
                newCube.Init(gridManager, cubeData, 2f);
                waveManager.activeCubes.Add(newCube);

                selectedCube = newCube;
                Debug.Log($"Changed cube at ({position.x}, {position.y}) to {newType}");
            }
        }

        private void MoveCube(CubeManager cube, int deltaX, int deltaY)
        {
            if (cube == null || cube.isDestroyed) return;

            Vector2Int newPos = new Vector2Int(cube.position.x + deltaX, cube.position.y + deltaY);
            if (!gridManager.IsValidGridPosition(newPos)) return;

            cube.position = newPos;
            Vector3 worldPos = gridManager.GridToWorldPosition(newPos.x, newPos.y, 2f);
            cube.transform.position = worldPos;

            Debug.Log($"Moved cube to ({newPos.x}, {newPos.y})");
        }

        private void DestroyCube(CubeManager cube)
        {
            if (cube == null || cube.isDestroyed) return;

            waveManager.activeCubes.Remove(cube);
            Object.Destroy(cube.gameObject);
            selectedCube = null;

            Debug.Log("Destroyed selected cube");
        }

        // Helper methods
        private List<CubeManager> GetActiveCubes()
        {
            return waveManager?.activeCubes?.Where(c => c != null && !c.isDestroyed).ToList() ?? new List<CubeManager>();
        }

        private List<CubeData> GetActiveCubesAsData()
        {
            var activeCubes = new List<CubeData>();
            if (waveManager != null)
            {
                foreach (var cube in waveManager.activeCubes)
                {
                    if (cube != null && !cube.isDestroyed)
                    {
                        activeCubes.Add(new CubeData
                        {
                            type = cube.type,
                            position = cube.position,
                            level = cube.level
                        });
                    }
                }
            }
            return activeCubes;
        }

        private CubeManager FindActiveCubeAt(int x, int y)
        {
            return GetActiveCubes().FirstOrDefault(c => c.position.x == x && c.position.y == y);
        }

        private void ClearAllCubes(WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            currentEditingWave.CubesData.Clear();
            waveManager?.ClearAllCubes();
            selectedCube = null;
            onSyncToGrid?.Invoke();
        }

        private void DrawCubeTypeSelector()
        {
            GUILayout.Label("Cube Type:");
            GUILayout.BeginHorizontal();
            if (DrawCubeTypeButton("Normal", 0, Color.gray)) selectedCubeType = 0;
            if (DrawCubeTypeButton("Blue", 1, Color.blue)) selectedCubeType = 1;
            if (DrawCubeTypeButton("Black", 2, Color.black)) selectedCubeType = 2;
            if (DrawCubeTypeButton("Reinforced", 3, Color.magenta)) selectedCubeType = 3;
            GUILayout.EndHorizontal();
        }

        private void DrawPlacementControls(WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            GUILayout.BeginHorizontal();
            isPlacementMode = GUILayout.Toggle(isPlacementMode, "Placement Mode");
            if (GUILayout.Button("Fill Random")) FillWaveRandom(currentEditingWave, onSyncToGrid);
            if (GUILayout.Button("Fill Top Row")) FillTopRow(currentEditingWave, onSyncToGrid);
            GUILayout.EndHorizontal();

            if (isPlacementMode)
            {
                GUILayout.Label("Click grid below to place/remove cubes");
            }

            if (waveManager != null && waveManager.activeCubes.Count > 0)
            {
                var bounds = GetCubeBounds(GetDisplayCubes(currentEditingWave));
                GUILayout.Label($"Active cube bounds: ({bounds.min.x},{bounds.min.y}) to ({bounds.max.x},{bounds.max.y})");
            }
        }

        private void DrawCubeGrid(WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            var cubesToShow = GetDisplayCubes(currentEditingWave);

            // Determine grid dimensions to show
            int gridWidth, gridHeight;
            int startY = 0;
            gridWidth = currentEditingWave.GridWidth;
            gridHeight = currentEditingWave.GridHeight;
            startY = 0;

            GUILayout.Label($"Grid Editor ({gridWidth}x{gridHeight}):");


            // Show grid coordinates header
            GUILayout.BeginHorizontal();
            GUILayout.Label("", GUILayout.Width(25)); // Space for row labels
            for (int x = 0; x < gridWidth; x++)
            {
                GUILayout.Label($"{x}", GUILayout.Width(25));
            }
            GUILayout.EndHorizontal();

            // Grid representation (top to bottom)
            for (int y = startY + gridHeight - 1; y >= startY; y--)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{y}:", GUILayout.Width(25));

                for (int x = 0; x < gridWidth; x++)
                {
                    var cubeAtPos = cubesToShow.FirstOrDefault(c => c.position.x == x && c.position.y == y);

                    Color buttonColor = Color.white;
                    string buttonText = "·";

                    if (cubeAtPos != null)
                    {
                        switch (cubeAtPos.type)
                        {
                            case CubeType.Normal: buttonColor = Color.gray; buttonText = "N"; break;
                            case CubeType.Blue: buttonColor = Color.blue; buttonText = "B"; break;
                            case CubeType.Black: buttonColor = Color.black; buttonText = "X"; break;
                            case CubeType.Reinforced: buttonColor = Color.magenta; buttonText = "R"; break;
                        }
                    }

                    // Highlight out-of-bounds positions when tracking
                    if ((x >= currentEditingWave.GridWidth || y >= currentEditingWave.GridHeight))
                    {
                        buttonColor = Color.red;
                        buttonText = "!";
                    }

                    GUI.backgroundColor = buttonColor;
                    if (GUILayout.Button(buttonText, GUILayout.Width(25), GUILayout.Height(25)))
                    {
                        if (isPlacementMode && x < currentEditingWave.GridWidth && y < currentEditingWave.GridHeight)
                        {
                            HandleGridClick(x, y, currentEditingWave, onSyncToGrid);
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }

            // Show cube positions summary
            if (cubesToShow.Count > 0)
            {
                GUILayout.Space(3);
                var cubesByRow = cubesToShow.GroupBy(c => c.position.y).OrderByDescending(g => g.Key);
                GUILayout.Label($"Cubes by row:");
                foreach (var rowGroup in cubesByRow.Take(3)) // Show only top 3 rows to save space
                {
                    string cubeTypes = string.Join(", ", rowGroup.Select(c => $"{GetCubeSymbol(c.type)}@{c.position.x}"));
                    GUILayout.Label($"  Row {rowGroup.Key}: {cubeTypes}");
                }
                if (cubesByRow.Count() > 3)
                {
                    GUILayout.Label($"  ... and {cubesByRow.Count() - 3} more rows");
                }
            }
        }

        private void DrawCubeList(WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            GUILayout.Label($"Cubes ({currentEditingWave.CubesData.Count}):");

            cubeListScroll = GUILayout.BeginScrollView(cubeListScroll, GUILayout.Height(100));

            for (int i = currentEditingWave.CubesData.Count - 1; i >= 0; i--)
            {
                var cube = currentEditingWave.CubesData[i];
                GUILayout.BeginHorizontal();

                // Type indicator
                GUI.backgroundColor = GetCubeColor(cube.type);
                GUILayout.Label(GetCubeSymbol(cube.type), GUILayout.Width(20));
                GUI.backgroundColor = Color.white;

                GUILayout.Label($"{cube.type} at ({cube.position.x},{cube.position.y})", GUILayout.Width(120));

                // Quick type change buttons
                if (GUILayout.Button("N", GUILayout.Width(20))) cube.type = CubeType.Normal;
                if (GUILayout.Button("B", GUILayout.Width(20))) cube.type = CubeType.Blue;
                if (GUILayout.Button("X", GUILayout.Width(20))) cube.type = CubeType.Black;

                if (GUILayout.Button("Del", GUILayout.Width(30)))
                {
                    currentEditingWave.CubesData.RemoveAt(i);
                    onSyncToGrid?.Invoke();
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
        }

        private bool DrawCubeTypeButton(string label, int type, Color color)
        {
            GUI.backgroundColor = selectedCubeType == type ? color : Color.white;
            bool result = GUILayout.Button(label);
            GUI.backgroundColor = Color.white;
            return result;
        }

        private void HandleGridClick(int x, int y, WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            var existingCube = currentEditingWave.CubesData.FirstOrDefault(c => c.position.x == x && c.position.y == y);

            if (existingCube != null)
            {
                // Remove existing cube
                currentEditingWave.CubesData.Remove(existingCube);
            }
            else
            {
                // Add new cube
                var newCube = new CubeData
                {
                    type = (CubeType)selectedCubeType,
                    position = new Vector2Int(x, y),
                    level = 1
                };
                currentEditingWave.CubesData.Add(newCube);
            }

            onSyncToGrid?.Invoke();
        }

        private void FillWaveRandom(WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            if (currentEditingWave == null) return;

            currentEditingWave.CubesData.Clear();

            for (int x = 0; x < currentEditingWave.GridWidth; x++)
            {
                for (int y = 0; y < currentEditingWave.GridHeight; y++)
                {
                    if (Random.value < 0.6f) // 60% chance to place a cube
                    {
                        var cubeData = new CubeData
                        {
                            type = (CubeType)Random.Range(0, 3), // Normal, Blue, Black only
                            position = new Vector2Int(x, y),
                            level = 1
                        };
                        currentEditingWave.CubesData.Add(cubeData);
                    }
                }
            }

            onSyncToGrid?.Invoke();
        }

        private void FillTopRow(WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            if (currentEditingWave == null) return;

            // Use the top of the grid (gridHeight - 1) instead of 0
            int topRow = currentEditingWave.GridHeight - 1;

            // Remove existing cubes in top row
            currentEditingWave.CubesData.RemoveAll(c => c.position.y == topRow);

            // Add cubes across top row
            for (int x = 0; x < currentEditingWave.GridWidth; x++)
            {
                var cubeData = new CubeData
                {
                    type = (CubeType)selectedCubeType,
                    position = new Vector2Int(x, topRow),
                    level = 1
                };
                currentEditingWave.CubesData.Add(cubeData);
            }

            onSyncToGrid?.Invoke();
        }

        private List<CubeData> GetDisplayCubes(WaveData currentEditingWave)
        {
            if (currentEditingWave != null)
            {
                return currentEditingWave.CubesData;
            }

            // Fallback: Show active cubes from manager, but adjust positions to wave coordinates
            var activeCubes = new List<CubeData>();
            if (waveManager != null && waveManager.activeCubes.Count > 0)
            {
                foreach (var cube in waveManager.activeCubes)
                {
                    if (cube != null && !cube.isDestroyed)
                    {
                        activeCubes.Add(new CubeData
                        {
                            type = cube.type,
                            position = cube.position,
                            level = cube.level
                        });
                    }
                }
            }
            return activeCubes;
        }

        // Helper method to get the bounds of cubes in a wave
        private (Vector2Int min, Vector2Int max) GetCubeBounds(List<CubeData> cubes)
        {
            if (cubes.Count == 0) return (Vector2Int.zero, Vector2Int.zero);

            Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
            Vector2Int max = new Vector2Int(int.MinValue, int.MinValue);

            foreach (var cube in cubes)
            {
                if (cube.position.x < min.x) min.x = cube.position.x;
                if (cube.position.y < min.y) min.y = cube.position.y;
                if (cube.position.x > max.x) max.x = cube.position.x;
                if (cube.position.y > max.y) max.y = cube.position.y;
            }

            return (min, max);
        }

        private Color GetCubeColor(CubeType type)
        {
            switch (type)
            {
                case CubeType.Normal: return Color.gray;
                case CubeType.Blue: return Color.blue;
                case CubeType.Black: return Color.black;
                case CubeType.Reinforced: return Color.magenta;
                default: return Color.white;
            }
        }

        private string GetCubeSymbol(CubeType type)
        {
            switch (type)
            {
                case CubeType.Normal: return "N";
                case CubeType.Blue: return "B";
                case CubeType.Black: return "X";
                case CubeType.Reinforced: return "R";
                default: return "?";
            }
        }

        public int GetSelectedCubeType() => selectedCubeType;
        public void SetSelectedCubeType(int type) => selectedCubeType = type;
        public bool GetPlacementMode() => isPlacementMode;
        public void SetPlacementMode(bool mode) => isPlacementMode = mode;
    }
}
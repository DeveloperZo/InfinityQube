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
        private bool trackActiveCubes = false;
        private Vector2 cubeListScroll;

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
            trackActiveCubes = GUILayout.Toggle(trackActiveCubes, "Track Active");
            if (GUILayout.Button("Fill Random")) FillWaveRandom(currentEditingWave, onSyncToGrid);
            if (GUILayout.Button("Fill Top Row")) FillTopRow(currentEditingWave, onSyncToGrid);
            GUILayout.EndHorizontal();

            if (isPlacementMode)
            {
                GUILayout.Label("Click grid below to place/remove cubes");
            }

            if (trackActiveCubes && waveManager != null && waveManager.activeCubes.Count > 0)
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

            if (trackActiveCubes && cubesToShow.Count > 0)
            {
                // Show a view focused on where the cubes actually are
                var bounds = GetCubeBounds(cubesToShow);
                gridWidth = Mathf.Max(bounds.max.x - bounds.min.x + 3, currentEditingWave.GridWidth); // Add padding
                gridHeight = Mathf.Max(bounds.max.y - bounds.min.y + 3, 5); // Show at least 5 rows
                startY = Mathf.Max(0, bounds.min.y - 1); // Start a bit above the lowest cube

                GUILayout.Label($"Grid View (Tracking) - Showing rows {startY} to {startY + gridHeight - 1}:");
            }
            else
            {
                // Show the full wave dimensions
                gridWidth = currentEditingWave.GridWidth;
                gridHeight = currentEditingWave.GridHeight;
                startY = 0;

                GUILayout.Label($"Grid Editor ({gridWidth}x{gridHeight}):");
            }

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
                    if (trackActiveCubes && (x >= currentEditingWave.GridWidth || y >= currentEditingWave.GridHeight))
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
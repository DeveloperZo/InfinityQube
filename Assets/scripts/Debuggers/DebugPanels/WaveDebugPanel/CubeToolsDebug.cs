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
            if (GUILayout.Button("Fill Random")) FillWaveRandom(currentEditingWave, onSyncToGrid);
            if (GUILayout.Button("Fill Top Row")) FillTopRow(currentEditingWave, onSyncToGrid);
            GUILayout.EndHorizontal();

            if (isPlacementMode)
            {
                GUILayout.Label("Click grid below to place/remove cubes");
            }
        }

        private void DrawCubeGrid(WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            GUILayout.Label("Grid Editor:");

            var cubesToShow = GetDisplayCubes(currentEditingWave);

            // Grid representation (top to bottom) - Fixed to show from top of grid
            for (int y = currentEditingWave.GridHeight - 1; y >= 0; y--)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{y}:", GUILayout.Width(15));

                for (int x = 0; x < currentEditingWave.GridWidth; x++)
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

                    GUI.backgroundColor = buttonColor;
                    if (GUILayout.Button(buttonText, GUILayout.Width(25), GUILayout.Height(25)))
                    {
                        if (isPlacementMode)
                        {
                            HandleGridClick(x, y, currentEditingWave, onSyncToGrid);
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
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
            // Show wave cubes if editing, or current active cubes if synced
            if (currentEditingWave != null)
            {
                return currentEditingWave.CubesData;
            }

            // Fallback to showing active cubes from manager
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
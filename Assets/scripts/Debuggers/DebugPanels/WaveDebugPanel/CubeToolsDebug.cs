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
        private bool showGridView = true;
        private bool showCubeInspector = true;
        private bool followCubes = true; // New: follow cubes as they move
        private Vector2 gridScroll;
        private Vector2 inspectorScroll;

        // Selection state
        private CubeManager selectedCube = null;
        private Vector2Int selectedGridPosition = new Vector2Int(-1, -1);
        private Vector2Int hoveredGridPosition = new Vector2Int(-1, -1);

        public void Initialize(WaveManager waveManager, GridManager gridManager)
        {
            this.waveManager = waveManager;
            this.gridManager = gridManager;
        }

        public void DrawPanel(WaveData currentEditingWave, System.Action onSyncToGrid = null)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("WAVE EDITOR", GUI.skin.box);

            DrawModeControls();
            DrawCubeTypeSelector();

            if (showGridView)
            {
                DrawCompactWaveEditor(currentEditingWave, onSyncToGrid);
            }

            if (showCubeInspector && selectedCube != null)
            {
                DrawCubeInspector();
            }

            GUILayout.EndVertical();
        }

        private void DrawModeControls()
        {
            GUILayout.BeginHorizontal();

            GUI.backgroundColor = isPlacementMode ? Color.green : Color.white;
            if (GUILayout.Button("Placement Mode"))
            {
                isPlacementMode = !isPlacementMode;
                if (isPlacementMode) selectedCube = null; // Clear selection in placement mode
            }

            GUI.backgroundColor = !isPlacementMode ? Color.cyan : Color.white;
            if (GUILayout.Button("Selection Mode"))
            {
                isPlacementMode = false;
            }

            GUI.backgroundColor = Color.white;

            showGridView = GUILayout.Toggle(showGridView, "Grid");
            showCubeInspector = GUILayout.Toggle(showCubeInspector, "Inspector");

            GUILayout.EndHorizontal();

            if (isPlacementMode)
            {
                GUILayout.Label("Click grid to place/remove cubes", GUI.skin.box);
            }
            else
            {
                GUILayout.Label("Click cubes to select and edit", GUI.skin.box);
            }
        }

        private void DrawCubeTypeSelector()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Type:", GUILayout.Width(35));

            if (DrawCubeTypeButton("Normal", 0, Color.gray)) selectedCubeType = 0;
            if (DrawCubeTypeButton("Blue", 1, new Color(0.3f, 0.6f, 1f))) selectedCubeType = 1;
            if (DrawCubeTypeButton("Black", 2, new Color(0.2f, 0.2f, 0.2f))) selectedCubeType = 2;
            if (DrawCubeTypeButton("Reinforced", 3, new Color(0.8f, 0.3f, 0.8f))) selectedCubeType = 3;

            GUILayout.EndHorizontal();
        }

        private void DrawCompactWaveEditor(WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            // Get all active cubes from the scene
            var activeCubes = GetActiveCubesDict();

            // Determine the wave dimensions to show
            int waveWidth = currentEditingWave?.GridWidth ?? 5;
            int waveHeight = currentEditingWave?.GridHeight ?? 3;

            // Show grid info
            GUILayout.Label($"Wave Grid ({waveWidth}x{waveHeight}) - Active Cubes: {activeCubes.Count}");

            // Show hovered position if any
            if (hoveredGridPosition.x >= 0)
            {
                // Calculate actual grid position (at top of grid)
                int actualY = gridManager.Height - 1 - hoveredGridPosition.y;
                GUILayout.Label($"Hover: Wave({hoveredGridPosition.x},{hoveredGridPosition.y}) → Grid({hoveredGridPosition.x},{actualY})", GUI.skin.box);
            }

            // Grid controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All", GUILayout.Width(70)))
            {
                waveManager?.ClearAllCubes();
                selectedCube = null;
            }
            if (GUILayout.Button("Spawn Test", GUILayout.Width(80)))
            {
                SpawnTestPattern();
            }
            if (GUILayout.Button("Fill Top Row", GUILayout.Width(90)))
            {
                FillTopRow(waveWidth);
            }
            GUILayout.EndHorizontal();

            // View mode toggle
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = followCubes ? Color.green : Color.white;
            if (GUILayout.Button("Follow Cubes"))
            {
                followCubes = true;
            }
            GUI.backgroundColor = !followCubes ? Color.cyan : Color.white;
            if (GUILayout.Button("Fixed Wave View"))
            {
                followCubes = false;
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            // Draw the compact grid
            gridScroll = GUILayout.BeginScrollView(gridScroll, GUILayout.MinHeight(400));

            if (followCubes)
            {
                DrawCompactWaveGrid(activeCubes, waveWidth, waveHeight, currentEditingWave, onSyncToGrid);
            }
            else
            {
                DrawFixedWaveGrid(activeCubes, waveWidth, waveHeight, currentEditingWave, onSyncToGrid);
            }

            GUILayout.EndScrollView();

            GUILayout.EndVertical();
        }

        private void DrawCompactWaveGrid(Dictionary<Vector2Int, CubeManager> activeCubes,
                                        int waveWidth, int waveHeight,
                                        WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            // Find the actual range of Y positions that contain cubes
            int minY = gridManager.Height - 1; // Start at top
            int maxY = gridManager.Height - 1; // Start at top

            foreach (var kvp in activeCubes)
            {
                if (kvp.Value != null && !kvp.Value.isDestroyed)
                {
                    minY = Mathf.Min(minY, kvp.Key.y);
                    maxY = Mathf.Max(maxY, kvp.Key.y);
                }
            }

            // Ensure we show at least the wave height from the top
            int topRow = gridManager.Height - 1;
            maxY = topRow; // Always show the top
            minY = Mathf.Min(minY, topRow - waveHeight + 1);

            // Calculate the display range
            int displayStartY = maxY;
            int displayEndY = minY;
            int displayHeight = displayStartY - displayEndY + 1;

            // Column headers
            GUILayout.BeginHorizontal();
            GUILayout.Label("Y\\X", GUILayout.Width(40));
            for (int x = 0; x < waveWidth; x++)
            {
                GUILayout.Label($"{x}", GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            // Draw the grid rows that contain cubes or are in the wave area
            for (int gridY = displayStartY; gridY >= displayEndY; gridY--)
            {
                GUILayout.BeginHorizontal();

                // Calculate wave Y (distance from top)
                int waveY = topRow - gridY;

                // Show both grid and wave Y coordinates
                GUI.color = (gridY >= topRow - waveHeight + 1) ? Color.white : Color.gray;
                GUILayout.Label($"G{gridY}/W{waveY}:", GUILayout.Width(40));
                GUI.color = Color.white;

                for (int x = 0; x < waveWidth; x++)
                {
                    Vector2Int gridPos = new Vector2Int(x, gridY);
                    Vector2Int wavePos = new Vector2Int(x, waveY);
                    DrawWaveGridCell(gridPos, wavePos, activeCubes, currentEditingWave, onSyncToGrid);
                }

                GUILayout.EndHorizontal();
            }

            // Show info about the view
            if (displayHeight > waveHeight)
            {
                GUILayout.Label($"Showing rows {displayEndY}-{displayStartY} (Wave area: top {waveHeight} rows)", GUI.skin.box);
            }

            // Show selection info with both coordinate systems
            if (selectedCube != null && !selectedCube.isDestroyed)
            {
                GUILayout.Space(5);
                GUI.color = Color.yellow;
                int waveY = gridManager.Height - 1 - selectedCube.position.y;
                GUILayout.Label($"Selected: {selectedCube.type} at Grid({selectedCube.position.x},{selectedCube.position.y}) = Wave({selectedCube.position.x},{waveY})");
                GUI.color = Color.white;
            }
        }

        private void DrawFixedWaveGrid(Dictionary<Vector2Int, CubeManager> activeCubes,
                                      int waveWidth, int waveHeight,
                                      WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            // Column headers
            GUILayout.BeginHorizontal();
            GUILayout.Label("Y\\X", GUILayout.Width(40));
            for (int x = 0; x < waveWidth; x++)
            {
                GUILayout.Label($"{x}", GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            // Draw only the wave height rows from the top
            int topRow = gridManager.Height - 1;

            for (int waveY = 0; waveY < waveHeight; waveY++)
            {
                GUILayout.BeginHorizontal();

                // Calculate actual grid Y position
                int gridY = topRow - waveY;

                // Show wave Y coordinate
                GUILayout.Label($"W{waveY}:", GUILayout.Width(40));

                for (int x = 0; x < waveWidth; x++)
                {
                    Vector2Int gridPos = new Vector2Int(x, gridY);
                    Vector2Int wavePos = new Vector2Int(x, waveY);
                    DrawWaveGridCell(gridPos, wavePos, activeCubes, currentEditingWave, onSyncToGrid);
                }

                GUILayout.EndHorizontal();
            }

            // Show selection info with both coordinate systems
            if (selectedCube != null && !selectedCube.isDestroyed)
            {
                GUILayout.Space(5);
                GUI.color = Color.yellow;
                int waveY = gridManager.Height - 1 - selectedCube.position.y;
                GUILayout.Label($"Selected: {selectedCube.type} at Grid({selectedCube.position.x},{selectedCube.position.y}) = Wave({selectedCube.position.x},{waveY})");
                GUI.color = Color.white;
            }
        }

        private void DrawWaveGridCell(Vector2Int gridPos, Vector2Int wavePos,
                                     Dictionary<Vector2Int, CubeManager> activeCubes,
                                     WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            bool hasCube = activeCubes.ContainsKey(gridPos);
            CubeManager cube = hasCube ? activeCubes[gridPos] : null;

            // Determine button appearance
            Color buttonColor = Color.white;
            string buttonText = "·";
            string tooltip = $"Wave({wavePos.x},{wavePos.y}) = Grid({gridPos.x},{gridPos.y})";

            if (cube != null)
            {
                buttonColor = GetCubeColor(cube.type);
                buttonText = GetCubeSymbol(cube.type);
                tooltip = $"{cube.type} at {tooltip}";

                // Highlight selected cube
                if (cube == selectedCube)
                {
                    buttonColor = Color.Lerp(buttonColor, Color.yellow, 0.5f);
                    buttonText = "[" + buttonText + "]";
                }

                // Show effective type if different
                var effectiveType = cube.GetEffectiveType();
                if (effectiveType != cube.type)
                {
                    buttonText += "*";
                }
            }

            // Handle hover
            Rect buttonRect = GUILayoutUtility.GetRect(40, 40, GUILayout.Width(40), GUILayout.Height(40));
            bool isHovered = buttonRect.Contains(Event.current.mousePosition);

            if (isHovered)
            {
                hoveredGridPosition = wavePos;
                buttonColor = Color.Lerp(buttonColor, Color.white, 0.3f);
            }

            // Draw the button with tooltip
            GUI.backgroundColor = buttonColor;
            GUIContent buttonContent = new GUIContent(buttonText, tooltip);
            if (GUI.Button(buttonRect, buttonContent))
            {
                HandleCellClick(gridPos, cube, currentEditingWave, onSyncToGrid);
            }
            GUI.backgroundColor = Color.white;
        }

        private void HandleCellClick(Vector2Int gridPos, CubeManager existingCube,
                                   WaveData currentEditingWave, System.Action onSyncToGrid)
        {
            if (isPlacementMode)
            {
                if (existingCube != null)
                {
                    // Remove cube
                    waveManager.activeCubes.Remove(existingCube);
                    Object.Destroy(existingCube.gameObject);
                    if (selectedCube == existingCube) selectedCube = null;
                }
                else
                {
                    // Place new cube at the grid position
                    SpawnCubeAt(gridPos, (CubeType)selectedCubeType);
                }
            }
            else
            {
                // Selection mode
                selectedCube = existingCube;
                selectedGridPosition = gridPos;
            }
        }

        private void DrawCubeInspector()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("CUBE INSPECTOR", GUI.skin.box);

            if (selectedCube == null || selectedCube.isDestroyed)
            {
                GUILayout.Label("No cube selected");
                selectedCube = null;
                GUILayout.EndVertical();
                return;
            }

            inspectorScroll = GUILayout.BeginScrollView(inspectorScroll, GUILayout.MaxHeight(300));

            // Basic info with both coordinate systems
            GUILayout.Label($"Type: {selectedCube.type}", GUI.skin.box);

            // Calculate wave Y position
            int waveY = gridManager.Height - 1 - selectedCube.position.y;
            GUILayout.Label($"Grid Position: ({selectedCube.position.x}, {selectedCube.position.y})");
            GUILayout.Label($"Wave Position: ({selectedCube.position.x}, {waveY})");
            GUILayout.Label($"HP: {selectedCube.currentHitPoints}/{selectedCube.maxHitPoints}");

            // Type changer
            GUILayout.Label("Change Type:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Normal")) ChangeCubeType(selectedCube, CubeType.Normal);
            if (GUILayout.Button("Blue")) ChangeCubeType(selectedCube, CubeType.Blue);
            if (GUILayout.Button("Black")) ChangeCubeType(selectedCube, CubeType.Black);
            if (GUILayout.Button("Reinf.")) ChangeCubeType(selectedCube, CubeType.Reinforced);
            GUILayout.EndHorizontal();

            // Position controls
            GUILayout.Label("Move:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("↑", GUILayout.Width(30))) MoveCube(selectedCube, 0, 1);
            if (GUILayout.Button("↓", GUILayout.Width(30))) MoveCube(selectedCube, 0, -1);
            if (GUILayout.Button("←", GUILayout.Width(30))) MoveCube(selectedCube, -1, 0);
            if (GUILayout.Button("→", GUILayout.Width(30))) MoveCube(selectedCube, 1, 0);
            if (GUILayout.Button("Top", GUILayout.Width(40)))
                MoveCube(selectedCube, 0, gridManager.Height - 1 - selectedCube.position.y);
            GUILayout.EndHorizontal();

            // Face status info
            GUILayout.Space(5);
            GUILayout.Label("Face Status:", GUI.skin.box);
            var downFace = selectedCube.GetCurrentDownFace();
            var faceStatus = selectedCube.GetActiveFaceStatus();
            var effectiveType = selectedCube.GetEffectiveType();

            GUILayout.Label($"Down Face: {downFace}");
            GUILayout.Label($"Status: {faceStatus}");
            GUILayout.Label($"Effective Type: {effectiveType}");
            GUILayout.Label($"Can Capture: {selectedCube.CanBeCaptured()}");

            // Face painting
            GUILayout.Label("Paint Face:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Corrupt", GUILayout.Width(60)))
                selectedCube.PaintCurrentDownFace(FaceStatus.Corrupted, Color.red, 5);
            if (GUILayout.Button("Enhance", GUILayout.Width(60)))
                selectedCube.PaintCurrentDownFace(FaceStatus.Enhanced, Color.blue, 5);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
                selectedCube.ClearAllFaces();
            GUILayout.EndHorizontal();

            // Actions
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Damage"))
            {
                bool destroyed = selectedCube.TakeDamage(1);
                if (destroyed)
                {
                    waveManager.activeCubes.Remove(selectedCube);
                    selectedCube = null;
                }
            }
            if (GUILayout.Button("Destroy"))
            {
                waveManager.activeCubes.Remove(selectedCube);
                Object.Destroy(selectedCube.gameObject);
                selectedCube = null;
            }
            GUILayout.EndHorizontal();

            // Debug
            GUILayout.Space(5);
            if (GUILayout.Button("Debug Face Mapping"))
                selectedCube.DebugPrintFaceMapping();

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        // Helper methods
        private Dictionary<Vector2Int, CubeManager> GetActiveCubesDict()
        {
            var dict = new Dictionary<Vector2Int, CubeManager>();

            // Get all cubes from scene (not just wave manager)
            var allCubes = Object.FindObjectsOfType<CubeManager>();
            foreach (var cube in allCubes)
            {
                if (cube != null && !cube.isDestroyed)
                {
                    dict[cube.position] = cube;
                }
            }

            return dict;
        }

        private void SpawnCubeAt(Vector2Int gridPos, CubeType type)
        {
            if (waveManager?.cubePrefabs == null || (int)type >= waveManager.cubePrefabs.Length)
            {
                Debug.LogWarning($"Cannot spawn cube type {type}");
                return;
            }

            if (!gridManager.IsValidGridPosition(gridPos))
            {
                Debug.LogWarning($"Invalid position ({gridPos.x}, {gridPos.y})");
                return;
            }

            Vector3 worldPos = gridManager.GridToWorldPosition(gridPos.x, gridPos.y, 2f);
            GameObject cubeObj = Object.Instantiate(waveManager.cubePrefabs[(int)type], worldPos, Quaternion.identity);

            var cube = cubeObj.GetComponent<CubeManager>();
            if (cube == null) cube = cubeObj.AddComponent<CubeManager>();

            var cubeData = new CubeData
            {
                type = type,
                position = gridPos,
                level = 1
            };

            cube.Init(gridManager, cubeData, 2f);
            waveManager.activeCubes.Add(cube);

            Debug.Log($"Spawned {type} at Grid({gridPos.x}, {gridPos.y})");
        }

        private void ChangeCubeType(CubeManager cube, CubeType newType)
        {
            if (cube == null || cube.isDestroyed) return;

            var position = cube.position;
            var level = cube.level;

            // Remove old cube
            waveManager.activeCubes.Remove(cube);
            Object.Destroy(cube.gameObject);

            // Spawn new cube of different type
            SpawnCubeAt(position, newType);

            // Select the new cube
            System.Threading.Tasks.Task.Delay(100).ContinueWith(_ =>
            {
                var newCube = GetActiveCubesDict().GetValueOrDefault(position);
                if (newCube != null) selectedCube = newCube;
            });
        }

        private void MoveCube(CubeManager cube, int deltaX, int deltaY)
        {
            if (cube == null || cube.isDestroyed) return;

            Vector2Int newPos = new Vector2Int(
                Mathf.Clamp(cube.position.x + deltaX, 0, gridManager.Width - 1),
                Mathf.Clamp(cube.position.y + deltaY, 0, gridManager.Height - 1)
            );

            // Check if position is occupied
            var cubesDict = GetActiveCubesDict();
            if (cubesDict.ContainsKey(newPos) && cubesDict[newPos] != cube)
            {
                Debug.LogWarning($"Position ({newPos.x}, {newPos.y}) is occupied");
                return;
            }

            cube.position = newPos;
            Vector3 worldPos = gridManager.GridToWorldPosition(newPos.x, newPos.y, 2f);
            cube.transform.position = worldPos;

            Debug.Log($"Moved cube to Grid({newPos.x}, {newPos.y})");
        }

        private void SpawnTestPattern()
        {
            // Clear existing cubes
            waveManager?.ClearAllCubes();

            // Spawn a test pattern at top of grid
            int topRow = gridManager.Height - 1;

            SpawnCubeAt(new Vector2Int(0, topRow), CubeType.Normal);
            SpawnCubeAt(new Vector2Int(1, topRow), CubeType.Blue);
            SpawnCubeAt(new Vector2Int(2, topRow), CubeType.Black);
            SpawnCubeAt(new Vector2Int(3, topRow), CubeType.Normal);
            SpawnCubeAt(new Vector2Int(4, topRow), CubeType.Reinforced);

            if (gridManager.Height > 1)
            {
                SpawnCubeAt(new Vector2Int(1, topRow - 1), CubeType.Normal);
                SpawnCubeAt(new Vector2Int(2, topRow - 1), CubeType.Blue);
                SpawnCubeAt(new Vector2Int(3, topRow - 1), CubeType.Normal);
            }
        }

        private void FillTopRow(int waveWidth)
        {
            int topRow = gridManager.Height - 1;

            // Only fill up to the wave width, not the entire grid width
            for (int x = 0; x < waveWidth && x < gridManager.Width; x++)
            {
                var pos = new Vector2Int(x, topRow);

                // Skip if position is occupied
                var cubesDict = GetActiveCubesDict();
                if (!cubesDict.ContainsKey(pos))
                {
                    SpawnCubeAt(pos, (CubeType)selectedCubeType);
                }
            }
        }

        private bool DrawCubeTypeButton(string label, int type, Color color)
        {
            GUI.backgroundColor = selectedCubeType == type ? color : Color.white;
            bool clicked = GUILayout.Button(label, GUILayout.Width(60));
            GUI.backgroundColor = Color.white;
            return clicked;
        }

        private Color GetCubeColor(CubeType type)
        {
            switch (type)
            {
                case CubeType.Normal: return Color.gray;
                case CubeType.Blue: return new Color(0.3f, 0.6f, 1f);
                case CubeType.Black: return new Color(0.2f, 0.2f, 0.2f);
                case CubeType.Reinforced: return new Color(0.8f, 0.3f, 0.8f);
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


    }
}
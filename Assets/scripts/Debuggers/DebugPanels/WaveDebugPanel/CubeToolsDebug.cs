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
        private bool showEmptyRows = false; // New: option to show empty rows for placement
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

        public void DrawPanel(WaveData currentEditingWave, System.Action onSyncToGrid = null, System.Action<Vector2Int, CubeType> onCubeAdded = null, System.Action<Vector2Int> onCubeRemoved = null)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("CUBE TOOLS", GUI.skin.box);

            DrawModeControls();
            DrawCubeTypeSelector();

            if (showGridView)
            {
                DrawDynamicCubeGrid(currentEditingWave, onSyncToGrid, onCubeAdded, onCubeRemoved);
            }

            if (showCubeInspector && selectedCube != null)
            {
                DrawCubeInspector(onCubeAdded, onCubeRemoved);
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
            showEmptyRows = GUILayout.Toggle(showEmptyRows, "Empty Rows");

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

        private void DrawDynamicCubeGrid(WaveData currentEditingWave, System.Action onSyncToGrid, System.Action<Vector2Int, CubeType> onCubeAdded, System.Action<Vector2Int> onCubeRemoved)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            // Get all active cubes from the scene
            var activeCubes = GetActiveCubesDict();

            // Calculate the dynamic view area based on cube positions
            var viewArea = CalculateDynamicViewArea(activeCubes);

            // Show grid info
            GUILayout.Label($"Active Cubes: {activeCubes.Count} | View Area: {viewArea.width}x{viewArea.height}");

            if (viewArea.width > 0 && viewArea.height > 0)
            {
                GUILayout.Label($"Grid Range: X({viewArea.xMin}-{viewArea.xMax}) Y({viewArea.yMin}-{viewArea.yMax})");
            }

            // Show hovered position if any
            if (hoveredGridPosition.x >= 0)
            {
                GUILayout.Label($"Hover: Grid({hoveredGridPosition.x},{hoveredGridPosition.y})", GUI.skin.box);
            }

            // Grid controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All", GUILayout.Width(70)))
            {
                waveManager?.ClearAllCubes();
                selectedCube = null;
                onSyncToGrid?.Invoke(); // Notify wave editor
            }
            if (GUILayout.Button("Spawn Test", GUILayout.Width(80)))
            {
                SpawnTestPattern();
                onSyncToGrid?.Invoke(); // Notify wave editor
            }
            if (GUILayout.Button("Fill Top Row", GUILayout.Width(90)))
            {
                FillTopRow();
                onSyncToGrid?.Invoke(); // Notify wave editor
            }
            if (GUILayout.Button("Compact View", GUILayout.Width(90)))
            {
                // Force recalculation of view area
                hoveredGridPosition = new Vector2Int(-1, -1);
            }
            GUILayout.EndHorizontal();

            // Draw the dynamic grid only if there are cubes or empty rows are enabled
            if (activeCubes.Count > 0 || showEmptyRows)
            {
                gridScroll = GUILayout.BeginScrollView(gridScroll, GUILayout.MinHeight(300), GUILayout.MaxHeight(500));
                DrawCompactGrid(activeCubes, viewArea, currentEditingWave, onSyncToGrid, onCubeAdded, onCubeRemoved);
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("No cubes on grid. Use placement mode or 'Spawn Test' to add cubes.", GUI.skin.box);
            }

            GUILayout.EndVertical();
        }

        private GridViewArea CalculateDynamicViewArea(Dictionary<Vector2Int, CubeManager> activeCubes)
        {
            if (activeCubes.Count == 0 && !showEmptyRows)
            {
                return new GridViewArea(); // Empty area
            }

            if (showEmptyRows || activeCubes.Count == 0)
            {
                // Show a reasonable default area when showing empty rows
                return new GridViewArea
                {
                    xMin = 0,
                    xMax = gridManager.Width - 1,
                    yMin = gridManager.Height - 5, // Show top 5 rows
                    yMax = gridManager.Height - 1
                };
            }

            // Calculate bounds based on actual cube positions
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;

            foreach (var kvp in activeCubes)
            {
                if (kvp.Value != null && !kvp.Value.isDestroyed)
                {
                    var pos = kvp.Key;
                    minX = Mathf.Min(minX, pos.x);
                    maxX = Mathf.Max(maxX, pos.x);
                    minY = Mathf.Min(minY, pos.y);
                    maxY = Mathf.Max(maxY, pos.y);
                }
            }

            // Add a small buffer around the cube area for easier placement
            int bufferX = 1;
            int bufferY = 2; // More Y buffer for typical gameplay

            return new GridViewArea
            {
                xMin = Mathf.Max(0, minX - bufferX),
                xMax = Mathf.Min(gridManager.Width - 1, maxX + bufferX),
                yMin = Mathf.Max(0, minY - bufferY),
                yMax = Mathf.Min(gridManager.Height - 1, maxY + bufferY)
            };
        }

        private void DrawCompactGrid(Dictionary<Vector2Int, CubeManager> activeCubes,
                                   GridViewArea viewArea,
                                   WaveData currentEditingWave,
                                   System.Action onSyncToGrid,
                                   System.Action<Vector2Int, CubeType> onCubeAdded,
                                   System.Action<Vector2Int> onCubeRemoved)
        {
            if (viewArea.width <= 0 || viewArea.height <= 0) return;

            // Column headers with actual grid X coordinates
            GUILayout.BeginHorizontal();
            GUILayout.Label("Y\\X", GUILayout.Width(50));
            for (int x = viewArea.xMin; x <= viewArea.xMax; x++)
            {
                GUILayout.Label($"{x}", GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            // Draw rows from top to bottom (highest Y to lowest Y)
            for (int y = viewArea.yMax; y >= viewArea.yMin; y--)
            {
                GUILayout.BeginHorizontal();

                // Row header with actual grid Y coordinate
                GUI.color = (y >= gridManager.Height - 3) ? Color.cyan : Color.white; // Highlight top rows
                GUILayout.Label($"{y}", GUILayout.Width(50));
                GUI.color = Color.white;

                // Draw cells for this row
                for (int x = viewArea.xMin; x <= viewArea.xMax; x++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    DrawGridCell(gridPos, activeCubes, currentEditingWave, onSyncToGrid, onCubeAdded, onCubeRemoved);
                }

                GUILayout.EndHorizontal();
            }

            // Show selection info with actual grid coordinates
            if (selectedCube != null && !selectedCube.isDestroyed)
            {
                GUILayout.Space(5);
                GUI.color = Color.yellow;
                GUILayout.Label($"Selected: {selectedCube.type} at Grid({selectedCube.position.x},{selectedCube.position.y})");
                GUI.color = Color.white;
            }

            // Show view area info
            GUILayout.Space(3);
            GUILayout.Label($"Viewing: {viewArea.width}x{viewArea.height} area of {gridManager.Width}x{gridManager.Height} grid", GUI.skin.box);
        }

        private void DrawGridCell(Vector2Int gridPos,
                                Dictionary<Vector2Int, CubeManager> activeCubes,
                                WaveData currentEditingWave,
                                System.Action onSyncToGrid,
                                System.Action<Vector2Int, CubeType> onCubeAdded,
                                System.Action<Vector2Int> onCubeRemoved)
        {
            bool hasCube = activeCubes.ContainsKey(gridPos);
            CubeManager cube = hasCube ? activeCubes[gridPos] : null;

            // Determine button appearance
            Color buttonColor = Color.white;
            string buttonText = "·";
            string tooltip = $"Grid({gridPos.x},{gridPos.y})";

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
                    tooltip += $" (acts as {effectiveType})";
                }

                // Show damage
                if (cube.currentHitPoints < cube.maxHitPoints)
                {
                    buttonText += $"{cube.currentHitPoints}";
                }
            }
            else
            {
                // Empty cell - show placement hint
                if (isPlacementMode)
                {
                    buttonColor = new Color(0.9f, 0.9f, 0.9f);
                    buttonText = "+";
                    tooltip += " (click to place)";
                }
            }

            // Handle hover
            Rect buttonRect = GUILayoutUtility.GetRect(40, 30, GUILayout.Width(40), GUILayout.Height(30));
            bool isHovered = buttonRect.Contains(Event.current.mousePosition);

            if (isHovered)
            {
                hoveredGridPosition = gridPos;
                buttonColor = Color.Lerp(buttonColor, Color.white, 0.3f);
            }

            // Draw the button with tooltip
            GUI.backgroundColor = buttonColor;
            GUIContent buttonContent = new GUIContent(buttonText, tooltip);
            if (GUI.Button(buttonRect, buttonContent))
            {
                HandleCellClick(gridPos, cube, currentEditingWave, onSyncToGrid, onCubeAdded, onCubeRemoved);
            }
            GUI.backgroundColor = Color.white;
        }

        private void HandleCellClick(Vector2Int gridPos, CubeManager existingCube,
                                   WaveData currentEditingWave,
                                   System.Action onSyncToGrid,
                                   System.Action<Vector2Int, CubeType> onCubeAdded,
                                   System.Action<Vector2Int> onCubeRemoved)
        {
            if (isPlacementMode)
            {
                if (existingCube != null)
                {
                    // Remove cube
                    waveManager.activeCubes.Remove(existingCube);
                    Object.Destroy(existingCube.gameObject);
                    if (selectedCube == existingCube) selectedCube = null;

                    // Notify wave editor
                    onCubeRemoved?.Invoke(gridPos);

                    Debug.Log($"Removed cube from Grid({gridPos.x}, {gridPos.y})");
                }
                else
                {
                    // Place new cube at the grid position
                    var cubeType = (CubeType)selectedCubeType;
                    SpawnCubeAt(gridPos, cubeType);

                    // Notify wave editor
                    onCubeAdded?.Invoke(gridPos, cubeType);
                }
            }
            else
            {
                // Selection mode
                selectedCube = existingCube;
                selectedGridPosition = gridPos;
                if (existingCube != null)
                {
                    Debug.Log($"Selected {existingCube.type} cube at Grid({gridPos.x}, {gridPos.y})");
                }
            }
        }

        private void DrawCubeInspector(System.Action<Vector2Int, CubeType> onCubeAdded = null, System.Action<Vector2Int> onCubeRemoved = null)
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

            // Basic info with actual grid coordinates
            GUILayout.Label($"Type: {selectedCube.type}", GUI.skin.box);
            GUILayout.Label($"Grid Position: ({selectedCube.position.x}, {selectedCube.position.y})");
            GUILayout.Label($"HP: {selectedCube.currentHitPoints}/{selectedCube.maxHitPoints}");

            // Type changer
            GUILayout.Label("Change Type:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Normal")) ChangeCubeType(selectedCube, CubeType.Normal, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("Blue")) ChangeCubeType(selectedCube, CubeType.Blue, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("Black")) ChangeCubeType(selectedCube, CubeType.Black, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("Reinf.")) ChangeCubeType(selectedCube, CubeType.Reinforced, onCubeAdded, onCubeRemoved);
            GUILayout.EndHorizontal();

            // Position controls
            GUILayout.Label("Move:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("↑", GUILayout.Width(30))) MoveCube(selectedCube, 0, 1, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("↓", GUILayout.Width(30))) MoveCube(selectedCube, 0, -1, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("←", GUILayout.Width(30))) MoveCube(selectedCube, -1, 0, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("→", GUILayout.Width(30))) MoveCube(selectedCube, 1, 0, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("Top", GUILayout.Width(40)))
                MoveCube(selectedCube, 0, gridManager.Height - 1 - selectedCube.position.y, onCubeAdded, onCubeRemoved);
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
                    var pos = selectedCube.position;
                    waveManager.activeCubes.Remove(selectedCube);
                    onCubeRemoved?.Invoke(pos);
                    selectedCube = null;
                }
            }
            if (GUILayout.Button("Destroy"))
            {
                var pos = selectedCube.position;
                waveManager.activeCubes.Remove(selectedCube);
                Object.Destroy(selectedCube.gameObject);
                onCubeRemoved?.Invoke(pos);
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

        // Helper classes
        private struct GridViewArea
        {
            public int xMin, xMax, yMin, yMax;
            public int width => xMax - xMin + 1;
            public int height => yMax - yMin + 1;
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

        private void ChangeCubeType(CubeManager cube, CubeType newType, System.Action<Vector2Int, CubeType> onCubeAdded = null, System.Action<Vector2Int> onCubeRemoved = null)
        {
            if (cube == null || cube.isDestroyed) return;

            var position = cube.position;
            var level = cube.level;

            // Remove old cube
            waveManager.activeCubes.Remove(cube);
            Object.Destroy(cube.gameObject);
            onCubeRemoved?.Invoke(position);

            // Spawn new cube of different type
            SpawnCubeAt(position, newType);
            onCubeAdded?.Invoke(position, newType);

            // Select the new cube
            System.Threading.Tasks.Task.Delay(100).ContinueWith(_ =>
            {
                var newCube = GetActiveCubesDict().GetValueOrDefault(position);
                if (newCube != null) selectedCube = newCube;
            });
        }

        private void MoveCube(CubeManager cube, int deltaX, int deltaY, System.Action<Vector2Int, CubeType> onCubeAdded = null, System.Action<Vector2Int> onCubeRemoved = null)
        {
            if (cube == null || cube.isDestroyed) return;

            Vector2Int oldPos = cube.position;
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

            // Update position
            cube.position = newPos;
            Vector3 worldPos = gridManager.GridToWorldPosition(newPos.x, newPos.y, 2f);
            cube.transform.position = worldPos;

            // Notify wave editor about the position change
            onCubeRemoved?.Invoke(oldPos);
            onCubeAdded?.Invoke(newPos, cube.type);

            Debug.Log($"Moved cube from Grid({oldPos.x}, {oldPos.y}) to Grid({newPos.x}, {newPos.y})");
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

        private void FillTopRow()
        {
            int topRow = gridManager.Height - 1;

            // Fill the top row across the grid width
            for (int x = 0; x < gridManager.Width; x++)
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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Enumerations;

namespace WaveDebugSystem
{
    public class CubeGridEditor
    {
        private WaveManager waveManager;
        private GridManager gridManager;

        // UI State
        private int selectedCubeType = 0; // Normal
        private bool isPlacementMode = false;
        private bool showEmptyRows = false;
        private Vector2 gridScroll;

        // Selection and tracking
        private Vector2Int hoveredGridPosition = new Vector2Int(-1, -1);
        private Dictionary<Vector2Int, CubeManager> lastKnownCubes = new Dictionary<Vector2Int, CubeManager>();

        public bool IsPlacementMode => isPlacementMode;
        public Vector2Int HoveredPosition => hoveredGridPosition;

        public void Initialize(WaveManager waveManager, GridManager gridManager)
        {
            this.waveManager = waveManager;
            this.gridManager = gridManager;
        }

        public void Update()
        {
            // Track cube changes for responsive updates
            TrackCubeChanges();
        }

        public void DrawGridEditor(WaveData currentEditingWave,
                                  System.Action onSyncToGrid,
                                  System.Action<Vector2Int, CubeType> onCubeAdded,
                                  System.Action<Vector2Int> onCubeRemoved,
                                  System.Action<CubeManager> onCubeSelected)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("CUBE GRID EDITOR", GUI.skin.box);

            DrawModeControls();
            DrawCubeTypeSelector();
            DrawGridControls(onSyncToGrid);
            DrawDynamicGrid(currentEditingWave, onSyncToGrid, onCubeAdded, onCubeRemoved, onCubeSelected);

            GUILayout.EndVertical();
        }

        private void DrawModeControls()
        {
            GUILayout.BeginHorizontal();

            GUI.backgroundColor = isPlacementMode ? Color.green : Color.white;
            if (GUILayout.Button("Placement Mode"))
            {
                isPlacementMode = !isPlacementMode;
            }

            GUI.backgroundColor = !isPlacementMode ? Color.cyan : Color.white;
            if (GUILayout.Button("Selection Mode"))
            {
                isPlacementMode = false;
            }

            GUI.backgroundColor = Color.white;

            showEmptyRows = GUILayout.Toggle(showEmptyRows, "Show Empty Area");

            GUILayout.EndHorizontal();

            // Mode instruction
            if (isPlacementMode)
            {
                GUILayout.Label("Click grid to place/remove cubes", GUI.skin.box);
            }
            else
            {
                GUILayout.Label("Click cubes to select and inspect", GUI.skin.box);
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

        private void DrawGridControls(System.Action onSyncToGrid)
        {
            var activeCubes = GetActiveCubesDict();
            var viewArea = CalculateDynamicViewArea(activeCubes);

            // Grid info with enhanced tracking
            GUILayout.BeginVertical(GUI.skin.box);

            // Current state summary
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Active Cubes: {activeCubes.Count}", GUILayout.Width(100));
            if (viewArea.width > 0 && viewArea.height > 0)
            {
                GUILayout.Label($"Area: {viewArea.width}x{viewArea.height}", GUILayout.Width(80));
                GUILayout.Label($"Range: X({viewArea.xMin}-{viewArea.xMax}) Y({viewArea.yMin}-{viewArea.yMax})");
            }
            GUILayout.EndHorizontal();

            // Cube composition analysis
            if (activeCubes.Count > 0)
            {
                var composition = AnalyzeCubeComposition(activeCubes);
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Normal: {composition.normal}", GUILayout.Width(70));
                GUILayout.Label($"Blue: {composition.blue}", GUILayout.Width(60));
                GUILayout.Label($"Black: {composition.black}", GUILayout.Width(60));
                GUILayout.Label($"Reinforced: {composition.reinforced}", GUILayout.Width(80));
                GUILayout.EndHorizontal();
            }

            // Hover information
            if (hoveredGridPosition.x >= 0)
            {
                var cubeAtHover = activeCubes.GetValueOrDefault(hoveredGridPosition);
                if (cubeAtHover != null)
                {
                    var effectiveType = cubeAtHover.GetEffectiveType();
                    string hoverInfo = $"Hover: {cubeAtHover.type} at ({hoveredGridPosition.x},{hoveredGridPosition.y})";
                    if (effectiveType != cubeAtHover.type)
                    {
                        hoverInfo += $" (acts as {effectiveType})";
                    }
                    GUILayout.Label(hoverInfo, GUI.skin.box);
                }
                else
                {
                    GUILayout.Label($"Hover: Empty at ({hoveredGridPosition.x},{hoveredGridPosition.y})", GUI.skin.box);
                }
            }

            GUILayout.EndVertical();

            // Action buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All", GUILayout.Width(70)))
            {
                waveManager?.ClearAllCubes();
                onSyncToGrid?.Invoke();
            }
            if (GUILayout.Button("Spawn Test", GUILayout.Width(80)))
            {
                SpawnTestPattern();
                onSyncToGrid?.Invoke();
            }
            if (GUILayout.Button("Fill Top Row", GUILayout.Width(90)))
            {
                FillTopRow();
                onSyncToGrid?.Invoke();
            }
            if (GUILayout.Button("Compact View", GUILayout.Width(90)))
            {
                hoveredGridPosition = new Vector2Int(-1, -1);
            }
            GUILayout.EndHorizontal();
        }

        private void DrawDynamicGrid(WaveData currentEditingWave,
                                   System.Action onSyncToGrid,
                                   System.Action<Vector2Int, CubeType> onCubeAdded,
                                   System.Action<Vector2Int> onCubeRemoved,
                                   System.Action<CubeManager> onCubeSelected)
        {
            var activeCubes = GetActiveCubesDict();
            var viewArea = CalculateDynamicViewArea(activeCubes);

            // Only show grid if there are cubes or empty rows are enabled
            if (activeCubes.Count > 0 || showEmptyRows)
            {
                // Use minimum height for better visibility
                gridScroll = GUILayout.BeginScrollView(gridScroll, GUILayout.MinHeight(300));
                DrawEnhancedGrid(activeCubes, viewArea, currentEditingWave, onSyncToGrid, onCubeAdded, onCubeRemoved, onCubeSelected);
                GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label("No cubes on grid");
                GUILayout.Label("Use placement mode or 'Spawn Test' to add cubes");
                GUILayout.EndVertical();
            }
        }

        private void DrawEnhancedGrid(Dictionary<Vector2Int, CubeManager> activeCubes,
                                    GridViewArea viewArea,
                                    WaveData currentEditingWave,
                                    System.Action onSyncToGrid,
                                    System.Action<Vector2Int, CubeType> onCubeAdded,
                                    System.Action<Vector2Int> onCubeRemoved,
                                    System.Action<CubeManager> onCubeSelected)
        {
            if (viewArea.width <= 0 || viewArea.height <= 0) return;

            // Enhanced column headers
            GUILayout.BeginHorizontal();
            GUILayout.Label("Y\\X", GUILayout.Width(50));
            for (int x = viewArea.xMin; x <= viewArea.xMax; x++)
            {
                // Highlight columns with cubes
                bool hasColumnCubes = activeCubes.Keys.Any(pos => pos.x == x);
                GUI.color = hasColumnCubes ? Color.cyan : Color.white;
                GUILayout.Label($"{x}", GUILayout.Width(40));
                GUI.color = Color.white;
            }
            GUILayout.EndHorizontal();

            // Draw rows with enhanced information
            for (int y = viewArea.yMax; y >= viewArea.yMin; y--)
            {
                GUILayout.BeginHorizontal();

                // Enhanced row header
                bool hasRowCubes = activeCubes.Keys.Any(pos => pos.y == y);
                GUI.color = (y >= gridManager.Height - 3) ? Color.cyan :
                           hasRowCubes ? Color.yellow : Color.white;
                GUILayout.Label($"{y}", GUILayout.Width(50));
                GUI.color = Color.white;

                // Draw cells for this row
                for (int x = viewArea.xMin; x <= viewArea.xMax; x++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    DrawEnhancedGridCell(gridPos, activeCubes, onSyncToGrid, onCubeAdded, onCubeRemoved, onCubeSelected);
                }

                GUILayout.EndHorizontal();
            }

            // Enhanced grid summary
            GUILayout.Space(5);
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"Grid View: {viewArea.width}x{viewArea.height} of {gridManager.Width}x{gridManager.Height} total");

            if (activeCubes.Count > 0)
            {
                var density = (float)activeCubes.Count / (viewArea.width * viewArea.height) * 100f;
                GUILayout.Label($"Density: {density:F1}% ({activeCubes.Count} cubes in view area)");
            }
            GUILayout.EndVertical();
        }

        private void DrawEnhancedGridCell(Vector2Int gridPos,
                                        Dictionary<Vector2Int, CubeManager> activeCubes,
                                        System.Action onSyncToGrid,
                                        System.Action<Vector2Int, CubeType> onCubeAdded,
                                        System.Action<Vector2Int> onCubeRemoved,
                                        System.Action<CubeManager> onCubeSelected)
        {
            bool hasCube = activeCubes.ContainsKey(gridPos);
            CubeManager cube = hasCube ? activeCubes[gridPos] : null;

            // Enhanced button appearance with more information
            Color buttonColor = Color.white;
            string buttonText = "·";
            string tooltip = $"Grid({gridPos.x},{gridPos.y})";

            if (cube != null)
            {
                buttonColor = GetCubeColor(cube.type);
                buttonText = GetCubeSymbol(cube.type);
                tooltip = $"{cube.type} at {tooltip}";

                // Show effective type if different
                var effectiveType = cube.GetEffectiveType();
                if (effectiveType != cube.type)
                {
                    buttonText += "*";
                    tooltip += $" (acts as {effectiveType})";
                }

                // Show health status
                if (cube.currentHitPoints < cube.maxHitPoints)
                {
                    buttonText += $"{cube.currentHitPoints}";
                    tooltip += $" HP:{cube.currentHitPoints}/{cube.maxHitPoints}";
                }

                // Show movement status
                if (cube.isMoving)
                {
                    buttonText = ">" + buttonText;
                    tooltip += " (moving)";
                }
            }
            else
            {
                // Enhanced empty cell display
                if (isPlacementMode)
                {
                    buttonColor = new Color(0.9f, 0.9f, 0.9f);
                    buttonText = "+";
                    tooltip += " (click to place)";
                }
                else
                {
                    // Show grid pattern for easier navigation
                    if ((gridPos.x + gridPos.y) % 2 == 0)
                    {
                        buttonColor = new Color(0.95f, 0.95f, 0.95f);
                    }
                }
            }

            // Enhanced hover handling
            Rect buttonRect = GUILayoutUtility.GetRect(40, 30, GUILayout.Width(40), GUILayout.Height(30));
            bool isHovered = buttonRect.Contains(Event.current.mousePosition);

            if (isHovered)
            {
                hoveredGridPosition = gridPos;
                buttonColor = Color.Lerp(buttonColor, Color.white, 0.3f);

                // Add hover border effect
                GUI.Box(new Rect(buttonRect.x - 1, buttonRect.y - 1, buttonRect.width + 2, buttonRect.height + 2), "", GUI.skin.box);
            }

            // Draw the enhanced button
            GUI.backgroundColor = buttonColor;
            GUIContent buttonContent = new GUIContent(buttonText, tooltip);
            if (GUI.Button(buttonRect, buttonContent))
            {
                HandleCellClick(gridPos, cube, onSyncToGrid, onCubeAdded, onCubeRemoved, onCubeSelected);
            }
            GUI.backgroundColor = Color.white;
        }

        private void HandleCellClick(Vector2Int gridPos, CubeManager existingCube,
                                   System.Action onSyncToGrid,
                                   System.Action<Vector2Int, CubeType> onCubeAdded,
                                   System.Action<Vector2Int> onCubeRemoved,
                                   System.Action<CubeManager> onCubeSelected)
        {
            if (isPlacementMode)
            {
                if (existingCube != null)
                {
                    // Remove cube
                    waveManager.activeCubes.Remove(existingCube);
                    Object.Destroy(existingCube.gameObject);
                    onCubeRemoved?.Invoke(gridPos);
                    Debug.Log($"Removed cube from Grid({gridPos.x}, {gridPos.y})");
                }
                else
                {
                    // Place new cube
                    var cubeType = (CubeType)selectedCubeType;
                    SpawnCubeAt(gridPos, cubeType);
                    onCubeAdded?.Invoke(gridPos, cubeType);
                }
            }
            else
            {
                // Selection mode
                if (existingCube != null)
                {
                    onCubeSelected?.Invoke(existingCube);
                    Debug.Log($"Selected {existingCube.type} cube at Grid({gridPos.x}, {gridPos.y})");
                }
            }
        }

        // Enhanced tracking methods
        private void TrackCubeChanges()
        {
            var currentCubes = GetActiveCubesDict();

            // Detect changes for responsive updates
            if (!DictionariesEqual(currentCubes, lastKnownCubes))
            {
                // Cube configuration changed
                lastKnownCubes = new Dictionary<Vector2Int, CubeManager>(currentCubes);
            }
        }

        private GridViewArea CalculateDynamicViewArea(Dictionary<Vector2Int, CubeManager> activeCubes)
        {
            if (activeCubes.Count == 0 && !showEmptyRows)
            {
                return new GridViewArea();
            }

            if (showEmptyRows || activeCubes.Count == 0)
            {
                return new GridViewArea
                {
                    xMin = 0,
                    xMax = gridManager.Width - 1,
                    yMin = gridManager.Height - 5,
                    yMax = gridManager.Height - 1
                };
            }

            // Calculate bounds with smart buffering
            int minX = activeCubes.Keys.Min(pos => pos.x);
            int maxX = activeCubes.Keys.Max(pos => pos.x);
            int minY = activeCubes.Keys.Min(pos => pos.y);
            int maxY = activeCubes.Keys.Max(pos => pos.y);

            int bufferX = Mathf.Max(1, (maxX - minX) / 4);
            int bufferY = Mathf.Max(2, (maxY - minY) / 2);

            return new GridViewArea
            {
                xMin = Mathf.Max(0, minX - bufferX),
                xMax = Mathf.Min(gridManager.Width - 1, maxX + bufferX),
                yMin = Mathf.Max(0, minY - bufferY),
                yMax = Mathf.Min(gridManager.Height - 1, maxY + bufferY)
            };
        }

        private CubeComposition AnalyzeCubeComposition(Dictionary<Vector2Int, CubeManager> cubes)
        {
            var composition = new CubeComposition();
            foreach (var cube in cubes.Values)
            {
                if (cube == null || cube.isDestroyed) continue;
                switch (cube.type)
                {
                    case CubeType.Unit: composition.normal++; break;
                    case CubeType.Prime: composition.blue++; break;
                    case CubeType.Infinity: composition.black++; break;
                    case CubeType.Recursion: composition.reinforced++; break;
                }
            }
            return composition;
        }

        // Helper methods
        private Dictionary<Vector2Int, CubeManager> GetActiveCubesDict()
        {
            var dict = new Dictionary<Vector2Int, CubeManager>();
            var allCubes = Object.FindObjectsByType<CubeManager>(FindObjectsSortMode.None);
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
                level = 1,
            };

            cube.Init(gridManager, cubeData, 2f);
            waveManager.activeCubes.Add(cube);
        }

        private void SpawnTestPattern()
        {
            waveManager?.ClearAllCubes();
            int topRow = gridManager.Height - 1;

            SpawnCubeAt(new Vector2Int(0, topRow), CubeType.Unit);
            SpawnCubeAt(new Vector2Int(1, topRow), CubeType.Prime);
            SpawnCubeAt(new Vector2Int(2, topRow), CubeType.Infinity);
            SpawnCubeAt(new Vector2Int(3, topRow), CubeType.Unit);

            if (gridManager.Width > 4)
                SpawnCubeAt(new Vector2Int(4, topRow), CubeType.Recursion);

            if (gridManager.Height > 1)
            {
                SpawnCubeAt(new Vector2Int(1, topRow - 1), CubeType.Unit);
                SpawnCubeAt(new Vector2Int(2, topRow - 1), CubeType.Prime);
            }
        }

        private void FillTopRow()
        {
            int topRow = gridManager.Height - 1;
            var cubesDict = GetActiveCubesDict();

            for (int x = 0; x < gridManager.Width; x++)
            {
                var pos = new Vector2Int(x, topRow);
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
                case CubeType.Unit: return Color.gray;
                case CubeType.Prime: return new Color(0.3f, 0.6f, 1f);
                case CubeType.Infinity: return new Color(0.2f, 0.2f, 0.2f);
                case CubeType.Recursion: return new Color(0.8f, 0.3f, 0.8f);
                default: return Color.white;
            }
        }

        private string GetCubeSymbol(CubeType type)
        {
            switch (type)
            {
                case CubeType.Unit: return "N";
                case CubeType.Prime: return "B";
                case CubeType.Infinity: return "X";
                case CubeType.Recursion: return "R";
                default: return "?";
            }
        }

        private bool DictionariesEqual(Dictionary<Vector2Int, CubeManager> dict1, Dictionary<Vector2Int, CubeManager> dict2)
        {
            if (dict1.Count != dict2.Count) return false;
            foreach (var kvp in dict1)
            {
                if (!dict2.ContainsKey(kvp.Key) || dict2[kvp.Key] != kvp.Value)
                    return false;
            }
            return true;
        }

        // Helper structures
        private struct GridViewArea
        {
            public int xMin, xMax, yMin, yMax;
            public int width => xMax - xMin + 1;
            public int height => yMax - yMin + 1;
        }

        private struct CubeComposition
        {
            public int normal, blue, black, reinforced;
        }
    }

}
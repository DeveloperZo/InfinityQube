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

        // Component instances
        private CubeGridEditor gridEditor;
        private CubeInspector cubeInspector;

        // UI State
        private bool showGridEditor = true;
        private bool showCubeInspector = false;

        public void Initialize(WaveManager waveManager, GridManager gridManager)
        {
            this.waveManager = waveManager;
            this.gridManager = gridManager;

            // Initialize components
            gridEditor = new CubeGridEditor();
            gridEditor.Initialize(waveManager, gridManager);

            cubeInspector = new CubeInspector();
            cubeInspector.Initialize(waveManager, gridManager);
        }

        public void Update()
        {
            // Update components
            gridEditor?.Update();
            cubeInspector?.Update();
        }

        public void DrawPanel(WaveData currentEditingWave,
                             System.Action onSyncToGrid = null,
                             System.Action<Vector2Int, CubeType> onCubeAdded = null,
                             System.Action<Vector2Int> onCubeRemoved = null)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("CUBE TOOLS", GUI.skin.box);

            DrawSectionToggles();
            GUILayout.Space(5);

            // Grid Editor Section
            if (showGridEditor)
            {
                gridEditor?.DrawGridEditor(
                    currentEditingWave,
                    onSyncToGrid,
                    onCubeAdded,
                    onCubeRemoved,
                    OnCubeSelected
                );
            }

            // Cube Inspector Section
            if (showCubeInspector)
            {
                GUILayout.Space(5);
                cubeInspector?.DrawInspector(onCubeAdded, onCubeRemoved);
            }

            // Status summary
            DrawStatusSummary();

            GUILayout.EndVertical();
        }

        private void DrawSectionToggles()
        {
            GUILayout.BeginHorizontal();

            showGridEditor = DebugUIHelpers.DrawToggleButton("Grid Editor", showGridEditor);
            showCubeInspector = DebugUIHelpers.DrawToggleButton("Inspector", showCubeInspector);

            // Quick access buttons
            if (GUILayout.Button("Focus Grid", GUILayout.Width(80)))
            {
                showGridEditor = true;
                showCubeInspector = false;
            }

            if (GUILayout.Button("Focus Inspector", GUILayout.Width(100)))
            {
                showGridEditor = false;
                showCubeInspector = true;
            }

            if (GUILayout.Button("Show Both", GUILayout.Width(80)))
            {
                showGridEditor = true;
                showCubeInspector = true;
            }

            GUILayout.EndHorizontal();
        }



        private void DrawStatusSummary()
        {
            GUILayout.Space(5);
            GUILayout.BeginVertical(GUI.skin.box);

            // Overall status
            var activeCubes = Object.FindObjectsOfType<CubeManager>()
                .Where(c => c != null && !c.isDestroyed).ToList();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Active Cubes: {activeCubes.Count}", GUILayout.Width(100));

            if (gridEditor.IsPlacementMode)
            {
                GUI.color = Color.green;
                GUILayout.Label("PLACEMENT MODE", GUILayout.Width(120));
                GUI.color = Color.white;
            }
            else
            {
                GUI.color = Color.cyan;
                GUILayout.Label("SELECTION MODE", GUILayout.Width(120));
                GUI.color = Color.white;
            }

            var selectedCube = cubeInspector?.SelectedCube;
            if (selectedCube != null)
            {
                GUI.color = Color.yellow;
                GUILayout.Label($"Selected: {selectedCube.type} at ({selectedCube.position.x},{selectedCube.position.y})");
                GUI.color = Color.white;
            }

            GUILayout.EndHorizontal();

            // Hover information from grid editor
            if (gridEditor.HoveredPosition.x >= 0)
            {
                GUILayout.Label($"Hovering: ({gridEditor.HoveredPosition.x},{gridEditor.HoveredPosition.y})");
            }

            GUILayout.EndVertical();
        }

        private void OnCubeSelected(CubeManager cube)
        {
            cubeInspector?.SetSelectedCube(cube);

            // Auto-show inspector when cube is selected
            if (cube != null)
            {
                showCubeInspector = true;
            }
        }

        // Public interface for external access
        public void SetSelectedCube(CubeManager cube)
        {
            OnCubeSelected(cube);
        }

        public CubeManager GetSelectedCube()
        {
            return cubeInspector?.SelectedCube;
        }

        public bool IsPlacementMode()
        {
            return gridEditor?.IsPlacementMode ?? false;
        }

        public void SetPlacementMode(bool enabled)
        {
            // This would require adding a method to CubeGridEditor
            // For now, user can toggle via the UI
        }
    }
}

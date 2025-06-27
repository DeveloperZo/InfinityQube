using System.Linq;
using UnityEngine;
using static Enumerations;

namespace WaveDebugSystem
{
    public class CubeInspector
    {
        private WaveManager waveManager;
        private GridManager gridManager;
        private CubeManager selectedCube = null;
        private Vector2 inspectorScroll;

        public CubeManager SelectedCube => selectedCube;

        public void Initialize(WaveManager waveManager, GridManager gridManager)
        {
            this.waveManager = waveManager;
            this.gridManager = gridManager;
        }

        public void Update()
        {
            // Auto-clear destroyed cube selection
            if (selectedCube != null && (selectedCube.isDestroyed || selectedCube == null))
            {
                selectedCube = null;
            }
        }

        public void SetSelectedCube(CubeManager cube)
        {
            selectedCube = cube;
        }

        public void DrawInspector(System.Action<Vector2Int, CubeType> onCubeAdded = null,
                                 System.Action<Vector2Int> onCubeRemoved = null)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("CUBE INSPECTOR", GUI.skin.box);

            if (selectedCube == null || selectedCube.isDestroyed)
            {
                DrawSelectionHelper();
                GUILayout.EndVertical();
                return;
            }

            // Use minimum height for better visibility
            inspectorScroll = GUILayout.BeginScrollView(inspectorScroll, GUILayout.MinHeight(350));

            DrawCubeConfiguration();
            GUILayout.Space(5);

            DrawPositionTracking();
            GUILayout.Space(5);

            DrawConfigurationControls(onCubeAdded, onCubeRemoved);
            GUILayout.Space(5);

            DrawAdvancedConfiguration(onCubeAdded, onCubeRemoved);

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawSelectionHelper()
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("No cube selected");
            GUILayout.Label("Click a cube in the grid to inspect it");

            // Show quick selection for nearby cubes
            var nearbyCubes = GetNearbyActiveCubes();
            if (nearbyCubes.Count > 0)
            {
                GUILayout.Space(3);
                GUILayout.Label("Quick Select:");
                foreach (var cube in nearbyCubes.Take(5))
                {
                    if (GUILayout.Button($"{cube.type} at ({cube.position.x},{cube.position.y})", GUILayout.Width(200)))
                    {
                        selectedCube = cube;
                    }
                }
            }
            GUILayout.EndVertical();
        }

        private void DrawCubeConfiguration()
        {
            GUILayout.Label("CUBE CONFIGURATION", GUI.skin.box);

            // Configuration summary table
            GUILayout.BeginVertical(GUI.skin.box);

            // Header
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label("Property", GUILayout.Width(80));
            GUILayout.Label("Value", GUILayout.Width(100));
            GUILayout.Label("Status", GUILayout.Width(80));
            GUILayout.EndHorizontal();

            // Type configuration
            DrawConfigRow("Type", selectedCube.type.ToString(), GetTypeStatus());

            // Position configuration  
            DrawConfigRow("Position", $"({selectedCube.position.x},{selectedCube.position.y})", GetPositionStatus());

            // Health configuration
            DrawConfigRow("Health", $"{selectedCube.currentHitPoints}/{selectedCube.maxHitPoints}", GetHealthStatus());

            // Level configuration
            DrawConfigRow("Level", selectedCube.level.ToString(), "Normal");

            // Movement configuration
            DrawConfigRow("Move Count", selectedCube.moveCount.ToString(), selectedCube.isMoving ? "Moving" : "Static");

            // Effective behavior (considering face paint)
            var effectiveType = selectedCube.GetEffectiveType();
            string behaviorStatus = effectiveType != selectedCube.type ? $"Acts as {effectiveType}" : "Normal";
            DrawConfigRow("Behavior", behaviorStatus, selectedCube.CanBeCaptured() ? "Capturable" : "Protected");

            GUILayout.EndVertical();
        }

        private void DrawConfigRow(string property, string value, string status)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(property, GUILayout.Width(80));
            GUILayout.Label(value, GUILayout.Width(100));

            // Color code status
            if (status.Contains("Damaged")) GUI.color = Color.red;
            else if (status.Contains("Moving")) GUI.color = Color.yellow;
            else if (status.Contains("Acts as")) GUI.color = Color.cyan;
            else if (status.Contains("Protected")) GUI.color = Color.red;

            GUILayout.Label(status, GUILayout.Width(80));
            GUI.color = Color.white;
            GUILayout.EndHorizontal();
        }

        private void DrawPositionTracking()
        {
            GUILayout.Label("POSITION TRACKING", GUI.skin.box);

            GUILayout.BeginVertical(GUI.skin.box);

            // Current position details
            GUILayout.Label($"Grid Position: ({selectedCube.position.x}, {selectedCube.position.y})");
            Vector3 worldPos = selectedCube.transform.position;
            GUILayout.Label($"World Position: ({worldPos.x:F1}, {worldPos.y:F1}, {worldPos.z:F1})");

            // Position validation
            bool validPosition = gridManager.IsValidGridPosition(selectedCube.position);
            GUI.color = validPosition ? Color.green : Color.red;
            GUILayout.Label($"Position Valid: {validPosition}");
            GUI.color = Color.white;

            // Movement tracking
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Is Moving: {selectedCube.isMoving}");
            GUILayout.Label($"Move Count: {selectedCube.moveCount}");
            GUILayout.EndHorizontal();

            // Grid context
            GUILayout.Label($"Grid Size: {gridManager.Width}x{gridManager.Height}");
            float distanceFromTop = gridManager.Height - 1 - selectedCube.position.y;
            GUILayout.Label($"Distance from Top: {distanceFromTop} rows");

            GUILayout.EndVertical();
        }

        private void DrawConfigurationControls(System.Action<Vector2Int, CubeType> onCubeAdded,
                                             System.Action<Vector2Int> onCubeRemoved)
        {
            GUILayout.Label("CONFIGURATION CONTROLS", GUI.skin.box);

            // Type modification
            GUILayout.Label("Change Type:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Normal", GUILayout.Width(60)))
                ChangeCubeType(CubeType.Unit, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("Blue", GUILayout.Width(60)))
                ChangeCubeType(CubeType.Prime, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("Black", GUILayout.Width(60)))
                ChangeCubeType(CubeType.Infinity, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("Reinforced", GUILayout.Width(80)))
                ChangeCubeType(CubeType.Recursion, onCubeAdded, onCubeRemoved);
            GUILayout.EndHorizontal();

            // Position modification
            GUILayout.Label("Adjust Position:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("↑", GUILayout.Width(30)))
                MoveCube(0, 1, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("↓", GUILayout.Width(30)))
                MoveCube(0, -1, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("←", GUILayout.Width(30)))
                MoveCube(-1, 0, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("→", GUILayout.Width(30)))
                MoveCube(1, 0, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("To Top", GUILayout.Width(60)))
                MoveCube(0, gridManager.Height - 1 - selectedCube.position.y, onCubeAdded, onCubeRemoved);
            if (GUILayout.Button("Center X", GUILayout.Width(60)))
                MoveCube(gridManager.Width / 2 - selectedCube.position.x, 0, onCubeAdded, onCubeRemoved);
            GUILayout.EndHorizontal();

            // Health modification
            GUILayout.Label("Health Controls:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Damage", GUILayout.Width(60)))
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
            if (GUILayout.Button("Heal", GUILayout.Width(60)))
            {
                selectedCube.currentHitPoints = Mathf.Min(selectedCube.currentHitPoints + 1, selectedCube.maxHitPoints);
            }
            if (GUILayout.Button("Full HP", GUILayout.Width(60)))
            {
                selectedCube.currentHitPoints = selectedCube.maxHitPoints;
            }
            if (GUILayout.Button("Reset", GUILayout.Width(60)))
            {
                selectedCube.currentHitPoints = selectedCube.maxHitPoints;
                selectedCube.moveCount = 0;
            }
            GUILayout.EndHorizontal();
        }

        private void DrawAdvancedConfiguration(System.Action<Vector2Int, CubeType> onCubeAdded,
                                             System.Action<Vector2Int> onCubeRemoved)
        {
            GUILayout.Label("ADVANCED CONFIGURATION", GUI.skin.box);

            // Testing functions
            GUILayout.Label("Testing Functions:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Force Move", GUILayout.Width(80)))
            {
                selectedCube.MoveForward();
            }
            if (GUILayout.Button("Reset State", GUILayout.Width(80)))
            {
                selectedCube.ResetMovementState();
            }
            if (GUILayout.Button("Debug Info", GUILayout.Width(80)))
            {
                Debug.Log($"=== CUBE DEBUG INFO ===");
                Debug.Log($"Type: {selectedCube.type}, Effective: {selectedCube.GetEffectiveType()}");
                Debug.Log($"Position: {selectedCube.position}, World: {selectedCube.transform.position}");
                Debug.Log($"HP: {selectedCube.currentHitPoints}/{selectedCube.maxHitPoints}");
                Debug.Log($"Can Capture: {selectedCube.CanBeCaptured()}");
                Debug.Log($"Should Detonate: {selectedCube.ShouldCreateDetonation()}");
            }
            GUILayout.EndHorizontal();

            // Duplication and removal
            GUILayout.Label("Cube Management:");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Duplicate", GUILayout.Width(80)))
            {
                DuplicateCube(onCubeAdded);
            }
            if (GUILayout.Button("Clone to Top", GUILayout.Width(80)))
            {
                CloneCubeToTop(onCubeAdded);
            }
            if (GUILayout.Button("Destroy", GUILayout.Width(80)))
            {
                var pos = selectedCube.position;
                waveManager.activeCubes.Remove(selectedCube);
                Object.Destroy(selectedCube.gameObject);
                onCubeRemoved?.Invoke(pos);
                selectedCube = null;
            }
            GUILayout.EndHorizontal();

            // Configuration export/import
            GUILayout.Label("Configuration Data:");
            if (GUILayout.Button("Copy Configuration to Clipboard"))
            {
                string config = $"Type:{selectedCube.type}, Pos:({selectedCube.position.x},{selectedCube.position.y}), HP:{selectedCube.currentHitPoints}/{selectedCube.maxHitPoints}";
                GUIUtility.systemCopyBuffer = config;
                Debug.Log($"Copied to clipboard: {config}");
            }
        }

        // Helper methods
        private string GetTypeStatus()
        {
            var effectiveType = selectedCube.GetEffectiveType();
            return effectiveType != selectedCube.type ? $"Modified ({effectiveType})" : "Normal";
        }

        private string GetPositionStatus()
        {
            bool valid = gridManager.IsValidGridPosition(selectedCube.position);
            bool nearTop = selectedCube.position.y >= gridManager.Height - 3;

            if (!valid) return "Invalid";
            if (nearTop) return "Top Area";
            if (selectedCube.position.y <= 2) return "Bottom Area";
            return "Middle";
        }

        private string GetHealthStatus()
        {
            if (selectedCube.currentHitPoints <= 0) return "Destroyed";
            if (selectedCube.currentHitPoints < selectedCube.maxHitPoints) return "Damaged";
            return "Full";
        }

        private void ChangeCubeType(CubeType newType, System.Action<Vector2Int, CubeType> onCubeAdded,
                                   System.Action<Vector2Int> onCubeRemoved)
        {
            if (selectedCube == null || selectedCube.isDestroyed) return;

            var position = selectedCube.position;
            var level = selectedCube.level;
            var currentHP = selectedCube.currentHitPoints;

            // Remove old cube
            waveManager.activeCubes.Remove(selectedCube);
            Object.Destroy(selectedCube.gameObject);
            onCubeRemoved?.Invoke(position);

            // Spawn new cube of different type
            SpawnCubeAt(position, newType);
            onCubeAdded?.Invoke(position, newType);

            // Try to select the new cube
            System.Threading.Tasks.Task.Delay(100).ContinueWith(_ =>
            {
                var newCube = FindCubeAt(position);
                if (newCube != null)
                {
                    selectedCube = newCube;
                    // Preserve health if possible
                    if (currentHP < newCube.maxHitPoints)
                        newCube.currentHitPoints = currentHP;
                }
            });
        }

        private void MoveCube(int deltaX, int deltaY, System.Action<Vector2Int, CubeType> onCubeAdded,
                             System.Action<Vector2Int> onCubeRemoved)
        {
            if (selectedCube == null || selectedCube.isDestroyed) return;

            Vector2Int oldPos = selectedCube.position;
            Vector2Int newPos = new Vector2Int(
                Mathf.Clamp(selectedCube.position.x + deltaX, 0, gridManager.Width - 1),
                Mathf.Clamp(selectedCube.position.y + deltaY, 0, gridManager.Height - 1)
            );

            // Check if position is occupied
            if (FindCubeAt(newPos) != null && FindCubeAt(newPos) != selectedCube)
            {
                Debug.LogWarning($"Position ({newPos.x}, {newPos.y}) is occupied");
                return;
            }

            // Update position
            selectedCube.position = newPos;
            Vector3 worldPos = gridManager.GridToWorldPosition(newPos.x, newPos.y, 2f);
            selectedCube.transform.position = worldPos;

            // Notify wave editor
            onCubeRemoved?.Invoke(oldPos);
            onCubeAdded?.Invoke(newPos, selectedCube.type);

            Debug.Log($"Moved cube from ({oldPos.x}, {oldPos.y}) to ({newPos.x}, {newPos.y})");
        }

        private void DuplicateCube(System.Action<Vector2Int, CubeType> onCubeAdded)
        {
            if (selectedCube == null) return;

            // Find empty adjacent position
            Vector2Int[] offsets = {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1)
            };

            foreach (var offset in offsets)
            {
                Vector2Int newPos = selectedCube.position + offset;
                if (gridManager.IsValidGridPosition(newPos) && FindCubeAt(newPos) == null)
                {
                    SpawnCubeAt(newPos, selectedCube.type);
                    onCubeAdded?.Invoke(newPos, selectedCube.type);
                    return;
                }
            }

            Debug.LogWarning("No adjacent position available for duplication");
        }

        private void CloneCubeToTop(System.Action<Vector2Int, CubeType> onCubeAdded)
        {
            if (selectedCube == null) return;

            Vector2Int topPos = new Vector2Int(selectedCube.position.x, gridManager.Height - 1);
            if (FindCubeAt(topPos) == null)
            {
                SpawnCubeAt(topPos, selectedCube.type);
                onCubeAdded?.Invoke(topPos, selectedCube.type);
            }
            else
            {
                Debug.LogWarning("Top position is occupied");
            }
        }

        private void SpawnCubeAt(Vector2Int gridPos, CubeType type)
        {
            if (waveManager?.cubePrefabs == null || (int)type >= waveManager.cubePrefabs.Length) return;

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
        }

        private CubeManager FindCubeAt(Vector2Int position)
        {
            foreach (CubeManager cube in Object.FindObjectsOfType<CubeManager>())
            {
                if (cube != null && !cube.isDestroyed && cube.position == position)
                    return cube;
            }
            return null;
        }

        private System.Collections.Generic.List<CubeManager> GetNearbyActiveCubes()
        {
            return Object.FindObjectsOfType<CubeManager>()
                .Where(c => c != null && !c.isDestroyed)
                .OrderBy(c => c.position.y)
                .ThenBy(c => c.position.x)
                .Take(5)
                .ToList();
        }
    }

}
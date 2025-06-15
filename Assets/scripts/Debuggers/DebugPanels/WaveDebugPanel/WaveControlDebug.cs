using System.Collections.Generic;
using UnityEngine;

namespace WaveDebugSystem
{
    public class WaveControlDebug
    {
        private WaveManager waveManager;
        private GridManager gridManager;
        private bool autoSyncToGrid;

        public void Initialize(WaveManager waveManager, GridManager gridManager)
        {
            this.waveManager = waveManager;
            this.gridManager = gridManager;
            this.autoSyncToGrid = true;
        }

        public void DrawPanel(System.Action<WaveData> onWaveChanged = null)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("WAVE CONTROLS", GUI.skin.box);

            if (waveManager != null)
            {
                DrawWaveStatus();
                DrawMainControls();
                DrawNavigationControls(onWaveChanged);
                DrawDebugControls();
                DrawCurrentWaveInfo();
            }
            else
            {
                GUILayout.Label("WaveManager not found");
            }

            GUILayout.EndVertical();
        }

        private void DrawWaveStatus()
        {
            GUILayout.Label($"Wave: {waveManager.CurrentWaveIndex + 1}/{waveManager.waveConfiguration.Count}");
            GUILayout.Label($"Step: {waveManager.MoveStep} | Active: {waveManager.waveActive}");
            GUILayout.Label($"Cubes: {waveManager.activeCubes.Count} | Speed: {(waveManager.isSpeedingUp ? "FAST" : "NORMAL")}");
        }

        private void DrawMainControls()
        {
            GUILayout.BeginHorizontal();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Start/Reset"))
            {
                waveManager.StopWave();
                waveManager.StartWave();
            }

            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Pause"))
                waveManager.PauseWave();

            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("Step ▶"))
                waveManager.ManualMoveWaveForward();

            GUI.backgroundColor = Color.magenta;
            if (GUILayout.Button("◀ Step Back"))
                StepWaveBackward();

            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Clear"))
            {
                waveManager.StopWave();
                waveManager.ClearAllCubes();
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }

        private void DrawNavigationControls(System.Action<WaveData> onWaveChanged)
        {
            // Get available waves from library for navigation
            var availableWaves = GetAvailableWavesFromLibrary();

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("◀◀ Prev") && availableWaves.Count > 0)
            {
                NavigateToPreviousWave(availableWaves, onWaveChanged);
            }

            // Show current wave info
            string currentInfo = waveManager.CurrentWave != null ?
                $"{waveManager.CurrentWave.name}" :
                $"Wave {waveManager.currentWaveIndex + 1}/{waveManager.waveConfiguration.Count}";
            GUILayout.Label(currentInfo, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Next ▶▶") && availableWaves.Count > 0)
            {
                NavigateToNextWave(availableWaves, onWaveChanged);
            }

            GUILayout.EndHorizontal();
        }

        private List<WaveData> GetAvailableWavesFromLibrary()
        {
            var waves = new List<WaveData>();
#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:WaveData", new[] { "Assets/data/waves" });
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                WaveData wave = UnityEditor.AssetDatabase.LoadAssetAtPath<WaveData>(path);
                if (wave != null) waves.Add(wave);
            }
#endif
            return waves;
        }

        private void NavigateToPreviousWave(List<WaveData> availableWaves, System.Action<WaveData> onWaveChanged)
        {
            if (availableWaves.Count == 0) return;

            // Find current wave in library
            int currentIndex = -1;
            if (waveManager.CurrentWave != null)
            {
                currentIndex = availableWaves.FindIndex(w => w.name == waveManager.CurrentWave.name);
            }

            // Move to previous (wrap around if needed)
            int prevIndex = currentIndex <= 0 ? availableWaves.Count - 1 : currentIndex - 1;
            LoadWaveToManager(availableWaves[prevIndex]);
            onWaveChanged?.Invoke(availableWaves[prevIndex]);
        }

        private void NavigateToNextWave(List<WaveData> availableWaves, System.Action<WaveData> onWaveChanged)
        {
            if (availableWaves.Count == 0) return;

            // Find current wave in library
            int currentIndex = -1;
            if (waveManager.CurrentWave != null)
            {
                currentIndex = availableWaves.FindIndex(w => w.name == waveManager.CurrentWave.name);
            }

            // Move to next (wrap around if needed)
            int nextIndex = currentIndex >= availableWaves.Count - 1 ? 0 : currentIndex + 1;
            LoadWaveToManager(availableWaves[nextIndex]);
            onWaveChanged?.Invoke(availableWaves[nextIndex]);
        }

        private void LoadWaveToManager(WaveData wave)
        {
            if (waveManager == null || wave == null) return;

            waveManager.useWaveConfiguration = true;

            // Add to wave manager configuration if not already there
            if (!waveManager.waveConfiguration.Contains(wave))
            {
                waveManager.waveConfiguration.Add(wave);
            }

            // Set as current wave
            waveManager.currentWaveIndex = waveManager.waveConfiguration.IndexOf(wave);
            Debug.Log($"Navigated to wave '{wave.name}'");
        }

        private void DrawDebugControls()
        {
            GUILayout.BeginHorizontal();

            bool newDebug = GUILayout.Toggle(waveManager.debugMode, "Debug Mode");
            if (newDebug != waveManager.debugMode)
            {
                if (newDebug)
                    waveManager.EnterDebugMode(true);
                else
                    waveManager.ExitDebugMode();
            }

            autoSyncToGrid = GUILayout.Toggle(autoSyncToGrid, "Auto Sync Grid");

            GUILayout.EndHorizontal();
        }

        private void DrawCurrentWaveInfo()
        {
            if (waveManager.CurrentWave != null)
            {
                var wave = waveManager.CurrentWave;
                GUILayout.Label($"Current: {wave.name} | {wave.GridWidth}x{wave.GridHeight} | {wave.CubesData.Count} cubes");
                GUILayout.Label($"Timing: {wave.moveInterval:F1}s / {wave.fastMoveInterval:F1}s");
            }
        }

        private void StepWaveBackward()
        {
            if (waveManager == null || waveManager.activeCubes.Count == 0) return;

            Debug.Log("Stepping wave backward - moving all cubes up one position");

            // Move all active cubes backward (up) one position
            for (int i = waveManager.activeCubes.Count - 1; i >= 0; i--)
            {
                var cube = waveManager.activeCubes[i];
                if (cube == null || cube.isDestroyed)
                {
                    waveManager.activeCubes.RemoveAt(i);
                    continue;
                }

                // Move cube up one position (reverse of MoveForward)
                cube.position.y += 1;

                // Check if cube moved off the top of the grid
                if (cube.position.y >= gridManager.Height)
                {
                    // Remove cube that went off the top
                    waveManager.activeCubes.RemoveAt(i);
                    Object.Destroy(cube.gameObject);
                    continue;
                }

                // Update cube's world position
                Vector3 newWorldPos = gridManager.GridToWorldPosition(cube.position.x, cube.position.y, 2f);
                cube.transform.position = newWorldPos;

                // Reverse face rotation (opposite of RotateFaceMapping)
                ReverseFaceRotation(cube);
            }

            // Decrease move step if possible
            if (waveManager.MoveStep > 0)
            {
                waveManager.MoveStep--;
            }

            Debug.Log($"Step backward complete. Move step: {waveManager.MoveStep}, Active cubes: {waveManager.activeCubes.Count}");
        }

        private void ReverseFaceRotation(CubeManager cube)
        {
            // This reverses the face mapping rotation that happens in MoveForward
            // Original rotation: Bottom->Front, Front->Top, Top->Back, Back->Bottom
            // Reverse: Front->Bottom, Top->Front, Back->Top, Bottom->Back

            // Access the private currentFaceMapping field via reflection or make it public
            // For now, we'll call a public method if available
            if (cube.GetType().GetMethod("ReverseFaceRotation") != null)
            {
                cube.GetType().GetMethod("ReverseFaceRotation").Invoke(cube, null);
            }
        }

        public bool GetAutoSyncToGrid() => autoSyncToGrid;
        public void SetAutoSyncToGrid(bool value) => autoSyncToGrid = value;
    }

}
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
                waveManager.useWaveConfiguration = false;
            }

            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }

        private void DrawNavigationControls(System.Action<WaveData> onWaveChanged)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("◀◀ Prev") && waveManager.currentWaveIndex > 0)
            {
                waveManager.currentWaveIndex--;
                onWaveChanged?.Invoke(waveManager.CurrentWave);
            }

            GUILayout.Label($"Wave {waveManager.currentWaveIndex + 1}", GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Next ▶▶") && waveManager.currentWaveIndex < waveManager.waveConfiguration.Count - 1)
            {
                waveManager.currentWaveIndex++;
                onWaveChanged?.Invoke(waveManager.CurrentWave);
            }

            GUILayout.EndHorizontal();
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
using UnityEngine;

public class WaveDebugPanel : IDebugPanel
{
    public string PanelName => "Wave";

    private WaveManager waveManager;

    public void Initialize()
    {
        waveManager = Object.FindObjectOfType<WaveManager>();
    }

    public void Update()
    {
        // No specific update logic needed
    }

    public void DrawPanel()
    {
        DrawWaveInfo();
        GUILayout.Space(10);
        DrawWaveControls();
        GUILayout.Space(10);
        DrawWaveSettings();
        GUILayout.Space(10);
        DrawActiveCubes();
    }

    private void DrawWaveInfo()
    {
        GUILayout.Label("WAVE INFO", GUI.skin.box);

        if (waveManager != null)
        {
            GUILayout.Label($"Wave Active: {waveManager.waveActive}");
            GUILayout.Label($"Debug Mode: {waveManager.debugMode}");
            GUILayout.Label($"Manual Control: {waveManager.manualControl}");
            GUILayout.Label($"Current Wave: {waveManager.CurrentWaveIndex}");
            GUILayout.Label($"Move Step: {waveManager.MoveStep}");
            GUILayout.Label($"Active Cubes: {waveManager.activeCubes.Count}");
            GUILayout.Label($"Speed Up: {waveManager.isSpeedingUp}");

            if (waveManager.CurrentWave != null)
            {
                GUILayout.Label($"Wave Name: {waveManager.CurrentWave.name}");
                GUILayout.Label($"Grid Size: {waveManager.CurrentWave.GridWidth}x{waveManager.CurrentWave.GridHeight}");
                GUILayout.Label($"Messages: {waveManager.CurrentWave.messages.Count}");
            }
        }
        else
        {
            GUILayout.Label("WaveManager not found");
        }
    }

    private void DrawWaveControls()
    {
        GUILayout.Label("WAVE CONTROLS", GUI.skin.box);

        if (waveManager == null) return;

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Start Wave"))
            waveManager.StartWave();
        if (GUILayout.Button("Pause Wave"))
            waveManager.PauseWave();
        if (GUILayout.Button("Resume Wave"))
            waveManager.ResumeWave();
        if (GUILayout.Button("Stop Wave"))
            waveManager.StopWave();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Manual Step"))
            waveManager.ManualMoveWaveForward();
        if (GUILayout.Button("Clear All Cubes"))
            waveManager.ClearAllCubes();
        GUILayout.EndHorizontal();
    }

    private void DrawWaveSettings()
    {
        GUILayout.Label("WAVE SETTINGS", GUI.skin.box);

        if (waveManager == null) return;

        // Debug mode toggle
        GUILayout.BeginHorizontal();
        GUILayout.Label("Debug Mode:");
        bool newDebugMode = GUILayout.Toggle(waveManager.debugMode, "");
        if (newDebugMode != waveManager.debugMode)
        {
            if (newDebugMode)
                waveManager.EnterDebugMode(true);
            else
                waveManager.ExitDebugMode();
        }
        GUILayout.EndHorizontal();

        // Manual control info
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Manual Control: {waveManager.manualControl}");
        GUILayout.EndHorizontal();

        // Speed settings
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Normal Speed: {waveManager.normalMoveInterval:F2}s");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Fast Speed: {waveManager.fastMoveInterval:F2}s");
        GUILayout.EndHorizontal();
    }

    private void DrawActiveCubes()
    {
        GUILayout.Label("ACTIVE CUBES", GUI.skin.box);

        if (waveManager == null || waveManager.activeCubes.Count == 0)
        {
            GUILayout.Label("No active cubes");
            return;
        }

        GUILayout.Label($"Total: {waveManager.activeCubes.Count}");

        int normalCount = 0, blueCount = 0, blackCount = 0;

        foreach (var cube in waveManager.activeCubes)
        {
            if (cube == null) continue;

            switch (cube.type)
            {
                case Enumerations.CubeType.Normal: normalCount++; break;
                case Enumerations.CubeType.Blue: blueCount++; break;
                case Enumerations.CubeType.Black: blackCount++; break;
            }
        }

        GUILayout.Label($"Normal: {normalCount}");
        GUILayout.Label($"Blue: {blueCount}");
        GUILayout.Label($"Black: {blackCount}");

        // Show first few cube positions
        GUILayout.Label("Positions (first 5):");
        for (int i = 0; i < Mathf.Min(5, waveManager.activeCubes.Count); i++)
        {
            var cube = waveManager.activeCubes[i];
            if (cube != null)
            {
                GUILayout.Label($"{cube.type}: ({cube.position.x}, {cube.position.y})");
            }
        }
    }
}
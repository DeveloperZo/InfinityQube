using UnityEngine;

public class WaveDebugUIRenderer : MonoBehaviour
{
    private WaveDebugGridConfigurator gridConfig;
    private WaveDebugWaveController waveCtrl;
    private WaveDebugDataCollector dataCol;

    [Header("UI")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F2;
    [SerializeField] private Color pauseButtonColor = new Color(1f, 0.5f, 0.5f, 1f);
    [SerializeField] private int buttonSize = 30;
    private bool showDebugger;
    private Vector2 mainScroll;
    private Rect windowRect;

    private void Awake()
    {
        gridConfig = GetComponent<WaveDebugGridConfigurator>();
        waveCtrl = GetComponent<WaveDebugWaveController>();
        dataCol = GetComponent<WaveDebugDataCollector>();
        windowRect = new Rect(10, 50, 420, Screen.height - 100);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey)) showDebugger = !showDebugger;
    }

    private void OnGUI()
    {
        if (!showDebugger) return;
        windowRect = GUILayout.Window(0, windowRect, DrawWindow, "Wave Debugger");
    }

    private void DrawWindow(int id)
    {
        mainScroll = GUILayout.BeginScrollView(mainScroll);

        // Status
        GUILayout.BeginHorizontal(GUI.skin.box);
        GUI.color = waveCtrl != null && waveCtrl.enabled ? Color.green : Color.yellow;
        GUILayout.Label(waveCtrl != null && waveCtrl.enabled ? "TRACKING" : "EDIT", GUILayout.Width(80));
        GUI.color = Color.white;
        GUILayout.Label($"Spawned: {dataCol.TotalSpawned} Removed: {dataCol.TotalRemoved}");
        GUILayout.EndHorizontal();

        // Grid blueprint
        if (GUILayout.Button("Apply Grid Size")) gridConfig.ApplyGridSize();
        if (GUILayout.Button("Clear Grid")) gridConfig.ClearGrid();
        if (GUILayout.Button("Randomize Grid")) gridConfig.RandomizeGrid();

        GUILayout.Label("Wave Blueprint");
        for (int y = 0; y < gridConfig.WaveHeight; y++)
        {
            GUILayout.BeginHorizontal();
            for (int x = 0; x < gridConfig.WaveWidth; x++)
            {
                int state = gridConfig.GridState[x, y];
                GUI.backgroundColor = state switch { 1 => Color.gray, 2 => Color.blue, 3 => Color.black, _ => Color.white };
                GUILayout.Button("", GUILayout.Width(buttonSize), GUILayout.Height(buttonSize));
            }
            GUILayout.EndHorizontal();
        }
        GUI.backgroundColor = Color.white;

        // Actions
        if (GUILayout.Button("Spawn Wave")) waveCtrl.SpawnWave();
        if (GUILayout.Button("Save Wave")) waveCtrl.SaveCurrentWave();

        GUILayout.EndScrollView();
        GUI.DragWindow();
    }
}

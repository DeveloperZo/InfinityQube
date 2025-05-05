using UnityEngine;
using System.Collections.Generic;

public class StageDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private StageManager stageManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private PlayerController playerController;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugger = true;
    [SerializeField] private Vector2 scrollPosition;
    [SerializeField] private int selectedStage = -1;

    private Dictionary<int, string> stageNames = new Dictionary<int, string>();
    private bool isInitialized = false;

    private void Start()
    {
        // Auto-find references
        if (stageManager == null) stageManager = FindObjectOfType<StageManager>();
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (waveManager == null) waveManager = FindObjectOfType<WaveManager>();
        if (playerController == null) playerController = FindObjectOfType<PlayerController>();

        // Initialize stage names
        InitializeStageNames();

        isInitialized = true;
    }

    private void Update()
    {
        // Toggle debugger visibility
        if (Input.GetKeyDown(KeyCode.F3))
        {
            showDebugger = !showDebugger;
        }
    }

    private void InitializeStageNames()
    {
        stageNames.Clear();

        // Tutorial stages
        stageNames.Add(-1, "Tutorial -1: First Steps");

        // Regular stages (will be populated later)
        stageNames.Add(1, "Stage 1: Getting Started");
        stageNames.Add(2, "Stage 2: Handling Pressure");
        // etc.
    }

    private void OnGUI()
    {
        if (!showDebugger || !isInitialized) return;

        // Define debugger panel
        GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20));
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("STAGE DEBUGGER", GUI.skin.box);

        // Stage selection
        GUILayout.Label("Stage Selection:", GUI.skin.box);

        foreach (var stage in stageNames)
        {
            if (GUILayout.Button($"{stage.Key}: {stage.Value}"))
            {
                selectedStage = stage.Key;
                LoadSelectedStage();
            }
        }

        GUILayout.Space(10);

        // Current stage status
        GUILayout.Label("Current Stage Status:", GUI.skin.box);
        GUILayout.Label($"Selected Stage: {(selectedStage == 0 ? "None" : selectedStage.ToString())}");

        if (gridManager != null)
        {
            GUILayout.Label($"Grid Size: {gridManager.Width}x{gridManager.Height}");
        }

        // Cube actions for testing
        GUILayout.Label("Test Actions:", GUI.skin.box);

        if (GUILayout.Button("Trigger Cube Captured"))
        {
            if (stageManager != null)
            {
                stageManager.OnCubeCaptured(Enumerations.CubeType.Normal);
            }
        }

        if (GUILayout.Button("Trigger Cube Escaped"))
        {
            if (stageManager != null)
            {
                stageManager.OnCubeEscaped(Enumerations.CubeType.Normal);
            }
        }

        // Wave controls for testing
        GUILayout.Label("Wave Controls:", GUI.skin.box);

        if (GUILayout.Button("Start Wave"))
        {
            if (waveManager != null)
            {
                // Call StartWave through reflection or a public method
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void LoadSelectedStage()
    {
        if (stageManager != null && selectedStage != 0)
        {
            stageManager.LoadStage(selectedStage);
        }
    }
}
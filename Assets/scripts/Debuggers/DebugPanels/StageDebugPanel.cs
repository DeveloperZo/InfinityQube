using UnityEngine;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
#endif

public class StageDebugPanel : IDebugPanel
{
    public string PanelName => "Stage Editor";

    private StageManager stageManager;
    private WaveManager waveManager;
    private StageDB stageDatabase;

    // UI State
    private Vector2 scrollPosition;
    private bool showCurrentStage = true;
    private bool showStageEditor = false;
    private bool showStageList = true;
    private bool showLifecycleControls = true;
    private bool showStageHistory = false;

    // Editor State
    private StageData editingStage = null;
    private bool isCreatingNewStage = false;
    private string newStageName = "New Stage";
    private string searchFilter = "";
    private int selectedStageIndex = 0;

    // Stage Editor Fields
    private string stageName = "";
    private string stageDescription = "";
    private string stageObjective = "";
    private int stageNumber = 0;
    private int gridWidth = 6;
    private int gridHeight = 10;
    private Vector2Int playerStartPosition = new Vector2Int(2, 0);
    private bool requireAllCubesDestroyed = false;
    private int requiredCaptureCount = 0;
    private int maxAllowedEscapes = 0;

    public void Initialize()
    {
        stageManager = Object.FindObjectOfType<StageManager>();
        waveManager = Object.FindObjectOfType<WaveManager>();

        // Try to find the stage database
        stageDatabase = Resources.Load<StageDB>("StageDatabase");
        if (stageDatabase == null)
        {
            // Try to find it as a field in StageManager
            if (stageManager != null)
            {
                var field = stageManager.GetType().GetField("stageDatabase",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                    stageDatabase = field.GetValue(stageManager) as StageDB;
            }
        }
    }

    public void Update()
    {
        // No specific update logic needed
    }

    public void DrawPanel()
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        DrawPanelTabs();
        GUILayout.Space(5);

        if (showCurrentStage)
            DrawCurrentStageSection();

        if (showLifecycleControls)
            DrawLifecycleSection();

        if (showStageList)
            DrawStageListSection();

        if (showStageEditor)
            DrawStageEditorSection();

        if (showStageHistory)
            DrawStageHistorySection();

        GUILayout.EndScrollView();
    }

    private void DrawPanelTabs()
    {
        GUILayout.BeginHorizontal();

        GUI.backgroundColor = showCurrentStage ? Color.cyan : Color.white;
        if (GUILayout.Button("Current", GUILayout.Height(25)))
            showCurrentStage = !showCurrentStage;

        GUI.backgroundColor = showLifecycleControls ? Color.cyan : Color.white;
        if (GUILayout.Button("Lifecycle", GUILayout.Height(25)))
            showLifecycleControls = !showLifecycleControls;

        GUI.backgroundColor = showStageList ? Color.cyan : Color.white;
        if (GUILayout.Button("List", GUILayout.Height(25)))
            showStageList = !showStageList;

        GUI.backgroundColor = showStageEditor ? Color.cyan : Color.white;
        if (GUILayout.Button("Editor", GUILayout.Height(25)))
            showStageEditor = !showStageEditor;

        GUI.backgroundColor = showStageHistory ? Color.cyan : Color.white;
        if (GUILayout.Button("History", GUILayout.Height(25)))
            showStageHistory = !showStageHistory;

        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();
    }

    private void DrawCurrentStageSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CURRENT STAGE", GUI.skin.box);

        if (stageManager?.CurrentStage != null)
        {
            var stage = stageManager.CurrentStage;

            GUILayout.BeginHorizontal();
            GUILayout.Label($"ID: {stageManager.CurrentStageIndex}", GUILayout.Width(60));
            GUILayout.Label($"Name: {stage.stageName}");
            if (GUILayout.Button("Edit", GUILayout.Width(50)))
            {
                LoadStageForEditing(stage);
            }
            GUILayout.EndHorizontal();

            GUILayout.Label($"Description: {stage.description}");
            GUILayout.Label($"Objective: {stage.objective}");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Grid: {stage.gridWidth}x{stage.gridHeight}");
            GUILayout.Label($"Player Start: ({stage.playerStartPosition.x}, {stage.playerStartPosition.y})");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Waves: {stage.waveConfigurations.Count}");
            GUILayout.Label($"Status: {(stageManager.IsStageInProgress ? "ACTIVE" : "INACTIVE")}");
            GUILayout.EndHorizontal();

            // Success criteria
            if (stage.requireAllCubesDestroyed || stage.requiredCaptureCount > 0 || stage.maxAllowedEscapes >= 0)
            {
                GUILayout.Label("Success Criteria:");
                if (stage.requireAllCubesDestroyed)
                    GUILayout.Label("  • All cubes must be destroyed");
                if (stage.requiredCaptureCount > 0)
                    GUILayout.Label($"  • Capture {stage.requiredCaptureCount} cubes");
                if (stage.maxAllowedEscapes >= 0)
                    GUILayout.Label($"  • Max {stage.maxAllowedEscapes} cube escapes allowed");
            }

            // Current wave info
            if (waveManager != null && stage.waveConfigurations.Count > 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Current Wave: {waveManager.CurrentWaveIndex + 1}/{stage.waveConfigurations.Count}");
                GUILayout.Label($"Move Step: {waveManager.MoveStep}");
                GUILayout.EndHorizontal();

                if (waveManager.CurrentWave != null)
                {
                    var wave = waveManager.CurrentWave;
                    GUILayout.Label($"Wave: {wave.name} ({wave.GridWidth}x{wave.GridHeight})");
                    GUILayout.Label($"Cubes: {wave.CubesData.Count}, Messages: {wave.messages.Count}");
                }
            }
        }
        else
        {
            GUILayout.Label("No stage loaded");
            if (GUILayout.Button("Load First Stage"))
            {
                stageManager?.ResetToFirstStage();
            }
        }

        GUILayout.EndVertical();
    }

    private void DrawLifecycleSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("STAGE LIFECYCLE", GUI.skin.box);

        // Basic navigation
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Previous"))
            stageManager?.LoadPreviousStage();
        if (GUILayout.Button("Restart"))
            stageManager?.RestartCurrentStage();
        if (GUILayout.Button("Next"))
            stageManager?.LoadNextStage();
        GUILayout.EndHorizontal();

        // Force completion
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Force Success"))
            stageManager?.ForceCompleteStage(true);
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Force Failure"))
            stageManager?.ForceCompleteStage(false);
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Advanced controls
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset to First"))
            stageManager?.ResetToFirstStage();
        if (GUILayout.Button("Skip to Last"))
        {
            var stages = stageManager?.GetAvailableStages();
            if (stages != null && stages.Count > 0)
            {
                stageManager.LoadStage(stages.Max());
            }
        }
        GUILayout.EndHorizontal();

        // Test specific stage IDs
        GUILayout.Label("Quick Load:");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Tutorial (1)", GUILayout.Width(80)))
            stageManager?.LoadStage(0);
        if (GUILayout.Button("Tutorial (2)", GUILayout.Width(60)))
            stageManager?.LoadStage(1);
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawStageListSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("STAGE DATABASE", GUI.skin.box);

        // Search filter
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search:", GUILayout.Width(50));
        searchFilter = GUILayout.TextField(searchFilter);
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
            searchFilter = "";
        GUILayout.EndHorizontal();

        // Create new stage button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("+ Create New Stage", GUILayout.Height(30)))
        {
            StartCreatingNewStage();
        }
        GUI.backgroundColor = Color.white;

        if (stageManager != null)
        {
            var availableStages = stageManager.GetAvailableStages();

            if (availableStages.Count > 0)
            {
                GUILayout.Label($"Available Stages: {availableStages.Count}");

                foreach (int stageId in availableStages)
                {
                    // Get stage data for display
                    StageData stageData = null;
                    if (stageDatabase != null)
                    {
                        stageData = stageDatabase.GetStage(stageId);
                    }

                    // Apply search filter
                    if (!string.IsNullOrEmpty(searchFilter))
                    {
                        string searchLower = searchFilter.ToLower();
                        if (stageData != null)
                        {
                            if (!stageData.stageName.ToLower().Contains(searchLower) &&
                                !stageData.description.ToLower().Contains(searchLower) &&
                                !stageId.ToString().Contains(searchFilter))
                            {
                                continue;
                            }
                        }
                        else if (!stageId.ToString().Contains(searchFilter))
                        {
                            continue;
                        }
                    }

                    DrawStageListItem(stageId, stageData);
                }
            }
            else
            {
                GUILayout.Label("No stages found in database");
            }
        }
        else
        {
            GUILayout.Label("StageManager not found");
        }

        GUILayout.EndVertical();
    }

    private void DrawStageListItem(int stageId, StageData stageData)
    {
        bool isCurrent = stageId == stageManager?.CurrentStageIndex;
        bool isSelected = selectedStageIndex == stageId;

        GUI.backgroundColor = isCurrent ? Color.yellow : (isSelected ? Color.cyan : Color.white);

        GUILayout.BeginVertical(GUI.skin.box);

        // Stage header
        GUILayout.BeginHorizontal();
        string stageName = stageData?.stageName ?? $"Stage {stageId}";
        string statusText = isCurrent ? " (CURRENT)" : "";
        GUILayout.Label($"[{stageId}] {stageName}{statusText}", GUI.skin.box);
        GUILayout.EndHorizontal();

        // Stage details
        if (stageData != null)
        {
            if (!string.IsNullOrEmpty(stageData.description))
                GUILayout.Label($"Desc: {stageData.description}");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Grid: {stageData.gridWidth}x{stageData.gridHeight}");
            GUILayout.Label($"Waves: {stageData.waveConfigurations.Count}");
            GUILayout.EndHorizontal();
        }

        // Action buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load", GUILayout.Width(50)))
        {
            stageManager?.LoadStage(stageId);
            selectedStageIndex = stageId;
        }
        if (GUILayout.Button("Edit", GUILayout.Width(50)))
        {
            if (stageData != null)
                LoadStageForEditing(stageData);
            selectedStageIndex = stageId;
        }
        if (GUILayout.Button("Copy", GUILayout.Width(50)))
        {
            if (stageData != null)
                CreateStageFromTemplate(stageData);
        }
        GUI.backgroundColor = Color.red;
#if UnityEditor
        if (GUILayout.Button("Delete", GUILayout.Width(60)))
        {

            if (EditorUtility.DisplayDialog("Delete Stage",
                $"Are you sure you want to delete Stage {stageId}?", "Delete", "Cancel"))
            {
                DeleteStage(stageId);
            }
        }
#endif
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
        GUI.backgroundColor = Color.white;
    }

    private void DrawStageEditorSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);

        if (editingStage != null || isCreatingNewStage)
        {
            string title = isCreatingNewStage ? "CREATE NEW STAGE" : $"EDIT STAGE: {editingStage?.stageName}";
            GUILayout.Label(title, GUI.skin.box);

            DrawStageEditorFields();
            DrawStageEditorActions();
        }
        else
        {
            GUILayout.Label("STAGE EDITOR", GUI.skin.box);
            GUILayout.Label("Select a stage to edit or create a new one");

            if (GUILayout.Button("Create New Stage"))
            {
                StartCreatingNewStage();
            }
        }

        GUILayout.EndVertical();
    }

    private void DrawStageEditorFields()
    {
        // Basic Info
        GUILayout.Label("Basic Information:", GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Stage Number:", GUILayout.Width(100));
        string stageNumStr = GUILayout.TextField(stageNumber.ToString(), GUILayout.Width(60));
        if (int.TryParse(stageNumStr, out int newStageNum))
            stageNumber = newStageNum;
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Name:", GUILayout.Width(100));
        stageName = GUILayout.TextField(stageName);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Description:", GUILayout.Width(100));
        GUILayout.EndHorizontal();
        stageDescription = GUILayout.TextArea(stageDescription, GUILayout.Height(60));

        GUILayout.BeginHorizontal();
        GUILayout.Label("Objective:", GUILayout.Width(100));
        GUILayout.EndHorizontal();
        stageObjective = GUILayout.TextArea(stageObjective, GUILayout.Height(40));

        // Grid Configuration
        GUILayout.Label("Grid Configuration:", GUI.skin.box);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Grid Size:", GUILayout.Width(100));
        string widthStr = GUILayout.TextField(gridWidth.ToString(), GUILayout.Width(40));
        GUILayout.Label("x", GUILayout.Width(15));
        string heightStr = GUILayout.TextField(gridHeight.ToString(), GUILayout.Width(40));
        if (int.TryParse(widthStr, out int newWidth))
            gridWidth = Mathf.Clamp(newWidth, 3, 15);
        if (int.TryParse(heightStr, out int newHeight))
            gridHeight = Mathf.Clamp(newHeight, 9, 25);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Player Start:", GUILayout.Width(100));
        string startXStr = GUILayout.TextField(playerStartPosition.x.ToString(), GUILayout.Width(40));
        GUILayout.Label(",", GUILayout.Width(15));
        string startYStr = GUILayout.TextField(playerStartPosition.y.ToString(), GUILayout.Width(40));
        if (int.TryParse(startXStr, out int newStartX))
            playerStartPosition.x = Mathf.Clamp(newStartX, 0, gridWidth - 1);
        if (int.TryParse(startYStr, out int newStartY))
            playerStartPosition.y = Mathf.Clamp(newStartY, 0, gridHeight - 1);
        GUILayout.EndHorizontal();

        // Success Criteria
        GUILayout.Label("Success Criteria:", GUI.skin.box);

        requireAllCubesDestroyed = GUILayout.Toggle(requireAllCubesDestroyed, "Require All Cubes Destroyed");

        GUILayout.BeginHorizontal();
        GUILayout.Label("Required Captures:", GUILayout.Width(120));
        string captureStr = GUILayout.TextField(requiredCaptureCount.ToString(), GUILayout.Width(60));
        if (int.TryParse(captureStr, out int newCaptures))
            requiredCaptureCount = Mathf.Max(0, newCaptures);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Max Escapes:", GUILayout.Width(120));
        string escapeStr = GUILayout.TextField(maxAllowedEscapes.ToString(), GUILayout.Width(60));
        if (int.TryParse(escapeStr, out int newEscapes))
            maxAllowedEscapes = Mathf.Max(-1, newEscapes);
        GUILayout.Label("(-1 = unlimited)", GUILayout.Width(100));
        GUILayout.EndHorizontal();

        // Wave Configuration
        GUILayout.Label("Wave Configuration:", GUI.skin.box);

        int waveCount = editingStage?.waveConfigurations?.Count ?? 0;
        GUILayout.Label($"Waves: {waveCount}");

        if (GUILayout.Button("Edit Waves (Open Wave Editor)"))
        {
            // This would open the Wave Editor focused on this stage's waves
            GUILayout.Label("Wave Editor integration would go here");
        }

        if (GUILayout.Button("Add New Wave"))
        {
            // Create a new basic wave and add it
            CreateAndAddBasicWave();
        }
    }

    private void DrawStageEditorActions()
    {
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Save Stage", GUILayout.Height(30)))
        {
            SaveCurrentStage();
        }
        GUI.backgroundColor = Color.blue;
        if (GUILayout.Button("Save & Test", GUILayout.Height(30)))
        {
            SaveCurrentStage();
            TestCurrentStage();
        }
        GUI.backgroundColor = Color.white;
        if (GUILayout.Button("Cancel", GUILayout.Height(30)))
        {
            CancelEditing();
        }
        GUILayout.EndHorizontal();

        if (!isCreatingNewStage && editingStage != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Original"))
            {
                LoadStageForEditing(editingStage); // Reload original values
            }
            if (GUILayout.Button("Duplicate Stage"))
            {
                CreateStageFromTemplate(editingStage);
            }
            GUILayout.EndHorizontal();
        }
    }

    private void DrawStageHistorySection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("STAGE HISTORY & STATISTICS", GUI.skin.box);

        if (stageManager != null)
        {
            var attempts = stageManager.GetStageAttempts();

            if (attempts.Count > 0)
            {
                GUILayout.Label("Stage Attempts:");
                foreach (var kvp in attempts.OrderBy(x => x.Key))
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"Stage {kvp.Key}: {kvp.Value} attempts");
                    if (GUILayout.Button("Load", GUILayout.Width(50)))
                    {
                        stageManager.LoadStage(kvp.Key);
                    }
                    GUILayout.EndHorizontal();
                }

                if (GUILayout.Button("Clear History"))
                {
                    // Clear stage attempts - would need to add this method to StageManager
                    GUILayout.Label("Clear history functionality would go here");
                }
            }
            else
            {
                GUILayout.Label("No stage attempts recorded yet");
            }

            // Additional statistics could go here
            GUILayout.Space(10);
            GUILayout.Label("Session Statistics:");
            GUILayout.Label($"Total Stages Available: {stageManager.GetAvailableStages().Count}");
            GUILayout.Label($"Current Session Time: {Time.time:F1}s");
        }

        GUILayout.EndVertical();
    }

    // Helper Methods

    private void StartCreatingNewStage()
    {
        isCreatingNewStage = true;
        editingStage = null;

        // Set default values
        stageNumber = GetNextAvailableStageNumber();
        stageName = $"New Stage {stageNumber}";
        stageDescription = "Enter stage description here...";
        stageObjective = "Enter stage objective here...";
        gridWidth = 6;
        gridHeight = 10;
        playerStartPosition = new Vector2Int(2, 0);
        requireAllCubesDestroyed = false;
        requiredCaptureCount = 0;
        maxAllowedEscapes = -1;

        showStageEditor = true;
    }

    private void LoadStageForEditing(StageData stage)
    {
        editingStage = stage;
        isCreatingNewStage = false;

        // Load values from stage
        stageNumber = stage.stageNumber;
        stageName = stage.stageName;
        stageDescription = stage.description;
        stageObjective = stage.objective;
        gridWidth = stage.gridWidth;
        gridHeight = stage.gridHeight;
        playerStartPosition = stage.playerStartPosition;
        requireAllCubesDestroyed = stage.requireAllCubesDestroyed;
        requiredCaptureCount = stage.requiredCaptureCount;
        maxAllowedEscapes = stage.maxAllowedEscapes;

        showStageEditor = true;
    }

    private void CreateStageFromTemplate(StageData templateStage)
    {
        StartCreatingNewStage();

        // Copy values from template
        stageName = templateStage.stageName + " (Copy)";
        stageDescription = templateStage.description;
        stageObjective = templateStage.objective;
        gridWidth = templateStage.gridWidth;
        gridHeight = templateStage.gridHeight;
        playerStartPosition = templateStage.playerStartPosition;
        requireAllCubesDestroyed = templateStage.requireAllCubesDestroyed;
        requiredCaptureCount = templateStage.requiredCaptureCount;
        maxAllowedEscapes = templateStage.maxAllowedEscapes;
    }

    private int GetNextAvailableStageNumber()
    {
        if (stageManager == null) return 1;

        var stages = stageManager.GetAvailableStages();
        if (stages.Count == 0) return 1;

        return stages.Max() + 1;
    }

    private void SaveCurrentStage()
    {
#if UNITY_EDITOR
        StageData stageToSave;

        if (isCreatingNewStage)
        {
            stageToSave = ScriptableObject.CreateInstance<StageData>();
        }
        else
        {
            stageToSave = editingStage;
        }

        // Apply all values
        stageToSave.stageNumber = stageNumber;
        stageToSave.stageName = stageName;
        stageToSave.description = stageDescription;
        stageToSave.objective = stageObjective;
        stageToSave.gridWidth = gridWidth;
        stageToSave.gridHeight = gridHeight;
        stageToSave.playerStartPosition = playerStartPosition;
        stageToSave.requireAllCubesDestroyed = requireAllCubesDestroyed;
        stageToSave.requiredCaptureCount = requiredCaptureCount;
        stageToSave.maxAllowedEscapes = maxAllowedEscapes;

        if (isCreatingNewStage)
        {
            // Create new asset
            string assetPath = $"Assets/data/stages/stage_{stageNumber:00}.asset";
            AssetDatabase.CreateAsset(stageToSave, assetPath);

            // Add to database
            if (stageDatabase != null)
            {
                stageDatabase.AddStage(stageToSave);
                EditorUtility.SetDirty(stageDatabase);
            }
        }
        else
        {
            EditorUtility.SetDirty(stageToSave);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Stage {stageNumber} saved successfully!");

        // Exit editing mode
        CancelEditing();
#else
        Debug.LogWarning("Stage saving only available in editor mode");
#endif
    }

    private void TestCurrentStage()
    {
        if (stageManager != null)
        {
            stageManager.LoadStage(stageNumber);
        }
    }

    private void CancelEditing()
    {
        editingStage = null;
        isCreatingNewStage = false;
        showStageEditor = false;
    }

    private void DeleteStage(int stageId)
    {
#if UNITY_EDITOR
        // Find and delete the stage asset
        string[] guids = AssetDatabase.FindAssets($"t:StageData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            StageData stage = AssetDatabase.LoadAssetAtPath<StageData>(path);
            if (stage != null && stage.stageNumber == stageId)
            {
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"Deleted stage {stageId} at {path}");
                break;
            }
        }

        AssetDatabase.Refresh();
#else
        Debug.LogWarning("Stage deletion only available in editor mode");
#endif
    }

    private void CreateAndAddBasicWave()
    {
        // This would create a basic wave and add it to the current stage
        // For now, just show a placeholder
        Debug.Log("Create and add basic wave - would integrate with Wave Editor");
    }
}
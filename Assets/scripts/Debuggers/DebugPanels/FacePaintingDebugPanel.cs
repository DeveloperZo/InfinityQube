using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Dedicated debug panel for face painting workflows and pattern management.
/// Provides specialized tools for pattern creation, testing, and rapid iteration.
/// </summary>
public class FacePaintingDebugPanel : DebugPanelBase
{
    #region Panel Properties
    public override string PanelName => "Face Painting";
    public override DebugPanelGroup Group => DebugPanelGroup.Testing;
    #endregion

    #region Manager References
    private FacePaintingManager facePaintingManager;
    private GridManager gridManager;
    private PlayerManager playerManager;
    private WaveManager waveManager;
    #endregion

    #region UI State
    // Section toggles
    private bool showPatternManagement = true;
    private bool showQuickSetup = true;
    private bool showPreviewControls = true;
    private bool showBatchOperations = true;
    private bool showStatusDisplay = true;

    // Pattern management state
    private string newPatternName = "Custom Pattern";
    private Vector2Int patternBasePosition = Vector2Int.zero;
    private List<Vector2Int> selectedPositions = new List<Vector2Int>();
    private int selectedPatternIndex = 0;

    // Quick setup state
    private FaceStatus selectedStatus = FaceStatus.Corrupted;
    private Color selectedColor = Color.red;
    private int selectedDuration = 3;
    private bool paintOnLanding = true;
    private bool paintOnExit = false;

    // Preview controls
    private bool showPatternPreviews = true;
    private bool showBatchPreviews = true;
    private int previewPatternIndex = 0;

    // Batch operations
    private int batchSize = 5;
    private Vector2Int batchStartPos = Vector2Int.zero;
    private Vector2Int batchEndPos = new Vector2Int(2, 2);

    // Position input helpers
    private Vector2 positionInput = Vector2.zero;
    private Vector2 scrollPosition = Vector2.zero;
    #endregion

    #region Initialization
    public override void Initialize()
    {
        base.Initialize();
        CacheManagerReferences();
        InitializeDefaults();
    }

    private void CacheManagerReferences()
    {
        facePaintingManager = FacePaintingManager.Instance ?? Object.FindObjectOfType<FacePaintingManager>();
        gridManager = GridManager.Instance ?? Object.FindObjectOfType<GridManager>();
        playerManager = Object.FindObjectOfType<PlayerManager>();
        waveManager = Object.FindObjectOfType<WaveManager>();
    }

    private void InitializeDefaults()
    {
        // Initialize selected positions with player position if available
        if (playerManager != null)
        {
            patternBasePosition = playerManager.currentTilePosition;
            selectedPositions.Add(playerManager.currentTilePosition);
        }

        // Set default colors based on status
        UpdateColorFromStatus();
    }
    #endregion

    #region Main Drawing Method
    protected override void DrawPanelContent()
    {
        if (!ValidateManagerReferences())
        {
            DrawManagerErrorState();
            return;
        }

        DrawSectionToggles();
        GUILayout.Space(5);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        try
        {
            if (showPatternManagement) DrawPatternManagement();
            if (showQuickSetup) DrawQuickSetup();
            if (showPreviewControls) DrawPreviewControls();
            if (showBatchOperations) DrawBatchOperations();
            if (showStatusDisplay) DrawStatusDisplay();
        }
        catch (System.Exception e)
        {
            GUILayout.Label($"Panel Error: {e.Message}");
            Debug.LogError($"FacePaintingDebugPanel error: {e.Message}\n{e.StackTrace}");
        }

        GUILayout.EndScrollView();
    }

    private void DrawSectionToggles()
    {
        GUILayout.Label("=== FACE PAINTING DEBUG PANEL ===", GUI.skin.box);
        
        GUILayout.BeginHorizontal();
        showPatternManagement = DrawSimpleToggle("Patterns", showPatternManagement);
        showQuickSetup = DrawSimpleToggle("Quick Setup", showQuickSetup);
        showPreviewControls = DrawSimpleToggle("Previews", showPreviewControls);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        showBatchOperations = DrawSimpleToggle("Batch Ops", showBatchOperations);
        showStatusDisplay = DrawSimpleToggle("Status", showStatusDisplay);
        GUILayout.EndHorizontal();
    }
    #endregion

    #region Pattern Management Section
    private void DrawPatternManagement()
    {
        DrawSimpleSection("PATTERN MANAGEMENT", () =>
        {
            // Pattern creation
            GUILayout.Label("Create New Pattern:", GUI.skin.label);
            newPatternName = GUILayout.TextField(newPatternName);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Base Position: ({patternBasePosition.x}, {patternBasePosition.y})");
            DrawSimpleButton("Use Player Pos", () =>
            {
                if (playerManager != null)
                    patternBasePosition = playerManager.currentTilePosition;
            });
            GUILayout.EndHorizontal();

            // Position management
            GUILayout.Label("Selected Positions:");
            DrawPositionList();
            DrawPositionInput();

            GUILayout.Space(5);

            // Pattern operations
            GUILayout.BeginHorizontal();
            DrawSimpleButton("Create Pattern", CreatePatternFromSelection);
            DrawSimpleButton("Clear Selection", () => selectedPositions.Clear());
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Existing patterns
            DrawExistingPatterns();
        });
    }

    private void DrawPositionList()
    {
        if (selectedPositions.Count == 0)
        {
            GUILayout.Label("  No positions selected");
            return;
        }

        for (int i = 0; i < selectedPositions.Count; i++)
        {
            GUILayout.BeginHorizontal();
            Vector2Int pos = selectedPositions[i];
            GUILayout.Label($"  [{i}] ({pos.x}, {pos.y})");
            
            DrawSimpleButton("Remove", () => selectedPositions.RemoveAt(i), 60);
            GUILayout.EndHorizontal();
        }
    }

    private void DrawPositionInput()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Add Position:");
        positionInput = DebugUIHelpers.DrawVector2Field("Add Position", positionInput, 0, gridManager.width, 0, gridManager.height);
        DrawSimpleButton("Add", () =>
        {
            Vector2Int newPos = new Vector2Int((int)positionInput.x, (int)positionInput.y);
            if (!selectedPositions.Contains(newPos))
            {
                selectedPositions.Add(newPos);
            }
        }, 50);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        DrawSimpleButton("Add Player Pos", () =>
        {
            if (playerManager != null)
            {
                Vector2Int playerPos = playerManager.currentTilePosition;
                if (!selectedPositions.Contains(playerPos))
                    selectedPositions.Add(playerPos);
            }
        });
        DrawSimpleButton("Add Adjacent", AddAdjacentPositions);
        GUILayout.EndHorizontal();
    }

    private void DrawExistingPatterns()
    {
        GUILayout.Label("Existing Patterns:");
        if (facePaintingManager.ActivePatternsCount == 0)
        {
            GUILayout.Label("  No active patterns");
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Pattern {selectedPatternIndex + 1} of {facePaintingManager.ActivePatternsCount}");
        
        GUILayout.BeginVertical();
        DrawSimpleButton("Prev", () => 
        {
            selectedPatternIndex = Mathf.Max(0, selectedPatternIndex - 1);
        }, 50);
        DrawSimpleButton("Next", () =>
        {
            selectedPatternIndex = Mathf.Min(facePaintingManager.ActivePatternsCount - 1, selectedPatternIndex + 1);
        }, 50);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        DrawSimpleButton("Apply Selected", ApplySelectedPattern);
        DrawSimpleButton("Delete Selected", DeleteSelectedPattern);
        GUILayout.EndHorizontal();
    }
    #endregion

    #region Quick Setup Section
    private void DrawQuickSetup()
    {
        DrawSimpleSection("QUICK SETUP", () =>
        {
            // Status and color selection
            GUILayout.Label("Face Status & Color:");
            GUILayout.BeginHorizontal();
            
            GUI.backgroundColor = selectedStatus == FaceStatus.Corrupted ? Color.red : Color.white;
            if (GUILayout.Button("Corrupted"))
            {
                selectedStatus = FaceStatus.Corrupted;
                UpdateColorFromStatus();
            }
            
            GUI.backgroundColor = selectedStatus == FaceStatus.Enhanced ? Color.blue : Color.white;
            if (GUILayout.Button("Enhanced"))
            {
                selectedStatus = FaceStatus.Enhanced;
                UpdateColorFromStatus();
            }
            
            GUI.backgroundColor = selectedStatus == FaceStatus.None ? Color.gray : Color.white;
            if (GUILayout.Button("None"))
            {
                selectedStatus = FaceStatus.None;
                selectedColor = Color.white;
            }
            
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            // Duration and trigger settings
            GUILayout.BeginHorizontal();
            GUILayout.Label("Duration:");
            selectedDuration = (int)GUILayout.HorizontalSlider(selectedDuration, -1, 10);
            GUILayout.Label(selectedDuration == -1 ? "∞" : selectedDuration.ToString(), GUILayout.Width(20));
            GUILayout.EndHorizontal();

            paintOnLanding = GUILayout.Toggle(paintOnLanding, "Paint on Landing");
            paintOnExit = GUILayout.Toggle(paintOnExit, "Paint on Exit");

            GUILayout.Space(5);

            // Quick setup patterns
            GUILayout.Label("Common Patterns:");
            
            GUILayout.BeginHorizontal();
            DrawSimpleButton("Player Tile", () => SetupPlayerTile());
            DrawSimpleButton("Cross Pattern", () => SetupCrossPattern());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawSimpleButton("Line Pattern", () => SetupLinePattern());
            DrawSimpleButton("Box Pattern", () => SetupBoxPattern());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawSimpleButton("Clear Player Tile", () => ClearPlayerTile());
            DrawSimpleButton("Clear All", () => facePaintingManager.ClearAllFacePainting());
            GUILayout.EndHorizontal();
        });
    }

    private void UpdateColorFromStatus()
    {
        switch (selectedStatus)
        {
            case FaceStatus.Corrupted:
                selectedColor = Color.red;
                break;
            case FaceStatus.Enhanced:
                selectedColor = Color.blue;
                break;
            default:
                selectedColor = Color.white;
                break;
        }
    }
    #endregion

    #region Preview Controls Section
    private void DrawPreviewControls()
    {
        DrawSimpleSection("PREVIEW CONTROLS", () =>
        {
            // Preview toggles
            showPatternPreviews = GUILayout.Toggle(showPatternPreviews, "Show Pattern Previews");
            showBatchPreviews = GUILayout.Toggle(showBatchPreviews, "Show Batch Previews");

            GUILayout.Space(5);

            // Pattern preview selection
            if (facePaintingManager.ActivePatternsCount > 0)
            {
                GUILayout.Label("Preview Pattern:");
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Pattern {previewPatternIndex + 1} of {facePaintingManager.ActivePatternsCount}");
                
                DrawSimpleButton("Prev", () =>
                {
                    previewPatternIndex = Mathf.Max(0, previewPatternIndex - 1);
                }, 50);
                DrawSimpleButton("Next", () =>
                {
                    previewPatternIndex = Mathf.Min(facePaintingManager.ActivePatternsCount - 1, previewPatternIndex + 1);
                }, 50);
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                DrawSimpleButton("Show Preview", ShowPatternPreview);
                DrawSimpleButton("Hide Preview", () => facePaintingManager.ClearAllPreviews());
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("No patterns to preview");
            }

            GUILayout.Space(5);

            // Batch preview
            GUILayout.Label("Batch Preview:");
            GUILayout.BeginHorizontal();
            DrawSimpleButton("Preview Selection", PreviewSelectedPositions);
            DrawSimpleButton("Clear Previews", () => facePaintingManager.ClearAllPreviews());
            GUILayout.EndHorizontal();
        });
    }
    #endregion

    #region Batch Operations Section
    private void DrawBatchOperations()
    {
        DrawSimpleSection("BATCH OPERATIONS", () =>
        {
            // Batch area definition
            GUILayout.Label("Batch Area:");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Start:");
            batchStartPos = DrawVector2IntField(batchStartPos);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("End:");
            batchEndPos = DrawVector2IntField(batchEndPos);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Size:");
            batchSize = (int)GUILayout.HorizontalSlider(batchSize, 1, 20);
            GUILayout.Label(batchSize.ToString(), GUILayout.Width(30));
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Batch operations
            GUILayout.Label("Batch Operations:");
            
            GUILayout.BeginHorizontal();
            DrawSimpleButton("Apply to Area", ApplyToArea);
            DrawSimpleButton("Apply to Selection", ApplyToSelection);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawSimpleButton("Clear Area", ClearArea);
            DrawSimpleButton("Copy Patterns", CopyPatternsToClipboard);
            GUILayout.EndHorizontal();

            // Advanced batch operations
            GUILayout.Space(5);
            GUILayout.Label("Advanced Operations:");
            
            GUILayout.BeginHorizontal();
            DrawSimpleButton("Corruption Line", () => CreateCorruptionLine());
            DrawSimpleButton("Enhancement Zone", () => CreateEnhancementZone());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawSimpleButton("Random Pattern", () => CreateRandomPattern());
            DrawSimpleButton("Checkerboard", () => CreateCheckerboardPattern());
            GUILayout.EndHorizontal();
        });
    }

    private Vector2Int DrawVector2IntField(Vector2Int value)
    {
        GUILayout.BeginVertical();
        value.x = (int)GUILayout.HorizontalSlider(value.x, 0, gridManager != null ? gridManager.Width - 1 : 10);
        value.y = (int)GUILayout.HorizontalSlider(value.y, 0, gridManager != null ? gridManager.Height - 1 : 10);
        GUILayout.Label($"({value.x}, {value.y})");
        GUILayout.EndVertical();
        return value;
    }
    #endregion

    #region Status Display Section
    private void DrawStatusDisplay()
    {
        DrawSimpleSection("STATUS DISPLAY", () =>
        {
            // Manager status
            GUILayout.Label("Manager Status:", GUI.skin.label);
            GUILayout.Label($"  Face Painting Enabled: {facePaintingManager.IsFacePaintingEnabled}");
            GUILayout.Label($"  Active Patterns: {facePaintingManager.ActivePatternsCount}");
            GUILayout.Label($"  Registered Tiles: {facePaintingManager.FacePaintingTilesCount}");

            GUILayout.Space(5);

            // Active patterns details
            if (facePaintingManager.ActivePatternsCount > 0)
            {
                GUILayout.Label("Active Patterns:", GUI.skin.label);
                var debugData = facePaintingManager.GetDebugData();
                
                if (debugData.ContainsKey("Pattern Names") && debugData["Pattern Names"] is List<string> patternNames)
                {
                    var entryCounts = debugData["Pattern Entry Counts"] as List<int>;
                    
                    for (int i = 0; i < patternNames.Count; i++)
                    {
                        int entries = entryCounts != null && i < entryCounts.Count ? entryCounts[i] : 0;
                        GUILayout.Label($"  [{i}] {patternNames[i]} ({entries} entries)");
                    }
                }
            }

            GUILayout.Space(5);

            // Tile status breakdown
            GUILayout.Label("Tile Status:", GUI.skin.label);
            var statusData = facePaintingManager.GetDebugData();
            if (statusData.ContainsKey("Tile Status Counts") && statusData["Tile Status Counts"] is Dictionary<string, int> statusCounts)
            {
                foreach (var kvp in statusCounts)
                {
                    GUILayout.Label($"  {kvp.Key}: {kvp.Value} tiles");
                }
            }
            else
            {
                GUILayout.Label("  No active face painting tiles");
            }

            GUILayout.Space(5);

            // Coordination status
            GUILayout.Label("Coordination Status:", GUI.skin.label);
            if (statusData.ContainsKey("Tracked Cubes Count"))
            {
                GUILayout.Label($"  Tracked Cubes: {statusData["Tracked Cubes Count"]}");
            }
            if (statusData.ContainsKey("Active Previews Count"))
            {
                GUILayout.Label($"  Active Previews: {statusData["Active Previews Count"]}");
            }

            GUILayout.Space(5);

            // System actions
            GUILayout.Label("System Actions:", GUI.skin.label);
            
            GUILayout.BeginHorizontal();
            DrawSimpleButton("Refresh Status", () => facePaintingManager.DebugPrintStatus());
            DrawSimpleButton("Validate System", () => facePaintingManager.ValidateAllFacePaintingStates());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            DrawSimpleButton("Reset Defaults", () => facePaintingManager.ResetToDefaults());
            DrawSimpleButton("Generate Report", () => facePaintingManager.GenerateSystemReport());
            GUILayout.EndHorizontal();
        });
    }
    #endregion

    #region Pattern Operations
    private void CreatePatternFromSelection()
    {
        if (selectedPositions.Count == 0)
        {
            Debug.LogWarning("No positions selected for pattern creation");
            return;
        }

        var pattern = facePaintingManager.CreatePatternFromTiles(newPatternName, patternBasePosition, selectedPositions);
        
        if (pattern.entries.Count > 0)
        {
            Debug.Log($"Created pattern '{newPatternName}' with {pattern.entries.Count} entries");
            newPatternName = "Custom Pattern"; // Reset name for next pattern
        }
        else
        {
            Debug.LogWarning("No valid tiles found for pattern creation");
        }
    }

    private void ApplySelectedPattern()
    {
        // This would require access to the active patterns list from the manager
        // For now, we'll log that this functionality needs the pattern list to be exposed
        Debug.Log($"Apply pattern {selectedPatternIndex} - requires pattern list access from manager");
    }

    private void DeleteSelectedPattern()
    {
        // Similar to apply - would need pattern management methods in the manager
        Debug.Log($"Delete pattern {selectedPatternIndex} - requires pattern management methods in manager");
    }

    private void AddAdjacentPositions()
    {
        if (selectedPositions.Count == 0) return;

        Vector2Int lastPos = selectedPositions[selectedPositions.Count - 1];
        Vector2Int[] adjacents = {
            lastPos + Vector2Int.up,
            lastPos + Vector2Int.down,
            lastPos + Vector2Int.left,
            lastPos + Vector2Int.right
        };

        foreach (var adj in adjacents)
        {
            if (gridManager != null && gridManager.IsValidGridPosition(adj) && !selectedPositions.Contains(adj))
            {
                selectedPositions.Add(adj);
            }
        }
    }
    #endregion

    #region Quick Setup Operations
    private void SetupPlayerTile()
    {
        if (playerManager == null) return;
        
        Vector2Int playerPos = playerManager.currentTilePosition;
        facePaintingManager.SetupSingleTilePainting(playerPos, selectedStatus, selectedColor, selectedDuration);
    }

    private void SetupCrossPattern()
    {
        if (playerManager == null) return;
        
        Vector2Int center = playerManager.currentTilePosition;
        List<Vector2Int> positions = new List<Vector2Int>
        {
            center,
            center + Vector2Int.up,
            center + Vector2Int.down,
            center + Vector2Int.left,
            center + Vector2Int.right
        };

        facePaintingManager.BatchSetFacePainting(positions, selectedStatus, selectedColor, selectedDuration, paintOnLanding, paintOnExit);
    }

    private void SetupLinePattern()
    {
        if (playerManager == null) return;
        
        Vector2Int start = playerManager.currentTilePosition;
        List<Vector2Int> positions = new List<Vector2Int>();
        
        for (int i = 0; i < 5; i++)
        {
            positions.Add(start + Vector2Int.right * i);
        }

        facePaintingManager.BatchSetFacePainting(positions, selectedStatus, selectedColor, selectedDuration, paintOnLanding, paintOnExit);
    }

    private void SetupBoxPattern()
    {
        if (playerManager == null) return;
        
        Vector2Int topLeft = playerManager.currentTilePosition;
        List<Vector2Int> positions = new List<Vector2Int>();
        
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                positions.Add(topLeft + new Vector2Int(x, y));
            }
        }

        facePaintingManager.BatchSetFacePainting(positions, selectedStatus, selectedColor, selectedDuration, paintOnLanding, paintOnExit);
    }

    private void ClearPlayerTile()
    {
        if (playerManager == null) return;
        
        Vector2Int playerPos = playerManager.currentTilePosition;
        facePaintingManager.SetupSingleTilePainting(playerPos, FaceStatus.None, Color.white, 0);
    }
    #endregion

    #region Preview Operations
    private void ShowPatternPreview()
    {
        // This would require access to the patterns list to show specific pattern preview
        Debug.Log($"Show preview for pattern {previewPatternIndex} - requires pattern access from manager");
    }

    private void PreviewSelectedPositions()
    {
        if (selectedPositions.Count == 0)
        {
            Debug.LogWarning("No positions selected for preview");
            return;
        }

        facePaintingManager.ShowBatchPreview(selectedPositions, selectedColor);
    }
    #endregion

    #region Batch Operations
    private void ApplyToArea()
    {
        List<Vector2Int> areaPositions = GetAreaPositions(batchStartPos, batchEndPos);
        facePaintingManager.BatchSetFacePainting(areaPositions, selectedStatus, selectedColor, selectedDuration, paintOnLanding, paintOnExit);
    }

    private void ApplyToSelection()
    {
        if (selectedPositions.Count == 0)
        {
            Debug.LogWarning("No positions selected for batch operation");
            return;
        }

        facePaintingManager.BatchSetFacePainting(selectedPositions, selectedStatus, selectedColor, selectedDuration, paintOnLanding, paintOnExit);
    }

    private void ClearArea()
    {
        List<Vector2Int> areaPositions = GetAreaPositions(batchStartPos, batchEndPos);
        facePaintingManager.BatchSetFacePainting(areaPositions, FaceStatus.None, Color.white, 0);
    }

    private void CreateCorruptionLine()
    {
        Vector2Int start = batchStartPos;
        Vector2Int end = batchEndPos;
        List<Vector2Int> linePositions = GetLinePositions(start, end);
        
        facePaintingManager.SetupCorruptionPattern(linePositions, selectedDuration);
    }

    private void CreateEnhancementZone()
    {
        List<Vector2Int> areaPositions = GetAreaPositions(batchStartPos, batchEndPos);
        facePaintingManager.SetupEnhancementPattern(areaPositions, selectedDuration);
    }

    private void CreateRandomPattern()
    {
        List<Vector2Int> areaPositions = GetAreaPositions(batchStartPos, batchEndPos);
        List<Vector2Int> randomPositions = new List<Vector2Int>();
        
        int count = Mathf.Min(batchSize, areaPositions.Count);
        for (int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, areaPositions.Count);
            if (!randomPositions.Contains(areaPositions[randomIndex]))
            {
                randomPositions.Add(areaPositions[randomIndex]);
            }
        }

        facePaintingManager.BatchSetFacePainting(randomPositions, selectedStatus, selectedColor, selectedDuration, paintOnLanding, paintOnExit);
    }

    private void CreateCheckerboardPattern()
    {
        List<Vector2Int> areaPositions = GetAreaPositions(batchStartPos, batchEndPos);
        List<Vector2Int> checkerPositions = new List<Vector2Int>();
        
        foreach (var pos in areaPositions)
        {
            if ((pos.x + pos.y) % 2 == 0)
            {
                checkerPositions.Add(pos);
            }
        }

        facePaintingManager.BatchSetFacePainting(checkerPositions, selectedStatus, selectedColor, selectedDuration, paintOnLanding, paintOnExit);
    }

    private void CopyPatternsToClipboard()
    {
        // For now, just log the pattern information
        Debug.Log("Copy patterns to clipboard - feature placeholder");
        Debug.Log($"Selected positions: {string.Join(", ", selectedPositions.Select(p => $"({p.x},{p.y})"))}");
        Debug.Log($"Status: {selectedStatus}, Color: {selectedColor}, Duration: {selectedDuration}");
    }
    #endregion

    #region Utility Methods
    private List<Vector2Int> GetAreaPositions(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
        int minX = Mathf.Min(start.x, end.x);
        int maxX = Mathf.Max(start.x, end.x);
        int minY = Mathf.Min(start.y, end.y);
        int maxY = Mathf.Max(start.y, end.y);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (gridManager != null && gridManager.IsValidGridPosition(pos))
                {
                    positions.Add(pos);
                }
            }
        }

        return positions;
    }

    private List<Vector2Int> GetLinePositions(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> positions = new List<Vector2Int>();
        
        Vector2Int current = start;
        Vector2Int direction = new Vector2Int(
            end.x > start.x ? 1 : end.x < start.x ? -1 : 0,
            end.y > start.y ? 1 : end.y < start.y ? -1 : 0
        );

        while (current != end)
        {
            if (gridManager != null && gridManager.IsValidGridPosition(current))
            {
                positions.Add(current);
            }

            current += direction;
        }

        // Add end position
        if (gridManager != null && gridManager.IsValidGridPosition(end))
        {
            positions.Add(end);
        }

        return positions;
    }

    private bool ValidateManagerReferences()
    {
        return facePaintingManager != null && gridManager != null;
    }

    private void DrawManagerErrorState()
    {
        GUILayout.Label("=== MANAGER ERROR ===", GUI.skin.box);
        GUILayout.Label("Required managers not found:");
        GUILayout.Label($"  FacePaintingManager: {(facePaintingManager != null ? "✓" : "✗")}");
        GUILayout.Label($"  GridManager: {(gridManager != null ? "✓" : "✗")}");
        GUILayout.Label($"  PlayerManager: {(playerManager != null ? "✓" : "✗")}");
        GUILayout.Label($"  WaveManager: {(waveManager != null ? "✓" : "✗")}");
        
        GUILayout.Space(10);
        DrawSimpleButton("Retry Manager Search", CacheManagerReferences);
    }
    #endregion
}

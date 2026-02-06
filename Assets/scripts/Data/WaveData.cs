using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using static Enumerations;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Wave", menuName = "Infinity Qube/Wave Data")]
public class WaveData : ScriptableObject
{
    #region Wave Identity
    
    [TabGroup("Main", "Identity")]
    [Title("Wave Identity")]
    [HorizontalGroup("Main/Identity/Basic", LabelWidth = 50)]
    [LabelText("Index")]
    [Tooltip("Wave index within the stage (0-based)")]
    public int Index = 0;  // Keep exact name for asset deserialization
    
    [HorizontalGroup("Main/Identity/Basic")]
    [LabelText("Name")]
    [Tooltip("Display name for this wave (optional)")]
    public string waveName;
    
    // Alias property for new code style
    public int waveIndex { get => Index; set => Index = value; }
    
    [TabGroup("Main", "Identity")]
    [Title("Spawn Area")]
    [InfoBox("Defines the cube spawn grid dimensions", InfoMessageType.None)]
    [HorizontalGroup("Main/Identity/SpawnSize", LabelWidth = 50)]
    [LabelText("Width")]
    [Tooltip("Width of the spawn area for this wave")]
    [Range(1, 12)] 
    [OnValueChanged("OnGridDimensionsChanged")]
    public int GridWidth = 3;  // Keep exact name for asset deserialization
    
    [HorizontalGroup("Main/Identity/SpawnSize")]
    [LabelText("Height")]
    [Tooltip("Height/depth of the spawn area")]
    [Range(1, 10)] 
    [OnValueChanged("OnGridDimensionsChanged")]
    public int GridHeight = 3;  // Keep exact name for asset deserialization
    
    // Alias properties for new code style
    public int spawnWidth { get => GridWidth; set => GridWidth = value; }
    public int spawnHeight { get => GridHeight; set => GridHeight = value; }
    
    [TabGroup("Main", "Identity")]
    [FoldoutGroup("Main/Identity/Advanced Settings")]
    [LabelText("Override Playable Grid Height")]
    [LabelWidth(180)]
    [Tooltip("Override playable grid height for this wave (0 = use stage default)")]
    [Range(0, 40)] public int overrideGridHeight = 0;
    
    #endregion
    
    // NOTE: Segment layout is defined at stage level (StageData.segmentLayoutPrefab), not wave level

    #region Cube Configuration - Grid Visualization
    
    [TabGroup("Main", "Cubes")]
    [PropertyOrder(-1)]
    [OnInspectorGUI("DrawGridVisualization", append: false)]
    [HideLabel]
    private int _gridVisualizationDummy; // Dummy: OnInspectorGUI draws grid (do not use ShowIf false or HideInInspector or callback is skipped)
    
    // Editor-only state for grid visualization
    #if UNITY_EDITOR
    [System.NonSerialized] private CubeType _selectedBrush = CubeType.Unit;
    [System.NonSerialized] private int _previousGridWidth = -1;
    [System.NonSerialized] private int _previousGridHeight = -1;
    
    private const float CELL_SIZE = 28f;
    #endif
    
    #if UNITY_EDITOR
    private string GetCubesSummary()
    {
        if (CubesData == null || CubesData.Count == 0)
            return "No cubes configured - click grid cells to place cubes";
        
        int unit = 0, matrix = 0, recursion = 0, infinity = 0;
        foreach (var cube in CubesData)
        {
            switch (cube.type)
            {
                case CubeType.Unit: unit++; break;
                case CubeType.Matrix: matrix++; break;
                case CubeType.Recursion: recursion++; break;
                case CubeType.Infinity: infinity++; break;
            }
        }
        return $"{CubesData.Count} cubes | {unit} Unit | {matrix} Matrix | {recursion} Recursion | {infinity} Infinity";
    }
    
    private void DrawGridVisualization()
    {
        // Title
        var titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 13;
        EditorGUILayout.LabelField("Cube Grid Editor", titleStyle);
        
        // Summary
        EditorGUILayout.LabelField(GetCubesSummary(), EditorStyles.miniLabel);
        
        EditorGUILayout.Space(8);
        
        // Brush selector and actions
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Brush:", GUILayout.Width(40));
        _selectedBrush = (CubeType)EditorGUILayout.EnumPopup(_selectedBrush, GUILayout.Width(90));
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("Clear All", GUILayout.Width(70), GUILayout.Height(20)))
        {
            if (EditorUtility.DisplayDialog("Clear All Cubes", 
                "Remove all cubes from this wave?", "Clear", "Cancel"))
            {
                Undo.RecordObject(this, "Clear All Cubes");
                CubesData.Clear();
                EditorUtility.SetDirty(this);
            }
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(6);
        
        // Draw the grid
        DrawGrid();
        
        EditorGUILayout.Space(5);
        
        // Fill row buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill Top Row (Unit)", GUILayout.Height(22)))
        {
            FillRow(GridHeight - 1, CubeType.Unit);
        }
        if (GUILayout.Button("Fill Top Row (Brush)", GUILayout.Height(22)))
        {
            FillRow(GridHeight - 1, _selectedBrush);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
    }
    
    private void DrawGrid()
    {
        int gridWidth = Mathf.Max(1, GridWidth);
        int gridHeight = Mathf.Max(1, GridHeight);
        
        // Create lookup grid
        CubeData[,] grid = new CubeData[gridWidth, gridHeight];
        foreach (var cube in CubesData)
        {
            if (cube.position.x >= 0 && cube.position.x < gridWidth &&
                cube.position.y >= 0 && cube.position.y < gridHeight)
            {
                grid[cube.position.x, cube.position.y] = cube;
            }
        }
        
        // Column headers
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(22);
        for (int x = 0; x < gridWidth; x++)
        {
            EditorGUILayout.LabelField(x.ToString(), EditorStyles.centeredGreyMiniLabel, 
                GUILayout.Width(CELL_SIZE), GUILayout.Height(14));
        }
        EditorGUILayout.EndHorizontal();
        
        // Grid rows (Y=0 is bottom, displayed at bottom)
        for (int y = gridHeight - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Row header
            EditorGUILayout.LabelField(y.ToString(), EditorStyles.centeredGreyMiniLabel, 
                GUILayout.Width(20), GUILayout.Height(CELL_SIZE));
            
            // Cells
            for (int x = 0; x < gridWidth; x++)
            {
                DrawGridCell(x, y, grid[x, y]);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        // Legend
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        DrawLegendItem("U", GetCubeColor(CubeType.Unit), "Unit");
        DrawLegendItem("M", GetCubeColor(CubeType.Matrix), "Matrix");
        DrawLegendItem("R", GetCubeColor(CubeType.Recursion), "Recursion");
        DrawLegendItem("∞", GetCubeColor(CubeType.Infinity), "Infinity");
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Left-click: Add/Remove | Right-click: Cycle type", EditorStyles.miniLabel);
        EditorGUILayout.EndHorizontal();
    }
    
    private void DrawLegendItem(string label, Color color, string tooltip)
    {
        var style = new GUIStyle(EditorStyles.miniLabel);
        style.normal.textColor = color;
        style.fontStyle = FontStyle.Bold;
        EditorGUILayout.LabelField(new GUIContent($"[{label}] {tooltip}", tooltip), style, GUILayout.Width(80));
    }
    
    private void DrawGridCell(int x, int y, CubeData existingCube)
    {
        Rect cellRect = GUILayoutUtility.GetRect(CELL_SIZE, CELL_SIZE, 
            GUILayout.Width(CELL_SIZE), GUILayout.Height(CELL_SIZE));
        
        // Background
        Color bgColor = existingCube != null ? GetCubeColor(existingCube.type) * 0.4f : new Color(0.2f, 0.2f, 0.2f);
        EditorGUI.DrawRect(cellRect, bgColor);
        
        // Border
        Color borderColor = existingCube != null ? GetCubeColor(existingCube.type) : new Color(0.4f, 0.4f, 0.4f);
        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1), borderColor);
        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1, cellRect.height), borderColor);
        EditorGUI.DrawRect(new Rect(cellRect.x + cellRect.width - 1, cellRect.y, 1, cellRect.height), borderColor);
        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y + cellRect.height - 1, cellRect.width, 1), borderColor);
        
        // Label
        if (existingCube != null)
        {
            var labelStyle = new GUIStyle(EditorStyles.boldLabel);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = GetCubeColor(existingCube.type);
            labelStyle.fontSize = 12;
            GUI.Label(cellRect, GetCubeTypeLabel(existingCube.type), labelStyle);
        }
        
        // Handle clicks
        Event e = Event.current;
        if (e.type == EventType.MouseDown && cellRect.Contains(e.mousePosition))
        {
            Undo.RecordObject(this, "Edit Wave Cube");
            
            if (e.button == 0) // Left click - add/remove
            {
                if (existingCube != null)
                {
                    CubesData.Remove(existingCube);
                }
                else
                {
                    CubesData.Add(new CubeData(_selectedBrush, new Vector2Int(x, y)));
                }
                EditorUtility.SetDirty(this);
                e.Use();
            }
            else if (e.button == 1 && existingCube != null) // Right click - cycle type
            {
                int currentType = (int)existingCube.type;
                existingCube.type = (CubeType)((currentType + 1) % 4);
                EditorUtility.SetDirty(this);
                e.Use();
            }
        }
    }
    
    private Color GetCubeColor(CubeType type)
    {
        return type switch
        {
            CubeType.Unit => new Color(0.3f, 0.9f, 0.3f),      // Green
            CubeType.Matrix => new Color(0.3f, 0.5f, 1f),      // Blue
            CubeType.Recursion => new Color(1f, 0.9f, 0.2f),   // Yellow
            CubeType.Infinity => new Color(1f, 0.3f, 0.3f),    // Red
            _ => Color.white
        };
    }
    
    private string GetCubeTypeLabel(CubeType type)
    {
        return type switch
        {
            CubeType.Unit => "U",
            CubeType.Matrix => "M",
            CubeType.Recursion => "R",
            CubeType.Infinity => "∞",
            _ => "?"
        };
    }
    
    private void FillRow(int y, CubeType type)
    {
        Undo.RecordObject(this, "Fill Row");
        
        // Remove existing cubes in this row
        CubesData.RemoveAll(cube => cube.position.y == y);
        
        // Add cubes for entire row
        for (int x = 0; x < GridWidth; x++)
        {
            CubesData.Add(new CubeData(type, new Vector2Int(x, y)));
        }
        
        EditorUtility.SetDirty(this);
    }
    
    private void OnGridDimensionsChanged()
    {
        // Handle grid resize - remove out-of-bounds cubes
        if (CubesData != null)
        {
            CubesData.RemoveAll(cube => 
                cube.position.x >= GridWidth || cube.position.y >= GridHeight);
        }
    }
    #endif
    
    #endregion

    #region Cube Data List (Hidden - use grid editor above)
    
    [TabGroup("Main", "Cubes")]
    [Title("Raw Cube Data")]
    [FoldoutGroup("Main/Cubes/Advanced - Raw Data")]
    [ListDrawerSettings(ShowIndexLabels = true, NumberOfItemsPerPage = 10)]
    [Tooltip("Direct cube data list - use grid editor above for visual editing")]
    public List<CubeData> CubesData = new List<CubeData>();  // Keep exact name for asset deserialization
    
    // Alias property for new code style
    public List<CubeData> cubes { get => CubesData; set => CubesData = value; }
    
    #endregion

    #region Marker Economy
    
    [TabGroup("Main", "Economy")]
    [Title("Wave Grants")]
    [InfoBox("Unit markers have infinite regeneration. Wave grants only affect Matrix, Recursion, and Infinity markers.", InfoMessageType.None)]
    [LabelText("Grants Add to Inventory")]
    [ToggleLeft]
    [Tooltip("If enabled, grants ADD to current inventory. Otherwise, they SET inventory.")]
    public bool grantsAddToInventory = true;
    
    [TabGroup("Main", "Economy")]
    [BoxGroup("Main/Economy/Grant Amounts")]
    [HorizontalGroup("Main/Economy/Grant Amounts/Row", LabelWidth = 65)]
    [LabelText("Matrix")]
    [Tooltip("Matrix marker charges granted (0 = none)")]
    [Range(0, 10)] public int grantMatrixCharges = 0;
    
    [HorizontalGroup("Main/Economy/Grant Amounts/Row")]
    [LabelText("Recursion")]
    [Tooltip("Recursion marker charges granted (0 = none)")]
    [Range(0, 10)] public int grantRecursionCharges = 0;
    
    [HorizontalGroup("Main/Economy/Grant Amounts/Row")]
    [LabelText("Infinity")]
    [Tooltip("Infinity marker charges granted (0 = none)")]
    [Range(0, 5)] public int grantInfinityCharges = 0;
    
    #endregion

    #region Marker Caps (Wave-Level Overrides)
    
    [TabGroup("Main", "Economy")]
    [FoldoutGroup("Main/Economy/Wave Overrides (0 = Stage Default)")]
    [Title("Max Markers On Grid")]
    [HorizontalGroup("Main/Economy/Wave Overrides (0 = Stage Default)/MaxGrid", LabelWidth = 60)]
    [LabelText("Unit")]
    [Tooltip("Override max Unit markers on grid (0 = stage default)")]
    [Range(0, 10)] public int overrideUnitMaxOnGrid = 0;
    
    [HorizontalGroup("Main/Economy/Wave Overrides (0 = Stage Default)/MaxGrid")]
    [LabelText("Matrix")]
    [Tooltip("Override max Matrix markers on grid (0 = stage default)")]
    [Range(0, 5)] public int overrideMatrixMaxOnGrid = 0;
    
    [HorizontalGroup("Main/Economy/Wave Overrides (0 = Stage Default)/MaxGrid")]
    [LabelText("Recursion")]
    [Tooltip("Override max Recursion markers on grid (0 = stage default)")]
    [Range(0, 5)] public int overrideRecursionMaxOnGrid = 0;
    
    [HorizontalGroup("Main/Economy/Wave Overrides (0 = Stage Default)/MaxGrid")]
    [LabelText("Infinity")]
    [Tooltip("Override max Infinity markers on grid (0 = stage default)")]
    [Range(0, 3)] public int overrideInfinityMaxOnGrid = 0;
    
    [FoldoutGroup("Main/Economy/Wave Overrides (0 = Stage Default)")]
    [Title("Unit Regeneration Overrides")]
    [HorizontalGroup("Main/Economy/Wave Overrides (0 = Stage Default)/UnitRegen", LabelWidth = 90)]
    [LabelText("Recharge Rate")]
    [Tooltip("Moves per charge regenerated (0 = stage default)")]
    [Range(0, 10)] public int overrideUnitMarkerRechargeRate = 0;
    
    [HorizontalGroup("Main/Economy/Wave Overrides (0 = Stage Default)/UnitRegen")]
    [LabelText("Max Charges")]
    [Tooltip("Max charges in regeneration pool (0 = stage default)")]
    [Range(0, 10)] public int overrideMaxUnitMarkerCharges = 0;
    
    #endregion

    #region Wave Timing
    
    [TabGroup("Main", "Timing")]
    [Title("Timing Settings")]
    [LabelText("Wave Start Delay")]
    [LabelWidth(120)]
    [Tooltip("Seconds to wait before spawning cubes")]
    [Range(0f, 10f)] public float waveStartDelay = 1f;
    
    [TabGroup("Main", "Timing")]
    [Title("Move Speed")]
    [HorizontalGroup("Main/Timing/Speeds", LabelWidth = 100)]
    [LabelText("Normal Interval")]
    [Tooltip("Seconds between cube movements (normal speed)")]
    [Range(0.1f, 3f)] public float moveInterval = 0.5f;
    
    [HorizontalGroup("Main/Timing/Speeds")]
    [LabelText("Fast Interval")]
    [Tooltip("Seconds between cube movements (fast-forward)")]
    [Range(0.05f, 1f)] public float fastMoveInterval = 0.1f;
    
    [TabGroup("Main", "Timing")]
    [LabelText("Respawn Delay (Moves)")]
    [LabelWidth(140)]
    [Tooltip("Move steps before player respawns after death (0 = stage default)")]
    [Range(0, 10)] public int respawnDelayMoves = 0;
    
    [TabGroup("Main", "Timing")]
    [FoldoutGroup("Main/Timing/Wave Success Criteria")]
    [LabelText("Use Wave-Specific Criteria")]
    [ToggleLeft]
    [Tooltip("Enable to override stage success criteria for this wave")]
    public bool hasOwnSuccessCriteria = false;
    
    [FoldoutGroup("Main/Timing/Wave Success Criteria")]
    [ShowIf("hasOwnSuccessCriteria")]
    [LabelText("Required Captures")]
    [LabelWidth(130)]
    [Tooltip("Minimum captures required (0 = no requirement)")]
    [Range(0, 50)] public int requiredCaptureCount = 0;
    
    [FoldoutGroup("Main/Timing/Wave Success Criteria")]
    [ShowIf("hasOwnSuccessCriteria")]
    [LabelText("Max Escapes Allowed")]
    [LabelWidth(130)]
    [Tooltip("Maximum escapes before failure (0 = none allowed, -1 = unlimited)")]
    [Range(-1, 20)] public int maxAllowedEscapes = -1;
    
    #endregion

    // Messages removed - use highlightSequences instead (sequences contain messageText field)
    
    #region Highlight Sequences
    
    [TabGroup("Main", "Sequences")]
    [PropertyOrder(-10)]
    [OnInspectorGUI("DrawSequenceVisualizer", append: false)]
    [HideLabel]
    private int _sequenceVisualizerDummy; // Dummy: OnInspectorGUI draws sequence editor (do not use ShowIf false or callback is skipped)
    
    #if UNITY_EDITOR
    [System.NonSerialized] private int _selectedSequenceIndex = -1;
    
    private void DrawSequenceVisualizer()
    {
        // Title
        var titleStyle = new GUIStyle(EditorStyles.boldLabel);
        titleStyle.fontSize = 13;
        EditorGUILayout.LabelField("Sequence Editor", titleStyle);
        
        // Summary
        string summary = GetSequenceSummary();
        EditorGUILayout.LabelField(summary, EditorStyles.miniLabel);
        
        EditorGUILayout.Space(8);
        
        // Quick add buttons - two rows for better fit
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Message", GUILayout.Height(24)))
            AddSequence(SequencePreset.MessageOnly);
        if (GUILayout.Button("+ Tile Highlight", GUILayout.Height(24)))
            AddSequence(SequencePreset.HighlightTile);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Cube Highlight", GUILayout.Height(24)))
            AddSequence(SequencePreset.HighlightCube);
        if (GUILayout.Button("+ Full Tutorial", GUILayout.Height(24)))
            AddSequence(SequencePreset.FullTutorial);
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(8);
        
        if (highlightSequences == null || highlightSequences.Count == 0)
        {
            EditorGUILayout.HelpBox("No sequences configured. Use buttons above to add tutorial sequences.", MessageType.Info);
            EditorGUILayout.Space(10);
            return;
        }
        
        // Draw sequence cards
        for (int i = 0; i < highlightSequences.Count; i++)
        {
            DrawSequenceCard(i, highlightSequences[i]);
            EditorGUILayout.Space(2);
        }
        
        EditorGUILayout.Space(8);
    }
    
    private string GetSequenceSummary()
    {
        if (highlightSequences == null || highlightSequences.Count == 0)
            return "No sequences configured";
        
        int withMessage = 0, withHighlight = 0;
        foreach (var seq in highlightSequences)
        {
            if (!string.IsNullOrEmpty(seq.messageText)) withMessage++;
            if (seq.targetType != HighlightTargetType.None) withHighlight++;
        }
        return $"{highlightSequences.Count} sequence(s) | {withMessage} with messages | {withHighlight} with highlights";
    }
    
    private void DrawSequenceCard(int index, HighlightSequence seq)
    {
        bool isExpanded = _selectedSequenceIndex == index;
        
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // === HEADER ROW ===
        EditorGUILayout.BeginHorizontal();
        
        // Step number (colored)
        var stepStyle = new GUIStyle(EditorStyles.boldLabel);
        stepStyle.normal.textColor = new Color(0.4f, 0.7f, 1f);
        EditorGUILayout.LabelField($"Step {seq.DisplayMoveStep}", stepStyle, GUILayout.Width(55));
        
        // Pause badge
        if (seq.pauseGame)
        {
            var badgeStyle = new GUIStyle(EditorStyles.miniLabel);
            badgeStyle.normal.textColor = new Color(1f, 0.7f, 0.2f);
            badgeStyle.fontStyle = FontStyle.Bold;
            EditorGUILayout.LabelField("[PAUSE]", badgeStyle, GUILayout.Width(50));
        }
        else
        {
            GUILayout.Space(54);
        }
        
        // Message preview (flexible width)
        string msgPreview = string.IsNullOrEmpty(seq.messageText) 
            ? "(no message)" 
            : TruncateString(seq.messageText, 40);
        var msgStyle = new GUIStyle(EditorStyles.label);
        msgStyle.normal.textColor = string.IsNullOrEmpty(seq.messageText) 
            ? new Color(0.5f, 0.5f, 0.5f) 
            : new Color(0.9f, 0.9f, 0.9f);
        EditorGUILayout.LabelField(msgPreview, msgStyle);
        
        GUILayout.FlexibleSpace();
        
        // Target badge
        string targetText = GetTargetBadge(seq);
        var targetStyle = new GUIStyle(EditorStyles.miniLabel);
        targetStyle.normal.textColor = seq.targetType != HighlightTargetType.None 
            ? new Color(0.4f, 1f, 0.4f) 
            : new Color(0.5f, 0.5f, 0.5f);
        targetStyle.alignment = TextAnchor.MiddleRight;
        EditorGUILayout.LabelField(targetText, targetStyle, GUILayout.Width(90));
        
        // Action buttons with text labels
        GUI.enabled = index > 0;
        if (GUILayout.Button("Up", GUILayout.Width(30), GUILayout.Height(20)))
            MoveSequence(index, index - 1);
        GUI.enabled = index < highlightSequences.Count - 1;
        if (GUILayout.Button("Dn", GUILayout.Width(30), GUILayout.Height(20)))
            MoveSequence(index, index + 1);
        GUI.enabled = true;
        
        string editLabel = isExpanded ? "Hide" : "Edit";
        if (GUILayout.Button(editLabel, GUILayout.Width(40), GUILayout.Height(20)))
            _selectedSequenceIndex = isExpanded ? -1 : index;
        
        if (GUILayout.Button("X", GUILayout.Width(22), GUILayout.Height(20)))
        {
            if (EditorUtility.DisplayDialog("Delete Sequence", 
                $"Delete sequence at step {seq.DisplayMoveStep}?", "Delete", "Cancel"))
            {
                Undo.RecordObject(this, "Delete Sequence");
                highlightSequences.RemoveAt(index);
                if (_selectedSequenceIndex >= highlightSequences.Count) 
                    _selectedSequenceIndex = -1;
                EditorUtility.SetDirty(this);
                GUIUtility.ExitGUI();
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        // === EXPANDED DETAILS ===
        if (isExpanded)
        {
            EditorGUILayout.Space(6);
            DrawSequenceDetails(seq);
        }
        
        EditorGUILayout.EndVertical();
    }
    
    private string TruncateString(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return "";
        // Replace newlines with spaces for preview
        text = text.Replace("\n", " ").Replace("\r", "");
        return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
    }
    
    private string GetTargetBadge(HighlightSequence seq)
    {
        return seq.targetType switch
        {
            HighlightTargetType.None => "-",
            HighlightTargetType.Tile => $"Tile({seq.targetPosition.x},{seq.targetPosition.y})",
            HighlightTargetType.Cube => $"{seq.targetCubeType.ToString().Substring(0,1)}({seq.targetPosition.x},{seq.targetPosition.y})",
            _ => "?"
        };
    }
    
    private void DrawSequenceDetails(HighlightSequence seq)
    {
        Undo.RecordObject(this, "Edit Sequence");
        
        // Row 1: Step & Flow Control
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Step:", GUILayout.Width(35));
        seq.DisplayMoveStep = EditorGUILayout.IntField(seq.DisplayMoveStep, GUILayout.Width(40));
        GUILayout.Space(20);
        seq.pauseGame = EditorGUILayout.Toggle(seq.pauseGame, GUILayout.Width(16));
        EditorGUILayout.LabelField("Pause Game", GUILayout.Width(80));
        seq.resumeGame = EditorGUILayout.Toggle(seq.resumeGame, GUILayout.Width(16));
        EditorGUILayout.LabelField("Auto Resume", GUILayout.Width(80));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(4);
        
        // Row 2: Message
        EditorGUILayout.LabelField("Message:", EditorStyles.boldLabel);
        seq.messageText = EditorGUILayout.TextArea(seq.messageText, GUILayout.Height(50));
        
        // Row 3: Message options (only if message exists)
        if (!string.IsNullOrEmpty(seq.messageText))
        {
            EditorGUILayout.BeginHorizontal();
            seq.messageRequirePause = EditorGUILayout.Toggle(seq.messageRequirePause, GUILayout.Width(16));
            EditorGUILayout.LabelField("Require K to continue", GUILayout.Width(130));
            GUILayout.Space(20);
            EditorGUILayout.LabelField("Auto-hide delay:", GUILayout.Width(95));
            seq.messageAutoHideDelay = EditorGUILayout.FloatField(seq.messageAutoHideDelay, GUILayout.Width(40));
            EditorGUILayout.LabelField("sec (0=manual)", GUILayout.Width(85));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space(6);
        
        // Row 4: Highlight Target
        EditorGUILayout.LabelField("Highlight Target:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Type:", GUILayout.Width(35));
        seq.targetType = (HighlightTargetType)EditorGUILayout.EnumPopup(seq.targetType, GUILayout.Width(70));
        
        if (seq.targetType != HighlightTargetType.None)
        {
            GUILayout.Space(15);
            EditorGUILayout.LabelField("Position:", GUILayout.Width(55));
            seq.targetPosition = EditorGUILayout.Vector2IntField("", seq.targetPosition, GUILayout.Width(100));
            
            if (seq.targetType == HighlightTargetType.Cube)
            {
                GUILayout.Space(10);
                EditorGUILayout.LabelField("Cube:", GUILayout.Width(35));
                seq.targetCubeType = (CubeType)EditorGUILayout.EnumPopup(seq.targetCubeType, GUILayout.Width(80));
            }
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        
        // Row 5: Highlight Settings (only if target set)
        if (seq.targetType != HighlightTargetType.None)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Color:", GUILayout.Width(40));
            seq.highlightColor = EditorGUILayout.ColorField(seq.highlightColor, GUILayout.Width(50));
            GUILayout.Space(15);
            seq.shouldPulse = EditorGUILayout.Toggle(seq.shouldPulse, GUILayout.Width(16));
            EditorGUILayout.LabelField("Pulse Effect", GUILayout.Width(75));
            GUILayout.Space(15);
            EditorGUILayout.LabelField("Duration:", GUILayout.Width(55));
            seq.highlightDuration = EditorGUILayout.IntField(seq.highlightDuration, GUILayout.Width(35));
            EditorGUILayout.LabelField("steps (0=auto)", GUILayout.Width(85));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            // Row 6: Validation options
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            seq.requireMarkerPlacementValidation = EditorGUILayout.Toggle(seq.requireMarkerPlacementValidation, GUILayout.Width(16));
            EditorGUILayout.LabelField("Require marker at position", GUILayout.Width(155));
            GUILayout.Space(15);
            seq.clearOnCapture = EditorGUILayout.Toggle(seq.clearOnCapture, GUILayout.Width(16));
            EditorGUILayout.LabelField("Clear on capture", GUILayout.Width(105));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Space(4);
        
        if (GUI.changed)
            EditorUtility.SetDirty(this);
    }
    
    private enum SequencePreset { MessageOnly, HighlightTile, HighlightCube, FullTutorial }
    
    private void AddSequence(SequencePreset preset)
    {
        Undo.RecordObject(this, "Add Sequence");
        
        var seq = new HighlightSequence();
        seq.DisplayMoveStep = highlightSequences.Count > 0 
            ? highlightSequences[highlightSequences.Count - 1].DisplayMoveStep + 1 
            : 0;
        
        switch (preset)
        {
            case SequencePreset.MessageOnly:
                seq.pauseGame = true;
                seq.messageText = "Enter message here...";
                seq.messageRequirePause = true;
                seq.targetType = HighlightTargetType.None;
                seq.resumeGame = true;
                break;
                
            case SequencePreset.HighlightTile:
                seq.pauseGame = true;
                seq.messageText = "Place a marker on the highlighted tile.";
                seq.targetType = HighlightTargetType.Tile;
                seq.targetPosition = new Vector2Int(2, 0);
                seq.requireMarkerPlacementValidation = true;
                seq.resumeGame = true;
                break;
                
            case SequencePreset.HighlightCube:
                seq.pauseGame = true;
                seq.messageText = "Target the highlighted cube!";
                seq.targetType = HighlightTargetType.Cube;
                seq.targetCubeType = CubeType.Unit;
                seq.targetPosition = new Vector2Int(2, 2);
                seq.clearOnCapture = true;
                seq.resumeGame = true;
                break;
                
            case SequencePreset.FullTutorial:
                seq.pauseGame = true;
                seq.messageText = "Tutorial step - edit this message.";
                seq.messageRequirePause = true;
                seq.targetType = HighlightTargetType.Tile;
                seq.targetPosition = new Vector2Int(2, 0);
                seq.shouldPulse = true;
                seq.requireMarkerPlacementValidation = true;
                seq.resumeGame = true;
                break;
        }
        
        highlightSequences.Add(seq);
        _selectedSequenceIndex = highlightSequences.Count - 1;
        EditorUtility.SetDirty(this);
    }
    
    private void MoveSequence(int fromIndex, int toIndex)
    {
        Undo.RecordObject(this, "Reorder Sequence");
        var item = highlightSequences[fromIndex];
        highlightSequences.RemoveAt(fromIndex);
        highlightSequences.Insert(toIndex, item);
        _selectedSequenceIndex = toIndex;
        EditorUtility.SetDirty(this);
    }
    #endif
    
    [TabGroup("Main", "Sequences")]
    [Title("Raw Sequence Data")]
    [FoldoutGroup("Main/Sequences/Advanced - Raw Data")]
    [ListDrawerSettings(ShowIndexLabels = true, NumberOfItemsPerPage = 5)]
    [Tooltip("Guided sequences: pause → message → highlight → resume. Executed at DisplayMoveStep timing.")]
    public List<HighlightSequence> highlightSequences = new List<HighlightSequence>();
    
    #endregion
    
    #region Test Configuration
    
    [TabGroup("Main", "Testing")]
    [Title("Automated Testing")]
    [InfoBox("Enable to run this wave as an automated test with scripted commands and assertions.", InfoMessageType.Info)]
    [LabelText("Enable Test Mode")]
    [ToggleLeft]
    [PropertyOrder(-1)]
    public bool isTestWave = false;
    
    [TabGroup("Main", "Testing")]
    [ShowIf("isTestWave")]
    [Title("Test Commands")]
    [InfoBox("Commands execute at specified wave steps", InfoMessageType.None)]
    [TableList(ShowIndexLabels = true, AlwaysExpanded = false, MinScrollViewHeight = 80, MaxScrollViewHeight = 200)]
    public List<TestCommand> testCommands = new List<TestCommand>();
    
    [TabGroup("Main", "Testing")]
    [ShowIf("isTestWave")]
    [Title("Assertions")]
    [InfoBox("Assertions validate test results", InfoMessageType.None)]
    [TableList(ShowIndexLabels = true, AlwaysExpanded = false, MinScrollViewHeight = 60, MaxScrollViewHeight = 150)]
    public List<TestAssertion> testAssertions = new List<TestAssertion>();
    
    [TabGroup("Main", "Testing")]
    [ShowIf("isTestWave")]
    [LabelText("Max Test Steps")]
    [LabelWidth(100)]
    [Tooltip("Auto-complete after this many steps (0 = no limit)")]
    public int maxTestSteps = 0;
    
    #endregion

    #region Runtime Statistics (Not Serialized)
    
    [FoldoutGroup("Runtime Statistics", expanded: false)]
    [ReadOnly]
    [SerializeField] private WaveStatistics _runtimeStats = new WaveStatistics();
    public WaveStatistics RuntimeStats => _runtimeStats;
    
    /// <summary>
    /// Reset runtime statistics for a new playthrough.
    /// </summary>
    [FoldoutGroup("Runtime Statistics")]
    [Button("Reset Statistics")]
    public void ResetRuntimeStats()
    {
        _runtimeStats = new WaveStatistics();
        Debug.Log($"[WaveData] {name}: Runtime stats reset");
    }
    
    #endregion
    
    #region Validation
    
    [FoldoutGroup("Validation", expanded: false)]
    [Button("Validate Wave", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f)]
    [PropertyOrder(100)]
    public void ValidateAndLog()
    {
        var issues = Validate();
        if (issues.Count == 0)
        {
            Debug.Log($"[WaveData] {name}: Validation PASSED ✓");
        }
        else
        {
            Debug.LogWarning($"[WaveData] {name}: {issues.Count} validation issue(s):");
            foreach (var issue in issues)
            {
                Debug.LogWarning($"  • {issue}");
            }
        }
    }
    
    [FoldoutGroup("Validation")]
    [Button("Remove Duplicate Cubes"), GUIColor(1f, 0.8f, 0.4f)]
    public void RemoveDuplicatesManual()
    {
        RemoveDuplicateCubes();
    }
    
    /// <summary>
    /// Called by Unity when asset is modified in editor. Enforces data integrity.
    /// </summary>
    private void OnValidate()
    {
        RemoveDuplicateCubes();
    }
    
    /// <summary>
    /// Removes duplicate cubes at the same position, keeping only the first one found.
    /// Called automatically on save via OnValidate().
    /// </summary>
    private void RemoveDuplicateCubes()
    {
        if (CubesData == null) return;
        
        HashSet<Vector2Int> seenPositions = new HashSet<Vector2Int>();
        List<CubeData> duplicates = new List<CubeData>();
        
        foreach (var cube in CubesData)
        {
            if (seenPositions.Contains(cube.position))
            {
                duplicates.Add(cube);
            }
            else
            {
                seenPositions.Add(cube.position);
            }
        }
        
        if (duplicates.Count > 0)
        {
            Debug.LogWarning($"[WaveData] {name}: Removing {duplicates.Count} duplicate cube(s) at same positions");
            foreach (var dupe in duplicates)
            {
                CubesData.Remove(dupe);
            }
        }
    }
    
    /// <summary>
    /// Validates wave data and returns list of issues found.
    /// </summary>
    public List<string> Validate(int stageGridWidth = 6)
    {
        var issues = new List<string>();
        
        if (spawnWidth < 1)
            issues.Add("Spawn width must be at least 1");
            
        if (spawnWidth > stageGridWidth)
            issues.Add($"Spawn width ({spawnWidth}) exceeds stage grid width ({stageGridWidth})");
            
        if (cubes == null || cubes.Count == 0)
            issues.Add("No cubes defined in wave");
            
        if (moveInterval <= 0)
            issues.Add("Move interval must be positive");
            
        if (fastMoveInterval >= moveInterval)
            issues.Add("Fast move interval should be less than normal move interval");
        
        // Validate cube positions and check for duplicates
        if (cubes != null)
        {
            HashSet<Vector2Int> seenPositions = new HashSet<Vector2Int>();
            
            for (int i = 0; i < cubes.Count; i++)
            {
                var cube = cubes[i];
                if (cube.position.x < 0 || cube.position.x >= spawnWidth)
                    issues.Add($"Cube {i} X position ({cube.position.x}) out of spawn bounds (0-{spawnWidth - 1})");
                if (cube.position.y < 0 || cube.position.y >= spawnHeight)
                    issues.Add($"Cube {i} Y position ({cube.position.y}) out of spawn bounds (0-{spawnHeight - 1})");
                
                // Check for duplicate positions
                if (seenPositions.Contains(cube.position))
                    issues.Add($"Cube {i} at ({cube.position.x}, {cube.position.y}) is a duplicate position - multiple cubes at same position not allowed");
                else
                    seenPositions.Add(cube.position);
            }
        }
        
        return issues;
    }
    
    #endregion
}

/// <summary>
/// Runtime statistics for a single wave playthrough.
/// </summary>
[System.Serializable]
public class WaveStatistics
{
    [FoldoutGroup("Captures by Type")]
    [HorizontalGroup("Captures by Type/Row", LabelWidth = 55)]
    [LabelText("Unit")]
    public int unitCubesCaptured;
    
    [HorizontalGroup("Captures by Type/Row")]
    [LabelText("Matrix")]
    public int matrixCubesCaptured;
    
    [HorizontalGroup("Captures by Type/Row")]
    [LabelText("Recur")]
    public int recursionCubesCaptured;
    
    [HorizontalGroup("Captures by Type/Row")]
    [LabelText("Infin")]
    public int infinityCubesCaptured;
    
    [FoldoutGroup("Results")]
    [HorizontalGroup("Results/Row", LabelWidth = 70)]
    [LabelText("Escaped")]
    public int cubesEscaped;
    
    [HorizontalGroup("Results/Row")]
    [LabelText("Placed")]
    public int markersPlaced;
    
    [HorizontalGroup("Results/Row")]
    [LabelText("Triggered")]
    public int markersTriggererd;
    
    [FoldoutGroup("Results")]
    [LabelText("Completion Time (sec)")]
    [LabelWidth(140)]
    public float completionTime;
}

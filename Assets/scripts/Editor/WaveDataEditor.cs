using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using static Enumerations;

/// <summary>
/// Custom editor for WaveData that displays cubes in a visual grid layout.
/// Makes it much easier to visualize and edit wave configurations.
/// </summary>
[CustomEditor(typeof(WaveData))]
public class WaveDataEditor : Editor
{
    private WaveData waveData;
    private CubeType selectedCubeType = CubeType.Unit;
    private bool showGridVisualization = true;
    private bool showListFallback = false;
    
    // Track previous dimensions to detect changes
    private int previousGridWidth = -1;
    private int previousGridHeight = -1;
    
    // Grid cell size
    private const float CELL_SIZE = 25f;
    private const float CELL_SPACING = 2f;
    
    private void OnEnable()
    {
        waveData = (WaveData)target;
        previousGridWidth = waveData.GridWidth;
        previousGridHeight = waveData.GridHeight;
    }
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        // Check for dimension changes before drawing
        int currentWidth = waveData.GridWidth;
        int currentHeight = waveData.GridHeight;
        
        // Handle grid dimension changes
        if (previousGridHeight != -1 && currentHeight != previousGridHeight)
        {
            HandleGridHeightChange(previousGridHeight, currentHeight);
        }
        
        if (previousGridWidth != -1 && currentWidth != previousGridWidth)
        {
            HandleGridWidthChange(previousGridWidth, currentWidth);
        }
        
        previousGridWidth = currentWidth;
        previousGridHeight = currentHeight;
        
        // Draw default inspector for non-cube fields
        DrawDefaultInspectorWithoutCubes();
        
        EditorGUILayout.Space(10);
        
        // Grid visualization toggle
        showGridVisualization = EditorGUILayout.Foldout(showGridVisualization, "Grid Visualization", true);
        
        if (showGridVisualization)
        {
            DrawGridVisualization();
        }
        
        // List fallback toggle
        EditorGUILayout.Space(5);
        showListFallback = EditorGUILayout.Foldout(showListFallback, "List View (Fallback)", false);
        
        if (showListFallback)
        {
            DrawListFallback();
        }
        
        serializedObject.ApplyModifiedProperties();
        
        // Force repaint if list was modified to update grid visualization
        if (GUI.changed)
        {
            Repaint();
        }
    }
    
    private void DrawDefaultInspectorWithoutCubes()
    {
        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;
        
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            
            // Skip the CubesData list - we'll draw it in grid view
            if (iterator.propertyPath == "CubesData")
            {
                continue;
            }
            
            EditorGUILayout.PropertyField(iterator, true);
        }
    }
    
    private void DrawGridVisualization()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // Grid dimensions info
        EditorGUILayout.LabelField($"Grid Size: {waveData.GridWidth} × {waveData.GridHeight}", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Cubes: {waveData.CubesData.Count}", EditorStyles.miniLabel);
        
        EditorGUILayout.Space(5);
        
        // Cube type selector
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Brush:", GUILayout.Width(50));
        selectedCubeType = (CubeType)EditorGUILayout.EnumPopup(selectedCubeType);
        
        if (GUILayout.Button("Clear All", GUILayout.Width(70)))
        {
            if (EditorUtility.DisplayDialog("Clear All Cubes", 
                "Are you sure you want to remove all cubes from this wave?", 
                "Clear", "Cancel"))
            {
                waveData.CubesData.Clear();
                EditorUtility.SetDirty(waveData);
            }
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Draw grid
        DrawGrid();
        
        EditorGUILayout.Space(5);
        
        // Quick actions
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Fill Row (Unit)"))
        {
            FillRow(waveData.GridHeight - 1, CubeType.Unit);
        }
        if (GUILayout.Button("Fill Row (Infinity)"))
        {
            FillRow(waveData.GridHeight - 1, CubeType.Infinity);
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.EndVertical();
    }
    
    private void DrawGrid()
    {
        // Get current grid dimensions (ensure they're valid)
        int gridWidth = Mathf.Max(1, waveData.GridWidth);
        int gridHeight = Mathf.Max(1, waveData.GridHeight);
        
        // Create a 2D array to track what's at each position
        CubeData[,] grid = new CubeData[gridWidth, gridHeight];
        
        // Populate grid from cube data
        foreach (var cube in waveData.CubesData)
        {
            if (cube.position.x >= 0 && cube.position.x < gridWidth &&
                cube.position.y >= 0 && cube.position.y < gridHeight)
            {
                grid[cube.position.x, cube.position.y] = cube;
            }
        }
        
        // Draw column headers (X coordinates)
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(20); // Space for row headers
        for (int x = 0; x < gridWidth; x++)
        {
            EditorGUILayout.LabelField(x.ToString(), EditorStyles.centeredGreyMiniLabel, 
                GUILayout.Width(CELL_SIZE), GUILayout.Height(15));
        }
        EditorGUILayout.EndHorizontal();
        
        // Draw grid rows (Y coordinates, top to bottom)
        // Note: Y=0 is bottom row, Y=GridHeight-1 is top row
        for (int y = gridHeight - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Row header (Y coordinate)
            EditorGUILayout.LabelField(y.ToString(), EditorStyles.centeredGreyMiniLabel, 
                GUILayout.Width(20), GUILayout.Height(CELL_SIZE));
            
            // Draw cells in this row
            for (int x = 0; x < gridWidth; x++)
            {
                CubeData cubeAtPos = grid[x, y];
                DrawGridCell(x, y, cubeAtPos);
            }
            
            EditorGUILayout.EndHorizontal();
        }
    }
    
    private void DrawGridCell(int x, int y, CubeData existingCube)
    {
        Vector2Int pos = new Vector2Int(x, y);
        Rect cellRect = GUILayoutUtility.GetRect(CELL_SIZE, CELL_SIZE, GUILayout.Width(CELL_SIZE), GUILayout.Height(CELL_SIZE));
        
        // Draw background
        Color bgColor = existingCube != null ? GetCubeColor(existingCube.type) : Color.gray;
        EditorGUI.DrawRect(cellRect, bgColor * 0.3f);
        
        // Draw border
        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 1), Color.black);
        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 1, cellRect.height), Color.black);
        EditorGUI.DrawRect(new Rect(cellRect.x + cellRect.width - 1, cellRect.y, 1, cellRect.height), Color.black);
        EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y + cellRect.height - 1, cellRect.width, 1), Color.black);
        
        // Draw cube type label
        string label = existingCube != null ? GetCubeTypeLabel(existingCube.type) : "";
        GUIStyle labelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        labelStyle.normal.textColor = existingCube != null ? GetCubeColor(existingCube.type) : Color.white;
        labelStyle.fontStyle = FontStyle.Bold;
        GUI.Label(cellRect, label, labelStyle);
        
        // Handle clicks
        Event e = Event.current;
        if (e.type == EventType.MouseDown && cellRect.Contains(e.mousePosition))
        {
            if (e.button == 0) // Left click
            {
                if (existingCube != null)
                {
                    // Remove cube
                    waveData.CubesData.Remove(existingCube);
                }
                else
                {
                    // Add cube
                    waveData.CubesData.Add(new CubeData(selectedCubeType, pos));
                }
                EditorUtility.SetDirty(waveData);
                e.Use();
            }
            else if (e.button == 1 && existingCube != null) // Right click - change type
            {
                // Cycle through cube types
                int currentType = (int)existingCube.type;
                int nextType = (currentType + 1) % 4; // 4 cube types
                existingCube.type = (CubeType)nextType;
                EditorUtility.SetDirty(waveData);
                e.Use();
            }
        }
    }
    
    private Color GetCubeColor(CubeType type)
    {
        switch (type)
        {
            case CubeType.Unit:
                return Color.green;
            case CubeType.Matrix:
                return Color.blue;
            case CubeType.Recursion:
                return Color.yellow;
            case CubeType.Infinity:
                return Color.red;
            default:
                return Color.white;
        }
    }
    
    private string GetCubeTypeLabel(CubeType type)
    {
        switch (type)
        {
            case CubeType.Unit:
                return "U";
            case CubeType.Matrix:
                return "M";
            case CubeType.Recursion:
                return "R";
            case CubeType.Infinity:
                return "∞";
            default:
                return "?";
        }
    }
    
    private void FillRow(int y, CubeType type)
    {
        int gridWidth = Mathf.Max(1, waveData.GridWidth);
        
        // Remove existing cubes in this row
        waveData.CubesData.RemoveAll(cube => cube.position.y == y);
        
        // Add cubes for entire row
        for (int x = 0; x < gridWidth; x++)
        {
            waveData.CubesData.Add(new CubeData(type, new Vector2Int(x, y)));
        }
        
        EditorUtility.SetDirty(waveData);
    }
    
    private void HandleGridHeightChange(int oldHeight, int newHeight)
    {
        if (newHeight > oldHeight)
        {
            // Grid expanded - shift all cubes up to maintain top row position
            int heightDiff = newHeight - oldHeight;
            
            // Remove cubes that are now out of bounds (shouldn't happen, but safety check)
            waveData.CubesData.RemoveAll(cube => cube.position.y >= newHeight);
            
            // Shift all existing cubes up by the height difference
            foreach (var cube in waveData.CubesData)
            {
                cube.position = new Vector2Int(cube.position.x, cube.position.y + heightDiff);
            }
            
            EditorUtility.SetDirty(waveData);
        }
        else if (newHeight < oldHeight)
        {
            // Grid shrunk - remove cubes that are now out of bounds
            waveData.CubesData.RemoveAll(cube => cube.position.y >= newHeight);
            
            // Shift remaining cubes down to fill from bottom
            int heightDiff = oldHeight - newHeight;
            foreach (var cube in waveData.CubesData)
            {
                cube.position = new Vector2Int(cube.position.x, Mathf.Max(0, cube.position.y - heightDiff));
            }
            
            EditorUtility.SetDirty(waveData);
        }
    }
    
    private void HandleGridWidthChange(int oldWidth, int newWidth)
    {
        if (newWidth < oldWidth)
        {
            // Grid width shrunk - remove cubes that are now out of bounds
            waveData.CubesData.RemoveAll(cube => cube.position.x >= newWidth);
            EditorUtility.SetDirty(waveData);
        }
        // If width increases, no need to shift - cubes stay in their X positions
    }
    
    private void DrawListFallback()
    {
        SerializedProperty cubesProperty = serializedObject.FindProperty("CubesData");
        if (cubesProperty != null)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(cubesProperty, true);
            if (EditorGUI.EndChangeCheck())
            {
                // List was modified - force repaint to update grid visualization
                Repaint();
            }
        }
    }
}


using static Enumerations;
using System.Collections.Generic;
using UnityEngine;

public class ContentDebugPanel : DebugPanelBase
{
    public override string PanelName => "Content";
    public override DebugPanelGroup Group => DebugPanelGroup.Content;

    private GridManager gridManager;
    private PlayerManager playerManager;
    private WaveManager waveManager;

    // UI State
    private bool showTileEditor = true;
    private bool showCubeSpawner = true;
    private bool showContentInfo = false;

    // Tile Editor
    private int selectedTileState = 0;
    private int enhancedTileCharges = 3;

    // Cube Spawner
    private int cubeSpawnRow = 15;
    private int selectedCubeType = 1;

    // Inspector
    private Vector2Int inspectorPosition = new Vector2Int(0, 0);
    private bool autoTrackPlayer = true;

    public override void Initialize()
    {
        gridManager = GridManager.Instance ?? Object.FindObjectOfType<GridManager>();
        playerManager = Object.FindObjectOfType<PlayerManager>();
        waveManager = Object.FindObjectOfType<WaveManager>();

        if (gridManager != null)
        {
            cubeSpawnRow = Mathf.Max(10, gridManager.Height - 5);
        }

        if (playerManager != null)
        {
            inspectorPosition = playerManager.currentTilePosition;
        }
    }

    public override void Update()
    {
        if (autoTrackPlayer && playerManager != null)
        {
            inspectorPosition = playerManager.currentTilePosition;
        }
    }

    public override void DrawPanel()
    {
        DrawSectionToggles();
        GUILayout.Space(5);

        if (showContentInfo) DrawContentInfoSection();
        if (showTileEditor) DrawTileEditorSection();
        if (showCubeSpawner) DrawCubeSpawnerSection();
    }

    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        showContentInfo = DrawToggleButton("Info", showContentInfo);
        showTileEditor = DrawToggleButton("Tiles", showTileEditor);
        showCubeSpawner = DrawToggleButton("Cubes", showCubeSpawner);
        GUILayout.EndHorizontal();
    }

    private bool DrawToggleButton(string label, bool current)
    {
        GUI.backgroundColor = current ? Color.cyan : Color.white;
        bool result = GUILayout.Button(label, GUILayout.Height(25));
        GUI.backgroundColor = Color.white;
        return result ? !current : current;
    }

    private void DrawContentInfoSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CONTENT INFO", GUI.skin.box);

        // Inspector controls
        GUILayout.BeginHorizontal();
        autoTrackPlayer = GUILayout.Toggle(autoTrackPlayer, "Track Player");
        if (!autoTrackPlayer)
        {
            GUILayout.Label("X:", GUILayout.Width(20));
            string xStr = GUILayout.TextField(inspectorPosition.x.ToString(), GUILayout.Width(40));
            if (int.TryParse(xStr, out int newX))
                inspectorPosition.x = Mathf.Clamp(newX, 0, gridManager?.Width - 1 ?? 10);

            GUILayout.Label("Y:", GUILayout.Width(20));
            string yStr = GUILayout.TextField(inspectorPosition.y.ToString(), GUILayout.Width(40));
            if (int.TryParse(yStr, out int newY))
                inspectorPosition.y = Mathf.Clamp(newY, 0, gridManager?.Height - 1 ?? 20);
        }
        GUILayout.EndHorizontal();

        // Tile info
        if (gridManager != null && gridManager.IsValidGridPosition(inspectorPosition))
        {
            Tile tile = gridManager.GetTileAt(inspectorPosition);
            if (tile != null)
            {
                GUILayout.Label($"Tile ({inspectorPosition.x}, {inspectorPosition.y}):");
                GUILayout.Label($"State: {GetTileStateDescription(tile)}");
                GUILayout.Label($"Marker: {tile.HasMarker} | Charges: {tile.DetonationCharges}");
                GUILayout.Label($"Playable: {tile.IsPlayable} | Face Paint: {tile.CanPaintCubes}");
            }
        }

        // Cubes at position
        var cubes = FindCubesAt(inspectorPosition);
        GUILayout.Label($"Cubes at position: {cubes.Count}");
        foreach (var cube in cubes)
        {
            if (cube != null)
            {
                GUILayout.Label($"  {cube.type} | Face: {cube.GetCurrentDownFace()}");
                GUILayout.Label($"  Effective: {cube.GetEffectiveType()} | Can Capture: {cube.CanBeCaptured()}");
            }
        }

        GUILayout.EndVertical();
    }

    private void DrawTileEditorSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("TILE EDITOR", GUI.skin.box);

        // Tile state selector
        GUILayout.BeginHorizontal();
        if (DrawStateButton("Normal", 0, Color.white)) selectedTileState = 0;
        if (DrawStateButton("Primed", 1, Color.blue)) selectedTileState = 1;
        if (DrawStateButton("Corrupt", 2, Color.red)) selectedTileState = 2;
        if (DrawStateButton("Enhanced", 3, Color.yellow)) selectedTileState = 3;
        GUILayout.EndHorizontal();

        if (selectedTileState == 3)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Charges:", GUILayout.Width(60));
            string chargesStr = GUILayout.TextField(enhancedTileCharges.ToString(), GUILayout.Width(40));
            if (int.TryParse(chargesStr, out int newCharges))
                enhancedTileCharges = Mathf.Clamp(newCharges, 1, 5);
            GUILayout.EndHorizontal();
        }

        // Quick actions
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Set Inspector Tile")) SetTileState(inspectorPosition, selectedTileState);
        if (GUILayout.Button("Set Player Tile") && playerManager != null)
            SetTileState(playerManager.currentTilePosition, selectedTileState);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Reset All Tiles")) ResetAllTiles();
        if (GUILayout.Button("Test Pattern")) CreateTestTilePattern();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawCubeSpawnerSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("CUBE SPAWNER", GUI.skin.box);

        // Spawn row control
        GUILayout.BeginHorizontal();
        GUILayout.Label("Row:", GUILayout.Width(40));
        string rowStr = GUILayout.TextField(cubeSpawnRow.ToString(), GUILayout.Width(50));
        if (int.TryParse(rowStr, out int newRow))
            cubeSpawnRow = Mathf.Clamp(newRow, 0, gridManager?.Height - 1 ?? 20);
        if (GUILayout.Button("Top")) cubeSpawnRow = gridManager?.Height - 1 ?? 20;
        GUILayout.EndHorizontal();

        // Cube type selector
        GUILayout.BeginHorizontal();
        if (DrawCubeButton("Normal", 1, Color.white)) selectedCubeType = 1;
        if (DrawCubeButton("Blue", 2, Color.blue)) selectedCubeType = 2;
        if (DrawCubeButton("Black", 3, Color.black)) selectedCubeType = 3;
        if (DrawCubeButton("Reinforced", 4, Color.magenta)) selectedCubeType = 4;
        GUILayout.EndHorizontal();

        // Spawn actions
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Spawn at Inspector"))
            SpawnCubeAt(inspectorPosition, (CubeType)(selectedCubeType - 1));
        if (GUILayout.Button("Spawn Row"))
            SpawnCubeRow((CubeType)(selectedCubeType - 1));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Inspector")) RemoveCubeAt(inspectorPosition);
        if (GUILayout.Button("Clear All")) ClearAllCubes();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    // Helper methods
    private bool DrawStateButton(string label, int state, Color color)
    {
        GUI.backgroundColor = selectedTileState == state ? color : Color.white;
        bool result = GUILayout.Button(label);
        GUI.backgroundColor = Color.white;
        return result;
    }

    private bool DrawCubeButton(string label, int type, Color color)
    {
        GUI.backgroundColor = selectedCubeType == type ? color : Color.white;
        bool result = GUILayout.Button(label);
        GUI.backgroundColor = Color.white;
        return result;
    }

    private void SetTileState(Vector2Int position, int state)
    {
        if (!gridManager.IsValidGridPosition(position)) return;

        Tile tile = gridManager.GetTileAt(position);
        if (tile == null) return;

        switch (state)
        {
            case 0: tile.ResetTile(); break;
            case 1: tile.PrimeTile(); break;
            case 2: tile.BlackenTile(); break;
            case 3: tile.AdvantageTile(enhancedTileCharges); break;
        }
    }

    private void ResetAllTiles()
    {
        if (gridManager == null) return;
        for (int x = 0; x < gridManager.Width; x++)
        {
            for (int y = 0; y < gridManager.Height; y++)
            {
                Tile tile = gridManager.GetTileAt(x, y);
                tile?.ResetTile();
            }
        }
    }

    private void CreateTestTilePattern()
    {
        if (gridManager == null || playerManager == null) return;
        Vector2Int center = playerManager.currentTilePosition;
        SetTileState(new Vector2Int(center.x - 1, center.y + 1), 1);
        SetTileState(new Vector2Int(center.x + 1, center.y + 1), 1);
        SetTileState(new Vector2Int(center.x, center.y + 2), 3);
    }

    private void SpawnCubeAt(Vector2Int position, CubeType cubeType)
    {
        if (waveManager?.cubePrefabs == null || (int)cubeType >= waveManager.cubePrefabs.Length) return;

        RemoveCubeAt(position);
        Vector3 worldPos = gridManager.GridToWorldPosition(position.x, position.y, 2f);
        GameObject cubeObj = Object.Instantiate(waveManager.cubePrefabs[(int)cubeType], worldPos, Quaternion.identity);

        var cube = cubeObj.GetComponent<CubeManager>();
        if (cube == null) cube = cubeObj.AddComponent<CubeManager>();

        var cubeData = new CubeData { type = cubeType, position = position, level = 1 };
        cube.Init(gridManager, cubeData, 2f);
        waveManager?.activeCubes.Add(cube);
    }

    private void SpawnCubeRow(CubeType cubeType)
    {
        if (gridManager == null) return;
        for (int x = 0; x < gridManager.Width; x++)
        {
            SpawnCubeAt(new Vector2Int(x, cubeSpawnRow), cubeType);
        }
    }

    private void RemoveCubeAt(Vector2Int position)
    {
        foreach (CubeManager cube in Object.FindObjectsOfType<CubeManager>())
        {
            if (cube != null && !cube.isDestroyed &&
                cube.position.x == position.x && cube.position.y == position.y)
            {
                Object.Destroy(cube.gameObject);
                break;
            }
        }
    }

    private void ClearAllCubes()
    {
        foreach (CubeManager cube in Object.FindObjectsOfType<CubeManager>())
        {
            if (cube != null) Object.Destroy(cube.gameObject);
        }
    }

    private List<CubeManager> FindCubesAt(Vector2Int position)
    {
        List<CubeManager> cubes = new List<CubeManager>();
        foreach (CubeManager cube in Object.FindObjectsOfType<CubeManager>())
        {
            if (cube != null && !cube.isDestroyed &&
                cube.position.x == position.x && cube.position.y == position.y)
            {
                cubes.Add(cube);
            }
        }
        return cubes;
    }

    private string GetTileStateDescription(Tile tile)
    {
        if (tile.IsBlackened) return "Corrupted";
        if (tile.IsPrimed) return "Primed";
        if (tile.HasCharges) return $"Enhanced ({tile.DetonationCharges})";
        if (tile.HasMarker) return "Has Marker";
        return "Normal";
    }
}

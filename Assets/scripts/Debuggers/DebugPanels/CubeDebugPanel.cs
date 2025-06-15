using static Enumerations;
using System.Collections.Generic;
using UnityEngine;

public class CubeDebugPanel : DebugPanelBase
{
    public override string PanelName => "Cube Faces";
    public override DebugPanelGroup Group => DebugPanelGroup.Cube;

    private GridManager gridManager;
    private PlayerManager playerManager;

    // UI State
    private bool showFacePainter = true;
    private bool showActiveCubes = true;

    // Face painting controls
    private int selectedFaceStatus = 1; // 1=Corrupted, 2=Enhanced
    private int paintDuration = 3;
    private Vector2Int inspectorPosition = new Vector2Int(0, 0);
    private bool autoTrackPlayer = true;

    public override void Initialize()
    {
        gridManager = GridManager.Instance;
        playerManager = Object.FindObjectOfType<PlayerManager>();

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

        if (showFacePainter) DrawFacePainterSection();
        if (showActiveCubes) DrawActiveCubesSection();
    }

    private void DrawSectionToggles()
    {
        GUILayout.BeginHorizontal();
        showFacePainter = DrawToggleButton("Face Painter", showFacePainter);
        showActiveCubes = DrawToggleButton("Active Cubes", showActiveCubes);
        GUILayout.EndHorizontal();
    }
    private bool DrawToggleButton(string label, bool current)
    {
        GUI.backgroundColor = current ? Color.cyan : Color.white;
        bool result = GUILayout.Button(label, GUILayout.Height(25));
        GUI.backgroundColor = Color.white;
        return result ? !current : current;
    }
    private void DrawFacePainterSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("FACE PAINTER", GUI.skin.box);

        // Inspector position
        GUILayout.BeginHorizontal();
        autoTrackPlayer = GUILayout.Toggle(autoTrackPlayer, "Track Player");
        if (!autoTrackPlayer)
        {
            GUILayout.Label("X:", GUILayout.Width(15));
            string xStr = GUILayout.TextField(inspectorPosition.x.ToString(), GUILayout.Width(30));
            if (int.TryParse(xStr, out int newX))
                inspectorPosition.x = Mathf.Clamp(newX, 0, gridManager?.Width - 1 ?? 10);

            GUILayout.Label("Y:", GUILayout.Width(15));
            string yStr = GUILayout.TextField(inspectorPosition.y.ToString(), GUILayout.Width(30));
            if (int.TryParse(yStr, out int newY))
                inspectorPosition.y = Mathf.Clamp(newY, 0, gridManager?.Height - 1 ?? 20);
        }
        GUILayout.EndHorizontal();

        // Face status selector
        GUILayout.BeginHorizontal();
        GUI.backgroundColor = selectedFaceStatus == 1 ? Color.red : Color.white;
        if (GUILayout.Button("Corrupted")) selectedFaceStatus = 1;
        GUI.backgroundColor = selectedFaceStatus == 2 ? Color.blue : Color.white;
        if (GUILayout.Button("Enhanced")) selectedFaceStatus = 2;
        GUI.backgroundColor = Color.white;
        GUILayout.EndHorizontal();

        // Duration control
        GUILayout.BeginHorizontal();
        GUILayout.Label("Duration:", GUILayout.Width(60));
        string durationStr = GUILayout.TextField(paintDuration.ToString(), GUILayout.Width(40));
        if (int.TryParse(durationStr, out int newDuration))
            paintDuration = Mathf.Clamp(newDuration, -1, 10);
        GUILayout.Label("(-1 = permanent)");
        GUILayout.EndHorizontal();

        // Tile painting setup
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Setup Tile Painting"))
        {
            SetupTilePainting(inspectorPosition);
        }
        if (GUILayout.Button("Clear Tile Painting"))
        {
            ClearTilePainting(inspectorPosition);
        }
        GUILayout.EndHorizontal();

        // Cube face painting (direct)
        var cubesAtPosition = FindCubesAt(inspectorPosition);
        if (cubesAtPosition.Count > 0)
        {
            GUILayout.Label($"Cubes at ({inspectorPosition.x}, {inspectorPosition.y}):");
            foreach (var cube in cubesAtPosition)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{cube.type}", GUILayout.Width(80));
                if (GUILayout.Button("Paint Face", GUILayout.Width(70)))
                {
                    PaintCubeFace(cube);
                }
                if (GUILayout.Button("Clear Faces", GUILayout.Width(70)))
                {
                    cube.ClearAllFaces();
                }
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndVertical();
    }

    private void DrawActiveCubesSection()
    {
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label("ACTIVE CUBES", GUI.skin.box);

        var allCubes = Object.FindObjectsOfType<CubeManager>();
        int shownCubes = 0;

        foreach (var cube in allCubes)
        {
            if (cube != null && !cube.isDestroyed && shownCubes < 5) // Show max 5 cubes
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label($"{cube.type} at ({cube.position.x},{cube.position.y}):");

                // Show face status
                CubeFace activeFace = cube.GetCurrentDownFace();
                FaceStatus activeStatus = cube.GetActiveFaceStatus();
                GUILayout.Label($"Down Face: {activeFace} ({activeStatus})");
                GUILayout.Label($"Effective Type: {cube.GetEffectiveType()}");
                GUILayout.Label($"Can Capture: {cube.CanBeCaptured()}");

                // Face control buttons
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Corrupt", GUILayout.Width(60)))
                {
                    cube.SetFaceStatus(activeFace, FaceStatus.Corrupted, paintDuration);
                }
                if (GUILayout.Button("Enhance", GUILayout.Width(60)))
                {
                    cube.SetFaceStatus(activeFace, FaceStatus.Enhanced, paintDuration);
                }
                if (GUILayout.Button("Clear", GUILayout.Width(50)))
                {
                    cube.SetFaceStatus(activeFace, FaceStatus.None, 0);
                }
                GUILayout.EndHorizontal();

                // Debug buttons
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Show All Faces", GUILayout.Width(100)))
                {
                    cube.DebugShowAllFaces();
                }
                if (GUILayout.Button("Print Mapping", GUILayout.Width(100)))
                {
                    cube.DebugPrintFaceMapping();
                }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                shownCubes++;
            }
        }

        if (shownCubes == 0)
        {
            GUILayout.Label("No active cubes found");
        }
        else if (allCubes.Length > 5)
        {
            GUILayout.Label($"Showing 5 of {allCubes.Length} cubes");
        }

        GUILayout.EndVertical();
    }

    // Helper methods
    private void SetupTilePainting(Vector2Int position)
    {
        if (!gridManager.IsValidGridPosition(position)) return;

        Tile tile = gridManager.GetTileAt(position);
        if (tile == null) return;

        FaceStatus status = selectedFaceStatus == 1 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
        Color color = selectedFaceStatus == 1 ? Color.red : Color.blue;

        tile.SetupFacePainting(status, color, paintDuration, true, false);
        Debug.Log($"Setup tile at ({position.x}, {position.y}) to paint cubes with {status} status");
    }

    private void ClearTilePainting(Vector2Int position)
    {
        if (!gridManager.IsValidGridPosition(position)) return;

        Tile tile = gridManager.GetTileAt(position);
        if (tile != null)
        {
            tile.DisableFacePainting();
            Debug.Log($"Cleared face painting from tile at ({position.x}, {position.y})");
        }
    }

    private void PaintCubeFace(CubeManager cube)
    {
        if (cube == null) return;

        FaceStatus status = selectedFaceStatus == 1 ? FaceStatus.Corrupted : FaceStatus.Enhanced;
        Color color = selectedFaceStatus == 1 ? Color.red : Color.blue;

        CubeFace currentFace = cube.GetCurrentDownFace();
        cube.SetFaceStatus(currentFace, status, paintDuration);

        Debug.Log($"Painted {currentFace} face of {cube.type} cube with {status} status");
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
}
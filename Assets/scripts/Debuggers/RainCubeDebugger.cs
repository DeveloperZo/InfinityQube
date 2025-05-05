using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RainCubeDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager grid;
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private WaveDebugger waveDebugger;
    [SerializeField] private GameObject[] cubePrefabs; // Normal, Green, Black, Blue
    [SerializeField] private Material highlightMaterial;

    [SerializeField] private CubeData cubeData;

    [Header("Rain Settings")]
    [SerializeField] private Enumerations.CubeType rainCubeType = Enumerations.CubeType.Normal;
    [SerializeField] private int rainX = 2;
    [SerializeField] private int rainY = 3;
    [SerializeField] private int rainMoveCount = 3;
    [SerializeField] private float moveInterval = 0.5f;

    private GameObject highlightObject;
    private Material originalTileMaterial;
    private bool isRaining = false;

    private void Start()
    {
        if (grid == null) grid = FindObjectOfType<GridManager>();

        if (highlightMaterial == null)
        {
            highlightMaterial = new Material(Shader.Find("Standard"));
            highlightMaterial.color = new Color(0.3f, 0.5f, 1.0f, 0.7f); // Blue highlight
        }

        UpdateTileHighlight();
    }

    private void OnDestroy()
    {
        ClearTileHighlight();
    }

    private void RainCube(int column, int row, Enumerations.CubeType type, int moveCount)
    {
        if (grid == null || column < 0 || column >= grid.Width ||
            row < 0 || row >= grid.Height) return;

        // Find prefab for this type
        int prefabIndex = (int)type;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"Invalid cube prefab for type {type}");
            return;
        }

        // Calculate spawn position - above the grid
        float spawnHeight = 5f;  // Fixed height above grid
        Vector3 spawnPosition = grid.tiles[column, row].transform.position;
        spawnPosition.y += spawnHeight;


        GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPosition, Quaternion.identity);

        if (cube != null)
        {
            CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
            if (behavior == null)
            {
                behavior = cube.AddComponent<CubeBehavior>();
                behavior.type = type;
            }

            // IMPORTANT: Set the grid position in Vector2Int where x = column, y = row
            cubeData.position = new Vector2Int(column, row);
            behavior.Init(grid, cubeData);

            // Mark as a raining cube with move count
            behavior.isRainingCube = true;
            behavior.moveCountRemaining = moveCount;
            behavior.transform.position = new Vector3(behavior.transform.position.x, 5f, behavior.transform.position.z);

            waveDebugger.debugObjects.Add(cube);

            // Register with wave manager but don't add to active cubes yet
            if (waveManager != null)
            {
                waveManager.RegisterRainCube(behavior);
            }

            Debug.Log($"Created rain cube of type {type} at column {column}, row {row} with {moveCount} moves remaining");
        }
    }

    private IEnumerator BounceEffect(CubeBehavior cube, float duration)
    {
        if (cube == null) yield break;

        Vector3 originalScale = cube.transform.localScale;
        Vector3 squashedScale = new Vector3(1.1f, 0.9f, 1.1f);

        // Squash
        float elapsed = 0f;
        float halfDuration = duration / 2f;

        while (elapsed < halfDuration)
        {
            if (cube == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);

            cube.transform.localScale = Vector3.Lerp(originalScale, squashedScale, t);

            yield return null;
        }

        // Return to normal
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            if (cube == null) yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / halfDuration);

            cube.transform.localScale = Vector3.Lerp(squashedScale, originalScale, t);

            yield return null;
        }

        // Ensure final scale
        if (cube != null)
        {
            cube.transform.localScale = originalScale;
        }
    }

    private void UpdateTileHighlight()
    {
        // Clear any existing highlight
        ClearTileHighlight();

        // Validate grid and selected position
        if (grid == null || rainX < 0 || rainX >= grid.Width ||
            rainY < 0 || rainY >= grid.Height) return;

        Tile targetTile = grid.tiles[rainX, rainY];
        if (targetTile == null) return;

        // Store original material
        Renderer tileRenderer = targetTile.GetComponent<Renderer>();
        if (tileRenderer != null)
        {
            originalTileMaterial = tileRenderer.material;

            // Apply highlight material
            tileRenderer.material = highlightMaterial;

            // Create highlight marker
            highlightObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            highlightObject.name = "RainTargetHighlight";

            // Position slightly above the tile with a small vertical offset
            highlightObject.transform.position = new Vector3(
                rainX,
                0.1f, // Slightly raised
                rainY
            );

            // Scale down slightly to make it visually distinct
            highlightObject.transform.localScale = new Vector3(0.9f, 0.1f, 0.9f);

            // Apply the highlight material
            Renderer highlightRenderer = highlightObject.GetComponent<Renderer>();
            if (highlightRenderer != null)
            {
                highlightRenderer.material = highlightMaterial;
            }

            // Remove collider to avoid physics interference
            Collider highlightCollider = highlightObject.GetComponent<Collider>();
            if (highlightCollider != null)
            {
                Destroy(highlightCollider);
            }
        }
    }

    private void ClearTileHighlight()
    {
        // Restore original material if possible
        if (grid != null && originalTileMaterial != null &&
            rainX >= 0 && rainX < grid.Width &&
            rainY >= 0 && rainY < grid.Height)
        {
            Tile targetTile = grid.tiles[rainX, rainY];
            if (targetTile != null)
            {
                Renderer tileRenderer = targetTile.GetComponent<Renderer>();
                if (tileRenderer != null)
                {
                    tileRenderer.material = originalTileMaterial;
                }
            }
        }

        // Destroy highlight object
        if (highlightObject != null)
        {
            Destroy(highlightObject);
            highlightObject = null;
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(Screen.width - 310, Screen.height - 210, 300, 200));

        GUILayout.Label("Rain Cube Debugger", GUI.skin.box);

        GUILayout.Label("Select Cube Type:");
        string[] typeNames = System.Enum.GetNames(typeof(Enumerations.CubeType));
        int selectedIndex = System.Array.IndexOf(typeNames, rainCubeType.ToString());
        if (selectedIndex < 0) selectedIndex = 0;

        rainCubeType = (Enumerations.CubeType)System.Enum.Parse(
            typeof(Enumerations.CubeType),
            typeNames[GUILayout.SelectionGrid(selectedIndex, typeNames, 2)]);

        GUILayout.Label("Select Coordinates:");
        GUILayout.BeginHorizontal();
        GUILayout.Label("X:", GUILayout.Width(20));
        string rainXInput = GUILayout.TextField(rainX.ToString(), GUILayout.Width(50));
        if (int.TryParse(rainXInput, out int parsedRainX))
        {
            rainX = Mathf.Clamp(parsedRainX, 0, grid.Width - 1);
        }
        GUILayout.Label("Y:", GUILayout.Width(20));
        string rainYInput = GUILayout.TextField(rainY.ToString(), GUILayout.Width(50));
        if (int.TryParse(rainYInput, out int parsedRainY))
        {
            rainY = Mathf.Clamp(parsedRainY, 0, grid.Height - 1);
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Rain Cube"))
        {
            RainCube(rainX, rainY, rainCubeType, rainMoveCount);
        }

        GUILayout.EndArea();
    }
}
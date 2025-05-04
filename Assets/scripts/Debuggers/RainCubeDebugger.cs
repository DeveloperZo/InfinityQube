using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RainCubeDebugger : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager grid;
    [SerializeField] private GameObject[] cubePrefabs; // Normal, Green, Black, Blue
    [SerializeField] private Material highlightMaterial;

    [Header("Rain Settings")]
    [SerializeField] private Enumerations.CubeType rainCubeType = Enumerations.CubeType.Normal;
    [SerializeField] private int rainX = 2;
    [SerializeField] private int rainY = 2;
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

    public void RainCube()
    {
        if (isRaining || grid == null) return;

        if (rainX < 0 || rainX >= grid.Width || rainY < 0 || rainY >= grid.Height)
        {
            Debug.LogWarning($"Invalid rain position: {rainX}, {rainY}");
            return;
        }

        int prefabIndex = (int)rainCubeType;
        if (prefabIndex < 0 || prefabIndex >= cubePrefabs.Length || cubePrefabs[prefabIndex] == null)
        {
            Debug.LogWarning($"Invalid cube prefab for type {rainCubeType}");
            return;
        }

        Vector3 spawnPos = new Vector3(rainX, 5f, rainY); // Adjusted spawn height to 5f above the grid
        GameObject cube = Instantiate(cubePrefabs[prefabIndex], spawnPos, Quaternion.identity);

        if (cube != null)
        {
            CubeBehavior behavior = cube.GetComponent<CubeBehavior>();
            if (behavior == null)
            {
                behavior = cube.AddComponent<CubeBehavior>();
                behavior.CubeType = rainCubeType;
            }

            behavior.Init(grid, new Vector2Int(rainX, rainY), 1);
            StartCoroutine(FallUniformly(behavior));
        }
    }

    private IEnumerator FallUniformly(CubeBehavior cube)
    {
        isRaining = true;

        for (int i = 0; i < rainMoveCount; i++)
        {
            if (cube == null) break;

            Vector3 targetPos = new Vector3(cube.position.x, 1f, cube.position.y - 1);
            cube.transform.position = targetPos;

            yield return new WaitForSeconds(moveInterval);
        }

        isRaining = false;
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
            RainCube();
        }

        GUILayout.EndArea();
    }
}
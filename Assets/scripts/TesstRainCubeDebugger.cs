using UnityEngine;
using System.Collections;

public class TestRainCubeDebugger : MonoBehaviour
{
    [SerializeField] public WaveManager waveManager;
    private int targetX;
    private GridManager grid;
    
    private float hoverHeight;
    private float fallSpeed = 4f;
    private string uniqueId;
    
    public void Initialize(int x, GridManager gridManager, float height)
    {
        targetX = x;
        grid = gridManager;
        hoverHeight = height;
        uniqueId = System.Guid.NewGuid().ToString().Substring(0, 8);
        waveManager = FindObjectOfType<WaveManager>();
        // Start falling immediately
        StartCoroutine(FallToGrid());
    }
    
    private IEnumerator FallToGrid()
    {
        // Set initial position with precise coordinates
        transform.position = new Vector3(targetX, hoverHeight, grid.Height - 1);
        
        // Calculate target on grid - use exact integers for grid alignment
        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(targetX, 1f, grid.Height - 1);
        
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / fallSpeed;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Quadratic easing for acceleration
            float eased = t * t;
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, eased);
            
            // Ensure X and Z coordinates stay locked to grid
            newPos.x = targetX;
            newPos.z = grid.Height - 1;
            
            transform.position = newPos;
            
            yield return null;
        }
        
        // Ensure final position is exactly on the grid
        transform.position = targetPos;
        
        // Look for any cube at this position
        CubeBehavior collidingCube = FindCubeAtPosition(targetX, grid.Height - 1);
        
        if (collidingCube != null && collidingCube != GetComponent<CubeBehavior>())
        {
            // We landed on a cube - replace it
            Debug.Log($"Test cube replaced a cube at ({targetX}, {grid.Height - 1})");
            Destroy(collidingCube.gameObject);
        }
        
        // Initialize our cube at this position with exact coordinates
        CubeBehavior thisCube = GetComponent<CubeBehavior>();
        if (thisCube != null)
        {
            thisCube.Init(grid, new Vector2Int(targetX, grid.Height - 1), 1);
            
            Debug.Log($"Test cube initialized at ({targetX}, {grid.Height - 1}) with ID: {uniqueId}");
            // Add to wave manager
            AddToWaveManager(thisCube);
        }
        
        // Clean up this controller
        Destroy(this);
    }
    
    private CubeBehavior FindCubeAtPosition(int x, int z)
    {
        // Find all cubes at the current position
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube.gameObject != gameObject && // Not this cube
                cube.position.x == x && cube.position.y == z)
            {
                return cube;
            }
        }
        return null;
    }
    
    private void AddToWaveManager(CubeBehavior cube)
    {
        
        if (waveManager != null)
        {
            // Register the cube with the wave manager
            waveManager.activeCubes.Add(cube);
        }
        else
        {
            Debug.LogWarning("WaveManager reference is missing in TestRainCubeController.");
        }
    }
}
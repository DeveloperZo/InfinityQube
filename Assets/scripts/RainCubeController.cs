using UnityEngine;
using System.Collections;

public class RainCubeController : MonoBehaviour
{
    private Vector2 targetPosition;
    private GridManager grid;
    private float fallSpeed = 3f;
    private bool hasLanded = false;
    private bool isWaitingForTarget = true;
    private float initialWaitTime = 0.5f; // Wait a bit before starting to look for targets

    // The cube stays in fixed position until it finds a target
    public void Initialize(Vector2 position, GridManager gridManager)
    {
        targetPosition = position;
        grid = gridManager;

        // CRITICAL CHANGE: Initialize position directly above its column
        transform.position = new Vector3(targetPosition.x, 7f, targetPosition.y);

        StartCoroutine(LandOnGrid());
        // Add a visual indicator for debugging
        CreateDebugMarker();
    }

    private void CreateDebugMarker()
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.SetParent(transform);
        marker.transform.localPosition = Vector3.up * 0.5f;
        marker.transform.localScale = Vector3.one * 0.2f;
        marker.GetComponent<Renderer>().material.color = Color.red;

        // Remove collider to avoid physics issues
        Destroy(marker.GetComponent<Collider>());

        // Name it for easy identification in hierarchy
        marker.name = "RainMarker";

        // Auto-destroy after 5 seconds
        Destroy(marker, 5f);
    }


    private IEnumerator LandOnGrid()
    {
        // Calculate target position at the top of the grid
        Vector3 targetPos = new Vector3(targetPosition.x, 1f, targetPosition.y);
        Vector3 startPos = transform.position;

        // Fall straight down
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / fallSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Use quadratic easing for accelerating fall
            float easedT = t * t;
            transform.position = Vector3.Lerp(startPos, targetPos, easedT);

            yield return null;
        }

        // Ensure we're exactly at the target position
        transform.position = targetPos;

        // Squash effect when landing
        transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);
        yield return new WaitForSeconds(0.1f);
        transform.localScale = Vector3.one;

        // Convert to normal cube
        Vector2Int gridPos = new Vector2Int((int)targetPosition.x, (int)targetPosition.y);

        // Reset cube rotation
        transform.rotation = Quaternion.identity;

        // Convert this into a normal cube that follows wave rules
        CubeBehavior thisCube = GetComponent<CubeBehavior>();
        if (thisCube != null)
        {
            thisCube.HandleVerticalImpact(gridPos, grid);
            thisCube.Init(grid, gridPos, 1);

            // Register with wave manager
            WaveManager waveManager = FindObjectOfType<WaveManager>();
            if (waveManager != null)
            {
                waveManager.RegisterRainCube(thisCube);
                Debug.Log($"Rain cube landed on grid at position ({gridPos.x}, {gridPos.y}) and registered with wave manager");
            }
        }

        // Remove this controller
        Destroy(this);
    }

}
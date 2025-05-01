using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TransienceManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Material phasedTileMaterial;

    [SerializeField] private Color phasedCubeColor = new Color(1f, 0.3f, 0.3f, 0.6f);

    [Header("Settings")]
    [SerializeField] private int zoneDuration = 3;
    [SerializeField] private float visualPulseRate = 0.5f;

    private bool zoneActive = false;
    private Vector2Int zoneCenter;
    private int remainingTicks;
    private List<Vector2Int> affectedTiles = new List<Vector2Int>();
    private List<CubeBehavior> phasedCubes = new List<CubeBehavior>();
    private Dictionary<Vector2Int, Material> originalTileMaterials = new Dictionary<Vector2Int, Material>();
    private Coroutine pulseCoroutine;

    public bool IsZoneActive => zoneActive;

    private void Awake()
    {
        if (gridManager == null)
            gridManager = FindObjectOfType<GridManager>();
    }

    public bool ActivateTransienceZone(Vector2Int center)
    {
        if (zoneActive) return false;

        zoneCenter = center;
        remainingTicks = zoneDuration;
        zoneActive = true;

        // Create + pattern
        affectedTiles.Clear();
        affectedTiles.Add(center);

        // Add cardinal directions (N, E, S, W)
        TryAddTile(new Vector2Int(center.x, center.y + 1)); // North
        TryAddTile(new Vector2Int(center.x + 1, center.y)); // East
        TryAddTile(new Vector2Int(center.x, center.y - 1)); // South
        TryAddTile(new Vector2Int(center.x - 1, center.y)); // West

        // Phase tiles and cubes within the zone
        SetupPhaseZone();

        // Start visual effects
        if (pulseCoroutine != null)
            StopCoroutine(pulseCoroutine);
        pulseCoroutine = StartCoroutine(PulseZoneVisuals());

        return true;
    }

    private void TryAddTile(Vector2Int position)
    {
        if (IsValidPosition(position))
            affectedTiles.Add(position);
    }

    private bool IsValidPosition(Vector2Int position)
    {
        return gridManager != null &&
               position.x >= 0 && position.x < gridManager.Width &&
               position.y >= 0 && position.y < gridManager.Height;
    }

    private void SetupPhaseZone()
    {
        phasedCubes.Clear();
        originalTileMaterials.Clear();

        foreach (Vector2Int tilePos in affectedTiles)
        {
            if (!IsValidPosition(tilePos)) continue;

            Tile tile = gridManager.tiles[tilePos.x, tilePos.y];
            if (tile != null)
            {
                // Set tile as phased
                tile.SetPhased(true);

                // Store original material
                Renderer tileRenderer = tile.GetComponent<Renderer>();
                if (tileRenderer != null && phasedTileMaterial != null)
                {
                    originalTileMaterials[tilePos] = tileRenderer.material;
                    tileRenderer.material = phasedTileMaterial;
                }

                // Handle cubes at this position
                FindAndProcessCubesAtPosition(tilePos);
            }
        }
    }

    private void FindAndProcessCubesAtPosition(Vector2Int position)
    {
        // Find all cubes at this position
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube.position.x == position.x && cube.position.y == position.y)
            {
                // Handle gray cubes - remove them entirely
                if (cube.CubeType == Enumerations.CubeType.Normal)
                {
                    Destroy(cube.gameObject);
                }
                // Handle colored cubes - make them phased
                else
                {
                    cube.SetPhased(true);
                    phasedCubes.Add(cube);

                    // Visual effect for phased cube
                    Renderer cubeRenderer = cube.GetComponent<Renderer>();
                    if (cubeRenderer != null)
                    {
                        cubeRenderer.material.color = phasedCubeColor;
                    }
                }
            }
        }
    }

    // Called by WaveManager after each step
    public void TickZone()
    {
        if (!zoneActive) return;

        remainingTicks--;

        if (remainingTicks <= 0)
        {
            EndTransienceZone();
        }
    }

    private void EndTransienceZone()
    {
        // Reset tiles
        foreach (Vector2Int tilePos in affectedTiles)
        {
            if (!IsValidPosition(tilePos)) continue;

            Tile tile = gridManager.tiles[tilePos.x, tilePos.y];
            if (tile != null)
            {
                // Reset tile phased state
                tile.SetPhased(false);

                // Restore original material
                Renderer tileRenderer = tile.GetComponent<Renderer>();
                if (tileRenderer != null && originalTileMaterials.ContainsKey(tilePos))
                {
                    tileRenderer.material = originalTileMaterials[tilePos];
                }
            }
        }

        // Return phased cubes
        ReturnPhasedCubes();

        // Reset state
        zoneActive = false;
        affectedTiles.Clear();
        originalTileMaterials.Clear();

        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
    }

    private void ReturnPhasedCubes()
    {
        foreach (CubeBehavior cube in phasedCubes)
        {
            if (cube == null) continue;

            // Reset phased state
            cube.SetPhased(false);

            // Reset visual appearance
            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeRenderer != null)
            {
                // Restore original color based on cube type
                switch (cube.CubeType)
                {
                    case Enumerations.CubeType.Green:
                        cubeRenderer.material.color = Color.green;
                        break;
                    case Enumerations.CubeType.Black:
                        cubeRenderer.material.color = Color.black;
                        break;
                    case Enumerations.CubeType.Red:
                        cubeRenderer.material.color = Color.red;
                        break;
                    case Enumerations.CubeType.Blue:
                        cubeRenderer.material.color = Color.blue;
                        break;
                    default:
                        cubeRenderer.material.color = Color.gray;
                        break;
                }
            }

            // Check for collision at current position
            Vector2Int cubePos = cube.position;
            if (IsValidPosition(cubePos))
            {
                bool hasCollision = false;

                // Check for other cubes at this position
                foreach (CubeBehavior otherCube in FindObjectsOfType<CubeBehavior>())
                {
                    if (otherCube != cube &&
                        otherCube.position.x == cubePos.x &&
                        otherCube.position.y == cubePos.y &&
                        !otherCube.isPhased)
                    {
                        // Collision detected - destroy both cubes
                        Destroy(otherCube.gameObject);
                        Destroy(cube.gameObject);
                        hasCollision = true;
                        break;
                    }
                }

                // Check if tile is corrupted
                Tile tile = gridManager.tiles[cubePos.x, cubePos.y];
                if (!hasCollision && tile != null && tile.IsBlackened)
                {
                    // If tile is corrupted, cube is lost
                    Destroy(cube.gameObject);
                }
            }
        }

        phasedCubes.Clear();
    }

    private IEnumerator PulseZoneVisuals()
    {
        while (zoneActive)
        {
            foreach (Vector2Int tilePos in affectedTiles)
            {
                if (!IsValidPosition(tilePos)) continue;

                Tile tile = gridManager.tiles[tilePos.x, tilePos.y];
                if (tile != null)
                {
                    // Update countdown visualization
                    tile.UpdatePhaseCountdown(remainingTicks);
                }
            }

            yield return new WaitForSeconds(visualPulseRate);
        }
    }

    public bool IsTileInTransienceZone(Vector2Int position)
    {
        return zoneActive && affectedTiles.Contains(position);
    }
}
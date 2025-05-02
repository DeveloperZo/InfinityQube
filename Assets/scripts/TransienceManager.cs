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
    private Dictionary<Tile, Material> originalTileMaterials = new Dictionary<Tile, Material>();
    private Coroutine pulseCoroutine;

    private Dictionary<Vector2Int, bool> slashDistortionPoints = new Dictionary<Vector2Int, bool>();
    private List<Vector2Int> distortionPoints = new List<Vector2Int>();

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
                    originalTileMaterials[tile] = tileRenderer.material;
                    tileRenderer.material = phasedTileMaterial;
                }

                // Handle cubes at this position
                FindAndProcessCubesAtPosition(tilePos);
            }
        }
    }

    public void RegisterSlashDistortionPoint(Vector2Int position)
    {
        if (!IsValidPosition(position)) return;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null || tile.IsBlackened) return;

        // Use Dictionary to track which points are from slash patterns
        if (!slashDistortionPoints.ContainsKey(position))
        {
            // Still add to regular detonation points for UI tracking
            if (!distortionPoints.Contains(position))
            {
                distortionPoints.Add(position);
            }

            slashDistortionPoints[position] = true;
            MarkTileAsDistortionPoint(position);
            Debug.Log($"Slash detonation point registered at {position}");
        }
    }

    // Trigger all points in a slash pattern immediately
    public void TriggerSlashDistortion(Vector2Int center)
    {
        List<Vector2Int> slashPoints = new List<Vector2Int>();
        bool useForwardSlash = (center.x + center.y) % 2 == 0;

        // Identify the 3 points in the slash
        if (useForwardSlash) // / pattern
        {
            for (int offset = -1; offset <= 1; offset++)
            {
                Vector2Int pos = new Vector2Int(center.x + offset, center.y + offset);
                if (IsValidPosition(pos) && slashDistortionPoints.ContainsKey(pos))
                {
                    slashPoints.Add(pos);
                }
            }
        }
        else // \ pattern
        {
            for (int offset = -1; offset <= 1; offset++)
            {
                Vector2Int pos = new Vector2Int(center.x - offset, center.y + offset);
                if (IsValidPosition(pos) && slashDistortionPoints.ContainsKey(pos))
                {
                    slashPoints.Add(pos);
                }
            }
        }

        // Trigger transience for each point in the slash
        foreach (Vector2Int point in slashPoints)
        {
            PerformSingleTileTransience(point);

            // Remove from tracking
            slashDistortionPoints.Remove(point);
            distortionPoints.Remove(point);
        }
    }

    // Special transience that only affects the exact tile
    private void PerformSingleTileTransience(Vector2Int position)
    {
        if (!IsValidPosition(position)) return;

        // Get the tile
        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile == null) return;

        // Reset the tile appearance
        ResetTileMaterial(tile);

        // Visual effect
        StartCoroutine(FlashTile(tile));

        // Process just this position
        // Similar to DetonateCubesAt but for transience effect
        ProcessTransienceAt(position);
    }

    private void ProcessTransienceAt(Vector2Int position)
    {
        // Find all cubes at this position
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube == null) continue;

            if (cube.position.x == position.x && cube.position.y == position.y)
            {
                // Handle normal cubes - they get consumed
                if (cube.CubeType == Enumerations.CubeType.Normal)
                {
                    Destroy(cube.gameObject);
                }
                // Handle non-normal cubes - they become phased
                else
                {
                    cube.SetPhased(true);
                    phasedCubes.Add(cube);

                    // Visual effect
                    Renderer cubeRenderer = cube.GetComponent<Renderer>();
                    if (cubeRenderer != null)
                    {
                        cubeRenderer.material.color = phasedCubeColor;
                    }
                }
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
                if (tileRenderer != null && originalTileMaterials.ContainsKey(tile))
                {
                    tileRenderer.material = originalTileMaterials[tile];
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

    private void MarkTileAsDistortionPoint(Vector2Int position)
    {
        if (!IsValidPosition(position)) return;

        Tile tile = gridManager.tiles[position.x, position.y];
        if (tile != null)
        {
            Renderer renderer = tile.GetComponent<Renderer>();
            if (renderer != null && phasedTileMaterial != null)
            {
                originalTileMaterials[tile] = renderer.material;
                renderer.material = phasedTileMaterial;
            }
        }
    }

    private void ResetTileMaterial(Tile tile)
    {
        if (tile == null) return;
        var tilePosition = new Vector2Int((int)tile.transform.position.x, (int)tile.transform.position.z);

        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer != null && originalTileMaterials.ContainsKey(tile))
        {
            renderer.material = originalTileMaterials[tile];
            originalTileMaterials.Remove(tile);
        }
    }

    private IEnumerator FlashTile(Tile tile)
    {
        if (tile == null) yield break;

        Renderer renderer = tile.GetComponent<Renderer>();
        if (renderer == null) yield break;

        Material originalMaterial = renderer.material;
        renderer.material.color = Color.yellow; // Example flash color

        yield return new WaitForSeconds(0.3f);

        if (renderer != null)
        {
            renderer.material = originalMaterial;
        }
    }
}
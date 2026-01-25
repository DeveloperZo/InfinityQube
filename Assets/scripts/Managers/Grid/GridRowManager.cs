using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles row management operations for the grid.
/// Extracted from GridManager as part of SRP refactoring.
/// GridManager maintains facade methods that delegate to this controller.
/// </summary>
public class GridRowManager : MonoBehaviour
{
    #region References
    private GridManager grid;
    private bool enableDebugLogs;
    #endregion

    #region Events
    /// <summary>
    /// Event fired when bottom row removal starts (for animation hooks)
    /// </summary>
    public System.Action<int> OnBottomRowRemovalStarted;
    
    /// <summary>
    /// Event fired when bottom row removal completes (for animation hooks)
    /// </summary>
    public System.Action<int> OnBottomRowRemovalCompleted;
    #endregion

    #region State
    private bool isRemovingBottomRow = false;
    public bool IsRemovingBottomRow => isRemovingBottomRow;
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the row manager with references to parent manager.
    /// </summary>
    public void Initialize(GridManager gridManager, bool debugLogs)
    {
        grid = gridManager;
        enableDebugLogs = debugLogs;
        DebugLog("GridRowManager initialized");
    }

    /// <summary>
    /// Updates debug logging state from parent manager.
    /// </summary>
    public void SetDebugLogs(bool enabled)
    {
        enableDebugLogs = enabled;
    }
    #endregion

    #region Row Removal
    /// <summary>
    /// Removes the bottom row with a controlled visual transition.
    /// Uses coroutine for smooth animation and provides hooks for future animation systems.
    /// </summary>
    public void RemoveBottomRow()
    {
        if (grid == null || !grid.IsGridReady) return;
        if (isRemovingBottomRow) 
        {
            DebugLog("RemoveBottomRow: Already removing a row, skipping duplicate call");
            return;
        }
        
        StartCoroutine(RemoveBottomRowCoroutine());
    }
    
    private IEnumerator RemoveBottomRowCoroutine()
    {
        isRemovingBottomRow = true;
        int rowToRemove = grid.bottom;
        int height = grid.Height;
        int width = grid.Width;
        
        // Safety check: ensure we have a valid row to remove
        if (rowToRemove >= height)
        {
            DebugLog($"⚠️ ROW PENALTY: Cannot remove row {rowToRemove} - exceeds grid height {height}. Aborting.");
            isRemovingBottomRow = false;
            yield break;
        }
        
        // Safety check: prevent removing if we'd have too few rows left
        int remainingRows = height - (rowToRemove + 1);
        if (remainingRows < 3)
        {
            DebugLog($"⚠️ ROW PENALTY: Cannot remove row {rowToRemove} - would leave only {remainingRows} rows. Minimum 3 rows required. Aborting.");
            isRemovingBottomRow = false;
            yield break;
        }
        
        DebugLog($"⚠️ ROW PENALTY: Removing bottom row {rowToRemove} due to Unit cube escape penalty (will leave {remainingRows} rows)");
        
        // Fire start event (for future animation systems)
        OnBottomRowRemovalStarted?.Invoke(rowToRemove);
        
        // Collect all tiles and cubes in the row
        List<Tile> tilesToRemove = new List<Tile>();
        List<CubeManager> cubesToRemove = new List<CubeManager>();
        
        for (int x = 0; x < width; x++)
        {
            Tile tile = grid.GetTileAt(x, rowToRemove);
            if (tile != null)
            {
                tilesToRemove.Add(tile);
            }
        }
        
        // Find cubes on this row
        var allCubes = FindObjectsByType<CubeManager>(FindObjectsSortMode.None);
        foreach (var cube in allCubes)
        {
            if (cube != null && !cube.isDestroyed && cube.position.y == rowToRemove)
            {
                cubesToRemove.Add(cube);
            }
        }
        
        // Simple transition: fade out tiles and cubes
        float transitionDuration = 0.5f;
        float elapsed = 0f;
        
        // Store initial renderers for fade
        Dictionary<Tile, Renderer> tileRenderers = new Dictionary<Tile, Renderer>();
        Dictionary<CubeManager, Renderer> cubeRenderers = new Dictionary<CubeManager, Renderer>();
        Dictionary<Tile, MaterialPropertyBlock> tilePropertyBlocks = new Dictionary<Tile, MaterialPropertyBlock>();
        Dictionary<CubeManager, MaterialPropertyBlock> cubePropertyBlocks = new Dictionary<CubeManager, MaterialPropertyBlock>();
        
        foreach (var tile in tilesToRemove)
        {
            Renderer renderer = tile.GetComponent<Renderer>();
            if (renderer != null)
            {
                tileRenderers[tile] = renderer;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                tilePropertyBlocks[tile] = block;
            }
        }
        
        foreach (var cube in cubesToRemove)
        {
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null)
            {
                cubeRenderers[cube] = renderer;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                cubePropertyBlocks[cube] = block;
            }
        }
        
        // Fade out animation
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - (elapsed / transitionDuration);
            
            // Fade tiles
            foreach (var tile in tilesToRemove)
            {
                if (tile != null && tileRenderers.ContainsKey(tile) && tilePropertyBlocks.ContainsKey(tile))
                {
                    Renderer renderer = tileRenderers[tile];
                    MaterialPropertyBlock block = tilePropertyBlocks[tile];
                    
                    Color color = block.GetColor("_Color");
                    if (color == Color.clear) color = Color.white;
                    color.a = alpha;
                    block.SetColor("_Color", color);
                    renderer.SetPropertyBlock(block);
                }
            }
            
            // Fade cubes
            foreach (var cube in cubesToRemove)
            {
                if (cube != null && !cube.isDestroyed && cubeRenderers.ContainsKey(cube) && cubePropertyBlocks.ContainsKey(cube))
                {
                    Renderer renderer = cubeRenderers[cube];
                    if (renderer != null)
                    {
                        MaterialPropertyBlock block = cubePropertyBlocks[cube];
                        
                        Color color = block.GetColor("_Color");
                        if (color == Color.clear) color = Color.white;
                        color.a = alpha;
                        block.SetColor("_Color", color);
                        renderer.SetPropertyBlock(block);
                    }
                }
            }
            
            yield return null;
        }
        
        // Safety check: Verify grid is still valid
        if (!grid.IsGridReady || rowToRemove >= grid.Height)
        {
            DebugLog($"⚠️ Grid state changed during removal. Row {rowToRemove} no longer valid. Aborting cleanup.");
            isRemovingBottomRow = false;
            yield break;
        }
        
        // Cleanup: Actually remove tiles and cubes
        foreach (var tile in tilesToRemove)
        {
            if (tile != null)
            {
                tile.MakeTileFall();
            }
        }
        
        // Remove cubes that still exist
        foreach (var cube in cubesToRemove)
        {
            if (cube != null && !cube.isDestroyed)
            {
                DebugLog($"Removing cube at ({cube.position.x}, {cube.position.y}) - row fell");
                Destroy(cube.gameObject);
            }
        }
        
        // Update grid bounds
        grid.bottom = Mathf.Min(grid.bottom + 1, grid.Height - 1);
        
        // Adjust player position if needed
        AdjustPlayerPositionAfterRowRemoval(rowToRemove);
        
        // Fire completion event
        OnBottomRowRemovalCompleted?.Invoke(rowToRemove);
        
        isRemovingBottomRow = false;
        DebugLog($"✅ ROW PENALTY: Bottom row {rowToRemove} removal complete. New bottom: {grid.bottom}, Grid height: {grid.Height}, Remaining playable rows: {grid.Height - grid.bottom}");
    }
    
    /// <summary>
    /// Adjusts player position after a row has been removed.
    /// </summary>
    private void AdjustPlayerPositionAfterRowRemoval(int removedRow)
    {
        var playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager == null) return;
        
        int playerY = playerManager.currentTilePosition.y;
        
        // If player was on or below the removed row, move them up
        if (playerY <= removedRow)
        {
            int safeRow = FindLowestPlayableRow();
            if (safeRow > removedRow)
            {
                DebugLog($"⚠️ ROW PENALTY: Moving player from row {playerY} to safe row {safeRow}");
                playerManager.SetPosition(playerManager.currentTilePosition.x, safeRow);
            }
            else
            {
                DebugLog($"⚠️ ROW PENALTY: No safe row found above {removedRow}. Moving to top row.");
                playerManager.SetPosition(playerManager.currentTilePosition.x, grid.Height - 1);
            }
        }
    }

    /// <summary>
    /// Removes all cubes on a specific row.
    /// </summary>
    public void RemoveCubesOnRow(int row)
    {
        var allCubes = FindObjectsByType<CubeManager>(FindObjectsSortMode.None);
        foreach (var cube in allCubes)
        {
            if (cube != null && !cube.isDestroyed && cube.position.y == row)
            {
                DebugLog($"Removing cube at ({cube.position.x}, {cube.position.y}) - row fell");
                Destroy(cube.gameObject);
            }
        }
    }
    #endregion

    #region Row Queries
    /// <summary>
    /// Finds the lowest playable row in the grid.
    /// </summary>
    public int FindLowestPlayableRow()
    {
        int startRow = Mathf.Max(grid.bottom + 1, 1);
        
        for (int y = startRow; y < grid.Height; y++)
        {
            bool rowIsPlayable = false;
            for (int x = 0; x < grid.Width; x++)
            {
                Tile tile = grid.GetTileAt(x, y);
                if (tile != null && tile.IsPlayable)
                {
                    rowIsPlayable = true;
                    break;
                }
            }
            if (rowIsPlayable) return y;
        }
        return grid.Height - 1;
    }

    /// <summary>
    /// Gets the count of playable rows in the grid.
    /// </summary>
    public int GetPlayableRowCount()
    {
        int playableRows = 0;
        for (int y = 0; y < grid.Height; y++)
        {
            bool hasPlayableTile = false;
            for (int x = 0; x < grid.Width; x++)
            {
                Tile tile = grid.GetTileAt(x, y);
                if (tile != null && tile.IsPlayable)
                {
                    hasPlayableTile = true;
                    break;
                }
            }
            if (hasPlayableTile) playableRows++;
        }
        return playableRows;
    }

    /// <summary>
    /// Checks if a specific row is playable.
    /// </summary>
    public bool IsRowPlayable(int row)
    {
        if (!grid.IsValidGridPosition(0, row)) return false;

        for (int x = 0; x < grid.Width; x++)
        {
            Tile tile = grid.GetTileAt(x, row);
            if (tile != null && tile.IsPlayable)
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    #region Debug Logging
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[GridRowManager] {message}");
        }
    }
    #endregion
}

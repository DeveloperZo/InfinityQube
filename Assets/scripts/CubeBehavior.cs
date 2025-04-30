using UnityEngine;
using System.Collections;

public class CubeBehavior : MonoBehaviour
{
    [Header("Cube Properties")]
    [SerializeField] public int level = 1;
    [SerializeField] public Vector2Int position;
    [SerializeField] public Enumerations.CubeType CubeType;
    
    [Header("Animation Settings")]
    [SerializeField] private float moveDuration = 0.25f;
    [SerializeField] private float squashDuration = 0.05f;
    
    private GridManager grid;
    private bool isMoving = false;
    private bool isDestroyed = false;

    public void Init(GridManager gridManager, Vector2Int startPos, int startLevel)
    {
        if (gridManager == null)
        {
            Debug.LogError("CubeBehavior initialized with null GridManager!");
            Destroy(gameObject);
            return;
        }
        
        grid = gridManager;
        position = startPos;
        level = startLevel;
        transform.position = new Vector3(position.x, 1f, position.y);
    }

    private void OnDestroy()
    {
        // Mark as destroyed to prevent issues during coroutines
        isDestroyed = true;
        StopAllCoroutines();
    }

    public bool MoveForward()
    {
        if (isMoving || isDestroyed) return true;

        position.y -= 1;

        // Off the grid = escape
        if (position.y < 0 || position.x < 0 || position.x >= grid.Width)
        {
            Debug.Log($"Cube escaped at level {level}");
            Destroy(gameObject);
            return false;
        }

        StartCoroutine(AnimateMove(position));
        return true;
    }

    private IEnumerator AnimateMove(Vector2Int newPos)
    {
        isMoving = true;

        Vector3 start = transform.position;
        Vector3 end = new Vector3(newPos.x, 1f, newPos.y);
        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(-90f, 0f, 0f); // 90° roll forward

        while (elapsed < moveDuration)
        {
            if (isDestroyed) yield break;
            
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);

            transform.position = Vector3.Lerp(start, end, t);
            transform.rotation = Quaternion.Slerp(startRot, Quaternion.Lerp(startRot, endRot, t), t);

            yield return null;
        }

        if (isDestroyed) yield break;

        // Weighty visual squash
        transform.position = end;
        transform.localScale = new Vector3(1.05f, 0.9f, 1.05f);
        yield return new WaitForSeconds(squashDuration);
        
        if (isDestroyed) yield break;
        
        transform.localScale = Vector3.one;

        // Check for marker interaction (guard against destroyed tiles)
        if (grid != null && newPos.x >= 0 && newPos.x < grid.Width && 
            newPos.y >= 0 && newPos.y < grid.Height)
        {
            Tile tile = grid.tiles[newPos.x, newPos.y];
            if (tile != null && tile.HasMarker)
            {
                tile.ProcessCubeInteraction(this);
            }
        }

        isMoving = false;
    }
}
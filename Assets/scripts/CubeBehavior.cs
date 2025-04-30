using UnityEngine;
using System.Collections;

public class CubeBehavior : MonoBehaviour
{
    public int level = 1;
    public Vector2Int position;
    private GridManager grid;
    public Enumerations.CubeType CubeType;
    private bool isMoving = false;

    public void Init(GridManager gridManager, Vector2Int startPos, int startLevel)
    {
        grid = gridManager;
        position = startPos;
        level = startLevel;
        transform.position = new Vector3(position.x, 1f, position.y);
    }

    public bool MoveForward()
    {
        if (isMoving) return true;

        position.y -= 1;

        // Off the grid = escape
        if (position.y < 0 || position.x < 0 || position.x >= grid.width)
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
        float duration = 0.25f;
        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = startRot * Quaternion.Euler(90f, 0f, 0f); // 90° roll forward

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.position = Vector3.Lerp(start, end, t);
            transform.rotation = Quaternion.Slerp(endRot, startRot, t);

            yield return null;
        }

        // Weighty visual squash
        transform.position = end;
        transform.localScale = new Vector3(1.05f, 0.9f, 1.05f);
        yield return new WaitForSeconds(0.05f);
        transform.localScale = Vector3.one;

        // Check for marker interaction
        var tile = grid.tiles[newPos.x, newPos.y];
        if (tile.HasMarker)
        {
            tile.ProcessCubeInteraction(this);
            // Reset the cube's position to the tile's position
        }

        isMoving = false;
    }
}

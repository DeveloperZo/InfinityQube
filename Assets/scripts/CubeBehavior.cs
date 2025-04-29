using UnityEngine;

public class CubeBehavior : MonoBehaviour
{
    public int level = 1;
    public float moveDelay = 0.5f;
    public Vector2Int position;
    private float moveTimer = 0f;

    void Update()
    {
        moveTimer += Time.deltaTime;
        if (moveTimer >= moveDelay)
        {
            moveTimer = 0f;
            MoveForward();
        }
    }

    void MoveForward()
    {
        Vector3 nextPos = transform.position + Vector3.forward;
        position += Vector2Int.up;
        transform.position = nextPos;

        GameObject hit = GameObject.Find($"Tile_{position.x}_{position.y}");
        if (hit != null && hit.GetComponent<Tile>().HasMarker)
        {
            level--;
            if (level <= 0) Destroy(gameObject);
        }

        if (position.y >= 6)
        {
            Debug.Log($"Cube escaped at level {level}");
            Destroy(gameObject);
        }
    }
}
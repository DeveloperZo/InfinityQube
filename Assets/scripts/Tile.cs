using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x, y;
    public bool HasMarker = false;
    public GameObject markerPrefab;

    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public void PlaceMarker()
    {
        HasMarker = true;
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.transform.position = transform.position + Vector3.up * 0.5f;
        marker.name = $"Marker_{x}_{y}";
    }
}

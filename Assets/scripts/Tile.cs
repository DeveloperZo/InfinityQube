using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x, y;
    public bool HasMarker = false;
    GameObject markerObj;
    private Color originalColor; // store original tile color
    private Renderer tileRenderer;

    public void Init(int x, int y)
    {
        this.x = x;
        this.y = y;
        tileRenderer = GetComponent<Renderer>();
        originalColor = tileRenderer.material.color; // capture initial color
    }

    public void PlaceMarker()
    {
        if (HasMarker) return;

        HasMarker = true;

        markerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        markerObj.transform.position = transform.position + Vector3.up * 0.3f;
        markerObj.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
        markerObj.name = $"Marker_{x}_{y}";

        var markerRenderer = markerObj.GetComponent<Renderer>();
        markerRenderer.material.color = Color.red;

        tileRenderer.material.color = new Color(1f, 0.4f, 0.4f); // subtle red tint
    }

    public void ResetTileVisual()
    {
        if (!HasMarker)
            GetComponent<Renderer>().material.color = Color.gray;
    }
    public void ToggleMarker()
    {
        if (HasMarker)
        {
            ClearMarker();
        }
        else
        {
            PlaceMarker();
        }
    }

    public void ClearMarker()
    {
        HasMarker = false;

        if (markerObj != null)
            Destroy(markerObj);

        tileRenderer.material.color = originalColor; // reset properly
    }

    public void ActivateMarker()
    {
        HasMarker = false;

        if (markerObj != null)
            Destroy(markerObj);

        tileRenderer.material.color = Color.grey; // reset properly
    }

}

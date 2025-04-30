using System;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public int x, y;
    public bool HasMarker = false;
    GameObject markerObj;
    private Color originalColor; // store original tile color
    private Renderer tileRenderer;
    private CubeBehavior cube;

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
        markerRenderer.material.color = Color.blue;

        tileRenderer.material.color = new Color(1f, 0.4f, 0.4f); // subtle red tint
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

    public void ProcessCubeInteraction(CubeBehavior cube)
    {
        this.cube = cube;
    }
    public void TriggerMarker()
    {
        if (HasMarker)
        {
            if(cube.gameObject == null)
                cube = null;

            CubeBehavior cubeToProcess = cube; // Store reference to avoid accessing after destruction
            ActivateMarker();

            if (cubeToProcess != null &&cubeToProcess.CubeType == Enumerations.CubeType.Green)
            {
                DetonationManager detonationManager = FindObjectOfType<DetonationManager>();
                if (detonationManager != null)
                {
                    detonationManager.RegisterDetonationPoint(new Vector2Int(x, y));
                }
            }

            switch (cubeToProcess.CubeType)
            {
                case Enumerations.CubeType.Normal:
                    // Normal cubes just get destroyed
                    cubeToProcess.level--;
                    if (cubeToProcess.level <= 0)
                    {
                        Destroy(cubeToProcess.gameObject);
                    }
                    break;

                case Enumerations.CubeType.Green:
                    // Green cubes register a detonation point
                    cubeToProcess.level--;
                    if (cubeToProcess.level <= 0)
                    {
                        Destroy(cubeToProcess.gameObject);
                    }
                    break;

                case Enumerations.CubeType.Black:
                    // Black cubes cause a penalty
                    transform.position = new Vector3(transform.position.x, -0.2f, transform.position.z);
                    break;
            }
        }
    }
}

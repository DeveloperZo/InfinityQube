using UnityEngine;

public class Tile : MonoBehaviour
{
    [Header("Tile Properties")]
    [SerializeField] private int x, y;
    [SerializeField] private bool hasMarker = false;
    [SerializeField] private float markerHeight = 0.3f;
    [SerializeField] private float markerScale = 0.5f;
    [SerializeField] private Color markerColor = Color.blue;
    [SerializeField] private Color markedTileColor = new Color(1f, 0.4f, 0.4f);

    public bool IsBlackened => isBlackened;

    private GameObject markerObj;
    private Color originalColor;
    private Renderer tileRenderer;
    private CubeBehavior currentCube;
    private bool isInitialized = false;
    private bool isBlackened = false;

    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        if (tileRenderer == null)
        {
            Debug.LogError("Tile requires a Renderer component!");
            enabled = false;
            return;
        }
    }

    public void Init(int xPos, int yPos)
    {
        x = xPos;
        y = yPos;
        
        if (tileRenderer != null)
        {
            originalColor = tileRenderer.material.color;
        }
        
        isInitialized = true;
    }

    private void OnDestroy()
    {
        if (markerObj != null)
        {
            Destroy(markerObj);
            markerObj = null;
        }
    }

    public bool HasMarker => hasMarker;

    public void PlaceMarker()
    {
        if (!isInitialized || hasMarker || isBlackened) return;

        hasMarker = true;

        // Create marker object
        if (markerObj == null)
        {
            markerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerObj.transform.SetParent(transform);
            markerObj.transform.localPosition = Vector3.up * markerHeight;
            markerObj.transform.localScale = new Vector3(markerScale, 0.1f, markerScale);
            markerObj.name = $"Marker_{x}_{y}";

            // Set marker color
            Renderer markerRenderer = markerObj.GetComponent<Renderer>();
            if (markerRenderer != null)
            {
                // Use bright, high-contrast color
                markerRenderer.material.color = Color.magenta;
                // Optional: Add emission to make it glow
                markerRenderer.material.EnableKeyword("_EMISSION");
                markerRenderer.material.SetColor("_EmissionColor", Color.magenta * 0.8f);
            }
        }

        // Make tile tint more obvious
        if (tileRenderer != null)
        {
            tileRenderer.material.color = new Color(1f, 0.2f, 0.2f); // Brighter red

            // Debug log for verification
            Debug.Log($"Marked tile at {x},{y} - hasMarker={hasMarker}");
        }
    }

    public void BlackenTile()
    {
        isBlackened = true;
        ClearMarker(); // Remove any existing marker

        // Visual indication
        if (tileRenderer != null)
        {
            tileRenderer.material.color = Color.black;
        }
    }

    public void ToggleMarker()
    {
        if (hasMarker)
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
        hasMarker = false;

        if (markerObj != null)
        {
            Destroy(markerObj);
            markerObj = null;
        }

        if (tileRenderer != null)
        {
            tileRenderer.material.color = originalColor;
        }
    }

    public void ActivateMarker()
    {
        hasMarker = false;

        if (markerObj != null)
        {
            Destroy(markerObj);
            markerObj = null;
        }

        if (tileRenderer != null)
        {
            tileRenderer.material.color = Color.grey;
        }
    }

    public void ProcessCubeInteraction(CubeBehavior cube)
    {
        if (cube != null)
        {
            currentCube = cube;
        }
    }

    public void TriggerMarker()
    {
        if (!hasMarker) return;
        
        // Store reference to cube before changing marker state
        CubeBehavior cubeToProcess = currentCube;
        ActivateMarker();

        if (cubeToProcess == null)
        {
            Debug.LogWarning("No cube to process on marker trigger.");
            return;
        }

        // Check if the cube has been destroyed
        if (cubeToProcess == null || cubeToProcess.gameObject == null)
        {
            return;
        }

        DetonationManager detonationManager = null;
        
        // Handle green cube before potential destruction
        if (cubeToProcess.CubeType == Enumerations.CubeType.Green)
        {
            detonationManager = FindObjectOfType<DetonationManager>();
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
        
        // Clear cube reference after processing
        currentCube = null;
    }
}
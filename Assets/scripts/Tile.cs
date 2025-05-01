using System;
using System.Collections;
using UnityEngine;
using static Enumerations;


public class TimeFrozenTag : MonoBehaviour
{
    // Just a tag component to identify frozen cubes
    public float frozenDuration = 1f; // Number of movement cycles to skip
    public Color originalColor;

    void Start()
    {
        // Auto-destroy after a set time to prevent permanent freeze
        Destroy(this, 5f);
    }

    private void OnDestroy()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null && originalColor != null)
        {
            renderer.material.color = originalColor;
        }
    }
}

public class Tile : MonoBehaviour
{
    [Header("Tile Properties")]
    [SerializeField] private int x, y;
    [SerializeField] private bool hasMarker = false;
    [SerializeField] private float markerHeight = 0.3f;
    [SerializeField] private float markerScale = 0.5f;
    [SerializeField] private Color markerColor = Color.blue;
    [SerializeField] private Color markedTileColor = new Color(1f, 0.4f, 0.4f);

    [Header("Enhanced Green Tile")]
    [SerializeField] private int detonationCharges = 0;
    [SerializeField] private int maxCharges = 3;


    [SerializeField]
    private Color[] chargeColors = new Color[3] {
    new Color(0f, 0.8f, 0.3f),  // First charge - green (1 tile)
    new Color(0f, 0.9f, 0.4f),  // Second charge - brighter green (2x2)
    new Color(0f, 1f, 0.5f)     // Third charge - brightest green (3x3)
};

    // Properties to access charge information
    public int DetonationCharges => detonationCharges;
    public bool HasCharges => detonationCharges > 0;

    public bool IsBlackened => isBlackened;
    public bool IsPrimed => isPrimed;
    public TileState currentState = TileState.Normal;
    private Color normalColor;
    private Color transformedColor = new Color(0.3f, 0.3f, 0.3f); // Dark gray

    public bool CanBeMarked => currentState == TileState.Normal;
    private GameObject markerObj;
    private Color originalColor;
    private Renderer tileRenderer;
    public CubeBehavior currentCube;
    private bool isInitialized = false;
    private bool isBlackened = false;
    private bool isPrimed = false;
    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        normalColor = tileRenderer.material.color;
    }

    // In Tile.cs
    public void TransformTile(CubeType cubeType)
    {
        if (currentState != TileState.Transformed)
        {
            currentState = TileState.Transformed;

            // Visual change - sink the tile
            transform.position = new Vector3(transform.position.x, -0.2f, transform.position.z);

            if (cubeType == CubeType.Black)
            {
                Debug.Log("Tile has been blackened");
                BlackenTile();
            }
            else if (cubeType == CubeType.Green)
            {
                // First green charge
                detonationCharges = 1;
                UpdateChargeVisuals();
            }
            else if (cubeType == CubeType.Blue)
            {
                // Blue tile effect
                if (tileRenderer != null)
                {
                    tileRenderer.material.color = new Color(0.5f, 0.8f, 1f); // Light blue
                }
            }
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

    public int Charges { get; set; }

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
        isPrimed = false;
        ClearMarker(); // Remove any existing marker

        // Visual indication
        if (tileRenderer != null)
        {
            tileRenderer.material.color = Color.black;
        }
    }

    public void EnhanceGreenTile()
    {
        // Increase charges up to the max
        if (detonationCharges < maxCharges)
        {
            detonationCharges++;

            // Visual indication of charge level
            UpdateChargeVisuals();

            Debug.Log($"Green tile at ({x}, {y}) enhanced to charge level {detonationCharges}");
        }
    }

    private void UpdateChargeVisuals()
    {
        if (tileRenderer != null && detonationCharges > 0 && detonationCharges <= chargeColors.Length)
        {
            // Apply color based on charge level
            tileRenderer.material.color = chargeColors[detonationCharges - 1];
        }
    }

    public void ReduceCharge()
    {
        if (detonationCharges > 0)
        {
            if(isPrimed)
                detonationCharges--;
            else
                detonationCharges = 0; // Reset to 0 if not primed

            if (detonationCharges > 0)
            {
                // Still has charges, update visuals
                UpdateChargeVisuals();
            }
            else
            {
                // Reset to normal state if no charges left
                currentState = TileState.Normal;
                if (tileRenderer != null)
                {
                    tileRenderer.material.color = originalColor;
                }
                transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
            }
        }
    }

    public void ResetTile()
    {
        currentState = TileState.Normal;
        isBlackened = false;
        detonationCharges = 0;

        // Reset visual appearance
        if (tileRenderer != null)
        {
            tileRenderer.material.color = originalColor;
        }

        // Reset position
        transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
        ClearMarker();
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
        // Handle blue cube special ability
        else if (cubeToProcess.CubeType == Enumerations.CubeType.Blue)
        {
            // Create time distortion field (2x2)
            CreateTimeDistortionField();
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

            case Enumerations.CubeType.Blue:
                // Blue cubes are consumed when triggered
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

    private void CreateTimeDistortionField()
    {
        // Get grid manager reference
        GridManager gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null) return;

        // Create a 2x2 time distortion field
        for (int dx = 0; dx <= 1; dx++)
        {
            for (int dy = 0; dy <= 1; dy++)
            {
                int targetX = x + dx;
                int targetY = y + dy;

                // Skip if out of bounds
                if (targetX >= gridManager.Width || targetY >= gridManager.Height)
                    continue;

                Tile tile = gridManager.tiles[targetX, targetY];
                if (tile != null)
                {
                    // Visual effect - blue glow
                    Renderer tileRenderer = tile.GetComponent<Renderer>();
                    if (tileRenderer != null)
                    {
                        // Store original color and temporarily change to blue tint
                        StartCoroutine(ApplyTimeDistortion(tile, tileRenderer));
                    }

                    // Mark the cubes in this area for time freeze
                    CubeBehavior cube = tile.currentCube;
                    if (cube != null)
                    {
                        // For MVP: Simple visual feedback
                        Renderer cubeRenderer = cube.GetComponent<Renderer>();
                        if (cubeRenderer != null)
                        {
                            cubeRenderer.material.color = new Color(0.7f, 0.7f, 1f); // Light blue tint
                        }

                        // Make cube skip one movement cycle
                        // For MVP, we'll just mark it as "frozen" for the Wave manager to check
                        cube.gameObject.AddComponent<TimeFrozenTag>();
                    }
                }
            }
        }
    }

    private IEnumerator ApplyTimeDistortion(Tile tile, Renderer renderer)
    {
        Color originalColor = renderer.material.color;
        renderer.material.color = new Color(0.6f, 0.8f, 1f); // Blue tint

        // Visual effect duration (one wave)
        yield return new WaitForSeconds(3f);

        // Restore original appearance if tile still exists
        if (tile != null && renderer != null)
        {
            renderer.material.color = originalColor;
        }
    }

    internal void SetPrime(bool isPrimed)
    {
        this.isPrimed = isPrimed;
    }
}
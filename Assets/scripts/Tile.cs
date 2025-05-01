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

        // Handle special cube abilities
        switch (cubeToProcess.CubeType)
        {
            case Enumerations.CubeType.Green:
                // Register with DetonationManager
                DetonationManager detonationManager = FindObjectOfType<DetonationManager>();
                if (detonationManager != null)
                {
                    detonationManager.RegisterDetonationPoint(new Vector2Int(x, y));
                }
                break;

            case Enumerations.CubeType.Blue:
                // Register with TimeDistortionManager
                TimeDistortionManager timeManager = FindObjectOfType<TimeDistortionManager>();
                if (timeManager != null)
                {
                    timeManager.RegisterDistortionPoint(new Vector2Int(x, y));
                }
                break;
        }

        // Process the actual cube that was captured
        switch (cubeToProcess.CubeType)
        {
            case Enumerations.CubeType.Normal:
            case Enumerations.CubeType.Green:
            case Enumerations.CubeType.Blue:
                // These cubes are consumed when triggered
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

    internal void SetPrime(bool isPrimed)
    {
        this.isPrimed = isPrimed;
    }
}
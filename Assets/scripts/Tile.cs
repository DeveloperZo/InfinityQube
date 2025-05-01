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

    private const float TRANSFORMED_HEIGHT = -0.25f;  // Lower by 0.25 for transformed tiles
    private const float MARKED_HEIGHT = 0.25f;        // Raise by 0.25 for marked tiles
    private const float NORMAL_HEIGHT = 0f;           // Normal baseline height

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
    public bool isPhasedZone { get; private set; }
    private TextMesh countdownText;
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
            transform.position = new Vector3(transform.position.x, TRANSFORMED_HEIGHT, transform.position.z);

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

        // Raise the tile for marked state
        transform.position = new Vector3(transform.position.x, MARKED_HEIGHT, transform.position.z);

        // Make tile tint more obvious
        if (tileRenderer != null)
        {
            tileRenderer.material.color = markedTileColor; // Brighter red

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

    public void SetPhased(bool phased)
    {
        isPhasedZone = phased;

        // Create countdown text if entering phased state
        if (phased && countdownText == null)
        {
            GameObject textObj = new GameObject("CountdownText");
            textObj.transform.SetParent(transform);
            textObj.transform.localPosition = new Vector3(0, 1.5f, 0);
            textObj.transform.rotation = Quaternion.Euler(90, 0, 0);

            countdownText = textObj.AddComponent<TextMesh>();
            countdownText.fontSize = 14;
            countdownText.alignment = TextAlignment.Center;
            countdownText.color = Color.red;
        }

        // Remove countdown text if exiting phased state
        if (!phased && countdownText != null)
        {
            Destroy(countdownText.gameObject);
            countdownText = null;
        }
    }

    public void UpdatePhaseCountdown(int remaining)
    {
        if (countdownText != null)
        {
            countdownText.text = remaining.ToString();
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
        transform.position = new Vector3(transform.position.x, NORMAL_HEIGHT, transform.position.z);
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

        // Reset the tile height if not transformed
        if (currentState != TileState.Transformed)
        {
            transform.position = new Vector3(transform.position.x, NORMAL_HEIGHT, transform.position.z);
        }
        else
        {
            // Keep transformed height
            transform.position = new Vector3(transform.position.x, TRANSFORMED_HEIGHT, transform.position.z);
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
        if (hasMarker && cube.CubeType == Enumerations.CubeType.Red)
        {
            // Trigger transience zone
            TransienceManager transManager = FindObjectOfType<TransienceManager>();
            if (transManager != null)
            {
                transManager.ActivateTransienceZone(new Vector2Int(x, y));
            }
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
            case Enumerations.CubeType.Red:
                TransienceManager transienceManager = FindObjectOfType<TransienceManager>();
                if (transienceManager != null)
                {
                    transienceManager.ActivateTransienceZone(new Vector2Int(x, y));
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

    public void HandleBlackCubeLanding(CubeBehavior blackCube)
    {
        if (blackCube == null || blackCube.CubeType != Enumerations.CubeType.Black)
            return;

        if (IsBlackened)
        {
            // Black cube lands on blackened tile - nothing happens
            Debug.Log("Black cube landed on blackened tile - no effect");
            return;
        }

        if (HasCharges)
        {
            // Black cube lands on a charged tile (primed)
            Debug.Log($"Black cube landed on charged tile with {DetonationCharges} charges");

            // Consume charges and trigger a 2x2 mark based on charge level
            DetonationManager detonationManager = FindObjectOfType<DetonationManager>();
            if (detonationManager != null)
            {
                // Register a 2x2 pattern around this position
                for (int dx = 0; dx <= 1; dx++)
                {
                    for (int dy = 0; dy <= 1; dy++)
                    {
                        int targetX = x + dx;
                        int targetY = y + dy;

                        GridManager grid = FindObjectOfType<GridManager>();
                        if (grid != null && targetX < grid.Width && targetY < grid.Height)
                        {
                            detonationManager.RegisterDetonationPoint(new Vector2Int(targetX, targetY));
                        }
                    }
                }

                // Trigger the detonation
                detonationManager.TriggerNextDetonation();
            }

            // Consume all charges
            detonationCharges = 0;

            // Reset to normal tile state
            currentState = Enumerations.TileState.Normal;
            if (tileRenderer != null)
            {
                tileRenderer.material.color = originalColor;
            }
            transform.position = new Vector3(transform.position.x, 0f, transform.position.z);
            return;
        }

        // Check for time frozen state - we need to examine cubes on this tile
        foreach (CubeBehavior cube in FindObjectsOfType<CubeBehavior>())
        {
            if (cube != blackCube && cube.position.x == x && cube.position.y == y)
            {
                TimeFrozenTag frozenTag = cube.GetComponent<TimeFrozenTag>();
                if (frozenTag != null)
                {
                    // Black cube landed on time distorted tile - double the freeze
                    frozenTag.frozenDuration *= 2;

                    // Apply the same freeze to the black cube
                    TimeFrozenTag blackFrozenTag = blackCube.gameObject.AddComponent<TimeFrozenTag>();
                    if (blackFrozenTag != null)
                    {
                        blackFrozenTag.frozenDuration = frozenTag.frozenDuration;

                        // Visual effect for frozen black cube
                        Renderer blackRenderer = blackCube.GetComponent<Renderer>();
                        if (blackRenderer != null)
                        {
                            blackFrozenTag.originalColor = blackRenderer.material.color;
                            blackRenderer.material.color = new Color(0.3f, 0.3f, 0.5f); // Dark blue-gray
                        }
                    }
                    return;
                }
            }
        }
    }
}
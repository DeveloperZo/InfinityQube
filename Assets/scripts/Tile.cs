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
    public bool IsAdvantaged => isAdvantaged;
    public TileState currentState = TileState.Normal;
    private Color normalColor;

    public bool CanBeMarked => currentState == TileState.Normal;
    private GameObject markerObj;
    private Color originalColor;
    private Renderer tileRenderer;
    public CubeBehavior currentCube;
    private bool isInitialized = false;
    private bool isBlackened = false;
    private bool isAdvantaged = false;
    public bool isPhasedZone { get; private set; }
    private TextMesh countdownText;
    private void Awake()
    {
        tileRenderer = GetComponent<Renderer>();
        normalColor = tileRenderer.material.color;
    }

    // In Tile.cs
    public void TransformTile(Enumerations.CubeType cubeType)
    {
        if (currentState != Enumerations.TileState.Transformed)
        {
            currentState = Enumerations.TileState.Transformed;

            // Visual change - sink the tile
            transform.position = new Vector3(transform.position.x, TRANSFORMED_HEIGHT, transform.position.z);

            switch (cubeType)
            {
                case Enumerations.CubeType.Black:
                    BlackenTile();
                    break;

                case Enumerations.CubeType.Green:
                    // First green charge
                    AdvantageTile();
                    break;

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
        isAdvantaged = false;
        ClearMarker(); // Remove any existing marker

        // Visual indication
        if (tileRenderer != null)
        {
            tileRenderer.material.color = Color.black;
        }

        // Optional: Add cracked texture or particle effect
        // PlayCorruptionEffect();

        // Lower the tile slightly to indicate it's damaged
        transform.position = new Vector3(
            transform.position.x,
            -0.2f, // Lowered position
            transform.position.z);
    }

    public void AdvantageTile()
    {
        if (isBlackened) { return; }

        isAdvantaged = true;
        detonationCharges = maxCharges;
        ClearMarker();
        if (tileRenderer != null)
        {
            tileRenderer.material.color = chargeColors[detonationCharges-1];
        }

        transform.position = new Vector3(
    transform.position.x,
    -0.2f, // Lowered position
    transform.position.z);

        UpdateChargeVisuals();
        Debug.Log($"Green tile at ({x}, {y}) enhanced to charge level {detonationCharges}");

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
            if (isAdvantaged)
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
        // No special logic needed here - we'll handle specific cube types in TriggerMarker
        
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

        // Handle cube type-specific behavior
        switch (cubeToProcess.CubeType)
        {
            case Enumerations.CubeType.Black:
                // Black cube captured = immediate corruption
                BlackenTile();

                // The black cube remains (not destroyed)
                // Could add visual feedback or sound effect here
                break;

            case Enumerations.CubeType.Green:
                // Register with DetonationManager as before
                DetonationManager detonationManager = FindObjectOfType<DetonationManager>();
                if (detonationManager != null)
                {
                    detonationManager.RegisterDetonationPoint(new Vector2Int(x, y));
                }

                // Consume the green cube
                Destroy(cubeToProcess.gameObject);
                break;

            case Enumerations.CubeType.Normal:
                // Normal cubes are simply consumed
                Destroy(cubeToProcess.gameObject);
                break;
        }

        // Clear cube reference after processing
        currentCube = null;
    }

    public void HandleCubeLanding(CubeBehavior cube)
    {
        if (cube == null || currentState != Enumerations.TileState.Transformed)
            return;

        if (IsBlackened)
        {
            // Black tiles have no effect
            return;
        }

        if (IsAdvantaged)
        {
            if(cube.CubeType == Enumerations.CubeType.Black)
            {
               Debug.Log("Black cube landed on an advantaged tile. Charge Reduced.");
                ReduceCharge();
            }
        }
    }
}
using System;
using System.Collections;
using UnityEngine;
using static Enumerations;

public class Tile : MonoBehaviour
{
    [Header("Tile Properties")]
    [SerializeField] public int x, y;
    [SerializeField] private bool hasMarker = false;
    [SerializeField] private float markerHeight = 0.5f;
    [SerializeField] private float markerScale = 0.8f;

    private const float TRANSFORMED_HEIGHT = -0.25f;  // Lower by 0.25 for transformed tiles
    private const float MARKED_HEIGHT = 0.25f;        // Raise by 0.25 for marked tiles
    private const float NORMAL_HEIGHT = 0f;           // Normal baseline height

    [Header("Enhanced Blue Tile")]
    [SerializeField] private int detonationCharges = 0;
    [SerializeField] private int maxCharges = 3;

    [Header("Player Hover Effect")]
    [SerializeField] private bool isPlayerOnTile = false;
    [SerializeField] private GameObject softHighlightObject;
    [SerializeField] private Material playerHoverMaterial;

    [Header("Materials")]
    [SerializeField] private Material markedTileMaterial;
    [SerializeField] private Material forbiddenMaterial;
    [SerializeField] private Material activateMarkerMaterial;
    [SerializeField] private Material originalMaterial;
    [SerializeField] private Material[] chargeMaterials;

    // Properties to access charge information
    public int DetonationCharges => detonationCharges;
    public bool HasCharges => detonationCharges > 0;
    public bool IsBlackened => isBlackened;
    public bool IsAdvantaged => isAdvantaged;
    public bool HasMarker => hasMarker;
    public TileState currentState = TileState.Normal;

    public bool CanBeMarked => currentState == TileState.Normal && !isBlackened;

    private GameObject markerObj;
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
        if (tileRenderer != null)
        {
            originalMaterial = tileRenderer.material;
        }
    }

    public void Init(int xPos, int yPos)
    {
        x = xPos;
        y = yPos;
        isInitialized = true;

        // Initialize hover material
        if (playerHoverMaterial == null)
        {
            playerHoverMaterial = new Material(Shader.Find("Standard"));
            playerHoverMaterial.color = new Color(0.3f, 0.8f, 1f, 0.5f);
        }
    }

    private void OnDestroy()
    {
        if (markerObj != null)
        {
            Destroy(markerObj);
            markerObj = null;
        }

        if (softHighlightObject != null)
        {
            Destroy(softHighlightObject);
            softHighlightObject = null;
        }
    }

    public void PlaceMarker()
    {
        if (!isInitialized || hasMarker || !CanBeMarked) return;

        hasMarker = true;

        // Create marker object if it doesn't exist
        if (markerObj == null)
        {
            markerObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            markerObj.transform.SetParent(transform);
            markerObj.transform.localPosition = new Vector3(0, markerHeight, 0);
            markerObj.transform.localScale = new Vector3(markerScale, 0.3f, markerScale);
            markerObj.name = $"Marker_{x}_{y}";

            // Remove collider to avoid physics interference
            Collider markerCollider = markerObj.GetComponent<Collider>();
            if (markerCollider != null)
            {
                Destroy(markerCollider);
            }

            // Set marker color (bright red/orange for visibility)
            Renderer markerRenderer = markerObj.GetComponent<Renderer>();
            if (markerRenderer != null)
            {
                Material markerMaterial = new Material(Shader.Find("Standard"));
                markerMaterial.color = Color.red;
                markerMaterial.SetFloat("_Metallic", 0.2f);
                markerMaterial.SetFloat("_Smoothness", 0.8f);
                markerRenderer.material = markerMaterial;
            }
        }

        // Update tile appearance for marked state
        if (tileRenderer != null && markedTileMaterial != null)
        {
            tileRenderer.material = markedTileMaterial;
        }

        // Raise the tile slightly for marked state
        

        // Hide soft highlight when marked (marker takes precedence)
        if (softHighlightObject != null)
        {
            Destroy(softHighlightObject);
        }

        Debug.Log($"Marked tile at ({x}, {y})");
    }

    public void ClearMarker()
    {
        if (!hasMarker) return;

        Debug.Log($"Tile ({x},{y}): ClearMarker called");

        hasMarker = false;

        // Destroy marker object
        if (markerObj != null)
        {
            Destroy(markerObj);
            markerObj = null;
        }

        // Reset tile appearance
        if (tileRenderer != null)
        {
            if (IsBlackened)
            {
                tileRenderer.material = forbiddenMaterial;
            }
            else if (IsAdvantaged)
            {
                UpdateChargeVisuals();
            }
            else
            {
                tileRenderer.material = originalMaterial;
            }
        }

        // Reset tile height based on current state

        // Restore soft highlight if player is still on this tile
        if (isPlayerOnTile)
        {
            Debug.Log($"Tile ({x},{y}): After clearing marker, restoring soft highlight");
            ShowSoftHighlight();
        }

        Debug.Log($"Tile ({x},{y}): Marker cleared successfully");
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

    public void TriggerMarker()
    {
        if (!hasMarker) return;

        // Store reference to cube before changing marker state
        CubeBehavior cubeToProcess = currentCube;

        // Change visual state to "activated"
        ActivateMarker();

        // Start a coroutine to reset the material after a delay
        StartCoroutine(ResetMarkerAfterDelay(0.5f));

        if (cubeToProcess == null)
        {
            Debug.LogWarning($"No cube to process on marker trigger at ({x}, {y}).");
            return;
        }

        // Handle cube type-specific behavior
        switch (cubeToProcess.type)
        {
            case Enumerations.CubeType.Black:
                // Black cube captured = immediate corruption
                BlackenTile();
                NotifyPlayerCubeCapture(Enumerations.CubeType.Black);
                // The black cube remains (not destroyed)
                break;

            case Enumerations.CubeType.Blue:
                // Register with DetonationManager
                DetonationManager detonationManager = FindObjectOfType<DetonationManager>();
                if (detonationManager != null)
                {
                    detonationManager.RegisterDetonationPoint(new Vector2Int(x, y));
                }
                NotifyPlayerCubeCapture(Enumerations.CubeType.Blue);
                // Consume the blue cube
                Destroy(cubeToProcess.gameObject);
                break;

            case Enumerations.CubeType.Normal:
                NotifyPlayerCubeCapture(Enumerations.CubeType.Normal);
                // Normal cubes are simply consumed
                Destroy(cubeToProcess.gameObject);
                break;
        }

        // Clear cube reference after processing
        currentCube = null;
    }

    private void ActivateMarker()
    {
        Debug.Log($"Tile ({x},{y}): ActivateMarker called - hasMarker before: {hasMarker}");

        // Hide the marker object temporarily but don't change hasMarker state yet
        if (markerObj != null)
        {
            markerObj.SetActive(false);
            Debug.Log($"Tile ({x},{y}): Marker object hidden");
        }

        if (tileRenderer != null && activateMarkerMaterial != null)
        {
            tileRenderer.material = activateMarkerMaterial;
            Debug.Log($"Tile ({x},{y}): Applied activation material");
        }
    }

    private IEnumerator ResetMarkerAfterDelay(float delay)
    {
        Debug.Log($"Tile ({x},{y}): Starting marker reset delay of {delay} seconds");

        yield return new WaitForSeconds(delay);

        Debug.Log($"Tile ({x},{y}): Resetting marker after delay - isPlayerOnTile: {isPlayerOnTile}");

        // Now clear the marker state completely
        hasMarker = false;

        if (markerObj != null)
        {
            Destroy(markerObj);
            markerObj = null;
            Debug.Log($"Tile ({x},{y}): Marker object destroyed");
        }

        // Reset the tile appearance based on current state
        if (tileRenderer != null)
        {
            if (IsBlackened)
            {
                tileRenderer.material = forbiddenMaterial;
            }
            else if (IsAdvantaged)
            {
                UpdateChargeVisuals();
            }
            else
            {
                tileRenderer.material = originalMaterial;
            }
            Debug.Log($"Tile ({x},{y}): Tile material reset");
        }

        

        // IMPORTANT: Restore soft highlight if player is still on this tile
        if (isPlayerOnTile)
        {
            Debug.Log($"Tile ({x},{y}): Player still on tile, restoring soft highlight");
            ShowSoftHighlight();
        }
        else
        {
            Debug.Log($"Tile ({x},{y}): Player not on tile, no highlight needed");
        }
    }

    private void NotifyPlayerCubeCapture(Enumerations.CubeType cubeType)
    {
        PlayerManager player = FindObjectOfType<PlayerManager>();
        if (player != null)
        {
            player.OnCubeCaptured(cubeType);
        }
    }

    public void SetPlayerHover(bool isHovering)
    {
        if (isPlayerOnTile == isHovering) return; // No change needed

        Debug.Log($"Tile ({x},{y}): SetPlayerHover({isHovering}) - hasMarker={hasMarker}, isBlackened={isBlackened}");

        isPlayerOnTile = isHovering;

        // Only show highlight if not marked and not blackened
        if (isHovering && !hasMarker && !isBlackened)
        {
            ShowSoftHighlight();
        }
        else
        {
            HideSoftHighlight();
        }
    }

    private void ShowSoftHighlight()
    {
        if (isBlackened || hasMarker)
        {
            Debug.Log($"Tile ({x},{y}): Cannot show highlight - isBlackened={isBlackened}, hasMarker={hasMarker}");
            return; // Don't highlight blackened or marked tiles
        }

        Debug.Log($"Tile ({x},{y}): Showing soft highlight");

        if (softHighlightObject == null)
        {
            // Create soft highlight object
            softHighlightObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            softHighlightObject.transform.SetParent(transform);
            softHighlightObject.transform.localPosition = new Vector3(0, 0.51f, 0); // Just above the tile
            softHighlightObject.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
            softHighlightObject.name = $"SoftHighlight_{x}_{y}";

            // Remove collider
            Collider highlightCollider = softHighlightObject.GetComponent<Collider>();
            if (highlightCollider != null)
            {
                Destroy(highlightCollider);
            }

            // Apply hover material
            Renderer highlightRenderer = softHighlightObject.GetComponent<Renderer>();
            if (highlightRenderer != null && playerHoverMaterial != null)
            {
                highlightRenderer.material = playerHoverMaterial;
            }

            Debug.Log($"Tile ({x},{y}): Created new soft highlight object");
        }

        // Activate the highlight object
        if (softHighlightObject != null)
        {
            softHighlightObject.SetActive(true);
            Debug.Log($"Tile ({x},{y}): Activated soft highlight object");
        }
    }

    private void HideSoftHighlight()
    {
        Debug.Log($"Tile ({x},{y}): Hiding soft highlight");

        if (softHighlightObject != null)
        {
            softHighlightObject.SetActive(false);
        }
    }


    // Tile state management methods
    public void TransformTile(Enumerations.CubeType cubeType)
    {
        if (currentState != Enumerations.TileState.Transformed)
        {
            currentState = Enumerations.TileState.Transformed;

            switch (cubeType)
            {
                case Enumerations.CubeType.Black:
                    BlackenTile();
                    break;
                case Enumerations.CubeType.Blue:
                    AdvantageTile();
                    break;
            }
        }
    }

    public void BlackenTile()
    {
        isBlackened = true;
        isAdvantaged = false;
        ClearMarker(); // Remove any existing marker

        // Visual indication
        if (tileRenderer != null && forbiddenMaterial != null)
        {
            tileRenderer.material = forbiddenMaterial;
        }

        PlayerManager player = FindObjectOfType<PlayerManager>();
        if (player != null)
        {
            player.OnTileCorrupted();
        }

        // Hide any highlights
        HideSoftHighlight();

       
    }

    public void AdvantageTile(int charges = 3)
    {
        if (isBlackened) return;

        isAdvantaged = true;
        detonationCharges = charges > maxCharges ? maxCharges : charges;
        ClearMarker();

        PlayerManager player = FindObjectOfType<PlayerManager>();
        if (player != null)
        {
            player.OnTileEnhanced();
        }

        UpdateChargeVisuals();

        Debug.Log($"Blue tile at ({x}, {y}) enhanced to charge level {detonationCharges}");
    }

    private void UpdateChargeVisuals()
    {
        if (tileRenderer != null && detonationCharges > 0 &&
            chargeMaterials != null && detonationCharges <= chargeMaterials.Length)
        {
            tileRenderer.material = chargeMaterials[detonationCharges - 1];
        }
        else if (tileRenderer != null && detonationCharges == 0)
        {
            tileRenderer.material = originalMaterial;
        }
    }

    public void ReduceCharge()
    {
        if (detonationCharges <= 0)
        {
            ResetToNormalState();
            return;
        }

        detonationCharges = isAdvantaged ? detonationCharges - 1 : 0;

        if (detonationCharges > 0)
        {
            UpdateChargeVisuals();
        }
        else
        {
            ResetToNormalState();
        }
    }

    private void ResetToNormalState()
    {
        currentState = TileState.Normal;
        isAdvantaged = false;
        detonationCharges = 0;

        if (tileRenderer != null)
        {
            tileRenderer.material = originalMaterial;
        }

    }

    public void ResetTile()
    {
        currentState = TileState.Normal;
        isBlackened = false;
        isAdvantaged = false;
        detonationCharges = 0;
        ClearMarker();

        if (tileRenderer != null)
        {
            tileRenderer.material = originalMaterial;
        }


    }

    public void ProcessCubeInteraction(CubeBehavior cube)
    {
        if (cube != null)
        {
            currentCube = cube;
        }
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
            if (cube.type == Enumerations.CubeType.Black)
            {
                Debug.Log("Black cube landed on an advantaged tile. Charge Reduced.");
            }
            ReduceCharge();
        }
    }

    // Phase zone methods (keeping existing functionality)
    public void SetPhased(bool phased)
    {
        isPhasedZone = phased;

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
}
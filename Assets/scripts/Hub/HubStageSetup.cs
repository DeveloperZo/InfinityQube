using UnityEngine;
using System.Collections;

/// <summary>
/// Sets up the Hub stage at runtime - creates interactive cubes for menus.
/// Subscribes to GameEvents.OnStageStart to setup/cleanup based on stage type.
/// Waits for grid to be ready before placing cubes.
/// </summary>
public class HubStageSetup : MonoBehaviour
{
    #region Inspector Configuration
    
    [Header("Cube Settings")]
    [SerializeField] private GameObject interactableCubePrefab;
    [SerializeField] private float cubeScale = 0.8f;
    [SerializeField] private float cubeYOffset = 0.5f;
    
    [Header("Colors")]
    [SerializeField] private Color stageSelectColor = new Color(0.2f, 0.5f, 0.9f);
    [SerializeField] private Color attunementColor = new Color(0.7f, 0.3f, 0.8f);
    [SerializeField] private Color statsColor = new Color(0.3f, 0.8f, 0.4f);
    [SerializeField] private Color exitColor = new Color(0.9f, 0.3f, 0.3f);
    
    [Header("Positions (Grid Coordinates)")]
    [SerializeField] private Vector2Int stageSelectPos = new Vector2Int(2, 4);
    [SerializeField] private Vector2Int attunementPos = new Vector2Int(1, 6);
    [SerializeField] private Vector2Int statsPos = new Vector2Int(3, 6);
    [SerializeField] private Vector2Int exitPos = new Vector2Int(2, 8);
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region Runtime State
    
    private GameObject hubCubesParent;
    private Coroutine setupCoroutine;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void OnEnable()
    {
        // Subscribe to stage events
        GameEvents.OnStageStart += HandleStageStart;
        DebugLog("Subscribed to GameEvents.OnStageStart");
    }
    
    private void OnDisable()
    {
        // Unsubscribe from stage events
        GameEvents.OnStageStart -= HandleStageStart;
        
        // Stop any pending setup
        if (setupCoroutine != null)
        {
            StopCoroutine(setupCoroutine);
            setupCoroutine = null;
        }
        
        DebugLog("Unsubscribed from GameEvents.OnStageStart");
    }
    
    #endregion
    
    #region Event Handlers
    
    private void HandleStageStart(int stageIndex, StageData stageData)
    {
        DebugLog($"HandleStageStart: Stage {stageIndex} - {stageData?.stageName}, Type: {stageData?.stageType}");
        
        // Stop any pending setup
        if (setupCoroutine != null)
        {
            StopCoroutine(setupCoroutine);
            setupCoroutine = null;
        }
        
        if (stageData != null && stageData.stageType == Enumerations.StageType.Hub)
        {
            // Hub stage - wait for grid then setup cubes
            setupCoroutine = StartCoroutine(WaitForGridAndSetup());
        }
        else
        {
            // Non-Hub stage - cleanup cubes immediately
            CleanupHubCubes();
        }
    }
    
    private IEnumerator WaitForGridAndSetup()
    {
        DebugLog("Waiting for grid to be fully generated...");
        
        // Wait for end of frame to let all OnStageStart handlers run
        yield return new WaitForEndOfFrame();
        
        // Wait additional time for grid generation to complete
        // Grid generation is triggered by WaveManager.HandleStageStart which runs after our handler
        yield return new WaitForSeconds(0.5f);
        
        GridManager gridManager = GridManager.Instance;
        if (gridManager == null)
        {
            Debug.LogError("[HubStageSetup] GridManager not found!");
            yield break;
        }
        
        // Verify tiles exist
        if (gridManager.tiles == null || gridManager.GetTileAt(stageSelectPos.x, stageSelectPos.y) == null)
        {
            Debug.LogError("[HubStageSetup] Grid tiles not ready!");
            yield break;
        }
        
        DebugLog("Grid ready - creating cubes");
        SetupHubCubes(gridManager);
        setupCoroutine = null;
    }
    
    #endregion
    
    #region Setup
    
    private void CleanupHubCubes()
    {
        if (hubCubesParent != null)
        {
            DebugLog("Cleaning up Hub cubes");
            Destroy(hubCubesParent);
            hubCubesParent = null;
        }
    }
    
    private void SetupHubCubes(GridManager gridManager)
    {
        // Cleanup first to avoid duplicates
        CleanupHubCubes();
        
        DebugLog("Setting up Hub stage interactive cubes...");
        
        // Create parent for organization
        hubCubesParent = new GameObject("HubInteractables");
        
        // Create interactive cubes
        CreateInteractableCube("StageSelect", HubInteractionType.StageSelect, stageSelectPos, stageSelectColor, "Celestial Atlas", gridManager);
        CreateInteractableCube("Attunement", HubInteractionType.Attunement, attunementPos, attunementColor, "Resonance Chamber", gridManager);
        CreateInteractableCube("Stats", HubInteractionType.Stats, statsPos, statsColor, "Chronicle", gridManager);
        CreateInteractableCube("Exit", HubInteractionType.Exit, exitPos, exitColor, "Return", gridManager);
        
        DebugLog("Hub stage setup complete - 4 interactive cubes created");
    }
    
    private void CreateInteractableCube(string name, HubInteractionType type, Vector2Int gridPos, Color color, string displayName, GridManager gridManager)
    {
        // Get world position from the actual tile object (more reliable than GridToWorldPosition)
        Vector3 worldPos;
        Tile tile = gridManager.GetTileAt(gridPos.x, gridPos.y);
        if (tile != null)
        {
            worldPos = tile.transform.position + new Vector3(0, cubeYOffset, 0);
        }
        else
        {
            // Fallback to calculation if tile not found
            worldPos = gridManager.GridToWorldPosition(gridPos.x, gridPos.y, cubeYOffset);
            Debug.LogWarning($"[HubStageSetup] Tile at ({gridPos.x}, {gridPos.y}) not found, using calculated position");
        }
        
        // Create cube GameObject
        GameObject cube;
        if (interactableCubePrefab != null)
        {
            cube = Instantiate(interactableCubePrefab, worldPos, Quaternion.identity, hubCubesParent.transform);
        }
        else
        {
            // Create primitive cube if no prefab assigned
            cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = worldPos;
            cube.transform.parent = hubCubesParent.transform;
        }
        
        cube.name = $"Hub_{name}";
        cube.transform.localScale = Vector3.one * cubeScale;
        
        // Set color
        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            // Make it slightly emissive
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 0.3f);
            renderer.material = mat;
        }
        
        // Add and configure HubInteractable
        HubInteractable interactable = cube.AddComponent<HubInteractable>();
        ConfigureInteractable(interactable, type, displayName, color);
        
        // Remove default collider and add trigger
        Collider oldCollider = cube.GetComponent<Collider>();
        if (oldCollider != null)
        {
            Destroy(oldCollider);
        }
        BoxCollider collider = cube.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        
        DebugLog($"Created {name} cube at grid ({gridPos.x}, {gridPos.y}) -> world {worldPos}");
    }
    
    private void ConfigureInteractable(HubInteractable interactable, HubInteractionType type, string displayName, Color color)
    {
        // Use reflection to set serialized fields since they're private
        var typeField = typeof(HubInteractable).GetField("interactionType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var nameField = typeof(HubInteractable).GetField("displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var normalColorField = typeof(HubInteractable).GetField("normalColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var highlightColorField = typeof(HubInteractable).GetField("highlightColor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (typeField != null) typeField.SetValue(interactable, type);
        if (nameField != null) nameField.SetValue(interactable, displayName);
        if (normalColorField != null) normalColorField.SetValue(interactable, color);
        if (highlightColorField != null) highlightColorField.SetValue(interactable, Color.Lerp(color, Color.white, 0.4f));
    }
    
    #endregion
    
    #region Debug
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[HubStageSetup] {message}");
        }
    }
    
    #endregion
}

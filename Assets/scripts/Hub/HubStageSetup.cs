using UnityEngine;
using System.Collections;

/// <summary>
/// Sets up the Hub stage at runtime - creates interactive cubes for menus.
/// Attach to a persistent object in the Stage scene.
/// Only activates when HubStage (stageType = Hub) is loaded.
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
    [SerializeField] private Vector2Int stageSelectPos = new Vector2Int(2, 6);
    [SerializeField] private Vector2Int attunementPos = new Vector2Int(1, 10);
    [SerializeField] private Vector2Int statsPos = new Vector2Int(3, 10);
    [SerializeField] private Vector2Int exitPos = new Vector2Int(2, 15);
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region Runtime State
    
    private bool hasSetup = false;
    private GameObject hubCubesParent;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Start()
    {
        StartCoroutine(WaitAndSetup());
    }
    
    private IEnumerator WaitAndSetup()
    {
        // Wait for StageManager to initialize and load stage
        yield return new WaitForSeconds(0.5f);
        
        // Check if this is a Hub stage
        var stageManager = FindFirstObjectByType<StageManager>();
        if (stageManager != null && stageManager.IsHubStage)
        {
            SetupHubCubes();
        }
    }
    
    #endregion
    
    #region Setup
    
    private void SetupHubCubes()
    {
        if (hasSetup) return;
        hasSetup = true;
        
        DebugLog("Setting up Hub stage interactive cubes...");
        
        // Create parent for organization
        hubCubesParent = new GameObject("HubInteractables");
        
        // Get GridManager for world position conversion
        GridManager gridManager = FindFirstObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("[HubStageSetup] GridManager not found!");
            return;
        }
        
        // Create interactive cubes
        CreateInteractableCube("StageSelect", HubInteractionType.StageSelect, stageSelectPos, stageSelectColor, "Celestial Atlas", gridManager);
        CreateInteractableCube("Attunement", HubInteractionType.Attunement, attunementPos, attunementColor, "Resonance Chamber", gridManager);
        CreateInteractableCube("Stats", HubInteractionType.Stats, statsPos, statsColor, "Chronicle", gridManager);
        CreateInteractableCube("Exit", HubInteractionType.Exit, exitPos, exitColor, "Return", gridManager);
        
        DebugLog("Hub stage setup complete - 4 interactive cubes created");
    }
    
    private void CreateInteractableCube(string name, HubInteractionType type, Vector2Int gridPos, Color color, string displayName, GridManager gridManager)
    {
        // Calculate world position from grid position
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos.x, gridPos.y, cubeYOffset);
        
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

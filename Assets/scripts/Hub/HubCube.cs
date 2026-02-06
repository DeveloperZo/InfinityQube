using UnityEngine;

/// <summary>
/// Interactive cube in the Hub grid.
/// Triggers actions when player interacts (collision or key press).
/// </summary>
public class HubCube : MonoBehaviour
{
    #region Inspector Configuration
    
    [Header("Cube Identity")]
    [SerializeField] private HubCubeType cubeType;
    [SerializeField] private string cubeName;
    [SerializeField, TextArea] private string cubeDescription;
    
    [Header("Behavior")]
    [SerializeField] private bool blocksMovement = true;
    [SerializeField] private bool interactOnEnter = false;
    [SerializeField] private bool requiresUnlock = false;
    
    [Header("Visual Settings")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.7f);
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.3f);
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseAmount = 0.1f;
    
    [Header("Label")]
    [SerializeField] private bool showLabel = true;
    [SerializeField] private Vector3 labelOffset = new Vector3(0, 1.5f, 0);
    
    #endregion
    
    #region Runtime State
    
    private Vector2Int gridPosition;
    private Renderer cubeRenderer;
    private Material cubeMaterial;
    private bool isPlayerAdjacent = false;
    private bool isLocked = false;
    private TextMesh labelMesh;
    private float pulseTimer = 0f;
    
    #endregion
    
    #region Properties
    
    public HubCubeType CubeType => cubeType;
    public string CubeName => string.IsNullOrEmpty(cubeName) ? cubeType.ToString() : cubeName;
    public bool BlocksMovement => blocksMovement;
    public bool InteractOnEnter => interactOnEnter && !isLocked;
    public bool IsLocked => isLocked;
    public Vector2Int GridPosition => gridPosition;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        cubeRenderer = GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            // Create instance of material to avoid modifying shared material
            cubeMaterial = new Material(cubeRenderer.material);
            cubeRenderer.material = cubeMaterial;
        }
        
        if (string.IsNullOrEmpty(cubeName))
        {
            cubeName = cubeType.ToString();
        }
    }
    
    private void Start()
    {
        CheckUnlockState();
        UpdateVisuals();
        CreateLabel();
    }
    
    private void Update()
    {
        UpdatePulse();
        CheckPlayerProximity();
    }
    
    private void OnDestroy()
    {
        if (cubeMaterial != null)
        {
            Destroy(cubeMaterial);
        }
        
        if (labelMesh != null && labelMesh.gameObject != null)
        {
            Destroy(labelMesh.gameObject);
        }
    }
    
    #endregion
    
    #region Initialization
    
    public void SetGridPosition(Vector2Int position)
    {
        gridPosition = position;
    }
    
    private void CreateLabel()
    {
        if (!showLabel) return;
        
        // Create floating label above cube
        GameObject labelObj = new GameObject($"{CubeName}_Label");
        labelObj.transform.SetParent(transform);
        labelObj.transform.localPosition = labelOffset;
        
        labelMesh = labelObj.AddComponent<TextMesh>();
        labelMesh.text = CubeName;
        labelMesh.fontSize = 24;
        labelMesh.characterSize = 0.1f;
        labelMesh.anchor = TextAnchor.MiddleCenter;
        labelMesh.alignment = TextAlignment.Center;
        labelMesh.color = isLocked ? lockedColor : Color.white;
        
        // Billboard effect - face camera
        labelObj.AddComponent<BillboardLabel>();
    }
    
    private void CheckUnlockState()
    {
        if (!requiresUnlock)
        {
            isLocked = false;
            return;
        }
        
        // Check unlock state based on cube type
        if (!SaveManager.IsInitialized)
        {
            isLocked = false;
            return;
        }
        
        switch (cubeType)
        {
            case HubCubeType.StageSelect:
                isLocked = false; // Always unlocked
                break;
                
            case HubCubeType.Attunement:
                isLocked = !SaveManager.Instance.Progression.resonanceAlignmentUnlocked;
                break;
                
            case HubCubeType.Stats:
                isLocked = !SaveManager.Instance.Progression.observationChronicleUnlocked;
                break;
                
            default:
                isLocked = false;
                break;
        }
    }
    
    #endregion
    
    #region Interaction
    
    /// <summary>
    /// Called when player interacts with this cube.
    /// </summary>
    public void Interact()
    {
        if (isLocked)
        {
            Debug.Log($"[HubCube] {CubeName} is locked");
            ShowLockedFeedback();
            return;
        }
        
        Debug.Log($"[HubCube] Interacting with {CubeName}");
        
        switch (cubeType)
        {
            case HubCubeType.StageSelect:
                HubUIManager.Instance?.OpenStageSelection();
                break;
                
            case HubCubeType.Attunement:
                HubUIManager.Instance?.OpenAttunements();
                break;
                
            case HubCubeType.Stats:
                HubUIManager.Instance?.OpenStats();
                break;
                
            case HubCubeType.Exit:
                OnExitInteract();
                break;
                
            case HubCubeType.Custom:
                // Override in subclass or use UnityEvent
                Debug.Log($"[HubCube] Custom cube '{CubeName}' - no action defined");
                break;
        }
    }
    
    private void OnExitInteract()
    {
        // Could return to main menu, quit, etc.
        Debug.Log("[HubCube] Exit interaction - implement as needed");
    }
    
    private void ShowLockedFeedback()
    {
        // Flash red or shake
        if (cubeMaterial != null)
        {
            StartCoroutine(FlashColor(Color.red, 0.3f));
        }
    }
    
    private System.Collections.IEnumerator FlashColor(Color flashColor, float duration)
    {
        Color original = cubeMaterial.color;
        cubeMaterial.color = flashColor;
        yield return new WaitForSeconds(duration);
        cubeMaterial.color = original;
    }
    
    #endregion
    
    #region Visual Updates
    
    private void UpdateVisuals()
    {
        if (cubeMaterial == null) return;
        
        Color targetColor;
        
        if (isLocked)
        {
            targetColor = lockedColor;
        }
        else if (isPlayerAdjacent)
        {
            targetColor = hoverColor;
        }
        else
        {
            targetColor = normalColor;
        }
        
        cubeMaterial.color = targetColor;
    }
    
    private void UpdatePulse()
    {
        if (!isPlayerAdjacent || isLocked) return;
        
        pulseTimer += Time.deltaTime * pulseSpeed;
        float pulse = 1f + Mathf.Sin(pulseTimer) * pulseAmount;
        transform.localScale = Vector3.one * pulse;
    }
    
    private void CheckPlayerProximity()
    {
        if (HubGridController.Instance == null) return;
        
        Vector2Int playerPos = HubGridController.Instance.PlayerPosition;
        
        // Check if player is adjacent (including diagonal would be: Manhattan distance <= 1.5)
        int distance = Mathf.Abs(playerPos.x - gridPosition.x) + Mathf.Abs(playerPos.y - gridPosition.y);
        bool wasAdjacent = isPlayerAdjacent;
        isPlayerAdjacent = distance <= 1;
        
        if (wasAdjacent != isPlayerAdjacent)
        {
            UpdateVisuals();
            
            // Reset scale when no longer adjacent
            if (!isPlayerAdjacent)
            {
                transform.localScale = Vector3.one;
                pulseTimer = 0f;
            }
        }
    }
    
    /// <summary>
    /// Refresh locked state (call when progression changes).
    /// </summary>
    public void RefreshLockedState()
    {
        CheckUnlockState();
        UpdateVisuals();
        
        if (labelMesh != null)
        {
            labelMesh.color = isLocked ? lockedColor : Color.white;
            labelMesh.text = isLocked ? $"{CubeName} (Locked)" : CubeName;
        }
    }
    
    #endregion
    
    #region Editor
    
    private void OnDrawGizmos()
    {
        // Draw cube type indicator
        Gizmos.color = GetGizmoColor();
        Gizmos.DrawWireCube(transform.position, Vector3.one * 1.1f);
        
        // Draw label position
        if (showLabel)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + labelOffset, 0.1f);
        }
    }
    
    private Color GetGizmoColor()
    {
        return cubeType switch
        {
            HubCubeType.StageSelect => Color.green,
            HubCubeType.Attunement => Color.magenta,
            HubCubeType.Stats => Color.cyan,
            HubCubeType.Exit => Color.red,
            _ => Color.white
        };
    }
    
    #endregion
}

/// <summary>
/// Types of hub cubes.
/// </summary>
public enum HubCubeType
{
    StageSelect,    // Opens stage selection panel
    Attunement,     // Opens attunement panel
    Stats,          // Opens stats panel
    Exit,           // Exit/quit action
    Custom          // Custom behavior
}

/// <summary>
/// Simple billboard component to face camera.
/// </summary>
public class BillboardLabel : MonoBehaviour
{
    private void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                            Camera.main.transform.rotation * Vector3.up);
        }
    }
}

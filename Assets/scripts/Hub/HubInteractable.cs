using UnityEngine;

/// <summary>
/// Attach to cubes in the Hub stage to make them interactive.
/// Detects when player is adjacent and allows interaction via key press.
/// Works with existing PlayerManager movement system.
/// </summary>
public class HubInteractable : MonoBehaviour
{
    #region Inspector Configuration
    
    [Header("Interaction Type")]
    [SerializeField] private HubInteractionType interactionType;
    [SerializeField] private string displayName;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.6f);
    [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float pulseIntensity = 0.15f;
    
    [Header("Unlock Requirements")]
    [SerializeField] private bool requiresUnlock = false;
    [SerializeField] private int requiredStageCleared = 0;
    
    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.F;
    [SerializeField] private float interactionRange = 1.5f;
    
    #endregion
    
    #region Runtime State
    
    private Renderer cubeRenderer;
    private Material cubeMaterial;
    private bool isPlayerInRange = false;
    private bool isLocked = false;
    private Transform playerTransform;
    private float pulseTimer = 0f;
    private Vector3 originalScale;
    
    #endregion
    
    #region Properties
    
    public string DisplayName => string.IsNullOrEmpty(displayName) ? interactionType.ToString() : displayName;
    public bool IsLocked => isLocked;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        cubeRenderer = GetComponent<Renderer>();
        originalScale = transform.localScale;
        
        if (cubeRenderer != null)
        {
            cubeMaterial = new Material(cubeRenderer.material);
            cubeRenderer.material = cubeMaterial;
        }
    }
    
    private void Start()
    {
        CheckUnlockState();
        UpdateVisuals();
        FindPlayer();
    }
    
    private void Update()
    {
        CheckPlayerProximity();
        HandleInput();
        UpdatePulseEffect();
    }
    
    private void OnDestroy()
    {
        if (cubeMaterial != null)
        {
            Destroy(cubeMaterial);
        }
    }
    
    #endregion
    
    #region Player Detection
    
    private void FindPlayer()
    {
        // Find PlayerManager in scene
        var playerManager = FindFirstObjectByType<PlayerManager>();
        if (playerManager != null)
        {
            playerTransform = playerManager.transform;
        }
    }
    
    private void CheckPlayerProximity()
    {
        if (playerTransform == null)
        {
            FindPlayer();
            return;
        }
        
        float distance = Vector3.Distance(transform.position, playerTransform.position);
        bool wasInRange = isPlayerInRange;
        isPlayerInRange = distance <= interactionRange;
        
        // Update visuals when range state changes
        if (wasInRange != isPlayerInRange)
        {
            UpdateVisuals();
            
            if (isPlayerInRange && !isLocked)
            {
                ShowInteractionHint();
            }
        }
    }
    
    #endregion
    
    #region Input Handling
    
    private void HandleInput()
    {
        if (!isPlayerInRange || isLocked) return;
        
        if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.Space))
        {
            Interact();
        }
    }
    
    private void Interact()
    {
        Debug.Log($"[HubInteractable] Interacting with: {DisplayName}");
        
        switch (interactionType)
        {
            case HubInteractionType.StageSelect:
                OpenStageSelection();
                break;
                
            case HubInteractionType.Attunement:
                OpenAttunements();
                break;
                
            case HubInteractionType.Stats:
                OpenStats();
                break;
                
            case HubInteractionType.Exit:
                HandleExit();
                break;
        }
    }
    
    #endregion
    
    #region Menu Actions
    
    private void OpenStageSelection()
    {
        if (HubUIManager.Instance != null)
        {
            HubUIManager.Instance.OpenStageSelection();
        }
        else
        {
            Debug.Log("[HubInteractable] Opening Stage Selection (HubUIManager not found)");
            // Fallback: Could show a simple stage selection UI
        }
    }
    
    private void OpenAttunements()
    {
        if (HubUIManager.Instance != null)
        {
            HubUIManager.Instance.OpenAttunements();
        }
        else
        {
            Debug.Log("[HubInteractable] Opening Attunements (HubUIManager not found)");
        }
    }
    
    private void OpenStats()
    {
        if (HubUIManager.Instance != null)
        {
            HubUIManager.Instance.OpenStats();
        }
        else
        {
            Debug.Log("[HubInteractable] Opening Stats (HubUIManager not found)");
        }
    }
    
    private void HandleExit()
    {
        Debug.Log("[HubInteractable] Exit requested");
        // Could load main menu or quit
    }
    
    #endregion
    
    #region Visual Feedback
    
    private void CheckUnlockState()
    {
        if (!requiresUnlock)
        {
            isLocked = false;
            return;
        }
        
        if (SaveManager.IsInitialized)
        {
            isLocked = SaveManager.Instance.Progression.highestStageUnlocked < requiredStageCleared;
        }
        else
        {
            isLocked = requiredStageCleared > 0;
        }
    }
    
    private void UpdateVisuals()
    {
        if (cubeMaterial == null) return;
        
        Color targetColor;
        
        if (isLocked)
        {
            targetColor = lockedColor;
        }
        else if (isPlayerInRange)
        {
            targetColor = highlightColor;
        }
        else
        {
            targetColor = normalColor;
        }
        
        cubeMaterial.color = targetColor;
    }
    
    private void UpdatePulseEffect()
    {
        if (!isPlayerInRange || isLocked)
        {
            transform.localScale = originalScale;
            return;
        }
        
        pulseTimer += Time.deltaTime * pulseSpeed;
        float pulse = 1f + Mathf.Sin(pulseTimer) * pulseIntensity;
        transform.localScale = originalScale * pulse;
    }
    
    private void ShowInteractionHint()
    {
        Debug.Log($"[HubInteractable] Press {interactKey} to interact with {DisplayName}");
        // Could show UI hint here
    }
    
    #endregion
    
    #region Editor
    
    private void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
    
    #endregion
}

/// <summary>
/// Types of hub interactions.
/// </summary>
public enum HubInteractionType
{
    StageSelect,
    Attunement,
    Stats,
    Exit
}

using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Attached to each hub building cube. Handles click detection and visual feedback.
/// Requires a Collider component for raycasting.
/// </summary>
public class HubBuilding : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    #region Inspector Configuration
    
    [Header("Building Configuration")]
    [SerializeField] private HubBuildingType buildingType;
    [SerializeField] private string buildingName;
    [SerializeField, TextArea] private string buildingDescription;
    
    [Header("Visual Feedback")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = new Color(1f, 1f, 0.7f);
    [SerializeField] private Color lockedColor = new Color(0.4f, 0.4f, 0.4f);
    [SerializeField] private float hoverScale = 1.1f;
    
    #endregion
    
    #region Runtime State
    
    private Renderer buildingRenderer;
    private Vector3 originalScale;
    private bool isHovered = false;
    private bool isLocked = false;
    
    #endregion
    
    #region Properties
    
    public HubBuildingType BuildingType => buildingType;
    public string BuildingName => buildingName;
    public bool IsLocked => isLocked;
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        buildingRenderer = GetComponent<Renderer>();
        originalScale = transform.localScale;
        
        // Set default name if not specified
        if (string.IsNullOrEmpty(buildingName))
        {
            buildingName = buildingType.ToString();
        }
    }
    
    private void Start()
    {
        // Check if this building should be locked based on progression
        UpdateLockedState();
        UpdateVisuals();
    }
    
    #endregion
    
    #region Click Handling
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (isLocked)
        {
            Debug.Log($"[HubBuilding] {buildingName} is locked");
            // Could show "locked" feedback here
            return;
        }
        
        Debug.Log($"[HubBuilding] Clicked: {buildingName}");
        HubUIManager.Instance?.OpenPanel(buildingType);
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        UpdateVisuals();
        
        // Show tooltip or highlight
        Debug.Log($"[HubBuilding] Hover: {buildingName}");
    }
    
    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        UpdateVisuals();
    }
    
    #endregion
    
    #region Visual Updates
    
    private void UpdateLockedState()
    {
        if (!SaveManager.IsInitialized) 
        {
            isLocked = false;
            return;
        }
        
        // Celestial Atlas is always unlocked
        // Resonance Alignment Chamber and Observation Chronicle unlock after Stage 3
        switch (buildingType)
        {
            case HubBuildingType.CelestialAtlas:
                isLocked = false;
                break;
                
            case HubBuildingType.ResonanceAlignmentChamber:
                isLocked = !SaveManager.Instance.Progression.resonanceAlignmentUnlocked;
                break;
                
            case HubBuildingType.ObservationChronicle:
                isLocked = !SaveManager.Instance.Progression.observationChronicleUnlocked;
                break;
        }
    }
    
    private void UpdateVisuals()
    {
        if (buildingRenderer == null) return;
        
        // Update color
        Color targetColor = isLocked ? lockedColor : (isHovered ? hoverColor : normalColor);
        buildingRenderer.material.color = targetColor;
        
        // Update scale (hover effect)
        Vector3 targetScale = isHovered && !isLocked ? originalScale * hoverScale : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 10f);
    }
    
    private void Update()
    {
        // Smooth scale transition
        if (isHovered && !isLocked)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale * hoverScale, Time.deltaTime * 10f);
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * 10f);
        }
    }
    
    /// <summary>
    /// Refresh locked state (call when progression changes).
    /// </summary>
    public void RefreshLockedState()
    {
        UpdateLockedState();
        UpdateVisuals();
    }
    
    #endregion
}

/// <summary>
/// Types of buildings in the hub.
/// </summary>
public enum HubBuildingType
{
    CelestialAtlas,           // Stage selection
    ResonanceAlignmentChamber, // Attunements
    ObservationChronicle       // Stats/history
}


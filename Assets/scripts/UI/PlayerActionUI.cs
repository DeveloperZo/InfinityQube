using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static Enumerations;

public class PlayerActionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image[] unitMarkerSegments = new Image[6];
    [SerializeField] private Image[] recursionMarkerSegments = new Image[6];
    [SerializeField] private Image[] matrixMarkerSegments = new Image[6];
    [SerializeField] private Image[] infinityMarkerSegments = new Image[6];
    [SerializeField] private GameObject UnitMarkerUI;
    [SerializeField] private GameObject MatrixMarkerUI;
    [SerializeField] private GameObject RecursionMarkerUI;
    [SerializeField] private GameObject InfinityMarkerUI;
    [SerializeField] private Image[] modeIndicatorSegments = new Image[4];
    [SerializeField] private TextMeshProUGUI unitChargeText;
    [SerializeField] private TextMeshProUGUI recursionChargeText;
    [SerializeField] private TextMeshProUGUI matrixChargeText;
    [SerializeField] private TextMeshProUGUI infinityChargeText;

    [Header("UI Colors")]
    [SerializeField] private Color segmentFullColor = new Color(1f, 0.7f, 0.2f, 1f);     // Orange when charged
    [SerializeField] private Color segmentChargingColor = new Color(0.5f, 0.5f, 1f, 0.8f); // Blue when charging
    [SerializeField] private Color segmentFirstChargeColor = new Color(0.7f, 0.7f, 1f, 0.6f); // Light blue when charging toward first charge
    [SerializeField] private Color segmentEmptyColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);  // Gray when empty
    [SerializeField] private Color unitMarkerColor = new Color(1f, 0.5f, 0.2f, 1f);       // Orange for unit markers
    [SerializeField] private Color recursionMarkerColor = new Color(0.6f, 0.2f, 0.6f, 1f); // Purple for recursion markers
    [SerializeField] private Color matrixMarkerColor = new Color(0.2f, 0.5f, 0.8f, 1f);    // Blue for matrix markers
    [SerializeField] private Color infinityMarkerColor = new Color(0.3f, 0.8f, 0.9f, 1f); // Cyan for infinity markers
    [SerializeField] private Color[] modeColors = new Color[4] 
    {
        new Color(1f, 0.5f, 0.2f, 1f),      // Orange for Unit mode (index 0 = Unit=1)
        new Color(0.2f, 0.5f, 0.8f, 1f),    // Blue for Matrix mode (index 1 = Matrix=2)
        new Color(0.6f, 0.2f, 0.6f, 1f),    // Purple for Recursion mode (index 2 = Recursion=3)
        new Color(0.3f, 0.8f, 0.9f, 1f)     // Cyan for Infinity mode (index 3 = Infinity=4)
    };

    [Header("Recharge Settings")]
    [SerializeField] private PlayerActionManager playerActionManager;
    [SerializeField] private AnimationTriggerManager animationTriggerManager;
    [Tooltip("Unit marker uses move-based recharge (0-1 progress fraction)")]
    public float unitMarkerRechargeProgress = 0f; // Move-based: fraction of progress toward next charge
    // Non-Unit markers use inventory grants only - no cooldown regeneration
    public float recursionMarkerCooldownTime = 0f;
    public float matrixMarkerCooldownTime = 0f;
    public float infinityMarkerCooldownTime = 0f;

    // Cached references
    public int unitCharges;
    public int unitMaxCharges;
    public int recursionCharges;
    public int recursionMaxCharges;
    public int matrixCharges;
    public int matrixMaxCharges;
    public int infinityCharges;
    public int infinityMaxCharges;

    void Start()
    {
        playerActionManager = FindFirstObjectByType<PlayerActionManager>();
        animationTriggerManager = FindFirstObjectByType<AnimationTriggerManager>();

        if (playerActionManager != null)
        {
            unitMaxCharges = playerActionManager.maxUnitMarkerCharges;
            recursionMaxCharges = playerActionManager.maxRecursionMarkerCharges;
            matrixMaxCharges = playerActionManager.maxMatrixMarkerCharges;
            infinityMaxCharges = playerActionManager.maxInfinityMarkerCharges;
            // Unit markers use move-based recharge, non-Unit use inventory grants only
            unitMarkerRechargeProgress = 0f;
        }

        UpdateDisplay();
    }

    private void Update()
    {
        UpdateDisplay();
    }

    public void UpdateCharges(int currentUnitCharges, int currentRecursionCharges, int currentMatrixCharges, int currentInfinityCharges)
    {
        // Check if charges have changed to trigger animations
        bool chargesChanged = (unitCharges != currentUnitCharges) || 
                             (recursionCharges != currentRecursionCharges) || 
                             (matrixCharges != currentMatrixCharges) ||
                             (infinityCharges != currentInfinityCharges);

        unitCharges = currentUnitCharges;
        recursionCharges = currentRecursionCharges;
        matrixCharges = currentMatrixCharges;
        infinityCharges = currentInfinityCharges;

        // Set max charges if not already set
        if (playerActionManager != null)
        {
            if (unitMaxCharges == 0)
            {
                unitMaxCharges = playerActionManager.maxUnitMarkerCharges;
            }
            if (recursionMaxCharges == 0)
            {
                recursionMaxCharges = playerActionManager.maxRecursionMarkerCharges;
            }
            if (matrixMaxCharges == 0)
            {
                matrixMaxCharges = playerActionManager.maxMatrixMarkerCharges;
            }
            if (infinityMaxCharges == 0)
            {
                infinityMaxCharges = playerActionManager.maxInfinityMarkerCharges;
            }
        }

        // Trigger animation for UI update if charges changed
        if (chargesChanged)
        {
            TriggerUIUpdateAnimation();
        }
    }

    public void UpdateCooldowns(float unitRechargeProgress, float recursionCooldown, float matrixCooldown, float infinityCooldown)
    {
        // Unit marker now uses move-based recharge (progress is 0-1 fraction)
        unitMarkerRechargeProgress = unitRechargeProgress;
        recursionMarkerCooldownTime = recursionCooldown;
        matrixMarkerCooldownTime = matrixCooldown;
        infinityMarkerCooldownTime = infinityCooldown;
    }

    public void OnMarkerPlaced(bool isUnitMarker)
    {
        Debug.Log($"Marker placed - {(isUnitMarker ? "Unit" : "Area")}");
    }

    /// <summary>
    /// Shows action feedback to the player
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="isError">Whether this is an error message or success message</param>
    public void ShowActionFeedback(string message, bool isError)
    {
        // For now, use Debug.Log to show the message
        // In a full implementation, this could show a temporary UI popup or notification
        if (isError)
        {
            Debug.LogWarning($"[Action Error] {message}");
        }
        else
        {
            Debug.Log($"[Action Success] {message}");
        }
        
        // TODO: Implement visual feedback UI elements
        // This could include:
        // - Temporary text popup near the UI
        // - Color-coded feedback indicators
        // - Flashing charge indicators for relevant marker type
        // - Screen shake or other visual effects for errors
    }

    private void UpdateDisplay()
    {
        if (playerActionManager == null) return;

        // Get current charges directly from PlayerActionManager for real-time display
        int currentUnitCharges = playerActionManager.GetCurrentUnitCharges();
        int currentRecursionCharges = playerActionManager.GetCurrentRecursionCharges();
        int currentMatrixCharges = playerActionManager.GetCurrentMatrixCharges();
        int currentInfinityCharges = playerActionManager.GetCurrentInfinityCharges();

        // Get max charges from PlayerActionManager (may be updated from wave configuration)
        unitMaxCharges = playerActionManager.maxUnitMarkerCharges;
        recursionMaxCharges = playerActionManager.maxRecursionMarkerCharges;
        matrixMaxCharges = playerActionManager.maxMatrixMarkerCharges;
        infinityMaxCharges = playerActionManager.maxInfinityMarkerCharges;

        // Update cached values for other methods that might need them
        unitCharges = currentUnitCharges;
        recursionCharges = currentRecursionCharges;
        matrixCharges = currentMatrixCharges;
        infinityCharges = currentInfinityCharges;

        // Calculate cooldown progress for UI segments
        // Unit marker uses move-based recharge - progress is already a 0-1 fraction
        float unitCooldownProgress = (currentUnitCharges >= unitMaxCharges) ? 1f : unitMarkerRechargeProgress;
        float unitCooldownRemaining = playerActionManager.GetUnitMarkerCooldownRemaining(); // Moves remaining
        
        float recursionCooldownRemaining = playerActionManager.GetRecursionMarkerCooldownRemaining();
        float matrixCooldownRemaining = playerActionManager.GetMatrixMarkerCooldownRemaining();
        float infinityCooldownRemaining = playerActionManager.GetInfinityMarkerCooldownRemaining();

        float recursionCooldownProgress = CalculateCooldownProgress(
            currentRecursionCharges,
            recursionMaxCharges,
            recursionCooldownRemaining,
            recursionMarkerCooldownTime
        );

        float matrixCooldownProgress = CalculateCooldownProgress(
            currentMatrixCharges,
            matrixMaxCharges,
            matrixCooldownRemaining,
            matrixMarkerCooldownTime
        );

        float infinityCooldownProgress = CalculateCooldownProgress(
            currentInfinityCharges,
            infinityMaxCharges,
            infinityCooldownRemaining,
            infinityMarkerCooldownTime
        );

        // Update Unit Marker UI - Unit markers are ALWAYS available (infinite with move-based regeneration)
        // unitMaxCharges represents the regenerating charge pool, not total available
        if (UnitMarkerUI != null)
        {
            UnitMarkerUI.SetActive(true);
            
            // Ensure we have valid max charges for display (Unit uses regenerating pool of 3)
            int displayMaxCharges = unitMaxCharges > 0 ? unitMaxCharges : 3;
            
            UpdateMarkerUI(
                currentUnitCharges,
                displayMaxCharges,
                unitCooldownProgress,
                unitMarkerSegments,
                unitChargeText,
                unitMarkerColor,
                unitCooldownRemaining
            );
        }

        // Update Recursion Marker UI - disable if not available
        bool recursionMarkersAvailable = recursionMaxCharges > 0;
        if (RecursionMarkerUI != null)
        {
            if (recursionMarkersAvailable)
            {
                RecursionMarkerUI.SetActive(true);
                UpdateMarkerUI(
                    currentRecursionCharges,
                    recursionMaxCharges,
                    recursionCooldownProgress,
                    recursionMarkerSegments,
                    recursionChargeText,
                    recursionMarkerColor,
                    recursionCooldownRemaining
                );
            }
            else
            {
                RecursionMarkerUI.SetActive(false);
            }
        }

        // Update Matrix Marker UI - disable if not available
        bool matrixMarkersAvailable = matrixMaxCharges > 0;
        if (MatrixMarkerUI != null)
        {
            if (matrixMarkersAvailable)
            {
                MatrixMarkerUI.SetActive(true);
                UpdateMarkerUI(
                    currentMatrixCharges,
                    matrixMaxCharges,
                    matrixCooldownProgress,
                    matrixMarkerSegments,
                    matrixChargeText,
                    matrixMarkerColor,
                    matrixCooldownRemaining
                );
            }
            else
            {
                MatrixMarkerUI.SetActive(false);
            }
        }

        // Update Infinity Marker UI - disable if not available
        bool infinityMarkersAvailable = infinityMaxCharges > 0;
        if (InfinityMarkerUI != null)
        {
            if (infinityMarkersAvailable)
            {
                InfinityMarkerUI.SetActive(true);
                UpdateMarkerUI(
                    currentInfinityCharges,
                    infinityMaxCharges,
                    infinityCooldownProgress,
                    infinityMarkerSegments,
                    infinityChargeText,
                    infinityMarkerColor,
                    infinityCooldownRemaining
                );
            }
            else
            {
                InfinityMarkerUI.SetActive(false);
            }
        }

        // Update Mode Indicator UI
        if (playerActionManager != null)
        {
            UpdateModeIndicator(playerActionManager.GetCurrentMode());
        }
    }

    private float CalculateCooldownProgress(int charges, int maxCharges, float cooldownRemaining, float cooldownTime)
    {
        // If infinite charges (maxCharges == 0), always show as ready
        if (maxCharges == 0)
        {
            return 1f; // Fully charged (infinite)
        }

        // If at max charges, no charging in progress
        if (charges >= maxCharges)
        {
            return 1f; // Fully charged
        }

        // If no cooldown time, show as ready
        if (cooldownTime <= 0f)
        {
            return 1f;
        }

        // If no cooldown remaining, charge is ready
        if (cooldownRemaining <= 0f)
        {
            return 1f;
        }

        // Calculate progress (inverted because cooldownRemaining counts down)
        float progress = 1f - (cooldownRemaining / cooldownTime);
        return Mathf.Clamp01(progress);
    }

    private void UpdateMarkerUI(int charges, int maxCharges, float cooldownProgress,
                               Image[] segments, TextMeshProUGUI chargeText, Color fullChargeColor,
                               float cooldownRemaining)
    {
        // Check if segments array is properly assigned
        if (segments == null || segments.Length == 0)
        {
            Debug.LogError($"[UpdateMarkerUI] Segments array is null or empty! Length: {(segments?.Length ?? 0)}");
            return;
        }

        // Safety: This method should only be called when maxCharges > 0 (UI is disabled otherwise)
        // But handle gracefully if called with maxCharges == 0
        if (maxCharges == 0)
        {
            maxCharges = 9999; // Treat as infinite for display purposes
        }

        // Update charge text with current/max format - consistent for all marker types
        if (chargeText != null)
        {
            // Show cooldown remaining when charging (not at max and cooldown active)
            if (charges < maxCharges && cooldownRemaining > 0f)
            {
                chargeText.text = $"{charges}/{maxCharges} ({cooldownRemaining:F1}s)";
            }
            else
            {
                // At max charges or no cooldown - just show current/max
                chargeText.text = $"{charges}/{maxCharges}";
            }
        }

        if (charges >= maxCharges)
        {
            // At max charges - all segments should be full color (ColorA)
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] != null)
                {
                    segments[i].color = fullChargeColor; // ColorA
                    segments[i].gameObject.SetActive(true);
                }
            }
        }
        else if (cooldownRemaining > 0f)
        {
            // Charging state - segments fill proportionally based on cooldown progress
            // cooldownProgress goes from 0 (just started) to 1 (almost ready)
            float segmentProgress = cooldownProgress * segments.Length;
            int filledSegments = Mathf.FloorToInt(segmentProgress);
            float partialSegmentProgress = segmentProgress - filledSegments;
            
            // Empty segment color depends on current charge state
            Color emptyColor = charges == 0 ? segmentFirstChargeColor : segmentChargingColor; // ColorC if 0 charges, ColorB if >0 charges
            
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;

                if (i < filledSegments)
                {
                    // Fully filled segments during cooldown use ColorA
                    segments[i].color = fullChargeColor; // ColorA
                }
                else if (i == filledSegments && partialSegmentProgress > 0f)
                {
                    // Partially filled segment - lerp from empty color to ColorA
                    segments[i].color = Color.Lerp(emptyColor, fullChargeColor, partialSegmentProgress);
                }
                else
                {
                    // Empty segments use ColorB or ColorC based on charge state
                    segments[i].color = emptyColor;
                }

                segments[i].gameObject.SetActive(true);
            }
        }
        else
        {
            // No cooldown but not at max charges - show existing charges as full color, rest empty
            Color emptyColor = charges == 0 ? segmentFirstChargeColor : segmentChargingColor; // ColorC if 0 charges, ColorB if >0 charges
            
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;

                if (i < charges)
                {
                    // Existing charges shown as full color (ColorA)
                    segments[i].color = fullChargeColor; // ColorA
                }
                else
                {
                    // Empty segments use ColorB or ColorC based on charge state
                    segments[i].color = emptyColor;
                }

                segments[i].gameObject.SetActive(true);
            }
        }
    }

    private void UpdateModeIndicator(MarkerMode currentMode)
    {
        if (modeIndicatorSegments == null || modeIndicatorSegments.Length < 3) return;

        // Check if mode has changed to trigger animation
        bool modeChanged = false;
        
        // Check which markers are available
        // Unit markers are ALWAYS available (infinite with move-based regeneration)
        bool unitMarkersAvailable = true;
        bool matrixMarkersAvailable = matrixMaxCharges > 0;
        bool recursionMarkersAvailable = recursionMaxCharges > 0;
        bool infinityMarkersAvailable = infinityMaxCharges > 0;
        
        // Update mode indicator segments - highlight current active mode
        // Segments: 0=Unit, 1=Matrix, 2=Recursion, 3=Infinity
        for (int i = 0; i < modeIndicatorSegments.Length && i < 4; i++)
        {
            if (modeIndicatorSegments[i] == null) continue;

            // Calculate which mode this segment represents (Unit=1, Matrix=2, Recursion=3, Infinity=4)
            MarkerMode segmentMode = (MarkerMode)(i + 1);

            // Hide mode indicator if markers aren't available
            bool shouldShow = segmentMode switch
            {
                MarkerMode.Unit => unitMarkersAvailable,
                MarkerMode.Matrix => matrixMarkersAvailable,
                MarkerMode.Recursion => recursionMarkersAvailable,
                MarkerMode.Infinity => infinityMarkersAvailable,
                _ => true
            };

            if (!shouldShow)
            {
                modeIndicatorSegments[i].gameObject.SetActive(false);
                continue;
            }

            if (segmentMode == currentMode)
            {
                // Check if this segment wasn't active before (mode change)
                if (modeIndicatorSegments[i].color == segmentEmptyColor)
                {
                    modeChanged = true;
                }
                
                // Active mode - use mode-specific color
                if (i < modeColors.Length)
                {
                    modeIndicatorSegments[i].color = modeColors[i];
                }
            }
            else
            {
                // Inactive mode - use empty color
                modeIndicatorSegments[i].color = segmentEmptyColor;
            }

            modeIndicatorSegments[i].gameObject.SetActive(true);
        }
        
        // Trigger animation for mode change
        if (modeChanged)
        {
            TriggerModeChangeAnimation(currentMode);
        }
    }

    // Public getters for UI state
    public float GetUnitCooldownProgress()
    {
        if (playerActionManager == null) return 0f;
        // Unit marker uses move-based recharge - progress is already a 0-1 fraction
        return (unitCharges >= unitMaxCharges) ? 1f : unitMarkerRechargeProgress;
    }

    public float GetRecursionCooldownProgress()
    {
        if (playerActionManager == null) return 0f;
        return CalculateCooldownProgress(
            recursionCharges,
            recursionMaxCharges,
            playerActionManager.GetRecursionMarkerCooldownRemaining(),
            recursionMarkerCooldownTime
        );
    }

    public float GetMatrixCooldownProgress()
    {
        if (playerActionManager == null) return 0f;
        return CalculateCooldownProgress(
            matrixCharges,
            matrixMaxCharges,
            playerActionManager.GetMatrixMarkerCooldownRemaining(),
            matrixMarkerCooldownTime
        );
    }

    public bool IsUnitCharging() => unitCharges < unitMaxCharges;
    public bool IsRecursionCharging() => recursionCharges < recursionMaxCharges;
    public bool IsMatrixCharging() => matrixCharges < matrixMaxCharges;
    

    // Set max charges explicitly
    public void SetMaxCharges(int maxUnit, int maxRecursion, int maxMatrix, int maxInfinity)
    {
        unitMaxCharges = maxUnit;
        recursionMaxCharges = maxRecursion;
        matrixMaxCharges = maxMatrix;
        infinityMaxCharges = maxInfinity;
    }

    // Public method to update mode indicator from external calls
    public void UpdateModeIndicatorDisplay(MarkerMode currentMode)
    {
        UpdateModeIndicator(currentMode);
    }

    #region Animation Trigger Integration

    /// <summary>
    /// Triggers animation for UI updates (charge changes, cooldown updates)
    /// </summary>
    private void TriggerUIUpdateAnimation()
    {
        if (animationTriggerManager != null)
        {
            // Use UI center position as reference point for animations
            Vector3 uiPosition = transform.position;
            var context = AnimationTriggerContext.Create(uiPosition, 0.5f);
            context.additionalData = "UI charge update";
            context.duration = 0.3f; // Quick UI update animation
            animationTriggerManager.TriggerAnimation(AnimationTriggerPoint.UIUpdate, context);
        }
    }

    /// <summary>
    /// Triggers animation for mode changes in the UI
    /// </summary>
    /// <param name="newMode">The new active mode</param>
    private void TriggerModeChangeAnimation(MarkerMode newMode)
    {
        if (animationTriggerManager != null)
        {
            // Use mode indicator position as reference point
            Vector3 modeIndicatorPosition = modeIndicatorSegments != null && modeIndicatorSegments.Length > 0 && modeIndicatorSegments[0] != null
                ? modeIndicatorSegments[0].transform.position
                : transform.position;
                
            var context = AnimationTriggerContext.Create(modeIndicatorPosition, 1.0f);
            context.markerMode = newMode;
            context.additionalData = $"Mode changed to {newMode}";
            context.duration = 0.5f; // Mode change animation duration
            animationTriggerManager.TriggerAnimation(AnimationTriggerPoint.UIUpdate, context);
        }
    }

    #endregion
}

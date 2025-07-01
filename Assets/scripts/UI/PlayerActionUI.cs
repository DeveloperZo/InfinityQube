using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerActionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image[] lightMarkerSegments = new Image[6];
    [SerializeField] private Image[] heavyMarkerSegments = new Image[6];
    [SerializeField] private Image[] primeMarkerSegments = new Image[6];
    [SerializeField] private Image[] cubeMarkerSegments = new Image[6];
    [SerializeField] private Image[] modeIndicatorSegments = new Image[3];
    [SerializeField] private TextMeshProUGUI lightChargeText;
    [SerializeField] private TextMeshProUGUI heavyChargeText;
    [SerializeField] private TextMeshProUGUI primeChargeText;
    [SerializeField] private TextMeshProUGUI cubeChargeText;

    [Header("UI Colors")]
    [SerializeField] private Color segmentFullColor = new Color(1f, 0.7f, 0.2f, 1f);     // Orange when charged
    [SerializeField] private Color segmentChargingColor = new Color(0.5f, 0.5f, 1f, 0.8f); // Blue when charging
    [SerializeField] private Color segmentFirstChargeColor = new Color(0.7f, 0.7f, 1f, 0.6f); // Light blue when charging toward first charge
    [SerializeField] private Color segmentEmptyColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);  // Gray when empty
    [SerializeField] private Color heavyMarkerColor = new Color(1f, 0.3f, 0.3f, 1f);      // Red for heavy markers
    [SerializeField] private Color primeMarkerColor = new Color(0.8f, 0.2f, 1f, 1f);      // Purple for prime markers
    [SerializeField] private Color cubeMarkerColor = new Color(0.2f, 1f, 0.2f, 1f);       // Green for cube markers
    [SerializeField] private Color[] modeColors = new Color[3] 
    {
        new Color(1f, 0.3f, 0.3f, 1f),      // Red for Light mode (index 0 = Light=1)
        new Color(0.8f, 0.2f, 1f, 1f),      // Purple for Prime mode (index 1 = Prime=2)
        new Color(0.6f, 0.1f, 0.1f, 1f)     // Dark Red for Heavy mode (index 2 = Heavy=3)
    };

    [Header("Cooldown Settings")]
    [SerializeField] private PlayerActionManager playerActionManager;
    [SerializeField] private AnimationTriggerManager animationTriggerManager;
    public float lightMarkerCooldownTime = 2f;
    public float heavyMarkerCooldownTime = 5f;
    public float primeMarkerCooldownTime = 4f;
    public float cubeMarkerCooldownTime = 1f;

    // Cached references
    public int lightCharges;
    public int lightMaxCharges;
    public int heavyCharges;
    public int heavyMaxCharges;
    public int primeCharges;
    public int primeMaxCharges;
    public int cubeCharges;
    public int cubeMaxCharges;

    void Start()
    {
        playerActionManager = FindAnyObjectByType<PlayerActionManager>();
        animationTriggerManager = FindAnyObjectByType<AnimationTriggerManager>();

        if (playerActionManager != null)
        {
            lightMaxCharges = playerActionManager.maxLightMarkerCharges;
            heavyMaxCharges = playerActionManager.maxHeavyMarkerCharges;
            primeMaxCharges = playerActionManager.maxPrimeMarkerCharges;
            cubeMaxCharges = 10; // Default value for cube markers
            lightMarkerCooldownTime = playerActionManager.lightMarkerCooldown;
            heavyMarkerCooldownTime = playerActionManager.heavyMarkerCooldown;
            primeMarkerCooldownTime = playerActionManager.primeMarkerCooldown;
            cubeMarkerCooldownTime = 1f; // Default value for cube markers
        }

        UpdateDisplay();
    }

    private void Update()
    {
        UpdateDisplay();
    }

    public void UpdateCharges(int currentLightCharges, int currentHeavyCharges, int currentPrimeCharges, int currentCubeCharges)
    {
        // Check if charges have changed to trigger animations
        bool chargesChanged = (lightCharges != currentLightCharges) || 
                             (heavyCharges != currentHeavyCharges) || 
                             (primeCharges != currentPrimeCharges) || 
                             (cubeCharges != currentCubeCharges);

        lightCharges = currentLightCharges;
        heavyCharges = currentHeavyCharges;
        primeCharges = currentPrimeCharges;
        cubeCharges = currentCubeCharges;

        // Set max charges if not already set
        if (lightMaxCharges == 0 && playerActionManager != null)
        {
            lightMaxCharges = playerActionManager.maxLightMarkerCharges;
        }
        if (heavyMaxCharges == 0 && playerActionManager != null)
        {
            heavyMaxCharges = playerActionManager.maxHeavyMarkerCharges;
        }
        if (primeMaxCharges == 0 && playerActionManager != null)
        {
            primeMaxCharges = playerActionManager.maxPrimeMarkerCharges;
        }
        if (cubeMaxCharges == 0)
        {
            cubeMaxCharges = 10; // Default value for cube markers
        }

        // Trigger animation for UI update if charges changed
        if (chargesChanged)
        {
            TriggerUIUpdateAnimation();
        }
    }

    // Backward compatibility method
    public void UpdateCharges(int currentLightCharges, int currentAreaCharges)
    {
        UpdateCharges(currentLightCharges, 0, currentAreaCharges, 0);
    }

    public void UpdateCooldowns(float lightCooldown, float heavyCooldown, float primeCooldown, float cubeCooldown)
    {
        lightMarkerCooldownTime = lightCooldown;
        heavyMarkerCooldownTime = heavyCooldown;
        primeMarkerCooldownTime = primeCooldown;
        cubeMarkerCooldownTime = cubeCooldown;
    }

    // Backward compatibility method
    public void UpdateCooldowns(float individualCooldown, float areaCooldown)
    {
        lightMarkerCooldownTime = individualCooldown;
        primeMarkerCooldownTime = areaCooldown;
    }

    public void OnMarkerPlaced(bool isLightMarker)
    {
        Debug.Log($"Marker placed - {(isLightMarker ? "Individual" : "Area")}");
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

        // Calculate cooldown progress for UI segments
        float lightCooldownProgress = CalculateCooldownProgress(
            lightCharges,
            lightMaxCharges,
            playerActionManager.GetLightMarkerCooldownRemaining(),
            lightMarkerCooldownTime
        );

        float heavyCooldownProgress = CalculateCooldownProgress(
            heavyCharges,
            heavyMaxCharges,
            playerActionManager.GetHeavyMarkerCooldownRemaining(),
            heavyMarkerCooldownTime
        );

        float primeCooldownProgress = CalculateCooldownProgress(
            primeCharges,
            primeMaxCharges,
            playerActionManager.GetPrimeMarkerCooldownRemaining(),
            primeMarkerCooldownTime
        );

        float cubeCooldownProgress = CalculateCooldownProgress(
            cubeCharges,
            cubeMaxCharges,
            0f, // Cube markers don't have cooldowns in the same way
            cubeMarkerCooldownTime
        );

        // Update Light Marker UI
        UpdateMarkerUI(
            lightCharges,
            lightMaxCharges,
            lightCooldownProgress,
            lightMarkerSegments,
            lightChargeText,
            segmentFullColor
        );

        // Update Heavy Marker UI
        UpdateMarkerUI(
            heavyCharges,
            heavyMaxCharges,
            heavyCooldownProgress,
            heavyMarkerSegments,
            heavyChargeText,
            heavyMarkerColor
        );

        // Update Prime Marker UI
        UpdateMarkerUI(
            primeCharges,
            primeMaxCharges,
            primeCooldownProgress,
            primeMarkerSegments,
            primeChargeText,
            primeMarkerColor
        );

        // Update Cube Marker UI
        UpdateCubeMarkerUI();

        // Update Mode Indicator UI
        if (playerActionManager != null)
        {
            UpdateModeIndicator(playerActionManager.GetCurrentMode());
        }
    }

    private float CalculateCooldownProgress(int charges, int maxCharges, float cooldownRemaining, float cooldownTime)
    {
        // If at max charges, show as fully charged
        if (charges >= maxCharges)
        {
            return 1f;
        }

        // If no cooldown time, show as ready
        if (cooldownTime <= 0f)
        {
            return 1f;
        }

        // Calculate progress (inverted because cooldownRemaining counts down)
        float progress = 1f - (cooldownRemaining / cooldownTime);
        return Mathf.Clamp01(progress);
    }

    private void UpdateMarkerUI(int charges, int maxCharges, float cooldownProgress,
                               Image[] segments, TextMeshProUGUI chargeText, Color fullChargeColor)
    {
        // Update charge text
        if (chargeText != null)
            chargeText.text = charges.ToString();

        if (charges >= maxCharges)
        {
            // Full charges - all segments with marker-specific color
            foreach (var segment in segments)
            {
                if (segment != null)
                {
                    segment.color = fullChargeColor;
                    segment.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            // Charging state - show progress based on cooldown
            int activeSegments = Mathf.FloorToInt(cooldownProgress * segments.Length);

            // Determine charging color based on current charges
            Color chargingColor = charges == 0 ? segmentFirstChargeColor : segmentChargingColor;

            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;

                if (i < activeSegments)
                {
                    // Filled segments - use appropriate charging color
                    segments[i].color = chargingColor;
                }
                else if (i == activeSegments && cooldownProgress < 1.0f)
                {
                    // Currently filling segment - lerp from empty to charging color
                    float segmentProgress = (cooldownProgress * segments.Length) % 1;
                    segments[i].color = Color.Lerp(segmentEmptyColor, chargingColor, segmentProgress);
                }
                else
                {
                    // Empty segments - gray
                    segments[i].color = segmentEmptyColor;
                }

                segments[i].gameObject.SetActive(true);
            }
        }
    }

    private void UpdateCubeMarkerUI()
    {
        // Get current cube marker count from PlayerActionManager
        int currentCubeMarkers = playerActionManager != null ? playerActionManager.GetCurrentCubeMarkers() : 0;
        cubeCharges = currentCubeMarkers;

        // Update cube charge text
        if (cubeChargeText != null)
            cubeChargeText.text = cubeCharges.ToString();

        // Update cube marker segments - show active markers
        for (int i = 0; i < cubeMarkerSegments.Length; i++)
        {
            if (cubeMarkerSegments[i] == null) continue;

            if (i < cubeCharges)
            {
                // Active cube marker
                cubeMarkerSegments[i].color = cubeMarkerColor;
            }
            else
            {
                // Inactive cube marker slot
                cubeMarkerSegments[i].color = segmentEmptyColor;
            }

            cubeMarkerSegments[i].gameObject.SetActive(true);
        }
    }

    private void UpdateModeIndicator(Enumerations.MarkerMode currentMode)
    {
        if (modeIndicatorSegments == null || modeIndicatorSegments.Length != 3) return;

        // Check if mode has changed to trigger animation
        bool modeChanged = false;
        
        // Update mode indicator segments - highlight current active mode
        for (int i = 0; i < modeIndicatorSegments.Length; i++)
        {
            if (modeIndicatorSegments[i] == null) continue;

            // Calculate which mode this segment represents (Light=1, Prime=2, Heavy=3)
            Enumerations.MarkerMode segmentMode = (Enumerations.MarkerMode)(i + 1);

            if (segmentMode == currentMode)
            {
                // Check if this segment wasn't active before (mode change)
                if (modeIndicatorSegments[i].color == segmentEmptyColor)
                {
                    modeChanged = true;
                }
                
                // Active mode - use mode-specific color
                modeIndicatorSegments[i].color = modeColors[i];
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
    public float GetLightCooldownProgress()
    {
        if (playerActionManager == null) return 0f;
        return CalculateCooldownProgress(
            lightCharges,
            lightMaxCharges,
            playerActionManager.GetLightMarkerCooldownRemaining(),
            lightMarkerCooldownTime
        );
    }

    public float GetHeavyCooldownProgress()
    {
        if (playerActionManager == null) return 0f;
        return CalculateCooldownProgress(
            heavyCharges,
            heavyMaxCharges,
            playerActionManager.GetHeavyMarkerCooldownRemaining(),
            heavyMarkerCooldownTime
        );
    }

    public float GetPrimeCooldownProgress()
    {
        if (playerActionManager == null) return 0f;
        return CalculateCooldownProgress(
            primeCharges,
            primeMaxCharges,
            playerActionManager.GetPrimeMarkerCooldownRemaining(),
            primeMarkerCooldownTime
        );
    }

    public float GetCubeCooldownProgress()
    {
        return 1f; // Cube markers are always ready when available
    }

    // Backward compatibility getter
    public float GetAreaCooldownProgress() => GetPrimeCooldownProgress();

    public bool IsLightCharging() => lightCharges < lightMaxCharges;
    public bool IsHeavyCharging() => heavyCharges < heavyMaxCharges;
    public bool IsPrimeCharging() => primeCharges < primeMaxCharges;
    public bool IsCubeCharging() => false; // Cube markers don't have traditional charging
    
    // Backward compatibility property
    public bool IsAreaCharging() => IsPrimeCharging();

    // Set max charges explicitly
    public void SetMaxCharges(int maxLight, int maxHeavy, int maxPrime, int maxCube)
    {
        lightMaxCharges = maxLight;
        heavyMaxCharges = maxHeavy;
        primeMaxCharges = maxPrime;
        cubeMaxCharges = maxCube;
    }

    // Backward compatibility method
    public void SetMaxCharges(int maxLight, int maxArea)
    {
        lightMaxCharges = maxLight;
        primeMaxCharges = maxArea;
    }

    // Public method to update mode indicator from external calls
    public void UpdateModeIndicatorDisplay(Enumerations.MarkerMode currentMode)
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
    private void TriggerModeChangeAnimation(Enumerations.MarkerMode newMode)
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

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerActionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image[] lightMarkerSegments = new Image[6];
    [SerializeField] private Image[] heavyMarkerSegments = new Image[6];
    [SerializeField] private Image[] primeMarkerSegments = new Image[6];
    [SerializeField] private GameObject LightMarkerUI;
    [SerializeField] private GameObject PrimeMarkerUI;
    [SerializeField] private Image[] modeIndicatorSegments = new Image[3];
    [SerializeField] private TextMeshProUGUI lightChargeText;
    [SerializeField] private TextMeshProUGUI heavyChargeText;
    [SerializeField] private TextMeshProUGUI primeChargeText;
    // Cube markers don't need UI elements

    [Header("UI Colors")]
    [SerializeField] private Color segmentFullColor = new Color(1f, 0.7f, 0.2f, 1f);     // Orange when charged
    [SerializeField] private Color segmentChargingColor = new Color(0.5f, 0.5f, 1f, 0.8f); // Blue when charging
    [SerializeField] private Color segmentFirstChargeColor = new Color(0.7f, 0.7f, 1f, 0.6f); // Light blue when charging toward first charge
    [SerializeField] private Color segmentEmptyColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);  // Gray when empty
    [SerializeField] private Color heavyMarkerColor = new Color(1f, 0.3f, 0.3f, 1f);      // Red for heavy markers
    [SerializeField] private Color primeMarkerColor = new Color(0.8f, 0.2f, 1f, 1f);      // Purple for prime markers
    // Cube markers don't need UI colors
    [SerializeField] private Color[] modeColors = new Color[3] 
    {
        new Color(1f, 0.3f, 0.3f, 1f),      // Red for Light mode (index 0 = Light=1)
        new Color(0.8f, 0.2f, 1f, 1f),      // Purple for Prime mode (index 1 = Prime=2)
        new Color(0.6f, 0.1f, 0.1f, 1f)     // Dark Red for Heavy mode (index 2 = Heavy=3)
    };

    [Header("Cooldown Settings")]
    [SerializeField] private PlayerActionManager playerActionManager;
    [SerializeField] private AnimationTriggerManager animationTriggerManager;
    public float lightMarkerCooldownTime = 6f;
    public float heavyMarkerCooldownTime = 8f;
    public float primeMarkerCooldownTime = 12f;
    // Cube markers don't have cooldowns

    // Cached references
    public int lightCharges;
    public int lightMaxCharges;
    public int heavyCharges;
    public int heavyMaxCharges;
    public int primeCharges;
    public int primeMaxCharges;
    // Cube markers don't need cached charge values

    void Start()
    {
        playerActionManager = FindAnyObjectByType<PlayerActionManager>();
        animationTriggerManager = FindAnyObjectByType<AnimationTriggerManager>();

        if (playerActionManager != null)
        {
            lightMaxCharges = playerActionManager.maxLightMarkerCharges;
            heavyMaxCharges = playerActionManager.maxHeavyMarkerCharges;
            primeMaxCharges = playerActionManager.maxPrimeMarkerCharges;
            lightMarkerCooldownTime = playerActionManager.lightMarkerCooldown;
            heavyMarkerCooldownTime = playerActionManager.heavyMarkerCooldown;
            primeMarkerCooldownTime = playerActionManager.primeMarkerCooldown;
            // Cube markers don't need cooldown time
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
                             (primeCharges != currentPrimeCharges);
                             // Cube markers don't have UI updates

        lightCharges = currentLightCharges;
        heavyCharges = currentHeavyCharges;
        primeCharges = currentPrimeCharges;
        // Cube markers don't need to be cached for UI

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
        // Cube markers don't have cooldowns in UI
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

        // Get current charges directly from PlayerActionManager for real-time display
        int currentLightCharges = playerActionManager.GetCurrentLightCharges();
        int currentHeavyCharges = playerActionManager.GetCurrentHeavyCharges();
        int currentPrimeCharges = playerActionManager.GetCurrentPrimeCharges();
        // Cube markers don't need UI updates

        // Debug: Always log current values to see what's happening
        Debug.Log($"[PlayerActionUI] Current values - Light: {currentLightCharges}/{lightMaxCharges}, Heavy: {currentHeavyCharges}/{heavyMaxCharges}, Prime: {currentPrimeCharges}/{primeMaxCharges}");

        // Only log significant charge changes to avoid spam
        if (currentLightCharges != lightCharges || currentHeavyCharges != heavyCharges || currentPrimeCharges != primeCharges)
        {
            // Only log when charges actually decrease (used) or when they go from 0 to 1 (regenerated)
            if ((currentLightCharges < lightCharges) || (currentLightCharges > 0 && lightCharges == 0) ||
                (currentHeavyCharges < heavyCharges) || (currentHeavyCharges > 0 && heavyCharges == 0) ||
                (currentPrimeCharges < primeCharges) || (currentPrimeCharges > 0 && primeCharges == 0))
            {
                Debug.Log($"[PlayerActionUI] Charges updated - Light: {lightCharges}→{currentLightCharges}, Heavy: {heavyCharges}→{currentHeavyCharges}, Prime: {primeCharges}→{currentPrimeCharges}");
            }
        }

        // Update cached values for other methods that might need them
        lightCharges = currentLightCharges;
        heavyCharges = currentHeavyCharges;
        primeCharges = currentPrimeCharges;
        // Cube markers don't need cached values

        // Calculate cooldown progress for UI segments
        float lightCooldownRemaining = playerActionManager.GetLightMarkerCooldownRemaining();
        float heavyCooldownRemaining = playerActionManager.GetHeavyMarkerCooldownRemaining();
        float primeCooldownRemaining = playerActionManager.GetPrimeMarkerCooldownRemaining();

        float lightCooldownProgress = CalculateCooldownProgress(
            currentLightCharges,
            lightMaxCharges,
            lightCooldownRemaining,
            lightMarkerCooldownTime
        );

        float heavyCooldownProgress = CalculateCooldownProgress(
            currentHeavyCharges,
            heavyMaxCharges,
            heavyCooldownRemaining,
            heavyMarkerCooldownTime
        );

        float primeCooldownProgress = CalculateCooldownProgress(
            currentPrimeCharges,
            primeMaxCharges,
            primeCooldownRemaining,
            primeMarkerCooldownTime
        );

        // Debug cooldown information only when cooldowns start (to avoid spam)
        // This will only log once per cooldown cycle when cooldown is near max time
        if ((lightCooldownRemaining > lightMarkerCooldownTime - 0.1f) ||
            (heavyCooldownRemaining > heavyMarkerCooldownTime - 0.1f) ||
            (primeCooldownRemaining > primeMarkerCooldownTime - 0.1f))
        {
            Debug.Log($"[PlayerActionUI] Cooldowns started - Light: {lightCooldownRemaining:F1}s, Heavy: {heavyCooldownRemaining:F1}s, Prime: {primeCooldownRemaining:F1}s");
        }

        // Cube markers don't need cooldown progress calculations

        // Update Light Marker UI
        UpdateMarkerUI(
            currentLightCharges,
            lightMaxCharges,
            lightCooldownProgress,
            lightMarkerSegments,
            lightChargeText,
            segmentFullColor,
            lightCooldownRemaining
        );

        // Update Heavy Marker UI
        UpdateMarkerUI(
            currentHeavyCharges,
            heavyMaxCharges,
            heavyCooldownProgress,
            heavyMarkerSegments,
            heavyChargeText,
            heavyMarkerColor,
            heavyCooldownRemaining
        );

        // Update Prime Marker UI - only show if prime markers are available
        bool primeMarkersAvailable = primeMaxCharges > 0;
        if (primeMarkersAvailable)
        {
            PrimeMarkerUI.SetActive(true);
            UpdateMarkerUI(
                currentPrimeCharges,
                primeMaxCharges,
                primeCooldownProgress,
                primeMarkerSegments,
                primeChargeText,
                primeMarkerColor,
                primeCooldownRemaining
            );
        }
        else
        {
            // Hide prime marker UI elements when unavailable
            PrimeMarkerUI.SetActive(false);
        }

        // Update Mode Indicator UI
        if (playerActionManager != null)
        {
            UpdateModeIndicator(playerActionManager.GetCurrentMode());
        }
    }

    private float CalculateCooldownProgress(int charges, int maxCharges, float cooldownRemaining, float cooldownTime)
    {
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

        // Update charge text with cooldown information
        if (chargeText != null)
        {
            if (charges >= maxCharges)
            {
                chargeText.text = $"{charges}/{maxCharges}";
            }
            else
            {
                // Show cooldown remaining when not at max charges
                if (cooldownRemaining > 0f)
                {
                    chargeText.text = $"{charges}/{maxCharges} ({cooldownRemaining:F1}s)";
                }
                else
                {
                    chargeText.text = $"{charges}/{maxCharges}";
                }
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

    private void UpdateModeIndicator(Enumerations.MarkerMode currentMode)
    {
        if (modeIndicatorSegments == null || modeIndicatorSegments.Length != 3) return;

        // Check if mode has changed to trigger animation
        bool modeChanged = false;
        
        // Check if prime markers are available
        bool primeMarkersAvailable = primeMaxCharges > 0;
        
        // Update mode indicator segments - highlight current active mode
        for (int i = 0; i < modeIndicatorSegments.Length; i++)
        {
            if (modeIndicatorSegments[i] == null) continue;

            // Calculate which mode this segment represents (Light=1, Prime=2, Heavy=3)
            Enumerations.MarkerMode segmentMode = (Enumerations.MarkerMode)(i + 1);

            // Hide prime mode indicator if prime markers aren't available
            if (segmentMode == Enumerations.MarkerMode.Prime && !primeMarkersAvailable)
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

    // Cube markers don't need cooldown progress getters

    // Backward compatibility getter
    public float GetAreaCooldownProgress() => GetPrimeCooldownProgress();

    public bool IsLightCharging() => lightCharges < lightMaxCharges;
    public bool IsHeavyCharging() => heavyCharges < heavyMaxCharges;
    public bool IsPrimeCharging() => primeCharges < primeMaxCharges;
    // Cube markers don't need charging state checks
    
    // Backward compatibility property
    public bool IsAreaCharging() => IsPrimeCharging();

    // Set max charges explicitly
    public void SetMaxCharges(int maxLight, int maxHeavy, int maxPrime, int maxCube)
    {
        lightMaxCharges = maxLight;
        heavyMaxCharges = maxHeavy;
        primeMaxCharges = maxPrime;
        // Cube markers don't need max charges stored
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

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerActionUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image[] lightMarkerSegments = new Image[6];
    [SerializeField] private Image[] areaMarkerSegments = new Image[6];
    [SerializeField] private TextMeshProUGUI lightChargeText;
    [SerializeField] private TextMeshProUGUI areaChargeText;

    [Header("UI Colors")]
    [SerializeField] private Color segmentFullColor = new Color(1f, 0.7f, 0.2f, 1f);     // Orange when charged
    [SerializeField] private Color segmentChargingColor = new Color(0.5f, 0.5f, 1f, 0.8f); // Blue when charging
    [SerializeField] private Color segmentFirstChargeColor = new Color(0.7f, 0.7f, 1f, 0.6f); // Light blue when charging toward first charge
    [SerializeField] private Color segmentEmptyColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);  // Gray when empty

    [Header("Cooldown Settings")]
    [SerializeField] private PlayerActionManager playerActionManager;
    public float lightMarkerCooldownTime = 2f;
    public float areaMarkerCooldownTime = 4f;

    // Cached references
    public int lightCharges;
    public int lightMaxCharges;
    public int areaCharges;
    public int areaMaxCharges;

    void Start()
    {
        playerActionManager = FindAnyObjectByType<PlayerActionManager>();

        if (playerActionManager != null)
        {
            lightMaxCharges = playerActionManager.maxIndividualMarkerCharges;
            areaMaxCharges = playerActionManager.maxAreaMarkerCharges;
            lightMarkerCooldownTime = playerActionManager.individualMarkerCooldown;
            areaMarkerCooldownTime = playerActionManager.areaMarkerCooldown;
        }

        UpdateDisplay();
    }

    private void Update()
    {
        UpdateDisplay();
    }

    public void UpdateCharges(int currentLightCharges, int currentAreaCharges)
    {
        lightCharges = currentLightCharges;
        areaCharges = currentAreaCharges;

        // Set max charges if not already set
        if (lightMaxCharges == 0 && playerActionManager != null)
        {
            lightMaxCharges = playerActionManager.maxIndividualMarkerCharges;
        }
        if (areaMaxCharges == 0 && playerActionManager != null)
        {
            areaMaxCharges = playerActionManager.maxAreaMarkerCharges;
        }
    }

    public void UpdateCooldowns(float individualCooldown, float areaCooldown)
    {
        lightMarkerCooldownTime = individualCooldown;
        areaMarkerCooldownTime = areaCooldown;
    }

    public void OnMarkerPlaced(bool isLightMarker)
    {
        Debug.Log($"Marker placed - {(isLightMarker ? "Individual" : "Area")}");
    }

    private void UpdateDisplay()
    {
        if (playerActionManager == null) return;

        // Calculate cooldown progress for UI segments
        float lightCooldownProgress = CalculateCooldownProgress(
            lightCharges,
            lightMaxCharges,
            playerActionManager.GetIndividualMarkerCooldownRemaining(),
            lightMarkerCooldownTime
        );

        float areaCooldownProgress = CalculateCooldownProgress(
            areaCharges,
            areaMaxCharges,
            playerActionManager.GetAreaMarkerCooldownRemaining(),
            areaMarkerCooldownTime
        );

        // Update Light Marker UI
        UpdateMarkerUI(
            lightCharges,
            lightMaxCharges,
            lightCooldownProgress,
            lightMarkerSegments,
            lightChargeText
        );

        // Update Area Marker UI
        UpdateMarkerUI(
            areaCharges,
            areaMaxCharges,
            areaCooldownProgress,
            areaMarkerSegments,
            areaChargeText
        );
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
                               Image[] segments, TextMeshProUGUI chargeText)
    {
        // Update charge text
        if (chargeText != null)
            chargeText.text = charges.ToString();

        if (charges >= maxCharges)
        {
            // Full charges - all segments bright orange
            foreach (var segment in segments)
            {
                if (segment != null)
                {
                    segment.color = segmentFullColor;
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

    // Public getters for UI state
    public float GetLightCooldownProgress()
    {
        if (playerActionManager == null) return 0f;
        return CalculateCooldownProgress(
            lightCharges,
            lightMaxCharges,
            playerActionManager.GetIndividualMarkerCooldownRemaining(),
            lightMarkerCooldownTime
        );
    }

    public float GetAreaCooldownProgress()
    {
        if (playerActionManager == null) return 0f;
        return CalculateCooldownProgress(
            areaCharges,
            areaMaxCharges,
            playerActionManager.GetAreaMarkerCooldownRemaining(),
            areaMarkerCooldownTime
        );
    }

    public bool IsLightCharging() => lightCharges < lightMaxCharges;
    public bool IsAreaCharging() => areaCharges < areaMaxCharges;

    // Set max charges explicitly
    public void SetMaxCharges(int maxLight, int maxArea)
    {
        lightMaxCharges = maxLight;
        areaMaxCharges = maxArea;
    }
}

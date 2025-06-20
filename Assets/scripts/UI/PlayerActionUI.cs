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
    [SerializeField] public float lightMarkerCooldownTime = 2f;
    [SerializeField] public float areaMarkerCooldownTime = 4f;

    // Cooldown timers
    public float lightMarkerCooldownTimer = 0f;
    public float areaMarkerCooldownTimer = 0f;

    // Cached references
    public int lightCharges;
    public int lightMaxCharges;
    public int areaCharges;
    public int areaMaxCharges;

    void Start()
    {
        playerActionManager = FindAnyObjectByType<PlayerActionManager>();
        UpdateDisplay();
    }

    private void Update()
    {
        // Sync timers with PlayerActionManager's stacked charge system
        if (playerActionManager != null)
        {
            // Get the time for the next charge to regenerate
            float nextIndividualChargeTime = playerActionManager.GetNextIndividualChargeTime();
            float nextAreaChargeTime = playerActionManager.GetNextAreaChargeTime();

            // Calculate progress for next charge
            if (lightCharges < lightMaxCharges)
            {
                lightMarkerCooldownTimer = Time.time - nextIndividualChargeTime;
            }
            else
            {
                lightMarkerCooldownTimer = 0f; // Ready to use
            }

            if (areaCharges < areaMaxCharges)
            {
                areaMarkerCooldownTimer = Time.time - nextAreaChargeTime;
            }
            else
            {
                areaMarkerCooldownTimer = 0f; // Ready to use
            }
        }

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

        // Reset cooldown timer if charges are at max
        if (lightCharges >= lightMaxCharges)
            lightMarkerCooldownTimer = 0f;
        if (areaCharges >= areaMaxCharges)
            areaMarkerCooldownTimer = 0f;
    }

    public void UpdateCooldowns(float individualCooldown, float areaCooldown)
    {
        lightMarkerCooldownTime = individualCooldown;
        areaMarkerCooldownTime = areaCooldown;
    }

    public void OnMarkerPlaced(bool isLightMarker)
    {
        // Update method will handle timer synchronization with stacked charges
        Debug.Log($"Marker placed - {(isLightMarker ? "Individual" : "Area")}");
    }

    private void UpdateDisplay()
    {
        // Update Light Marker UI
        UpdateMarkerUI(
            lightCharges,
            lightMaxCharges,
            lightMarkerCooldownTimer,
            lightMarkerCooldownTime,
            lightMarkerSegments,
            lightChargeText
        );

        // Update Area Marker UI
        UpdateMarkerUI(
            areaCharges,
            areaMaxCharges,
            areaMarkerCooldownTimer,
            areaMarkerCooldownTime,
            areaMarkerSegments,
            areaChargeText
        );
    }

    private void UpdateMarkerUI(int charges, int maxCharges, float cooldownTimer,
                               float cooldownTime, Image[] segments, TextMeshProUGUI chargeText)
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
            // Charging state - show progress
            float progress = Mathf.Clamp01(cooldownTimer / cooldownTime);
            int activeSegments = Mathf.FloorToInt(progress * segments.Length);

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
                else if (i == activeSegments && progress < 1.0f)
                {
                    // Currently filling segment - lerp from empty to charging color
                    float segmentProgress = (progress * segments.Length) % 1;
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
    public float GetLightCooldownProgress() => lightMarkerCooldownTimer / lightMarkerCooldownTime;
    public float GetAreaCooldownProgress() => areaMarkerCooldownTimer / areaMarkerCooldownTime;
    public bool IsLightCharging() => lightCharges < lightMaxCharges;
    public bool IsAreaCharging() => areaCharges < areaMaxCharges;

    // Set max charges explicitly
    public void SetMaxCharges(int maxLight, int maxArea)
    {
        lightMaxCharges = maxLight;
        areaMaxCharges = maxArea;
    }
}
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
    [SerializeField] private Color segmentEmptyColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);  // Gray when empty

    [Header("Cooldown Settings")]
    [SerializeField] PlayerActionManager playerActionManager;
    [SerializeField] public float lightMarkerCooldownTime = 3f;
    [SerializeField] public float areaMarkerCooldownTime = 5f;

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
        UpdateDisplay();
    }
    public void UpdateCharges(int currentLightCharges, int currentAreaCharges)
    {
        lightCharges = currentLightCharges;
        areaCharges = currentAreaCharges;

        // Reset cooldown timer if charges were restored externally
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

    public void OnMarkerUsed(bool isLightMarker)
    {
        if (isLightMarker)
        {
            if (lightCharges == 1) // Was our last charge
                lightMarkerCooldownTimer = 0f;
        }
        else
        {
            if (areaCharges == 1) // Was our last charge
                areaMarkerCooldownTimer = 0f;
        }
    }

    public void UpdateDisplay()
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
            // Full charges - all segments lit
            foreach (var segment in segments)
            {
                segment.color = segmentFullColor;
                segment.gameObject.SetActive(true);
            }
        }
        else if (charges == 0 && cooldownTimer < 0.01f)
        {
            // Empty - all segments dark
            foreach (var segment in segments)
            {
                segment.color = segmentEmptyColor;
                segment.gameObject.SetActive(true);
            }
        }
        else
        {
            // Charging - show progress
            float progress = cooldownTimer / cooldownTime;
            int activeSegments = Mathf.FloorToInt(progress * 6);

            for (int i = 0; i < 6; i++)
            {
                if (i < activeSegments)
                {
                    segments[i].color = segmentChargingColor;
                }
                else if (i == activeSegments)
                {
                    // Currently filling segment
                    float segmentProgress = (progress * 6) % 1;
                    segments[i].color = Color.Lerp(segmentEmptyColor, segmentChargingColor, segmentProgress);
                }
                else
                {
                    segments[i].color = segmentEmptyColor;
                }
            }
        }
    }

    // Public getters for UI state
    public float GetLightCooldownProgress() => lightMarkerCooldownTimer / lightMarkerCooldownTime;
    public float GetAreaCooldownProgress() => areaMarkerCooldownTimer / areaMarkerCooldownTime;
    public bool IsLightCharging() => lightCharges < lightMaxCharges;
    public bool IsAreaCharging() => areaCharges < areaMaxCharges;
}
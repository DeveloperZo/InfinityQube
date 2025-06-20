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
    [SerializeField] private Color segmentChargingColor = new Color(0.5f, 0.5f, 1f, 0.8f); // Blue when charging (has charges)
    [SerializeField] private Color segmentFirstChargeColor = new Color(0.7f, 0.7f, 1f, 0.6f); // Light blue when charging toward first charge
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

    void Update()
    {
        // Increment cooldown timers for visual feedback
        if (lightCharges < lightMaxCharges)
        {
            lightMarkerCooldownTimer += Time.deltaTime;
            lightMarkerCooldownTimer = Mathf.Min(lightMarkerCooldownTimer, lightMarkerCooldownTime);
        }
        else
        {
            lightMarkerCooldownTimer = 0f; // Ready to use immediately
        }

        if (areaCharges < areaMaxCharges)
        {
            areaMarkerCooldownTimer += Time.deltaTime;
            areaMarkerCooldownTimer = Mathf.Min(areaMarkerCooldownTimer, areaMarkerCooldownTime);
        }
        else
        {
            areaMarkerCooldownTimer = 0f; // Ready to use immediately
        }

        // Update display every frame for smooth animations
        UpdateDisplay();
    }
    public void UpdateCharges(int currentLightCharges, int currentAreaCharges)
    {
        lightCharges = currentLightCharges;
        areaCharges = currentAreaCharges;

        // Set max charges if not already set (defensive programming)
        if (lightMaxCharges == 0 && playerActionManager != null)
        {
            lightMaxCharges = playerActionManager.maxIndividualMarkerCharges;
        }
        if (areaMaxCharges == 0 && playerActionManager != null)
        {
            areaMaxCharges = playerActionManager.maxAreaMarkerCharges;
        }

        // Reset cooldown timer if charges are at max (ready to use)
        if (lightCharges >= lightMaxCharges)
            lightMarkerCooldownTimer = 0f;
        if (areaCharges >= areaMaxCharges)
            areaMarkerCooldownTimer = 0f;

        Debug.Log($"Charges updated - Light: {lightCharges}/{lightMaxCharges}, Area: {areaCharges}/{areaMaxCharges}");
    }

    public void SetMaxCharges(int maxLight, int maxArea)
    {
        lightMaxCharges = maxLight;
        areaMaxCharges = maxArea;
    }
    public void UpdateCooldowns(float individualCooldown, float areaCooldown)
    {
        lightMarkerCooldownTime = individualCooldown;
        areaMarkerCooldownTime = areaCooldown;
    }

    public void UpdateCooldownTimers(float lightTimer, float areaTimer)
    {
        lightMarkerCooldownTimer = lightTimer;
        areaMarkerCooldownTimer = areaTimer;
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
        // Increment cooldown timers for visual feedback (but don't exceed cooldown time)
        if (lightCharges < lightMaxCharges && lightMarkerCooldownTimer < lightMarkerCooldownTime)
        {
            lightMarkerCooldownTimer += Time.deltaTime;
        }

        if (areaCharges < areaMaxCharges && areaMarkerCooldownTimer < areaMarkerCooldownTime)
        {
            areaMarkerCooldownTimer += Time.deltaTime;
        }

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
        else if (charges == 0 && cooldownTimer <= 0.01f)
        {
            // No charges and not cooling down - all segments dark gray
            foreach (var segment in segments)
            {
                if (segment != null)
                {
                    segment.color = segmentEmptyColor;
                    segment.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            // Charging state - show progress with appropriate colors
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

    public void OnMarkerPlaced(bool isLightMarker)
    {
        if (isLightMarker)
        {
            // Start cooldown timer for individual marker
            lightMarkerCooldownTimer = 0f; // Reset timer to start counting
            Debug.Log($"Individual marker placed - cooldown timer reset");
        }
        else
        {
            // Start cooldown timer for area marker
            areaMarkerCooldownTimer = 0f; // Reset timer to start counting
            Debug.Log($"Area marker placed - cooldown timer reset");
        }
    }


}
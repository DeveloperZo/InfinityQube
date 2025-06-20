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
        if (lightCharges < lightMaxCharges)
        {
            lightMarkerCooldownTimer += Time.deltaTime;
            lightMarkerCooldownTimer = Mathf.Min(lightMarkerCooldownTimer, lightMarkerCooldownTime);
        }

        if (areaCharges < areaMaxCharges)
        {
            areaMarkerCooldownTimer += Time.deltaTime;
            areaMarkerCooldownTimer = Mathf.Min(areaMarkerCooldownTimer, areaMarkerCooldownTime);
        }
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
        // Update charge text - show available charges
        if (chargeText != null)
            chargeText.text = $"{charges}";

        // Handle visual states
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
            // Charging state - show progress
            float progress = Mathf.Clamp01(cooldownTimer / cooldownTime);
            int activeSegments = Mathf.FloorToInt(progress * segments.Length);

            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == null) continue;

                if (i < activeSegments)
                {
                    // Filled segments - blue charging color
                    segments[i].color = segmentChargingColor;
                }
                else if (i == activeSegments && progress < 1.0f)
                {
                    // Currently filling segment - lerp from empty to charging
                    float segmentProgress = (progress * segments.Length) % 1;
                    segments[i].color = Color.Lerp(segmentEmptyColor, segmentChargingColor, segmentProgress);
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
            // Start cooldown timer when charge is consumed
            if (lightCharges < lightMaxCharges)
            {
                lightMarkerCooldownTimer = 0f;
            }
        }
        else
        {
            // Start cooldown timer when charge is consumed  
            if (areaCharges < areaMaxCharges)
            {
                areaMarkerCooldownTimer = 0f;
            }
        }
    }

}
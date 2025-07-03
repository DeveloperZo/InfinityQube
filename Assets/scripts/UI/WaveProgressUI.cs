using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

/// <summary>
/// POC: Placeholder UI component for wave progress display.
/// Shows countdown timer, progress bar, and wave information during gameplay.
/// </summary>
public class WaveProgressUI : MonoBehaviour
{
    #region Inspector Configuration
    [Header("UI References")]
    [SerializeField] private Text countdownText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text waveNumberText;
    [SerializeField] private Text waveStatusText;
    [SerializeField] private GameObject progressPanel;
    
    [Header("Countdown Settings")]
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private bool showCountdown = true;
    [SerializeField] private string[] countdownMessages = { "3", "2", "1", "GO!" };
    
    [Header("Animation Settings")]
    [SerializeField] private float pulseScale = 1.5f;
    [SerializeField] private float pulseDuration = 0.3f;
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Progress Bar Settings")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private Color normalProgressColor = Color.green;
    [SerializeField] private Color dangerProgressColor = Color.red;
    [SerializeField] private float dangerThreshold = 0.25f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    #endregion

    #region Runtime State
    private int currentWaveIndex = -1;
    private float targetProgress = 0f;
    private Coroutine countdownCoroutine;
    private Image progressBarFill;
    private Vector3 originalCountdownScale;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeComponents();
        ValidateReferences();
    }

    private void OnEnable()
    {
        // Subscribe to GameEvents
        GameEvents.OnWaveStart += HandleWaveStart;
        GameEvents.OnWaveStep += HandleWaveStep;
        GameEvents.OnWaveComplete += HandleWaveComplete;
        GameEvents.OnWaveProgress += HandleWaveProgress;
        
        DebugLog("OnEnable", "Subscribed to wave events");
    }

    private void OnDisable()
    {
        // Unsubscribe from GameEvents (important to prevent memory leaks!)
        GameEvents.OnWaveStart -= HandleWaveStart;
        GameEvents.OnWaveStep -= HandleWaveStep;
        GameEvents.OnWaveComplete -= HandleWaveComplete;
        GameEvents.OnWaveProgress -= HandleWaveProgress;
        
        // Stop any running coroutines
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        
        DebugLog("OnDisable", "Unsubscribed from wave events");
    }

    private void Update()
    {
        // Smooth progress bar updates
        if (progressBar != null && Math.Abs(progressBar.value - targetProgress) > 0.01f)
        {
            progressBar.value = Mathf.Lerp(progressBar.value, targetProgress, Time.deltaTime * smoothSpeed);
            UpdateProgressBarColor();
        }
    }
    #endregion

    #region Initialization
    private void InitializeComponents()
    {
        // Get progress bar fill image for color changes
        if (progressBar != null)
        {
            var fillArea = progressBar.transform.Find("Fill Area/Fill");
            if (fillArea != null)
            {
                progressBarFill = fillArea.GetComponent<Image>();
            }
        }
        
        // Store original countdown scale for animation
        if (countdownText != null)
        {
            originalCountdownScale = countdownText.transform.localScale;
        }
        
        // Hide progress panel initially
        if (progressPanel != null)
        {
            progressPanel.SetActive(false);
        }
    }
    
    private void ValidateReferences()
    {
        if (countdownText == null)
            Debug.LogWarning("[WaveProgressUI] CountdownText not assigned - countdown won't display");
            
        if (progressBar == null)
            Debug.LogWarning("[WaveProgressUI] ProgressBar not assigned - progress won't be tracked");
            
        if (waveNumberText == null)
            Debug.LogWarning("[WaveProgressUI] WaveNumberText not assigned - wave number won't display");
    }
    #endregion

    #region Event Handlers
    private void HandleWaveStart(int waveIndex, WaveData waveData)
    {
        DebugLog("HandleWaveStart", $"Wave {waveIndex} started");
        
        currentWaveIndex = waveIndex;
        
        // Update wave number display
        if (waveNumberText != null)
        {
            waveNumberText.text = $"Wave {waveIndex + 1}";
        }
        
        // Reset progress
        targetProgress = 0f;
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }
        
        // Show progress panel
        if (progressPanel != null)
        {
            progressPanel.SetActive(true);
        }
        
        // Update status
        if (waveStatusText != null)
        {
            waveStatusText.text = "Wave Starting...";
        }
        
        // Start countdown if enabled
        if (showCountdown && countdownText != null)
        {
            if (countdownCoroutine != null)
            {
                StopCoroutine(countdownCoroutine);
            }
            countdownCoroutine = StartCoroutine(ShowCountdown());
        }
    }
    
    private void HandleWaveStep(int waveIndex, int stepNumber)
    {
        if (waveIndex != currentWaveIndex) return;
        
        DebugLog("HandleWaveStep", $"Wave {waveIndex} - Step {stepNumber}");
        
        // Update status to show active wave
        if (waveStatusText != null && stepNumber == 1)
        {
            waveStatusText.text = "Wave Active";
        }
        
        // POC: Simple step tracking - can be enhanced to show cube positions later
    }
    
    private void HandleWaveProgress(int waveIndex, float progressPercent)
    {
        if (waveIndex != currentWaveIndex) return;
        
        DebugLog("HandleWaveProgress", $"Wave {waveIndex} - Progress {progressPercent:F1}%");
        
        // Update progress bar target
        targetProgress = progressPercent / 100f;
    }
    
    private void HandleWaveComplete(int waveIndex)
    {
        if (waveIndex != currentWaveIndex) return;
        
        DebugLog("HandleWaveComplete", $"Wave {waveIndex} completed");
        
        // Set progress to 100%
        targetProgress = 1f;
        
        // Update status
        if (waveStatusText != null)
        {
            waveStatusText.text = "Wave Complete!";
        }
        
        // POC: Simple completion display - can add celebration effects later
        StartCoroutine(ShowCompletionAndHide());
    }
    #endregion

    #region UI Animations
    private IEnumerator ShowCountdown()
    {
        countdownText.gameObject.SetActive(true);
        
        // Calculate time per countdown step
        float timePerStep = countdownDuration / countdownMessages.Length;
        
        for (int i = 0; i < countdownMessages.Length; i++)
        {
            // Update countdown text
            countdownText.text = countdownMessages[i];
            
            // Pulse animation
            yield return StartCoroutine(PulseCountdown());
            
            // Wait before next number (except for the last one)
            if (i < countdownMessages.Length - 1)
            {
                yield return new WaitForSeconds(timePerStep - pulseDuration);
            }
        }
        
        // Hide countdown after "GO!"
        yield return new WaitForSeconds(0.5f);
        countdownText.gameObject.SetActive(false);
        
        countdownCoroutine = null;
    }
    
    private IEnumerator PulseCountdown()
    {
        if (countdownText == null) yield break;
        
        float elapsed = 0f;
        Transform textTransform = countdownText.transform;
        
        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / pulseDuration;
            float curveValue = pulseCurve.Evaluate(t);
            
            // Scale based on curve
            float scale = 1f + (pulseScale - 1f) * curveValue;
            textTransform.localScale = originalCountdownScale * scale;
            
            yield return null;
        }
        
        // Reset scale
        textTransform.localScale = originalCountdownScale;
    }
    
    private IEnumerator ShowCompletionAndHide()
    {
        // Wait a moment to show completion
        yield return new WaitForSeconds(2f);
        
        // POC: Simple hide - can add fade animation later
        if (progressPanel != null)
        {
            progressPanel.SetActive(false);
        }
    }
    
    private void UpdateProgressBarColor()
    {
        if (progressBarFill == null) return;
        
        // Change color based on progress (danger when low progress)
        Color targetColor = progressBar.value < dangerThreshold ? dangerProgressColor : normalProgressColor;
        progressBarFill.color = Color.Lerp(progressBarFill.color, targetColor, Time.deltaTime * smoothSpeed);
    }
    #endregion

    #region Helper Methods
    private void DebugLog(string methodName, string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[WaveProgressUI] {methodName}: {message}");
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Force hide all UI elements (useful for cleanup or testing)
    /// </summary>
    public void HideUI()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
        
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
        
        if (progressPanel != null)
        {
            progressPanel.SetActive(false);
        }
        
        targetProgress = 0f;
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }
        
        DebugLog("HideUI", "All UI elements hidden");
    }
    
    /// <summary>
    /// Test the countdown sequence
    /// </summary>
    [ContextMenu("Test Countdown")]
    public void TestCountdown()
    {
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
        }
        countdownCoroutine = StartCoroutine(ShowCountdown());
    }
    
    /// <summary>
    /// Test wave progress
    /// </summary>
    [ContextMenu("Test Wave Progress")]
    public void TestWaveProgress()
    {
        HandleWaveStart(0, null);
        StartCoroutine(SimulateWaveProgress());
    }
    
    private IEnumerator SimulateWaveProgress()
    {
        float progress = 0f;
        while (progress < 100f)
        {
            progress += 10f;
            HandleWaveProgress(0, progress);
            yield return new WaitForSeconds(0.5f);
        }
        HandleWaveComplete(0);
    }
    #endregion
}

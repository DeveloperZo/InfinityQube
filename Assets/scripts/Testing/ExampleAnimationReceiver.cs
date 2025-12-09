using UnityEngine;
using static Enumerations;

/// <summary>
/// Example implementation of IAnimationTriggerReceiver for testing animation trigger points.
/// Demonstrates how to receive and respond to animation trigger events from AnimationTriggerManager.
/// Can be used as a template for creating custom animation receivers.
/// </summary>
public class ExampleAnimationReceiver : MonoBehaviour, IAnimationTriggerReceiver
{
    [Header("Example Animation Settings")]
    [SerializeField] private bool isActive = true;
    [SerializeField] private bool logReceivedTriggers = true;
    [SerializeField] private bool visualizeTriggers = true;
    [SerializeField] private float visualEffectDuration = 1.0f;
    
    [Header("Visual Effects")]
    [SerializeField] private ParticleSystem effectParticles;
    [SerializeField] private Light effectLight;
    [SerializeField] private AudioSource effectAudio;
    [SerializeField] private Renderer targetRenderer;
    
    [Header("Animation Components")]
    [SerializeField] private Animator animator;
    [SerializeField] private Animation legacyAnimation;
    
    // State tracking
    private int triggersReceived = 0;
    private AnimationTriggerPoint lastTriggerType;
    private float lastTriggerTime;
    private Material originalMaterial;
    private Color originalLightColor;
    
    #region Unity Lifecycle
    
    private void Start()
    {
        InitializeExampleReceiver();
        RegisterWithAnimationManager();
        CacheOriginalValues();
    }
    
    private void OnDestroy()
    {
        UnregisterFromAnimationManager();
    }
    
    private void InitializeExampleReceiver()
    {
        // Auto-find components if not assigned
        if (effectParticles == null)
            effectParticles = GetComponentInChildren<ParticleSystem>();
            
        if (effectLight == null)
            effectLight = GetComponentInChildren<Light>();
            
        if (effectAudio == null)
            effectAudio = GetComponentInChildren<AudioSource>();
            
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<Renderer>();
            
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
            
        if (legacyAnimation == null)
            legacyAnimation = GetComponentInChildren<Animation>();
    }
    
    private void RegisterWithAnimationManager()
    {
        var animationManager = FindFirstObjectByType<AnimationTriggerManager>();
        if (animationManager != null)
        {
            // Register for all trigger types for demonstration
            animationManager.RegisterReceiverForAllTriggers(this);
            
            if (logReceivedTriggers)
            {
                Debug.Log($"[ExampleAnimationReceiver] Registered with AnimationTriggerManager: {animationManager.name}");
            }
        }
        else
        {
            Debug.LogWarning("[ExampleAnimationReceiver] AnimationTriggerManager not found - triggers will not be received");
        }
    }
    
    private void UnregisterFromAnimationManager()
    {
        var animationManager = FindFirstObjectByType<AnimationTriggerManager>();
        if (animationManager != null)
        {
            animationManager.UnregisterReceiver(this);
        }
    }
    
    private void CacheOriginalValues()
    {
        // Cache original values for restoration
        if (targetRenderer != null && targetRenderer.material != null)
        {
            originalMaterial = targetRenderer.material;
        }
        
        if (effectLight != null)
        {
            originalLightColor = effectLight.color;
        }
    }
    
    #endregion
    
    #region IAnimationTriggerReceiver Implementation
    
    public void OnAnimationTrigger(AnimationTriggerPoint triggerPoint, AnimationTriggerContext context)
    {
        if (!isActive)
            return;
            
        // Update statistics
        triggersReceived++;
        lastTriggerType = triggerPoint;
        lastTriggerTime = Time.time;
        
        // Log the trigger event
        if (logReceivedTriggers)
        {
            Debug.Log($"[ExampleAnimationReceiver] Received trigger: {triggerPoint} at position {context.primaryPosition} with intensity {context.intensity:F2}");
            
            if (!string.IsNullOrEmpty(context.additionalData))
            {
                Debug.Log($"[ExampleAnimationReceiver] Additional data: {context.additionalData}");
            }
        }
        
        // Handle the trigger based on type
        HandleTriggerByType(triggerPoint, context);
        
        // Apply visual effects if enabled
        if (visualizeTriggers)
        {
            StartVisualEffect(triggerPoint, context);
        }
        
        // Trigger Unity animations if available
        TriggerUnityAnimations(triggerPoint, context);
    }
    
    public string GetReceiverName()
    {
        return $"ExampleAnimationReceiver ({gameObject.name})";
    }
    
    public bool IsReceiverActive()
    {
        return isActive && gameObject.activeInHierarchy;
    }
    
    #endregion
    
    #region Trigger Handling
    
    private void HandleTriggerByType(AnimationTriggerPoint triggerPoint, AnimationTriggerContext context)
    {
        switch (triggerPoint)
        {
            case AnimationTriggerPoint.ModeSwitch:
                HandleModeSwitchTrigger(context);
                break;
                
            case AnimationTriggerPoint.MarkerPlace:
                HandleMarkerPlaceTrigger(context);
                break;
                
            case AnimationTriggerPoint.MarkerTrigger:
                HandleMarkerTriggerTrigger(context);
                break;
                
            case AnimationTriggerPoint.UIUpdate:
                HandleUIUpdateTrigger(context);
                break;
                
            case AnimationTriggerPoint.ActionFailed:
                HandleActionFailedTrigger(context);
                break;
                
            case AnimationTriggerPoint.ActionSuccess:
                HandleActionSuccessTrigger(context);
                break;
                
            case AnimationTriggerPoint.CubeMarkerAction:
                HandleCubeMarkerActionTrigger(context);
                break;
                
            case AnimationTriggerPoint.ResourceRegeneration:
                HandleResourceRegenerationTrigger(context);
                break;
                
            default:
                Debug.LogWarning($"[ExampleAnimationReceiver] Unhandled trigger type: {triggerPoint}");
                break;
        }
    }
    
    private void HandleModeSwitchTrigger(AnimationTriggerContext context)
    {
        // Example: Change color based on marker mode
        if (targetRenderer != null && targetRenderer.material != null)
        {
            Color modeColor = GetModeColor(context.markerMode);
            StartCoroutine(FlashColor(modeColor, context.duration));
        }
        
        // Example: Play mode switch sound
        PlayAudioEffect(0.5f);
    }
    
    private void HandleMarkerPlaceTrigger(AnimationTriggerContext context)
    {
        // Example: Create placement effect at marker position
        CreateEffectAtPosition(context.primaryPosition, context.intensity);
        
        // Example: Scale pulse effect
        StartCoroutine(ScalePulse(1.2f, context.duration));
    }
    
    private void HandleMarkerTriggerTrigger(AnimationTriggerContext context)
    {
        // Example: Create explosion-like effect
        CreateExplosionEffect(context.primaryPosition, context.intensity);
        
        // Example: Bright flash effect
        StartCoroutine(FlashLight(context.intensity, context.duration));
    }
    
    private void HandleUIUpdateTrigger(AnimationTriggerContext context)
    {
        // Example: Subtle UI animation response
        StartCoroutine(SubtleGlow(context.intensity * 0.5f, context.duration));
    }
    
    private void HandleActionFailedTrigger(AnimationTriggerContext context)
    {
        // Example: Red flash for errors
        StartCoroutine(FlashColor(Color.red, context.duration));
        
        // Example: Error sound
        PlayAudioEffect(0.8f, true);
    }
    
    private void HandleActionSuccessTrigger(AnimationTriggerContext context)
    {
        // Example: Green flash for success
        StartCoroutine(FlashColor(Color.green, context.duration));
        
        // Example: Success sound
        PlayAudioEffect(0.6f);
    }
    
    private void HandleCubeMarkerActionTrigger(AnimationTriggerContext context)
    {
        // Example: Special cube marker effect
        CreateEffectAtPosition(context.primaryPosition, context.intensity * 1.5f);
        
        // Example: Unique cube marker animation
        StartCoroutine(RotationEffect(context.duration));
    }
    
    private void HandleResourceRegenerationTrigger(AnimationTriggerContext context)
    {
        // Example: Gentle regeneration glow
        StartCoroutine(RegenerationGlow(context.intensity, context.duration));
    }
    
    #endregion
    
    #region Visual Effects
    
    private void StartVisualEffect(AnimationTriggerPoint triggerPoint, AnimationTriggerContext context)
    {
        // Create particle effect if available
        if (effectParticles != null)
        {
            var main = effectParticles.main;
            main.startLifetime = context.duration;
            main.startColor = GetTriggerColor(triggerPoint);
            main.startSpeed = context.intensity * 10f;
            
            effectParticles.Play();
        }
    }
    
    private void CreateEffectAtPosition(Vector3 position, float intensity)
    {
        // Example: Create temporary effect at world position
        if (visualizeTriggers)
        {
            Debug.DrawRay(position, Vector3.up * intensity, Color.yellow, visualEffectDuration);
        }
    }
    
    private void CreateExplosionEffect(Vector3 position, float intensity)
    {
        // Example: Create explosion-like visual effect
        if (effectParticles != null)
        {
            var main = effectParticles.main;
            main.startSpeed = intensity * 15f;
            main.startSize = intensity * 2f;
            effectParticles.transform.position = position;
            effectParticles.Play();
        }
    }
    
    private void PlayAudioEffect(float volume, bool isError = false)
    {
        if (effectAudio != null && effectAudio.clip != null)
        {
            effectAudio.volume = volume;
            effectAudio.pitch = isError ? 0.8f : 1.0f;
            effectAudio.Play();
        }
    }
    
    #endregion
    
    #region Animation Coroutines
    
    private System.Collections.IEnumerator FlashColor(Color targetColor, float duration)
    {
        if (targetRenderer == null || targetRenderer.material == null)
            yield break;
            
        Color originalColor = targetRenderer.material.color;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float flash = Mathf.Sin(t * Mathf.PI * 2f) * 0.5f + 0.5f;
            targetRenderer.material.color = Color.Lerp(originalColor, targetColor, flash);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        targetRenderer.material.color = originalColor;
    }
    
    private System.Collections.IEnumerator ScalePulse(float maxScale, float duration)
    {
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float scale = Mathf.Lerp(1f, maxScale, Mathf.Sin(t * Mathf.PI));
            transform.localScale = originalScale * scale;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    private System.Collections.IEnumerator FlashLight(float intensity, float duration)
    {
        if (effectLight == null)
            yield break;
            
        float originalIntensity = effectLight.intensity;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float flash = Mathf.Sin(t * Mathf.PI * 3f) * intensity;
            effectLight.intensity = originalIntensity + flash;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        effectLight.intensity = originalIntensity;
    }
    
    private System.Collections.IEnumerator SubtleGlow(float intensity, float duration)
    {
        if (effectLight == null)
            yield break;
            
        float originalIntensity = effectLight.intensity;
        float targetIntensity = originalIntensity + intensity;
        float elapsed = 0f;
        
        // Fade in
        while (elapsed < duration * 0.3f)
        {
            float t = elapsed / (duration * 0.3f);
            effectLight.intensity = Mathf.Lerp(originalIntensity, targetIntensity, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Hold
        yield return new WaitForSeconds(duration * 0.4f);
        
        // Fade out
        elapsed = 0f;
        while (elapsed < duration * 0.3f)
        {
            float t = elapsed / (duration * 0.3f);
            effectLight.intensity = Mathf.Lerp(targetIntensity, originalIntensity, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        effectLight.intensity = originalIntensity;
    }
    
    private System.Collections.IEnumerator RotationEffect(float duration)
    {
        float elapsed = 0f;
        Vector3 originalRotation = transform.eulerAngles;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float rotation = t * 360f;
            transform.eulerAngles = originalRotation + Vector3.up * rotation;
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.eulerAngles = originalRotation;
    }
    
    private System.Collections.IEnumerator RegenerationGlow(float intensity, float duration)
    {
        if (effectLight == null)
            yield break;
            
        Color originalColor = effectLight.color;
        Color glowColor = Color.green;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float glow = Mathf.Sin(t * Mathf.PI) * intensity;
            effectLight.color = Color.Lerp(originalColor, glowColor, glow);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        effectLight.color = originalColor;
    }
    
    #endregion
    
    #region Unity Animation Integration
    
    private void TriggerUnityAnimations(AnimationTriggerPoint triggerPoint, AnimationTriggerContext context)
    {
        // Trigger Animator if available
        if (animator != null && animator.isActiveAndEnabled)
        {
            string triggerName = GetAnimatorTriggerName(triggerPoint);
            if (!string.IsNullOrEmpty(triggerName))
            {
                try
                {
                    animator.SetTrigger(triggerName);
                    
                    // Set context parameters if they exist
                    SetAnimatorContextParameters(context);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[ExampleAnimationReceiver] Failed to trigger animator: {e.Message}");
                }
            }
        }
        
        // Trigger Legacy Animation if available
        if (legacyAnimation != null && legacyAnimation.isActiveAndEnabled)
        {
            string animationName = GetLegacyAnimationName(triggerPoint);
            if (!string.IsNullOrEmpty(animationName) && legacyAnimation[animationName] != null)
            {
                legacyAnimation.Play(animationName);
            }
        }
    }
    
    private string GetAnimatorTriggerName(AnimationTriggerPoint triggerPoint)
    {
        // Map trigger points to animator trigger names
        switch (triggerPoint)
        {
            case AnimationTriggerPoint.ModeSwitch: return "ModeSwitch";
            case AnimationTriggerPoint.MarkerPlace: return "MarkerPlace";
            case AnimationTriggerPoint.MarkerTrigger: return "MarkerTrigger";
            case AnimationTriggerPoint.ActionFailed: return "ActionFailed";
            case AnimationTriggerPoint.ActionSuccess: return "ActionSuccess";
            default: return null;
        }
    }
    
    private string GetLegacyAnimationName(AnimationTriggerPoint triggerPoint)
    {
        // Map trigger points to legacy animation clip names
        switch (triggerPoint)
        {
            case AnimationTriggerPoint.ModeSwitch: return "ModeSwitch";
            case AnimationTriggerPoint.MarkerPlace: return "PlaceMarker";
            case AnimationTriggerPoint.MarkerTrigger: return "TriggerMarker";
            default: return null;
        }
    }
    
    private void SetAnimatorContextParameters(AnimationTriggerContext context)
    {
        if (animator == null)
            return;
            
        // Set context parameters if they exist in the animator
        foreach (var parameter in animator.parameters)
        {
            switch (parameter.name)
            {
                case "TriggerIntensity":
                    if (parameter.type == AnimatorControllerParameterType.Float)
                        animator.SetFloat("TriggerIntensity", context.intensity);
                    break;
                    
                case "MarkerMode":
                    if (parameter.type == AnimatorControllerParameterType.Int)
                        animator.SetInteger("MarkerMode", (int)context.markerMode);
                    break;
                    
                case "TriggerDuration":
                    if (parameter.type == AnimatorControllerParameterType.Float)
                        animator.SetFloat("TriggerDuration", context.duration);
                    break;
            }
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    private Color GetModeColor(MarkerMode mode)
    {
        switch (mode)
        {
            case MarkerMode.Unit: return Color.yellow;
            case MarkerMode.Matrix: return Color.magenta;
            case MarkerMode.Recursion: return Color.red;
            default: return Color.white;
        }
    }
    
    private Color GetTriggerColor(AnimationTriggerPoint triggerPoint)
    {
        switch (triggerPoint)
        {
            case AnimationTriggerPoint.ModeSwitch: return Color.blue;
            case AnimationTriggerPoint.MarkerPlace: return Color.green;
            case AnimationTriggerPoint.MarkerTrigger: return Color.red;
            case AnimationTriggerPoint.UIUpdate: return Color.yellow;
            case AnimationTriggerPoint.ActionFailed: return Color.magenta;
            case AnimationTriggerPoint.ActionSuccess: return Color.cyan;
            case AnimationTriggerPoint.CubeMarkerAction: return Color.white;
            case AnimationTriggerPoint.ResourceRegeneration: return new Color(0.5f, 1f, 0.5f);
            default: return Color.gray;
        }
    }
    
    #endregion
    
    #region Public Interface for Testing
    
    /// <summary>
    /// Public method to manually test trigger reception
    /// </summary>
    /// <param name="triggerPoint">Trigger point to test</param>
    public void TestTrigger(AnimationTriggerPoint triggerPoint)
    {
        var context = AnimationTriggerContext.Create(transform.position, 1.0f);
        OnAnimationTrigger(triggerPoint, context);
    }
    
    /// <summary>
    /// Gets statistics about triggers received
    /// </summary>
    /// <returns>Formatted string with trigger statistics</returns>
    public string GetStatistics()
    {
        return $"Triggers received: {triggersReceived}, Last: {lastTriggerType} at {lastTriggerTime:F1}s";
    }
    
    /// <summary>
    /// Enables or disables this receiver
    /// </summary>
    /// <param name="active">Whether the receiver should be active</param>
    public void SetActive(bool active)
    {
        isActive = active;
    }
    
    #endregion
}

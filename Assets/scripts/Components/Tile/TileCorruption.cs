using UnityEngine;
using System.Collections;

/// <summary>
/// Handles tile corruption mechanics including duration, interactions, and visual effects
/// </summary>
public class TileCorruption
{
    #region Configuration
    private bool showCorruptionCountdown = true;
    #endregion

    #region Runtime State
    private bool isCorrupted = false;
    private int corruptionDuration = 5;
    private int maxCorruptionInteractions = 3;
    private int corruptionInteractions = 0;
    private int corruptionDecayCount = 0;
    
    private GameObject corruptionEffect;
    private TextMesh countdownText;
    private Transform parentTransform;
    private Tile parentTile;
    private bool enableDebugLogs;
    #endregion

    #region Properties
    public bool IsCorrupted => isCorrupted;
    public int CorruptionInteractions => corruptionInteractions;
    public int MaxCorruptionInteractions => maxCorruptionInteractions;
    #endregion

    #region Constructor
    public TileCorruption(Transform tileTransform, Tile tile, bool showCountdown = true, bool debugLogs = false)
    {
        parentTransform = tileTransform;
        parentTile = tile;
        showCorruptionCountdown = showCountdown;
        enableDebugLogs = debugLogs;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Corrupts the tile for a specified duration with interaction limits
    /// </summary>
    public void CorruptTile(int duration = 5, int maxInteractions = 3)
    {
        if (isCorrupted) return; // Already corrupted
        
        isCorrupted = true;
        corruptionDuration = duration;
        maxCorruptionInteractions = maxInteractions;
        corruptionInteractions = 0;
        corruptionDecayCount = 0;
        
        // Create corruption visual effects
        CreateCorruptionEffect();
        
        DebugLog($"Tile {parentTransform.name} corrupted with duration {duration} and max interactions {maxInteractions}");
    }
    
    /// <summary>
    /// Cleanses corruption from the tile
    /// </summary>
    public void CleanseCorruption()
    {
        if (!isCorrupted) return;
        
        isCorrupted = false;
        corruptionDuration = 0;
        corruptionInteractions = 0;
        corruptionDecayCount = 0;
        
        // Remove corruption effects
        RemoveCorruptionEffect();
        
        DebugLog($"Tile {parentTransform.name} corruption cleansed");
    }
    
    /// <summary>
    /// Processes corruption decay each move
    /// </summary>
    public void ProcessCorruptionDecay()
    {
        if (!isCorrupted || corruptionDuration == -1) return;
        
        corruptionDecayCount++;
        
        // Update countdown display
        UpdateCorruptionCountdown();
        
        // Check if corruption duration has expired
        if (corruptionDecayCount >= corruptionDuration)
        {
            CleanseCorruption();
            DebugLog($"Tile {parentTransform.name} corruption expired after {corruptionDecayCount} moves");
        }
    }
    
    /// <summary>
    /// Increments corruption interaction count
    /// </summary>
    public void IncrementInteraction()
    {
        corruptionInteractions++;
        DebugLog($"Corruption interaction {corruptionInteractions}/{maxCorruptionInteractions}");
    }
    
    /// <summary>
    /// Checks if corruption should end due to interaction limit
    /// </summary>
    public bool ShouldCleanseFromInteractions()
    {
        return corruptionInteractions >= maxCorruptionInteractions;
    }
    
    /// <summary>
    /// Gets corruption status information
    /// </summary>
    public string GetCorruptionStatus()
    {
        if (!isCorrupted) return "Clean";
        
        int remaining = corruptionDuration == -1 ? -1 : corruptionDuration - corruptionDecayCount;
        return $"Corrupted: {corruptionInteractions}/{maxCorruptionInteractions} painted, {remaining} moves left";
    }
    
    /// <summary>
    /// Cleanup when tile is destroyed
    /// </summary>
    public void OnDestroy()
    {
        RemoveCorruptionEffect();
    }
    
    /// <summary>
    /// Stop any running coroutines (must be called from MonoBehaviour)
    /// </summary>
    public void StopCorruptionEffects()
    {
        if (corruptionEffect != null)
        {
            parentTile.StopCoroutine(PulseCorruptionEffect());
        }
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Creates visual corruption effect
    /// </summary>
    private void CreateCorruptionEffect()
    {
        if (corruptionEffect != null) return;
        
        // Create corruption particle effect
        corruptionEffect = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        corruptionEffect.name = $"CorruptionEffect_{parentTransform.name}";
        corruptionEffect.transform.SetParent(parentTransform);
        corruptionEffect.transform.localPosition = new Vector3(0, 0.53f, 0); // Just above overlay height
        corruptionEffect.transform.localScale = new Vector3(0.8f, 0.01f, 0.8f);
        
        // Remove collider
        Object.Destroy(corruptionEffect.GetComponent<Collider>());
        
        // Set corruption material with pulsing effect
        Renderer effectRenderer = corruptionEffect.GetComponent<Renderer>();
        if (effectRenderer != null)
        {
            Material corruptionMaterial = new Material(Shader.Find("Standard"));
            corruptionMaterial.color = new Color(0.5f, 0f, 0.5f, 0.8f); // Dark purple
            corruptionMaterial.EnableKeyword("_EMISSION");
            corruptionMaterial.SetColor("_EmissionColor", new Color(0.5f, 0f, 0.5f) * 0.5f);
            effectRenderer.material = corruptionMaterial;
        }
        
        // Start pulsing animation
        parentTile.StartCoroutine(PulseCorruptionEffect());
        
        // Create countdown text if enabled
        if (showCorruptionCountdown)
        {
            CreateCorruptionCountdown();
        }
    }
    
    /// <summary>
    /// Removes corruption visual effect
    /// </summary>
    private void RemoveCorruptionEffect()
    {
        if (corruptionEffect != null)
        {
            StopCorruptionEffects();
            Object.Destroy(corruptionEffect);
            corruptionEffect = null;
        }
        
        RemoveCorruptionCountdown();
    }
    
    /// <summary>
    /// Pulsing animation for corruption effect
    /// </summary>
    private IEnumerator PulseCorruptionEffect()
    {
        if (corruptionEffect == null) yield break;
        
        Vector3 baseScale = corruptionEffect.transform.localScale;
        Vector3 pulseScale = baseScale * 1.2f;
        
        while (isCorrupted && corruptionEffect != null)
        {
            // Pulse up
            float elapsed = 0f;
            float duration = 1f;
            
            while (elapsed < duration && corruptionEffect != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Sin(elapsed / duration * Mathf.PI);
                corruptionEffect.transform.localScale = Vector3.Lerp(baseScale, pulseScale, t * 0.3f);
                yield return null;
            }
            
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    /// <summary>
    /// Creates countdown text for corruption duration
    /// </summary>
    private void CreateCorruptionCountdown()
    {
        if (corruptionDuration == -1) return; // No countdown for permanent corruption
        
        GameObject countdownObj = new GameObject($"CorruptionCountdown_{parentTransform.name}");
        countdownObj.transform.SetParent(parentTransform);
        countdownObj.transform.localPosition = new Vector3(0, 1.01f, 0); // Above tile
        
        countdownText = countdownObj.AddComponent<TextMesh>();
        countdownText.text = (corruptionDuration - corruptionDecayCount).ToString();
        countdownText.fontSize = 10;
        countdownText.color = Color.red;
        countdownText.anchor = TextAnchor.MiddleCenter;
        
        // Make text face camera
        if (Camera.main != null)
        {
            countdownObj.transform.LookAt(Camera.main.transform);
            countdownObj.transform.Rotate(0, 180, 0);
        }
    }
    
    /// <summary>
    /// Updates corruption countdown display
    /// </summary>
    private void UpdateCorruptionCountdown()
    {
        if (countdownText != null && corruptionDuration != -1)
        {
            int remaining = corruptionDuration - corruptionDecayCount;
            countdownText.text = remaining.ToString();
            
            // Change color as time runs out
            if (remaining <= 1)
                countdownText.color = Color.red;
            else if (remaining <= 2)
                countdownText.color = Color.yellow;
            else
                countdownText.color = Color.white;
        }
    }
    
    /// <summary>
    /// Removes corruption countdown display
    /// </summary>
    private void RemoveCorruptionCountdown()
    {
        if (countdownText != null)
        {
            Object.Destroy(countdownText.gameObject);
            countdownText = null;
        }
    }
    
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[TileCorruption] {message}");
        }
    }
    #endregion
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using static Enumerations;

/// <summary>
/// Simple message data structure for message-only display (no highlight)
/// </summary>
[System.Serializable]
public class SimpleMessage
{
    public string text;
    public bool requirePause;
    public float autoHideDelay;
    public int moveStep; // For statistics tracking
}

/// <summary>
/// Manages message and highlight sequences: pause → message → highlight → resume.
/// Executes sequences that combine messaging and visual highlighting
/// to provide contextual guidance to players.
/// </summary>
public class MessageHighlightManager : MonoBehaviour, IManagerDebugInterface
{
    #region Inspector Configuration
    
    [Header("Highlight Settings")]
    [SerializeField] private Color capturableCubeColor = new Color(0.3f, 0.8f, 0.3f, 0.4f); // Subtle green
    [SerializeField] private Color infinityCubeColor = new Color(0.8f, 0.2f, 0.2f, 0.4f); // Subtle red
    [SerializeField] private float highlightPulseSpeed = 1.5f;
    [SerializeField] private float highlightPulseIntensity = 0.2f;
    [SerializeField] private float highlightEmissionIntensity = 0.3f; // Much more subtle emission
    
    [Header("Auto-Highlight Settings")]
    [SerializeField] private bool enableAutoHighlighting = false; // Disabled by default - use highlight sequences instead
    [SerializeField] private bool autoHighlightFirstCapturable = true;
    [SerializeField] private bool autoHighlightInfinityCubes = true;
    [SerializeField] private float autoHighlightDuration = 3f;
    [SerializeField] private float messageToHighlightDelay = 1.5f; // Wait after message before highlighting
    [SerializeField] private bool sequenceHighlights = true; // Highlight one at a time
    [SerializeField] private float highlightSequenceDelay = 2f; // Delay between sequenced highlights
    
    [Header("Debug")]
    [Tooltip("Enable debug logging for this manager")]
    [SerializeField] private bool enableDebugLogs = true;
    
    #endregion
    
    #region Manager References
    
    private WaveManager waveManager;
    private GridManager gridManager;
    private MarkerVisualManager markerVisualManager;
    private StageManager stageManager;
    private PlayerActionManager playerActionManager;
    
    // Message system references
    private GameObject messagePanel;
    private TextMeshProUGUI messageTextUI;
    private GameObject continuePrompt;
    
    // Message queue for simple messages
    private Queue<SimpleMessage> pendingMessages = new Queue<SimpleMessage>();
    private bool isProcessingMessageQueue = false;
    
    #endregion
    
    #region Runtime State
    
    // Active highlights - track by cube instance ID since cubes move (position changes)
    private Dictionary<int, CubeManager> highlightedCubes = new Dictionary<int, CubeManager>(); // Key: cube.GetInstanceID()
    private Dictionary<int, Material> originalMaterials = new Dictionary<int, Material>(); // Key: cube.GetInstanceID()
    private Dictionary<int, Coroutine> pulseCoroutines = new Dictionary<int, Coroutine>(); // Key: cube.GetInstanceID()
    
    // Tile highlights (for marker placement guidance)
    private Dictionary<Vector2Int, GameObject> highlightedTiles = new Dictionary<Vector2Int, GameObject>();
    
    // Sequence tracking
    private Dictionary<int, HighlightSequence> activeSequences = new Dictionary<int, HighlightSequence>(); // Key: cube.GetInstanceID() or position hash
    private bool isPaused = false;
    
    // Validation tracking
    private Dictionary<Vector2Int, HighlightSequence> pendingValidations = new Dictionary<Vector2Int, HighlightSequence>(); // Key: required marker position
    private bool isWavePausedForValidation = false;
    
    // Configuration state (for ResetToDefaults)
    private Color defaultCapturableColor;
    private Color defaultInfinityColor;
    private float defaultPulseSpeed;
    private float defaultPulseIntensity;
    private float defaultHighlightEmissionIntensity;
    private bool defaultEnableAutoHighlighting;
    private bool defaultAutoHighlightFirstCapturable;
    private bool defaultAutoHighlightInfinityCubes;
    private float defaultAutoHighlightDuration;
    
    #endregion
    
    #region Properties
    
    public bool EnableDebugLogs 
    { 
        get => enableDebugLogs; 
        set => enableDebugLogs = value; 
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    private void Awake()
    {
        EnableDebugLogs = enableDebugLogs;
        StoreDefaultValues();
    }
    
    private void Start()
    {
        CacheManagerReferences();
        SubscribeToEvents();
    }
    
    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        ClearAllHighlights();
    }
    
    #endregion
    
    #region Initialization
    
    private void StoreDefaultValues()
    {
        defaultCapturableColor = capturableCubeColor;
        defaultInfinityColor = infinityCubeColor;
        defaultPulseSpeed = highlightPulseSpeed;
        defaultPulseIntensity = highlightPulseIntensity;
        defaultHighlightEmissionIntensity = highlightEmissionIntensity;
        defaultEnableAutoHighlighting = enableAutoHighlighting;
        defaultAutoHighlightFirstCapturable = autoHighlightFirstCapturable;
        defaultAutoHighlightInfinityCubes = autoHighlightInfinityCubes;
        defaultAutoHighlightDuration = autoHighlightDuration;
    }
    
    private void CacheManagerReferences()
    {
        waveManager = FindFirstObjectByType<WaveManager>();
        gridManager = GridManager.Instance;
        markerVisualManager = FindFirstObjectByType<MarkerVisualManager>();
        stageManager = FindFirstObjectByType<StageManager>();
        playerActionManager = FindFirstObjectByType<PlayerActionManager>();
        
        // Get message system references from WaveManager
        if (waveManager != null)
        {
            messagePanel = waveManager.messagePanel;
            messageTextUI = waveManager.messageText;
            continuePrompt = waveManager.continuePrompt;
        }
    }
    
    private void SubscribeToEvents()
    {
        GameEvents.OnWaveStart += HandleWaveStart;
        GameEvents.OnWaveComplete += HandleWaveComplete;
        GameEvents.OnCubeCaptured += HandleCubeCaptured;
        GameEvents.OnCubeMove += HandleCubeMove;
        GameEvents.OnCubeEscaped += HandleCubeEscaped;
        GameEvents.OnMarkerPlaced += HandleMarkerPlaced;
    }
    
    private void UnsubscribeFromEvents()
    {
        GameEvents.OnWaveStart -= HandleWaveStart;
        GameEvents.OnWaveComplete -= HandleWaveComplete;
        GameEvents.OnCubeCaptured -= HandleCubeCaptured;
        GameEvents.OnCubeMove -= HandleCubeMove;
        GameEvents.OnCubeEscaped -= HandleCubeEscaped;
        GameEvents.OnMarkerPlaced -= HandleMarkerPlaced;
    }
    
    #endregion
    
    #region Event Handlers
    
    private void HandleWaveStart(int waveIndex, WaveData waveData)
    {
        // Clear previous highlights from previous wave
        ClearAllHighlights();
        
        // All highlighting is now driven by highlight sequences
        // Sequences are handled by WaveManager at the appropriate move steps
        if (waveData.highlightSequences != null && waveData.highlightSequences.Count > 0)
        {
            DebugLog("HandleWaveStart", $"Wave {waveIndex} has {waveData.highlightSequences.Count} sequences - they will be executed by WaveManager");
        }
        else
        {
            DebugLog("HandleWaveStart", $"Wave {waveIndex} has no highlight sequences - no highlights will be shown");
        }
    }
    
    private void HandleWaveComplete(int waveIndex)
    {
        // Clear all highlights when wave completes
        ClearAllHighlights();
        DebugLog("HandleWaveComplete", $"Wave {waveIndex} completed - cleared all highlights");
    }
    
    private void HandleCubeMove(Vector2Int oldPosition, Vector2Int newPosition, CubeType cubeType)
    {
        // Find cube that moved and update tracking if needed
        // Since we track by instance ID now, this is mainly for cleanup of invalid references
        CleanupInvalidHighlights();
    }
    
    private void HandleCubeEscaped(Vector2Int position, CubeType cubeType)
    {
        // Clear highlight when cube escapes
        CleanupInvalidHighlights();
    }
    
    private void HandleMarkerPlaced(Vector2Int position, MarkerType markerType)
    {
        DebugLog("HandleMarkerPlaced", $"Marker {markerType} placed at ({position.x}, {position.y}). Pending validations: {pendingValidations.Count}");
        
        // Log all pending validation positions for debugging
        if (pendingValidations.Count > 0)
        {
            foreach (var kvp in pendingValidations)
            {
                DebugLog("HandleMarkerPlaced", $"  Pending validation at ({kvp.Key.x}, {kvp.Key.y})");
            }
        }
        
        // Check if there's a pending validation for this position
        if (pendingValidations.ContainsKey(position))
        {
            // Validation passed - marker placed at correct position
            var sequence = pendingValidations[position];
            pendingValidations.Remove(position);
            
            // Resume wave movement
            if (waveManager != null && isWavePausedForValidation)
            {
                waveManager.ResumeWaveAfterValidation();
                isWavePausedForValidation = false;
                DebugLog("HandleMarkerPlaced", "Wave resumed after validation");
            }
            
            // Clear tile highlight
            ClearTileHighlight(position);
            
            // Clear sequence tracking
            int tileKey = GetPositionHash(position);
            activeSequences.Remove(tileKey);
            
            DebugLog("HandleMarkerPlaced", $"Validation passed - marker placed correctly at ({position.x}, {position.y})");
            
            // Check if any sequences should trigger on marker placement
            CheckAndTriggerMarkerSequences(position);
        }
        else
        {
            // Check if there are any pending validations (marker placed at wrong position)
            if (pendingValidations.Count > 0)
            {
                // Get the required position from the first pending validation
                var requiredPosition = pendingValidations.Keys.First();
                DebugLog("HandleMarkerPlaced", $"Validation failed - marker at ({position.x}, {position.y}) but required ({requiredPosition.x}, {requiredPosition.y})");
                
                // Marker placed at incorrect position - remove it and show feedback
                RemoveMarkerAtPosition(position, markerType);
                
                // Show validation failure message
                var firstValidation = pendingValidations.Values.First();
                string failureMessage = !string.IsNullOrEmpty(firstValidation.validationFailureMessage) 
                    ? firstValidation.validationFailureMessage 
                    : "Place your marker on the highlighted tile.";
                
                ShowMessage(failureMessage, false, 2f, 0); // Non-blocking, auto-hide after 2 seconds
                
                DebugLog("HandleMarkerPlaced", $"Validation failed - marker removed from ({position.x}, {position.y})");
                return; // Don't process further, wait for correct placement
            }
            
            // No validation pending - normal marker placement
            // Clear tile highlight immediately when marker is placed (marker overrides highlight)
            ClearTileHighlight(position);
            
            // Also clear any active sequences for this tile position
            int tileKey = GetPositionHash(position);
            if (activeSequences.ContainsKey(tileKey))
            {
                activeSequences.Remove(tileKey);
            }
            
            // Check if any sequences should trigger on marker placement
            CheckAndTriggerMarkerSequences(position);
        }
    }
    
    /// <summary>
    /// Checks all sequences in the current wave and triggers any that match the marker placement position
    /// </summary>
    private void CheckAndTriggerMarkerSequences(Vector2Int position)
    {
        if (waveManager == null || waveManager.CurrentWave == null || waveManager.CurrentWave.highlightSequences == null)
        {
            DebugLog("CheckAndTriggerMarkerSequences", "No wave or sequences available");
            return;
        }
        
        DebugLog("CheckAndTriggerMarkerSequences", $"Checking {waveManager.CurrentWave.highlightSequences.Count} sequences for trigger at ({position.x}, {position.y})");
        
        int sequenceIndex = 0;
        foreach (var sequence in waveManager.CurrentWave.highlightSequences)
        {
            if (sequence == null)
            {
                DebugLog("CheckAndTriggerMarkerSequences", $"Sequence {sequenceIndex} is null, skipping");
                sequenceIndex++;
                continue;
            }
            
            DebugLog("CheckAndTriggerMarkerSequences", $"Sequence {sequenceIndex}: triggerOnMarkerAtPosition=({sequence.triggerOnMarkerAtPosition.x}, {sequence.triggerOnMarkerAtPosition.y}), targetType={sequence.targetType}, targetPosition=({sequence.targetPosition.x}, {sequence.targetPosition.y})");
            
            // Check if this sequence should trigger on marker placement
            if (sequence.triggerOnMarkerAtPosition != Vector2Int.zero && 
                sequence.triggerOnMarkerAtPosition == position)
            {
                DebugLog("CheckAndTriggerMarkerSequences", $"✅ MATCH! Triggering sequence {sequenceIndex} with trigger position ({sequence.triggerOnMarkerAtPosition.x}, {sequence.triggerOnMarkerAtPosition.y})");
                ExecuteSequence(sequence);
            }
            else
            {
                DebugLog("CheckAndTriggerMarkerSequences", $"Sequence {sequenceIndex} skipped - trigger: ({sequence.triggerOnMarkerAtPosition.x}, {sequence.triggerOnMarkerAtPosition.y}), placed: ({position.x}, {position.y})");
            }
            
            sequenceIndex++;
        }
    }
    
    /// <summary>
    /// Removes a marker at the given position (for validation failures)
    /// </summary>
    private void RemoveMarkerAtPosition(Vector2Int position, MarkerType markerType)
    {
        if (playerActionManager == null || playerActionManager.MarkerSystem == null) return;
        
        // Remove marker based on type
        switch (markerType)
        {
            case MarkerType.Unit:
                playerActionManager.MarkerSystem.RemoveUnitMarkerAt(position);
                break;
            case MarkerType.Matrix:
                playerActionManager.MarkerSystem.RemoveMatrixMarkerAt(position);
                break;
            case MarkerType.Recursion:
                playerActionManager.MarkerSystem.RemoveRecursionMarkerAt(position);
                break;
            // Note: Infinity markers don't exist in MarkerType enum, only Unit/Matrix/Recursion/Cube
            default:
                DebugLog("RemoveMarkerAtPosition", $"Unknown marker type: {markerType}, attempting to remove Unit marker");
                playerActionManager.MarkerSystem.RemoveUnitMarkerAt(position);
                break;
        }
        
        DebugLog("RemoveMarkerAtPosition", $"Removed {markerType} marker at ({position.x}, {position.y})");
    }
    
    private void HandleCubeCaptured(Vector2Int position, CubeType cubeType)
    {
        // Find and clear highlight for the captured cube
        if (waveManager != null)
        {
            // Find the cube that was captured (might be destroyed already, so check all highlighted cubes)
            var cubesToRemove = new List<int>();
            foreach (var kvp in highlightedCubes)
            {
                if (kvp.Value == null || kvp.Value.isDestroyed || kvp.Value.position == position)
                {
                    cubesToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var cubeId in cubesToRemove)
            {
                // Check if sequence should clear on capture
                if (activeSequences.TryGetValue(cubeId, out HighlightSequence sequence))
                {
                    if (sequence.clearOnCapture)
                    {
                        ClearCubeHighlightById(cubeId);
                        activeSequences.Remove(cubeId);
                    }
                }
                else
                {
                    ClearCubeHighlightById(cubeId);
                }
            }
            
            // Check if any sequences should trigger on cube capture
            if (waveManager.CurrentWave != null && waveManager.CurrentWave.highlightSequences != null)
            {
                DebugLog("HandleCubeCaptured", $"Cube captured at ({position.x}, {position.y}), checking {waveManager.CurrentWave.highlightSequences.Count} sequences");
                
                foreach (var sequence in waveManager.CurrentWave.highlightSequences)
                {
                    if (sequence != null)
                    {
                        DebugLog("HandleCubeCaptured", $"Sequence triggerOnCaptureAtPosition=({sequence.triggerOnCaptureAtPosition.x}, {sequence.triggerOnCaptureAtPosition.y}), captured at=({position.x}, {position.y})");
                        
                        if (sequence.triggerOnCaptureAtPosition == position)
                        {
                            DebugLog("HandleCubeCaptured", $"✅ Triggering sequence with message: '{sequence.messageText}'");
                            ExecuteSequence(sequence);
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Cleans up highlights for cubes that no longer exist or are invalid
    /// </summary>
    private void CleanupInvalidHighlights()
    {
        var cubesToRemove = new List<int>();
        foreach (var kvp in highlightedCubes)
        {
            if (kvp.Value == null || kvp.Value.isDestroyed)
            {
                cubesToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var cubeId in cubesToRemove)
        {
            ClearCubeHighlightById(cubeId);
        }
    }
    
    #endregion
    
    #region Highlighting Methods
    
    /// <summary>
    /// Highlights all Infinity cubes with red pulsing glow
    /// </summary>
    public void HighlightInfinityCubes()
    {
        if (waveManager == null || gridManager == null) return;
        
        // Clear any existing highlights first
        ClearAllHighlights();
        
        foreach (var cube in waveManager.activeCubes)
        {
            if (cube != null && !cube.isDestroyed && cube.type == CubeType.Infinity)
            {
                HighlightCube(cube.position, infinityCubeColor, true); // Pulse Infinity cubes
            }
        }
        
        DebugLog("HighlightInfinityCubes", $"Highlighted {waveManager.activeCubes.Count(c => c != null && c.type == CubeType.Infinity)} Infinity cubes");
    }
    
    /// <summary>
    /// Highlights the first capturable cube (Unit, Matrix, or Recursion) with green glow
    /// </summary>
    public void HighlightFirstCapturableCube()
    {
        if (waveManager == null || gridManager == null) return;
        
        // Clear any existing highlights first (to avoid conflicts)
        ClearAllHighlights();
        
        // Find first capturable cube (closest to player, or first in spawn order)
        CubeManager firstCapturable = null;
        int minY = int.MaxValue;
        
        foreach (var cube in waveManager.activeCubes)
        {
            if (cube != null && !cube.isDestroyed && cube.type != CubeType.Infinity)
            {
                if (cube.position.y < minY)
                {
                    minY = cube.position.y;
                    firstCapturable = cube;
                }
            }
        }
        
        if (firstCapturable != null)
        {
            int cubeId = firstCapturable.GetInstanceID();
            HighlightCube(firstCapturable.position, capturableCubeColor, false);
            
            // Auto-clear after duration
            StartCoroutine(AutoClearHighlightById(cubeId, autoHighlightDuration));
            
            DebugLog("HighlightFirstCapturableCube", $"Highlighted first capturable cube {cubeId} at ({firstCapturable.position.x}, {firstCapturable.position.y})");
        }
    }
    
    /// <summary>
    /// Highlights a specific cube at the given position by modifying the cube's material
    /// </summary>
    public void HighlightCube(Vector2Int position, Color color, bool shouldPulse)
    {
        if (waveManager == null || gridManager == null) return;
        
        // Find the cube at this position
        CubeManager cube = waveManager.activeCubes.FirstOrDefault(c => 
            c != null && !c.isDestroyed && c.position == position);
        
        if (cube == null)
        {
            DebugLog("HighlightCube", $"No cube found at ({position.x}, {position.y})");
            return;
        }
        
        // Clear existing highlight for this cube if it exists
        int cubeId = cube.GetInstanceID();
        ClearCubeHighlightById(cubeId);
        
        // Get the cube's renderer
        Renderer cubeRenderer = cube.GetComponent<Renderer>();
        if (cubeRenderer == null)
        {
            DebugLog("HighlightCube", $"Cube at ({position.x}, {position.y}) has no Renderer component");
            return;
        }
        
        // Store original material (create a copy to avoid shared material issues)
        Material originalMat = new Material(cubeRenderer.material);
        originalMaterials[cubeId] = originalMat;
        
        // Create highlight material based on ORIGINAL material (not current, which might already be highlighted)
        Material highlightMat = new Material(originalMat);
        
        // Blend highlight color with original material color (more subtle)
        Color blendedColor = Color.Lerp(originalMat.color, color, 0.3f);
        highlightMat.color = blendedColor;
        highlightMat.SetFloat("_Metallic", originalMat.GetFloat("_Metallic"));
        highlightMat.SetFloat("_Smoothness", originalMat.GetFloat("_Smoothness"));
        
        // Enable emission for subtle glow effect
        highlightMat.EnableKeyword("_EMISSION");
        highlightMat.SetColor("_EmissionColor", color * highlightEmissionIntensity);
        
        // Apply highlight material to cube
        cubeRenderer.material = highlightMat;
        
        // Store cube reference by instance ID (so it works even when cube moves)
        highlightedCubes[cubeId] = cube;
        
        // Start pulsing if requested
        if (shouldPulse)
        {
            Coroutine pulseCoroutine = StartCoroutine(PulseCubeHighlight(cubeRenderer, color, highlightMat));
            pulseCoroutines[cubeId] = pulseCoroutine;
        }
        
        DebugLog("HighlightCube", $"Highlighted cube {cubeId} at ({position.x}, {position.y}) with color {color}, pulse: {shouldPulse}");
    }
    
    /// <summary>
    /// Clears highlight for a specific cube position by restoring original material
    /// </summary>
    public void ClearCubeHighlight(Vector2Int position)
    {
        // Find cube at this position and clear by instance ID
        if (waveManager != null)
        {
            CubeManager cube = waveManager.activeCubes.FirstOrDefault(c => 
                c != null && !c.isDestroyed && c.position == position);
            
            if (cube != null)
            {
                ClearCubeHighlightById(cube.GetInstanceID());
            }
        }
    }
    
    /// <summary>
    /// Clears highlight for a cube by its instance ID (works even when cube moves)
    /// </summary>
    private void ClearCubeHighlightById(int cubeId)
    {
        // Stop pulsing coroutine if active
        if (pulseCoroutines.TryGetValue(cubeId, out Coroutine pulseCoroutine))
        {
            if (pulseCoroutine != null)
            {
                StopCoroutine(pulseCoroutine);
            }
            pulseCoroutines.Remove(cubeId);
        }
        
        // Restore original material if we have it
        if (highlightedCubes.TryGetValue(cubeId, out CubeManager cube) && cube != null && !cube.isDestroyed)
        {
            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            if (cubeRenderer != null && originalMaterials.TryGetValue(cubeId, out Material originalMat))
            {
                cubeRenderer.material = originalMat;
                DebugLog("ClearCubeHighlightById", $"Restored original material for cube {cubeId} at ({cube.position.x}, {cube.position.y})");
            }
        }
        
        // Clean up references
        highlightedCubes.Remove(cubeId);
        originalMaterials.Remove(cubeId);
        
        DebugLog("ClearCubeHighlightById", $"Cleared highlight for cube {cubeId}");
    }
    
    /// <summary>
    /// Clears all active highlights (cubes and tiles)
    /// </summary>
    public void ClearAllHighlights()
    {
        // Clear cube highlights
        var cubeIdsToClear = highlightedCubes.Keys.ToList();
        foreach (var cubeId in cubeIdsToClear)
        {
            ClearCubeHighlightById(cubeId);
        }
        
        // Clear tile highlights
        var tilePositionsToClear = highlightedTiles.Keys.ToList();
        foreach (var pos in tilePositionsToClear)
        {
            ClearTileHighlight(pos);
        }
        
        // Clear active sequences
        activeSequences.Clear();
        
        DebugLog("ClearAllHighlights", "Cleared all highlights");
    }
    
    #endregion
    
    #region Sequence Execution
    
    /// <summary>
    /// Executes a highlight sequence: pause (optional) → message (optional) → highlight → resume
    /// </summary>
    public void ExecuteSequence(HighlightSequence sequence)
    {
        if (sequence == null) return;
        
        StartCoroutine(ExecuteSequenceCoroutine(sequence));
    }
    
    private IEnumerator ExecuteSequenceCoroutine(HighlightSequence sequence)
    {
        DebugLog("ExecuteSequenceCoroutine", $"Starting sequence: targetType={sequence.targetType}, targetPosition=({sequence.targetPosition.x}, {sequence.targetPosition.y}), pauseGame={sequence.pauseGame}, messageText='{sequence.messageText}'");
        
        // Step 1: Pause game (optional) - only for message display
        if (sequence.pauseGame)
        {
            isPaused = true;
            Time.timeScale = 0f;
            DebugLog("ExecuteSequence", "Game paused for sequence");
        }
        
        // Step 2: Show message (optional)
        if (!string.IsNullOrEmpty(sequence.messageText))
        {
            DebugLog("ExecuteSequenceCoroutine", $"Showing message: '{sequence.messageText}'");
            yield return ShowMessageSequence(sequence.messageText, sequence.messageRequirePause, sequence.messageAutoHideDelay, 0);
        }
        
        // Step 3: Resume game BEFORE highlighting (so player can interact)
        // If validation is required, we'll pause the wave instead (game must run for input)
        if (sequence.pauseGame)
        {
            // Always resume game if validation is required (player needs to interact)
            if (sequence.requireMarkerPlacementValidation)
            {
                Time.timeScale = 1f;
                isPaused = false;
                DebugLog("ExecuteSequence", "Game resumed for validation (wave will be paused)");
            }
            // Otherwise, resume only if resumeGame is true
            else if (sequence.resumeGame)
            {
                Time.timeScale = 1f;
                isPaused = false;
                DebugLog("ExecuteSequence", "Game resumed after message");
            }
        }
        
        // Step 4: Highlight target (optional)
        if (sequence.targetType != HighlightTargetType.None)
        {
            yield return HighlightTargetSequence(sequence);
        }
        
        // Step 5: Final resume (if we didn't resume earlier and no validation)
        if (sequence.pauseGame && !sequence.resumeGame && !sequence.requireMarkerPlacementValidation)
        {
            Time.timeScale = 1f;
            isPaused = false;
            DebugLog("ExecuteSequence", "Game resumed after sequence");
        }
    }
    
    /// <summary>
    /// Shows a simple message (message-only, no highlight). Can be queued or immediate.
    /// </summary>
    public void ShowMessage(string message, bool requirePause = true, float autoHideDelay = 0f, int moveStep = 0)
    {
        if (string.IsNullOrEmpty(message)) return;
        
        var simpleMessage = new SimpleMessage
        {
            text = message,
            requirePause = requirePause,
            autoHideDelay = autoHideDelay,
            moveStep = moveStep
        };
        
        pendingMessages.Enqueue(simpleMessage);
        if (!isProcessingMessageQueue)
        {
            StartCoroutine(ProcessMessageQueue());
        }
    }
    
    /// <summary>
    /// Processes the message queue
    /// </summary>
    private IEnumerator ProcessMessageQueue()
    {
        isProcessingMessageQueue = true;
        
        while (pendingMessages.Count > 0)
        {
            var message = pendingMessages.Dequeue();
            yield return ShowMessageSequence(message.text, message.requirePause, message.autoHideDelay, message.moveStep);
        }
        
        isProcessingMessageQueue = false;
    }
    
    private IEnumerator ShowMessageSequence(string message, bool requirePause, float autoHideDelay, int moveStep = 0)
    {
        if (messagePanel == null || messageTextUI == null)
        {
            DebugLog("ShowMessageSequence", "Message panel or text not available");
            yield break;
        }
        
        // Store current time scale state
        float previousTimeScale = Time.timeScale;
        bool wasPaused = (Time.timeScale == 0f);
        
        // Pause game if required (only if not already paused)
        bool shouldPause = requirePause && !wasPaused;
        if (shouldPause)
        {
            isPaused = true;
            Time.timeScale = 0f;
            DebugLog("ShowMessageSequence", "Game paused for message");
        }
        
        messagePanel.SetActive(true);
        messageTextUI.text = message;
        
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(requirePause);
        }
        
        // Notify statistics manager about message display
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMessageDisplayed(message, moveStep);
        }
        
        DebugLog("ShowMessageSequence", $"Showing message: {message} (paused: {Time.timeScale == 0f})");
        
        bool wasSkipped = false;
        if (requirePause)
        {
            // Wait for player to press K (works whether game is paused or not)
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.K));
        }
        else if (autoHideDelay > 0)
        {
            // Wait for auto-hide delay
            // Use unscaledDeltaTime if paused, deltaTime if not paused
            float timer = 0f;
            while (timer < autoHideDelay)
            {
                if (Input.GetKeyDown(KeyCode.K)) // Allow skipping
                {
                    wasSkipped = true;
                    break;
                }
                // Use appropriate time delta based on pause state
                timer += (Time.timeScale == 0f) ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }
        
        messagePanel.SetActive(false);
        if (continuePrompt != null)
        {
            continuePrompt.SetActive(false);
        }
        
        // Resume game only if we paused it (don't resume if it was already paused)
        if (shouldPause)
        {
            Time.timeScale = previousTimeScale;
            isPaused = false;
            DebugLog("ShowMessageSequence", "Game resumed after message");
        }
        
        // Notify statistics manager about message dismissal
        if (PlayerStatisticsManager.Instance != null)
        {
            PlayerStatisticsManager.Instance.OnMessageDismissed(message, wasSkipped);
        }
        
        DebugLog("ShowMessageSequence", "Message hidden");
    }
    
    private IEnumerator HighlightTargetSequence(HighlightSequence sequence)
    {
        DebugLog("HighlightTargetSequence", $"Starting highlight sequence: targetType={sequence.targetType}, targetPosition=({sequence.targetPosition.x}, {sequence.targetPosition.y})");
        
        if (sequence.targetType == HighlightTargetType.Tile)
        {
            DebugLog("HighlightTargetSequence", $"Highlighting TILE at ({sequence.targetPosition.x}, {sequence.targetPosition.y})");
            HighlightTile(sequence.targetPosition, sequence.highlightColor);
            
            // Store sequence for cleanup
            int tileKey = GetPositionHash(sequence.targetPosition);
            activeSequences[tileKey] = sequence;
            
            DebugLog("HighlightTargetSequence", $"Tile highlighted, requireMarkerPlacementValidation={sequence.requireMarkerPlacementValidation}, waveActive={waveManager?.waveActive}");
            
            // If validation is required, pause wave and wait for marker placement
            if (sequence.requireMarkerPlacementValidation)
            {
                // Pause wave movement (cubes stop moving, but game still runs for input)
                if (waveManager != null && waveManager.waveActive)
                {
                    waveManager.PauseWaveForValidation();
                    isWavePausedForValidation = true;
                    
                    // Track this sequence as waiting for validation
                    pendingValidations[sequence.targetPosition] = sequence;
                    
                    DebugLog("HighlightTargetSequence", $"Wave paused for validation at ({sequence.targetPosition.x}, {sequence.targetPosition.y})");
                }
                else
                {
                    DebugLog("HighlightTargetSequence", $"⚠️ Cannot pause wave - waveManager={waveManager != null}, waveActive={waveManager?.waveActive}");
                }
                
                // Wait for validation to complete (handled in HandleMarkerPlaced)
                // The sequence will continue when marker is placed correctly
                yield return new WaitUntil(() => !pendingValidations.ContainsKey(sequence.targetPosition));
                
                DebugLog("HighlightTargetSequence", $"Validation completed for tile at ({sequence.targetPosition.x}, {sequence.targetPosition.y})");
                
                // Wave will be resumed in HandleMarkerPlaced when validation passes
            }
            else
            {
                // Auto-clear after duration if specified
                if (sequence.highlightDuration > 0)
                {
                    yield return new WaitForSecondsRealtime(sequence.highlightDuration);
                    ClearTileHighlight(sequence.targetPosition);
                    activeSequences.Remove(tileKey);
                }
            }
        }
        else if (sequence.targetType == HighlightTargetType.Cube)
        {
            // targetPosition is already in grid coordinates
            DebugLog("HighlightTargetSequence", $"Looking for cube at grid position ({sequence.targetPosition.x}, {sequence.targetPosition.y}), type={sequence.targetCubeType}");
            
            // Find cube at initial position (position is only used to locate the cube initially)
            // Once highlighted, the cube is tracked by instance ID, so highlight persists even when cube moves
            CubeManager cube = FindCubeAtPosition(sequence.targetPosition, sequence.targetCubeType);
            
            if (cube != null)
            {
                DebugLog("HighlightTargetSequence", $"✅ Cube found! ID={cube.GetInstanceID()}, type={cube.type}, position=({cube.position.x}, {cube.position.y})");
                int cubeId = cube.GetInstanceID();
                // Highlight the cube (uses grid position to find it, but tracks by instance ID)
                HighlightCube(sequence.targetPosition, sequence.highlightColor, sequence.shouldPulse);
                
                // Store sequence for cleanup (tracked by instance ID, not position)
                activeSequences[cubeId] = sequence;
                
                DebugLog("HighlightTargetSequence", $"Cube highlighted, clearOnCapture={sequence.clearOnCapture}, highlightDuration={sequence.highlightDuration}");
                
                // Auto-clear after duration if specified (and not waiting for capture)
                if (sequence.highlightDuration > 0 && !sequence.clearOnCapture)
                {
                    yield return new WaitForSecondsRealtime(sequence.highlightDuration);
                    ClearCubeHighlightById(cubeId);
                    activeSequences.Remove(cubeId);
                }
                // If clearOnCapture is true, highlight stays until cube is captured (handled in HandleCubeCaptured)
                // Note: Highlight will remain on cube even if it moves to different positions
            }
            else
            {
                DebugLog("HighlightTargetSequence", $"❌ Cube not found at grid position ({sequence.targetPosition.x}, {sequence.targetPosition.y})");
                
                // Additional debugging: list all active cubes
                if (waveManager != null && waveManager.activeCubes != null)
                {
                    DebugLog("HighlightTargetSequence", $"Active cubes count: {waveManager.activeCubes.Count}");
                    foreach (var activeCube in waveManager.activeCubes)
                    {
                        if (activeCube != null && !activeCube.isDestroyed)
                        {
                            DebugLog("HighlightTargetSequence", $"  - Cube at ({activeCube.position.x}, {activeCube.position.y}), type={activeCube.type}");
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Highlights a tile at the given position (for marker placement guidance)
    /// </summary>
    public void HighlightTile(Vector2Int position, Color color)
    {
        DebugLog("HighlightTile", $"Called for position ({position.x}, {position.y}), color={color}");
        
        if (gridManager == null)
        {
            DebugLog("HighlightTile", "❌ GridManager is null!");
            return;
        }
        
        // Clear existing tile highlight
        ClearTileHighlight(position);
        
        Tile tile = gridManager.GetTileAt(position.x, position.y);
        if (tile == null)
        {
            DebugLog("HighlightTile", $"❌ No tile found at ({position.x}, {position.y})");
            return;
        }
        
        DebugLog("HighlightTile", $"✅ Tile found, creating highlight overlay");
        
        // Create highlight overlay
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cube);
        highlight.name = $"TileHighlight_{position.x}_{position.y}";
        highlight.transform.SetParent(tile.transform);
        highlight.transform.localPosition = new Vector3(0, 0.52f, 0); // Above tile
        highlight.transform.localScale = new Vector3(0.95f, 0.08f, 0.95f);
        
        // Remove collider
        Destroy(highlight.GetComponent<Collider>());
        
        // Set material
        Renderer renderer = highlight.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.SetFloat("_Metallic", 0.2f);
            mat.SetFloat("_Smoothness", 0.8f);
            
            // Enable emission for glow
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * highlightEmissionIntensity);
            
            renderer.material = mat;
            DebugLog("HighlightTile", $"✅ Highlight material created and applied");
        }
        else
        {
            DebugLog("HighlightTile", "❌ Renderer component not found on highlight object");
        }
        
        highlightedTiles[position] = highlight;
        DebugLog("HighlightTile", $"✅ Tile highlighted at ({position.x}, {position.y}), stored in highlightedTiles dictionary");
    }
    
    /// <summary>
    /// Clears tile highlight at the given position
    /// </summary>
    public void ClearTileHighlight(Vector2Int position)
    {
        if (highlightedTiles.TryGetValue(position, out GameObject highlight))
        {
            if (highlight != null)
            {
                Destroy(highlight);
            }
            highlightedTiles.Remove(position);
            
            // Clear sequence if exists
            int tileKey = GetPositionHash(position);
            activeSequences.Remove(tileKey);
            
            DebugLog("ClearTileHighlight", $"Cleared tile highlight at ({position.x}, {position.y})");
        }
    }
    
    /// <summary>
    /// Finds a cube at the given position, optionally filtering by type
    /// </summary>
    private CubeManager FindCubeAtPosition(Vector2Int position, CubeType preferredType = CubeType.Unit)
    {
        if (waveManager == null) return null;
        
        // First try to find exact type match
        CubeManager cube = waveManager.activeCubes.FirstOrDefault(c => 
            c != null && !c.isDestroyed && c.position == position && c.type == preferredType);
        
        // If not found, find any cube at position
        if (cube == null)
        {
            cube = waveManager.activeCubes.FirstOrDefault(c => 
                c != null && !c.isDestroyed && c.position == position);
        }
        
        return cube;
    }
    
    /// <summary>
    /// Gets a hash for a position (for sequence tracking)
    /// </summary>
    private int GetPositionHash(Vector2Int position)
    {
        return position.x * 1000 + position.y; // Simple hash
    }
    
    #endregion
    
    #region Visual Effects
    
    private IEnumerator PulseCubeHighlight(Renderer cubeRenderer, Color baseColor, Material highlightMat)
    {
        if (cubeRenderer == null || highlightMat == null) yield break;
        
        float baseEmission = highlightEmissionIntensity;
        
        while (cubeRenderer != null && cubeRenderer.material == highlightMat)
        {
            float pulse = Mathf.Sin(Time.time * highlightPulseSpeed) * highlightPulseIntensity;
            float emission = baseEmission + pulse;
            
            // Clamp emission to keep it subtle
            emission = Mathf.Clamp(emission, 0.1f, highlightEmissionIntensity + highlightPulseIntensity);
            
            highlightMat.SetColor("_EmissionColor", baseColor * emission);
            
            yield return null;
        }
    }
    
    private IEnumerator AutoClearHighlightById(int cubeId, float duration)
    {
        yield return new WaitForSeconds(duration);
        ClearCubeHighlightById(cubeId);
    }
    
    #endregion
    
    #region IManagerDebugInterface Implementation
    
    public string GetDebugStatus()
    {
        return $"MessageHighlightManager: {highlightedCubes.Count} active highlights, Auto-Highlight: {enableAutoHighlighting}";
    }
    
    public Dictionary<string, object> GetDebugData()
    {
        return new Dictionary<string, object>
        {
            ["Active Highlights"] = highlightedCubes.Count,
            ["Enable Auto-Highlighting"] = enableAutoHighlighting,
            ["Auto-Highlight First Capturable"] = autoHighlightFirstCapturable,
            ["Auto-Highlight Infinity Cubes"] = autoHighlightInfinityCubes,
            ["WaveManager Set"] = waveManager != null,
            ["GridManager Set"] = gridManager != null,
            ["Capturable Color"] = capturableCubeColor.ToString(),
            ["Infinity Color"] = infinityCubeColor.ToString(),
            ["Pulse Speed"] = highlightPulseSpeed,
            ["Pulse Intensity"] = highlightPulseIntensity
        };
    }
    
    public void ResetToDefaults()
    {
        ClearAllHighlights();
        
        // Restore default values
        capturableCubeColor = defaultCapturableColor;
        infinityCubeColor = defaultInfinityColor;
        highlightPulseSpeed = defaultPulseSpeed;
        highlightPulseIntensity = defaultPulseIntensity;
        highlightEmissionIntensity = defaultHighlightEmissionIntensity;
        enableAutoHighlighting = defaultEnableAutoHighlighting;
        autoHighlightFirstCapturable = defaultAutoHighlightFirstCapturable;
        autoHighlightInfinityCubes = defaultAutoHighlightInfinityCubes;
        autoHighlightDuration = defaultAutoHighlightDuration;
        
        DebugLog("ResetToDefaults", "Reset to default configuration values");
    }
    
    public void LoadConfiguration(string configName)
    {
        // TODO: Implement configuration loading from ScriptableObject or JSON
        // For now, just log the request
        DebugLog("LoadConfiguration", $"Loading configuration: {configName} (not yet implemented)");
    }
    
    public void SaveConfiguration(string configName)
    {
        // TODO: Implement configuration saving to ScriptableObject or JSON
        // For now, just log the request
        DebugLog("SaveConfiguration", $"Saving configuration: {configName} (not yet implemented)");
    }
    
    #endregion
    
    #region Debug
    
    private void DebugLog(string methodName, string message)
    {
        if (EnableDebugLogs)
            Debug.Log($"[MessageHighlightManager] {methodName}: {message}");
    }
    
    #endregion
}


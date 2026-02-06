using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static Enumerations;

/// <summary>
/// Handles wave messages, sequences, and player feedback.
/// Extracted from WaveManager as part of SRP refactoring.
/// WaveManager maintains facade methods that delegate to this controller.
/// </summary>
public class WaveMessageController : MonoBehaviour
{
    #region References
    private WaveManager waveManager;
    private MessageHighlightManager messageHighlightManager;
    private GameObject messagePanel;
    private TextMeshProUGUI messageText;
    
    // Message state
    private Queue<WaveMessage> pendingMessages = new Queue<WaveMessage>();
    private bool isProcessingMessageQueue = false;
    private bool isPaused = false;
    
    // Logging
    private bool enableDebugLogs;
    #endregion

    #region Properties
    public bool IsPaused => isPaused;
    public bool IsProcessingMessageQueue => isProcessingMessageQueue;
    public int PendingMessagesCount => pendingMessages.Count;
    #endregion
    
    #region State Management
    /// <summary>
    /// Resets message controller state to defaults.
    /// </summary>
    public void ResetState()
    {
        isPaused = false;
        isProcessingMessageQueue = false;
        pendingMessages.Clear();
        
        if (messagePanel != null)
            messagePanel.SetActive(false);
            
        DebugLog("Message controller state reset");
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Initializes the message controller with references to parent manager and dependencies.
    /// </summary>
    public void Initialize(WaveManager manager, MessageHighlightManager highlightManager, 
        GameObject panel, TextMeshProUGUI text, bool debugLogs)
    {
        waveManager = manager;
        messageHighlightManager = highlightManager;
        messagePanel = panel;
        messageText = text;
        enableDebugLogs = debugLogs;
        
        DebugLog("WaveMessageController initialized");
    }
    
    /// <summary>
    /// Updates debug logging state from parent manager.
    /// </summary>
    public void SetDebugLogs(bool enabled)
    {
        enableDebugLogs = enabled;
    }
    #endregion

    #region Initial Messages
    /// <summary>
    /// Starts the initial message display sequence with camera pan delay.
    /// </summary>
    public void ShowInitialMessages()
    {
        StartCoroutine(ShowInitialMessagesDelayed());
    }
    
    private IEnumerator ShowInitialMessagesDelayed()
    {
        // Wait for camera to pan to default position (CameraFollow uses 0.25s smooth time)
        // Add extra buffer to ensure camera is fully positioned before showing messages
        yield return new WaitForSeconds(0.6f);

        // Process initial sequences (sequences handle messages internally)
        ProcessInitialSequences();
    }
    
    /// <summary>
    /// Processes highlight sequences at move step 0 (wave start)
    /// Only executes sequences with DisplayMoveStep == 0 that don't have trigger conditions
    /// </summary>
    private void ProcessInitialSequences()
    {
        var currentWave = waveManager?.CurrentWave;
        if (currentWave?.highlightSequences == null || messageHighlightManager == null) return;
        
        var initialSequences = currentWave.highlightSequences.Where(s => 
            s != null && 
            s.DisplayMoveStep == 0 &&
            s.triggerOnMarkerAtPosition == Vector2Int.zero && 
            s.triggerOnCaptureAtPosition == Vector2Int.zero);
        
        foreach (var sequence in initialSequences)
        {
            messageHighlightManager.ExecuteSequence(sequence);
        }
    }
    #endregion

    #region Step Sequences
    /// <summary>
    /// Processes highlight sequences at the current move step
    /// </summary>
    public void ProcessStepSequences()
    {
        var currentWave = waveManager?.CurrentWave;
        int moveStep = waveManager?.MoveStep ?? 0;
        
        if (currentWave?.highlightSequences == null || messageHighlightManager == null) return;
        
        DebugLog($"ProcessStepSequences: Checking sequences for MoveStep={moveStep}, total sequences={currentWave.highlightSequences.Count}");
        
        // Get sequences for current move step that aren't event-triggered
        var stepSequences = currentWave.highlightSequences.Where(s => 
            s != null && 
            s.DisplayMoveStep == moveStep &&
            s.triggerOnMarkerAtPosition == Vector2Int.zero && 
            s.triggerOnCaptureAtPosition == Vector2Int.zero);
        
        var sequencesList = stepSequences.ToList();
        DebugLog($"ProcessStepSequences: Found {sequencesList.Count} sequences to execute at MoveStep={moveStep}");
        
        foreach (var sequence in sequencesList)
        {
            DebugLog($"ProcessStepSequences: Executing sequence with DisplayMoveStep={sequence.DisplayMoveStep}, targetType={sequence.targetType}, targetPosition=({sequence.targetPosition.x}, {sequence.targetPosition.y})");
            messageHighlightManager.ExecuteSequence(sequence);
        }
    }

    /// <summary>
    /// Processes highlight sequences at wave end (DisplayMoveStep == -1)
    /// </summary>
    public void ProcessEndSequences()
    {
        var currentWave = waveManager?.CurrentWave;
        if (currentWave?.highlightSequences == null || messageHighlightManager == null) return;

        var endSequences = currentWave.highlightSequences.Where(s => 
            s != null && 
            s.DisplayMoveStep == -1 &&
            s.triggerOnMarkerAtPosition == Vector2Int.zero && 
            s.triggerOnCaptureAtPosition == Vector2Int.zero);

        foreach (var sequence in endSequences)
        {
            messageHighlightManager.ExecuteSequence(sequence);
        }
    }
    #endregion

    #region Wave Completion Message
    /// <summary>
    /// Shows wave completion feedback message with progress and statistics.
    /// </summary>
    public void ShowWaveCompletionMessage(bool showMessages, int currentWaveIndex, 
        int totalWaves, int normalCaptured, int blueCaptured, int reinforcedCaptured, int escaped)
    {
        if (!showMessages) return;

        int waveNum = currentWaveIndex + 1;
        
        // Simple, minimal message
        string message = $"Wave {waveNum}/{totalWaves}\n\n";
        
        // Add statistics only if there were failures
        int totalCaptured = normalCaptured + blueCaptured + reinforcedCaptured;
        if (escaped > 0)
        {
            message += $"Captured: {totalCaptured}\nEscaped: {escaped}\n\n";
        }
        
        // Simple prompt
        message += "Press K to continue";
        
        // Use MessageHighlightManager if available, otherwise fallback
        int moveStep = waveManager?.MoveStep ?? 0;
        if (messageHighlightManager != null)
        {
            messageHighlightManager.ShowMessage(message, true, 0f, moveStep);
        }
        else
        {
            var completionMsg = new WaveMessage
            {
                Message = message,
                RequirePause = true,
                AutoHideDelay = 0f
            };
            ShowMessage(completionMsg, showMessages);
        }
        DebugLog($"ShowWaveCompletionMessage: Wave {waveNum}/{totalWaves} - Captured: {totalCaptured}, Escaped: {escaped}");
    }
    #endregion

    #region Message Queue
    /// <summary>
    /// Enqueues a message for display.
    /// </summary>
    public void ShowMessage(WaveMessage message, bool showMessages)
    {
        pendingMessages.Enqueue(message);
        if (!isProcessingMessageQueue && showMessages)
        {
            StartCoroutine(ProcessMessageQueue(showMessages));
        }
    }

    private IEnumerator ProcessMessageQueue(bool showMessages)
    {
        isProcessingMessageQueue = true;

        while (pendingMessages.Count > 0 && showMessages)
        {
            var message = pendingMessages.Dequeue();
            yield return DisplayMessage(message, showMessages);
        }

        isProcessingMessageQueue = false;
    }

    /// <summary>
    /// Displays a message with optional pause and auto-hide behavior.
    /// </summary>
    public IEnumerator DisplayMessage(WaveMessage message, bool showMessages)
    {
        if (messagePanel != null && messageText != null && showMessages)
        {
            messagePanel.SetActive(true);
            messageText.text = message.Message;
            
            int moveStep = waveManager?.MoveStep ?? 0;
            
            // Notify statistics manager about message display
            if (PlayerStatisticsManager.Instance != null)
            {
                PlayerStatisticsManager.Instance.OnMessageDisplayed(message.Message, moveStep);
            }

            bool wasSkipped = false;
            if (message.RequirePause)
            {
                isPaused = true;
                Time.timeScale = 0f;
                yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.K));
                Time.timeScale = 1f;
                isPaused = false;
            }
            else if (message.AutoHideDelay > 0)
            {
                float timer = 0f;
                while (timer < message.AutoHideDelay)
                {
                    if (Input.GetKeyDown(KeyCode.K)) // Allow skipping auto-hide messages
                    {
                        wasSkipped = true;
                        break;
                    }
                    timer += Time.deltaTime;
                    yield return null;
                }
            }

            messagePanel.SetActive(false);
            
            // Notify statistics manager about message dismissal
            if (PlayerStatisticsManager.Instance != null)
            {
                PlayerStatisticsManager.Instance.OnMessageDismissed(message.Message, wasSkipped);
            }
        }
    }
    #endregion

    #region Debug Logging
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[WaveMessageController] {message}");
        }
    }
    #endregion
}

using UnityEngine;

/// <summary>
/// Example integration showing how to use ExecutionReportHooks with Shrimp Task Manager.
/// This script demonstrates the proper hook integration pattern for task completion and validation.
/// </summary>
public class ShrimpTaskIntegrationExample : MonoBehaviour
{
    #region Example Usage
    
    /// <summary>
    /// Example: Task completion hook integration
    /// Call this when a Shrimp task is completed successfully
    /// </summary>
    /// <param name="taskId">The completed task ID from Shrimp</param>
    /// <param name="taskName">Name of the completed task</param>
    /// <param name="summary">Task completion summary</param>
    public void OnShrimpTaskCompleted(string taskId, string taskName, string summary)
    {
        // Example task completion data
        string[] filesModified = {
            "Assets/scripts/Managers/AudioManager.cs",
            "Assets/scripts/Audio/SpatialAudioController.cs",
            "Assets/Docs/Audio Implementation Guide.md"
        };
        
        string implementationDetails = @"
### Key Implementation Details:
- Integrated Unity AudioSource components with spatial audio settings
- Added audio pooling system for performance optimization  
- Implemented distance-based volume attenuation
- Created audio event system for marker interactions
- Added fallback audio clips for missing sound files

### Technical Decisions:
- Used AudioSource.spatialBlend for 3D positioning
- Implemented object pooling to reduce garbage collection
- Added audio validation system for missing assets
- Created modular audio configuration system";

        string nextSteps = @"
1. **Testing Phase**: Test audio in different environments and scenarios
2. **Performance Validation**: Monitor audio performance in intensive scenarios  
3. **User Testing**: Gather feedback on audio clarity and spatial positioning
4. **Documentation Update**: Complete audio system documentation
5. **Integration Testing**: Verify audio works with all game mechanics";

        // Generate the summary report
        ExecutionReportHooks.OnTaskCompleted(
            taskName: taskName,
            taskDescription: summary,
            filesModified: filesModified,
            implementationDetails: implementationDetails,
            nextSteps: nextSteps
        );

        Debug.Log($"[ShrimpTaskIntegration] Task completion report generated for: {taskName}");
    }

    /// <summary>
    /// Example: Task validation hook integration
    /// Call this when a Shrimp task validation is completed
    /// </summary>
    /// <param name="taskId">The validated task ID from Shrimp</param>
    /// <param name="taskName">Name of the validated task</param>
    /// <param name="score">Validation score from Shrimp (0-100)</param>
    /// <param name="validationSummary">Validation summary from Shrimp</param>
    public void OnShrimpTaskValidated(string taskId, string taskName, int score, string validationSummary)
    {
        // Example validation data
        string[] testsExecuted = {
            "Audio System Compilation Test",
            "Spatial Audio Positioning Test", 
            "Audio Pooling Performance Test",
            "Audio Event System Integration Test",
            "Missing Asset Fallback Test",
            "Audio Configuration Validation Test"
        };

        string[] issuesFound = score >= 90 ? new string[0] : new[] {
            "Audio clip loading could be optimized for faster startup",
            "Consider adding more granular volume controls",
            "Some edge cases in spatial positioning need refinement"
        };

        string[] recommendations = {
            "Monitor audio performance during extended gameplay sessions",
            "Consider implementing audio streaming for large files",
            "Add audio accessibility options for hearing-impaired players",
            "Implement audio quality settings for different hardware capabilities"
        };

        // Generate the validation report
        ExecutionReportHooks.OnTaskValidated(
            taskName: taskName,
            overallScore: score,
            validationDetails: validationSummary,
            testsExecuted: testsExecuted,
            issuesFound: issuesFound,
            recommendations: recommendations
        );

        Debug.Log($"[ShrimpTaskIntegration] Task validation report generated for: {taskName} (Score: {score})");
    }

    /// <summary>
    /// Example: Task failure hook integration
    /// Call this when a Shrimp task fails during execution
    /// </summary>
    /// <param name="taskId">The failed task ID from Shrimp</param>
    /// <param name="taskName">Name of the failed task</param>
    /// <param name="errorDetails">Details about the failure</param>
    public void OnShrimpTaskFailed(string taskId, string taskName, string errorDetails)
    {
        string partialResults = @"
### Partial Progress Achieved:
- Audio system architecture designed and documented
- Basic AudioManager class structure implemented
- Audio folder structure created and organized
- Initial audio pooling system partially implemented

### Work Remaining:
- Complete spatial audio implementation
- Implement audio event system
- Add audio validation and fallback systems
- Complete testing and validation";

        string recoverySteps = @"
### Recovery Actions:
1. **Root Cause Analysis**: Review error logs and identify specific failure point
2. **Dependency Check**: Verify all required Unity packages and components are available
3. **Code Review**: Check for syntax errors or missing references
4. **Incremental Approach**: Break down remaining work into smaller, testable chunks
5. **Testing Strategy**: Implement unit tests for completed components before proceeding";

        // Generate the failure report
        ExecutionReportHooks.OnTaskFailed(
            taskName: taskName,
            errorDetails: errorDetails,
            partialResults: partialResults,
            recoverySteps: recoverySteps
        );

        Debug.LogWarning($"[ShrimpTaskIntegration] Task failure report generated for: {taskName}");
    }
    #endregion

    #region Unity Testing Methods (Editor Only)
    
    [ContextMenu("Test Task Completion Hook")]
    private void TestTaskCompletionHook()
    {
        OnShrimpTaskCompleted(
            "test-task-001",
            "Audio System Integration Test",
            "Test execution of task completion hook with sample audio system implementation."
        );
    }

    [ContextMenu("Test Task Validation Hook")]
    private void TestTaskValidationHook()
    {
        OnShrimpTaskValidated(
            "test-task-001",
            "Audio System Integration Test",
            95,
            "Validation completed successfully with minor optimization recommendations."
        );
    }

    [ContextMenu("Test Task Failure Hook")]
    private void TestTaskFailureHook()
    {
        OnShrimpTaskFailed(
            "test-task-002",
            "UI Modernization Test",
            "Task failed due to missing Unity UI components. OnGUI to Unity UI conversion incomplete."
        );
    }

    [ContextMenu("Clear Execution Reports")]
    private void ClearReports()
    {
        ExecutionReportHooks.ClearExecutionReports();
        Debug.Log("[ShrimpTaskIntegration] Execution reports cleared for testing");
    }
    #endregion
}

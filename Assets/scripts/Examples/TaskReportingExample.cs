using UnityEngine;
using UnityEditor;
using InfinityQube.Testing;

/// <summary>
/// Example demonstrating how to use the updated BuildValidationSystem for task-specific reporting.
/// This replaces the previous ExecutionReportHooks approach with Unity Editor script methods.
/// </summary>
public class TaskReportingExample : MonoBehaviour
{
    #region Unity Menu Integration
    
    [MenuItem("InfinityQube/Examples/Generate Sample Task Completion Report")]
    public static void GenerateSampleCompletionReport()
    {
        BuildValidationSystem.GenerateTaskCompletionReport(
            taskName: "Audio System Integration Example",
            taskDescription: "Example task demonstrating how to generate completion reports for audio system implementation including spatial audio, pooling, and event systems.",
            filesModified: new[] {
                "Assets/scripts/Managers/AudioManager.cs",
                "Assets/scripts/Audio/SpatialAudioController.cs",
                "Assets/Docs/Audio Implementation Guide.md"
            },
            implementationDetails: @"
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
- Created modular audio configuration system",
            nextSteps: @"
1. **Testing Phase**: Test audio in different environments and scenarios
2. **Performance Validation**: Monitor audio performance in intensive scenarios  
3. **User Testing**: Gather feedback on audio clarity and spatial positioning
4. **Documentation Update**: Complete audio system documentation
5. **Integration Testing**: Verify audio works with all game mechanics"
        );
        
        Debug.Log("[TaskReportingExample] Sample task completion report generated in Assets/Docs/Execution/");
    }
    
    [MenuItem("InfinityQube/Examples/Generate Sample Task Validation Report")]
    public static void GenerateSampleValidationReport()
    {
        BuildValidationSystem.GenerateTaskValidationReport(
            taskName: "Audio System Integration Example",
            overallScore: 95,
            validationDetails: "Comprehensive validation of the audio system implementation confirms successful integration with excellent performance and minimal issues. All core functionality operational with minor optimization opportunities identified.",
            testsExecuted: new[] {
                "Audio System Compilation Test",
                "Spatial Audio Positioning Test", 
                "Audio Pooling Performance Test",
                "Audio Event System Integration Test",
                "Missing Asset Fallback Test",
                "Audio Configuration Validation Test"
            },
            issuesFound: new[] {
                "Audio clip loading could be optimized for faster startup",
                "Consider adding more granular volume controls",
                "Some edge cases in spatial positioning need refinement"
            },
            recommendations: new[] {
                "Monitor audio performance during extended gameplay sessions",
                "Consider implementing audio streaming for large files",
                "Add audio accessibility options for hearing-impaired players",
                "Implement audio quality settings for different hardware capabilities"
            }
        );
        
        Debug.Log("[TaskReportingExample] Sample task validation report generated in Assets/Docs/Execution/");
    }
    
    [MenuItem("InfinityQube/Examples/Generate Sample Task Failure Report")]
    public static void GenerateSampleFailureReport()
    {
        BuildValidationSystem.GenerateTaskFailureReport(
            taskName: "UI Modernization Example",
            errorDetails: "Task failed due to missing Unity UI components. OnGUI to Unity UI conversion could not be completed because required UI packages were not installed in the project. Additionally, some legacy UI scripts had compilation errors after attempting the conversion.",
            partialResults: @"
### Partial Progress Achieved:
- UI system architecture designed and documented
- Basic UI component mapping completed (OnGUI → Unity UI)
- UI folder structure created and organized
- Initial Canvas setup implemented for main menu

### Work Remaining:
- Complete Unity UI package installation
- Fix compilation errors in converted scripts
- Implement Unity UI event system integration
- Complete testing and validation of converted UI elements",
            recoverySteps: @"
### Recovery Actions:
1. **Package Installation**: Install Unity UI package via Package Manager
2. **Dependency Resolution**: Verify all required UI components are available
3. **Code Review**: Fix compilation errors in converted UI scripts
4. **Incremental Conversion**: Convert UI elements one at a time to isolate issues
5. **Testing Strategy**: Test each converted UI element before proceeding to next"
        );
        
        Debug.LogWarning("[TaskReportingExample] Sample task failure report generated in Assets/Docs/Execution/");
    }
    
    #endregion
    
    #region Example Usage in Code
    
    /// <summary>
    /// Example of how to call task reporting from within a script or external system
    /// </summary>
    public static void ExampleTaskCompletionFromCode()
    {
        // This could be called from a Shrimp Task Manager integration, build script, or other automation
        try
        {
            // Simulate task completion
            Debug.Log("[TaskReportingExample] Simulating task execution...");
            
            // Generate completion report
            BuildValidationSystem.GenerateTaskCompletionReport(
                taskName: "Scripted Task Example",
                taskDescription: "Example task executed from code to demonstrate reporting integration.",
                filesModified: new[] { "ExampleFile.cs" },
                implementationDetails: "Example implementation completed programmatically.",
                nextSteps: "Verify automated reporting works correctly."
            );
            
            Debug.Log("[TaskReportingExample] Automated task reporting completed successfully!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TaskReportingExample] Failed to generate automated report: {e.Message}");
            
            // Generate failure report
            BuildValidationSystem.GenerateTaskFailureReport(
                taskName: "Scripted Task Example",
                errorDetails: $"Automated task execution failed: {e.Message}",
                partialResults: "Task execution attempted but encountered errors.",
                recoverySteps: "Review error logs and fix underlying issues before retrying."
            );
        }
    }
    
    #endregion
}

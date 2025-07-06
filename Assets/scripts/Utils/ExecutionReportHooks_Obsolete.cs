using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Manages execution reports for task completion and validation.
/// Creates generic reports that are overwritten after each task execution.
/// Reports are stored in Assets/Docs/Execution/ folder.
/// </summary>
public static class ExecutionReportHooks
{
    #region Constants
    private const string EXECUTION_FOLDER = "Assets/Docs/Execution";
    private const string SUMMARY_REPORT_FILE = "SummaryReport.md";
    private const string VALIDATION_RESULT_FILE = "ValidationResult.md";
    #endregion

    #region Report Generation Hooks

    /// <summary>
    /// Hook called when a task is completed. Generates a summary report for the last executed task.
    /// </summary>
    /// <param name="taskName">Name of the completed task</param>
    /// <param name="taskDescription">Description of what the task accomplished</param>
    /// <param name="filesModified">Array of file paths that were modified</param>
    /// <param name="implementationDetails">Key implementation details and decisions</param>
    /// <param name="nextSteps">Recommended next steps or follow-up actions</param>
    public static void OnTaskCompleted(
        string taskName, 
        string taskDescription, 
        string[] filesModified, 
        string implementationDetails, 
        string nextSteps = "")
    {
        try
        {
            EnsureExecutionFolderExists();
            
            string summaryContent = GenerateSummaryReport(
                taskName, 
                taskDescription, 
                filesModified, 
                implementationDetails, 
                nextSteps
            );
            
            string summaryPath = Path.Combine(EXECUTION_FOLDER, SUMMARY_REPORT_FILE);
            File.WriteAllText(summaryPath, summaryContent);
            
            Debug.Log($"[ExecutionReportHooks] Summary report generated: {summaryPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ExecutionReportHooks] Failed to generate summary report: {e.Message}");
        }
    }

    /// <summary>
    /// Hook called when task validation is completed. Generates validation results for the last executed task.
    /// </summary>
    /// <param name="taskName">Name of the validated task</param>
    /// <param name="overallScore">Overall validation score (0-100)</param>
    /// <param name="validationDetails">Detailed validation results</param>
    /// <param name="testsExecuted">List of tests that were executed</param>
    /// <param name="issuesFound">Any issues or concerns identified</param>
    /// <param name="recommendations">Recommendations for improvement or follow-up</param>
    public static void OnTaskValidated(
        string taskName,
        int overallScore,
        string validationDetails,
        string[] testsExecuted,
        string[] issuesFound,
        string[] recommendations)
    {
        try
        {
            EnsureExecutionFolderExists();
            
            string validationContent = GenerateValidationReport(
                taskName,
                overallScore,
                validationDetails,
                testsExecuted,
                issuesFound,
                recommendations
            );
            
            string validationPath = Path.Combine(EXECUTION_FOLDER, VALIDATION_RESULT_FILE);
            File.WriteAllText(validationPath, validationContent);
            
            Debug.Log($"[ExecutionReportHooks] Validation report generated: {validationPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ExecutionReportHooks] Failed to generate validation report: {e.Message}");
        }
    }

    /// <summary>
    /// Hook called when a task fails or encounters errors during execution.
    /// </summary>
    /// <param name="taskName">Name of the failed task</param>
    /// <param name="errorDetails">Details about the failure</param>
    /// <param name="partialResults">Any partial progress or results</param>
    /// <param name="recoverySteps">Steps to recover or retry the task</param>
    public static void OnTaskFailed(
        string taskName,
        string errorDetails,
        string partialResults = "",
        string recoverySteps = "")
    {
        try
        {
            EnsureExecutionFolderExists();
            
            string failureContent = GenerateFailureReport(
                taskName,
                errorDetails,
                partialResults,
                recoverySteps
            );
            
            string summaryPath = Path.Combine(EXECUTION_FOLDER, SUMMARY_REPORT_FILE);
            File.WriteAllText(summaryPath, failureContent);
            
            Debug.LogWarning($"[ExecutionReportHooks] Failure report generated: {summaryPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ExecutionReportHooks] Failed to generate failure report: {e.Message}");
        }
    }
    #endregion

    #region Report Generation Methods

    private static string GenerateSummaryReport(
        string taskName, 
        string taskDescription, 
        string[] filesModified, 
        string implementationDetails, 
        string nextSteps)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        return $@"# Task Execution Summary Report

> **Task**: {taskName}  
> **Executed**: {timestamp}  
> **Status**: ✅ COMPLETED  

---

## Task Overview

{taskDescription}

---

## Implementation Summary

{implementationDetails}

---

## Files Modified

{(filesModified?.Length > 0 ? string.Join("\n", Array.ConvertAll(filesModified, f => $"- `{f}`")) : "- No files modified")}

---

## Next Steps

{(string.IsNullOrEmpty(nextSteps) ? "No specific next steps identified." : nextSteps)}

---

**Report Generated**: {timestamp}  
**Execution System**: InfinityQube Task Management Pipeline  
**Report Type**: Generic Summary (Overwritten per task)";
    }

    private static string GenerateValidationReport(
        string taskName,
        int overallScore,
        string validationDetails,
        string[] testsExecuted,
        string[] issuesFound,
        string[] recommendations)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        string status = overallScore >= 80 ? "✅ PASSED" : "⚠️ REQUIRES ATTENTION";
        
        return $@"# Task Validation Results

> **Task**: {taskName}  
> **Validated**: {timestamp}  
> **Overall Score**: {overallScore}/100  
> **Status**: {status}  

---

## Validation Summary

{validationDetails}

---

## Tests Executed

{(testsExecuted?.Length > 0 ? string.Join("\n", Array.ConvertAll(testsExecuted, t => $"- {t}")) : "- No specific tests executed")}

---

## Issues Identified

{(issuesFound?.Length > 0 ? string.Join("\n", Array.ConvertAll(issuesFound, i => $"- ⚠️ {i}")) : "- No issues identified")}

---

## Recommendations

{(recommendations?.Length > 0 ? string.Join("\n", Array.ConvertAll(recommendations, r => $"- {r}")) : "- No specific recommendations")}

---

**Validation Score Breakdown**:
- Requirements Compliance: {(overallScore >= 80 ? "✅" : "⚠️")} 
- Technical Quality: {(overallScore >= 80 ? "✅" : "⚠️")}
- Integration Compatibility: {(overallScore >= 80 ? "✅" : "⚠️")}
- Performance Impact: {(overallScore >= 80 ? "✅" : "⚠️")}

---

**Report Generated**: {timestamp}  
**Validation System**: Unity + Shrimp Task Manager Pipeline  
**Report Type**: Generic Validation (Overwritten per task)";
    }

    private static string GenerateFailureReport(
        string taskName,
        string errorDetails,
        string partialResults,
        string recoverySteps)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        
        return $@"# Task Execution Failure Report

> **Task**: {taskName}  
> **Failed**: {timestamp}  
> **Status**: ❌ FAILED  

---

## Failure Details

{errorDetails}

---

## Partial Progress

{(string.IsNullOrEmpty(partialResults) ? "No partial progress to report." : partialResults)}

---

## Recovery Steps

{(string.IsNullOrEmpty(recoverySteps) ? "No specific recovery steps identified." : recoverySteps)}

---

## Next Actions

1. Review failure details and error logs
2. Implement recovery steps if available
3. Consider task breakdown or alternative approach
4. Retry task execution after addressing root cause

---

**Report Generated**: {timestamp}  
**Execution System**: InfinityQube Task Management Pipeline  
**Report Type**: Generic Failure Report (Overwritten per task)";
    }
    #endregion

    #region Utility Methods

    private static void EnsureExecutionFolderExists()
    {
        if (!Directory.Exists(EXECUTION_FOLDER))
        {
            Directory.CreateDirectory(EXECUTION_FOLDER);
            Debug.Log($"[ExecutionReportHooks] Created execution folder: {EXECUTION_FOLDER}");
        }
    }

    /// <summary>
    /// Gets the path to the current summary report
    /// </summary>
    public static string GetSummaryReportPath()
    {
        return Path.Combine(EXECUTION_FOLDER, SUMMARY_REPORT_FILE);
    }

    /// <summary>
    /// Gets the path to the current validation report
    /// </summary>
    public static string GetValidationReportPath()
    {
        return Path.Combine(EXECUTION_FOLDER, VALIDATION_RESULT_FILE);
    }

    /// <summary>
    /// Clears all execution reports (useful for testing or manual cleanup)
    /// </summary>
    public static void ClearExecutionReports()
    {
        try
        {
            string summaryPath = GetSummaryReportPath();
            string validationPath = GetValidationReportPath();
            
            if (File.Exists(summaryPath)) File.Delete(summaryPath);
            if (File.Exists(validationPath)) File.Delete(validationPath);
            
            Debug.Log("[ExecutionReportHooks] Execution reports cleared");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ExecutionReportHooks] Failed to clear reports: {e.Message}");
        }
    }
    #endregion
}

#!/usr/bin/env bash
set -euo pipefail

# Script arguments from Shrimp
TASK_ID="$1"
RESULT_JSON="$2"

# Project configuration
PROJECT_PATH="C:\Users\awill\Unity\InfinityQube"
UNITY_PATH="/c/Program Files/Unity/Hub/Editor/2022.3.21f1/Editor/Unity.exe"  # Update this to your Unity version
BUILD_LOG="$PROJECT_PATH/CI/BuildLog.txt"
TEST_RESULTS="$PROJECT_PATH/CI/TestResults.xml"
BUILD_OUTPUT="$PROJECT_PATH/Builds"

# Ensure CI directory exists
mkdir -p "$PROJECT_PATH/CI"
mkdir -p "$BUILD_OUTPUT"

echo "=== Unity Build & Test Script ==="
echo "Task ID: $TASK_ID"
echo "Project Path: $PROJECT_PATH"
echo "Unity Path: $UNITY_PATH"
echo "Build Log: $BUILD_LOG"

# Function to check if Unity is available
check_unity() {
    if [[ ! -f "$UNITY_PATH" ]]; then
        echo "❌ Unity not found at $UNITY_PATH"
        echo "Please update UNITY_PATH in the script to match your Unity installation"
        return 1
    fi
    echo "✅ Unity found at $UNITY_PATH"
}

# Function to run Unity build
run_unity_build() {
    echo "Starting Unity build..."
    "$UNITY_PATH" \
        -batchmode -nographics -quit \
        -projectPath "$PROJECT_PATH" \
        -executeMethod CI.Build \
        -logFile "$BUILD_LOG" \
        -buildTarget Win64 \
        -customBuildPath "$BUILD_OUTPUT" || return $?
    
    echo "✅ Unity build completed"
    return 0
}

# Function to run Unity tests
run_unity_tests() {
    echo "Starting Unity tests..."
    "$UNITY_PATH" \
        -batchmode -nographics -quit \
        -projectPath "$PROJECT_PATH" \
        -runTests \
        -testResults "$TEST_RESULTS" \
        -testPlatform EditMode \
        -logFile "$BUILD_LOG" || return $?
    
    echo "✅ Unity tests completed"
    return 0
}

# Function to extract error summary from build log
extract_error_summary() {
    if [[ -f "$BUILD_LOG" ]]; then
        # Look for common Unity error patterns
        ERROR_SUMMARY=$(grep -m1 -E 'error CS[0-9]+:|Exception:|Error building Player:|Failed to build player' "$BUILD_LOG" | head -c 120 || echo "Unknown build error")
        echo "$ERROR_SUMMARY"
    else
        echo "Build log not found"
    fi
}

# Function to check for InfinityQube-specific issues
check_project_specific() {
    echo "Checking InfinityQube project specific requirements..."
    
    # Check if required scripts exist
    if [[ ! -f "$PROJECT_PATH/Assets/scripts/Utils/BuildInfo.cs" ]]; then
        echo "⚠️  Warning: BuildInfo.cs not found - build version info may be missing"
    fi
    
    # Check if CI build script exists
    if [[ ! -d "$PROJECT_PATH/Assets/Editor" ]] || [[ ! -f "$PROJECT_PATH/Assets/Editor/CI.cs" ]]; then
        echo "⚠️  Warning: CI.cs build script not found - creating basic build method"
        create_ci_build_script
    fi
    
    echo "✅ Project structure check completed"
}

# Function to create basic CI build script if missing
create_ci_build_script() {
    mkdir -p "$PROJECT_PATH/Assets/Editor"
    cat > "$PROJECT_PATH/Assets/Editor/CI.cs" << 'EOF'
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;

public class CI
{
    public static void Build()
    {
        var buildPath = GetBuildPath();
        var buildTarget = GetBuildTarget();
        
        Debug.Log($"Building to: {buildPath}");
        Debug.Log($"Target platform: {buildTarget}");
        
        var buildOptions = new BuildPlayerOptions
        {
            scenes = GetScenes(),
            locationPathName = buildPath,
            target = buildTarget,
            options = BuildOptions.None
        };
        
        var report = BuildPipeline.BuildPlayer(buildOptions);
        
        if (report.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build succeeded: {report.summary.totalSize} bytes");
            EditorApplication.Exit(0);
        }
        else
        {
            Debug.LogError($"Build failed: {report.result}");
            EditorApplication.Exit(1);
        }
    }
    
    private static string GetBuildPath()
    {
        var customPath = Environment.GetEnvironmentVariable("customBuildPath");
        if (!string.IsNullOrEmpty(customPath))
            return $"{customPath}/InfinityQube.exe";
        
        return "Builds/InfinityQube.exe";
    }
    
    private static BuildTarget GetBuildTarget()
    {
        var targetStr = Environment.GetEnvironmentVariable("buildTarget");
        return targetStr switch
        {
            "Win64" => BuildTarget.StandaloneWindows64,
            "Win32" => BuildTarget.StandaloneWindows,
            "Linux64" => BuildTarget.StandaloneLinux64,
            "OSX" => BuildTarget.StandaloneOSX,
            _ => BuildTarget.StandaloneWindows64
        };
    }
    
    private static string[] GetScenes()
    {
        var scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }
        return scenes;
    }
}
EOF
    echo "✅ Created basic CI build script"
}

# Main execution
main() {
    echo "=== Starting Build & Test Process ==="
    
    # Initial checks
    check_unity || {
        shrimp comment "$TASK_ID" "❌ Unity not found. Please update UNITY_PATH in build script."
        shrimp update-field "$TASK_ID" build_ok false
        exit 1
    }
    
    check_project_specific
    
    # Initialize build status
    BUILD_STATUS=0
    
    # Run Unity build
    echo "=== Unity Build Phase ==="
    run_unity_build || BUILD_STATUS=$?
    
    # Run Unity tests (only if build succeeded)
    if [[ $BUILD_STATUS -eq 0 ]]; then
        echo "=== Unity Test Phase ==="
        run_unity_tests || BUILD_STATUS=$?
    else
        echo "⚠️  Skipping tests due to build failure"
    fi
    
    # Report results back to Shrimp
    if [[ $BUILD_STATUS -eq 0 ]]; then
        echo "=== SUCCESS ==="
        shrimp comment "$TASK_ID" "✅ Unity build & tests passed successfully!"
        shrimp update-field "$TASK_ID" build_ok true
        shrimp update-field "$TASK_ID" scene "Main"
        shrimp update-field "$TASK_ID" subsystem "Build"
        
        # Optional: Show build artifacts
        if [[ -f "$BUILD_OUTPUT/InfinityQube.exe" ]]; then
            BUILD_SIZE=$(stat -c%s "$BUILD_OUTPUT/InfinityQube.exe" 2>/dev/null || echo "unknown")
            shrimp comment "$TASK_ID" "📦 Build artifact: InfinityQube.exe (${BUILD_SIZE} bytes)"
        fi
        
        echo "Build completed successfully!"
        exit 0
    else
        echo "=== FAILURE ==="
        ERROR_SUMMARY=$(extract_error_summary)
        echo "Error: $ERROR_SUMMARY"
        
        # Create follow-up task for build failures
        shrimp add-task --priority 0 "Fix Unity build failure: $ERROR_SUMMARY" --parent "$TASK_ID"
        shrimp comment "$TASK_ID" "❌ Build failed. Created follow-up task. Check build log: $BUILD_LOG"
        shrimp update-field "$TASK_ID" build_ok false
        shrimp update-field "$TASK_ID" subsystem "Build"
        
        # Show last few lines of build log for quick debugging
        if [[ -f "$BUILD_LOG" ]]; then
            echo "=== Last 10 lines from build log ==="
            tail -10 "$BUILD_LOG"
        fi
        
        exit 1  # Stops continuous mode until build issue is resolved
    fi
}

# Execute main function
main "$@"
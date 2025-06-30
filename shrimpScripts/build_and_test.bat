@echo off
setlocal enabledelayedexpansion

REM Script arguments from Shrimp
set TASK_ID=%1
set RESULT_JSON=%2

REM Project configuration - UPDATE THESE PATHS FOR YOUR SYSTEM
set PROJECT_PATH=C:\Users\awill\Unity\InfinityQube
set UNITY_PATH=C:\Program Files\Unity\Hub\Editor\2022.3.21f1\Editor\Unity.exe
set BUILD_LOG=%PROJECT_PATH%\CI\BuildLog.txt
set TEST_RESULTS=%PROJECT_PATH%\CI\TestResults.xml
set BUILD_OUTPUT=%PROJECT_PATH%\Builds

echo === Unity Build ^& Test Script ===
echo Task ID: %TASK_ID%
echo Project Path: %PROJECT_PATH%
echo Unity Path: %UNITY_PATH%
echo Build Log: %BUILD_LOG%

REM Ensure CI directory exists
if not exist "%PROJECT_PATH%\CI" mkdir "%PROJECT_PATH%\CI"
if not exist "%BUILD_OUTPUT%" mkdir "%BUILD_OUTPUT%"

REM Check if Unity exists
if not exist "%UNITY_PATH%" (
    echo ❌ Unity not found at %UNITY_PATH%
    echo Please update UNITY_PATH in the script to match your Unity installation
    shrimp comment %TASK_ID% "❌ Unity not found. Please update UNITY_PATH in build script."
    shrimp update-field %TASK_ID% build_ok false
    exit /b 1
)

echo ✅ Unity found at %UNITY_PATH%

REM Self-healing checks
echo === Self-Healing Checks ===

REM Check if CI build script exists
if not exist "%PROJECT_PATH%\Assets\Editor\CI.cs" (
    echo ⚠️ Warning: CI.cs build script not found - you may need to create it
    echo Creating basic CI build script...
    if not exist "%PROJECT_PATH%\Assets\Editor" mkdir "%PROJECT_PATH%\Assets\Editor"
    
    REM Create basic CI.cs file
    > "%PROJECT_PATH%\Assets\Editor\CI.cs" (
        echo using UnityEngine;
        echo using UnityEditor;
        echo using UnityEditor.Build.Reporting;
        echo using System;
        echo.
        echo public class CI
        echo {
        echo     public static void Build^(^)
        echo     {
        echo         var buildPath = GetBuildPath^(^);
        echo         var buildTarget = GetBuildTarget^(^);
        echo.        
        echo         Debug.Log^($"Building to: {buildPath}"^);
        echo         Debug.Log^($"Target platform: {buildTarget}"^);
        echo.        
        echo         var buildOptions = new BuildPlayerOptions
        echo         {
        echo             scenes = GetScenes^(^),
        echo             locationPathName = buildPath,
        echo             target = buildTarget,
        echo             options = BuildOptions.None
        echo         };
        echo.        
        echo         var report = BuildPipeline.BuildPlayer^(buildOptions^);
        echo.        
        echo         if ^(report.result == BuildResult.Succeeded^)
        echo         {
        echo             Debug.Log^($"Build succeeded: {report.summary.totalSize} bytes"^);
        echo             EditorApplication.Exit^(0^);
        echo         }
        echo         else
        echo         {
        echo             Debug.LogError^($"Build failed: {report.result}"^);
        echo             EditorApplication.Exit^(1^);
        echo         }
        echo     }
        echo.    
        echo     private static string GetBuildPath^(^)
        echo     {
        echo         return "Builds/InfinityQube.exe";
        echo     }
        echo.    
        echo     private static BuildTarget GetBuildTarget^(^)
        echo     {
        echo         return BuildTarget.StandaloneWindows64;
        echo     }
        echo.    
        echo     private static string[] GetScenes^(^)
        echo     {
        echo         var scenes = new string[EditorBuildSettings.scenes.Length];
        echo         for ^(int i = 0; i ^< scenes.Length; i++^)
        echo         {
        echo             scenes[i] = EditorBuildSettings.scenes[i].path;
        echo         }
        echo         return scenes;
        echo     }
        echo }
    )
    echo ✅ Created basic CI build script
    shrimp comment %TASK_ID% "🔧 Self-healed: Created missing CI.cs build script"
) else (
    echo ✅ CI build script exists
)

REM Check for common Unity project issues and auto-fix
if not exist "%PROJECT_PATH%\ProjectSettings\ProjectSettings.asset" (
    echo ❌ Critical: ProjectSettings.asset missing - this is not a valid Unity project
    shrimp comment %TASK_ID% "❌ Invalid Unity project detected. ProjectSettings missing."
    shrimp update-field %TASK_ID% build_ok false
    exit /b 1
)

REM Check Unity version compatibility
findstr /c:"m_EditorVersion:" "%PROJECT_PATH%\ProjectSettings\ProjectVersion.txt" > nul 2>&1
if errorlevel 1 (
    echo ⚠️ Warning: Could not detect Unity version in project
) else (
    echo ✅ Unity project version file found
)

REM Auto-clean common Unity cache issues
if exist "%PROJECT_PATH%\Library\StateCache" (
    echo 🧹 Cleaning Unity state cache for fresh build...
    rmdir /s /q "%PROJECT_PATH%\Library\StateCache" 2>nul
    shrimp comment %TASK_ID% "🧹 Self-healed: Cleared Unity state cache"
)

echo ✅ Self-healing checks completed

echo === Unity Build Phase ===
echo Starting Unity build...

REM Start timing
set BUILD_START_TIME=%time%
powershell -Command "$start = Get-Date; $start.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')" > build_start.tmp

"%UNITY_PATH%" -batchmode -nographics -quit -projectPath "%PROJECT_PATH%" -executeMethod CI.Build -logFile "%BUILD_LOG%" -buildTarget Win64

set BUILD_STATUS=%ERRORLEVEL%

REM Calculate build time
set BUILD_END_TIME=%time%
powershell -Command "$start = Get-Date (Get-Content build_start.tmp); $end = Get-Date; $duration = ($end - $start).TotalMilliseconds; [int]$duration" > build_duration.tmp
set /p BUILD_TIME_MS=<build_duration.tmp
del build_start.tmp build_duration.tmp 2>nul

if %BUILD_STATUS% equ 0 (
    echo ✅ Unity build completed (took %BUILD_TIME_MS%ms)
    echo === Unity Test Phase ===
    echo Starting Unity tests...
    
    REM Start test timing
    powershell -Command "$start = Get-Date; $start.ToString('yyyy-MM-ddTHH:mm:ss.fffZ')" > test_start.tmp
    
    "%UNITY_PATH%" -batchmode -nographics -quit -projectPath "%PROJECT_PATH%" -runTests -testResults "%TEST_RESULTS%" -testPlatform EditMode -logFile "%BUILD_LOG%"
    set BUILD_STATUS=!ERRORLEVEL!
    
    REM Calculate test time
    powershell -Command "$start = Get-Date (Get-Content test_start.tmp); $end = Get-Date; $duration = ($end - $start).TotalMilliseconds; [int]$duration" > test_duration.tmp
    set /p TEST_TIME_MS=<test_duration.tmp
    del test_start.tmp test_duration.tmp 2>nul
    
    if !BUILD_STATUS! equ 0 (
        echo ✅ Unity tests completed (took !TEST_TIME_MS!ms)
    )
) else (
    echo ⚠️ Skipping tests due to build failure
    set TEST_TIME_MS=0
)

REM Report results back to Shrimp
if %BUILD_STATUS% equ 0 (
    echo === SUCCESS ===
    
    REM Analytics: Update healthy streak
    powershell -Command "Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffZ'" > success_time.tmp
    set /p SUCCESS_TIME=<success_time.tmp
    del success_time.tmp 2>nul
    
    shrimp comment %TASK_ID% "✅ Unity build & tests passed! Build: %BUILD_TIME_MS%ms, Tests: %TEST_TIME_MS%ms"
    shrimp update-field %TASK_ID% build_ok true
    shrimp update-field %TASK_ID% build_time_ms %BUILD_TIME_MS%
    shrimp update-field %TASK_ID% test_time_ms %TEST_TIME_MS%
    shrimp update-field %TASK_ID% scene "Main"
    shrimp update-field %TASK_ID% subsystem "Build"
    shrimp update-field %TASK_ID% self_healed false
    
    if exist "%BUILD_OUTPUT%\InfinityQube.exe" (
        for %%A in ("%BUILD_OUTPUT%\InfinityQube.exe") do (
            shrimp comment %TASK_ID% "📦 Build artifact: InfinityQube.exe (%%~zA bytes)"
        )
    )
    
    echo Build completed successfully!
    exit /b 0
) else (
    echo === FAILURE ===
    
    REM Extract error from build log
    set ERROR_SUMMARY=Unknown build error
    if exist "%BUILD_LOG%" (
        for /f "tokens=*" %%i in ('findstr /c:"error CS" /c:"Exception:" /c:"Error building Player:" "%BUILD_LOG%" 2^>nul') do (
            set ERROR_SUMMARY=%%i
            goto :found_error
        )
        :found_error
    )
    
    echo Error: !ERROR_SUMMARY!
    
    REM Analytics: Record failure details
    powershell -Command "Get-Date -Format 'yyyy-MM-ddTHH:mm:ss.fffZ'" > failure_time.tmp
    set /p FAILURE_TIME=<failure_time.tmp
    del failure_time.tmp 2>nul
    
    REM 🔴 STOP-ON-RED: Create blocking follow-up task
    shrimp add-task --priority 0 "Fix Unity build failure: !ERROR_SUMMARY!" --parent %TASK_ID%
    shrimp comment %TASK_ID% "❌ 🔴 BUILD BROKEN - Continuous mode STOPPED. Fix required before proceeding."
    shrimp update-field %TASK_ID% build_ok false
    shrimp update-field %TASK_ID% build_time_ms %BUILD_TIME_MS%
    shrimp update-field %TASK_ID% test_time_ms 0
    shrimp update-field %TASK_ID% failure_reason "!ERROR_SUMMARY!"
    shrimp update-field %TASK_ID% last_red_at "!FAILURE_TIME!"
    shrimp update-field %TASK_ID% subsystem "Build"
    
    REM Show last few lines of build log for quick debugging
    if exist "%BUILD_LOG%" (
        echo === Last lines from build log ===
        powershell "Get-Content '%BUILD_LOG%' | Select-Object -Last 10"
    )
    
    exit /b 1
)

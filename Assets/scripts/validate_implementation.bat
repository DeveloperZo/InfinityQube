@echo off
echo ========================================
echo InfinityQube Validation Pipeline
echo ========================================
echo Starting Unity Validation Pipeline...
echo.

REM Get Unity Editor path - adjust this to match your Unity installation
set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2022.3.47f1\Editor\Unity.exe"

REM Check if Unity exists
if not exist %UNITY_PATH% (
    echo ERROR: Unity Editor not found at %UNITY_PATH%
    echo Please update the UNITY_PATH in this batch file to match your Unity installation
    echo.
    pause
    exit /b 1
)

REM Launch Unity in batch mode with custom validation
echo Running Unity validation in batch mode...
%UNITY_PATH% ^
  -batchmode ^
  -quit ^
  -projectPath "%~dp0\.." ^
  -executeMethod InfinityQube.Testing.BuildValidationSystem.RunFullValidationPipeline ^
  -logFile validation_log.txt

echo.
echo ========================================
echo Validation Results
echo ========================================

REM Check if validation completed successfully
if exist "..\Assets\Docs\Technical Doc\ValidationResults.md" (
    echo ✅ Validation completed successfully!
    echo.
    echo Current Validation Results:
    type "..\Assets\Docs\Technical Doc\ValidationResults.md"
    echo.
    echo ========================================
    echo Current Handoff Report:
    type "..\Assets\Docs\Technical Doc\HandoffReport.md"
) else (
    echo ❌ Validation failed to complete
    echo Check validation_log.txt for detailed error information
    if exist validation_log.txt (
        echo.
        echo Last few lines of log:
        powershell "Get-Content validation_log.txt -Tail 20"
    )
    exit /b 1
)

echo.
echo ========================================
echo Validation Pipeline Complete
echo ========================================
pause
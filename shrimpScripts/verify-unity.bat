@echo off
REM Unity Build Validation for Shrimp Workflow
REM Usage: verify-unity.bat

set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2022.3.9f1\Editor\Unity.exe"
set PROJECT_PATH="C:\Users\awill\Unity\InfinityQube"

echo [Verify Unity] Starting validation...
%UNITY_PATH% -batchmode -quit -projectPath %PROJECT_PATH% -executeMethod InfinityQube.Testing.BuildValidationSystem.ValidateForShrimp -logFile unity-validation.log

if %ERRORLEVEL% EQU 0 (
    echo [Verify Unity] VALIDATION PASSED
) else if %ERRORLEVEL% EQU 1 (
    echo [Verify Unity] VALIDATION FAILED
) else (
    echo [Verify Unity] ERROR: Unity validation crashed
)

exit /b %ERRORLEVEL%
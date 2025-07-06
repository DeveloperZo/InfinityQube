@echo off
REM Unity Build Validation for Shrimp Workflow
REM Usage: verify-unity.bat

set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2022.3.21f1\Editor\Unity.exe"
set PROJECT_PATH="C:\Users\awill\Unity\InfinityQube"

echo [Verify Unity] Checking Unity installation...

REM Check if Unity exists at the expected path
if not exist %UNITY_PATH% (
    echo [Verify Unity] ERROR: Unity not found at %UNITY_PATH%
    echo [Verify Unity] Please check your Unity installation path
    echo [Verify Unity] Common locations:
    echo   - C:\Program Files\Unity\Hub\Editor\2022.3.21f1\Editor\Unity.exe
    echo   - C:\Program Files (x86)\Unity\Hub\Editor\2022.3.21f1\Editor\Unity.exe
    echo   - %LOCALAPPDATA%\Unity\Hub\Editor\2022.3.21f1\Editor\Unity.exe
    exit /b 3
)

echo [Verify Unity] Unity found at %UNITY_PATH%
echo [Verify Unity] Starting validation...
echo [Verify Unity] Project: %PROJECT_PATH%

%UNITY_PATH% -batchmode -quit -projectPath %PROJECT_PATH% -executeMethod InfinityQube.Testing.BuildValidationSystem.ValidateForShrimp -logFile unity-validation.log

if %ERRORLEVEL% EQU 0 (
    echo [Verify Unity] VALIDATION PASSED
    type unity-validation.log | findstr /C:"SHRIMP_JSON_RESULT"
) else if %ERRORLEVEL% EQU 1 (
    echo [Verify Unity] VALIDATION FAILED
    type unity-validation.log | findstr /C:"[BuildValidationSystem]" | findstr /C:"Score"
) else (
    echo [Verify Unity] ERROR: Unity validation crashed
    echo [Verify Unity] Check unity-validation.log for details
)

exit /b %ERRORLEVEL%
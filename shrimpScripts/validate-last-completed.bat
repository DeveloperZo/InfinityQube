@echo off
REM Generate validation reports for the last completed task
REM Usage: validate-last-completed.bat

set UNITY_PATH="C:\Program Files\Unity\Hub\Editor\2022.3.9f1\Editor\Unity.exe"
set PROJECT_PATH="C:\Users\awill\Unity\InfinityQube"

echo [Validate Last Completed] Generating reports for last completed task...

%UNITY_PATH% -batchmode -quit -projectPath %PROJECT_PATH% -executeMethod InfinityQube.Testing.BuildValidationSystem.ValidateLastCompleted -logFile validate-last.log

if %ERRORLEVEL% EQU 0 (
    echo [Validate Last Completed] VALIDATION PASSED
    echo.
    echo Reports generated:
    echo - Assets\Docs\Execution\ValidationResult.md
    echo - Assets\Docs\Execution\SummaryReport.md
) else if %ERRORLEVEL% EQU 1 (
    echo [Validate Last Completed] VALIDATION FAILED
    echo Check reports for details.
) else (
    echo [Validate Last Completed] ERROR: Failed to generate reports
    type validate-last.log
)

del validate-last.log 2>nul
exit /b %ERRORLEVEL%
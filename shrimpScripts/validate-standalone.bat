@echo off
REM Standalone Unity project validator - no Unity required!
REM Usage: validate-standalone.bat

echo [Standalone Validator] Validating Unity project...
echo [Standalone Validator] No Unity instance required!

node "%~dp0validate-project.js"

if %ERRORLEVEL% EQU 0 (
    echo.
    echo [Standalone Validator] VALIDATION PASSED
    echo Reports generated:
    echo - Assets\Docs\Execution\ValidationResult.md
    echo - Assets\Docs\Execution\SummaryReport.md
) else (
    echo.
    echo [Standalone Validator] VALIDATION FAILED
    echo Check reports for details.
)

exit /b %ERRORLEVEL%
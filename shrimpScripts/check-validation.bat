@echo off
REM Check validation results from Unity (while Unity is open)
REM Usage: check-validation.bat

set RESULT_FILE="validation-result.json"

echo [Check Validation] Looking for validation results...

if exist %RESULT_FILE% (
    echo [Check Validation] Found results:
    echo.
    type %RESULT_FILE%
    echo.
    
    REM Parse the JSON to check if passed
    findstr /C:"\"passed\": true" %RESULT_FILE% >nul
    if %ERRORLEVEL% EQU 0 (
        echo [Check Validation] STATUS: PASSED
        exit /b 0
    ) else (
        echo [Check Validation] STATUS: FAILED
        exit /b 1
    )
) else (
    echo [Check Validation] No validation results found.
    echo [Check Validation] Run validation in Unity: Menu -^> InfinityQube -^> Quick Validation (While Open)
    exit /b 2
)
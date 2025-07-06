# Unity Build Validation for Shrimp Workflow
# Usage: ./verify-unity.ps1

$unityPath = "C:\Program Files\Unity\Hub\Editor\2022.3.21f1\Editor\Unity.exe"
$projectPath = "C:\Users\awill\Unity\InfinityQube"
$logFile = "unity-validation.log"

Write-Host "[Verify Unity] Starting validation..." -ForegroundColor Cyan

# Run Unity validation
& $unityPath -batchmode -quit -projectPath $projectPath -executeMethod InfinityQube.Testing.BuildValidationSystem.ValidateForShrimp -logFile $logFile

$exitCode = $LASTEXITCODE

# Read log and extract JSON result
if (Test-Path $logFile) {
    $content = Get-Content $logFile -Raw
    if ($content -match "SHRIMP_JSON_RESULT: (.+)") {
        $jsonResult = $matches[1]
        try {
            $result = $jsonResult | ConvertFrom-Json
            Write-Host "[Verify Unity] Task: $($result.task)" -ForegroundColor Gray
            Write-Host "[Verify Unity] Score: $($result.score)/100" -ForegroundColor $(if ($result.passed) { "Green" } else { "Red" })
            Write-Host "[Verify Unity] Status: $(if ($result.passed) { 'PASSED' } else { 'FAILED' })" -ForegroundColor $(if ($result.passed) { "Green" } else { "Red" })
        } catch {
            Write-Host "[Verify Unity] Could not parse JSON result" -ForegroundColor Yellow
        }
    }
}

# Clean up
Remove-Item $logFile -ErrorAction SilentlyContinue

exit $exitCode
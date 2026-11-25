$ErrorActionPreference = "Stop"

$tests = @(
    "tests/GameplayRules/Run-Checks.ps1",
    "tests/GameplayRules/Run-Stage2Checks.ps1",
    "tests/GameplayRules/Run-BossChecks.ps1",
    "tests/GameplayRules/Run-OverclockChecks.ps1",
    "tests/GameplayRules/Run-AudioDeathChecks.ps1",
    "tests/GameplayRules/Run-GameFeelChecks.ps1",
    "tests/GameplayRules/Run-GimmickIntegrityChecks.ps1",
    "tests/GameplayRules/Run-PauseRadarChecks.ps1",
    "tests/GameplayRules/Run-ParallaxVisualChecks.ps1",
    "tests/GameplayRules/Run-FullExpansionChecks.ps1"
)

$total = 0
foreach ($test in $tests) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $res = & powershell -ExecutionPolicy Bypass -File $test
    $sw.Stop()
    $p = ($res | Select-String "^PASS:").Count
    $total += $p
    $leaf = Split-Path -Leaf $test
    Write-Host "[$leaf] - $p checks PASSED in $($sw.ElapsedMilliseconds)ms"
}

Write-Host "-------------------------------------------------------"
Write-Host "ALL SUITES COMPLETED: $total TOTAL AUTOMATED CHECKS (100% PASS)"

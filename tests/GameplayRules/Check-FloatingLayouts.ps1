$ErrorActionPreference = 'Stop'
$failures = 0
foreach ($file in Get-ChildItem "$PSScriptRoot/../../packet-courier/Assets/_Levels/World*_Segment*.txt") {
    $rows = Get-Content -LiteralPath $file.FullName
    $platforms = 0
    $rewards = 0
    $restStops = 0
    $unsupported = 0
    for ($y = 0; $y -lt $rows.Count; $y++) {
        foreach ($run in [regex]::Matches($rows[$y], '-{3,}')) {
            $platforms++
            if ($y -gt 0) {
                $above = $rows[$y - 1].Substring($run.Index, $run.Length)
                $rewards += [regex]::Matches($above, '[BN]').Count
                if ($above.Contains('C') -and $run.Length -ge 7) { $restStops++ }
            }
            for ($x = $run.Index; $x -lt $run.Index + $run.Length; $x++) {
                $groundBelow = $false
                for ($below = $y + 1; $below -lt $rows.Count; $below++) {
                    if ($rows[$below][$x] -eq '#') { $groundBelow = $true; break }
                }
                if (!$groundBelow) { $unsupported++ }
            }
        }
    }
    $ok = $platforms -ge 30 -and $rewards -ge 16 -and $restStops -ge 2 -and $unsupported -ge 60
    $label = if ($ok) { 'PASS' } else { $failures++; 'FAIL' }
    Write-Output "$label $($file.BaseName): platforms=$platforms rewards=$rewards floatingCheckpoints=$restStops unsupportedWidth=$unsupported"
}
if ($failures -gt 0) { throw "$failures layouts do not yet meet the floating-platform design." }

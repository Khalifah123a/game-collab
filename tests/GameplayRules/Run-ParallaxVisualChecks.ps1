$ErrorActionPreference = "Stop"
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
    $csc = (Get-ChildItem -Path "C:\Windows\Microsoft.NET\Framework64\v4.0.30319" -Filter "csc.exe" -Recurse | Select-Object -First 1).FullName
}

$testScript = Join-Path $PSScriptRoot "ParallaxVisualChecks.cs"
$outputExe = Join-Path $PSScriptRoot "ParallaxVisualChecks.exe"

& $csc /nologo /out:$outputExe $testScript
if ($LASTEXITCODE -ne 0) {
    Write-Error "Gagal mengkompilasi ParallaxVisualChecks.cs"
    exit 1
}

& $outputExe
$testExit = $LASTEXITCODE
Remove-Item -Force -ErrorAction SilentlyContinue $outputExe
exit $testExit

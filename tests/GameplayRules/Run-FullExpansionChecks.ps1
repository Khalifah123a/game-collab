$ErrorActionPreference = "Stop"

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$packetRoot = Resolve-Path (Join-Path $here "..\..\packet-courier")
$outDir = Join-Path $here "..\..\artifacts\bin"
if (!(Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }
$exe = Join-Path $outDir "FullExpansionChecks.exe"

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (!(Test-Path $csc)) {
    Write-Host "CSC compiler not found at $csc"
    exit 1
}

$source = Join-Path $here "FullExpansionChecks.cs"

& $csc /nologo /target:exe /out:$exe $source
if ($LASTEXITCODE -ne 0) {
    Write-Host "CSC compilation failed"
    exit $LASTEXITCODE
}

& $exe $packetRoot
exit $LASTEXITCODE

$ErrorActionPreference = 'Stop'
$packetRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$output = Join-Path $packetRoot 'artifacts/AudioDeathChecks.exe'
New-Item -ItemType Directory -Path (Join-Path $packetRoot 'artifacts') -Force | Out-Null
& $compiler /nologo /r:Microsoft.CSharp.dll "/out:$output" `
    (Join-Path $PSScriptRoot 'AudioDeathChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'AudioDeath compilation failed.' }
& $output "$packetRoot"
if ($LASTEXITCODE -ne 0) { throw 'Audio and Death Screen checks failed.' }

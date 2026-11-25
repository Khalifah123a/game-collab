$ErrorActionPreference = 'Stop'
$packetRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$bossCompiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$bossOutput = Join-Path $packetRoot 'artifacts/BossChecks.exe'
New-Item -ItemType Directory -Path (Join-Path $packetRoot 'artifacts') -Force | Out-Null
& $bossCompiler /nologo /r:Microsoft.CSharp.dll "/out:$bossOutput" `
    (Join-Path $PSScriptRoot 'BossChecks.cs')
if ($LASTEXITCODE -ne 0) { throw 'Boss checks compilation failed.' }
& $bossOutput
if ($LASTEXITCODE -ne 0) { throw 'Boss checks failed.' }

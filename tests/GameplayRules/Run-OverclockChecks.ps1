$ErrorActionPreference = 'Stop'
$packetRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$output = Join-Path $packetRoot 'artifacts/OverclockChecks.exe'
New-Item -ItemType Directory -Path (Join-Path $packetRoot 'artifacts') -Force | Out-Null
& $compiler /nologo /r:Microsoft.CSharp.dll "/out:$output" `
    (Join-Path $PSScriptRoot 'OverclockChecks.cs') `
    (Join-Path $packetRoot 'packet-courier/Assets/_Scripts/UI/ShopCatalog.cs') `
    (Join-Path $packetRoot 'packet-courier/Assets/_Scripts/UI/OverclockCatalog.cs')
if ($LASTEXITCODE -ne 0) { throw 'Overclock compilation failed.' }
& $output
if ($LASTEXITCODE -ne 0) { throw 'Overclock system checks failed.' }

$ErrorActionPreference = 'Stop'
$packetRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$stage2Compiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$stage2Output = Join-Path $packetRoot 'artifacts/Stage2Checks.exe'
New-Item -ItemType Directory -Path (Join-Path $packetRoot 'artifacts') -Force | Out-Null
& $stage2Compiler /nologo /r:Microsoft.CSharp.dll "/out:$stage2Output" `
    (Join-Path $PSScriptRoot 'Stage2OptionalShopLogic.cs') `
    (Join-Path $packetRoot 'packet-courier/Assets/_Scripts/Core/RunLedger.cs') `
    (Join-Path $packetRoot 'packet-courier/Assets/_Scripts/Quest/OptionalQuestService.cs') `
    (Join-Path $packetRoot 'packet-courier/Assets/_Scripts/UI/ShopCatalog.cs')
if ($LASTEXITCODE -ne 0) { throw 'Stage 2 compilation failed.' }
& $stage2Output
if ($LASTEXITCODE -ne 0) { throw 'Stage 2 optional/shop logic checks failed.' }

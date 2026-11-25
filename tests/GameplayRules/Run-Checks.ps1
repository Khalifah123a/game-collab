$ErrorActionPreference = 'Stop'
$packetRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$packetCompiler = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319/csc.exe'
$packetOutput = Join-Path $packetRoot 'artifacts/GameplayChecks.exe'
New-Item -ItemType Directory -Path (Join-Path $packetRoot 'artifacts') -Force | Out-Null
& $packetCompiler /nologo /r:Microsoft.CSharp.dll "/out:$packetOutput" (Join-Path $PSScriptRoot 'Program.cs') (Join-Path $PSScriptRoot 'PackageRuleChecks.cs') (Join-Path $packetRoot 'packet-courier/Assets/_Scripts/Player/StaminaState.cs') (Join-Path $packetRoot 'packet-courier/Assets/_Scripts/Core/RunLedger.cs') (Join-Path $packetRoot 'packet-courier/Assets/_Scripts/Quest/PackageQuestState.cs')
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
& $packetOutput
if ($LASTEXITCODE -ne 0) { throw 'Gameplay checks failed.' }

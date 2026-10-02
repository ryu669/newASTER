param([string]$DotNet='C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Data\DotNetSdk\dotnet.exe')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo 'tmp/automatic-chain-validation'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$sdkRoot=Split-Path $DotNet -Parent
$sdk=Get-ChildItem (Join-Path $sdkRoot 'sdk') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$pack=Get-ChildItem (Join-Path $sdkRoot 'packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$refs=Get-ChildItem (Join-Path $pack.FullName 'ref/net8.0') -Filter '*.dll'
$sources=Get-ChildItem (Join-Path $repo 'game/unity/Assets/Game/Scripts/Core'),(Join-Path $repo 'game/unity/Assets/Game/Scripts/Data') -Filter '*.cs'
$assembly=Join-Path $output 'AutomaticChainTests.dll'
$lines=@('-nologo','-nostdlib+','-target:exe','-langversion:9',('-out:"'+$assembly+'"'))
$lines+=$refs | ForEach-Object {'-r:"'+$_.FullName+'"'}
$lines+=$sources | ForEach-Object {'"'+$_.FullName+'"'}
$lines+='"'+(Join-Path $PSScriptRoot 'AutomaticChainTests.cs')+'"'
$response=Join-Path $output 'compile.rsp'
[IO.File]::WriteAllLines($response,$lines,[Text.UTF8Encoding]::new($false))
& $DotNet (Join-Path $sdk.FullName 'Roslyn/bincore/csc.dll') ('@'+$response)
if($LASTEXITCODE -ne 0){throw 'Automatic chain compile failed'}
[IO.File]::WriteAllText((Join-Path $output 'AutomaticChainTests.runtimeconfig.json'),'{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}')
& $DotNet $assembly
if($LASTEXITCODE -ne 0){throw 'Automatic chain tests failed'}

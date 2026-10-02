param(
    [string]$UnityData = 'C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Data'
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
$output = Join-Path $repo 'tmp/unity-compile'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$dotnet = Join-Path $UnityData 'DotNetSdk/dotnet.exe'
$sdk = Get-ChildItem (Join-Path $UnityData 'DotNetSdk/sdk') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$refs = @(Get-ChildItem (Join-Path $UnityData 'NetStandard/ref/2.1.0') -Filter '*.dll')
$refs += @(Get-ChildItem (Join-Path $UnityData 'Managed') -Recurse -Filter '*.dll')
$sources = @(Get-ChildItem (Join-Path $repo 'game/unity/Assets/Game') -Recurse -Filter '*.cs')
$coreSources = @(Get-ChildItem (Join-Path $repo 'game/unity/Assets/Game/Scripts/Core') -Filter '*.cs')
$coreAssembly = Join-Path $output 'NewAster.Core.dll'
$coreLines = @('-nologo','-nostdlib+','-target:library','-langversion:9',('-out:"'+$coreAssembly+'"'))
$coreLines += Get-ChildItem (Join-Path $UnityData 'NetStandard/ref/2.1.0') -Filter '*.dll' | ForEach-Object { '-r:"'+$_.FullName+'"' }
$coreLines += $coreSources | ForEach-Object { '"'+$_.FullName+'"' }
$coreResponse = Join-Path $output 'core-compile.rsp'
[IO.File]::WriteAllLines($coreResponse, $coreLines, [Text.UTF8Encoding]::new($false))
& $dotnet (Join-Path $sdk.FullName 'Roslyn/bincore/csc.dll') ('@'+$coreResponse)
if ($LASTEXITCODE -ne 0) { throw "Unity Core assembly compile failed: $LASTEXITCODE" }
$response = Join-Path $output 'compile.rsp'
$lines = @('-nologo','-nostdlib+','-target:library','-langversion:9',('-out:"'+(Join-Path $output 'NewAster.CompileCheck.dll')+'"'))
$lines += $refs | ForEach-Object { '-r:"'+$_.FullName+'"' }
$lines += '-r:"'+$coreAssembly+'"'
$lines += $sources | Where-Object { $_.FullName -notin $coreSources.FullName } | ForEach-Object { '"'+$_.FullName+'"' }
[IO.File]::WriteAllLines($response, $lines, [Text.UTF8Encoding]::new($false))
& $dotnet (Join-Path $sdk.FullName 'Roslyn/bincore/csc.dll') ('@'+$response)
if ($LASTEXITCODE -ne 0) { throw "Unity script compile failed: $LASTEXITCODE" }
Write-Output "UNITY_SCRIPT_COMPILE_PASS $($sources.Count) source files"
# This checks Core isolation and C# compilation against Unity APIs, not imports,
# runtime UI, serialization or the Windows player build.

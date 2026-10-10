param([string]$Output='tmp/plan11-10-balance.json',[switch]$Equipment,[switch]$Arsenal,[switch]$Progression,[ValidateSet('','Matrix','Journey','FinishJourney','Audit','Teams','TeamFollowup')][string]$Final='')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$runtime='C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Data\DotNetSdk'
$dotnet=Join-Path $runtime 'dotnet.exe'
$sdk=Get-ChildItem "$runtime/sdk" -Directory | Sort-Object Name -Descending | Select-Object -First 1
$pack=Get-ChildItem "$runtime/packs/Microsoft.NETCore.App.Ref" -Directory | Sort-Object Name -Descending | Select-Object -First 1
$build=Join-Path $repo 'tmp/plan11-10-measurements'
New-Item -ItemType Directory -Force $build | Out-Null
$assembly=Join-Path $build ('Balance'+$Final+'.dll')
$lines=@('-nologo','-nostdlib+','-target:exe','-langversion:9',('-out:"'+$assembly+'"'))
$lines+=Get-ChildItem "$($pack.FullName)/ref/net8.0" -Filter '*.dll' | ForEach-Object {'-r:"'+$_.FullName+'"'}
$lines+=Get-ChildItem "$repo/game/unity/Assets/Game/Scripts/Core","$repo/game/unity/Assets/Game/Scripts/Data" -Filter '*.cs' | ForEach-Object {'"'+$_.FullName+'"'}
$lines+='"'+(Join-Path $PSScriptRoot 'Plan1110BalanceMeasurements.cs')+'"'
if($Final){$lines+='-main:Plan1110FinalBalanceMeasurements';$lines+='"'+(Join-Path $PSScriptRoot 'Plan1110FinalBalanceMeasurements.cs')+'"'}
$response=Join-Path $build 'compile.rsp'
$lines | Set-Content $response -Encoding UTF8
& $dotnet "$($sdk.FullName)/Roslyn/bincore/csc.dll" "@$response"
if($LASTEXITCODE -ne 0){throw 'Balance harness compilation failed'}
'{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}' | Set-Content ([IO.Path]::ChangeExtension($assembly,'runtimeconfig.json'))
Push-Location $repo
try { & $dotnet $assembly 'game/unity/Assets/Game/Resources/Combat/battle-plan11-7.json' $Output $Equipment.IsPresent $Arsenal.IsPresent $Progression.IsPresent $Final; if($LASTEXITCODE -ne 0){throw 'Balance measurement failed'} } finally {Pop-Location}

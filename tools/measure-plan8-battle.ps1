param([ValidateSet('pilot','full')][string]$Phase='pilot',[int]$DamagePerLevel=-1,[int]$HpPerLevel=-1,[switch]$Plan8Profile,[switch]$CollectionStudy,[string]$Output)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$dotnet='C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Data\DotNetSdk\dotnet.exe'
$runtime=Split-Path $dotnet -Parent
$sdk=Get-ChildItem (Join-Path $runtime 'sdk') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$pack=Get-ChildItem (Join-Path $runtime 'packs/Microsoft.NETCore.App.Ref') -Directory | Sort-Object Name -Descending | Select-Object -First 1
$work=Join-Path $repo 'tmp/plan8-battle-measurement'
New-Item -ItemType Directory -Force -Path $work | Out-Null
if(-not $Output){$Output=Join-Path $work ($Phase+'-damage-'+$DamagePerLevel+'-hp-'+$HpPerLevel+'.json')}
if($CollectionStudy -and -not $PSBoundParameters.ContainsKey('Output')){$Output=Join-Path $work 'collection.json'}
$assembly=Join-Path $work 'Plan8BattleMeasurements.dll'
$lines=@('-nologo','-nostdlib+','-target:exe','-langversion:9',('-out:"'+$assembly+'"'))
$lines+=Get-ChildItem (Join-Path $pack.FullName 'ref/net8.0') -Filter '*.dll' | ForEach-Object {'-r:"'+$_.FullName+'"'}
$lines+=Get-ChildItem (Join-Path $repo 'game/unity/Assets/Game/Scripts/Core'),(Join-Path $repo 'game/unity/Assets/Game/Scripts/Data') -Filter '*.cs' | ForEach-Object {'"'+$_.FullName+'"'}
$source=if($CollectionStudy){'Plan8CollectionMeasurements.cs'}else{'Plan8BattleMeasurements.cs'}
$lines+='"'+(Join-Path $PSScriptRoot $source)+'"'
$response=Join-Path $work 'compile.rsp'
[IO.File]::WriteAllLines($response,$lines,[Text.UTF8Encoding]::new($false))
& $dotnet (Join-Path $sdk.FullName 'Roslyn/bincore/csc.dll') ('@'+$response)
if($LASTEXITCODE -ne 0){throw 'Plan8 measurement compile failed'}
[IO.File]::WriteAllText((Join-Path $work 'Plan8BattleMeasurements.runtimeconfig.json'),'{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}')
$seeds=if($Phase -eq 'pilot'){2}else{20}
if($CollectionStudy){& $dotnet $assembly (Join-Path $repo 'game/unity/Assets/Game/Resources/Combat/battle-formal.json') (Join-Path $repo 'game/unity/Assets/Game/Resources/Trial/plan8-story-content.json') $Output}
else{& $dotnet $assembly (Join-Path $repo 'game/unity/Assets/Game/Resources/Combat/battle-formal.json') $Output $seeds $DamagePerLevel $HpPerLevel $Plan8Profile.IsPresent}
if($LASTEXITCODE -ne 0){throw 'Plan8 measurement failed'}

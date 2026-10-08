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
$lines+='"'+(Join-Path $PSScriptRoot 'FormalProgressionTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'FormalKinderTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'FormalCampaignTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'FormalRecoveryTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'FormalEngagementTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'CollectionContractTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'BattleEndTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan5WorldRelicTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan9ColossusTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan9ColossusEndTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan9StoryTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan9BalanceTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan5TrialMeasurements.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan6HomeTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan6BookTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan6ExperienceTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan8BaselineTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan8TelemetryTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan8StoryTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan8StoryIntegrationTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'HeroineRosterTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'HeroineSanctuaryTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'BookRedesignTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan9EconomySupplyTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'CombatExpansionTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'BattleV2Tests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan10RTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan10AnnihilatorTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan10ShellTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan10OriflammeTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan10NighthawkTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan10SlayerSwimTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan10ArcaneTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'AspectLayoutTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan10ArcaneAcademyTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan10ShangrilaTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan10FormContinuityTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan11TerraformTests.cs')+'"'
$lines+='"'+(Join-Path $PSScriptRoot 'Plan12GardenLifeTests.cs')+'"'
$response=Join-Path $output 'compile.rsp'
[IO.File]::WriteAllLines($response,$lines,[Text.UTF8Encoding]::new($false))
& $DotNet (Join-Path $sdk.FullName 'Roslyn/bincore/csc.dll') ('@'+$response)
if($LASTEXITCODE -ne 0){throw 'Automatic chain compile failed'}
[IO.File]::WriteAllText((Join-Path $output 'AutomaticChainTests.runtimeconfig.json'),'{"runtimeOptions":{"tfm":"net8.0","framework":{"name":"Microsoft.NETCore.App","version":"8.0.0"}}}')
& $DotNet $assembly (Join-Path $repo 'game/unity/Assets/Game/Resources/Combat/battle-preview.json') (Join-Path $repo 'game/unity/Assets/Game/Resources/Combat/heroine-reference.json')
if($LASTEXITCODE -ne 0){throw 'Automatic chain tests failed'}

param(
    [switch]$SkipBuild,
    [switch]$SkipPlayer,
    [string]$Unity='C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo 'tmp/plan11-9'
New-Item -ItemType Directory -Force -Path $output | Out-Null
& (Join-Path $PSScriptRoot 'validate-plan10-expanded-roster.ps1') | Tee-Object -FilePath (Join-Path $output 'core-streaming-final.log')
& python (Join-Path $PSScriptRoot 'audit-plan11-9-audio.py')
if($LASTEXITCODE -ne 0){throw 'Plan11-9 audio waveform audit failed'}
if(-not $SkipBuild){
    $buildLog=Join-Path $output 'unity-streaming-final-build.log'
    $args=@('-batchmode','-nographics','-quit','-projectPath',(Join-Path $repo 'game/unity'),'-executeMethod','Plan119LoadFixtureBuild.BuildAndPlayer','-buildOutput','../Builds/plan11-9/newASTER.exe','-logFile',$buildLog)
    $build=Start-Process -FilePath $Unity -ArgumentList $args -WindowStyle Hidden -PassThru
    while(-not $build.WaitForExit(1000)){}
    if($build.ExitCode -ne 0 -or (Get-Content $buildLog -Raw) -notmatch 'PLAN15_16_BUILD_PASS'){throw "Plan11-9 build failed: $buildLog"}
}
if(-not $SkipPlayer){
    $player='game/Builds/plan11-9/newASTER.exe'
    $fixture=Join-Path $output 'fixtures/plan119-images'
    if(-not (Test-Path -LiteralPath $fixture)){throw 'Missing test-only image bundle; run without SkipBuild'}
    $views=@('roster','roster-second','detail','tree-grown','formation-general','affection-cap99','garden-life','daily-dates','save-main','save-restore','save-delete-second','job-fighter','job-berserker','job-defender','job-blaster','job-gunner','job-artist','job-healer','job-panzer','job-alchemist','job-chaser','job-sniper','job-gambler','job-general')
    $uiLog=Join-Path $output 'ui-streaming-final.log'
    & (Join-Path $PSScriptRoot 'validate-plan10-ui-player.ps1') -PlayerPath $player -Width 1920 -Height 1080 -Views $views | Tee-Object -FilePath $uiLog
    & (Join-Path $PSScriptRoot 'validate-plan10-ui-player.ps1') -PlayerPath $player -Width 1280 -Height 720 -Views @('roster-second','tree-grown','formation-general','affection-cap99','garden-life','daily-dates','save-main','job-gambler','job-fighter') | Tee-Object -FilePath $uiLog -Append
    & (Join-Path $PSScriptRoot 'validate-plan11-9-player-quality.ps1') -PlayerPath $player
    & python (Join-Path $PSScriptRoot 'record-plan11-9-quality.py')
    if($LASTEXITCODE -ne 0){throw 'Plan11-9 evidence recording failed'}

}
Write-Output "PLAN11_9_VALIDATION_PASS output=$output"

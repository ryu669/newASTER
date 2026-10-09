param(
    [switch]$SkipBuild,
    [switch]$SkipPlayer,
    [string]$Unity='C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe'
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo 'tmp/plan11-9'
New-Item -ItemType Directory -Force -Path $output | Out-Null
& (Join-Path $PSScriptRoot 'validate-plan10-expanded-roster.ps1')
if(-not $SkipBuild){
    $buildLog=Join-Path $output 'unity-build-final.log'
    $args=@('-batchmode','-nographics','-quit','-projectPath',(Join-Path $repo 'game/unity'),'-executeMethod','Plan119BuildValidation.Build','-buildOutput','../Builds/plan11-9/newASTER.exe','-logFile',$buildLog)
    $build=Start-Process -FilePath $Unity -ArgumentList $args -WindowStyle Hidden -PassThru
    while(-not $build.WaitForExit(1000)){}
    if($build.ExitCode -ne 0 -or (Get-Content $buildLog -Raw) -notmatch 'PLAN15_16_BUILD_PASS'){throw "Plan11-9 build failed: $buildLog"}
}
if(-not $SkipPlayer){
    $player='game/Builds/plan11-9/newASTER.exe'
    $views=@('roster','roster-second','detail','tree-grown','formation-general','affection-cap99','garden-life','daily-dates','save-main','save-restore','save-delete-second','job-fighter','job-berserker','job-defender','job-blaster','job-gunner','job-artist','job-healer','job-panzer','job-alchemist','job-chaser','job-sniper','job-gambler','job-general')
    & (Join-Path $PSScriptRoot 'validate-plan10-ui-player.ps1') -PlayerPath $player -Width 1920 -Height 1080 -Views $views
    & (Join-Path $PSScriptRoot 'validate-plan10-ui-player.ps1') -PlayerPath $player -Width 1280 -Height 720 -Views @('roster-second','tree-grown','formation-general','affection-cap99','garden-life','daily-dates','save-main','job-gambler','job-fighter')
    $log=Join-Path $output 'player-quality.log';$png=Join-Path $output 'player-quality.png'
    $args=@('-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',$log,'-presentationCapture',$png,'-capturePlan10Ui','-uiView','garden-life','-plan15Manual','-validatePlan119')
    $process=Start-Process -FilePath (Join-Path $repo $player) -ArgumentList $args -WindowStyle Normal -PassThru
    if(-not $process.WaitForExit(60000)){Stop-Process -Id $process.Id;throw 'Plan11-9 Player measurement timeout'}
    if($process.ExitCode -ne 0 -or (Get-Content $log -Raw) -notmatch 'PLAN11_9_PLAYER_QUALITY_PASS'){throw "Plan11-9 Player measurement failed: $log"}
}
Write-Output "PLAN11_9_VALIDATION_PASS output=$output"

param([string]$Player,[string[]]$Cases=@('closed','expanded','actions','targets','status','timeline','broken','paused','playing','healing'),[int[]]$Heights=@(720,1080),[switch]$SixParts)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Player){$Player=Join-Path $repo 'game/Builds/playable/newASTER.exe'}
$output=Join-Path $repo ('tmp/plan7-battle-menu-'+[DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))
New-Item -ItemType Directory -Path $output | Out-Null
foreach($case in $Cases){foreach($height in $Heights){
    if($case -notin @('closed','expanded','actions','targets','status','timeline','broken','paused','playing','healing')){throw "Unknown battle menu case: $case"}
    if($height -notin @(720,1080)){throw 'Only 720p and 1080p supported'}
    $name=$case+'-'+$height;$png=Join-Path $output ($name+'.png');$log=Join-Path $output ($name+'.log')
    $flags=@('-screen-fullscreen','0','-screen-width',"$([int]($height*16/9))",'-screen-height',"$height",'-presentationCapture',('"'+$png+'"'),'-captureBattleMenu',$case,'-validatePlan7Assets','-logFile',('"'+$log+'"'))
    if($SixParts){$flags+='-captureSixParts'}
    $process=Start-Process -FilePath $Player -ArgumentList $flags -WindowStyle Normal -PassThru
    if(-not $process.WaitForExit(60000)){$process.Kill();throw "Battle menu capture timed out: $name"}
    $text=Get-Content -LiteralPath $log -Raw
    if($process.ExitCode -ne 0 -or $text -notmatch ('BATTLE_MENU_CAPTURE_PASS '+[regex]::Escape($case)+' / isolated / art-first') -or $text -notmatch 'PLAN7_SAMPLE_ASSETS_PASS' -or $text -notmatch 'PLAN7_BUNDLED_FONT_PASS' -or @([regex]::Matches($text,'PLAN7_AUDIO_WAVEFORM_PASS')).Count -ne 6 -or $text -match '(Exception:|PLAN7_ASSET_MISSING|ILLUSTRATION_MANIFEST_WARNING)' -or -not(Test-Path -LiteralPath $png)){throw "Battle menu capture failed: $log"}
    Write-Output "PLAN7_BATTLE_MENU_PLAYER_PASS $name sixParts=$([bool]$SixParts)"
}}
Write-Output "PLAN7_BATTLE_MENU_PLAYER_OUTPUT $output"

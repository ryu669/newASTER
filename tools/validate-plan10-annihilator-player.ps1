param([string[]]$Forms=@('normal','holy'),[string[]]$Views=@('recruitment','roster','detail','weapon','formation','battle','garden','chapter','event0','event1','event2','event3','event4','journey'),[int]$Width=1280,[int]$Height=720)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo "tmp/plan10-annihilator/player-$Width"
New-Item -ItemType Directory -Force $output | Out-Null
foreach($form in $Forms){foreach($view in $Views){
    $base=Join-Path $output "$form-$view"
    $proc=Start-Process -FilePath (Join-Path $repo 'game/Builds/plan10-annihilator/newASTER.exe') -ArgumentList @('-screen-width',"$Width",'-screen-height',"$Height",'-screen-fullscreen','0','-logFile',"$base.log",'-presentationCapture',"$base.png",'-captureAnnihilator','-anniView',$view,'-anniForm',$form) -WindowStyle Normal -PassThru
    if(-not $proc.WaitForExit(45000)){Stop-Process -Id $proc.Id;throw "Player timeout $form $view"}
    $log=Get-Content "$base.log" -Raw
    if($log -notmatch "PLAN10_ANNIHILATOR_PLAYER_PASS form=$form view=$view " -or $log -match '(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)' -or -not (Test-Path "$base.png")){Get-Content "$base.log" -Tail 65;throw "Player failed $form $view"}
    Write-Output "PLAYER_PASS $form $view ${Width}x${Height}"
}}

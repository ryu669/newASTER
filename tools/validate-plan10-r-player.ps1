param([string[]]$Views=@('recruitment','detail','weapon','formation','battle','song','buff','garden','event0','event1','event2','event3','event4','chapter','journey'),[int]$Width=1280,[int]$Height=720)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo "tmp/plan10-r/player-$Width"
New-Item -ItemType Directory -Force $output | Out-Null
foreach($view in $Views){
    $base=Join-Path $output $view
    $proc=Start-Process -FilePath (Join-Path $repo 'game/Builds/plan10-r/newASTER.exe') -ArgumentList @('-screen-width',"$Width",'-screen-height',"$Height",'-screen-fullscreen','0','-logFile',"$base.log",'-presentationCapture',"$base.png",'-capturePlan10','-plan10View',$view) -WindowStyle Normal -PassThru
    if(-not $proc.WaitForExit(45000)){Stop-Process -Id $proc.Id;throw "Player timeout: $view"}
    $log=Get-Content "$base.log" -Raw
    if($log -notmatch "PLAN10_R_PLAYER_PASS view=$view " -or $log -match '(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)' -or -not (Test-Path "$base.png")){
        Get-Content "$base.log" -Tail 60;throw "Player failed: $view"
    }
    Write-Output "PLAYER_PASS $view ${Width}x${Height}"
}

param([string[]]$Views=@('recruitment','recruit','roster','detail','weapon','formation','battle','status','formation-member','formation-roster','command','buff','ultimate','attack','garden','chapter','event0','event1','event2','event3','event4','journey'),[int]$Width=1280,[int]$Height=720)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo "tmp/plan10-slayer-swim/player-${Width}x${Height}"
New-Item -ItemType Directory -Force $output | Out-Null
foreach($view in $Views){
    $base=Join-Path $output "slayer-swim-$view"
    $proc=Start-Process -FilePath (Join-Path $repo 'game/Builds/plan10-slayer-swim/newASTER.exe') -ArgumentList @('-screen-width',"$Width",'-screen-height',"$Height",'-screen-fullscreen','0','-logFile',"$base.log",'-presentationCapture',"$base.png",'-captureSlayerSwim','-slayer-swimView',$view) -WindowStyle Normal -PassThru
    if(-not $proc.WaitForExit(45000)){Stop-Process -Id $proc.Id;throw "Player timeout $view"}
    $log=Get-Content "$base.log" -Raw
    if($log -notmatch "PLAN10_SLAYER_SWIM_PLAYER_PASS view=$view " -or $log -match '(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)' -or -not (Test-Path "$base.png")){Get-Content "$base.log" -Tail 65;throw "Player failed $view"}
    python -c "from PIL import Image,ImageStat; import sys; s=ImageStat.Stat(Image.open(sys.argv[1]).convert('RGB')); assert max(s.mean)>20 and max(s.stddev)>20, 'Black capture'" "$base.png"
    if($LASTEXITCODE -ne 0){throw "Black capture $view"}
    Write-Output "PLAYER_PASS slayer-swim $view ${Width}x${Height}"
}

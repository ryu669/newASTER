param([string[]]$Views=@('domains','warning','blocked','extremes','deep','phenomena','name','journey'),[int]$Width=1600,[int]$Height=900,[switch]$Visible)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo "tmp/plan11/player-${Width}x${Height}"
New-Item -ItemType Directory -Force -Path $output | Out-Null
$windowStyle=if($Visible){"Normal"}else{"Hidden"}
foreach($view in $Views){
    $base=Join-Path $output "terraform-$view"
    $process=Start-Process -FilePath (Join-Path $repo 'game/Builds/plan11-1/newASTER.exe') -ArgumentList @('-screen-width',"$Width",'-screen-height',"$Height",'-screen-fullscreen','0','-logFile',"$base.log",'-presentationCapture',"$base.png",'-capturePlan11','-terraformView',$view) -WindowStyle $windowStyle -PassThru
    if(-not $process.WaitForExit(45000)){Stop-Process -Id $process.Id;throw "Plan11 player timeout: $view"}
    $log=Get-Content "$base.log" -Raw
    if($log -notmatch "PLAN11_PLAYER_PASS view=$view " -or $log -match '(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)' -or -not (Test-Path "$base.png")){Get-Content "$base.log" -Tail 65;throw "Plan11 player failed: $view"}
    if($Visible){
    & 'C:\Program Files\Python310\python.exe' -c "from PIL import Image,ImageStat; import sys; im=Image.open(sys.argv[1]); stat=ImageStat.Stat(im.convert('RGB')); assert im.size==(int(sys.argv[2]),int(sys.argv[3])), im.size; assert max(stat.mean)>20 and max(stat.stddev)>20, 'Black capture'" "$base.png" $Width $Height
    if($LASTEXITCODE -ne 0){throw "Invalid Plan11 framebuffer: $view"}
    Write-Output "PLAN11_FRAMEBUFFER_PASS $view ${Width}x${Height}"
    }else{Write-Output "PLAN11_LOGIC_PLAYER_PASS $view"}
}

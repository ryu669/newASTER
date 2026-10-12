param([string]$Hero='iron-maiden',[int]$TimeoutSeconds=150,[string]$PlayerPath='game/Builds/layered-tree-preview/newASTER.exe')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$player=Join-Path $repo $PlayerPath
$output=Join-Path $repo $(if($Hero -eq 'iron-maiden'){'tmp/plan12-batch/layered-tree-player'}else{"tmp/plan12-batch/layered-tree-player/$Hero"})
New-Item -ItemType Directory -Force $output | Out-Null
foreach($stage in @(0,2,4)){
 $base=Join-Path $output "stage-$stage"
 $proc=Start-Process -FilePath $player -ArgumentList @('-logFile',"$base.log",'-presentationCapture',"$base.png",'-captureFirstBatch','-batchHero',$Hero,'-batchView','weapon','-treeStage',"$stage") -WindowStyle Normal -PassThru
 if(-not $proc.WaitForExit($TimeoutSeconds*1000)){Stop-Process -Id $proc.Id;throw "Tree capture timed out at $stage"}
 $log=Get-Content "$base.log" -Raw
 if($log -notmatch "LAYERED_TREE_SAVE_PASS stage=$stage nonRoot=$($stage*3)" -or $log -match '(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException)'){throw "Tree capture failed at $stage; see $base.log"}
 & 'C:/Program Files/Python310/python.exe' -X utf8 -c 'from PIL import Image,ImageStat;import sys;im=Image.open(sys.argv[1]);assert im.size==(1920,1080);assert max(ImageStat.Stat(im.convert("RGB")).stddev)>20' "$base.png"
 if($LASTEXITCODE -ne 0){throw 'Wrong default capture resolution'}
 Write-Output "LAYERED_TREE_PLAYER_PASS stage=$stage saved-and-reloaded default=1920x1080"
}

$hashes=@(0,2,4 | ForEach-Object {(Get-FileHash (Join-Path $output "stage-$_.png") -Algorithm SHA256).Hash})
if(@($hashes | Select-Object -Unique).Count -ne 3){throw 'Unlock stages did not change the rendered screen'}
Write-Output 'LAYERED_TREE_DISTINCT_FRAMES_PASS root / partial / full'

param([string[]]$Heroes=@('aeon','iron-maiden','avalanche','anathema','shiva'),[int]$TimeoutSeconds=150)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$player=Join-Path $repo 'game/Builds/plan12-first-batch/newASTER.exe'
foreach($hero in $Heroes){
 $out=Join-Path $repo "tmp/plan12-batch/battle-art/$hero"
 New-Item -ItemType Directory -Force $out | Out-Null
 foreach($pose in @('attack','hit','cutin')){
  $base=Join-Path $out $pose
  if(Test-Path "$base.complete"){Write-Output "BATTLE_ART_CHECKPOINT $hero $pose";continue}
  $flags=if($hero -eq 'aeon'){@('-captureAeon','-aeonView','battle')}else{@('-captureFirstBatch','-batchHero',$hero,'-batchView','battle')}
  $proc=Start-Process $player -ArgumentList (@('-logFile',"$base.log",'-presentationCapture',"$base.png",'-inspectPlan7Battle',$pose,'-inspectPlan9Hero',"heroine.$hero")+$flags) -WindowStyle Normal -PassThru
  $timer=[Diagnostics.Stopwatch]::StartNew();$peak=0L
  while(-not $proc.WaitForExit(500)){
   $proc.Refresh();$peak=[Math]::Max($peak,$proc.PeakWorkingSet64)
   if($timer.Elapsed.TotalSeconds -gt $TimeoutSeconds){Stop-Process -Id $proc.Id;throw "Battle art timeout $hero $pose"}
  }
  $log=Get-Content "$base.log" -Raw
  if($log -notmatch 'PLAYER_PASS' -or $log -match '(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)'){throw "Battle art failed $hero $pose"}
  & 'C:/Program Files/Python310/python.exe' -X utf8 -c 'from PIL import Image,ImageStat;import sys;im=Image.open(sys.argv[1]);assert im.size==(1920,1080);assert max(ImageStat.Stat(im.convert("RGB")).stddev)>20' "$base.png"
  if($LASTEXITCODE -ne 0){throw 'Invalid framebuffer'}
  @{hero=$hero;pose=$pose;seconds=$timer.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak;resolution='1920x1080'} | ConvertTo-Json | Set-Content "$base.metrics.json"
  Set-Content "$base.complete" 'capture and binding passed; separate visual review required'
  Write-Output "BATTLE_ART_PLAYER_PASS $hero $pose default=1920x1080"
 }
}

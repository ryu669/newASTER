param([string[]]$Cases=@('art','gallery','recruitment','recruit','trait','skill','formation','command','attack0','attack1','attack2','garden','journey','tree0','tree2','tree4','battle-attack','battle-hit','battle-cutin'),[switch]$Force)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$player=Join-Path $repo 'game/Builds/plan12-viper/newASTER.exe'
$output=Join-Path $repo 'tmp/plan12-viper/player'
New-Item -ItemType Directory -Force $output | Out-Null
foreach($case in $Cases){
 $caseOutput=if($case -eq 'gallery'){Join-Path $output 'gallery-frames'}else{$output}
 New-Item -ItemType Directory -Force $caseOutput | Out-Null
 $base=Join-Path $caseOutput $case
 if((Test-Path "$base.complete") -and -not $Force){Write-Output "INVISIBLE_PLAYER_CHECKPOINT $case";continue}
 $flags=@('-plan12ViperPreview')
 if($case -in @('art','gallery')){
  $flags+=@('-capturePlan10Ui','-uiView','detail','-heroineId','heroine.viper','-plan12ArtSweep','-plan12ArtHero','viper')
  if($case -eq 'gallery'){$flags+='-plan12CgGallerySweep'}
  $marker='PLAN12_ART_SWEEP_PASS captures='+$(if($case -eq 'art'){'20'}else{'5'})+' '
 }else{
  $flags+=@('-captureFirstBatch','-batchHero','viper','-batchView',$(if($case.StartsWith('tree')){'weapon'}elseif($case.StartsWith('battle-')){'battle'}else{$case}))
  if($case.StartsWith('tree')){$stage=$case.Substring(4);$flags+=@('-treeStage',$stage);$marker="LAYERED_TREE_SAVE_PASS stage=$stage"}
  elseif($case.StartsWith('battle-')){$flags+=@('-inspectPlan7Battle',$case.Substring(7),'-inspectPlan9Hero','heroine.viper');$marker='PLAN12_FIRST_BATCH_PLAYER_PASS'}
  else{$marker="PLAN12_FIRST_BATCH_PLAYER_PASS hero=heroine.viper view=$case "}
 }
 $proc=Start-Process -FilePath $player -ArgumentList (@('-logFile',"$base.log",'-presentationCapture',"$base.png")+$flags) -WindowStyle Normal -PassThru
 $timer=[Diagnostics.Stopwatch]::StartNew();$peak=0L
 while(-not $proc.WaitForExit(500)){$proc.Refresh();$peak=[Math]::Max($peak,$proc.PeakWorkingSet64);if($timer.Elapsed.TotalSeconds -gt 150){Stop-Process -Id $proc.Id;throw "Viper timeout $case"}}
 $log=Get-Content "$base.log" -Raw
 if($log -notmatch $marker -or $log -match '(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)'){throw "Viper Player failed $case; see $base.log"}
 if($case -eq 'command' -and $log -notmatch 'JOB_PANEL_OPERATIONS_PASS'){throw 'Gunner independent commands were not tested'}
 if($case -notin @('art','gallery')){
  & 'C:/Program Files/Python310/python.exe' -X utf8 -c 'from PIL import Image,ImageStat;import sys;im=Image.open(sys.argv[1]);assert im.size==(1920,1080);assert max(ImageStat.Stat(im.convert("RGB")).stddev)>20' "$base.png"
  if($LASTEXITCODE -ne 0){throw "Wrong framebuffer $case"}
 }
 @{seconds=$timer.Elapsed.TotalSeconds;peakWorkingSetBytes=$peak;resolution='default 1920x1080';includesCaptureDelay=$true} | ConvertTo-Json | Set-Content "$base.metrics.json"
 Set-Content "$base.complete" 'automatic behavior passed; visual review is separate'
 Write-Output "VIPER_PLAYER_PASS $case"
}

param([string[]]$Heroes=@('iron-maiden','avalanche','anathema','shiva'),[string[]]$Views=@('recruitment','recruit','trait','skill','formation','command','attack0','attack1','attack2','garden','journey'),[switch]$SkipArt,[switch]$Force,[int]$TimeoutSeconds=150,[string]$PlayerPath='game/Builds/plan12-first-batch/newASTER.exe')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$player=Join-Path $repo $PlayerPath
foreach($hero in $Heroes){
  $output=Join-Path $repo "tmp/plan12-batch/player-default/$hero"
  New-Item -ItemType Directory -Force $output | Out-Null
  function RunCase($name,$flags,$marker){
    $base=Join-Path $output $name
    if((Test-Path "$base.complete") -and -not $Force){Write-Output "FIRST_BATCH_CHECKPOINT $hero $name";return}
    $proc=Start-Process -FilePath $player -ArgumentList (@('-logFile',"$base.log",'-presentationCapture',"$base.png")+$flags) -WindowStyle Normal -PassThru
    if(-not $proc.WaitForExit($TimeoutSeconds*1000)){Stop-Process -Id $proc.Id;throw "Player timeout $hero $name"}
    $log=Get-Content "$base.log" -Raw
    if($log -notmatch $marker -or $log -match '(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)'){Get-Content "$base.log" -Tail 30;throw "Player failed $hero $name"}
    if($name -eq 'command' -and $log -notmatch 'JOB_PANEL_OPERATIONS_PASS'){throw "Job operations did not execute: $hero"}
    & 'C:/Program Files/Python310/python.exe' -X utf8 -c "from pathlib import Path; from PIL import Image,ImageStat; import sys; root=Path(sys.argv[1]); name=sys.argv[2]; paths=list(root.glob(sys.argv[3]+'-*.png')) if name in ('art','gallery') else [root/(name+'.png')]; assert paths; [(lambda im: ((im.size==(1920,1080) and max(ImageStat.Stat(im.convert('RGB')).stddev)>20) or (_ for _ in ()).throw(AssertionError('Invalid framebuffer'))))(Image.open(p)) for p in paths]" $output $name $hero
    if($LASTEXITCODE -ne 0){throw "Invalid framebuffer $hero $name"}
    Set-Content "$base.complete" 'passed at default 1920x1080; no resolution overrides'
    Write-Output "FIRST_BATCH_PLAYER_PASS $hero $name"
  }
  if(-not $SkipArt){
    RunCase 'art' @('-plan12FirstBatchPreview','-capturePlan10Ui','-uiView','detail','-heroineId',"heroine.$hero",'-plan12ArtSweep','-plan12ArtHero',$hero) 'PLAN12_ART_SWEEP_PASS captures=20 '
    RunCase 'gallery' @('-plan12FirstBatchPreview','-capturePlan10Ui','-uiView','detail','-heroineId',"heroine.$hero",'-plan12ArtSweep','-plan12CgGallerySweep','-plan12ArtHero',$hero) 'PLAN12_ART_SWEEP_PASS captures=5 '
  }
  foreach($view in $Views){RunCase $view @('-captureFirstBatch','-batchHero',$hero,'-batchView',$view) "PLAN12_FIRST_BATCH_PLAYER_PASS hero=heroine.$hero view=$view "}
}

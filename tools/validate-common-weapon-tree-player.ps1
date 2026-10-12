param([string[]]$Heroes=@(),[int[]]$Stages=@(0,2,4),[switch]$Force)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo 'tmp/plan12-common-tree/player'
New-Item -ItemType Directory -Force $output | Out-Null
if(!$Heroes){
 $player=Join-Path $repo 'game/Builds/plan12-weapon-master/newASTER.exe'
 $base=Join-Path $output 'sweep'
 $proc=Start-Process $player -ArgumentList @('-plan12WeaponMasterPreview','-logFile',"$base.log",'-presentationCapture',"$base.png",'-capturePlan10Ui','-uiView','detail','-heroineId','heroine.weapon-master','-plan12ArtSweep','-commonTreeSweep') -WindowStyle Normal -PassThru
 $timer=[Diagnostics.Stopwatch]::StartNew()
 while(!$proc.WaitForExit(500)){if($timer.Elapsed.TotalSeconds -gt 300){Stop-Process -Id $proc.Id;throw 'Common tree sweep timeout'}}
 $log=Get-Content "$base.log" -Raw
 if($log -notmatch 'PLAN12_ART_SWEEP_PASS captures=72 ' -or $log -match '(?m)^(\w*Exception:|ILLUSTRATION_MANIFEST_WARNING)'){throw 'Common tree sweep failed'}
 Write-Output 'COMMON_TREE_PLAYER_PASS forms=24 stages=3 captures=72'
 return
}
if(!$Heroes){$catalog=Get-Content (Join-Path $repo 'game/unity/Assets/Game/Resources/UI/heroine-weapon-trees.json') -Raw | ConvertFrom-Json;$Heroes=@($catalog.entries.heroineId | ForEach-Object {$_.Substring(8)})}
foreach($hero in $Heroes){foreach($stage in $Stages){
 $base=Join-Path $output "$hero-$stage"
 if((Test-Path "$base.complete") -and !$Force){continue}
 $player=Join-Path $repo 'game/Builds/plan12-weapon-master/newASTER.exe'
 $proc=Start-Process $player -ArgumentList @('-plan12WeaponMasterPreview','-logFile',"$base.log",'-presentationCapture',"$base.png",'-captureFirstBatch','-batchHero',$hero,'-batchView','weapon','-treeStage',$stage) -WindowStyle Normal -PassThru
 $timer=[Diagnostics.Stopwatch]::StartNew()
 while(!$proc.WaitForExit(500)){if($timer.Elapsed.TotalSeconds -gt 150){Stop-Process -Id $proc.Id;throw "Common tree timeout $hero/$stage"}}
 $log=Get-Content "$base.log" -Raw
 if($log -notmatch "LAYERED_TREE_SAVE_PASS stage=$stage" -or $log -match '(?m)^(\w*Exception:|ILLUSTRATION_MANIFEST_WARNING)'){throw "Common tree failed $hero/$stage"}
 & 'C:/Program Files/Python310/python.exe' -X utf8 (Join-Path $PSScriptRoot 'check-player-frame.py') "$base.png"
 if($LASTEXITCODE -ne 0){throw 'Unexpected framebuffer'}
 Set-Content "$base.complete" 'save/reload passed; manual tree review separate'
 Write-Output "COMMON_TREE_PLAYER_PASS $hero/$stage"
}}

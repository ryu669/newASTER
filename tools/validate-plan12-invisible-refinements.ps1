$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$out=Join-Path $root 'tmp/plan12-invisible/refined'
$exe=Join-Path $root 'game/Builds/plan12-invisible/newASTER.exe'
New-Item -ItemType Directory -Force $out | Out-Null
foreach($case in @('art','trait','skill')){
 $flags=@('-plan12InvisiblePreview')
 if($case -eq 'art'){$flags+=@('-capturePlan10Ui','-uiView','detail','-heroineId','heroine.invisible','-plan12ArtSweep','-plan12ArtHero','invisible','-plan12ArtViews','detail,expression.normal,expression.joy,expression.puzzled,expression.determined,sd.work,sd.furniture');$marker='PLAN12_ART_SWEEP_PASS captures=7 '}
 else{$flags+=@('-captureFirstBatch','-batchHero','invisible','-batchView',$case);$marker="PLAN12_FIRST_BATCH_PLAYER_PASS hero=heroine.invisible view=$case "}
 $base=Join-Path $out $case
 $p=Start-Process $exe -ArgumentList (@('-logFile',"$base.log",'-presentationCapture',"$base.png")+$flags) -WindowStyle Normal -PassThru
 if(-not $p.WaitForExit(150000)){Stop-Process -Id $p.Id;throw "Refinement capture timed out: $case"}
 $log=Get-Content "$base.log" -Raw
 if($log -notmatch $marker -or $log -match '(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)'){throw "Refinement capture failed: $case"}
 Write-Output "INVISIBLE_REFINEMENT_PASS $case"
}

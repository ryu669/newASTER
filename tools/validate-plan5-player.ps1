param([string]$Player)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Player){$Player=Join-Path $repo 'game/Builds/playable/newASTER.exe'}
if(-not (Test-Path -LiteralPath $Player)){throw 'Build the playable Windows player first.'}
$output=Join-Path $repo ('tmp/plan5-player-acceptance-'+[DateTime]::UtcNow.ToString('yyyyMMddHHmmss')+'-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
function Run-PlayerCase([string]$name,[int]$width,[int]$height,[string[]]$flags,[string]$marker,[string]$captureName=$name){
    $png=Join-Path $output ($captureName+'.png');$log=Join-Path $output ($name+'.log')
    $playerArgs=@('-screen-fullscreen','0','-screen-width',"$width",'-screen-height',"$height",'-presentationCapture',('"'+$png+'"'),'-logFile',('"'+$log+'"'))+$flags
    $process=Start-Process -FilePath $Player -ArgumentList $playerArgs -WindowStyle Normal -PassThru
    if(-not $process.WaitForExit(60000)){ $process.Kill();throw "Player case timed out: $name" }
    $text=Get-Content -LiteralPath $log -Raw
    if($process.ExitCode -ne 0 -or $text -notmatch [regex]::Escape($marker) -or $text -match '(?m)^\w*Exception:'){throw "Player case failed: $name; inspect $log"}
    if(-not (Test-Path -LiteralPath $png)){throw "Screenshot missing: $name"}
    Write-Output "PLAN5_PLAYER_CASE_PASS $name"
}
Run-PlayerCase 'legacy' 1280 720 @('-capturePlan5Acceptance') 'PLAN5_PLAYER_ACCEPTANCE_PASS'
Copy-Item -LiteralPath (Join-Path $output 'legacy.png') -Destination (Join-Path $output 'legacy-confirm.png')
Run-PlayerCase 'legacy-resume' 1280 720 @('-capturePlan5Acceptance','-plan5Resume') 'PLAN5_PROCESS_RESTART_PASS' 'legacy'
Run-PlayerCase 'new' 1920 1080 @('-capturePlan5Acceptance','-plan5NewSave') 'PLAN5_PLAYER_ACCEPTANCE_PASS'
Copy-Item -LiteralPath (Join-Path $output 'new.png') -Destination (Join-Path $output 'new-confirm.png')
Run-PlayerCase 'new-resume' 1920 1080 @('-capturePlan5Acceptance','-plan5Resume') 'PLAN5_PROCESS_RESTART_PASS' 'new'
Run-PlayerCase 'many' 1280 720 @('-captureCollection','-collectionMany','-collectionInventory') 'PLAN5_MANY_RELICS_PASS'
Run-PlayerCase 'world' 1920 1080 @('-captureCollection','-collectionWorld','-collectionRetry') 'COLLECTION_END_NAVIGATION_PASS'
Write-Output "PLAN5_PLAYER_SUITE_PASS 6 cases; screenshots require visual inspection: $output"

param([string]$Player)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Player){$Player=Join-Path $repo 'game/Builds/playable/newASTER.exe'}
$output=Join-Path $repo ('tmp/plan7-standing-'+[DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))
New-Item -ItemType Directory -Path $output | Out-Null
foreach($resolution in @(@(1280,720),@(1920,1080))){
    $png=Join-Path $output ('standing-'+$resolution[1]+'.png')
    $log=Join-Path $output ('standing-'+$resolution[1]+'.log')
    $flags=@('-screen-fullscreen','0','-screen-width',"$($resolution[0])",'-screen-height',"$($resolution[1])",'-presentationCapture',('"'+$png+'"'),'-inspectPlan7Standing','-logFile',('"'+$log+'"'))
    $process=Start-Process -FilePath $Player -ArgumentList $flags -WindowStyle Normal -PassThru
    if(-not $process.WaitForExit(60000)){$process.Kill();throw 'Standing capture timed out'}
    $logText=Get-Content -LiteralPath $log -Raw
    if($process.ExitCode -ne 0 -or $logText -match '(Exception:|ILLUSTRATION_MANIFEST_WARNING)'){throw "Standing capture failed: $log"}
    if(-not(Test-Path -LiteralPath $png)){throw "Capture missing: $png"}
    Write-Output "PLAN7_STANDING_CAPTURE_PASS $($resolution[1])"
}
Write-Output "PLAN7_STANDING_OUTPUT $output"

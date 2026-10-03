param([string]$Player)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Player){$Player=Join-Path $repo 'game/Builds/playable/newASTER.exe'}
$output=Join-Path $repo ('tmp/plan7-expressions-'+[DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))
New-Item -ItemType Directory -Path $output | Out-Null
$save=Join-Path $output 'candidate-trial.json'
foreach($expression in @('normal','joy','puzzled','determined')){
    foreach($resolution in @(@(1280,720),@(1920,1080))){
        $name=$expression+'-'+$resolution[1];$png=Join-Path $output ($name+'.png');$log=Join-Path $output ($name+'.log')
        $flags=@('-screen-fullscreen','0','-screen-width',"$($resolution[0])",'-screen-height',"$($resolution[1])",'-presentationCapture',('"'+$png+'"'),'-capturePlan6Home','-plan6Save',('"'+$save+'"'),'-homeCase','Adv','-inspectPlan7Expression',$expression,'-logFile',('"'+$log+'"'))
        if(Test-Path -LiteralPath $save){$flags+='-plan6Resume'}else{$flags+='-plan6NewSave'}
        $process=Start-Process -FilePath $Player -ArgumentList $flags -WindowStyle Normal -PassThru
        if(-not $process.WaitForExit(60000)){$process.Kill();throw "Expression timeout: $name"}
        $logText=Get-Content -LiteralPath $log -Raw
        if($process.ExitCode -ne 0 -or $logText -notmatch ('PLAN7_EXPRESSION_CAPTURE '+$expression+' asset=') -or $logText -match '(Exception:|ILLUSTRATION_MANIFEST_WARNING)'){throw "Expression failed: $log"}
        if(-not(Test-Path -LiteralPath $png)){throw "Missing image: $png"}
        Write-Output "PLAN7_EXPRESSION_CAPTURE_PASS $name"
    }
}
Write-Output "PLAN7_EXPRESSION_OUTPUT $output"

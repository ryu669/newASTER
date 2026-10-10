param([string]$PlayerPath='game/Builds/plan12-preparation/newASTER.exe',[string]$Label='current',[int]$Runs=3)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo 'tmp/ui-revision/startup'
New-Item -ItemType Directory -Force $output | Out-Null
$rows=@()
for($run=1;$run -le $Runs;$run++){
    $log=Join-Path $output "$Label-$run.log"
    $capture=Join-Path $output "$Label-$run.png"
    $args=@('-screen-width','1920','-screen-height','1080','-screen-fullscreen','0','-logFile',$log,'-presentationCapture',$capture,'-capturePlan9Title')
    $timer=[Diagnostics.Stopwatch]::StartNew()
    $proc=Start-Process -FilePath (Join-Path $repo $PlayerPath) -ArgumentList $args -WindowStyle Hidden -PassThru
    try {
        $ready=$false
        while($timer.Elapsed.TotalSeconds -lt 55 -and !$proc.HasExited){
            if((Test-Path $log) -and (Get-Content -LiteralPath $log -Raw -ErrorAction SilentlyContinue) -match 'FORMAL_CAMPAIGN_READY'){$ready=$true;break}
            Start-Sleep -Milliseconds 50
        }
        if(!$ready){throw "Startup did not become ready: $Label/$run"}
        $rows+=@{run=$run;processToCampaignReadyMs=[Math]::Round($timer.Elapsed.TotalMilliseconds,1);log=$log}
    } finally {if(!$proc.HasExited){Stop-Process -Id $proc.Id; $proc.WaitForExit()}}
}
@{label=$Label;resolution=@(1920,1080);marker='FORMAL_CAMPAIGN_READY';pollMs=50;isolatedDiagnostic=$true;includesOsCacheAndProcessStartup=$true;runs=$rows} | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $output "$Label.json") -Encoding utf8
$rows | Format-Table

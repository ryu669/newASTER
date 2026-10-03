param([string]$Player,[string[]]$Cases=@('break-0','break-1','break-2','break-3','break-4','break-5','break-6','break-7','break-8','break-9','break-10','break-11','break-12','break-13','break-14','break-15','idle','attack','hit','cutin','sit','work','look','cg','settings','enemycutin'),[int[]]$Heights=@(720,1080),[switch]$Measure,[switch]$LargeText,[switch]$Uncapped)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Player){$Player=Join-Path $repo 'game/Builds/playable/newASTER.exe'}
$output=Join-Path $repo ('tmp/plan7-sample-'+[DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))
New-Item -ItemType Directory -Path $output | Out-Null
foreach($case in $Cases){foreach($height in $Heights){
    if($height -ne 720 -and $height -ne 1080){throw 'Only 720p and 1080p supported'}
    $name=$case+'-'+$height;$png=Join-Path $output ($name+'.png');$log=Join-Path $output ($name+'.log')
    if($Measure -or $Uncapped){& (Join-Path $PSScriptRoot 'check-plan7-performance-environment.ps1') -Output (Join-Path $output ($name+'-environment-before.json'))}
    $flags=@('-screen-fullscreen','0','-screen-width',"$([int]($height*16/9))",'-screen-height',"$height",'-presentationCapture',('"'+$png+'"'),'-capturePlan7Sample','-artCase',$case,'-logFile',('"'+$log+'"'))
    if($case -eq 'gameplay'){$flags=$flags | Where-Object {$_ -notin @('-capturePlan7Sample','-artCase','gameplay')};$flags+='-capture2DActor0'}
    if($Measure -or $Uncapped){$flags+='-measurePlan7'}
    if($Uncapped){$flags+='-measurePlan7Uncapped'}
    if($LargeText){$flags+='-inspectLargeText'}
    $watch=[Diagnostics.Stopwatch]::StartNew();$peakWorking=0L;$peakObserved=0L
    $process=Start-Process -FilePath $Player -ArgumentList $flags -WindowStyle Normal -PassThru
    while(-not $process.WaitForExit(250)){
        if($watch.ElapsedMilliseconds -gt 60000){$process.Kill();throw "Sample timed out: $name"}
        try{$process.Refresh();$peakWorking=[Math]::Max($peakWorking,$process.PeakWorkingSet64);$peakObserved=[Math]::Max($peakObserved,$process.WorkingSet64)}catch [InvalidOperationException]{}
    }
    $watch.Stop()
    $text=Get-Content -LiteralPath $log -Raw
    if($process.ExitCode -ne 0 -or $text -notmatch 'PLAN7_SAMPLE_ASSETS_PASS' -or ($case -ne 'gameplay' -and $text -notmatch ('PLAN7_SAMPLE_CAPTURE '+[regex]::Escape($case)+' / read-only')) -or $text -match '(Exception:|PLAN7_ASSET_MISSING|ILLUSTRATION_MANIFEST_WARNING)'){throw "Sample failed: $log"}
    if(-not(Test-Path -LiteralPath $png)){throw "Missing screenshot: $png"}
    $memory=[ordered]@{case=$case;height=$height;peakWorkingBytes=$peakWorking;observedWorkingPeakBytes=$peakObserved;sampleIntervalMs=250;processElapsedMs=$watch.ElapsedMilliseconds;uncapped=[bool]$Uncapped}
    [IO.File]::WriteAllText((Join-Path $output ($name+'-memory.json')),($memory | ConvertTo-Json)+[Environment]::NewLine)
    if($Measure -or $Uncapped){Write-Output "PLAN7_PROCESS_MEMORY case=$name peakWorkingBytes=$peakWorking observedWorkingPeakBytes=$peakObserved"}
    if($Measure -or $Uncapped){if($text -notmatch 'PLAN7_PERFORMANCE'){throw 'Missing performance record'};($text -split "`n") | Where-Object {$_ -match 'PLAN7_PERFORMANCE'} | Write-Output}
    Write-Output "PLAN7_SAMPLE_PLAYER_CASE_PASS $name"
}}
Write-Output "PLAN7_SAMPLE_PLAYER_OUTPUT $output"

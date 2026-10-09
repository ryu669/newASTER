param(
    [string]$PlayerPath='game/Builds/plan11-9/newASTER.exe',
    [string[]]$Checks=@('streaming','cold-book','audio','environment-audio'),
    [int]$Width=1920,
    [int]$Height=1080,
    [string]$MeasurementContext='Recording and other game activity are not controlled or observed'
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo 'tmp/plan11-9'
$player=Join-Path $repo $PlayerPath
$fixture=Join-Path $output 'fixtures/plan119-images'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$measurements=@()
if(Test-Path (Join-Path $output 'process-memory.json')){$measurements=@(Get-Content (Join-Path $output 'process-memory.json') -Raw | ConvertFrom-Json | Where-Object {$_.check -notin $Checks})}
foreach($check in $Checks){
    if($check -notin @('streaming','cold-book','audio','environment-audio')){throw "Unknown quality check: $check"}
    $name=if($check -eq 'streaming'){'player-streaming'}else{$check}
    $log=Join-Path $output "$name.log";$png=Join-Path $output "$name.png"
    # Never accept a previous capture or stop on a stale exception before this Player opens its log.
    foreach($previousOutput in @($log,$png)){if(Test-Path -LiteralPath $previousOutput){Remove-Item -LiteralPath $previousOutput}}
    $view=if($check -eq 'cold-book'){'roster'}elseif($check -eq 'audio'){'garden-expanded'}else{'garden-life'}
    $flags=@('-screen-width',"$Width",'-screen-height',"$Height",'-screen-fullscreen','0','-logFile',$log,'-presentationCapture',$png,'-capturePlan10Ui','-uiView',$view)
    if($check -eq 'streaming'){
        if(-not (Test-Path -LiteralPath $fixture)){throw 'Build the separate 320-image fixture bundle first'}
        $flags+=@('-plan15Manual','-validatePlan119','-qualityFixtureBundle',$fixture)
    }elseif($check -eq 'environment-audio'){$flags+='-validatePlan119EnvironmentAudio'}else{$flags+=if($check -eq 'cold-book'){'-validatePlan119ColdBook'}else{'-validatePlan9Audio'}}
    $clock=[Diagnostics.Stopwatch]::StartNew()
    $process=Start-Process -FilePath $player -ArgumentList $flags -WindowStyle Normal -PassThru
    $peakObserved=0L;$peakProcess=0L;$samples=0
    while(-not $process.WaitForExit(100)){
        if($clock.ElapsedMilliseconds -gt 60000){Stop-Process -Id $process.Id;throw "$check Player timeout: $log"}
        try{
            $process.Refresh();$peakObserved=[Math]::Max($peakObserved,$process.WorkingSet64);$peakProcess=[Math]::Max($peakProcess,$process.PeakWorkingSet64);$samples++
        }catch [InvalidOperationException]{}
        if(Test-Path -LiteralPath $log){
            $pending=Get-Content -LiteralPath $log -Raw -ErrorAction SilentlyContinue
            if($pending -match '(?m)^(InvalidOperationException|ArgumentException|NullReferenceException|Exception):'){
                Stop-Process -Id $process.Id -ErrorAction SilentlyContinue;throw "$check Player exception: $log"
            }
        }
    }
    $clock.Stop();$text=Get-Content -LiteralPath $log -Raw
    $marker=switch($check){'streaming'{'PLAN11_9_IMAGE_STREAMING_PASS'}'cold-book'{'PLAN11_9_COLD_BOOK_MEASUREMENT'}'audio'{'PLAN9_PRODUCTION_AUDIO_PASS'}'environment-audio'{'PLAN11_9_ENVIRONMENT_AUDIO_PASS'}}
    if($process.ExitCode -ne 0 -or $text -notmatch $marker -or -not (Test-Path -LiteralPath $png)){throw "$check Player failed: $log"}
    if($check -eq 'streaming' -and $text -notmatch 'PLAN11_9_PLAYER_QUALITY_PASS'){throw 'Missing normal/watch completion after image load test'}
    if($peakObserved -le 0 -or $samples -eq 0){throw 'External process memory sampling unavailable'}
    $measurements+=[ordered]@{check=$check;measurementContext=$MeasurementContext;sampleIntervalMs=100;samples=$samples;peakObservedWorkingSetBytes=$peakObserved;peakProcessWorkingSetBytes=$peakProcess;elapsedMs=$clock.Elapsed.TotalMilliseconds;scope='Whole Windows Player working set, includes startup; not texture-only memory or VRAM';log=$log;sha256=(Get-FileHash -LiteralPath $log).Hash}
    [IO.File]::WriteAllText((Join-Path $output 'process-memory.json'),(ConvertTo-Json -InputObject @($measurements) -Depth 6)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
    Write-Output "PLAN11_9_PLAYER_CHECK_PASS check=$check observedPeakBytes=$peakObserved"
}

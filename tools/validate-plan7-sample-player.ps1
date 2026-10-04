param([string]$Player,[string[]]$Cases=@('break-0','break-1','break-2','break-3','break-4','break-5','break-6','break-7','break-8','break-9','break-10','break-11','break-12','break-13','break-14','break-15','idle','attack','hit','cutin','sit','work','look','cg','settings','enemycutin'),[int[]]$Heights=@(720,1080),[switch]$Measure,[switch]$LargeText,[switch]$Uncapped,[switch]$ValidateFocus,[switch]$CompleteBattle,[string]$ExpectedAssemblySha256,[switch]$Plan8Telemetry,[switch]$Plan8Profile)
$ErrorActionPreference='Stop'
if($Plan8Telemetry -and $Uncapped){throw 'Plan8 telemetry performance joins use normal synchronization'}
if($Plan8Profile -and (-not $Plan8Telemetry -or -not $Measure -or -not $CompleteBattle)){throw 'Plan8 profile requires normal measured full battle with telemetry'}
if($ValidateFocus -and ($Measure -or $Uncapped -or @($Cases | Where-Object {$_ -ne 'settings'}).Count -gt 0)){throw 'Focus validation requires settings cases without performance measurement'}
if($Uncapped -and $Cases -contains 'activecombat'){throw 'Active combat measurement requires normal synchronization so frame count covers action playback'}
if($CompleteBattle -and @($Cases | Where-Object {$_ -ne 'activecombat'}).Count -gt 0){throw 'CompleteBattle requires activecombat cases'}
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Player){$Player=Join-Path $repo 'game/Builds/playable/newASTER.exe'}
$assemblyPath=Join-Path (Split-Path $Player -Parent) (([IO.Path]::GetFileNameWithoutExtension($Player))+'_Data/Managed/Assembly-CSharp.dll')
$assemblySha256=(Get-FileHash -LiteralPath $assemblyPath).Hash
$resourceAssetsPath=Join-Path (Split-Path $assemblyPath -Parent | Split-Path -Parent) 'resources.assets'
$resourceAssetsSha256=(Get-FileHash -LiteralPath $resourceAssetsPath).Hash
if($ExpectedAssemblySha256 -and $assemblySha256 -ne $ExpectedAssemblySha256){throw 'Player assembly differs from expected validated build; no player launched.'}
$output=Join-Path $repo ('tmp/plan7-sample-'+[DateTime]::UtcNow.ToString('yyyyMMddHHmmss'))
New-Item -ItemType Directory -Path $output | Out-Null
foreach($case in $Cases){foreach($height in $Heights){
    if((Get-FileHash -LiteralPath $assemblyPath).Hash -ne $assemblySha256){throw 'Player assembly changed during capture; no further player launched.'}
    if((Get-FileHash -LiteralPath $resourceAssetsPath).Hash -ne $resourceAssetsSha256){throw 'Player resources changed during capture; no further player launched.'}
    if($height -ne 720 -and $height -ne 1080){throw 'Only 720p and 1080p supported'}
    $name=$case+'-'+$height;$png=Join-Path $output ($name+'.png');$log=Join-Path $output ($name+'.log')
    if($Measure -or $Uncapped){
        & (Join-Path $PSScriptRoot 'check-plan8-measurement-readiness.ps1') -Output (Join-Path $output ($name+'-environment-before.json'))
    }
    $flags=@('-screen-fullscreen','0','-screen-width',"$([int]($height*16/9))",'-screen-height',"$height",'-presentationCapture',('"'+$png+'"'),'-capturePlan7Sample','-artCase',$case,'-logFile',('"'+$log+'"'))
    if($case -in @('gameplay','activecombat')){$flags=$flags | Where-Object {$_ -notin @('-capturePlan7Sample','-artCase',$case)};$flags+='-validatePlan7Assets';if($case -eq 'gameplay'){$flags+='-capture2DActor0'}}
    if($case -eq 'activecombat'){$flags+='-validatePlan7Playback'}
    if($CompleteBattle){$flags+='-validatePlan7FullCombat'}
    if($Measure -or $Uncapped){$flags+='-measurePlan7'}
    if($Uncapped){$flags+='-measurePlan7Uncapped'}
    if($LargeText){$flags+='-inspectLargeText'}
    if($ValidateFocus){$flags+='-validatePlan7Focus'}
    if($Plan8Telemetry){$telemetryId='telemetry-'+[Guid]::NewGuid().ToString('N');$flags+=@('-plan8Telemetry','-plan8RepositoryRoot',('"'+$repo+'"'),'-plan8RunId',$telemetryId)}
    if($Plan8Profile){$profileId='profile-'+[Guid]::NewGuid().ToString('N');$flags+=@('-plan8Performance','-plan8ProfileRunId',$profileId)}
    $watch=[Diagnostics.Stopwatch]::StartNew();$peakWorking=0L;$peakObserved=0L
    $process=Start-Process -FilePath $Player -ArgumentList $flags -WindowStyle Normal -PassThru
    while(-not $process.WaitForExit(250)){
        if(Test-Path -LiteralPath $log){$pendingText=Get-Content -LiteralPath $log -Raw -ErrorAction SilentlyContinue;if($pendingText -match 'Exception:'){$process.Kill();$process.WaitForExit();throw "Own sample failed: $log"}}
        if($watch.ElapsedMilliseconds -gt $(if($CompleteBattle){180000}else{60000})){$process.Kill();throw "Sample timed out: $name"}
        try{$process.Refresh();$peakWorking=[Math]::Max($peakWorking,$process.PeakWorkingSet64);$peakObserved=[Math]::Max($peakObserved,$process.WorkingSet64)}catch [InvalidOperationException]{}
    }
    $watch.Stop()
    $text=Get-Content -LiteralPath $log -Raw
    if($process.ExitCode -ne 0 -or $text -notmatch 'PLAN7_SAMPLE_ASSETS_PASS' -or ($case -notin @('gameplay','activecombat') -and $text -notmatch ('PLAN7_SAMPLE_CAPTURE '+[regex]::Escape($case)+' / read-only')) -or $text -match '(Exception:|PLAN7_ASSET_MISSING|ILLUSTRATION_MANIFEST_WARNING)'){throw "Sample failed: $log"}
    if($case -eq 'activecombat' -and $text -notmatch 'PLAN7_PLAYBACK_EQUIVALENCE_PASS'){throw "Missing playback equivalence result: $log"}
    if($case -eq 'activecombat' -and $text -notmatch 'PLAN7_ACTIVE_COMBAT_PROGRESS_PASS'){throw "Active combat did not progress while capturing: $log"}
    if($CompleteBattle -and $text -notmatch 'PLAN7_FULL_COMBAT_PASS'){throw "Complete rendered battle did not match reward/save reference: $log"}
    if($Plan8Profile -and $text -notmatch 'PLAN8_PERFORMANCE_PROFILE_PASS colossus-plan8-2026-10-04'){throw 'Adjusted performance profile not used'}
    if($text -notmatch 'PLAN7_BUNDLED_FONT_PASS'){throw "Bundled font not validated: $log"}
    if(@([regex]::Matches($text,'PLAN7_AUDIO_WAVEFORM_PASS')).Count -ne 6){throw "Imported audio waveform validation missing: $log"}
    if($ValidateFocus -and $text -notmatch 'PLAN7_FOCUS_AUDIO_PASS'){throw "Focus/audio validation did not finish (requires application focus): $log"}
    if($Plan8Telemetry){
        if($text -notmatch 'PLAN8_TELEMETRY_COMPLETE events=' -or $text -match 'PLAN8_TELEMETRY_INCOMPLETE'){throw 'Telemetry run is incomplete'}
        $events=@(Get-Content -LiteralPath (Join-Path $repo ('tmp/plan8-runs/'+$telemetryId+'/events.jsonl')) | ForEach-Object {$_ | ConvertFrom-Json})
        if($events.Count -lt 3 -or @($events | Where-Object buildHash -ne $assemblySha256).Count -gt 0 -or @($events | Where-Object runId -ne $telemetryId).Count -gt 0){throw 'Telemetry provenance mismatch'}
        if(@($events.eventId | Select-Object -Unique).Count -ne $events.Count){throw 'Duplicate telemetry event IDs'}
        $events | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $output ($name+'-telemetry.json')) -Encoding utf8
        Write-Output "PLAN8_TELEMETRY_PLAYER_PASS run=$telemetryId events=$($events.Count)"
    }
    if(-not(Test-Path -LiteralPath $png)){throw "Missing screenshot: $png"}
    $memory=[ordered]@{case=$case;height=$height;peakWorkingBytes=$peakWorking;observedWorkingPeakBytes=$peakObserved;sampleIntervalMs=250;processElapsedMs=$watch.ElapsedMilliseconds;uncapped=[bool]$Uncapped;completeBattle=[bool]$CompleteBattle;assemblySha256=$assemblySha256;resourceAssetsSha256=$resourceAssetsSha256}
    [IO.File]::WriteAllText((Join-Path $output ($name+'-memory.json')),($memory | ConvertTo-Json)+[Environment]::NewLine)
    if($Plan8Telemetry -and $Measure){
        $performance=@(($text -split "`n") | Where-Object {$_ -match '^PLAN7_(EFFECT_)?PERFORMANCE '})
        $line=$performance | Where-Object {$_ -match '^PLAN7_PERFORMANCE '} | Select-Object -Last 1
        if(-not $line -or @($events|Where-Object category -eq 'performance').Count -eq 0){throw 'Same-run performance telemetry missing'}
        $ratio=[double]::Parse([regex]::Match($line,'under16_7ms=([0-9.]+)').Groups[1].Value,[Globalization.CultureInfo]::InvariantCulture)
        $joined=[ordered]@{schemaVersion=1;runId=$telemetryId;profile=if($Plan8Profile){'colossus-plan8-2026-10-04'}else{'ordinary-baseline'};buildHash=$assemblySha256;resourceHash=$resourceAssetsSha256;memory=$memory;performance=$performance;frameBudgetPassed=($ratio -ge .95);under16_7ms=$ratio;telemetryEvents=$events.Count;instrumentationEnabled=$true;humanTimingMeasured=$false;environmentBefore=$name+'-environment-before.json'}
        [IO.File]::WriteAllText((Join-Path $output ($name+'-plan8-measurement.json')),($joined|ConvertTo-Json -Depth 7)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
        Write-Output "PLAN8_PERFORMANCE_JOIN_PASS $telemetryId frameBudgetPassed=$($joined.frameBudgetPassed)"
    }
    if($Measure -or $Uncapped){Write-Output "PLAN7_PROCESS_MEMORY case=$name peakWorkingBytes=$peakWorking observedWorkingPeakBytes=$peakObserved"}
    if($Measure -or $Uncapped){if($text -notmatch 'PLAN7_PERFORMANCE'){throw 'Missing performance record'};($text -split "`n") | Where-Object {$_ -match 'PLAN7_PERFORMANCE'} | Write-Output}
    Write-Output "PLAN7_SAMPLE_PLAYER_CASE_PASS $name"
}}
Write-Output "PLAN7_SAMPLE_PLAYER_OUTPUT $output"

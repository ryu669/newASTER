param([string]$PlayerPath='game/Builds/plan12-april/newASTER.exe',[int]$Runs=3,[switch]$Book)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo ('tmp/runtime-timing/'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$profile=Join-Path $output 'isolated-profile';New-Item -ItemType Directory -Path $profile | Out-Null
$player=Join-Path $repo $PlayerPath;$rows=@()
. (Join-Path $PSScriptRoot 'player-validation-cache.ps1')
$buildFingerprint=Get-PlayerBuildFingerprint $player
for($run=1;$run -le $Runs;$run++){
    $log=Join-Path $output ("run-$run.log")
    $flags=@('-logFile',$log,'-runtimeTimingRoot',$profile)
    if($Book){$flags+='-runtimeTimingBook'}
    $clock=[Diagnostics.Stopwatch]::StartNew()
    # Measuring rendered readiness requires the visible interactive player window.
    $process=Start-Process -FilePath $player -ArgumentList $flags -WindowStyle Normal -PassThru
    $observed=@{};$peak=0L
    try{
        while(-not $process.HasExited){
            $process.Refresh();$peak=[Math]::Max($peak,$process.PeakWorkingSet64)
            if(Test-Path -LiteralPath $log){
                $text=[string](Get-Content -LiteralPath $log -Raw -ErrorAction SilentlyContinue)
                if($text -match '(?m)^(\w*Exception:|COMBAT_DEFINITIONS_ERROR)'){throw "Runtime timing failed: $log"}
                foreach($match in [regex]::Matches($text,'RUNTIME_TIMING phase=([a-z-]+) elapsedMs=([0-9.]+)')){
                    if(-not $observed.ContainsKey($match.Groups[1].Value)){$observed[$match.Groups[1].Value]=[Math]::Round($clock.Elapsed.TotalMilliseconds,1)}
                }
            }
            if($clock.Elapsed.TotalSeconds -gt 90){throw "Runtime timing timeout: $log"}
            Start-Sleep -Milliseconds 50
        }
        $text=Get-Content -LiteralPath $log -Raw
        if($process.ExitCode -ne 0 -or -not $observed.ContainsKey('title-interactive') -or ($Book -and -not $observed.ContainsKey('book-ready'))){throw "Missing runtime readiness: $log"}
        $phases=@([regex]::Matches($text,'RUNTIME_TIMING phase=([a-z-]+) elapsedMs=([0-9.]+)') | ForEach-Object {@{phase=$_.Groups[1].Value;elapsedMs=[double]::Parse($_.Groups[2].Value,[Globalization.CultureInfo]::InvariantCulture)}})
        $capture=Join-Path $profile ('timing-'+$(if($Book){'book-ready'}else{'title-interactive'})+'.png')
        & 'C:/Program Files/Python310/python.exe' -X utf8 (Join-Path $PSScriptRoot 'check-player-frame.py') $capture
        if($LASTEXITCODE -ne 0){throw 'Invalid timing framebuffer'}
        $retained=Join-Path $output "run-$run.png";Copy-Item -LiteralPath $capture -Destination $retained
        $rows+=@{run=$run;profile=$(if($run -eq 1){'new slot'}else{'existing slot'});processToTitleInteractiveMs=$observed['title-interactive'];phases=$phases;peakWorkingSetBytes=$peak;log=$log;capture=$retained}
        @{player=$player;buildFingerprint=$buildFingerprint;playerSha256=(Get-FileHash -LiteralPath $player).Hash;scope='Normal startup including intro and isolated slot load; rendered frame-end readiness, not physical input or GPU fence; OS cache uncontrolled';pollMs=50;resolution='default';runs=$rows} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $output 'results.json') -Encoding utf8
        Write-Output "RUNTIME_TIME_PASS run=$run titleInteractiveMs=$($observed['title-interactive']) output=$output"
    }finally{if(-not $process.HasExited){Stop-Process -Id $process.Id;$process.WaitForExit()}}
}

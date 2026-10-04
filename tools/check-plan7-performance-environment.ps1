param([string]$Output,[switch]$QuickSnapshot)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Output){$Output=Join-Path $repo ('tmp/plan7-environment-'+[DateTime]::UtcNow.ToString('yyyyMMddHHmmss')+'.json')}
New-Item -ItemType Directory -Force -Path (Split-Path $Output -Parent) | Out-Null
# Window titles and command lines are intentionally not collected.
$systemWindows=@('explorer','ChatGPT','Codex','TextInputHost','SystemSettings','ApplicationFrameHost','NVIDIA Overlay')
$windows=@(Get-Process | Where-Object {$_.MainWindowHandle -ne 0 -and $_.ProcessName -notin $systemWindows} | ForEach-Object {[ordered]@{name=$_.ProcessName;id=$_.Id}})
$logical=[Environment]::ProcessorCount
$cpu=@();$gpu=@();$gpuAvailable=$true;$systemCpu=@()
$sampleCount=if($QuickSnapshot){1}else{3}
for($sample=0;$sample -lt $sampleCount;$sample++){
    $cpu+=@(Get-CimInstance Win32_PerfFormattedData_PerfProc_Process | Where-Object {$_.Name -notin @('_Total','Idle') -and $_.PercentProcessorTime -gt 5} | ForEach-Object {[ordered]@{sample=$sample;name=$_.Name;id=$_.IDProcess;percent=$_.PercentProcessorTime}})
    $systemCpu+=@(Get-CimInstance Win32_PerfFormattedData_PerfOS_Processor | Where-Object Name -eq '_Total' | ForEach-Object {[int]$_.PercentProcessorTime})
    try{$engines=@(Get-CimInstance Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine);if($engines.Count -eq 0){$gpuAvailable=$false};$gpu+=@($engines | Where-Object {$_.UtilizationPercentage -gt 3} | ForEach-Object {$owner='unknown';if($_.Name -match '^pid_(\d+)_'){$p=Get-Process -Id ([int]$Matches[1]) -ErrorAction SilentlyContinue;if($p){$owner=$p.ProcessName}};[ordered]@{sample=$sample;engine=$_.Name;owner=$owner;percent=$_.UtilizationPercentage}})}catch{$gpuAvailable=$false}
    if($sample -lt $sampleCount-1){Start-Sleep -Milliseconds 1000}
}
$firstAcceptedSample=if($QuickSnapshot){0}else{1}
$busyCpu=@($cpu | Where-Object {$_.sample -ge $firstAcceptedSample -and $_.percent/$logical -gt 5})
$busyGpu=@($gpu | Where-Object {$_.sample -ge $firstAcceptedSample -and ($_.owner -ne 'dwm' -or $_.percent -gt 10)})
$availableMemory=@(Get-CimInstance Win32_PerfFormattedData_PerfOS_Memory)[0].AvailableMBytes
$clear=$busyCpu.Count -eq 0 -and $busyGpu.Count -eq 0 -and $gpuAvailable -and $systemCpu.Count -eq $sampleCount -and @($systemCpu | Select-Object -Skip $firstAcceptedSample | Where-Object {$_ -gt 15}).Count -eq 0 -and $availableMemory -ge 4096
$report=[ordered]@{timeUtc=[DateTime]::UtcNow.ToString('o');clear=$clear;interactiveApps=$windows;logicalProcessors=$logical;systemCpuPercent=$systemCpu;cpu=$cpu;gpu=$gpu;gpuCountersAvailable=$gpuAvailable;availableMemoryMiB=$availableMemory;note='Low-load desktop preflight, not proof all applications are closed. Initial startup sample is retained, then two consecutive samples must have process CPU <=5% of machine, total CPU <=15%, non-desktop GPU <=3%, desktop GPU <=10%. Unknown GPU or available RAM <4GiB blocks. No apps are closed.'}
if($QuickSnapshot){$report.note='Single change check within a recently cleared batch; same load thresholds, no waiting or automatic retry. No apps are closed.'}
$report.checkMode=if($QuickSnapshot){'batch-change-snapshot'}else{'full-three-sample'}
[IO.File]::WriteAllText($Output,($report | ConvertTo-Json -Depth 8)+[Environment]::NewLine)
if(-not $clear){throw "PLAN7_PERFORMANCE_DEFERRED: other apps/load or unavailable counters; inspect $Output"}
Write-Output "PLAN7_PERFORMANCE_ENVIRONMENT_CLEAR low-load-desktop apps=$($windows.Count) cpu=$($systemCpu -join ',') $Output"

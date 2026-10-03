param([string]$Output)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Output){$Output=Join-Path $repo ('tmp/plan7-environment-'+[DateTime]::UtcNow.ToString('yyyyMMddHHmmss')+'.json')}
New-Item -ItemType Directory -Force -Path (Split-Path $Output -Parent) | Out-Null
# Window titles and command lines are intentionally not collected.
$systemWindows=@('explorer','ChatGPT','Codex','TextInputHost','SystemSettings','ApplicationFrameHost','NVIDIA Overlay')
$windows=@(Get-Process | Where-Object {$_.MainWindowHandle -ne 0 -and $_.ProcessName -notin $systemWindows} | ForEach-Object {[ordered]@{name=$_.ProcessName;id=$_.Id}})
$cpu=@();$gpu=@();$gpuAvailable=$true
for($sample=0;$sample -lt 3;$sample++){
    $cpu+=@(Get-CimInstance Win32_PerfFormattedData_PerfProc_Process | Where-Object {$_.Name -notin @('_Total','Idle') -and $_.PercentProcessorTime -gt 5} | ForEach-Object {[ordered]@{sample=$sample;name=$_.Name;id=$_.IDProcess;percent=$_.PercentProcessorTime}})
    try{$engines=@(Get-CimInstance Win32_PerfFormattedData_GPUPerformanceCounters_GPUEngine);if($engines.Count -eq 0){$gpuAvailable=$false};$gpu+=@($engines | Where-Object {$_.UtilizationPercentage -gt 3} | ForEach-Object {[ordered]@{sample=$sample;engine=$_.Name;percent=$_.UtilizationPercentage}})}catch{$gpuAvailable=$false}
    if($sample -lt 2){Start-Sleep -Milliseconds 1000}
}
$busyCpu=@($cpu | Where-Object {$_.name -notmatch '^(ChatGPT|Codex|dwm|System|WmiPrvSE|powershell|pwsh|conhost|MsMpEng|csrss)(#\d+)?$'})
$clear=$windows.Count -eq 0 -and $busyCpu.Count -eq 0 -and $gpu.Count -eq 0 -and $gpuAvailable
$report=[ordered]@{timeUtc=[DateTime]::UtcNow.ToString('o');clear=$clear;interactiveApps=$windows;cpu=$cpu;gpu=$gpu;gpuCountersAvailable=$gpuAvailable;note='Conservative preflight. No apps are closed. Unknown GPU data or other interactive apps defer isolated performance measurement.'}
[IO.File]::WriteAllText($Output,($report | ConvertTo-Json -Depth 8)+[Environment]::NewLine)
if(-not $clear){throw "PLAN7_PERFORMANCE_DEFERRED: other apps/load or unavailable counters; inspect $Output"}
Write-Output "PLAN7_PERFORMANCE_ENVIRONMENT_CLEAR $Output"

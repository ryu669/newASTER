param([string]$PlayerPath='game/Builds/plan12-april/newASTER.exe')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo ('tmp/heroine-scale-ui/'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $output | Out-Null
$log=Join-Path $output 'player.log';$png=Join-Path $output 'frame.png'
$p=Start-Process (Join-Path $repo $PlayerPath) -ArgumentList @('-logFile',$log,'-presentationCapture',$png,'-plan12AprilPreview','-capturePlan10Ui','-uiView','roster','-validateScaleUi') -WindowStyle Normal -PassThru
$clock=[Diagnostics.Stopwatch]::StartNew();$peak=0L
try{
 while(-not $p.WaitForExit(100)){
  $p.Refresh();$peak=[Math]::Max($peak,$p.PeakWorkingSet64)
  if(Test-Path $log){$text=Get-Content $log -Raw;if($text -match '(?m)^(\w*Exception:|COMBAT_DEFINITIONS_ERROR|Serialization depth limit)'){throw "Scale UI error: $log"}}
  if($clock.Elapsed.TotalSeconds -gt 240){throw "Scale UI timeout: $log"}
 }
 $text=Get-Content $log -Raw
 if($p.ExitCode -ne 0 -or $text -notmatch 'SCALE_UI_PLAYER_PASS' -or $text -notmatch 'SCALE_UI_OPERATIONS_PASS'){throw "Scale UI incomplete: $log"}
 $files=@(Get-ChildItem $output -Filter '*.png')
 if($files.Count -lt 36){throw "Missing scale screenshots: $output"}
 foreach($file in $files){& 'C:/Program Files/Python310/python.exe' -X utf8 (Join-Path $PSScriptRoot 'check-player-frame.py') $file.FullName;if($LASTEXITCODE -ne 0){throw "Invalid scale screenshot $file"}}
 $measurements=@(foreach($line in Get-Content (Join-Path $output 'scale-ui-times.txt')){
  if($line -notmatch '^(\S+) readyMs=([\d.]+) p95FrameMs=([\d.]+) maxFrameMs=([\d.]+)$'){throw "Invalid timing row: $line"}
  $culture=[Globalization.CultureInfo]::InvariantCulture
  $row=@{view=$Matches[1];readyMs=[double]::Parse($Matches[2],$culture);p95FrameMs=[double]::Parse($Matches[3],$culture);maxFrameMs=[double]::Parse($Matches[4],$culture)}
  $limit=if($row.view -eq 'roster-last'){3000}else{2000}
  if($row.readyMs -gt $limit -or $row.p95FrameMs -gt 67){throw "Scale UI timing regression: $line"}
  $row
 })
 if($measurements.Count -ne 35){throw "Incomplete timing coverage: $output"}
 @{persons=200;forms=203;sharedFixtureArt=$true;screens=$files.Count;peakWorkingSetBytes=$peak;defaultResolution='1920x1080';output=$output;measurements=$measurements} | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $output 'results.json') -Encoding utf8
 "SCALE_UI_VALIDATION_PASS $output"
}finally{if(-not $p.HasExited){Stop-Process -Id $p.Id}}

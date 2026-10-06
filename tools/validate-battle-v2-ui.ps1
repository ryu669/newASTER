param([int[]]$Heights=@(720,1080),[string]$PlayerPath='game/Builds/playable/newASTER.exe')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo ('tmp/battle-v2-ui-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
$player=Join-Path $repo $PlayerPath
$assembly=Join-Path (Split-Path $player -Parent) 'newASTER_Data/Managed/Assembly-CSharp.dll'
$hash=(Get-FileHash $assembly).Hash
$settings=Get-Content (Join-Path $repo 'game/unity/ProjectSettings/ProjectSettings.asset')
$company=($settings | Select-String '^  companyName: (.+)$').Matches[0].Groups[1].Value
$product=($settings | Select-String '^  productName: (.+)$').Matches[0].Groups[1].Value
$normal=Join-Path ([Environment]::GetFolderPath('UserProfile')) ('AppData/LocalLow/'+$company+'/'+$product)
function Fingerprint { $files=@();if(Test-Path $normal){$files=@(Get-ChildItem -LiteralPath $normal -File | Where-Object Name -notin @('Player.log','Player-prev.log') | Sort-Object Name | ForEach-Object { @{name=$_.Name;hash=(Get-FileHash -LiteralPath $_.FullName).Hash} })};ConvertTo-Json -InputObject @($files) -Compress }
$before=Fingerprint;$results=@()
foreach($height in $Heights){foreach($actor in 0..4){
 $name='job-'+$actor+'-'+$height;$png=Join-Path $output ($name+'.png');$log=Join-Path $output ($name+'.log')
 $flags=@('-screen-fullscreen','0','-screen-width',"$([int]($height*16/9))",'-screen-height',"$height",'-presentationCapture',$png,'-logFile',$log,'-capturePlan9ProductionEntry','-captureHeroineSanctuary','-heroineView','battle-idle','-heroineId','heroine.slayer','-captureBattleJob',"$actor")
 $watchdog=Start-Job -ArgumentList $player,$log -ScriptBlock {param($p,$l) Start-Sleep -Seconds 90;Get-CimInstance Win32_Process -Filter "Name='newASTER.exe'" | Where-Object {$_.ExecutablePath -eq $p -and $_.CommandLine.Contains($l)} | ForEach-Object {Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue}}
 try{& $player @flags | Out-Null}finally{Stop-Job $watchdog;Remove-Job $watchdog}
 $text=Get-Content -LiteralPath $log -Raw
 if($LASTEXITCODE -ne 0 -or $text -match 'Exception:|error CS' -or -not(Test-Path $png) -or $text -notmatch ('BATTLE_JOB_V2_CAPTURE_PASS actor='+$actor)){throw ('Job capture failed '+$name+' '+$log)}
 if((Get-FileHash $assembly).Hash -ne $hash){throw 'Build changed during capture'}
 $results+=@{actor=$actor;height=$height;image=$png;log=$log;state='rendered-not-visually-approved';physicalInput=0}
 Write-Output ('BATTLE_V2_UI_PASS '+$name)
}}
if((Fingerprint) -ne $before){throw 'Normal profile changed'}
@{schemaVersion=1;assemblySha256=$hash;normalSaveUnchanged=$true;performanceMeasured=$false;physicalInputCount=0;cases=$results} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'results.json') -Encoding utf8
Write-Output ('BATTLE_V2_UI_OUTPUT '+$output)

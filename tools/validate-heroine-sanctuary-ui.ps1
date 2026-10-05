param([int[]]$Heights=@(720,1080),[string[]]$Cases=@('roster','empty','detail.slayer','detail.iconoclast','detail.undermine','detail.echidna','detail.excalipan','trait','skill','skillmax','skill-pending','tree.slayer','tree.iconoclast','tree.undermine','tree.echidna','tree.excalipan','tree-grown','tree-confirm','level','level-confirm','large'),[string]$PlayerPath='game/Builds/playable/newASTER.exe')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo ('tmp/heroine-sanctuary-ui-'+[Guid]::NewGuid().ToString('N'));New-Item -ItemType Directory -Path $output | Out-Null
$player=Join-Path $repo $PlayerPath;$assembly=Join-Path (Split-Path $player -Parent) 'newASTER_Data/Managed/Assembly-CSharp.dll';$hash=(Get-FileHash $assembly).Hash
$normal=Join-Path ([Environment]::GetFolderPath('UserProfile')) 'AppData/LocalLow/newASTER/巨神と誓女2'
$settings=Get-Content (Join-Path $repo 'game/unity/ProjectSettings/ProjectSettings.asset')
$company=($settings | Select-String '^  companyName: (.+)$').Matches[0].Groups[1].Value;$product=($settings | Select-String '^  productName: (.+)$').Matches[0].Groups[1].Value
$normal=Join-Path ([Environment]::GetFolderPath('UserProfile')) ('AppData/LocalLow/'+$company+'/'+$product)
function Fingerprint { $files=@();if(Test-Path $normal){$files=@(Get-ChildItem -LiteralPath $normal -File | Where-Object Name -notin @('Player.log','Player-prev.log') | Sort-Object Name | ForEach-Object { @{name=$_.Name;hash=(Get-FileHash -LiteralPath $_.FullName).Hash} })};ConvertTo-Json -InputObject @($files) -Compress }
$before=Fingerprint;$results=@()
foreach($height in $Heights){if($height -notin @(720,1080)){throw 'Unsupported height'}
 foreach($case in $Cases){
  $parts=$case.Split('.');$view=$parts[0];$hero=if($parts.Length -gt 1){'heroine.'+$parts[1]}else{'heroine.slayer'}
  if($view -notin @('roster','empty','detail','trait','skill','skillmax','skill-pending','tree','tree-grown','tree-confirm','level','level-confirm','large')){throw 'Unknown view'}
  $name=$case+'-'+$height;$png=Join-Path $output ($name+'.png');$log=Join-Path $output ($name+'.log')
  $flags=@('-screen-fullscreen','0','-screen-width',"$([int]($height*16/9))",'-screen-height',"$height",'-presentationCapture',$png,'-logFile',$log,'-capturePlan9ProductionEntry','-captureHeroineSanctuary','-heroineView',$(if($view -eq 'large'){'detail'}else{$view}),'-heroineId',$hero)
  if($view -eq 'large'){$flags+='-inspectLargeText'}
  $watchdog=Start-Job -ArgumentList $player,$log -ScriptBlock {param($p,$l) Start-Sleep -Seconds 90;Get-CimInstance Win32_Process -Filter "Name='newASTER.exe'" | Where-Object {$_.ExecutablePath -eq $p -and $_.CommandLine.Contains($l)} | ForEach-Object {Stop-Process -Id $_.ProcessId -ErrorAction SilentlyContinue}}
  try{& $player @flags | Out-Null}finally{Stop-Job $watchdog;Remove-Job $watchdog}
  $text=Get-Content -LiteralPath $log -Raw
  if($LASTEXITCODE -ne 0 -or $text -match 'Exception:|error CS' -or -not(Test-Path $png) -or $text -notmatch ('HEROINE_SANCTUARY_CAPTURE_PASS.*heroine='+[regex]::Escape($hero))){throw ('Sanctuary capture failed '+$name+' '+$log)}
  if((Get-FileHash $assembly).Hash -ne $hash){throw 'Build changed during capture'}
  $results+=@{case=$case;height=$height;image=$png;log=$log;state='rendered-not-visually-approved';physicalInput=0}
  Write-Output ('HEROINE_SANCTUARY_UI_PASS '+$name)
 }
}
if((Fingerprint) -ne $before){throw 'Normal profile changed'}
@{schemaVersion=1;assemblySha256=$hash;normalSaveUnchanged=$true;performanceMeasured=$false;physicalInputCount=0;cases=$results} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'results.json') -Encoding utf8
Write-Output ('HEROINE_SANCTUARY_UI_OUTPUT '+$output)

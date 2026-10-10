param([string[]]$Views=@('job-fighter','job-berserker','job-defender','job-blaster','job-gunner','job-artist','job-healer','job-panzer','job-alchemist','job-chaser','job-sniper','job-gambler','job-general'),[string]$PlayerPath='game/Builds/plan12-preparation/newASTER.exe')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo 'tmp/job-panel-validation'
New-Item -ItemType Directory -Force $output | Out-Null
$assembly=Join-Path (Split-Path (Join-Path $repo $PlayerPath) -Parent) 'newASTER_Data/Managed/Assembly-CSharp.dll'
$hash=(Get-FileHash $assembly).Hash
$cases=@()
foreach($view in $Views){
 try{
  & (Join-Path $PSScriptRoot 'validate-plan10-ui-player.ps1') -Views @($view) -Width 1920 -Height 1080 -PlayerPath $PlayerPath -TimeoutSeconds 90
  $base=Join-Path $repo "tmp/plan10-ui/player-1920x1080/ui-$view"
  $log=Get-Content "$base.log" -Raw
  if($view -match '^job-[a-z]+$' -and $log -notmatch ('JOB_PANEL_OPERATIONS_PASS job=job.'+$view.Substring(4)+' ')){throw "Missing operation verification $view"}
  Copy-Item -LiteralPath "$base.png" -Destination (Join-Path $output "$view.png") -Force
  Copy-Item -LiteralPath "$base.log" -Destination (Join-Path $output "$view.log") -Force
  if((Get-FileHash $assembly).Hash -ne $hash){throw 'Player changed during verification'}
  $cases+=@{view=$view;passed=$true;operationProbe=($view -match '^job-[a-z]+$');image=(Join-Path $output "$view.png");imageSha256=(Get-FileHash "$base.png").Hash;physicalInput=0;visualReview='pending'}
 }catch{
  $cases+=@{view=$view;passed=$false;error=$_.Exception.Message};Write-Output "JOB_PANEL_CASE_FAILED $view $($_.Exception.Message)"
 }
 @{assemblySha256=$hash;resolution=@(1920,1080);cases=$cases} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'latest-run.json') -Encoding UTF8
}
if(@($cases | Where-Object {-not $_.passed}).Count){throw 'One or more job panel cases failed; see tmp/job-panel-validation/latest-run.json'}
Write-Output "JOB_PANEL_CAPTURE_PASS cases=$($cases.Count)"

param([int[]]$Heights=@(720,1080),[string[]]$Cases=@('title','settings','credits','exit','development','book','growth','battle','victory','garden','adv'))
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$player=Join-Path $repo 'game/Builds/playable/newASTER.exe'
$output=Join-Path $repo ('tmp/plan9-ui-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
$settings=Get-Content (Join-Path $repo 'game/unity/ProjectSettings/ProjectSettings.asset')
$company=($settings | Select-String '^  companyName: (.+)$').Matches[0].Groups[1].Value
$product=($settings | Select-String '^  productName: (.+)$').Matches[0].Groups[1].Value
$normal=Join-Path ([Environment]::GetFolderPath('UserProfile')) ('AppData/LocalLow/'+$company+'/'+$product)
function Fingerprint {
 $items=@();if(Test-Path $normal){$items=@(Get-ChildItem -LiteralPath $normal -File | Where-Object Name -notin @('Player.log','Player-prev.log') | Sort-Object Name | ForEach-Object { [ordered]@{name=$_.Name;hash=(Get-FileHash -LiteralPath $_.FullName).Hash} })}
 ConvertTo-Json -InputObject @($items) -Compress
}
$before=Fingerprint
$assembly=Join-Path (Split-Path $player -Parent) 'newASTER_Data/Managed/Assembly-CSharp.dll'
$buildHash=(Get-FileHash $assembly).Hash
$results=@()
foreach($height in $Heights){
 if($height -notin @(720,1080)){throw 'Unsupported height'}
 foreach($case in $Cases){
  $name=$case+'-'+$height;$png=Join-Path $output ($name+'.png');$log=Join-Path $output ($name+'.log')
  $flags=@('-screen-fullscreen','0','-screen-width',"$([int]($height*16/9))",'-screen-height',"$height",'-presentationCapture',$png,'-logFile',$log)
  switch($case){
   'title' {$flags+='-capturePlan9Title'}
   'intro' {$flags+=@('-capturePlan9Title','-capturePlan9Intro')}
   'startup-error' {$flags+=@('-capturePlan9Title','-capturePlan9StartupError')}
   'settings-cancel' {$flags+=@('-capturePlan9Title','-validatePlan9SettingsCancel')}
   'settings-large' {$flags+=@('-capturePlan9Title','-plan9TitlePanel','settings','-inspectLargeText')}
   'audio-focus' {$flags+=@('-capturePlan7Sample','-artCase','settings','-validatePlan7Focus')}
   {$_ -in @('settings','credits','exit','development','help')} {$flags+=@('-capturePlan9Title','-plan9TitlePanel',$case)}
   'book' {$flags+='-captureBook'}
   'book-empty' {$flags+=@('-captureBook','-bookEmpty')}
   'book-last' {$flags+=@('-captureBook','-bookLast')}
   'growth' {$flags+='-captureGrowth'}
   'iconoclast-growth' {$flags+=@('-captureGrowth','-captureGrowthHero','heroine.iconoclast')}
   'iconoclast-standing' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Standing','-inspectPlan9Hero','heroine.iconoclast')}
   'iconoclast-attack' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','attack','-inspectPlan9Hero','heroine.iconoclast')}
   'iconoclast-hit' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','hit','-inspectPlan9Hero','heroine.iconoclast')}
   'iconoclast-cutin' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','cutin','-inspectPlan9Hero','heroine.iconoclast')}
   'growth-large' {$flags+=@('-captureGrowth','-inspectLargeText')}
   'book-large' {$flags+=@('-captureBook','-inspectLargeText')}
   'battle' {$flags+=@('-captureBattleMenu','closed')}
   'victory' {$flags+='-captureVictory'}
   'victory-pending' {$flags+=@('-captureVictory','-captureVictoryPending')}
   {$_ -in @('garden','adv')} {$flags+=@('-capturePlan6Home','-plan6Save',(Join-Path $output ($name+'-fixture.json')),'-plan6NewSave','-homeCase',([Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase($case)))}
   default {throw ('Unknown case '+$case)}
  }
  # A piped GUI executable waits for its own process; no physical input is injected.
  & $player @flags | Out-Null
  $text=Get-Content -LiteralPath $log -Raw
  if($LASTEXITCODE -ne 0 -or $text -match 'Exception:|error CS' -or -not(Test-Path $png)){throw ('Capture failed: '+$name+' '+$log)}
  if($case -in @('garden','adv') -and $text -cnotmatch ('case='+[Globalization.CultureInfo]::InvariantCulture.TextInfo.ToTitleCase($case))){throw ('Incorrect home scene: '+$case)}
  if($case -eq 'settings-cancel' -and $text -notmatch 'PLAN9_TITLE_SETTINGS_CANCEL_PASS'){throw 'Title settings cancellation diagnostic missing'}
  if($case -eq 'audio-focus' -and ($text -notmatch 'PLAN9_TITLE_AUDIO_PASS' -or $text -notmatch 'PLAN7_FOCUS_AUDIO_PASS')){throw 'Audio focus diagnostic missing'}
  if((Get-FileHash $assembly).Hash -ne $buildHash){throw 'Build changed during capture'}
  $results+=@{case=$case;height=$height;image=$png;log=$log;state='rendered-not-visually-approved';humanInput=$false}
  Write-Output ('PLAN9_UI_CAPTURE_PASS '+$name)
 }
}
if((Fingerprint) -ne $before){throw 'Normal player files changed'}
@{schemaVersion=1;assemblySha256=$buildHash;normalSaveUnchanged=$true;performanceMeasured=$false;cases=$results} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'results.json') -Encoding utf8
Write-Output ('PLAN9_UI_OUTPUT '+$output)

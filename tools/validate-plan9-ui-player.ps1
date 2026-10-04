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
   'undermine-growth' {$flags+=@('-captureGrowth','-captureGrowthHero','heroine.undermine')}
   'echidna-growth' {$flags+=@('-captureGrowth','-captureGrowthHero','heroine.echidna')}
   'slayer-growth' {$flags+=@('-captureGrowth','-captureGrowthHero','heroine.slayer')}
   {$_ -in @('slayer-normal','slayer-joy','slayer-puzzled','slayer-determined')} {$flags+=@('-capturePlan9Title','-inspectPlan9Expression',$case.Substring(7),'-inspectPlan9ArtSlayer')}
   {$_ -in @('slayer-adv-normal','slayer-adv-joy','slayer-adv-puzzled','slayer-adv-determined')} {$flags+=@('-capturePlan6Home','-plan6Save',(Join-Path $output ($name+'-fixture.json')),'-plan6NewSave','-homeCase','Adv','-inspectPlan9AdvSlayer',$case.Substring(11))}
   {$_ -in @('slayer-cg-0','slayer-cg-1','slayer-cg-2','slayer-cg-3','slayer-cg-4')} {$flags+=@('-capturePlan9Title','-inspectPlan9Cg',$case.Substring(10),'-inspectPlan9ArtSlayer')}
   {$_ -in @('slayer-garden-idle','slayer-garden-sit','slayer-garden-work','slayer-garden-look','slayer-garden-move','slayer-garden-remove')} {$flags+=@('-capturePlan6Home','-plan6Save',(Join-Path $output ($name+'-fixture.json')),'-plan6NewSave','-homeCase','Garden','-inspectPlan7GardenUse',$case.Substring(14),'-inspectPlan9GardenSlayer')}
   'slayer-standing' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Standing','-inspectPlan9Hero','heroine.slayer')}
   'slayer-attack' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','attack','-inspectPlan9Hero','heroine.slayer')}
   'slayer-hit' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','hit','-inspectPlan9Hero','heroine.slayer')}
   'slayer-cutin' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','cutin','-inspectPlan9Hero','heroine.slayer')}
   'excalipan-growth' {$flags+=@('-captureGrowth','-captureGrowthHero','heroine.excalipan')}
   {$_ -in @('excalipan-normal','excalipan-joy','excalipan-puzzled','excalipan-determined')} {$flags+=@('-capturePlan9Title','-inspectPlan9Expression',$case.Substring(10),'-inspectPlan9ArtExcalipan')}
   {$_ -in @('excalipan-adv-normal','excalipan-adv-joy','excalipan-adv-puzzled','excalipan-adv-determined')} {$flags+=@('-capturePlan6Home','-plan6Save',(Join-Path $output ($name+'-fixture.json')),'-plan6NewSave','-homeCase','Adv','-inspectPlan9AdvExcalipan',$case.Substring(14))}
   {$_ -in @('excalipan-cg-0','excalipan-cg-1','excalipan-cg-2','excalipan-cg-3','excalipan-cg-4')} {$flags+=@('-capturePlan9Title','-inspectPlan9Cg',$case.Substring(13),'-inspectPlan9ArtExcalipan')}
   {$_ -in @('excalipan-garden-idle','excalipan-garden-sit','excalipan-garden-work','excalipan-garden-look','excalipan-garden-move','excalipan-garden-remove')} {$flags+=@('-capturePlan6Home','-plan6Save',(Join-Path $output ($name+'-fixture.json')),'-plan6NewSave','-homeCase','Garden','-inspectPlan7GardenUse',$case.Substring(17),'-inspectPlan9GardenExcalipan')}
   'excalipan-standing' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Standing','-inspectPlan9Hero','heroine.excalipan')}
   'excalipan-attack' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','attack','-inspectPlan9Hero','heroine.excalipan')}
   'excalipan-hit' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','hit','-inspectPlan9Hero','heroine.excalipan')}
   'excalipan-cutin' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','cutin','-inspectPlan9Hero','heroine.excalipan')}
   {$_ -in @('echidna-normal','echidna-joy','echidna-puzzled','echidna-determined')} {$flags+=@('-capturePlan9Title','-inspectPlan9Expression',$case.Substring(8),'-inspectPlan9ArtEchidna')}
   {$_ -in @('echidna-adv-normal','echidna-adv-joy','echidna-adv-puzzled','echidna-adv-determined')} {$flags+=@('-capturePlan6Home','-plan6Save',(Join-Path $output ($name+'-fixture.json')),'-plan6NewSave','-homeCase','Adv','-inspectPlan9AdvEchidna',$case.Substring(12))}
   {$_ -in @('echidna-cg-0','echidna-cg-1','echidna-cg-2','echidna-cg-3','echidna-cg-4')} {$flags+=@('-capturePlan9Title','-inspectPlan9Cg',$case.Substring(11),'-inspectPlan9ArtEchidna')}
   {$_ -in @('echidna-garden-idle','echidna-garden-sit','echidna-garden-work','echidna-garden-look','echidna-garden-move','echidna-garden-remove')} {$flags+=@('-capturePlan6Home','-plan6Save',(Join-Path $output ($name+'-fixture.json')),'-plan6NewSave','-homeCase','Garden','-inspectPlan7GardenUse',$case.Substring(15),'-inspectPlan9GardenEchidna')}
   'echidna-standing' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Standing','-inspectPlan9Hero','heroine.echidna')}
   'echidna-attack' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','attack','-inspectPlan9Hero','heroine.echidna')}
   'echidna-hit' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','hit','-inspectPlan9Hero','heroine.echidna')}
   'echidna-cutin' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','cutin','-inspectPlan9Hero','heroine.echidna')}
   'undermine-standing' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Standing','-inspectPlan9Hero','heroine.undermine')}
   'undermine-attack' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','attack','-inspectPlan9Hero','heroine.undermine')}
   'undermine-hit' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','hit','-inspectPlan9Hero','heroine.undermine')}
   'undermine-cutin' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','cutin','-inspectPlan9Hero','heroine.undermine')}
   {$_ -in @('undermine-cg-0','undermine-cg-1','undermine-cg-2','undermine-cg-3','undermine-cg-4')} {$flags+=@('-capturePlan9Title','-inspectPlan9Cg',$case.Substring(13),'-inspectPlan9ArtUndermine')}
   {$_ -in @('undermine-normal','undermine-joy','undermine-puzzled','undermine-determined')} {$flags+=@('-capturePlan9Title','-inspectPlan9Expression',$case.Substring(10),'-inspectPlan9ArtUndermine')}
   {$_ -in @('undermine-adv-normal','undermine-adv-joy','undermine-adv-puzzled','undermine-adv-determined')} {$flags+=@('-capturePlan6Home','-plan6Save',(Join-Path $output ($name+'-fixture.json')),'-plan6NewSave','-homeCase','Adv','-inspectPlan9AdvUndermine',$case.Substring(14))}
   {$_ -in @('undermine-garden-idle','undermine-garden-sit','undermine-garden-work','undermine-garden-look','undermine-garden-move','undermine-garden-remove')} {$flags+=@('-capturePlan6Home','-plan6Save',(Join-Path $output ($name+'-fixture.json')),'-plan6NewSave','-homeCase','Garden','-inspectPlan7GardenUse',$case.Substring(17),'-inspectPlan9GardenUndermine')}
   'iconoclast-standing' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Standing','-inspectPlan9Hero','heroine.iconoclast')}
   'iconoclast-attack' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','attack','-inspectPlan9Hero','heroine.iconoclast')}
   'iconoclast-hit' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','hit','-inspectPlan9Hero','heroine.iconoclast')}
   'iconoclast-cutin' {$flags+=@('-captureBattleMenu','closed','-inspectPlan7Battle','cutin','-inspectPlan9Hero','heroine.iconoclast')}
   {$_ -in @('iconoclast-cg-0','iconoclast-cg-1','iconoclast-cg-2','iconoclast-cg-3','iconoclast-cg-4')} {$flags+=@('-capturePlan9Title','-inspectPlan9Cg',$case.Substring(14))}
   {$_ -in @('iconoclast-normal','iconoclast-joy','iconoclast-puzzled','iconoclast-determined')} {$flags+=@('-capturePlan9Title','-inspectPlan9Expression',$case.Substring(11))}
   {$_ -in @('iconoclast-adv-normal','iconoclast-adv-joy','iconoclast-adv-puzzled','iconoclast-adv-determined')} {$flags+=@('-capturePlan6Home','-plan6Save',(Join-Path $output ($name+'-fixture.json')),'-plan6NewSave','-homeCase','Adv','-inspectPlan9AdvExpression',$case.Substring(15))}
   {$_ -in @('iconoclast-garden-idle','iconoclast-garden-sit','iconoclast-garden-work','iconoclast-garden-look','iconoclast-garden-move','iconoclast-garden-remove')} {$flags+=@('-capturePlan6Home','-plan6Save',(Join-Path $output ($name+'-fixture.json')),'-plan6NewSave','-homeCase','Garden','-inspectPlan7GardenUse',$case.Substring(18),'-inspectPlan9GardenHero')}
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
  if($case.StartsWith('slayer-adv-') -and $text -notmatch 'PLAN9_ADV_EXPRESSION_CAPTURE.*heroine=heroine.slayer'){throw 'Wrong ADV heroine'}
  if($case.StartsWith('slayer-garden-') -and $text -notmatch 'PLAN9_GARDEN_USE_CAPTURE.*heroine=heroine.slayer'){throw 'Wrong garden heroine'}
  if($case.StartsWith('slayer-cg-') -and $text -notmatch ('PLAN9_CG_CAPTURE heroine=heroine.slayer event='+$case.Substring(10))){throw 'Wrong CG heroine or stale build'}
  if($case.StartsWith('iconoclast-adv-') -and $text -notmatch 'PLAN9_ADV_EXPRESSION_CAPTURE.*heroine=heroine.iconoclast'){throw 'Wrong ADV heroine'}
  if($case.StartsWith('undermine-adv-') -and $text -notmatch 'PLAN9_ADV_EXPRESSION_CAPTURE.*heroine=heroine.undermine'){throw 'Wrong ADV heroine'}
  if($case.StartsWith('undermine-cg-') -and $text -notmatch ('PLAN9_CG_CAPTURE heroine=heroine.undermine event='+$case.Substring(13))){throw 'Wrong CG heroine or stale build'}
  if($case.StartsWith('iconoclast-cg-') -and $text -notmatch ('PLAN9_CG_CAPTURE heroine=heroine.iconoclast event='+$case.Substring(14))){throw 'Wrong CG heroine or stale build'}
  if($case.StartsWith('iconoclast-garden-') -and $text -notmatch 'PLAN9_GARDEN_USE_CAPTURE.*heroine=heroine.iconoclast'){throw 'Wrong garden heroine'}
  if($case.StartsWith('undermine-garden-') -and $text -notmatch 'PLAN9_GARDEN_USE_CAPTURE.*heroine=heroine.undermine'){throw 'Wrong garden heroine'}
  if($case.StartsWith('echidna-garden-') -and $text -notmatch 'PLAN9_GARDEN_USE_CAPTURE.*heroine=heroine.echidna'){throw 'Wrong garden heroine'}
  if($case.StartsWith('excalipan-garden-') -and $text -notmatch 'PLAN9_GARDEN_USE_CAPTURE.*heroine=heroine.excalipan'){throw 'Wrong garden heroine'}
  if($case.StartsWith('echidna-adv-') -and $text -notmatch 'PLAN9_ADV_EXPRESSION_CAPTURE.*heroine=heroine.echidna'){throw 'Wrong ADV heroine'}
  if($case.StartsWith('excalipan-adv-') -and $text -notmatch 'PLAN9_ADV_EXPRESSION_CAPTURE.*heroine=heroine.excalipan'){throw 'Wrong ADV heroine'}
  if($case.StartsWith('excalipan-cg-') -and $text -notmatch ('PLAN9_CG_CAPTURE heroine=heroine.excalipan event='+$case.Substring(13))){throw 'Wrong CG heroine or stale build'}
  if($case.StartsWith('echidna-cg-') -and $text -notmatch ('PLAN9_CG_CAPTURE heroine=heroine.echidna event='+$case.Substring(11))){throw 'Wrong CG heroine or stale build'}
  if((Get-FileHash $assembly).Hash -ne $buildHash){throw 'Build changed during capture'}
  $results+=@{case=$case;height=$height;image=$png;log=$log;state='rendered-not-visually-approved';humanInput=$false}
  Write-Output ('PLAN9_UI_CAPTURE_PASS '+$name)
 }
}
if((Fingerprint) -ne $before){throw 'Normal player files changed'}
@{schemaVersion=1;assemblySha256=$buildHash;normalSaveUnchanged=$true;performanceMeasured=$false;cases=$results} | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $output 'results.json') -Encoding utf8
Write-Output ('PLAN9_UI_OUTPUT '+$output)

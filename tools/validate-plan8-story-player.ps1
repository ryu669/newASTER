param([int]$Height=720,[switch]$Journey,[switch]$Exchange,[switch]$Telemetry,[ValidateSet('progress','conditions','garden','events')][string[]]$Views=@())
$ErrorActionPreference='Stop'
if($Height -notin @(720,1080)){throw 'Only 720p/1080p supported'}
if($Exchange -and -not $Journey){throw 'Exchange requires earned Journey'}
$repo=Split-Path $PSScriptRoot -Parent
$player=Join-Path $repo 'game/Builds/playable/newASTER.exe'
$id='story-'+[Guid]::NewGuid().ToString('N')
$output=Join-Path $repo ('tmp/plan8-story-player/'+$id)
New-Item -ItemType Directory -Path $output | Out-Null
$settings=Get-Content -LiteralPath (Join-Path $repo 'game/unity/ProjectSettings/ProjectSettings.asset')
$company=($settings | Select-String '^  companyName: (.+)$').Matches[0].Groups[1].Value
$product=($settings | Select-String '^  productName: (.+)$').Matches[0].Groups[1].Value
$normal=Join-Path ([Environment]::GetFolderPath('UserProfile')) ('AppData/LocalLow/'+$company+'/'+$product)
function Fingerprint {
    $items=@()
    if(Test-Path -LiteralPath $normal){foreach($file in (Get-ChildItem -LiteralPath $normal -File | Where-Object Name -notin @('Player.log','Player-prev.log') | Sort-Object Name)){
        $items+=[ordered]@{name=$file.Name;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash;bytes=$file.Length}
    }}
    return ConvertTo-Json -InputObject @($items) -Depth 4 -Compress
}
$before=Fingerprint
$data=Join-Path (Split-Path $player -Parent) 'newASTER_Data'
$assembly=(Get-FileHash -LiteralPath (Join-Path $data 'Managed/Assembly-CSharp.dll')).Hash
$resource=(Get-FileHash -LiteralPath (Join-Path $data 'resources.assets')).Hash
$save=Join-Path $repo ('tmp/plan8-runs/'+$id+'/formal-campaign-v1.json')
$cases=@()
try {
    foreach($case in (@('new','resume')+$Views)){
        if((Get-FileHash -LiteralPath (Join-Path $data 'Managed/Assembly-CSharp.dll')).Hash -ne $assembly -or (Get-FileHash -LiteralPath (Join-Path $data 'resources.assets')).Hash -ne $resource){throw 'Player changed between story processes'}
        $log=Join-Path $output ($case+'.log');$png=Join-Path $output ($case+'.png')
        $saveBefore=if($case -ne 'new'){(Get-FileHash -LiteralPath $save).Hash}else{$null}
        $flags=@('-screen-fullscreen','0','-screen-width',"$([int]($Height*16/9))",'-screen-height',"$Height",'-presentationCapture',('"'+$png+'"'),'-capturePlan8Story','-plan8RepositoryRoot',('"'+$repo+'"'),'-plan8StoryRunId',$id,'-logFile',('"'+$log+'"'))
        if($case -ne 'new'){$flags+='-plan8StoryResume'}
        if($case -in $Views){$flags+=@('-plan8StoryView',$case)}
        if($Journey){$flags+='-plan8Journey'}
        if($Exchange){$flags+='-plan8JourneyExchange'}
        if($Telemetry){$telemetryId='journey-telemetry-'+[Guid]::NewGuid().ToString('N');$flags+=@('-plan8Telemetry','-plan8RunId',$telemetryId)}
        $process=Start-Process -FilePath $player -ArgumentList $flags -WindowStyle Normal -PassThru
        $timer=[Diagnostics.Stopwatch]::StartNew()
        while(-not $process.WaitForExit(250)){
            if($timer.Elapsed.TotalSeconds -gt 180){$process.Kill();throw 'Own story diagnostic timed out'}
            if(Test-Path -LiteralPath $log){$pendingText=Get-Content -LiteralPath $log -Raw -ErrorAction SilentlyContinue;if($pendingText -match 'Exception:'){$process.Kill();$process.WaitForExit();throw "Own story diagnostic failed: $log"}}
        }
        $text=Get-Content -LiteralPath $log -Raw
        if($process.ExitCode -ne 0 -or $text -notmatch 'PLAN8_STORY_PLAYER_PASS' -or $text -match 'Exception:|Error:|error CS'){throw "Story case failed: $case ($log)"}
        if(-not(Test-Path -LiteralPath $png)){throw 'Story screenshot missing'}
        $snapshot=Get-Content -LiteralPath $save -Raw | ConvertFrom-Json
        if($snapshot.home.contentVersion -ne 'home-trial-story-2026-10-04' -or $snapshot.collection.contentVersion -ne 'collection-trial-story-2026-10-04' -or $snapshot.home.loverHeroineIds.Count -ne 0){throw 'Original story save version or lover state invalid'}
        if(@($snapshot.world.readStoryIds | Where-Object {$_ -like 'trial.plan8.*'}).Count -ne 8 -or $snapshot.home.readEventIds.Count -ne 1 -or $snapshot.home.readLineKeys.Count -ne 77){throw 'Not all eight chapters and one event have durable original read records'}
        if($Journey){
            $draws=if($Exchange){100}else{10};$points=if($Exchange){0}else{10}
            if(@($snapshot.growth.heroines | Where-Object level -ne 30).Count -ne 0 -or $snapshot.home.weaponEquipment.Count -ne 5 -or $snapshot.home.furnitureInstances.Count -ne 3 -or $snapshot.home.furniturePlacements.Count -ne 3 -or $snapshot.collection.equipment.Count -ne 1 -or $snapshot.growth.totalKinderDraws -ne $draws -or $snapshot.growth.kinderPoints -ne $points -or $snapshot.engagement.activeSeconds -ne 60 -or $snapshot.engagement.lastLoginDay -le 0){throw 'Journey progression/economy states were not durably restored'}
            if($Exchange -and (@($snapshot.growth.receipts | Where-Object transactionId -in @('journey.exchange','journey.ticket')).Count -ne 2 -or @($snapshot.growth.tickets | Where-Object count -gt 0).Count -ne 0)){throw 'Exchange/ticket receipt was not durably restored'}
            if(-not (Test-Path -LiteralPath (Join-Path (Split-Path $save -Parent) 'journey.json'))){throw 'Journey milestone report missing'}
        }
        if($Telemetry){
            if($text -notmatch 'PLAN8_TELEMETRY_COMPLETE' -or $text -match 'PLAN8_TELEMETRY_INCOMPLETE'){throw 'Journey telemetry incomplete'}
            $events=@(Get-Content -LiteralPath (Join-Path $repo ('tmp/plan8-runs/'+$telemetryId+'/events.jsonl')) | ForEach-Object {$_|ConvertFrom-Json})
            if(@($events | Where-Object buildHash -ne $assembly).Count -ne 0 -or @($events | Where-Object runId -ne $telemetryId).Count -ne 0){throw 'Journey telemetry provenance mismatch'}
            $summary=[ordered]@{runId=$telemetryId;buildHash=$assembly;events=$events.Count;categories=@($events|Group-Object category|ForEach-Object {[ordered]@{category=$_.Name;events=$_.Count}});performanceMeasured=$false;humanTimingMeasured=$false}
            [IO.File]::WriteAllText((Join-Path $output ($case+'-telemetry-summary.json')),($summary|ConvertTo-Json -Depth 7),[Text.UTF8Encoding]::new($false))
        }
        if($case -ne 'new' -and (Get-FileHash -LiteralPath $save).Hash -ne $saveBefore){throw 'Replay/view process changed saved progress'}
        if($case -in $Views -and $text -notmatch ('PLAN8_STORY_VIEW_PASS '+$case+' mode=')){throw 'Requested trial view was not rendered'}
        $cases+=[ordered]@{case=$case;passed=$true;saveSha256=(Get-FileHash -LiteralPath $save).Hash;readChapters=$snapshot.world.readStoryIds.Count;readEvents=$snapshot.home.readEventIds.Count;readLines=$snapshot.home.readLineKeys.Count;replaySaveUnchanged=($case -ne 'new')}
        Write-Output "PLAN8_STORY_CASE_PASS $case"
    }
}finally{
    $after=Fingerprint
    [IO.File]::WriteAllText((Join-Path $output 'normal-save-before.json'),$before,[Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $output 'normal-save-after.json'),$after,[Text.UTF8Encoding]::new($false))
    if($before -ne $after){throw 'Normal save/files changed during story trial'}
}
$result=[ordered]@{schemaVersion=1;scope='original-story-transactions-and-rendered-replay; automated-not-human-playtest';passed=$true;journey=$Journey.IsPresent;exchange=$Exchange.IsPresent;telemetry=$Telemetry.IsPresent;runId=$id;assemblySha256=$assembly;resourceSha256=$resource;height=$Height;normalSaveUnchanged=$true;performanceMeasured=$false;cases=$cases}
[IO.File]::WriteAllText((Join-Path $output 'result.json'),($result | ConvertTo-Json -Depth 6)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
Write-Output "PLAN8_STORY_PLAYER_PASS $output"

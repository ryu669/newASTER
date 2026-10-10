param([string]$InputPath='tmp/plan11-10-team-diversity.json',[string]$Output='tmp/plan11-10-team-diversity-summary.json',[string]$FollowupPath='tmp/plan11-10-team-diversity.json.followup.json')
$ErrorActionPreference='Stop'
$data=Get-Content -LiteralPath $InputPath -Raw|ConvertFrom-Json
if(!$data.complete -or $data.legalParties -ne 2178 -or $data.caps -ne 0){throw 'Team matrix is incomplete or capped'}
$combat=Get-Content game/unity/Assets/Game/Resources/Combat/battle-plan11-7.json -Raw|ConvertFrom-Json
$initial=@($combat.formation)
$baseline=@($data.rows|Where-Object {$_.level -eq 50 -and $_.enemy -eq 'colossus.newborn-asteria'})
if($baseline.Count -ne 2178){throw 'Final-enemy combination coverage is incomplete'}
function Metrics($name,$rows){
    $rows=@($rows);$runs=($rows|Measure-Object seeds -Sum).Sum;$wins=($rows|Measure-Object wins -Sum).Sum
    [ordered]@{group=$name;parties=$rows.Count;runs=$runs;wins=$wins;winPercent=if($runs){[Math]::Round(100*$wins/$runs,2)}else{0};allSeedWins=@($rows|Where-Object {$_.wins -eq $_.seeds}).Count;zeroWins=@($rows|Where-Object {$_.wins -eq 0}).Count}
}
$groups=@(
    (Metrics 'all' $baseline),
    (Metrics 'no-initial-five' @($baseline|Where-Object {@($_.formation|Where-Object {$_ -in $initial}).Count -eq 0})),
    (Metrics 'no-healer-job' @($baseline|Where-Object {'job.healer' -notin $_.jobs})),
    (Metrics 'with-healer-job' @($baseline|Where-Object {'job.healer' -in $_.jobs})),
    (Metrics 'multiple-support-jobs' @($baseline|Where-Object {@($_.jobs|Where-Object {$_ -in @('job.artist','job.general','job.healer')}).Count -ge 2})),
    (Metrics 'no-initial-and-no-healer-job' @($baseline|Where-Object {'job.healer' -notin $_.jobs -and @($_.formation|Where-Object {$_ -in $initial}).Count -eq 0}))
)
$absence=@(foreach($hero in $combat.heroines){Metrics ('without-'+$hero.id) @($baseline|Where-Object {$hero.id -notin $_.formation})})
$representatives=@(foreach($key in @('no-initial-five','no-healer-job','multiple-support-jobs')){
    $eligible=@($baseline|Where-Object {if($key -eq 'no-initial-five'){@($_.formation|Where-Object {$_ -in $initial}).Count -eq 0}elseif($key -eq 'no-healer-job'){'job.healer' -notin $_.jobs}else{@($_.jobs|Where-Object {$_ -in @('job.artist','job.general','job.healer')}).Count -ge 2}})
    $best=$eligible|Where-Object {$_.wins -eq $_.seeds}|Sort-Object winningClock|Select-Object -First 1
    if($best){[ordered]@{category=$key;formation=$best.formation;names=@($best.formation|ForEach-Object {$id=$_;($combat.heroines|Where-Object {$_.id -eq $id}).name});wins=$best.wins;seeds=$best.seeds;meanWinningClock=$best.winningClock/$best.wins}}
})
$sample=@($data.rows|Where-Object {$_.level -ne 50 -or $_.enemy -ne 'colossus.newborn-asteria'})
$summary=[ordered]@{schemaVersion=1;date='2026-10-10';status='complete';runs=$data.runs;caps=$data.caps;groups=$groups;absence=$absence;representatives=$representatives;sampleGroups=@($sample|Group-Object level|ForEach-Object {Metrics ('level-'+$_.Name) $_.Group});limitations=@('Four deterministic seeds per case; descriptive comparison, not causal hero ranking.','Canonical formation order only. General slot effects and other permutations are not exhausted.','No healer-job does not prohibit self-healing or protection.','Additional-enemy sample intentionally includes observed best/worst parties and is not a random estimate for all2178.','Duplicate0, no gear; no production balance changes.');source=@{path=$InputPath;sha256=(Get-FileHash -LiteralPath $InputPath -Algorithm SHA256).Hash}}
if(Test-Path -LiteralPath $FollowupPath){
    $followup=Get-Content -LiteralPath $FollowupPath -Raw|ConvertFrom-Json
    if(!$followup.complete -or $followup.caps -ne 0){throw 'Team followup is incomplete or capped'}
    $summary.followup=[ordered]@{runs=$followup.runs;caps=$followup.caps;groups=@($followup.rows|Group-Object weaponTier|ForEach-Object {Metrics ('weapon-tier-'+$_.Name) $_.Group});source=@{path=$FollowupPath;sha256=(Get-FileHash -LiteralPath $FollowupPath -Algorithm SHA256).Hash}}
}
$summary|ConvertTo-Json -Depth 12|Set-Content -LiteralPath $Output -Encoding UTF8
$groups|ForEach-Object {[pscustomobject]$_}|Format-Table

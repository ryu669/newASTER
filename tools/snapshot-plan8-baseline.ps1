param([string]$Output,[string]$BaselineCommit='d83cc95a590641e7a3f83b33c0c28fb6ad3d57d2')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Output){$Output=Join-Path $repo 'tmp/plan8-baseline.json'}
$resolved=& git -C $repo rev-parse ($BaselineCommit+'^{commit}')
if($LASTEXITCODE -ne 0){throw 'Baseline commit does not resolve'}
& git -C $repo merge-base --is-ancestor $resolved origin/main
if($LASTEXITCODE -ne 0){throw 'Baseline must belong to main'}
$paths=@('game/unity/Assets/Game/Resources/Combat/battle-formal.json','game/unity/Assets/Game/Resources/Illustrations/battle-formal.json','game/unity/Assets/Game/Resources/Economy/kinder-trial.json','game/unity/Assets/Game/Resources/Economy/engagement-trial.json',
    'game/unity/Assets/Game/Scripts/Data/ColossusCombatCatalog.cs','game/unity/Assets/Game/Scripts/Data/CollectionContractFixture.cs','game/unity/Assets/Game/Scripts/Data/HomeExperienceFixture.cs','game/unity/Assets/Game/Scripts/Core/FormalCampaignJournal.cs','game/unity/Assets/Game/Scripts/Core/FormalProgression.cs','game/unity/Assets/Game/Scripts/Presentation/FormalGrowthView.cs')
$files=@()
foreach($relative in $paths){
    $path=Join-Path $repo $relative
    $blob=& git -C $repo rev-parse ($resolved+':'+$relative)
    if($LASTEXITCODE -ne 0){throw "Missing baseline source: $relative"}
    $workingBlob=& git -C $repo hash-object ('--path='+$relative) $path
    if($LASTEXITCODE -ne 0){throw "Cannot fingerprint working source: $relative"}
    $files+=[ordered]@{path=$relative;baselineGitBlob=$blob;workingGitBlob=$workingBlob;workingSha256=(Get-FileHash -LiteralPath $path).Hash}
}
$trees=@()
foreach($relative in @('game/unity/Assets/Game/Scripts/Core','game/unity/Assets/Game/Scripts/Data','game/unity/Assets/Game/Scripts/Presentation','game/unity/Assets/Game/Editor')){
    $tree=& git -C $repo rev-parse ($resolved+':'+$relative)
    if($LASTEXITCODE -ne 0){throw "Missing baseline tree: $relative"}
    $trees+=[ordered]@{path=$relative;baselineGitTree=$tree}
}
$player=@()
foreach($relative in @('game/Builds/playable/newASTER.exe','game/Builds/playable/newASTER_Data/Managed/Assembly-CSharp.dll','game/Builds/playable/newASTER_Data/resources.assets')){
    $path=Join-Path $repo $relative
    $player+=[ordered]@{path=$relative;present=(Test-Path -LiteralPath $path -PathType Leaf);sha256=$(if(Test-Path -LiteralPath $path -PathType Leaf){(Get-FileHash -LiteralPath $path).Hash}else{$null})}
}
$trialPath=Join-Path $repo 'game/unity/Assets/Game/Resources/Trial/plan8-baseline.json'
$trial=Get-Content -LiteralPath $trialPath -Raw | ConvertFrom-Json
$result=[ordered]@{schemaVersion=1;checkedAtUtc=[DateTime]::UtcNow.ToString('o');baselineSourceCommit=$resolved;scope='Source and local player identity snapshot, not a build/run/performance acceptance';sourceTrees=$trees;sources=$files;player=$player;trialDefinitionSha256=(Get-FileHash -LiteralPath $trialPath).Hash;trialDefinition=$trial;baselineSourcesChanged=(@($files | Where-Object {$_.baselineGitBlob -ne $_.workingGitBlob}).Count -gt 0);authoredContentReady=$false}
New-Item -ItemType Directory -Force -Path (Split-Path $Output -Parent) | Out-Null
[IO.File]::WriteAllText($Output,($result | ConvertTo-Json -Depth 10)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
Write-Output "PLAN8_BASELINE_SNAPSHOT $Output sources=$($files.Count) sourceTrees=$($trees.Count)"

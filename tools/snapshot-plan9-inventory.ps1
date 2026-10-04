param([switch]$Write)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$combat=Get-Content -LiteralPath (Join-Path $repo 'game/unity/Assets/Game/Resources/Combat/battle-formal.json') -Raw | ConvertFrom-Json
$art=Get-Content -LiteralPath (Join-Path $repo 'game/unity/Assets/Game/Resources/Illustrations/battle-formal.json') -Raw | ConvertFrom-Json
$world=Get-Content -LiteralPath (Join-Path $repo 'game/unity/Assets/Game/Scripts/Data/WorldCatalog.cs') -Raw
$items=[Collections.Generic.List[object]]::new()
foreach($hero in $combat.heroines){
 $heroArt=@($art.heroes | Where-Object heroineId -eq $hero.id)
 $items.Add([ordered]@{id=$hero.id;kind='heroine';name=$hero.name;owner='combat/art';dependencies=@($hero.jobId);state='functional';artState=if($heroArt.resourcePath){'candidate'}else{'missing'};acceptance='P9-05; unique art + 3 skills + chain + equipment';source='Combat/battle-formal.json'})
 foreach($chapter in 1..3){$items.Add([ordered]@{id=($hero.id+'.chapter.'+$chapter);kind='chapter';owner='story';dependencies=@($hero.id);state='formal-review-required';poemCount=6;acceptance='P9-06; original text + exact quotations + unlock + resume'})}
 foreach($event in 0..4){$items.Add([ordered]@{id=($hero.id+'.event.'+$event);kind='event';owner='story/art';dependencies=@($hero.id);eventKind=if($event -lt 3){'affinity'}else{'lover'};state='fixture-only';acceptance='P9-06; authored scene + conditions + expressions + replay'})}
}
$matches=[regex]::Matches($world,'new ColossusDefinition\("([^"]+)", "([^"]+)", (null|"[^"]+"), (\d+), (null|"[^"]+")')
foreach($match in $matches){
 $id=$match.Groups[1].Value
 $items.Add([ordered]@{id=$id;kind='colossus';name=$match.Groups[2].Value;owner='combat/world/art';world=$match.Groups[3].Value.Trim('"');order=[int]$match.Groups[4].Value;dependencies=@($match.Groups[5].Value.Trim('"'));state=if($id -eq 'colossus.green-return-dragon'){'functional-candidate'}else{'catalog-only'};acceptance='P9-05; unique battle + 4-6 parts + major + level45 skill + drops';source='Scripts/Data/WorldCatalog.cs'})
 foreach($chapter in 1..3){$items.Add([ordered]@{id=($id+'.chapter.'+$chapter);kind='chapter';owner='story';dependencies=@($id);state='formal-review-required';poemCount=8;acceptance='P9-06; original text + exact quotations + unlock + resume'})}
}
$jobs=@('fighter','defender','chaser','berserker','gunner','sniper','blaster','healer','artist','gambler','general','alchemist','panzer')
foreach($chapter in @($items | Where-Object kind -eq 'chapter')){
 foreach($poem in 1..$chapter.poemCount){$items.Add([ordered]@{id=($chapter.id+'.poem.'+$poem);kind='poem';owner='story';dependencies=@($chapter.id);managementOnly=$true;state='formal-quotation-review-required';acceptance='P9-06; exact text reference and explicit heroine links'})}
}
foreach($event in @($items | Where-Object kind -eq 'event')){
 $items.Add([ordered]@{id=($event.id+'.cg');kind='planned-cg';owner='art';dependencies=@($event.id);managementOnly=$true;state='unproduced';acceptance='P9-01; matching authored event + visual/source review'})
}
foreach($job in $jobs){$id='job.'+$job;$present=@($combat.jobs | Where-Object id -eq $id).Count -gt 0;$items.Add([ordered]@{id=$id;kind='job';owner='combat';dependencies=@();state=if($present){'initial-roster-functional'}else{'unimplemented-in-formal-combat'};acceptance='job-specific resource/rule + skills + tests';source='Combat/battle-formal.json'})}
foreach($n in 1..7){$items.Add([ordered]@{id=('W{0:00}' -f $n);kind='world';owner='world/art';dependencies=@();state='catalog-only';acceptance='background/environment/furniture mapping; P9-07'})}
$resources=Join-Path $repo 'game/unity/Assets/Game/Resources'
foreach($file in (Get-ChildItem -LiteralPath $resources -Recurse -File | Where-Object Extension -in @('.png','.wav','.ogg'))){$items.Add([ordered]@{id=$file.BaseName;kind='asset';owner='art/audio';dependencies=@();state='candidate-review-required';path=$file.FullName.Substring($repo.Length+1).Replace('\','/');sha256=(Get-FileHash -LiteralPath $file.FullName).Hash;acceptance='P9-01; rights/source/hash/visual-review'})}
$result=[ordered]@{schemaVersion=1;generatedOn='2026-10-04';status='inventory-not-release-approval';counts=[ordered]@{heroines=$combat.heroines.Count;colossi=$matches.Count;chapters=@($items|Where-Object kind -eq 'chapter').Count;poems=@($items|Where-Object kind -eq 'poem').Count;events=@($items|Where-Object kind -eq 'event').Count;jobs=13};items=$items.ToArray()}
if($result.counts.heroines -ne 5 -or $result.counts.colossi -ne 15 -or $result.counts.chapters -ne 60 -or $result.counts.events -ne 25){throw 'Plan9 scope mismatch'}
if($result.counts.poems -ne 450){throw 'Plan9 poem count mismatch'}
if($Write){$result | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $repo 'docs/production/plan9-content-inventory.json') -Encoding utf8}
'PLAN9_INVENTORY_PASS heroes=5 colossi=15 chapters=60 events=25 jobs=13 assets='+@($items|Where-Object kind -eq 'asset').Count

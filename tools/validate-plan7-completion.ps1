param([string]$Ledger,[string]$Output,[switch]$ReportOnly)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Ledger){$Ledger=Join-Path $repo 'docs/production/plan7-candidate-assets.json'}
if(-not $Output){$Output=Join-Path $repo 'tmp/plan7-completion-audit.json'}
$data=Get-Content -LiteralPath $Ledger -Raw | ConvertFrom-Json
$problems=[Collections.Generic.List[object]]::new()
$images=@();$audio=@()
function Add-Problem([string]$Gate,[string]$Id,[string]$Reason){$problems.Add([ordered]@{gate=$Gate;assetId=$Id;reason=$Reason})}
function Resolve-Asset([string]$Relative){
    $path=[IO.Path]::GetFullPath((Join-Path $repo $Relative))
    if(-not $path.StartsWith($repo+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Asset path must stay inside repository'}
    return $path
}
function Read-BigEndian([byte[]]$Bytes,[int]$Offset){return [long]$Bytes[$Offset]*16777216+[long]$Bytes[$Offset+1]*65536+[long]$Bytes[$Offset+2]*256+$Bytes[$Offset+3]}
foreach($asset in $data.art){
    $path=Resolve-Asset $asset.path
    if(-not(Test-Path -LiteralPath $path -PathType Leaf)){Add-Problem 'integrity' $asset.assetId 'PNG missing';continue}
    $bytes=[IO.File]::ReadAllBytes($path)
    if($bytes.Length -lt 33 -or [Convert]::ToHexString($bytes[0..7]) -ne '89504E470D0A1A0A' -or [Text.Encoding]::ASCII.GetString($bytes,12,4) -ne 'IHDR'){Add-Problem 'integrity' $asset.assetId 'Invalid PNG header';continue}
    $width=Read-BigEndian $bytes 16;$height=Read-BigEndian $bytes 20
    $hash=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
    if($hash -ne $asset.sha256 -or $width -ne $asset.width -or $height -ne $asset.height){Add-Problem 'integrity' $asset.assetId 'File differs from ledger hash or dimensions'}
    $minimum=$null
    if($asset.usage -in @('background','middle','foreground','cg')){$minimum=@(1920,1080)}
    if($asset.usage -eq 'standing'){$minimum=@(1600,2400)}
    if($minimum -and ($width -lt $minimum[0] -or $height -lt $minimum[1])){Add-Problem 'nativeDimensions' $asset.assetId "Actual ${width}x${height}; target $($minimum[0])x$($minimum[1]). Upscaled exports are not proof of new native detail."}
    if(-not $asset.editableLayerSource){Add-Problem 'paintingSource' $asset.assetId 'Original painting layers are not delivered; composition SVG is a separate artifact'}
    elseif(-not $asset.paintingSourcePath -or -not(Test-Path -LiteralPath (Resolve-Asset $asset.paintingSourcePath) -PathType Leaf)){Add-Problem 'paintingSource' $asset.assetId 'Layer source marked delivered without a source file'}
    if($asset.adoption -ne 'accepted'){Add-Problem 'artQuality' $asset.assetId 'Candidate quality remains unaccepted; pixel/hash checks do not certify face, hands, clothing or silhouette'}
    $images+=[ordered]@{assetId=$asset.assetId;path=$asset.path;width=$width;height=$height;sha256=$hash;usage=$asset.usage}
}
foreach($asset in $data.audio){
    $path=Resolve-Asset $asset.path
    if(-not(Test-Path -LiteralPath $path -PathType Leaf)){Add-Problem 'integrity' $asset.assetId 'WAV missing';continue}
    $hash=(Get-FileHash -LiteralPath $path).Hash
    if($hash -ne $asset.sha256){Add-Problem 'integrity' $asset.assetId 'WAV differs from ledger hash'}
    if($asset.listeningAcceptance -ne 'passed'){Add-Problem 'listening' $asset.assetId 'Loop seam and effect clarity have no listening acceptance record; waveform tests are separate'}
    $audio+=[ordered]@{assetId=$asset.assetId;path=$asset.path;sha256=$hash;listeningAcceptance=$asset.listeningAcceptance}
}
if($images.Count -ne 24 -or $audio.Count -ne 6){Add-Problem 'integrity' 'sample-set' 'Plan7 sample requires 24 current images and six audio clips'}
# Historical active work hours cannot be reconstructed from generation/build waits.
if(-not $data.productionTimeEvidencePath -or -not(Test-Path -LiteralPath (Resolve-Asset $data.productionTimeEvidencePath) -PathType Leaf)){
    Add-Problem 'productionTime' 'Q6' 'Person, enemy and formal event active-work records H/E/V are not available; quantity estimate exists, duration estimate remains unverified'
}else{
    $work=Get-Content -LiteralPath (Resolve-Asset $data.productionTimeEvidencePath) -Raw | ConvertFrom-Json
    foreach($kind in @('person','enemy','formalEvent')){
        $units=@($work.units | Where-Object kind -eq $kind)
        if($units.Count -ne 1 -or $units[0].status -ne 'complete' -or $units[0].coverage -ne 'complete-unit-production'){
            Add-Problem 'productionTime' $kind 'A complete unit production record is required; a single revision or fixture event is insufficient';continue
        }
        $minutes=0.0;$lastEnd=$null
        foreach($entry in $units[0].activities){
            try{$start=[DateTimeOffset]::Parse($entry.startedUtc);$end=[DateTimeOffset]::Parse($entry.endedUtc)}catch{Add-Problem 'productionTime' $kind 'Work interval has invalid timestamps';continue}
            if(($end -le $start) -or ($null -ne $lastEnd -and $start -lt $lastEnd)){Add-Problem 'productionTime' $kind 'Work intervals must be positive and ordered without overlap';continue}
            $lastEnd=$end
            if($entry.kind -eq 'active-work' -and $entry.actor -in @('human','agent')){$minutes+=($end-$start).TotalMinutes}
            elseif($entry.kind -ne 'waiting'){Add-Problem 'productionTime' $kind 'Activity must distinguish attributed active work from waiting'}
        }
        if($minutes -le 0){Add-Problem 'productionTime' $kind 'No attributed active work was recorded; build/generation waits are excluded'}
    }
}
$gates=@('integrity','nativeDimensions','paintingSource','artQuality','listening','productionTime') | ForEach-Object {
    $items=@($problems | Where-Object gate -eq $_)
    [ordered]@{id=$_;status=$(if($items.Count){'incomplete'}else{'passed'});issues=$items.Count}
}
$result=[ordered]@{schemaVersion=1;checkedAtUtc=[DateTime]::UtcNow.ToString('o');complete=($problems.Count -eq 0);scope='Read-only material delivery checks. Does not re-run or certify Q1-Q5 functional/visual acceptance, artistic judgement, cold load or human work hours.';gates=@($gates);issues=@($problems.ToArray());art=@($images);audio=@($audio);functionalEvidence=@('docs/production/plan7-crown.md','docs/production/plan7-full-combat.md','docs/production/plan7-validation.md');requiredApproval=$false}
New-Item -ItemType Directory -Force -Path (Split-Path $Output -Parent) | Out-Null
[IO.File]::WriteAllText($Output,($result | ConvertTo-Json -Depth 10)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
foreach($gate in $gates){Write-Output "PLAN7_COMPLETION_GATE $($gate.id) $($gate.status) issues=$($gate.issues)"}
Write-Output "PLAN7_COMPLETION_REPORT $Output"
if(-not $result.complete -and -not $ReportOnly){exit 2}

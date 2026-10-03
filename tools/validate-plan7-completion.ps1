param([string]$Ledger,[string]$Output,[switch]$ReportOnly)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Ledger){$Ledger=Join-Path $repo 'docs/production/plan7-candidate-assets.json'}
if(-not $Output){$Output=Join-Path $repo 'tmp/plan7-completion-audit.json'}
$data=Get-Content -LiteralPath $Ledger -Raw | ConvertFrom-Json
$policy=Get-Content -LiteralPath (Join-Path $repo 'docs/production/plan7-completion-policy.json') -Raw | ConvertFrom-Json
$problems=[Collections.Generic.List[object]]::new()
$images=@();$audio=@()
function Add-Problem([string]$Gate,[string]$Id,[string]$Reason){$problems.Add([ordered]@{gate=$Gate;assetId=$Id;reason=$Reason})}
function Resolve-Asset([string]$Relative){
    $path=[IO.Path]::GetFullPath((Join-Path $repo $Relative))
    if(-not $path.StartsWith($repo+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Asset path must stay inside repository'}
    return $path
}
function Get-EvidenceHash([string]$Path){
    if([IO.Path]::GetExtension($Path) -in @('.md','.json','.svg','.cs','.ps1','.txt')){
        $text=[IO.File]::ReadAllText($Path).Replace("`r`n","`n")
        return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text)))
    }
    return (Get-FileHash -LiteralPath $Path).Hash
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
    $minimum=$policy.minimums.($asset.usage)
    if(-not $minimum){$minimum=$policy.minimums.default}
    if($width -lt $minimum[0] -or $height -lt $minimum[1]){Add-Problem 'displayDimensions' $asset.assetId "Actual ${width}x${height}; development minimum $($minimum[0])x$($minimum[1])"}
    if(-not $asset.sourceRecord -or -not(Test-Path -LiteralPath (Resolve-Asset $asset.sourceRecord) -PathType Leaf)){Add-Problem 'editableSources' $asset.assetId 'PNG provenance/prompt record missing'}
    $images+=[ordered]@{assetId=$asset.assetId;path=$asset.path;width=$width;height=$height;sha256=$hash;usage=$asset.usage}
}
foreach($asset in $data.audio){
    $path=Resolve-Asset $asset.path
    if(-not(Test-Path -LiteralPath $path -PathType Leaf)){Add-Problem 'integrity' $asset.assetId 'WAV missing';continue}
    $hash=(Get-FileHash -LiteralPath $path).Hash
    if($hash -ne $asset.sha256){Add-Problem 'integrity' $asset.assetId 'WAV differs from ledger hash'}
    # Inspect actual WAV chunks and samples; ledger values alone cannot certify PCM safety.
    $bytes=[IO.File]::ReadAllBytes($path);$pcm=$null;$formatOk=$false
    if($bytes.Length -lt 12 -or [Text.Encoding]::ASCII.GetString($bytes,0,4) -ne 'RIFF' -or [Text.Encoding]::ASCII.GetString($bytes,8,4) -ne 'WAVE'){
        Add-Problem 'audioTechnical' $asset.assetId 'Invalid WAV header'
    }else{
        $offset=12
        while($offset+8 -le $bytes.Length){
            $tag=[Text.Encoding]::ASCII.GetString($bytes,$offset,4);$size=[BitConverter]::ToUInt32($bytes,$offset+4);$start=$offset+8
            if($start+$size -gt $bytes.Length){Add-Problem 'audioTechnical' $asset.assetId 'Truncated WAV chunk';break}
            if($tag -eq 'fmt ' -and $size -ge 16){$formatOk=([BitConverter]::ToUInt16($bytes,$start) -eq 1 -and [BitConverter]::ToUInt16($bytes,$start+2) -eq 1 -and [BitConverter]::ToUInt32($bytes,$start+4) -eq 44100 -and [BitConverter]::ToUInt16($bytes,$start+14) -eq 16)}
            if($tag -eq 'data' -and $size -ge 2){$pcm=@{start=$start;size=$size}}
            $offset=$start+$size+($size%2)
        }
        if(-not $formatOk -or -not $pcm -or $pcm.size%2){Add-Problem 'audioTechnical' $asset.assetId 'Expected nonempty 44100Hz mono 16-bit PCM'}
        else{
            $peak=0
            for($i=$pcm.start;$i -lt $pcm.start+$pcm.size;$i+=2){$peak=[Math]::Max($peak,[Math]::Abs([int][BitConverter]::ToInt16($bytes,$i)))}
            if($peak -eq 0 -or $peak -ge 32767 -or [BitConverter]::ToInt16($bytes,$pcm.start) -ne 0 -or [BitConverter]::ToInt16($bytes,$pcm.start+$pcm.size-2) -ne 0){Add-Problem 'audioTechnical' $asset.assetId 'Silent/clipped PCM or nonzero endpoints'}
        }
    }
    $audio+=[ordered]@{assetId=$asset.assetId;path=$asset.path;sha256=$hash;listeningAcceptance=$asset.listeningAcceptance}
}
if($images.Count -ne 24 -or $audio.Count -ne 6){Add-Problem 'integrity' 'sample-set' 'Plan7 sample requires 24 current images and six audio clips'}
# This revision accepts a development sample, while preserving final candidate/listening flags.
if($policy.schemaVersion -ne 2 -or $policy.acceptance -ne 'development-sample' -or $policy.review.decision -ne 'usable-for-game-development'){
    Add-Problem 'sampleReview' 'policy' 'Development scope and explicit review decision required'
}
if((Get-EvidenceHash $Ledger) -ne $policy.ledgerSha256){Add-Problem 'integrity' 'sample-set' 'Ledger changed since reviewed sample; review and evidence must be updated'}
$svgCount=0
foreach($record in $policy.evidence){
    $path=Resolve-Asset $record.path
    if(-not(Test-Path -LiteralPath $path -PathType Leaf)){Add-Problem 'evidence' $record.path 'Source/evidence file missing';continue}
    if((Get-EvidenceHash $path) -ne $record.sha256){Add-Problem 'evidence' $record.path 'Source/evidence changed since review'}
    if([IO.Path]::GetExtension($path) -eq '.svg'){
        $svgCount++
        try{
            [xml]$svg=Get-Content -LiteralPath $path -Raw
            foreach($node in $svg.SelectNodes("//*[local-name()='image']")){
                $href=$node.GetAttribute('href','http://www.w3.org/1999/xlink')
                if(-not $href){$href=$node.GetAttribute('href')}
                $reference=[IO.Path]::GetFullPath((Join-Path (Split-Path $path -Parent) $href))
                if(-not $reference.StartsWith($repo+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase) -or -not(Test-Path -LiteralPath $reference -PathType Leaf)){Add-Problem 'editableSources' $record.path 'SVG image reference missing/outside repository'}
            }
        }catch{Add-Problem 'editableSources' $record.path 'Invalid composition SVG'}
    }
}
if($svgCount -ne 8){Add-Problem 'editableSources' 'composition-set' 'Eight editable composition sources required'}
$functionalPath=Join-Path $repo 'docs/production/plan7-completion-functional-evidence.json'
$functional=Get-Content -LiteralPath $functionalPath -Raw | ConvertFrom-Json
$markers=$functional.markers -join "`n"
foreach($required in @('PLAN7_SAMPLE_ASSETS_PASS 24 textures / 6 audio clips / Japanese glyphs','PLAN7_FULL_COMBAT_PASS commands=32 repaintedEvents=66 victory=True queueEmpty=True rewardSignatureMatches=True saveUnchanged=True playbackRate=1','PLAN7_PLAYBACK_EQUIVALENCE_PASS modes=4')){
    if(-not $markers.Contains($required)){Add-Problem 'functionalEvidence' 'battle' "Required prior-run marker absent: $required"}
}
foreach($mode in @('normal','shortened','skip','paused')){if(-not $markers.Contains("PLAN7_PLAYBACK_MODE_PASS $mode ")){Add-Problem 'functionalEvidence' $mode 'Replay comparison absent'}}
$performance=@($functional.markers | Where-Object {$_ -match '^PLAN7_PERFORMANCE '})
if($performance.Count -ne 1 -or $performance[0] -notmatch 'under16_7ms=([0-9.]+)' -or [double]::Parse($Matches[1],[Globalization.CultureInfo]::InvariantCulture) -lt 0.95 -or $functional.build.uncapped -or -not $functional.build.completeBattle){Add-Problem 'functionalEvidence' 'performance' 'Normal-speed complete battle must meet 95% frame target'}
# If the local build exists, ensure that the recorded run still describes that build.
$localBuildStatus='not-present; historical build identity retained'
$buildFiles=@{assemblySha256='game/Builds/playable/newASTER_Data/Managed/Assembly-CSharp.dll';resourceAssetsSha256='game/Builds/playable/newASTER_Data/resources.assets'}
foreach($key in $buildFiles.Keys){
    $path=Resolve-Asset $buildFiles[$key]
    if(Test-Path -LiteralPath $path -PathType Leaf){
        $localBuildStatus='checked against recorded build'
        if((Get-FileHash -LiteralPath $path).Hash -ne $functional.build.$key){Add-Problem 'functionalEvidence' $key 'Local build differs from previously tested build'}
    }
}
$gates=@('integrity','displayDimensions','editableSources','audioTechnical','sampleReview','evidence','functionalEvidence') | ForEach-Object {
    $items=@($problems | Where-Object gate -eq $_)
    [ordered]@{id=$_;status=$(if($items.Count){'incomplete'}else{'passed'});issues=$items.Count}
}
$result=[ordered]@{schemaVersion=2;policyRevision=$policy.revision;checkedAtUtc=[DateTime]::UtcNow.ToString('o');complete=($problems.Count -eq 0);scope=$policy.scope;finalReleaseAccepted=$false;gates=@($gates);issues=@($problems.ToArray());art=@($images);audio=@($audio);evidence=@($policy.evidence);localBuildStatus=$localBuildStatus;deferred=@($policy.deferred);requiredApproval=$false}
New-Item -ItemType Directory -Force -Path (Split-Path $Output -Parent) | Out-Null
[IO.File]::WriteAllText($Output,($result | ConvertTo-Json -Depth 10)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
foreach($gate in $gates){Write-Output "PLAN7_COMPLETION_GATE $($gate.id) $($gate.status) issues=$($gate.issues)"}
Write-Output "PLAN7_COMPLETION_REPORT $Output"
if(-not $result.complete -and -not $ReportOnly){exit 2}

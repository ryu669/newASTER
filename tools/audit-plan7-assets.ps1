param([string]$Output)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Output){$Output=Join-Path $repo 'docs/production/plan7-candidate-assets.json'}
Add-Type -AssemblyName System.Drawing
$definitions=@(
    @('slayer-standing','standing','heroine.slayer','slayer-expression'),
    @('slayer-expression-joy','expression','heroine.slayer','slayer-expression'),
    @('slayer-expression-puzzled','expression','heroine.slayer','slayer-expression'),
    @('slayer-expression-determined','expression','heroine.slayer','slayer-expression'),
    @('slayer-attack','attack','heroine.slayer','slayer-battle'),
    @('slayer-hit','hit','heroine.slayer','slayer-battle'),
    @('slayer-cutin','cutin','heroine.slayer','slayer-battle'),
    @('green-body','body','colossus.green-return-dragon','green-parts'),
    @('green-crown','crystal-horn-crown','colossus.green-return-dragon','green-parts'),
    @('green-wing-left','left-wing-root','colossus.green-return-dragon','green-parts'),
    @('green-wing-right','right-wing-root','colossus.green-return-dragon','green-parts'),
    @('green-tail','vine-wrapped-tail','colossus.green-return-dragon','green-parts'),
    @('green-major','major','colossus.green-return-dragon','green-major'),
    @('forest-far','background','garden.grassland-forest','forest'),
    @('forest-mid','middle','garden.grassland-forest','forest'),
    @('forest-front','foreground','garden.grassland-forest','forest'),
    @('garden-bench','furniture','furniture.fixture.0','garden-props'),
    @('garden-desk','furniture','furniture.fixture.1','garden-props'),
    @('garden-fountain','furniture','furniture.fixture.2','garden-props'),
    @('slayer-sd-idle','idle','heroine.slayer','slayer-sd'),
    @('slayer-sd-sit','sit','heroine.slayer','slayer-sd'),
    @('slayer-sd-work','work','heroine.slayer','slayer-sd'),
    @('slayer-sd-look','look','heroine.slayer','slayer-sd'),
    @('slayer-garden-cg','cg','heroine.slayer','slayer-cg'))
$assets=@()
foreach($d in $definitions){
    $version=if($d[0] -in @('slayer-cutin','green-major')){'v3'}else{'v1'}
    $resource='Illustrations/'+$d[0]+'-candidate-'+$version
    $relative='game/unity/Assets/Game/Resources/'+$resource+'.png'
    $path=Join-Path $repo $relative
    $assetId='art.candidate.'+$d[0]+'.'+$version
    if($d[0] -eq 'slayer-standing'){$assetId='art.candidate.slayer.standing.v1'}
    if($d[0].StartsWith('slayer-expression-')){$assetId='art.candidate.slayer.expression.'+$d[0].Substring(18)+'.v1'}
    if($d[1] -eq 'furniture'){$assetId='art.candidate.furniture.'+$d[0].Substring(7)+'.v1'}
    $bitmap=[Drawing.Bitmap]::new($path)
    try{
        $edge=0;$transparent=0
        for($x=0;$x -lt $bitmap.Width;$x++){foreach($y in @(0,($bitmap.Height-1))){$a=$bitmap.GetPixel($x,$y).A;if($a -gt 16){$edge++};if($a -eq 0){$transparent++}}}
        for($y=1;$y -lt $bitmap.Height-1;$y++){foreach($x in @(0,($bitmap.Width-1))){$a=$bitmap.GetPixel($x,$y).A;if($a -gt 16){$edge++};if($a -eq 0){$transparent++}}}
        $assets+=[ordered]@{assetId=$assetId;resourcePath=$resource;ownerId=$d[2];usage=$d[1];canvasGroupId=$d[3];width=$bitmap.Width;height=$bitmap.Height;renderAnchor='center / ScaleToFit; garden authored slots in HomeExperienceFixture';edgeAlphaAbove16=$edge;transparentBorderPixels=$transparent;sha256=(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash;path=$relative;creator='OpenAI imagegen / project-directed original candidate';created='2026-10-03';sourceRecord='docs/production/plan7-art-prompts.md';usageTerms='Original generated project asset; candidate, no third-party extraction';adoption='candidate';editableLayerSource=$false}
    }finally{$bitmap.Dispose()}
}
$audio=@()
foreach($name in @('hit','shield','heal','break','victory','bgm')){
    $relative='game/unity/Assets/Game/Resources/Audio/candidate-'+$name+'-v2.wav'
    $path=Join-Path $repo $relative;$bytes=[IO.File]::ReadAllBytes($path)
    if([Text.Encoding]::ASCII.GetString($bytes,0,4) -ne 'RIFF' -or [BitConverter]::ToInt16($bytes,20) -ne 1 -or [BitConverter]::ToInt16($bytes,34) -ne 16){throw "Unexpected PCM format: $path"}
    $peak=0;for($i=44;$i -lt $bytes.Length;$i+=2){$peak=[Math]::Max($peak,[Math]::Abs([int][BitConverter]::ToInt16($bytes,$i)))}
    if($peak -eq 0 -or $peak -ge 32767){throw "Silent or clipped audio: $path"}
    $audio+=[ordered]@{assetId=('audio.candidate.'+$name+'.v2');resourcePath=('Audio/candidate-'+$name+'-v2');path=$relative;sampleRate=[BitConverter]::ToInt32($bytes,24);channels=[BitConverter]::ToInt16($bytes,22);bits=16;seconds=($bytes.Length-44)/88200.0;peak=$peak;firstSample=[BitConverter]::ToInt16($bytes,44);lastSample=[BitConverter]::ToInt16($bytes,$bytes.Length-2);sha256=(Get-FileHash -LiteralPath $path).Hash;creator='Original procedural score / tools/Plan7AudioSource.cs';created='2026-10-03';adoption='candidate';listeningAcceptance='pending'}
}
$result=[ordered]@{schemaVersion=1;status='candidate';art=@($assets);audio=@($audio);limitations=@('Background/CG 1672x941 and standing 1024x1536 fall below requested delivery dimensions.','Nonzero alpha at canvas edges is an inspection flag, not automatic proof of cropping.','Generation originals and prompts retained; layered editable source not delivered.','Art direction, consistent expression geometry, sound listening and formal adoption pending.')}
[IO.File]::WriteAllText($Output,($result | ConvertTo-Json -Depth 10)+[Environment]::NewLine)
Write-Output "PLAN7_ASSET_AUDIT_PASS $($assets.Count) images / $($audio.Count) unclipped PCM clips / $Output"

param()
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo 'game/art-source/2d'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$utf8=New-Object Text.UTF8Encoding($false)
function Write-Composition([string]$Name,[int]$Width,[int]$Height,[object[]]$Layers,[string]$Defs=''){
    $content=@('<?xml version="1.0" encoding="UTF-8"?>',('<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" width="'+$Width+'" height="'+$Height+'" viewBox="0 0 '+$Width+' '+$Height+'">'),'<title>newASTER candidate composition</title>','<desc>Editable composition layers referencing original PNG pixels. Not original painting layers or a higher resolution delivery.</desc>')
    if($Defs){$content+='<defs>'+$Defs+'</defs>'}
    foreach($layer in $Layers){
        $relative='../../unity/Assets/Game/Resources/'+$layer.Resource+'.png'
        $source=Join-Path $output $relative
        if(-not(Test-Path -LiteralPath $source)){throw "Missing source: $source"}
        $bytes=[IO.File]::ReadAllBytes($source)
        $actualWidth=[int]([uint32]$bytes[16]*16777216+[uint32]$bytes[17]*65536+[uint32]$bytes[18]*256+$bytes[19])
        $actualHeight=[int]([uint32]$bytes[20]*16777216+[uint32]$bytes[21]*65536+[uint32]$bytes[22]*256+$bytes[23])
        if($actualWidth -ne $Width -or $actualHeight -ne $Height){throw "Source dimension mismatch: $source"}
        $clip=if($layer.Clip){' clip-path="url(#face-region)"'}else{''}
        $content+='<g id="'+$layer.Id+'" inkscape:groupmode="layer" inkscape:label="'+$layer.Id+'"'+$clip+'><image x="0" y="0" width="'+$Width+'" height="'+$Height+'" xlink:href="'+$relative+'" /></g>'
    }
    $content+='</svg>'
    $path=Join-Path $output ($Name+'.svg')
    [IO.File]::WriteAllText($path,($content -join "`n")+"`n",$utf8)
    [xml]$document=Get-Content -LiteralPath $path -Raw
    if($document.svg.g.Count -ne $Layers.Count){throw "Layer count mismatch: $Name"}
    Write-Output "PLAN7_COMPOSITION_SOURCE_PASS $Name layers=$($Layers.Count) ${Width}x${Height}"
}
$manifest=Get-Content (Join-Path $repo 'game/unity/Assets/Game/Resources/Illustrations/battle-formal.json') -Raw | ConvertFrom-Json
$layers=@()
foreach($part in ($manifest.parts | Where-Object {$_.drawOrder -lt 0} | Sort-Object drawOrder,partId)){$layers+=@{Id=$part.partId;Resource=$part.resourcePath}}
$layers+=@{Id='body';Resource=$manifest.bodyResourcePath}
foreach($part in ($manifest.parts | Where-Object {$_.drawOrder -ge 0} | Sort-Object drawOrder,partId)){$layers+=@{Id=$part.partId;Resource=$part.resourcePath}}
Write-Composition 'green-colossus-candidate-v1' 1254 1254 $layers
Write-Composition 'forest-candidate-v1' 1672 941 @(@{Id='far';Resource=$manifest.backgroundResourcePath},@{Id='middle';Resource=$manifest.middleResourcePath},@{Id='front';Resource=$manifest.foregroundResourcePath})
foreach($expression in @('joy','puzzled','determined')){
    Write-Composition ('slayer-expression-'+$expression+'-candidate-v1') 1024 1536 @(@{Id='standing';Resource='Illustrations/slayer-standing-candidate-v1'},@{Id='face-'+$expression;Resource='Illustrations/slayer-expression-'+$expression+'-candidate-v1';Clip=$true}) '<clipPath id="face-region"><rect x="435" y="178" width="111" height="98" /></clipPath>'
}

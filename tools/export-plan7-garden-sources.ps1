param()
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo 'game/art-source/2d'
$source=Get-Content (Join-Path $repo 'game/unity/Assets/Game/Scripts/Presentation/GardenArtComposition.cs') -Raw
$culture=[Globalization.CultureInfo]::InvariantCulture
function Number([string]$Value){return [double]::Parse($Value.Trim().TrimEnd('f'),$culture)}
function Format([double]$Value){return $Value.ToString('0.######',$culture)}
function Paths([string]$Code){
    $paths=@()
    foreach($match in [regex]::Matches($Code,'GardenPolygon\(([^)]+)\)')){
        $values=@($match.Groups[1].Value.Split(',') | ForEach-Object {Number $_})
        if($values.Count -lt 6 -or $values.Count%2 -ne 0 -or @($values | Where-Object {$_ -lt 0 -or $_ -gt 1}).Count -gt 0){throw 'Invalid authored garden polygon'}
        $path='M'+(Format ($values[0]*1254))+','+(Format ($values[1]*1254))
        for($i=2;$i -lt $values.Count;$i+=2){$path+=' L'+(Format ($values[$i]*1254))+','+(Format ($values[$i+1]*1254))}
        $paths+=$path+' Z'
    }
    return $paths
}
function Masks([string]$Id,[string[]]$Paths){
    $front='<mask id="'+$Id+'-front" maskUnits="userSpaceOnUse" x="0" y="0" width="1254" height="1254"><rect width="1254" height="1254" fill="black" />'
    $back='<mask id="'+$Id+'-back" maskUnits="userSpaceOnUse" x="0" y="0" width="1254" height="1254"><rect width="1254" height="1254" fill="white" />'
    foreach($path in $Paths){$front+='<path d="'+$path+'" fill="white" />';$back+='<path d="'+$path+'" fill="black" />'}
    return $front+'</mask>'+$back+'</mask>'
}
function Layer([string]$Id,[string]$Resource,[string]$Mask,[string]$Transform=''){
    $relative='../../unity/Assets/Game/Resources/Illustrations/'+$Resource+'-candidate-v1.png'
    $original=Join-Path $output $relative
    if(-not(Test-Path -LiteralPath $original)){throw "Missing original PNG: $relative"}
    $header=[IO.File]::ReadAllBytes($original)
    if($header.Length -lt 24 -or [BitConverter]::ToString($header,0,8) -ne '89-50-4E-47-0D-0A-1A-0A'){throw "Invalid PNG: $relative"}
    $pngWidth=([uint32]$header[16]*16777216)+([uint32]$header[17]*65536)+([uint32]$header[18]*256)+$header[19]
    $pngHeight=([uint32]$header[20]*16777216)+([uint32]$header[21]*65536)+([uint32]$header[22]*256)+$header[23]
    if($pngWidth -ne 1254 -or $pngHeight -ne 1254){throw "Original canvas changed: $relative ${pngWidth}x${pngHeight}"}
    $text='<g id="'+$Id+'" inkscape:groupmode="layer" inkscape:label="'+$Id+'"'
    if($Transform){$text+=' transform="'+$Transform+'"'}
    $text+='><g';if($Mask){$text+=' mask="url(#'+$Mask+')"'}
    return $text+'><image x="0" y="0" width="1254" height="1254" xlink:href="'+$relative+'" /></g></g>'
}
$records=@()
$entries=[regex]::Matches($source,'new GardenArtUse\{furnitureId="([^"\r\n]+)",action="([^"\r\n]+)"(?<body>.*?)(?=\r?\n\s*new GardenArtUse|\r?\n\s*\};)',[Text.RegularExpressions.RegexOptions]::Singleline)
if($entries.Count -ne 3){throw 'Garden authoring schema changed; expected three authored furniture uses'}
foreach($entry in $entries){
    $action=$entry.Groups[2].Value;$body=$entry.Groups['body'].Value
    $contact=[regex]::Match($body,'contact=new Vector2\(([^,]+),([^)]+)\),actorContact=new Vector2\(([^,]+),([^)]+)\),actorScale=([^,\r\n}]+)')
    if(-not $contact.Success){throw "Missing authored contacts: $action"}
    $cx=Number $contact.Groups[1].Value;$cy=Number $contact.Groups[2].Value;$ax=Number $contact.Groups[3].Value;$ay=Number $contact.Groups[4].Value;$scale=Number $contact.Groups[5].Value
    $split=$body -split 'hands=new\[\]',2
    $front=@(Paths $split[0]);$hands=if($split.Count -eq 2){@(Paths $split[1])}else{@()}
    $actorX=1254*($cx-$ax*$scale);$actorY=1254*($cy-$ay*$scale)
    $minX=[Math]::Min(0,$actorX);$minY=[Math]::Min(0,$actorY)
    $width=[Math]::Ceiling([Math]::Max(1254,$actorX+1254*$scale)-$minX);$height=[Math]::Ceiling([Math]::Max(1254,$actorY+1254*$scale)-$minY)
    $prop=if($action -eq 'sit'){'bench'}elseif($action -eq 'work'){'desk'}else{'fountain'}
    $xml='<?xml version="1.0" encoding="UTF-8"?>' + "`n" + '<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" xmlns:inkscape="http://www.inkscape.org/namespaces/inkscape" width="'+$width+'" height="'+$height+'" viewBox="0 0 '+$width+' '+$height+'">'
    $xml+='<title>newASTER garden '+$action+' editable composition</title><desc>Original PNG references; authored contacts and editable front/back masks. Canvas expanded to contain both objects without cropping.</desc><defs>'+(Masks 'furniture' $front)+(Masks 'actor' $hands)+'</defs>'
    $xml+='<g transform="translate('+(Format (-$minX))+','+(Format (-$minY))+')">'
    $xml+=Layer 'furniture-back' ('garden-'+$prop) 'furniture-back'
    $transform='translate('+(Format $actorX)+','+(Format $actorY)+') scale('+(Format $scale)+')'
    $xml+=Layer 'actor-back' ('slayer-sd-'+$action) 'actor-back' $transform
    if($front.Count -gt 0){$xml+=Layer 'furniture-front' ('garden-'+$prop) 'furniture-front'}
    if($hands.Count -gt 0){$xml+=Layer 'actor-hands-front' ('slayer-sd-'+$action) 'actor-front' $transform}
    $xml+='</g></svg>'
    $path=Join-Path $output ('garden-'+$action+'-composition-v1.svg')
    [IO.File]::WriteAllText($path,$xml+"`n",[Text.UTF8Encoding]::new($false))
    [xml]$check=Get-Content -LiteralPath $path -Raw
    $expectedLayers=2+[int]($front.Count -gt 0)+[int]($hands.Count -gt 0)
    $layers=$check.SelectNodes('//*[local-name()="g" and @id]')
    if($layers.Count -ne $expectedLayers -or $check.SelectNodes('//*[local-name()="mask"]').Count -ne 4){throw "Invalid SVG layers or masks: $action"}
    if($check.SelectNodes('//*[local-name()="path"]').Count -ne 2*($front.Count+$hands.Count)){throw "Invalid SVG polygon count: $action"}
    $records+=[ordered]@{action=$action;furnitureId=$entry.Groups[1].Value;canvas=@($width,$height);actorOrigin=@($actorX,$actorY);actorScale=$scale;furnitureFrontPolygons=$front.Count;actorHandsPolygons=$hands.Count;sourceSha256=(Get-FileHash (Join-Path $repo 'game/unity/Assets/Game/Scripts/Presentation/GardenArtComposition.cs')).Hash}
    Write-Output "PLAN7_GARDEN_SOURCE_PASS $action ${width}x${height} front=$($front.Count) hands=$($hands.Count)"
}
[IO.File]::WriteAllText((Join-Path $output 'garden-composition-records.json'),($records | ConvertTo-Json -Depth 8)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))

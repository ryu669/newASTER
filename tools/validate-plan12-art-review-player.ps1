param(
    [string]$PlayerPath='game/Builds/plan12-preparation/newASTER.exe'
)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$player=Join-Path $repo $PlayerPath
if(-not (Test-Path -LiteralPath $player)){throw "Missing review Player: $player"}
$output=Join-Path $repo ('tmp/plan12-art-review/player-'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
$records=@()
& {
    foreach($hero in @('annihilator-holy','arcane')){
        foreach($view in @('chapter','garden','battle','event0')){
            $base=Join-Path $output "$hero-$view-default"
            $flags=@('-logFile',"$base.log",'-presentationCapture',"$base.png")
            if($hero -eq 'arcane'){
                $flags+=@('-captureArcane','-arcaneView',$view)
                $marker="PLAN10_ARCANE_PLAYER_PASS view=$view "
            }else{
                $flags+=@('-captureAnnihilator','-anniForm','holy','-anniView',$view)
                $marker="PLAN10_ANNIHILATOR_PLAYER_PASS form=holy view=$view "
            }
            # This is the interactive game surface being visually reviewed. Hidden windows can yield black captures.
            $proc=Start-Process -FilePath $player -ArgumentList $flags -WindowStyle Normal -PassThru
            if(-not $proc.WaitForExit(45000)){Stop-Process -Id $proc.Id;throw "Review capture timeout: $hero $view"}
            $log=Get-Content -LiteralPath "$base.log" -Raw
            if($proc.ExitCode -ne 0 -or $log -notmatch [regex]::Escape($marker) -or $log -match '(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)' -or -not (Test-Path -LiteralPath "$base.png")){throw "Review capture failed: $base.log"}
            $size=python -c 'from PIL import Image; import sys; print(*Image.open(sys.argv[1]).size)' "$base.png"
            $width,$height=$size.Split(' ')
            python -c 'from PIL import Image,ImageStat; import sys; im=Image.open(sys.argv[1]); assert im.size==(int(sys.argv[2]),int(sys.argv[3])); s=ImageStat.Stat(im.convert("RGB")); assert max(s.mean)>20 and max(s.stddev)>20' "$base.png" $width $height
            if($LASTEXITCODE -ne 0){throw "Invalid capture image: $base.png"}
            $records+=@{heroineId="heroine.$hero";view=$view;width=$width;height=$height;image="$base.png";imageSha256=(Get-FileHash -LiteralPath "$base.png" -Algorithm SHA256).Hash.ToLower();log="$base.log";logSha256=(Get-FileHash -LiteralPath "$base.log" -Algorithm SHA256).Hash.ToLower()}
            Write-Output "PLAN12_REVIEW_CAPTURE_PASS $hero $view ${width}x${height}"
        }
    }
}
$report=@{scope='Existing dedicated form fixtures on review build; fixture combat catalogs are historical subsets';manualInput=$false;visualAcceptance='requires image inspection';player=$player;assemblySha256=(Get-FileHash -LiteralPath (Join-Path (Split-Path $player -Parent) 'newASTER_Data/Managed/Assembly-CSharp.dll') -Algorithm SHA256).Hash.ToLower();records=$records}
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $output 'captures.json') -Encoding utf8
Write-Output "PLAN12_REVIEW_PLAYER_PASS output=$output"

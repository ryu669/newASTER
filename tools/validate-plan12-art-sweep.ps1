param([string]$PlayerPath='game/Builds/plan12-preparation/newASTER.exe',[switch]$GalleryOnly,[string]$Hero,[string]$Views)
$ErrorActionPreference='Stop'
if(-not $Hero -or -not $Views){throw 'Character review requires -Hero and pending -Views; completed characters are not swept again'}
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo ('tmp/plan12-art-sweep/'+[guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $output | Out-Null
& {
    $folder=Join-Path $output 'default'
    New-Item -ItemType Directory -Path $folder | Out-Null
    $log=Join-Path $folder 'player.log'
    $flags=@('-logFile',$log,'-presentationCapture',(Join-Path $folder 'entry.png'),'-capturePlan10Ui','-uiView','detail','-plan12ArtSweep')
    if($GalleryOnly){$flags+='-plan12CgGallerySweep'}
    $expectedCaptures=if($GalleryOnly){75}else{240}
    if($Hero){
        if(-not $Views){throw 'Specify only the pending Views when selecting a Hero'}
        $flags+=@('-plan12ArtHero',$Hero,'-plan12ArtViews',$Views)
        $expectedCaptures=$Views.Split(',').Count
    }
    # The game window is the interactive visual review surface; hidden rendering may be black.
    $proc=Start-Process -FilePath (Join-Path $repo $PlayerPath) -ArgumentList $flags -WindowStyle Normal -PassThru
    $deadline=[DateTime]::UtcNow.AddMinutes(12)
    while(-not $proc.WaitForExit(1000)){
        if([DateTime]::UtcNow -gt $deadline){Stop-Process -Id $proc.Id;throw 'Art sweep timeout'}
        if(Test-Path -LiteralPath $log){$body=Get-Content -LiteralPath $log -Raw;if($body -match '(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)'){Stop-Process -Id $proc.Id;Get-Content -LiteralPath $log -Tail 25;throw 'Art sweep exception'}}
    }
    if($proc.ExitCode -ne 0 -or (Get-Content -LiteralPath $log -Raw) -notmatch "PLAN12_ART_SWEEP_PASS captures=$expectedCaptures "){throw "Art sweep failed: $log"}
    $firstImage=Get-ChildItem -LiteralPath $folder -Filter '*.png' | Select-Object -First 1
    $size=python -c 'from PIL import Image; import sys; print(*Image.open(sys.argv[1]).size)' $firstImage.FullName
    $width,$height=$size.Split(' ')
    $recordArgs=@($folder,$width,$height)
    if($Hero){$recordArgs+=@($Hero,$Views)}
    python (Join-Path $PSScriptRoot 'record-plan12-art-sweep.py') @recordArgs
    if($LASTEXITCODE -ne 0){throw "Art sweep image checks failed: $folder"}
    Write-Output "PLAN12_ART_SWEEP_PLAYER_PASS default=${width}x${height} output=$folder"
}
Write-Output "PLAN12_ART_SWEEP_ALL_PASS output=$output"

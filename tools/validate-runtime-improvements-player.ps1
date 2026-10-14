param([string]$PlayerPath='game/Builds/plan12-april/newASTER.exe',[string[]]$Cases=@('cache','paging','streaming','garden'))
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo ('tmp/runtime-improvements-player/'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output -Force | Out-Null
$player=Join-Path $repo $PlayerPath
foreach($case in $Cases){
    $folder=Join-Path $output $case;New-Item -ItemType Directory -Path $folder | Out-Null
    $log=Join-Path $folder 'player.log';$png=Join-Path $folder 'frame.png'
    $flags=@('-logFile',$log,'-presentationCapture',$png,'-plan12AprilPreview')
    if($case -in @('cache','history')){
        $flags+=@('-captureFirstBatch','-batchHero','april','-batchView','detail','-validateRuntimeImprovements');$marker='RUNTIME_IMPROVEMENTS_CACHE_PASS'
        if($case -eq 'history'){$flags+='-validateSaveHistory';$marker='SAVE_HISTORY_PLAYER_PASS'}
    }
    elseif($case -eq 'paging'){$flags+=@('-capturePlan10Ui','-uiView','roster-second');$marker='PLAN10_UI_AUDIT_PLAYER_PASS'}
    elseif($case -in @('streaming','garden')){
        $flags+=@('-capturePlan10Ui','-uiView','garden-life','-plan15Manual','-validatePlan119');$marker='PLAN11_9_PLAYER_QUALITY_PASS'
        if($case -eq 'streaming'){
            $fixture=Join-Path $repo 'tmp/plan11-9/fixtures/plan119-images'
            if(-not (Test-Path -LiteralPath $fixture)){throw 'Build the separate Plan119 image fixture before the streaming case.'}
            $flags+=@('-qualityFixtureBundle',$fixture)
        }
    }else{throw "Unknown case $case"}
    # The visible Player is required for rendering and frame-end readiness.
    $process=Start-Process -FilePath $player -ArgumentList $flags -WindowStyle Normal -PassThru
    $clock=[Diagnostics.Stopwatch]::StartNew()
    try{
        while(-not $process.WaitForExit(100)){
            if($clock.Elapsed.TotalSeconds -gt 150){throw "Player timeout: $log"}
            if(Test-Path -LiteralPath $log){$text=[string](Get-Content -LiteralPath $log -Raw -ErrorAction SilentlyContinue);if($text -match '(?m)^(\w*Exception:|COMBAT_DEFINITIONS_ERROR)'){throw "Player error: $log"}}
        }
        $text=Get-Content -LiteralPath $log -Raw
        if($process.ExitCode -ne 0 -or $text -notmatch $marker -or ($case -eq 'streaming' -and $text -notmatch 'PLAN11_9_IMAGE_STREAMING_PASS')){throw "Missing validation marker: $log"}
        & 'C:/Program Files/Python310/python.exe' -X utf8 (Join-Path $PSScriptRoot 'check-player-frame.py') $png
        if($LASTEXITCODE -ne 0){throw "Invalid framebuffer: $png"}
        Write-Output "RUNTIME_IMPROVEMENTS_PLAYER_PASS case=$case output=$folder"
    }finally{if(-not $process.HasExited){Stop-Process -Id $process.Id;$process.WaitForExit()}}
}

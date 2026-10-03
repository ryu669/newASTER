param([string]$Player,[string[]]$Cases=@('First','Last','Empty','EmptyHero','Stories','StoryDetails','Garden','GardenLast','Hero','Transition'))
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Player){$Player=Join-Path $repo 'game/Builds/playable/newASTER.exe'}
$output=Join-Path $repo ('tmp/plan6-player-'+[DateTime]::UtcNow.ToString('yyyyMMddHHmmss')+'-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
foreach($resolution in @(@(1280,720),@(1920,1080))){
    foreach($case in $Cases){
        $name=$case+'-'+$resolution[1];$png=Join-Path $output ($name+'.png');$log=Join-Path $output ($name+'.log')
        $flags=@('-screen-fullscreen','0','-screen-width',"$($resolution[0])",'-screen-height',"$($resolution[1])",'-presentationCapture',('"'+$png+'"'),'-captureBook','-logFile',('"'+$log+'"'))
        if($case -ne 'First'){$flags+='-book'+$case}
        $process=Start-Process -FilePath $Player -ArgumentList $flags -WindowStyle Normal -PassThru
        if(-not $process.WaitForExit(60000)){$process.Kill();throw "Plan6 player timeout: $name"}
        $text=Get-Content -LiteralPath $log -Raw
        if($process.ExitCode -ne 0 -or $text -notmatch 'PLAN6_BOOK_PLAYER_PASS' -or $text -match '(?m)^\w*Exception:'){throw "Plan6 player failed: $name; inspect $log"}
        if(-not(Test-Path -LiteralPath $png)){throw "Screenshot missing: $name"}
        Write-Output "PLAN6_PLAYER_CASE_PASS $name"
    }
}
Write-Output "PLAN6_PLAYER_OUTPUT $output"

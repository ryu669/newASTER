param([string]$Player,[ValidateSet('Garden','Weapons','Events','Adv','Cg','Backlog')][string[]]$Cases=@('Garden','Weapons','Events','Adv','Cg','Backlog'),[ValidateSet('Old','New')][string[]]$SaveForms=@('Old','New'))
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Player){$Player=Join-Path $repo 'game/Builds/playable/newASTER.exe'}
$output=Join-Path $repo ('tmp/plan6-home-player-'+[DateTime]::UtcNow.ToString('yyyyMMddHHmmss')+'-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $output | Out-Null
foreach($saveForm in $SaveForms){
    $save=Join-Path $output ($saveForm+'-campaign.json')
    foreach($display in $Cases){
        foreach($resolution in @(@(1280,720),@(1920,1080))){
            $name=$saveForm+'-'+$display+'-'+$resolution[1];$png=Join-Path $output ($name+'.png');$log=Join-Path $output ($name+'.log')
            $flags=@('-screen-fullscreen','0','-screen-width',"$($resolution[0])",'-screen-height',"$($resolution[1])",'-presentationCapture',('"'+$png+'"'),'-capturePlan6Home','-plan6Save',('"'+$save+'"'),'-homeCase',$display,'-logFile',('"'+$log+'"'))
            if(Test-Path -LiteralPath $save){$flags+='-plan6Resume'}elseif($saveForm -eq 'New'){$flags+='-plan6NewSave'}
            $process=Start-Process -FilePath $Player -ArgumentList $flags -WindowStyle Normal -PassThru
            if(-not $process.WaitForExit(60000)){$process.Kill();throw "Plan6 home player timeout: $name"}
            $text=Get-Content -LiteralPath $log -Raw
            if($process.ExitCode -ne 0 -or $text -notmatch 'PLAN6_HOME_PLAYER_PASS' -or $text -match '(?m)^\w*Exception:'){throw "Plan6 home player failed: $name; inspect $log"}
            if(-not(Test-Path -LiteralPath $png)){throw "Screenshot missing: $name"}
            Write-Output "PLAN6_HOME_PLAYER_CASE_PASS $name"
        }
    }
}
Write-Output "PLAN6_HOME_PLAYER_OUTPUT $output"

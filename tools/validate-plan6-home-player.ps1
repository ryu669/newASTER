param([string]$Player,[ValidateSet('Garden','Weapons','Events','Adv','Cg','Backlog')][string[]]$Cases=@('Garden','Weapons','Events','Adv','Cg','Backlog'),[ValidateSet('Old','New')][string[]]$SaveForms=@('Old','New'),[switch]$LargeText,[ValidateSet('None','sit','work','look','move','remove','idle')][string]$GardenUse='None',[ValidateSet('None','closed','expanded','furniture','residents','events','navigation','placement','confirmation')][string]$GardenMenu='None')
$ErrorActionPreference='Stop'
if($GardenUse -ne 'None' -and ($Cases.Count -ne 1 -or $Cases[0] -ne 'Garden')){throw 'GardenUse requires only Garden case'}
if($GardenMenu -ne 'None' -and ($Cases.Count -ne 1 -or $Cases[0] -ne 'Garden')){throw 'GardenMenu requires only Garden case'}
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
            if($LargeText){$flags+='-inspectLargeText'}
            if($GardenUse -ne 'None'){$flags+=@('-inspectPlan7GardenUse',$GardenUse)}
            if($GardenMenu -ne 'None'){$flags+=@('-inspectGardenMenu',$GardenMenu)}
            $process=Start-Process -FilePath $Player -ArgumentList $flags -WindowStyle Normal -PassThru
            $deadline=[DateTime]::UtcNow.AddSeconds(60)
            while(-not $process.WaitForExit(1000)){
                if((Test-Path -LiteralPath $log) -and (Get-Content -LiteralPath $log -Raw) -match '(?m)^\w*Exception:'){$process.Kill();$process.WaitForExit();throw "Plan6 home player exception: $name; inspect $log"}
                if([DateTime]::UtcNow -ge $deadline){$process.Kill();$process.WaitForExit();throw "Plan6 home player timeout: $name; inspect $log"}
            }
            $text=Get-Content -LiteralPath $log -Raw
            if($process.ExitCode -ne 0 -or $text -notmatch 'PLAN6_HOME_PLAYER_PASS' -or $text -match '(?m)^\w*Exception:'){throw "Plan6 home player failed: $name; inspect $log"}
            if($GardenUse -ne 'None' -and $text -notmatch ('PLAN7_GARDEN_USE_CAPTURE '+$GardenUse+' action=')){throw "Garden scenario not validated: $name"}
            if($GardenMenu -ne 'None' -and $text -notmatch ('GARDEN_MENU_CAPTURE_PASS '+$GardenMenu+' / isolated')){throw "Garden menu not validated: $name"}
            if(-not(Test-Path -LiteralPath $png)){throw "Screenshot missing: $name"}
            Write-Output "PLAN6_HOME_PLAYER_CASE_PASS $name"
        }
    }
}
Write-Output "PLAN6_HOME_PLAYER_OUTPUT $output"

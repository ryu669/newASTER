param([string]$Player,[ValidateSet('Garden','Weapons','Events','Adv','Cg','Backlog')][string[]]$Cases=@('Garden','Weapons','Events','Adv','Cg','Backlog'),[ValidateSet('Old','New')][string[]]$SaveForms=@('Old','New'),[switch]$LargeText,[ValidateSet('None','sit','work','look','move','remove','idle')][string]$GardenUse='None',[ValidateSet('None','closed','expanded','furniture','residents','events','navigation','placement','confirmation')][string]$GardenMenu='None',[switch]$Measure)
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
            if($Measure){& (Join-Path $PSScriptRoot 'check-plan8-measurement-readiness.ps1') -Output (Join-Path $output ($name+'-environment-before.json'))}
            $flags=@('-screen-fullscreen','0','-screen-width',"$($resolution[0])",'-screen-height',"$($resolution[1])",'-presentationCapture',('"'+$png+'"'),'-capturePlan6Home','-plan6Save',('"'+$save+'"'),'-homeCase',$display,'-logFile',('"'+$log+'"'))
            if(Test-Path -LiteralPath $save){$flags+='-plan6Resume'}elseif($saveForm -eq 'New'){$flags+='-plan6NewSave'}
            if($LargeText){$flags+='-inspectLargeText'}
            if($GardenUse -ne 'None'){$flags+=@('-inspectPlan7GardenUse',$GardenUse)}
            if($GardenMenu -ne 'None'){$flags+=@('-inspectGardenMenu',$GardenMenu)}
            if($Measure){$flags+='-measurePlan7'}
            $watch=[Diagnostics.Stopwatch]::StartNew();$peakWorking=0L;$peakObserved=0L;$firstRepaintObservedMs=$null
            $process=Start-Process -FilePath $Player -ArgumentList $flags -WindowStyle Normal -PassThru
            $deadline=[DateTime]::UtcNow.AddSeconds(60)
            while(-not $process.WaitForExit(250)){
                if($Measure){try{$process.Refresh();$peakWorking=[Math]::Max($peakWorking,$process.PeakWorkingSet64);$peakObserved=[Math]::Max($peakObserved,$process.WorkingSet64)}catch [InvalidOperationException]{};if($null -eq $firstRepaintObservedMs -and (Test-Path -LiteralPath $log) -and (Get-Content -LiteralPath $log -Raw) -match 'PLAN8_FIRST_REPAINT '){$firstRepaintObservedMs=$watch.ElapsedMilliseconds}}
                if((Test-Path -LiteralPath $log) -and (Get-Content -LiteralPath $log -Raw) -match '(?m)^\w*Exception:'){$process.Kill();$process.WaitForExit();throw "Plan6 home player exception: $name; inspect $log"}
                if([DateTime]::UtcNow -ge $deadline){$process.Kill();$process.WaitForExit();throw "Plan6 home player timeout: $name; inspect $log"}
            }
            $text=Get-Content -LiteralPath $log -Raw
            if($process.ExitCode -ne 0 -or $text -notmatch 'PLAN6_HOME_PLAYER_PASS' -or $text -match '(?m)^\w*Exception:'){throw "Plan6 home player failed: $name; inspect $log"}
            if($GardenUse -ne 'None' -and $text -notmatch ('PLAN7_GARDEN_USE_CAPTURE '+$GardenUse+' action=')){throw "Garden scenario not validated: $name"}
            if($GardenMenu -ne 'None' -and $text -notmatch ('GARDEN_MENU_CAPTURE_PASS '+$GardenMenu+' / isolated')){throw "Garden menu not validated: $name"}
            if(-not(Test-Path -LiteralPath $png)){throw "Screenshot missing: $name"}
            if($Measure){
                if($text -notmatch 'PLAN8_HOME_LOAD ' -or $text -notmatch 'PLAN7_PERFORMANCE ' -or $null -eq $firstRepaintObservedMs){throw "Missing home measurement: $name"}
                $data=Join-Path (Split-Path $Player -Parent) 'newASTER_Data'
                $measurement=[ordered]@{schemaVersion=1;case=$display;gardenUse=$GardenUse;height=$resolution[1];saveForm=$saveForm;resumed=($flags -contains '-plan6Resume');fixture='isolated-formal-home-acceptance';assemblySha256=(Get-FileHash -LiteralPath (Join-Path $data 'Managed/Assembly-CSharp.dll')).Hash;resourceSha256=(Get-FileHash -LiteralPath (Join-Path $data 'resources.assets')).Hash;peakWorkingBytes=$peakWorking;observedWorkingPeakBytes=$peakObserved;sampleIntervalMs=250;processToFirstRepaintLogObservedMs=$firstRepaintObservedMs;firstRepaintObservationIncludesPollingAndLogFlush=$true;osCache='uncontrolled';coldOsCacheMeasured=$false;resourcePrevalidation=$true;humanInput=$false;performance=@(($text -split "`n")|Where-Object {$_ -match '^PLAN(7_(EFFECT_)?PERFORMANCE|8_HOME_LOAD|8_FIRST_REPAINT|8_GUI_CPU) '})}
                [IO.File]::WriteAllText((Join-Path $output ($name+'-measurement.json')),($measurement|ConvertTo-Json -Depth 5),[Text.UTF8Encoding]::new($false))
            }
            Write-Output "PLAN6_HOME_PLAYER_CASE_PASS $name"
        }
    }
}
Write-Output "PLAN6_HOME_PLAYER_OUTPUT $output"

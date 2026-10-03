param([string]$Player,[string]$RunId,[switch]$UnsafeRunFixture)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Player){$Player=Join-Path $repo 'game/Builds/playable/newASTER.exe'}
if(-not $RunId){$RunId='baseline-'+[Guid]::NewGuid().ToString('N')}
if($RunId -notmatch '\A[A-Za-z0-9][A-Za-z0-9_-]{0,63}\z'){throw 'Tool output RunId must be a safe path component'}
$output=Join-Path $repo ('tmp/plan8-baseline-player/'+$RunId)
if(Test-Path -LiteralPath $output){throw 'Run already exists'}
New-Item -ItemType Directory -Path $output | Out-Null
$project=Get-Content -LiteralPath (Join-Path $repo 'game/unity/ProjectSettings/ProjectSettings.asset')
$company=($project | Select-String '^  companyName: (.+)$').Matches[0].Groups[1].Value
$product=($project | Select-String '^  productName: (.+)$').Matches[0].Groups[1].Value
$normalRoot=Join-Path ([Environment]::GetFolderPath('UserProfile')) ('AppData/LocalLow/'+$company+'/'+$product)
function Save-Fingerprint {
    $items=@()
    if(Test-Path -LiteralPath $normalRoot){
        foreach($file in (Get-ChildItem -LiteralPath $normalRoot -File | Where-Object Name -notin @('Player.log','Player-prev.log') | Sort-Object Name)){
            $items+=[ordered]@{name=$file.Name;bytes=$file.Length;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash}
        }
    }
    return ConvertTo-Json -InputObject @($items) -Depth 4 -Compress
}
$before=Save-Fingerprint
$dataPath=Join-Path (Split-Path $Player -Parent) ([IO.Path]::GetFileNameWithoutExtension($Player)+'_Data')
$assemblyHash=(Get-FileHash -LiteralPath (Join-Path $dataPath 'Managed/Assembly-CSharp.dll')).Hash
$resourceHash=(Get-FileHash -LiteralPath (Join-Path $dataPath 'resources.assets')).Hash
$log=Join-Path $output 'baseline.log'
$diagnosticId=if($UnsafeRunFixture){'../normal'}else{$RunId}
$arguments=@('-batchmode','-nographics','-validatePlan8Baseline','-plan8RepositoryRoot',('"'+$repo+'"'),'-plan8RunId',$diagnosticId,'-logFile',('"'+$log+'"'))
$process=$null
try{
    $process=Start-Process -FilePath $Player -ArgumentList $arguments -PassThru -WindowStyle Hidden
    if(-not $process.WaitForExit(60000)){Stop-Process -Id $process.Id;throw 'Own baseline diagnostic exceeded 60 seconds'}
    $text=Get-Content -LiteralPath $log -Raw
    if($UnsafeRunFixture){
        if($process.ExitCode -ne 2 -or $text -notmatch 'PLAN8_BASELINE_ERROR Invalid or reserved trial run ID'){throw 'Unsafe diagnostic run was not rejected'}
    }else{
        if($process.ExitCode -ne 0 -or $text -notmatch 'PLAN8_BASELINE_PASS' -or $text -notmatch 'PLAN8_SAVE_BOUNDARY_PASS.*storeOpened=False saveWritten=False' -or $text -match 'PLAN8_BASELINE_ERROR|NullReferenceException'){throw 'Baseline player validation failed'}
    }
}finally{
    $after=Save-Fingerprint
    [IO.File]::WriteAllText((Join-Path $output 'normal-save-before.json'),$before,[Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $output 'normal-save-after.json'),$after,[Text.UTF8Encoding]::new($false))
    if($before -ne $after){throw 'Normal player save/settings changed during baseline diagnostic'}
}
$result=[ordered]@{schemaVersion=1;case=$(if($UnsafeRunFixture){'unsafe-run-rejected'}else{'baseline-valid'});passed=$true;normalSaveUnchanged=$true;assemblySha256=$assemblyHash;resourceAssetsSha256=$resourceHash;exitCode=$process.ExitCode;performanceMeasured=$false;authoredContentReady=$false}
[IO.File]::WriteAllText((Join-Path $output 'result.json'),($result | ConvertTo-Json)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
Write-Output "PLAN8_BASELINE_PLAYER_PASS $output case=$($result.case) normalSaveUnchanged=True"

param([int]$Height=720)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$settings=Get-Content -LiteralPath (Join-Path $repo 'game/unity/ProjectSettings/ProjectSettings.asset')
$company=($settings | Select-String '^  companyName: (.+)$').Matches[0].Groups[1].Value
$product=($settings | Select-String '^  productName: (.+)$').Matches[0].Groups[1].Value
$normalRoot=Join-Path ([Environment]::GetFolderPath('UserProfile')) ('AppData/LocalLow/'+$company+'/'+$product)
function Fingerprint {
    $items=@()
    if(Test-Path -LiteralPath $normalRoot){foreach($file in (Get-ChildItem -LiteralPath $normalRoot -File | Where-Object Name -notin @('Player.log','Player-prev.log') | Sort-Object Name)){
        $items+=[ordered]@{name=$file.Name;sha256=(Get-FileHash -LiteralPath $file.FullName).Hash;bytes=$file.Length}
    }}
    return ConvertTo-Json -InputObject @($items) -Depth 4 -Compress
}
$before=Fingerprint
$fingerprintOutput=Join-Path $repo ('tmp/plan8-telemetry-validation/'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $fingerprintOutput | Out-Null
try {
    $lines=@(& (Join-Path $PSScriptRoot 'validate-plan7-sample-player.ps1') -Cases activecombat -Heights $Height -CompleteBattle -Plan8Telemetry)
} finally {
    $after=Fingerprint
    [IO.File]::WriteAllText((Join-Path $fingerprintOutput 'normal-save-before.json'),$before,[Text.UTF8Encoding]::new($false))
    [IO.File]::WriteAllText((Join-Path $fingerprintOutput 'normal-save-after.json'),$after,[Text.UTF8Encoding]::new($false))
    if($before -ne $after){throw 'Normal save/settings changed during rendered diagnostic'}
}
$lines | Write-Output
$outputLine=$lines | Where-Object {$_ -like 'PLAN7_SAMPLE_PLAYER_OUTPUT *'} | Select-Object -Last 1
if(-not $outputLine){throw 'Missing rendered telemetry output'}
$output=$outputLine.Substring('PLAN7_SAMPLE_PLAYER_OUTPUT '.Length)
$after=Fingerprint
[IO.File]::WriteAllText((Join-Path $output 'normal-save-before.json'),$before,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $output 'normal-save-after.json'),$after,[Text.UTF8Encoding]::new($false))
if($before -ne $after){throw 'Normal save/settings changed during rendered diagnostic'}
$events=@(Get-Content -LiteralPath (Join-Path $output ('activecombat-'+$Height+'-telemetry.json')) -Raw | ConvertFrom-Json)
$presentations=@($events | Where-Object action -eq 'presentation')
$end=@($events | Where-Object action -eq 'diagnostic-end')
if($presentations.Count -ne 66 -or $end.Count -ne 1){throw 'Rendered battle telemetry includes reference simulations or misses actual events'}
if(@($events | Where-Object {$_.resourceHash -notmatch '^[A-F0-9]{64}$' -or $_.definitionHash -notmatch '^[A-F0-9]{64}$'}).Count -gt 0){throw 'Missing resource/definition provenance'}
$memory=Get-Content -LiteralPath (Join-Path $output ('activecombat-'+$Height+'-memory.json')) -Raw | ConvertFrom-Json
if(@($events | Where-Object {$_.resourceHash -ne $memory.resourceAssetsSha256}).Count -gt 0){throw 'Telemetry resources differ from rendered player'}
$previousElapsed=0.0;$previousActive=0.0
foreach($event in $events){
    if($event.elapsedSeconds -lt $previousElapsed -or $event.activeSeconds -lt $previousActive -or $event.activeSeconds -gt $event.elapsedSeconds+.000001){throw 'Invalid telemetry clock progression'}
    $previousElapsed=$event.elapsedSeconds;$previousActive=$event.activeSeconds
}
$result=[ordered]@{schemaVersion=1;scope='8-2-rendered-diagnostic-not-human-playtest';passed=$true;normalSaveUnchanged=$true;runId=$events[0].runId;buildHash=$events[0].buildHash;resourceHash=$events[0].resourceHash;definitionHash=$events[0].definitionHash;events=$events.Count;renderedPresentations=$presentations.Count;performanceMeasured=$false;authoredContentConnected=$false}
[IO.File]::WriteAllText((Join-Path $output 'plan8-telemetry-result.json'),($result | ConvertTo-Json)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
Write-Output "PLAN8_TELEMETRY_VALIDATION_PASS events=$($events.Count) normalSaveUnchanged=True output=$output"

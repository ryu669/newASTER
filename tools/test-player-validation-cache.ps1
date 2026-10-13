$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'player-validation-cache.ps1')
$fixture=Join-Path (Split-Path $PSScriptRoot -Parent) ('tmp/cache-tests/'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path (Join-Path $fixture 'newASTER_Data/Managed') -Force | Out-Null
$player=Join-Path $fixture 'newASTER.exe';$payload=Join-Path $fixture 'newASTER_Data/Managed/Assembly-CSharp.dll'
Set-Content -LiteralPath $player 'exe';Set-Content -LiteralPath $payload 'data'
$base=Join-Path $fixture 'case';Set-Content -LiteralPath ($base+'.log') 'pass';Set-Content -LiteralPath (Join-Path $fixture 'case.png') 'capture'
$build=Get-PlayerBuildFingerprint $player;$flags=@('-test','one')
Set-Content -LiteralPath ($base+'.complete') 'legacy complete'
if(Test-PlayerCheckpoint $base $build $PSCommandPath $flags){throw 'Legacy marker reused'}
Save-PlayerCheckpoint $base $build $PSCommandPath $flags $fixture
if(-not (Test-PlayerCheckpoint $base $build $PSCommandPath $flags)){throw 'Unchanged result not reusable'}
if(Test-PlayerCheckpoint $base $build $PSCommandPath @('-test','two')){throw 'Changed args reused'}
Set-Content -LiteralPath $payload 'changed-data';$changed=Get-PlayerBuildFingerprint $player
if($changed -eq $build -or (Test-PlayerCheckpoint $base $changed $PSCommandPath $flags)){throw 'Changed Player data reused'}
Set-Content -LiteralPath (Join-Path $fixture 'case.png') 'changed-capture'
if(Test-PlayerCheckpoint $base $build $PSCommandPath $flags){throw 'Changed evidence reused'}
Write-Output 'PLAYER_CACHE_PASS unchanged / legacy / args / Player data / evidence'

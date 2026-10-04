param([string]$Version='0.9.0-rc.1')
$ErrorActionPreference='Stop'
$taskRepo=Split-Path $PSScriptRoot -Parent
$taskBuild=Join-Path $taskRepo 'game/Builds/playable'
$taskOutput=Join-Path $taskRepo ('dist/newASTER-'+$Version+'-windows-x64')
if(Test-Path -LiteralPath $taskOutput){throw 'Distribution directory already exists; preserve the previous candidate and use a new version.'}
$taskRequired=@('newASTER.exe','UnityPlayer.dll','newASTER_Data','MonoBleedingEdge','ThirdPartyNotices')
foreach($taskItem in $taskRequired){if(!(Test-Path -LiteralPath (Join-Path $taskBuild $taskItem))){throw "Missing player dependency: $taskItem"}}
New-Item -ItemType Directory -Path $taskOutput | Out-Null
foreach($taskItem in $taskRequired){Copy-Item -LiteralPath (Join-Path $taskBuild $taskItem) -Destination (Join-Path $taskOutput $taskItem) -Recurse}
foreach($taskDll in (Get-ChildItem -LiteralPath $taskBuild -File -Filter '*.dll' | Where-Object Name -ne 'UnityPlayer.dll')){Copy-Item -LiteralPath $taskDll.FullName -Destination (Join-Path $taskOutput $taskDll.Name)}
foreach($taskItem in @('UnityCrashHandler64.exe','D3D12')){if(Test-Path -LiteralPath (Join-Path $taskBuild $taskItem)){Copy-Item -LiteralPath (Join-Path $taskBuild $taskItem) -Destination (Join-Path $taskOutput $taskItem) -Recurse}}
Copy-Item -LiteralPath (Join-Path $taskRepo 'docs/production/plan9-distribution-readme.txt') -Destination (Join-Path $taskOutput 'README.txt')
Copy-Item -LiteralPath (Join-Path $taskRepo 'docs/production/plan9-credits.txt') -Destination (Join-Path $taskOutput 'CREDITS.txt')
Copy-Item -LiteralPath (Join-Path $taskRepo 'docs/production/plan9-asset-adoption.json') -Destination (Join-Path $taskOutput 'ASSET-ADOPTION.json')
$taskFiles=@(Get-ChildItem -LiteralPath $taskOutput -File -Recurse | Sort-Object FullName | ForEach-Object {[ordered]@{path=$_.FullName.Substring($taskOutput.Length+1).Replace('\','/');bytes=$_.Length;sha256=(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()}})
$taskManifest=[ordered]@{schemaVersion=1;version=$Version;platform='windows-x64';scope='initial-five-distribution-candidate';performanceMeasured=$false;physicalInputCount=0;listeningSeconds=0;files=$taskFiles}
$taskManifest | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $taskOutput 'PACKAGE-MANIFEST.json') -Encoding utf8
foreach($taskFile in $taskFiles){$taskPath=Join-Path $taskOutput $taskFile.path;if((Get-FileHash -LiteralPath $taskPath).Hash.ToLowerInvariant() -ne $taskFile.sha256){throw "Candidate hash mismatch: $($taskFile.path)"}}
$taskManifest | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $taskRepo 'docs/production/plan9-package-validation.json') -Encoding utf8
Write-Output "PLAN9_PACKAGE_PASS $taskOutput files=$($taskFiles.Count)"

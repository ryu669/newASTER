# Automatic behavior checkpoints are separate from per-character visual approvals.
$script:PlayerCheckpointHelperPath=$PSCommandPath
function Get-PlayerBuildFingerprint([string]$Player) {
    $playerFile=Get-Item -LiteralPath $Player
    $root=$playerFile.Directory.FullName
    $data=Join-Path $root ($playerFile.BaseName+'_Data')
    $files=@($playerFile)+@(Get-ChildItem -LiteralPath $data -File -Recurse)
    foreach($name in @('UnityPlayer.dll','UnityCrashHandler64.exe')){ $p=Join-Path $root $name;if(Test-Path -LiteralPath $p){$files+=Get-Item -LiteralPath $p} }
    $parts=@($files | Sort-Object FullName | ForEach-Object { $_.FullName.Substring($root.Length)+'|'+(Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash })
    $sha=[Security.Cryptography.SHA256]::Create()
    try { return [BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes(($parts -join "`n")))).Replace('-','') } finally { $sha.Dispose() }
}
function Test-PlayerCheckpoint([string]$Base,[string]$Build,[string]$Script,[string[]]$Flags) {
    try {
        $record=Get-Content -LiteralPath ($Base+'.complete') -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
        if($record.schemaVersion -ne 1 -or $record.build -ne $Build -or $record.script -ne (Get-FileHash -LiteralPath $Script).Hash -or $record.helper -ne (Get-FileHash -LiteralPath $script:PlayerCheckpointHelperPath).Hash -or $record.flags -cne ($Flags -join "`n") -or @($record.evidence).Count -lt 2){return $false}
        foreach($item in $record.evidence){if(-not (Test-Path -LiteralPath $item.path) -or (Get-FileHash -LiteralPath $item.path).Hash -ne $item.sha256){return $false}}
        return $true
    } catch { return $false }
}
function Save-PlayerCheckpoint([string]$Base,[string]$Build,[string]$Script,[string[]]$Flags,[string]$CaptureDirectory) {
    $files=@(Get-Item -LiteralPath ($Base+'.log'))+@(Get-ChildItem -LiteralPath $CaptureDirectory -Filter '*.png' -File)
    if($files.Count -lt 2){throw "Player checkpoint requires a log and rendered evidence: $Base"}
    $record=@{schemaVersion=1;build=$Build;script=(Get-FileHash -LiteralPath $Script).Hash;helper=(Get-FileHash -LiteralPath $script:PlayerCheckpointHelperPath).Hash;flags=($Flags -join "`n");scope='automatic behavior only; unchanged visual approvals remain valid';evidence=@($files | ForEach-Object { @{path=$_.FullName;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash} })}
    $record | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath ($Base+'.complete') -Encoding utf8
}

param([string]$Output,[switch]$ValidateReusePolicy)
$ErrorActionPreference='Stop'
function Test-RecentFullClear($Previous,[DateTimeOffset]$Now){
    try{
        if(-not $Previous.clear -or $null -eq $Previous.fullClearAtUtc){return $false}
        # PowerShell 7 can decode ISO dates as UTC DateTime objects. Parse(string)
        # would lose their Kind through culture formatting and shift the cache age.
        $age=($Now-[DateTimeOffset]$Previous.timeUtc).TotalSeconds
        $batchAge=($Now-[DateTimeOffset]$Previous.fullClearAtUtc).TotalSeconds
        return $age -ge 0 -and $age -le 120 -and $batchAge -ge 0 -and $batchAge -le 600
    }catch{return $false}
}
if($ValidateReusePolicy){
    $now=[DateTimeOffset]::UtcNow
    foreach($datesAsStrings in @($true,$false)){
        foreach($case in @(@(30,90,$true,$true),@(120,600,$true,$true),@(121,200,$true,$false),@(30,601,$true,$false),@(-1,90,$true,$false),@(30,90,$false,$false))){
            $previous=[pscustomobject]@{clear=$case[2];timeUtc=$now.AddSeconds(-$case[0]).UtcDateTime;fullClearAtUtc=$now.AddSeconds(-$case[1]).UtcDateTime}
            if($datesAsStrings){$previous.timeUtc=$previous.timeUtc.ToString('o');$previous.fullClearAtUtc=$previous.fullClearAtUtc.ToString('o')}
            if((Test-RecentFullClear $previous $now) -ne $case[3]){throw 'Readiness cache boundary regression'}
        }
    }
    if(Test-RecentFullClear ([pscustomobject]@{clear=$true;timeUtc='invalid';fullClearAtUtc=$null}) $now){throw 'Invalid cache reused'}
    Write-Output 'PLAN8_READINESS_REUSE_POLICY_PASS UTC DateTime/string, gap, batch, future, failure and invalid boundaries';exit
}
if(-not $Output){throw 'Readiness output path required'}
$repo=Split-Path $PSScriptRoot -Parent
$cache=Join-Path $repo 'tmp/plan8-performance-readiness-cache.json'
$recent=$false
if(Test-Path -LiteralPath $cache){
    try{$previous=Get-Content -LiteralPath $cache -Raw|ConvertFrom-Json;$recent=Test-RecentFullClear $previous ([DateTimeOffset]::UtcNow)}catch{$recent=$false}
}
try{
    & (Join-Path $PSScriptRoot 'check-plan7-performance-environment.ps1') -Output $Output -QuickSnapshot:$recent
}finally{
    # A failed check invalidates the batch too. The next explicit run uses a full check.
    if(Test-Path -LiteralPath $Output){
        $record=Get-Content -LiteralPath $Output -Raw|ConvertFrom-Json -AsHashtable
        $record.fullClearAtUtc=if(-not $record.clear){$null}elseif($recent){$previous.fullClearAtUtc}else{$record.timeUtc}
        $record.fullCheckSha256=if($recent){$previous.fullCheckSha256}else{(Get-FileHash -LiteralPath $Output).Hash}
        if($recent){[IO.File]::WriteAllText($Output,($record|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))}
        [IO.File]::WriteAllText($cache,($record|ConvertTo-Json -Depth 8),[Text.UTF8Encoding]::new($false))
    }
}

$ErrorActionPreference='Stop'
$flags=@('R','ANNIHILATOR','SHELL','SHELL_ART','ORIFLAMME','ORIFLAMME_ART','NIGHTHAWK','NIGHTHAWK_ART','SLAYER_SWIM','ARCANE','ARCANE_ART','ACADEMY','ACADEMY_ART','SHANGRILA','SHANGRILA_ART','UI')
$previous=@{}
try {
    foreach($flag in $flags){$key="NEWASTER_PLAN10_$flag";$previous[$key]=[Environment]::GetEnvironmentVariable($key);[Environment]::SetEnvironmentVariable($key,'1')}
    & (Join-Path $PSScriptRoot 'validate-automatic-chain.ps1')
    & (Join-Path $PSScriptRoot 'compile-unity-scripts.ps1')
} finally {
    foreach($key in $previous.Keys){[Environment]::SetEnvironmentVariable($key,$previous[$key])}
}

param([string]$Player,[ValidateSet('sit','work','look','move','remove','idle')][string[]]$Scenarios=@('sit','work','look','move','remove','idle'))
$ErrorActionPreference='Stop'
foreach($scenario in $Scenarios){
    Write-Output "PLAN7_GARDEN_PLAYER_SCENARIO $scenario"
    & (Join-Path $PSScriptRoot 'validate-plan6-home-player.ps1') -Player $Player -Cases Garden -SaveForms New -GardenUse $scenario
}
Write-Output 'PLAN7_GARDEN_PLAYER_PASS'

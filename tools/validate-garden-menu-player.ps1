param([string]$Player,[ValidateSet('closed','expanded','furniture','residents','events','navigation','placement','confirmation')][string[]]$Cases=@('closed','expanded','furniture','residents','events','navigation','placement','confirmation'))
$ErrorActionPreference='Stop'
foreach($case in $Cases){
    Write-Output "GARDEN_MENU_PLAYER_CASE $case"
    & (Join-Path $PSScriptRoot 'validate-plan6-home-player.ps1') -Player $Player -Cases Garden -SaveForms New -GardenMenu $case
}
Write-Output 'GARDEN_MENU_PLAYER_PASS'

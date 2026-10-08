param(
    [int]$Width=1280,
    [int]$Height=720,
    [string]$PlayerPath='game/Builds/plan11-5-6/newASTER.exe'
)
$ErrorActionPreference='Stop'
$views=@(
    'roster','detail','detail-information','detail-information-ring','material-exchange','materials','engagement',
    'book-materials','book-new-world','book-possible-worlds','book-system','book-items-equip',
    'affection-lover','affection-cap20','affection-cap99','affection-unread','affection-confirm','affection-shop','affection-max99',
    'garden-life','garden-life-social','garden-life-viewing','garden-life-settings','garden-life-records','garden-life-level6','garden-life-level7',
    'oopart-conditions','oopart-direct','oopart-level','oopart-pending','oopart-presets','oopart-items','oopart-battle','oopart-result','oopart-empty-slot'
)
& (Join-Path $PSScriptRoot 'validate-plan10-ui-player.ps1') -Views $views -Width $Width -Height $Height -PlayerPath $PlayerPath
Write-Output "PLAN15_16_UI_PASS $($views.Count) cases ${Width}x${Height}"

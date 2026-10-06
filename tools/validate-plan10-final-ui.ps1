$ErrorActionPreference='Stop'
# Run only after the final build has exited. Player windows are sequential.
& (Join-Path $PSScriptRoot 'validate-plan10-ui-player.ps1') -Views @('help','level-confirm','garden-residents','garden-residents-last','garden-events','garden-events-last','formation-general','formation-general-confirm','recruitment-second')
& (Join-Path $PSScriptRoot 'validate-plan10-ui-player.ps1') -Width 1024 -Height 768 -Views @('settings','roster-second','formation-general-confirm','job-gambler','job-sniper','garden-events-last','adv-help','model')
& (Join-Path $PSScriptRoot 'validate-plan10-ui-player.ps1') -Width 1720 -Height 720 -Views @('title','job-gambler','formation-general')
& (Join-Path $PSScriptRoot 'validate-plan10-shangrila-player.ps1') -Width 1024 -Height 768 -Views @('detail','weapon','formation-member','sniper-mode','event2')
& (Join-Path $PSScriptRoot 'validate-plan10-shangrila-player.ps1') -Width 1600 -Height 900 -Views @('detail','formation-member','weapon')

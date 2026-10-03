$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'Plan7AudioSource.cs')
$audioOutput=Join-Path (Split-Path $PSScriptRoot -Parent) 'game/unity/Assets/Game/Resources/Audio'
[Plan7AudioSource]::Generate($audioOutput)
Write-Output 'PLAN7_ORIGINAL_AUDIO_GENERATED 6 PCM clips, 44100 Hz mono 16-bit'

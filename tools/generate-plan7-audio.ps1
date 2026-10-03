$ErrorActionPreference='Stop'
Add-Type -Path (Join-Path $PSScriptRoot 'Plan7AudioSource.cs')
$audioOutput=Join-Path (Split-Path $PSScriptRoot -Parent) 'game/unity/Assets/Game/Resources/Audio'
[Plan7AudioSource]::Generate($audioOutput)
foreach($name in @('hit','shield','heal','break','victory','bgm')){
    $meta=Join-Path $audioOutput ('candidate-'+$name+'-v2.wav.meta')
    $guid=if(Test-Path -LiteralPath $meta){(Get-Content -LiteralPath $meta | Where-Object {$_ -match '^guid: '}) -replace '^guid: ',''}else{[Guid]::NewGuid().ToString('N')}
    $content=@"
fileFormatVersion: 2
guid: $guid
AudioImporter:
  externalObjects: {}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: 0
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 0
    quality: 1
    conversionMode: 0
    preloadAudioData: 1
  platformSettingOverrides: {}
  forceToMono: 0
  normalize: 0
  loadInBackground: 0
  ambisonic: 0
  3D: 1
  userData:
  assetBundleName:
  assetBundleVariant:
"@
    [IO.File]::WriteAllText($meta,$content+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))
}
Write-Output 'PLAN7_ORIGINAL_AUDIO_GENERATED 6 v2 PCM clips, 44100 Hz mono 16-bit; v1 retained'

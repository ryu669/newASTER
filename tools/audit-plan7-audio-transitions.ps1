param([string]$Output)
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
if(-not $Output){$Output=Join-Path $repo 'tmp/plan7-audio-transitions.json'}
function Read-Pcm([string]$Name,[string]$Version){
    $suffix=if($Version -eq 'v2'){'-v2'}else{''}
    $path=Join-Path $repo ('game/unity/Assets/Game/Resources/Audio/candidate-'+$Name+$suffix+'.wav')
    $bytes=[IO.File]::ReadAllBytes($path)
    if([Text.Encoding]::ASCII.GetString($bytes,0,4) -ne 'RIFF' -or [Text.Encoding]::ASCII.GetString($bytes,8,4) -ne 'WAVE' -or [BitConverter]::ToInt16($bytes,20) -ne 1 -or [BitConverter]::ToInt16($bytes,22) -ne 1 -or [BitConverter]::ToInt32($bytes,24) -ne 44100 -or [BitConverter]::ToInt16($bytes,34) -ne 16 -or [BitConverter]::ToInt32($bytes,40) -ne $bytes.Length-44){throw "Unexpected generated PCM header: $path"}
    $samples=New-Object short[] (($bytes.Length-44)/2)
    [Buffer]::BlockCopy($bytes,44,$samples,0,$bytes.Length-44)
    return ,$samples
}
$report=@()
foreach($name in @('bgm','heal','victory')){
    $interval=if($name -eq 'bgm'){.5}elseif($name -eq 'heal'){.2}else{.3}
    foreach($version in @('v1','v2')){
        $samples=Read-Pcm $name $version;$boundaries=@()
        $limit=if($name -eq 'victory' -and $version -eq 'v2'){5}else{[Math]::Floor(($samples.Length-1)/44100/$interval)}
        for($step=1;$step -le $limit;$step++){
            $index=[int][Math]::Round($step*$interval*44100)
            if($index+1 -ge $samples.Length){continue}
            $jump=[int]$samples[$index]-[int]$samples[$index-1]
            $left=[int]$samples[$index-1]-[int]$samples[$index-2]
            $right=[int]$samples[$index+1]-[int]$samples[$index]
            $boundaries+=[ordered]@{seconds=$step*$interval;jump=$jump;slopeError=[Math]::Abs($jump-($left+$right)/2.0)}
        }
        $max=($boundaries | ForEach-Object {$_.slopeError} | Measure-Object -Maximum).Maximum
        if($boundaries.Count -ne $limit -or $null -eq $max){throw "Missing transition measurements: $name $version"}
        $seam=[Math]::Abs([int]$samples[0]-[int]$samples[$samples.Length-1])
        $loopError=[Math]::Abs(([int]$samples[0]-[int]$samples[$samples.Length-1])-(([int]$samples[$samples.Length-1]-[int]$samples[$samples.Length-2])+([int]$samples[1]-[int]$samples[0]))/2.0)
        if($version -eq 'v2' -and ($max -gt 8 -or $seam -ne 0 -or ($name -eq 'bgm' -and $loopError -gt 8))){throw "Tonal transition discontinuity: $name error=$max seam=$seam loopSlopeError=$loopError"}
        $report+=[ordered]@{name=$name;version=$version;sampleRate=44100;samples=$samples.Length;maxBoundarySlopeError=$max;endpointJump=$seam;loopBoundarySlopeError=$loopError;boundaries=$boundaries}
        Write-Output "PLAN7_AUDIO_TRANSITION_PASS $name $version maxBoundarySlopeError=$max endpointJump=$seam loopBoundarySlopeError=$loopError"
    }
}
foreach($name in @('hit','shield','heal','break','victory','bgm')){
    $samples=Read-Pcm $name 'v2'
    if($samples[0] -ne 0 -or $samples[$samples.Length-1] -ne 0){throw "Endpoint not zero: $name"}
}
[IO.File]::WriteAllText($Output,([ordered]@{schemaVersion=1;status='waveform-check-only';units='16-bit signed PCM sample units';v2MaxBoundarySlopeErrorLimit=8;listeningAcceptance='pending';clips=$report} | ConvertTo-Json -Depth 8)+[Environment]::NewLine,[Text.UTF8Encoding]::new($false))

param([string[]]$Views=@('recruitment','recruit','detail','trait','skill','weapon','formation','battle','command','attack0','attack1','attack2','garden','chapter1','chapter2','chapter3','journey'),[switch]$SkipArt,[int]$TimeoutSeconds=120,[string]$PlayerPath='game/Builds/plan12-aeon/newASTER.exe')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo 'tmp/plan12-aeon/player-default'
New-Item -ItemType Directory -Force $output | Out-Null
$player=Join-Path $repo $PlayerPath
function RunCase($name,$flags,$marker){
    $base=Join-Path $output $name
    $arguments=@('-logFile',"$base.log",'-presentationCapture',"$base.png")+$flags
    $watch=[Diagnostics.Stopwatch]::StartNew()
    # The interactive Player must render a visible window; hiding it yields a black framebuffer.
    $proc=Start-Process -FilePath $player -ArgumentList $arguments -WindowStyle Normal -PassThru
    if(-not $proc.WaitForExit($TimeoutSeconds*1000)){Stop-Process -Id $proc.Id;throw "Aeon player timeout $name"}
    $log=Get-Content "$base.log" -Raw
    if($log -notmatch $marker -or $log -match '(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)') {Get-Content "$base.log" -Tail 35;throw "Aeon player failed $name"}
    & 'C:/Program Files/Python310/python.exe' -X utf8 -c "from pathlib import Path; from PIL import Image,ImageStat; import sys; paths=list(Path(sys.argv[1]).glob('aeon-*.png')) if sys.argv[2]=='art' else [Path(sys.argv[1])/(sys.argv[2]+'.png')]; assert paths; [(lambda im: ((im.size==(1920,1080) and max(ImageStat.Stat(im.convert('RGB')).stddev)>20) or (_ for _ in ()).throw(AssertionError('Invalid framebuffer'))))(Image.open(p)) for p in paths]" $output $name
    if($LASTEXITCODE -ne 0){throw "Invalid Aeon framebuffer $name"}
    Write-Output "AEON_PLAYER_PASS $name elapsed=$([Math]::Round($watch.Elapsed.TotalSeconds,1))s"
}
if(-not $SkipArt){RunCase 'art' @('-capturePlan10Ui','-uiView','detail','-heroineId','heroine.aeon','-plan12ArtSweep','-plan12ArtHero','aeon') 'PLAN12_ART_SWEEP_PASS captures=20 '}
foreach($view in $Views){RunCase $view @('-captureAeon','-aeonView',$view) "PLAN12_AEON_PLAYER_PASS view=$view "}
& 'C:/Program Files/Python310/python.exe' -X utf8 -c "from pathlib import Path; from PIL import Image,ImageStat; images=list(Path('tmp/plan12-aeon/player-default').glob('*.png')); assert images; [(lambda im: (im.size==(1920,1080) or (_ for _ in ()).throw(AssertionError((str(p),im.size)))))(Image.open(p)) for p in images]; print('AEON_DEFAULT_FRAMEBUFFER_PASS',len(images),'images 1920x1080; no resolution overrides')"
if($LASTEXITCODE -ne 0){throw 'Aeon default framebuffer check failed'}

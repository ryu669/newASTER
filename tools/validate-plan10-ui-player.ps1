param([string[]]$Views=@('title','settings','credits','title-help','help','model','book-colossi','book-world','book-stories','navigation-routes','roster','roster-filter-empty','summoning-debug','summoning-debug-dialog','roster-second','formation-roster-second','formation-weapon-return','empty','detail','trait','skill','skillmax','skill-pending','level','level-confirm','tree-grown','tree-confirm','materials','collection','relics','engagement','kinder','kinder-draw','kinder-exchange','kinder-tickets','kinder-rates','train-arrival','train-doors','train-disembark','garden-expanded','garden-furniture','garden-residents','garden-residents-last','garden-events','garden-events-last','garden-navigation','garden-confirmation','adv-backlog','adv-help','formation-general','formation-general-confirm','recruitment-second','battle-targets','battle-menu-actions','battle-menu-playing','battle-menu-healing','battle-menu-help','battle-timeline','battle-pause','battle-retreat','battle-result','job-fighter','job-berserker','job-defender','job-blaster','job-gunner','job-artist','job-healer','job-panzer','job-alchemist','job-chaser','job-sniper','job-gambler','job-general'),[int]$Width=1600,[int]$Height=900,[string]$PlayerPath='game/Builds/plan10-shangrila/newASTER.exe',[string]$HeroineId='')
$ErrorActionPreference='Stop'
$repo=Split-Path $PSScriptRoot -Parent
$output=Join-Path $repo "tmp/plan10-ui/player-${Width}x${Height}"
New-Item -ItemType Directory -Force $output | Out-Null
foreach($view in $Views){
    $name=if($HeroineId){"$view-$($HeroineId.Replace('heroine.',''))"}else{$view}
    $base=Join-Path $output "ui-$name"
    $flags=@('-screen-width',"$Width",'-screen-height',"$Height",'-screen-fullscreen','0','-logFile',"$base.log",'-presentationCapture',"$base.png",'-capturePlan10Ui','-uiView',$view)
    if($HeroineId){$flags+=@('-heroineId',$HeroineId)}
    $proc=Start-Process -FilePath (Join-Path $repo $PlayerPath) -ArgumentList $flags -WindowStyle Normal -PassThru
    if(-not $proc.WaitForExit(45000)){Stop-Process -Id $proc.Id;throw "UI player timeout $view"}
    $log=Get-Content "$base.log" -Raw
    if($log -notmatch "PLAN10_UI_AUDIT_PLAYER_PASS view=$view " -or $log -match '(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)' -or -not (Test-Path "$base.png")){Get-Content "$base.log" -Tail 65;throw "UI player failed $view"}
    if($HeroineId -and $view -eq 'detail' -and $log -notmatch ('HEROINE_SANCTUARY_CAPTURE_PASS view=detail heroine='+[regex]::Escape($HeroineId)+' ')){throw "Wrong heroine detail capture: $HeroineId"}
    & 'C:\Program Files\Python310\python.exe' -c "from PIL import Image,ImageStat; import sys; im=Image.open(sys.argv[1]); s=ImageStat.Stat(im.convert('RGB')); assert im.size==(int(sys.argv[2]),int(sys.argv[3])), im.size; assert max(s.mean)>20 and max(s.stddev)>20, 'Black capture'" "$base.png" $Width $Height
    if($LASTEXITCODE -ne 0){throw "Invalid UI framebuffer $view"}
    if($view -eq 'model'){
        & 'C:\Program Files\Python310\python.exe' -c "from PIL import Image,ImageStat; import sys; im=Image.open(sys.argv[1]).convert('RGB'); w,h=im.size; s=ImageStat.Stat(im.crop((w//3,h//4,w*2//3,h*3//4))); assert max(s.stddev)>20, 'Missing model in viewer'" "$base.png"
        if($LASTEXITCODE -ne 0){throw 'Missing model in viewer'}
    }
    Write-Output "UI_PLAYER_PASS $name ${Width}x${Height}"
}

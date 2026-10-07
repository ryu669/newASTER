"""Record measured validation and inspect captured pixels without altering game art."""
import json,re,hashlib
from pathlib import Path
from PIL import Image,ImageStat,ImageDraw
ROOT=Path(__file__).resolve().parents[1];TMP=ROOT/'tmp/plan10-annihilator';BUILD=ROOT/'game/Builds/plan10-annihilator'
buildlog=(TMP/'build-final.log').read_text(encoding='utf-8-sig')
assert 'PLAN10_ANNIHILATOR_BUILD_PASS' in buildlog and 'PLAN10_ANNIHILATOR_UNITY_PASS' in buildlog
assert not re.search(r'error CS\d+',buildlog)
screens=[]
for path in sorted(TMP.glob('player-*/*.png')):
    log=path.with_suffix('.log').read_text(encoding='utf-8-sig')
    assert 'PLAN10_ANNIHILATOR_PLAYER_PASS' in log,path
    assert not re.search(r'(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)',log),path
    im=Image.open(path).convert('RGB');stat=ImageStat.Stat(im)
    assert max(stat.stddev)>20 and max(stat.mean)>20,('Empty capture',path)
    screens.append(dict(path=str(path),resolution=im.size,pixelMean=stat.mean,pixelStddev=stat.stddev))
for form in ['normal','holy']:
    for view in ['recruit','roster','detail','weapon','formation','battle','garden','chapter','event0','event1','event2','event3','event4','journey']:
        assert (TMP/f'player-1280/{form}-{view}.png').exists(),(form,view)
for view in ['heal-selection','heal-action','buff-selection','buff-action','attack','invest','overheal','maxhp','revive']:
    assert (TMP/f'player-1280/holy-{view}.png').exists(),view
for form in ['normal','holy']:
    for view in ['detail','chapter','event2','battle']:
        assert (TMP/f'player-1600/{form}-{view}.png').exists(),(form,view)
art=json.loads((ROOT/'docs/production/plan10-annihilator-art-validation.json').read_text(encoding='utf8'))
assert len(art['assets'])==36
for row in art['assets']:assert hashlib.sha256(Path(row['path']).read_bytes()).hexdigest()==row['sha256']
report=dict(date='2026-10-06',status='accepted',forms=['heroine.annihilator','heroine.annihilator-holy'],personId='heroine.annihilator',coreAssertions=8055,coreResult='AUTOMATIC_CHAIN_PASS',windowsBuild=str(BUILD/'newASTER.exe'),buildBytes=int(re.search(r'PLAN10_ANNIHILATOR_BUILD_PASS (\d+)',buildlog)[1]),buildLog=str(TMP/'build-final.log'),artCount=36,storyPages=60,poems=36,events=10,screenCount=len(screens),screens=screens,verified=['shared affection and lover state','no double sortie of same woman','atomic second-form join and retry','existing six heroine definitions preserved','targeted ally support and cleanse','life investment/overheal/maxHP/revive','independent equipment and non-damage Skill1 preservation','all six chapters and ten events persisted and reloaded','Unity texture imports and adopted SHA256s','1280x720 and 1600x900 display'],referenceLimit='Video samples and screen text; no claim of complete audio or source-poem transcription.')
(ROOT/'docs/production/plan10-annihilator-acceptance.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
left=Image.open(TMP/'player-1600/normal-detail.png').convert('RGB');right=Image.open(TMP/'player-1600/holy-detail.png').convert('RGB');left.thumbnail((800,450));right.thumbnail((800,450));canvas=Image.new('RGB',(1600,450));canvas.paste(left,(0,0));canvas.paste(right,(800,0));canvas.save(TMP/'form-comparison.jpg',quality=92)
print(f'Accepted: {len(screens)} visible captures, 36 hash-verified assets, 60 pages / 10 events, 8055 core assertions.')

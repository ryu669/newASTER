"""Record measured results and make read-only screenshot comparisons."""
import json,re,hashlib
from pathlib import Path
from PIL import Image,ImageStat
ROOT=Path(__file__).resolve().parents[1];TMP=ROOT/'tmp/plan10-shell';BUILD=ROOT/'game/Builds/plan10-shell'
log=(TMP/'build-final.log').read_text(encoding='utf-8-sig');assert 'PLAN10_SHELL_BUILD_PASS' in log and 'PLAN10_SHELL_UNITY_PASS' in log;assert not re.search(r'error CS\d+',log)
core=(TMP/'core-final.log').read_text(encoding='utf-8-sig');count=int(re.search(r'AUTOMATIC_CHAIN_PASS (\d+) assertions',core)[1]);assert 'PLAN10_SHELL_CONTENT_PASS' in core
views=['recruitment','recruit','roster','detail','weapon','formation','setup','setup-pending','setup-save','battle','status','bare','defense','call','repair','guard','attack','garden','chapter','event0','event1','event2','event3','event4','journey'];screens=[]
for view in views:assert (TMP/f'player-1280/shell-{view}.png').exists(),view
for view in ['detail','battle','bare','setup','event2']:assert (TMP/f'player-1600/shell-{view}.png').exists(),view
for p in sorted(TMP.glob('player-*/*.png')):
    text=p.with_suffix('.log').read_text(encoding='utf-8-sig');assert 'PLAN10_SHELL_PLAYER_PASS' in text,p;assert not re.search(r'(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)',text),p
    im=Image.open(p).convert('RGB');st=ImageStat.Stat(im);assert max(st.stddev)>20 and max(st.mean)>20,('Empty capture',p);screens.append(dict(path=str(p),resolution=im.size,pixelMean=st.mean,pixelStddev=st.stddev))
art=json.loads((ROOT/'docs/production/plan10-shell-art-validation.json').read_text(encoding='utf8'));assert len(art['assets'])==19
for row in art['assets']:assert hashlib.sha256(Path(row['path']).read_bytes()).hexdigest()==row['sha256']
report=dict(date='2026-10-06',status='accepted',heroineId='heroine.shell',jobId='job.panzer',formsTotal=9,peopleTotal=8,jobsTotal=8,coreAssertions=count,windowsBuild=str(BUILD/'newASTER.exe'),buildBytes=int(re.search(r'PLAN10_SHELL_BUILD_PASS (\d+)',log)[1]),artCount=19,storyPages=30,poems=18,events=5,screenCount=len(screens),screens=screens,verified=['previous eight forms unchanged','armor rejects ordinary healing','flesh allows only defense','exact Clock300 call, no resurrection of dead flesh','finite two-slot tools never refill on call','wounded flesh carried through successive armor breaks','physical/magic/fire armor selection persisted atomically','source three skills and burning-target synergy','five formation positions','additive saves and idempotent free recruitment','all three chapters and five events persisted/reloaded','19 original images, alpha/import/hash validation','1280x720 and 1600x900'],referenceLimit='Sampled screen evidence; authored voice is separate from untranscribed source audio.')
(ROOT/'docs/production/plan10-shell-acceptance.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
canvas=Image.new('RGB',(1600,900))
for i,key in enumerate(['detail','battle','bare','event2']):
    im=Image.open(TMP/f'player-1600/shell-{key}.png').convert('RGB');im.thumbnail((800,450));canvas.paste(im,((i%2)*800,(i//2)*450))
canvas.save(TMP/'shell-game-comparison.jpg',quality=93)
print(f'Accepted Shell: {count} assertions, {len(screens)} visible captures, nineteen original assets.')

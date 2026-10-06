"""Record measured results and read-only screenshot comparison."""
import json,re,hashlib
from pathlib import Path
from PIL import Image,ImageStat
ROOT=Path(__file__).resolve().parents[1];TMP=ROOT/'tmp/plan10-oriflamme';BUILD=ROOT/'game/Builds/plan10-oriflamme'
log=(TMP/'build-final.log').read_text(encoding='utf-8-sig');assert 'PLAN10_ORIFLAMME_BUILD_PASS' in log and 'PLAN10_ORIFLAMME_UNITY_PASS' in log;assert not re.search(r'error CS\d+',log)
raw=(TMP/'core-final.log').read_bytes();core=raw.decode('utf-16') if raw[:2] in [b'\xff\xfe',b'\xfe\xff'] else raw.decode('utf-8-sig');count=int(re.search(r'AUTOMATIC_CHAIN_PASS (\d+) assertions',core)[1]);assert 'PLAN10_ORIFLAMME_CONTENT_PASS' in core
views=['recruitment','recruit','roster','detail','weapon','formation','battle','status','input','alchemy','weakness','buff','ultimate','attack','garden','chapter','event0','event1','event2','event3','event4','journey'];screens=[]
for view in views:assert (TMP/f'player-1280/oriflamme-{view}.png').exists(),view
for view in ['detail','battle','input','alchemy','weakness','event2']:assert (TMP/f'player-1600/oriflamme-{view}.png').exists(),view
for p in sorted(TMP.glob('player-*/*.png')):
 text=p.with_suffix('.log').read_text(encoding='utf-8-sig');assert 'PLAN10_ORIFLAMME_PLAYER_PASS' in text,p;assert not re.search(r'(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)',text),p
 im=Image.open(p).convert('RGB');st=ImageStat.Stat(im);assert max(st.stddev)>20 and max(st.mean)>20,('Empty capture',p);screens.append(dict(path=str(p),resolution=im.size,pixelMean=st.mean,pixelStddev=st.stddev))
art=json.loads((ROOT/'docs/production/plan10-oriflamme-art-validation.json').read_text(encoding='utf8'));assert len(art['assets'])==18
for row in art['assets']:assert hashlib.sha256(Path(row['path']).read_bytes()).hexdigest()==row['sha256']
report=dict(date='2026-10-06',status='accepted',heroineId='heroine.oriflamme',jobId='job.alchemist',formsTotal=10,peopleTotal=9,jobsTotal=9,coreAssertions=count,windowsBuild=str(BUILD/'newASTER.exe'),buildBytes=int(re.search(r'PLAN10_ORIFLAMME_BUILD_PASS (\d+)',log)[1]),artCount=18,storyPages=30,poems=18,events=5,screenCount=len(screens),screens=screens,verified=['previous nine forms unchanged','five distinct alchemy attributes','resource inputs validated atomically and consumed once','READY Clock and chain preserved by alchemy','target-local fire vulnerability with exact tick expiry','fire amplification does not affect non-fire attacks','source three skills and burning-target synergy','five formation positions','compatible saves and idempotent free recruitment','all three chapters and five events read persisted and reloaded','18 original images alpha import hash validation','1280x720 and 1600x900'],referenceLimit='Sampled screen evidence; authored speech separate from untranscribed source audio.')
(ROOT/'docs/production/plan10-oriflamme-acceptance.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
canvas=Image.new('RGB',(1600,900))
for i,key in enumerate(['detail','input','weakness','event2']):
 im=Image.open(TMP/f'player-1600/oriflamme-{key}.png').convert('RGB');im.thumbnail((800,450));canvas.paste(im,((i%2)*800,(i//2)*450))
canvas.save(TMP/'oriflamme-game-comparison.jpg',quality=93)
print(f'Accepted Oriflamme: {count} assertions, {len(screens)} visible captures, eighteen original assets.')

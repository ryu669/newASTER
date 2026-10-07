"""Record passing UI frames and generate review-only contact sheets."""
import hashlib,json,re,sys
from pathlib import Path
from PIL import Image,ImageDraw,ImageStat
R=Path(__file__).resolve().parents[1]
rows=[]
icons=json.loads((R/'docs/production/job-resource-images.json').read_text(encoding='utf8'))
silhouettes=[]
for icon in icons:
    path=R/f'game/unity/Assets/Game/Resources/UI/Jobs/{icon["job"]}.png'
    assert hashlib.sha256(path.read_bytes()).hexdigest()==icon['sha256'],path
    silhouettes.append(hashlib.sha256(Image.open(path).getchannel('A').tobytes()).hexdigest())
assert len(icons)==len(set(silhouettes))==13,'Job images need distinct silhouettes, independent of color'
for p in sorted((R/'tmp/plan10-ui').glob('player-*/*.png')):
    log=p.with_suffix('.log').read_text(encoding='utf8',errors='replace')
    view=p.stem.removeprefix('ui-')
    assert f'PLAN10_UI_AUDIT_PLAYER_PASS view={view} ' in log,p
    assert not re.search(r'(?m)^(Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)',log),p
    im=Image.open(p).convert('RGB');w,h=im.size
    assert p.parent.name==f'player-{w}x{h}',p
    assert max(ImageStat.Stat(im).stddev)>20,p
    if view=='model':assert max(ImageStat.Stat(im.crop((w//3,h//4,w*2//3,h*3//4))).stddev)>20,'Empty model viewer'
    rows.append(dict(view=view,path=str(p),size=[w,h],sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
if '--complete' in sys.argv:
    defaults=(R/'tools/validate-plan10-ui-player.ps1').read_text(encoding='utf8').split('[int]$Width')[0]
    expected=set(re.findall(r"'([^']+)'",defaults))
    captured={r['view'] for r in rows if r['size']==[1600,900]}
    assert expected<=captured,sorted(expected-captured)
out=R/'tmp/plan10-ui/contacts';out.mkdir(exist_ok=True)
for start in range(0,len(rows),12):
    canvas=Image.new('RGB',(1440,936),(18,30,38));draw=ImageDraw.Draw(canvas)
    for i,row in enumerate(rows[start:start+12]):
        im=Image.open(row['path']).convert('RGB');im.thumbnail((480,270));x=(i%3)*480;y=(i//3)*234
        im.thumbnail((480,212));canvas.paste(im,(x+(480-im.width)//2,y))
        draw.text((x+10,y+215),row['view']+' '+str(row['size']),fill='white')
    canvas.save(out/f'ui-{start//12+1}.jpg',quality=92)
report=dict(date='2026-10-07',captureCount=len(rows),uniqueViews=len(set(r['view'] for r in rows)),physicalInput=0,method='Isolated saves; production screens and operation helpers; visual review of contact sheets and selected full-size frames.',screens=rows,fixes=['Uniform logical canvas and camera viewport preserve aspect ratios.','Portraits, expressions, weapon art and CG retain their aspect ratios.','CG fits above dialogue; weapon branches and hitboxes share coordinates.','Model viewer camera remains visible when opened from heroine screens.','Global help remains visible from heroine screens.','Garden residents and events use clipped scrolling lists; placement stays inside the garden.','Formation overview/member/roster hierarchy reserves one future OOPart per member.','Thirteen job resource images have distinct geometric silhouettes.'],limitations=['OOPart equipment logic is intentionally deferred to a later request.','Capture scenarios use programmatic navigation, not physical mouse input.'])
report['distinctJobImageSilhouettes']=len(set(silhouettes))
report['jobImages']=[dict(job=i['job'],sha256=i['sha256'],alphaSha256=a) for i,a in zip(icons,silhouettes)]
(R/'docs/production/plan10-ui-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(f'UI audit recorded: {len(rows)} frames / {report["uniqueViews"]} views')

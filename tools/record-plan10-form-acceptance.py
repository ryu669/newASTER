"""Record only existing passing logs and exact adopted files, then create a QA montage."""
import json,hashlib,re,sys
from pathlib import Path
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[1];key=sys.argv[1];T=R/f'tmp/plan10-{key}';art=json.loads((R/f'docs/production/plan10-{key}-art-validation.json').read_text(encoding='utf8'))
for row in art['assets']:assert hashlib.sha256(Path(row['path']).read_bytes()).hexdigest()==row['sha256']
screens=[]
for p in T.glob('player-*/*.png'):
 log=p.with_suffix('.log').read_text(encoding='utf8',errors='replace');assert '_PLAYER_PASS view=' in log,p
 assert not re.search(r'(?m)^(?:Exception|InvalidOperationException|ArgumentException|NullReferenceException|ILLUSTRATION_MANIFEST_WARNING)',log),p
 screens.append(dict(path=str(p),size=Image.open(p).size,view=p.stem[len(key)+1:]))
builds=[p for p in T.glob('unity-build*.log') if '_BUILD_PASS' in p.read_text(encoding='utf8',errors='replace')];assert builds,'No passing Unity build';assert screens,'No player screenshots'
out=dict(form=key,artCount=len(art['assets']),pixelEditing=False,buildLogs=[str(p) for p in builds],playerCaptureCount=len(screens),screens=screens,remarks='Screens are isolated acceptance saves. Final art revisions require new captures; older screenshots are retained as QA history.')
(R/f'docs/production/plan10-{key}-acceptance.json').write_text(json.dumps(out,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
canvas=Image.new('RGB',(1600,900),(18,30,38));d=ImageDraw.Draw(canvas)
for i,view in enumerate(['detail','weapon','formation-member','battle']):
 row=next((s for s in reversed(screens) if s['view']==view),None)
 if row is None:continue
 im=Image.open(row['path']);im.thumbnail((800,430));x=(i%2)*800+(800-im.width)//2;y=(i//2)*450;canvas.paste(im,(x,y));d.text(((i%2)*800+14,y+431),view,fill='white')
folder=R/'docs/production/images';folder.mkdir(exist_ok=True);canvas.save(folder/f'{key}-game-comparison.jpg',quality=90);print(key+': exact assets / passing Unity build / '+str(len(screens))+' player captures recorded.')

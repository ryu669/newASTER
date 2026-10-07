"""Adopt exact generated PNGs from a persisted manifest; never edit generated pixels."""
import json,hashlib,shutil,re,uuid,sys
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[1];key=sys.argv[1];RES=ROOT/'game/unity/Assets/Game/Resources/Illustrations';OUT=ROOT/'tmp'/('plan10-'+key);OUT.mkdir(exist_ok=True,parents=True)
manifest=json.loads((ROOT/f'game/art-source/plan10/{key}-generation.json').read_text(encoding='utf8'));report=[];hashes=[]
assert {r['key'] for r in manifest['assets']}=={'standing','portrait','expression-joy','expression-puzzled','expression-determined','attack','hit','cutin','sd-idle','sd-walk','sd-talk','sd-react','equipment-tree-v1',*[f'event-{i}-cg' for i in range(5)]}
for row in manifest['assets']:
 name=key+'-'+row['key']+('' if row['key']=='equipment-tree-v1' else '-candidate-v1')+'.png';dst=RES/name;author=ROOT/'game/art-source/plan10'/key/name;author.parent.mkdir(parents=True,exist_ok=True)
 generated=Path(row['path']);src=author if '--saved-source' in sys.argv[2:] or not generated.exists() else generated
 if not src.exists():raise FileNotFoundError(f'Neither generated nor saved author PNG exists: {name}')
 for target in (dst,author):
  if src.resolve()!=target.resolve():shutil.copy2(src,target)
 meta=Path(str(dst)+'.meta')
 if not meta.exists():meta.write_text(re.sub(r'guid: [a-f0-9]+','guid: '+uuid.uuid4().hex,(RES/'r-standing-candidate-v1.png.meta').read_text(encoding='utf8')),encoding='utf8')
 im=Image.open(dst);alpha=im.getchannel('A').getextrema() if im.mode=='RGBA' else None
 if not row['key'].startswith('event-') and (alpha is None or alpha[0]!=0 or alpha[1]<250):raise ValueError(f'Expected genuine transparent PNG {name}: {alpha}')
 digest=hashlib.sha256(dst.read_bytes()).hexdigest();hashes.append('            {"Illustrations/'+dst.stem+'","'+digest+'"},');report.append(dict(key=row['key'],path=str(dst),sha256=digest,size=im.size,mode=im.mode,alphaExtrema=alpha))
cs=ROOT/'game/unity/Assets/Game/Scripts/Data/ProductionAssetAcceptance.cs';t=cs.read_text(encoding='utf-8-sig');t=re.sub(r'^\s*\{"Illustrations/'+re.escape(key)+r'-[^\n]+\n','',t,flags=re.M);t=t.replace('        });','\n'.join(hashes)+'\n        });');cs.write_text(t,encoding='utf8')
canvas=Image.new('RGB',(1200,1200),(36,42,51));d=ImageDraw.Draw(canvas)
for i,row in enumerate(report):
 im=Image.open(row['path']).convert('RGBA');im.thumbnail((285,220));x=(i%4)*300+(300-im.width)//2;y=(i//4)*240;canvas.paste(im,(x,y),im);d.text(((i%4)*300+8,y+225),row['key'],fill='white')
canvas.save(OUT/'art-contact.jpg');(ROOT/f'docs/production/plan10-{key}-art-validation.json').write_text(json.dumps(dict(tool='builtin ImageGen',count=len(report),pixelEditing=False,assets=report),ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print(key+': 18 exact assets adopted, digests and transparency checked.')

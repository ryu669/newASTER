"""Copy unmodified ImageGen PNGs, bind exact digests, and make read-only QA sheets."""
import json,hashlib,shutil,re,uuid
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[1];RES=ROOT/'game/unity/Assets/Game/Resources/Illustrations';OUT=ROOT/'tmp/plan10-annihilator'
final=ROOT/'game/art-source/plan10/annihilator-generation-final.json'
manifest=json.loads((final if final.exists() else ROOT/'game/art-source/plan10/annihilator-generation.json').read_text(encoding='utf8'))
report=[];hashes=[]
for row in manifest['assets']:
    prefix='annihilator'+('-holy' if row['form']=='holy' else '')
    name=prefix+'-'+row['key']+('' if row['key']=='equipment-tree-v1' else '-candidate-v1')+'.png'
    src=Path(row['path']);dst=RES/name;author=ROOT/'game/art-source/plan10'/prefix/name;author.parent.mkdir(parents=True,exist_ok=True)
    shutil.copy2(src,dst);shutil.copy2(src,author)
    meta=Path(str(dst)+'.meta')
    if not meta.exists():meta.write_text(re.sub(r'guid: [a-f0-9]+','guid: '+uuid.uuid4().hex,(RES/'r-standing-candidate-v1.png.meta').read_text(encoding='utf8')),encoding='utf8')
    im=Image.open(dst);alpha=im.getchannel('A').getextrema() if im.mode=='RGBA' else None
    transparent=not row['key'].startswith('event-')
    if transparent and (alpha is None or alpha[0]!=0 or alpha[1]<250):raise ValueError(f'Expected genuine transparent sprite {name}: {alpha}')
    digest=hashlib.sha256(dst.read_bytes()).hexdigest();resource='Illustrations/'+dst.stem
    hashes.append('            {"'+resource+'","'+digest+'"},')
    report.append(dict(form=row['form'],key=row['key'],path=str(dst),sha256=digest,size=im.size,mode=im.mode,alphaExtrema=alpha))
cs=ROOT/'game/unity/Assets/Game/Scripts/Data/ProductionAssetAcceptance.cs';text=cs.read_text(encoding='utf-8-sig')
text=re.sub(r'^\s*\{"Illustrations/annihilator-[^\n]+\n','',text,flags=re.M)
text=text.replace('        });','\n'.join(hashes)+'\n        });');cs.write_text(text,encoding='utf8')
for form in ['normal','holy']:
    rows=[x for x in report if x['form']==form];canvas=Image.new('RGB',(1200,1200),(36,42,51));d=ImageDraw.Draw(canvas)
    for i,row in enumerate(rows):
        im=Image.open(row['path']).convert('RGBA');im.thumbnail((285,230));x=(i%4)*300+(300-im.width)//2;y=(i//4)*240;canvas.paste(im,(x,y),im);d.text(((i%4)*300+8,y+225),row['key'],fill='white')
    canvas.save(OUT/f'{form}-art-contact.jpg')
(ROOT/'docs/production/plan10-annihilator-art-validation.json').write_text(json.dumps(dict(tool='builtin ImageGen',count=len(report),pixelEditing=False,assets=report),ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('36 unmodified generated assets adopted; transparent sprites checked; QA sheets written.')

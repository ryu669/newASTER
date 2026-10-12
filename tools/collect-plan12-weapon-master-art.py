"""Preserve generated originals and make a review contact sheet, never auto-approve."""
import hashlib,json,re,shutil
from pathlib import Path
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[1];O=R/'game/art-source/weapon-master-v1';O.mkdir(parents=True,exist_ok=True)
if (O/'generation.json').exists():
 previous=json.loads((O/'generation.json').read_text(encoding='utf8'))
 if any(a.get('purposeReview')=='approved' for a in previous.get('assets',[])):
  raise SystemExit('Preserve existing accepted art reviews; collect only into a new version.')
records=[]
for p in sorted((R/'tmp').glob('wm-*-result.json')):
 d=json.loads(p.read_text(encoding='utf8'));key=d['key']
 if key=='tree-ornament':continue
 matches=re.findall(r'[A-Z]:\\[^\n:]*?exec-[a-f0-9-]+\.png',d['output_hint'])
 assert matches,d['output_hint'];source=Path(matches[-1]);dest=O/(key+'-v2.png' if key=='standing' else key+'-v1.png')
 if not dest.exists():shutil.copyfile(source,dest)
 assert dest.read_bytes()==source.read_bytes()
 im=Image.open(dest);assert im.size==(1536,1024),(key,im.size)
 records.append(dict(key=key,path=dest.relative_to(R).as_posix(),originalPath=str(source),sha256=hashlib.sha256(dest.read_bytes()).hexdigest(),prompt=d['prompt'],purposeReview='pending',runtimeReview='pending',width=im.width,height=im.height,mode=im.mode))
g=dict(schemaVersion=1,heroineId='heroine.weapon-master',available=False,assets=records,remainingAssets=20-len(records))
(O/'generation.json').write_text(json.dumps(g,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
cols=3;w=480;h=350;sheet=Image.new('RGB',(cols*w,((len(records)+cols-1)//cols)*h),'#647075');draw=ImageDraw.Draw(sheet)
for i,a in enumerate(records):
 im=Image.open(R/a['path']).convert('RGBA');im.thumbnail((w,h-28));x=(i%cols)*w+(w-im.width)//2;y=(i//cols)*h+25;sheet.paste(im,(x,y),im);draw.text(((i%cols)*w+8,(i//cols)*h+5),a['key'],fill='white')
sheet.save(R/'tmp/plan12-weapon-master/source-review.jpg')
print('WEAPON_MASTER_ART_PRESERVED',len(records),'review pending')

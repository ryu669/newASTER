"""Preserve generated originals verbatim; never equate collection with approval."""
import hashlib,json,shutil
from pathlib import Path
from PIL import Image
R=Path(__file__).resolve().parents[1]
O=R/'game/art-source/estreya-v1'
g=json.loads((O/'generation.json').read_text(encoding='utf8'))
p=json.loads((O/'prompts.json').read_text(encoding='utf8'))
for row in json.loads((R/'tmp/estreya-art-inbox.json').read_text(encoding='utf8')):
 key=row['key'];src=Path(row['source']);dst=O/(key+'.png')
 digest=hashlib.sha256(src.read_bytes()).hexdigest()
 if dst.exists():
  assert hashlib.sha256(dst.read_bytes()).hexdigest()==digest,'Use a versioned asset for replacements'
  continue
 shutil.copyfile(src,dst)
 im=Image.open(dst)
 if not key.startswith('event-'):
  assert im.mode=='RGBA' and im.getchannel('A').histogram()[0]>im.width*im.height*.05,key
 g['assets'].append(dict(key=key,path=dst.relative_to(R).as_posix(),generatedSource=str(src),sha256=digest,width=im.width,height=im.height,purposeReview='pending',runtimeReview='pending'))
 g['remainingAssets'].remove(key)
 p['prompts'].append(dict(key=key,prompt=row['prompt'],reference='game/art-source/estreya-v1/standing.png',transparentBackground=not key.startswith('event-')))
(O/'generation.json').write_text(json.dumps(g,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
(O/'prompts.json').write_text(json.dumps(p,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('ESTREYA_ART_COLLECTED',len(g['assets']),'remaining',len(g['remainingAssets']))

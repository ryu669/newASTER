"""Read-only pixel audit; generated PNG bytes are never edited."""
import hashlib,json,uuid
from pathlib import Path
from PIL import Image
import numpy as np
ROOT=Path(__file__).resolve().parents[1]
ART=ROOT/'game/unity/Assets/Game/Resources/Illustrations'
PROMPTS=ROOT/'game/art-source/plan9/gardens'
GENERATED=Path.home()/'.codex/generated_images/01a104c7-1dd7-7f73-8ccf-1b79338e1d01'
def main():
 sources={hashlib.sha256(p.read_bytes()).hexdigest():str(p)for p in GENERATED.glob('*.png')}
 records=[];template=(ART/'forest-front-candidate-v1.png.meta').read_text()
 for prompt in sorted(PROMPTS.glob('*.prompt.txt')):
  name=prompt.name.removesuffix('.prompt.txt');path=ART/(name+'-candidate-v1.png');data=path.read_bytes();digest=hashlib.sha256(data).hexdigest();assert digest in sources,name+' native source missing'
  with Image.open(path)as im:
   width,height=im.size;alpha=np.asarray(im.convert('RGBA'))[:,:,3];transparent=float(np.mean(alpha==0));opaque=float(np.mean(alpha==255));bounds=Image.fromarray(alpha).getbbox()
   kind='furniture'if name.startswith('garden-')else name.rsplit('-',1)[1]
   assert width>=1000 and height>=700,name+' dimensions'
   if kind=='far':assert opaque>.99,name+' far must be opaque'
   else:assert transparent>.1 and np.max(alpha)>=200,name+' real transparent layer required'
   center=alpha[int(height*.5):int(height*.9),int(width*.2):int(width*.8)]
   center_transparency=float(np.mean(center==0))
  meta=path.with_suffix('.png.meta')
  if not meta.exists():meta.write_text('\n'.join('guid: '+uuid.uuid4().hex if l.startswith('guid: ')else l for l in template.splitlines())+'\n')
  records.append({'id':name,'file':str(path.relative_to(ROOT)).replace('\\','/'),'sha256':digest,'generatedSource':sources[digest],'sourceBytesEqual':True,'prompt':str(prompt.relative_to(ROOT)).replace('\\','/'),'generator':'built-in image_gen','width':width,'height':height,'nativeAlphaPreserved':True,'fullyTransparentFraction':transparent,'opaqueFraction':opaque,'alphaBounds':bounds,'centralLowerTransparency':center_transparency,'status':'candidate-awaiting-runtime-visual-review'})
 assert len(records)==26
 out=ROOT/'docs/production/plan9-garden-art-source-validation.json';out.write_text(json.dumps({'schemaVersion':1,'assets':records,'readonlyPixelAudit':True,'performanceMeasured':False},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 print('PLAN9_GARDEN_ART_SOURCE_PASS',len(records),'native generated assets, no pixel modifications')
if __name__=='__main__':main()

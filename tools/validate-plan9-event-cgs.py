"""Read-only original/resource and unique event CG inspection."""
import argparse
import hashlib
import json
from pathlib import Path
from PIL import Image
parser=argparse.ArgumentParser()
parser.add_argument('hero')
args=parser.parse_args()
root=Path(__file__).resolve().parents[1]
rows=[]
for event in range(5):
    name=f'{args.hero}-event-{event}-cg-candidate-v1.png'
    original=root/'game/art-source/plan9'/args.hero/name
    resource=root/'game/unity/Assets/Game/Resources/Illustrations'/name
    if original.read_bytes()!=resource.read_bytes():
        raise ValueError(f'Original mismatch: {name}')
    with Image.open(resource) as image:
        if abs(image.width/image.height-16/9)>.02:
            raise ValueError(f'Wide CG required: {name}')
        if image.mode=='RGBA' and image.getchannel('A').getextrema()!=(255,255):
            raise ValueError(f'Opaque CG required: {name}')
        rows.append(dict(eventId=f'heroine.{args.hero}.event.{event}',path=resource.relative_to(root).as_posix(),
                         width=image.width,height=image.height,mode=image.mode,
                         sha256=hashlib.sha256(resource.read_bytes()).hexdigest(),status='candidate',finalDeliveryApproved=False))
if len({r['sha256'] for r in rows})!=5:
    raise ValueError('Events reuse identical CGs')
(root/f'docs/production/plan9-{args.hero}-cg-assets.json').write_text(json.dumps(dict(schemaVersion=1,performanceMeasured=False,assets=rows),indent=2)+'\n',encoding='utf-8')
print(f'PLAN9_EVENT_CG_PASS {args.hero} 5 unique opaque wide candidates / originals match')

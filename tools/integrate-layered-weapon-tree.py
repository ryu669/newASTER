"""Export the accepted Iron Maiden prototype to layered Unity resources.
Run author-equipment-tree-bloom-trial.py first. Does not promote draft heroines.
"""
from pathlib import Path
import json, shutil, uuid
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'game/art-source/equipment-tree-layered-trial-v3'
DEST=ROOT/'game/unity/Assets/Game/Resources/UI/WeaponTrees'
DEST.mkdir(parents=True,exist_ok=True)
trial=json.loads((SRC/'layout.json').read_text(encoding='utf-8'))
config=json.loads((DEST/'iron-maiden.json').read_text(encoding='utf-8'))
image=Image.open(SRC/'common-tree.png');used=[]
for node,(x,y) in zip(config['nodes'],trial['nodes']):
    candidates=[]
    for dy in range(-50,51):
        for dx in range(-50,51):
            xx=x+dx*.001;yy=y+dy*.001
            ix=int((xx-.1)/.8*image.width);iy=int((yy-.1)/.8*image.height)
            if not (0<=ix<image.width and 0<=iy<image.height) or image.getpixel((ix,iy))[3]<230:continue
            if any(abs(xx-ux)*906<80 and abs(yy-uy)*604<89 for ux,uy in used):continue
            candidates.append((dx*dx+dy*dy,xx,yy))
    assert candidates,node['nodeId']
    _,xx,yy=min(candidates);used.append((xx,yy));node['position']={'x':xx,'y':yy}
config['foliageAsset']='silver-foliage-simple-v2';config['canopy']=trial['canopySlots'];config['flowers']=trial['flowerSlots']
for i,flower in enumerate(config['flowers']):
    node=config['nodes'][1+(flower['tier']-1)*3+flower['route']]['position']
    flower.update(x=node['x']+(-.038 if i%2==0 else .038),y=node['y']+.020)
config['decorations']=[dict(asset=k,x=x,y=y,size=size*1.85,angle=angle) for k,x,y,size,angle in trial['decorations']]
(DEST/'iron-maiden.json').write_text(json.dumps(config,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
for name in ['common-tree','silver-foliage-simple-v2','crimson-flower','silver-leaf','ruby','iron-clasp']:
    target=DEST/(name+'.png');shutil.copyfile(SRC/target.name,target)
    assert Path(str(target)+'.meta').exists(),'Preserve reviewed Unity texture settings'
print('LAYERED_TREE_EXPORT_PASS Iron Maiden; stable node IDs; roots excluded')

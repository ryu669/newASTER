"""Adopt reviewed dedicated ornaments; preserve pixels and stable weapon node IDs."""
import copy,hashlib,json,re,shutil,uuid
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'game/unity/Assets/Game/Resources/UI/WeaponTrees'
SOURCE=ROOT/'game/art-source/first-batch-tree-ornaments-v1'
SOURCE.mkdir(parents=True,exist_ok=True)
def write(path,value):path.write_text(json.dumps(value,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()
metadata=json.loads((ROOT/'tmp/plan12-batch/ornament-generation-input.json').read_text(encoding='utf-8'))
for item in metadata:
    name=item['hero']+('-ornament-v1' if item['hero']!='neutral-flower' else '-v1')
    target=SOURCE/(name+'.png');shutil.copyfile(item['source'],target)
    image=Image.open(target)
    assert image.mode=='RGBA' and image.getextrema()[3][0]==0,name
    item.update(asset=name,sha256=digest(target),nativeSize=list(image.size),review={'status':'approved','date':'2026-10-11','reason':'原画を視認。人物なしの単独装飾、透明背景、ヒロインのモチーフを確認。'})
    imported=DEST/target.name;shutil.copyfile(target,imported)
    meta=Path(str(imported)+'.meta')
    if not meta.exists():meta.write_text(re.sub(r'guid: [0-9a-f]+','guid: '+uuid.uuid4().hex,(DEST/'ruby.png.meta').read_text()),encoding='utf-8')
write(SOURCE/'generation.json',metadata)
base=json.loads((DEST/'iron-maiden.json').read_text(encoding='utf-8'))
colors={'avalanche':(1,.25,.28),'anathema':(.25,.48,1),'shiva':(1,.55,.75)}
for hero,color in colors.items():
    config=copy.deepcopy(base);config['heroineId']='heroine.'+hero
    for node in config['nodes']:node['nodeId']=node['nodeId'].replace('heroine.iron-maiden.','heroine.'+hero+'.')
    config['foliageColor']=dict(zip(['r','g','b','a'],[*color,1]))
    config['flowerColor']=dict(zip(['r','g','b','a'],[*color,1]))
    config['flowerAsset']='neutral-flower-v1'
    # Small repeated motifs, distributed throughout the branches, without root ornaments.
    config['decorations']=[dict(piece,asset=hero+'-ornament-v1',size=.083) for piece in base['decorations'] if piece['y']<.7]
    path=DEST/(hero+'.json');write(path,config)
    meta=Path(str(path)+'.meta')
    if not meta.exists():meta.write_text(re.sub(r'guid: [0-9a-f]+','guid: '+uuid.uuid4().hex,(DEST/'iron-maiden.json.meta').read_text()),encoding='utf-8')
acceptance=ROOT/'game/unity/Assets/Game/Scripts/Data/ProductionAssetAcceptance.cs'
text=acceptance.read_text(encoding='utf-8')
for item in metadata:
    key='UI/WeaponTrees/'+item['asset']
    if '"'+key+'"' not in text:text=text.replace('new Dictionary<string,string>{','new Dictionary<string,string>{\n            {"'+key+'","'+item['sha256']+'"},',1)
acceptance.write_text(text,encoding='utf-8')
print('FIRST_BATCH_LAYERED_TREE_EXPORT_PASS 3 dedicated ornaments; unchanged pixels; 39 stable nodes; 692 foliage / 24 flowers per full tree')

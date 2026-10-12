"""Copy reviewed originals unchanged, wire dedicated art and stage the next catalog."""
import copy,hashlib,json,shutil,uuid
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];RES=ROOT/'game/unity/Assets/Game/Resources'
SRC=ROOT/'game/art-source/invisible-v1';SRC.mkdir(parents=True,exist_ok=True)
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def meta(p):
    m=Path(str(p)+'.meta')
    if not m.exists():m.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
records=read(ROOT/'tmp/plan12-invisible-generation-input.json');assert len(records)==21
assets=[];mapping={}
for a in records:
    key=a['key'];source=SRC/(key+'.png');shutil.copyfile(a['path'],source)
    dest=RES/('UI/WeaponTrees/invisible-ornament-v1.png' if key=='tree-ornament' else 'Illustrations/'+('heroine-portrait-invisible-v1' if key=='portrait' else 'invisible-'+key+'-candidate-v1')+'.png')
    shutil.copyfile(source,dest);meta(dest)
    resource=dest.relative_to(RES).as_posix()[:-4];mapping[key]=resource
    assets.append(dict(key=key,path=source.relative_to(ROOT).as_posix(),generatedFrom=a['path'],prompt=a['prompt'],sha256=digest(source),resourcePath=resource,purposeReview='approved',purposeEvidence='Original inspected individually: adult heroine identity, distinct purpose/pose, no males in five CGs; ornament contains no person',runtimeReview='pending'))
write(SRC/'generation.json',dict(tool='built-in-imagegen',pixelEdits=False,assets=assets))
write(RES/'Illustrations/invisible-battle-binding.json',dict(heroineId='heroine.invisible',placeholder=False,fullCanvas=True,resourcePath=mapping['standing'],attackResourcePath=mapping['attack'],hitResourcePath=mapping['hit'],cutinResourcePath=mapping['cutin']))
framing=read(RES/'UI/heroine-portrait-framing.json');framing['entries']=[e for e in framing['entries'] if e['heroineId']!='heroine.invisible']+[dict(heroineId='heroine.invisible',resourcePath=mapping['portrait'],faceCenterX=.49,faceCenterY=.27,faceHeight=.28,legacyPortrait=False)];write(RES/'UI/heroine-portrait-framing.json',framing)
tree=read(RES/'UI/WeaponTrees/shiva.json')
tree=json.loads(json.dumps(tree).replace('heroine.shiva','heroine.invisible'));tree['foliageColor']=dict(r=1,g=.64,b=.80,a=1);tree['flowerColor']=dict(r=1,g=.46,b=.70,a=1)
for piece in tree['decorations']:
    piece['asset']='invisible-ornament-v1';piece['size']=.105
write(RES/'UI/WeaponTrees/invisible.json',tree)
trees=read(RES/'UI/heroine-weapon-trees.json');entry=copy.deepcopy(next(e for e in trees['entries'] if e['heroineId']=='heroine.shiva'));entry['heroineId']='heroine.invisible';entry['nodes']=tree['nodes'];entry['resourcePath']='UI/WeaponTrees/common-tree';trees['entries']=[e for e in trees['entries'] if e['heroineId']!='heroine.invisible']+[entry];write(RES/'UI/heroine-weapon-trees.json',trees)
accept=ROOT/'game/unity/Assets/Game/Scripts/Data/ProductionAssetAcceptance.cs';s=accept.read_text(encoding='utf-8')
for a in assets:
    row='            {"'+a['resourcePath']+'","'+a['sha256']+'"},'
    if '"'+a['resourcePath']+'"' not in s:s=s.replace('        });',row+'\n        });')
accept.write_text(s,encoding='utf-8')
for name,resource in [('combat.json','Combat/battle-plan12-invisible.json'),('story.json','Story/plan12-invisible-story-content.json')]:
    dest=RES/resource;shutil.copyfile(ROOT/'game/unity/Assets/Game/ContentDrafts/Invisible'/name,dest);meta(dest)
meta(RES/'UI/WeaponTrees/invisible.json');meta(RES/'Illustrations/invisible-battle-binding.json')
print('INVISIBLE_ART_ADOPTED 21 unchanged originals; staged catalog 21 forms; runtime review pending')

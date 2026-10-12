"""Adopt only individually reviewed WeaponMaster originals; keep normal paths unchanged."""
import copy,hashlib,json,shutil,uuid
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];RES=ROOT/'game/unity/Assets/Game/Resources';SRC=ROOT/'game/art-source/weapon-master-v1'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def meta(p):
 m=Path(str(p)+'.meta')
 if not m.exists():m.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf8')
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=read(SRC/'generation.json');assert len(g['assets'])==20 and not g['remainingAssets']
mapping={}
for a in g['assets']:
 assert a['purposeReview']=='approved',a['key'];source=ROOT/a['path'];assert digest(source)==a['sha256']
 key=a['key'];dest=RES/(a['resourcePath']+'.png') if a.get('resourcePath') else RES/('UI/WeaponTrees/weapon-master-ornament-v1.png' if key=='tree-ornament' else 'Illustrations/'+('heroine-portrait-weapon-master-v1' if key=='portrait' else 'weapon-master-'+key+'-candidate-v1')+'.png')
 if dest.exists():assert digest(dest)==a['sha256'],'Use a new version instead of overwriting art'
 else:shutil.copyfile(source,dest)
 meta(dest);resource=dest.relative_to(RES).as_posix()[:-4];a['resourcePath']=resource;mapping[key]=resource
write(SRC/'generation.json',g)
write(RES/'Illustrations/weapon-master-battle-binding.json',dict(heroineId='heroine.weapon-master',placeholder=False,fullCanvas=True,resourcePath=mapping['standing'],attackResourcePath=mapping['attack'],hitResourcePath=mapping['hit'],cutinResourcePath=mapping['cutin']))
framing=read(RES/'UI/heroine-portrait-framing.json');framing['entries']=[e for e in framing['entries'] if e['heroineId']!='heroine.weapon-master']+[dict(heroineId='heroine.weapon-master',resourcePath=mapping['portrait'],faceCenterX=.49,faceCenterY=.17,faceHeight=.20,legacyPortrait=False)];write(RES/'UI/heroine-portrait-framing.json',framing)
tree=read(RES/'UI/WeaponTrees/viper.json');tree=json.loads(json.dumps(tree).replace('heroine.viper','heroine.weapon-master'));tree['foliageColor']=dict(r=.60,g=.78,b=.24,a=1);tree['flowerColor']=dict(r=.78,g=.94,b=.35,a=1)
tree['decorations']=[]
write(RES/'UI/WeaponTrees/weapon-master.json',tree)
trees=read(RES/'UI/heroine-weapon-trees.json');entry=copy.deepcopy(next(e for e in trees['entries'] if e['heroineId']=='heroine.viper'));entry.update(heroineId='heroine.weapon-master',nodes=tree['nodes'],resourcePath='UI/WeaponTrees/common-tree');trees['entries']=[e for e in trees['entries'] if e['heroineId']!='heroine.weapon-master']+[entry];write(RES/'UI/heroine-weapon-trees.json',trees)
accept=ROOT/'game/unity/Assets/Game/Scripts/Data/ProductionAssetAcceptance.cs';s=accept.read_text(encoding='utf8')
for a in g['assets']:
 row='            {"'+a['resourcePath']+'","'+a['sha256']+'"},'
 if '"'+a['resourcePath']+'"' not in s:s=s.replace('        });',row+'\n        });')
accept.write_text(s,encoding='utf8')
for name,resource in [('combat.json','Combat/battle-plan12-weapon-master.json'),('story.json','Story/plan12-weapon-master-story-content.json')]:
 dest=RES/resource;shutil.copyfile(ROOT/'game/unity/Assets/Game/ContentDrafts/WeaponMaster'/name,dest);meta(dest)
meta(RES/'UI/WeaponTrees/weapon-master.json');meta(RES/'Illustrations/weapon-master-battle-binding.json')
print('WEAPON_MASTER_ART_ADOPTED 20 unchanged reviewed originals; preview 24 forms; normal play still 23')

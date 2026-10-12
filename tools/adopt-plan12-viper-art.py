"""Adopt only individually reviewed Viper originals; keep normal paths unchanged."""
import copy,hashlib,json,shutil,uuid
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];RES=ROOT/'game/unity/Assets/Game/Resources';SRC=ROOT/'game/art-source/viper-v1'
def read(p):return json.loads(p.read_text(encoding='utf-8-sig'))
def write(p,d):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def meta(p):
 m=Path(str(p)+'.meta')
 if not m.exists():m.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf8')
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
g=read(SRC/'generation.json');assert len(g['assets'])==21 and not g['remainingAssets']
mapping={}
for a in g['assets']:
 assert a['purposeReview']=='approved',a['key'];source=ROOT/a['path'];assert digest(source)==a['sha256']
 key=a['key'];dest=RES/('UI/WeaponTrees/viper-ornament-v1.png' if key=='tree-ornament' else 'Illustrations/'+('heroine-portrait-viper-v1' if key=='portrait' else 'viper-'+key+'-candidate-v1')+'.png')
 if dest.exists():assert digest(dest)==a['sha256'],'Use a new version instead of overwriting art'
 else:shutil.copyfile(source,dest)
 meta(dest);resource=dest.relative_to(RES).as_posix()[:-4];a['resourcePath']=resource;mapping[key]=resource
write(SRC/'generation.json',g)
write(RES/'Illustrations/viper-battle-binding.json',dict(heroineId='heroine.viper',placeholder=False,fullCanvas=True,resourcePath=mapping['standing'],attackResourcePath=mapping['attack'],hitResourcePath=mapping['hit'],cutinResourcePath=mapping['cutin']))
framing=read(RES/'UI/heroine-portrait-framing.json');framing['entries']=[e for e in framing['entries'] if e['heroineId']!='heroine.viper']+[dict(heroineId='heroine.viper',resourcePath=mapping['portrait'],faceCenterX=.49,faceCenterY=.29,faceHeight=.28,legacyPortrait=False)];write(RES/'UI/heroine-portrait-framing.json',framing)
tree=read(RES/'UI/WeaponTrees/invisible.json');tree=json.loads(json.dumps(tree).replace('heroine.invisible','heroine.viper'));tree['foliageColor']=dict(r=1,g=.78,b=.38,a=1);tree['flowerColor']=dict(r=1,g=.87,b=.58,a=1)
for piece in tree['decorations']:piece['asset']='viper-ornament-v1';piece['size']=.105
write(RES/'UI/WeaponTrees/viper.json',tree)
trees=read(RES/'UI/heroine-weapon-trees.json');entry=copy.deepcopy(next(e for e in trees['entries'] if e['heroineId']=='heroine.invisible'));entry.update(heroineId='heroine.viper',nodes=tree['nodes'],resourcePath='UI/WeaponTrees/common-tree');trees['entries']=[e for e in trees['entries'] if e['heroineId']!='heroine.viper']+[entry];write(RES/'UI/heroine-weapon-trees.json',trees)
accept=ROOT/'game/unity/Assets/Game/Scripts/Data/ProductionAssetAcceptance.cs';s=accept.read_text(encoding='utf8')
for a in g['assets']:
 row='            {"'+a['resourcePath']+'","'+a['sha256']+'"},'
 if '"'+a['resourcePath']+'"' not in s:s=s.replace('        });',row+'\n        });')
accept.write_text(s,encoding='utf8')
for name,resource in [('combat.json','Combat/battle-plan12-viper.json'),('story.json','Story/plan12-viper-story-content.json')]:
 dest=RES/resource;shutil.copyfile(ROOT/'game/unity/Assets/Game/ContentDrafts/Viper'/name,dest);meta(dest)
meta(RES/'UI/WeaponTrees/viper.json');meta(RES/'Illustrations/viper-battle-binding.json')
print('VIPER_ART_ADOPTED 21 unchanged reviewed originals; preview 22 forms; normal play still 21')

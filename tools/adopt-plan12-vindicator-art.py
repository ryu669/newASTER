"""Adopt only individually reviewed Vindicator originals; keep normal paths unchanged."""
import copy,hashlib,json,shutil,uuid
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];RES=ROOT/'game/unity/Assets/Game/Resources';SRC=ROOT/'game/art-source/vindicator-v1'
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
 key=a['key'];dest=RES/(a['resourcePath']+'.png') if a.get('resourcePath') else RES/('UI/WeaponTrees/vindicator-ornament-v1.png' if key=='tree-ornament' else 'Illustrations/'+('heroine-portrait-vindicator-v1' if key=='portrait' else 'vindicator-'+key+'-candidate-v1')+'.png')
 if dest.exists():assert digest(dest)==a['sha256'],'Use a new version instead of overwriting art'
 else:shutil.copyfile(source,dest)
 meta(dest);resource=dest.relative_to(RES).as_posix()[:-4];a['resourcePath']=resource;mapping[key]=resource
write(SRC/'generation.json',g)
write(RES/'Illustrations/vindicator-battle-binding.json',dict(heroineId='heroine.vindicator',placeholder=False,fullCanvas=True,resourcePath=mapping['standing'],attackResourcePath=mapping['attack'],hitResourcePath=mapping['hit'],cutinResourcePath=mapping['cutin']))
framing=read(RES/'UI/heroine-portrait-framing.json');framing['entries']=[e for e in framing['entries'] if e['heroineId']!='heroine.vindicator']+[dict(heroineId='heroine.vindicator',resourcePath=mapping['portrait'],faceCenterX=.55,faceCenterY=.27,faceHeight=.25,legacyPortrait=False)];write(RES/'UI/heroine-portrait-framing.json',framing)
tree=read(RES/'UI/WeaponTrees/viper.json');tree=json.loads(json.dumps(tree).replace('heroine.viper','heroine.vindicator'));tree['foliageColor']=dict(r=.53,g=.67,b=.78,a=1);tree['flowerColor']=dict(r=.65,g=.85,b=1,a=1)
for piece in tree['decorations']:piece['asset']='vindicator-ornament-v1';piece['size']=.105
write(RES/'UI/WeaponTrees/vindicator.json',tree)
trees=read(RES/'UI/heroine-weapon-trees.json');entry=copy.deepcopy(next(e for e in trees['entries'] if e['heroineId']=='heroine.viper'));entry.update(heroineId='heroine.vindicator',nodes=tree['nodes'],resourcePath='UI/WeaponTrees/common-tree');trees['entries']=[e for e in trees['entries'] if e['heroineId']!='heroine.vindicator']+[entry];write(RES/'UI/heroine-weapon-trees.json',trees)
accept=ROOT/'game/unity/Assets/Game/Scripts/Data/ProductionAssetAcceptance.cs';s=accept.read_text(encoding='utf8')
for a in g['assets']:
 row='            {"'+a['resourcePath']+'","'+a['sha256']+'"},'
 if '"'+a['resourcePath']+'"' not in s:s=s.replace('        });',row+'\n        });')
accept.write_text(s,encoding='utf8')
for name,resource in [('combat.json','Combat/battle-plan12-vindicator.json'),('story.json','Story/plan12-vindicator-story-content.json')]:
 dest=RES/resource;shutil.copyfile(ROOT/'game/unity/Assets/Game/ContentDrafts/Vindicator'/name,dest);meta(dest)
meta(RES/'UI/WeaponTrees/vindicator.json');meta(RES/'Illustrations/vindicator-battle-binding.json')
print('VINDICATOR_ART_ADOPTED 21 unchanged reviewed originals; preview 23 forms; normal play still 22')

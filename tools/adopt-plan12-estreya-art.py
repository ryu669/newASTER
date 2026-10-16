"""Adopt only individually reviewed Estreya originals; keep normal paths unchanged."""
import copy,hashlib,json,shutil,uuid
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];RES=ROOT/'game/unity/Assets/Game/Resources';SRC=ROOT/'game/art-source/estreya-v1'
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
 key=a['key'];dest=RES/(a['resourcePath']+'.png') if a.get('resourcePath') else RES/('UI/WeaponTrees/estreya-ornament-v1.png' if key=='tree-ornament' else 'Illustrations/'+('heroine-portrait-estreya-v1' if key=='portrait' else 'estreya-'+key+'-candidate-v1')+'.png')
 if dest.exists():assert digest(dest)==a['sha256'],'Use a new version instead of overwriting art'
 else:shutil.copyfile(source,dest)
 meta(dest);resource=dest.relative_to(RES).as_posix()[:-4];a['resourcePath']=resource;mapping[key]=resource
write(SRC/'generation.json',g)
write(RES/'Illustrations/estreya-battle-binding.json',dict(heroineId='heroine.estreya',placeholder=False,fullCanvas=True,resourcePath=mapping['standing'],attackResourcePath=mapping['attack'],hitResourcePath=mapping['hit'],cutinResourcePath=mapping['cutin']))
framing=read(RES/'UI/heroine-portrait-framing.json');framing['entries']=[e for e in framing['entries'] if e['heroineId']!='heroine.estreya']+[dict(heroineId='heroine.estreya',resourcePath=mapping['portrait'],faceCenterX=.54,faceCenterY=.32,faceHeight=.22,legacyPortrait=False)];write(RES/'UI/heroine-portrait-framing.json',framing)
colors=read(RES/'UI/WeaponTrees/heroine-colors.json');colors['entries']=[e for e in colors['entries'] if e['heroineId']!='heroine.estreya']+[dict(heroineId='heroine.estreya',foliageColor=dict(r=.88,g=.38,b=.14,a=1),flowerColor=dict(r=1,g=.68,b=.22,a=1))];write(RES/'UI/WeaponTrees/heroine-colors.json',colors)
trees=read(RES/'UI/heroine-weapon-trees.json');entry=copy.deepcopy(next(e for e in trees['entries'] if e['heroineId']=='heroine.weapon-master'));entry=json.loads(json.dumps(entry).replace('heroine.weapon-master','heroine.estreya'));trees['entries']=[e for e in trees['entries'] if e['heroineId']!='heroine.estreya']+[entry];write(RES/'UI/heroine-weapon-trees.json',trees)
accept=ROOT/'game/unity/Assets/Game/Scripts/Data/ProductionAssetAcceptance.cs';s=accept.read_text(encoding='utf8')
for a in g['assets']:
 row='            {"'+a['resourcePath']+'","'+a['sha256']+'"},'
 if '"'+a['resourcePath']+'"' not in s:s=s.replace('        });',row+'\n        });')
accept.write_text(s,encoding='utf8')
for name,resource in [('combat.json','Combat/battle-plan12-estreya.json'),('story.json','Story/plan12-estreya-story-content.json')]:
 dest=RES/resource;shutil.copyfile(ROOT/'game/unity/Assets/Game/ContentDrafts/Estreya'/name,dest);meta(dest)
meta(RES/'Illustrations/estreya-battle-binding.json')
print('ESTREYA_ART_ADOPTED 20 unchanged reviewed originals; preview 26 forms; normal play still 25')

"""Run only after manually reviewing the captured tree frames for every form."""
import hashlib,json
from pathlib import Path
from PIL import Image
R=Path(__file__).resolve().parents[1];BASE=R/'tmp/plan12-common-tree/player'
def read(p):return json.loads((R/p).read_text(encoding='utf-8-sig'))
def digest(p):return hashlib.sha256((R/p).read_bytes()).hexdigest()
def write(p,d):(R/p).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
catalog=read('game/unity/Assets/Game/Resources/UI/heroine-weapon-trees.json')
components=['game/unity/Assets/Game/Resources/UI/WeaponTrees/'+s for s in ['common-layout.json','heroine-colors.json','common-tree.png','silver-foliage-simple-v2.png','neutral-flower-v1.png']]+['game/unity/Assets/Game/Resources/UI/heroine-weapon-trees.json','game/unity/Assets/Game/Scripts/Presentation/LayeredWeaponTree.cs']
frames=[]
for e in catalog['entries']:
 for stage in (0,2,4):
  name=e['heroineId'][8:]+'-tree-stage-'+str(stage);p=BASE/(name+'.png')
  assert (BASE/(name+'.complete')).exists(),name
  assert Image.open(p).size==(1920,1080)
  log=(BASE/'sweep.log').read_text(encoding='utf8');assert 'COMMON_TREE_SAVE_PASS hero='+e['heroineId']+' stage='+str(stage) in log
  relative=p.relative_to(R).as_posix();frames.append(dict(heroineId=e['heroineId'],stage=stage,path=relative,sha256=digest(relative),status='approved'))
record='docs/production/plan12-common-weapon-tree-review.json'
write(record,dict(status='approved',forms=len(catalog['entries']),construction='one common wooden tree/layout/leaves/flowers/round node frame; color only per form; no hanging ornaments',resolution='default 1920x1080',method='manual review of all tree captures; saved node stage reloaded in actual Player; no repeated review of unchanged heroine art',components=[dict(path=p,sha256=digest(p),status='approved') for p in components],captures=frames,unresolved=[]))
for hero in ['invisible','viper','vindicator']:
 path='docs/production/plan12-'+hero+'-form-record.json';r=read(path)
 for item in r['renderers']:
  if item['path'].endswith('/LayeredWeaponTree.cs'):
   item['previousReviewedSha256']=item['sha256'];item['sha256']=digest(item['path']);item['reviewRecord']=record
 r['commonTreeReview']=record
 for p in components:
  if not p.endswith('.cs') and not any(c['path']==p for c in r['treeComponents']):r['treeComponents'].append(dict(path=p,sha256=digest(p),status='approved',reviewRecord=record))
 write(path,r)
scope='docs/production/plan12-weapon-master-renderer-scope.json';s=read(scope);s['supersededTreeRendererProof']=record;s['note']='Heroine-only registration proof remains historical; shared tree renderer changed subsequently and was visually reviewed for all forms.';write(scope,s)
print('COMMON_WEAPON_TREE_REVIEW_RECORDED forms='+str(len(catalog['entries']))+' captures='+str(len(frames)))

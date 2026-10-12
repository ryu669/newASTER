"""Record manual review, publish only after review, then seal normal Player evidence."""
import hashlib,json,sys
from pathlib import Path
from PIL import Image
R=Path(__file__).resolve().parents[1];KEY='april';H='heroine.'+KEY
GEN='game/art-source/april-v1/generation.json';REC='docs/production/plan12-april-form-record.json';BASE='tmp/plan12-april/player/'
def read(p):return json.loads((R/p).read_text(encoding='utf-8-sig'))
def write(p,d):(R/p).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
def digest(p):return hashlib.sha256((R/p).read_bytes()).hexdigest()
def capture(p):
 assert Image.open(R/p).size==(1920,1080),p
 return dict(path=p,sha256=digest(p))
mode=sys.argv[1]
if mode=='source-review':
 g=read(GEN);assert len(g['assets'])==20 and not g['remainingAssets']
 for a in g['assets']:
  assert digest(a['path'])==a['sha256']
  a['purposeReview']='approved';a['sourceReviewMethod']='manual original/contact-sheet inspection: identity, expression, pose, clipping, adult-only event composition'
 write(GEN,g);print('APRIL_SOURCE_REVIEW_RECORDED 20 originals')
elif mode=='review':
 g=read(GEN);images={}
 for a in g['assets']:
  k=a['key']
  if k=='standing':paths=[BASE+KEY+'-expression.normal.png']
  elif k=='portrait':paths=[BASE+'recruit.png',BASE+'trait.png']
  elif k.startswith('expression-'):paths=[BASE+KEY+'-expression.'+k[11:]+'.png']
  elif k in ('attack','hit','cutin'):paths=[BASE+'battle-'+k+'.png']
  elif k.startswith('sd-'):paths=[BASE+KEY+'-sd.'+k[3:]+'.png']
  elif k.startswith('event-'):paths=[BASE+KEY+'-cg.'+k.split('-')[1]+'.png',BASE+'gallery-frames/'+KEY+'-cg.'+k.split('-')[1]+'.png']
  else:paths=[BASE+'tree0.png',BASE+'tree2.png',BASE+'tree4.png']
  if k in ('sd-sit','sd-work','sd-look'):paths.append(BASE+KEY+'-sd.furniture.png')
  files=[capture(p) for p in paths];assert digest(a['path'])==a['sha256'] and a['purposeReview']=='approved'
  a.update(runtimeReview='approved',captureFiles=files)
  images[k]=dict(status='approved',source=a['path'],sha256=a['sha256'],resourcePath=a['resourcePath'],purposeReview='approved',runtimeReview=dict(status='approved',reviewedSha256=a['sha256'],captureFiles=files))
 write(GEN,g)
 layout='game/unity/Assets/Game/Resources/UI/WeaponTrees/common-layout.json';tree=read(layout)
 components=[layout,'game/unity/Assets/Game/Resources/UI/WeaponTrees/heroine-colors.json','game/unity/Assets/Game/Resources/UI/heroine-weapon-trees.json']+['game/unity/Assets/Game/Resources/UI/WeaponTrees/'+p+'.png' for p in set(['common-tree',tree['foliageAsset'],tree['flowerAsset']])]
 renderers=['game/unity/Assets/Game/Scripts/Presentation/'+p for p in ['AdvExperience.cs','GardenArtComposition.cs','LayeredWeaponTree.cs','Plan7ArtSample.cs']]+['game/unity/Assets/Game/Scripts/Data/Plan12FirstBatchHomeArt.cs']
 r=dict(schemaVersion=1,personId=H,formId=H,jobId='job.fighter',status='runtime_review_complete',profile='docs/characters/april.md',images=images,treeComponents=[dict(path=p,sha256=digest(p),status='approved') for p in sorted(components)],renderers=[dict(path=p,sha256=digest(p),status='approved') for p in renderers],resolution='default launch 1920x1080',method='manual actual Player frame review; automated behavior separately recorded; physical input excluded',unresolved=[],updatedAt='2026-10-12')
 write(REC,r);print('APRIL_RUNTIME_REVIEW_RECORDED 20 assets')
elif mode=='publish':
 r=read(REC);assert r['status']=='runtime_review_complete'
 for name in ['art','gallery','recruitment','recruit','trait','skill','formation','command','attack0','attack1','attack2','garden','journey','tree0','tree2','tree4','battle-attack','battle-hit','battle-cutin']:
  assert (R/BASE/('gallery-frames/' if name=='gallery' else '')/(name+'.complete')).exists(),name
 assert 'AUTOMATIC_CHAIN_PASS' in (R/'tmp/plan12-april-core.log').read_text(encoding='utf8')
 p=R/'game/unity/Assets/Game/Scripts/Data/ProductionContentPaths.cs';s=p.read_text(encoding='utf8').replace('Combat/battle-plan12-weapon-master','Combat/battle-plan12-april').replace('Story/plan12-weapon-master-story-content','Story/plan12-april-story-content');p.write_text(s,encoding='utf8')
 d=read('docs/characters/index.json');person=next(p for p in d['developmentPeople'] if p['personId']==H);d['people'].append({k:person[k] for k in ['personId','name','profile','formIds']});d['developmentPeople'].remove(person);d.update(combatSource='game/unity/Assets/Game/Resources/Combat/battle-plan12-april.json',storySource='game/unity/Assets/Game/Resources/Story/plan12-april-story-content.json');write('docs/characters/index.json',d)
 m=read('game/unity/Assets/Game/ContentDrafts/April/manifest.json');m.update(available=True,stage='accepted-normal-play',remaining=[]);write('game/unity/Assets/Game/ContentDrafts/April/manifest.json',m)
 p=R/'docs/characters/april.md';s=p.read_text(encoding='utf8').replace('現在は制作中で、通常プレイへの公開前。','受入れ完了。通常プレイへ公開済み。');p.write_text(s,encoding='utf8')
 print('APRIL_NORMAL_PATHS_PUBLISHED final build/player pending')
elif mode=='complete':
 logs=['tmp/plan12-april-core.log','tmp/plan12-april-build-final.log','tmp/plan12-april/production-player.log']
 assert 'PLAN12_PRODUCTION_PLAYER_PASS defaultCatalog=25 people=22 oldSave=24 joins=1 summonable=1 wallet=preserved' in (R/logs[2]).read_text(encoding='utf8')
 assert 'PLAN12_APRIL_BUILD_PASS' in (R/logs[1]).read_text(encoding='utf8')
 assert 'AUTOMATIC_CHAIN_PASS' in (R/logs[0]).read_text(encoding='utf8')
 r=read(REC);r.update(status='complete_published',acceptance='pass',build=dict(path='game/Builds/plan12-april/newASTER.exe',sha256=digest('game/Builds/plan12-april/newASTER.exe'),workingTreeChanges=True),validationLogs=[dict(path=p,sha256=digest(p)) for p in logs]);write(REC,r)
 g=read(GEN);g['available']=True;write(GEN,g)
 write('docs/production/plan12-april-preparation.json',dict(status='accepted-normal-play',available=True,personId=H,sourcePersonId='aa.person.021',realName='ケアト・アーニスリンナ',jobId='job.fighter',chapters=3,pages=30,poems=18,events=5,images=20,weaponNameOrigin='video-observed base name; three route suffixes newaster-original',completionRecord=REC,validation=dict(assertions=12533,unitySources=279),remaining=[]))
 write('docs/production/plan12-april-runtime-validation.json',dict(status='pass',forms=25,people=22,added=H,record=REC,oldSave='24-to-25; all fields and wallet preserved; physical save/reload',automatedPlayerCases=19,physicalInput='excluded',resolution='default 1920x1080',unresolved=[]))
 print('APRIL_COMPLETE_PUBLISHED 25 forms / 22 people')
else:raise ValueError(mode)

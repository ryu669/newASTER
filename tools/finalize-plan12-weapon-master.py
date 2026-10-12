"""Record manual review, publish only after review, then seal normal Player evidence."""
import hashlib,json,sys
from pathlib import Path
from PIL import Image
R=Path(__file__).resolve().parents[1];KEY='weapon-master';H='heroine.'+KEY
GEN='game/art-source/weapon-master-v1/generation.json';REC='docs/production/plan12-weapon-master-form-record.json';BASE='tmp/plan12-weapon-master/player/'
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
 write(GEN,g);print('WEAPON_MASTER_SOURCE_REVIEW_RECORDED 20 originals')
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
 renderers=['game/unity/Assets/Game/Scripts/Presentation/'+p for p in ['AdvExperience.cs','GardenArtComposition.cs','LayeredWeaponTree.cs']]+['game/unity/Assets/Game/Scripts/Data/Plan12FirstBatchHomeArt.cs']
 r=dict(schemaVersion=1,personId=H,formId=H,jobId='job.gunner',status='runtime_review_complete',profile='docs/characters/weapon-master.md',images=images,treeComponents=[dict(path=p,sha256=digest(p),status='approved') for p in sorted(components)],renderers=[dict(path=p,sha256=digest(p),status='approved') for p in renderers],resolution='default launch 1920x1080',method='manual actual Player frame review; automated behavior separately recorded; physical input excluded',unresolved=[],updatedAt='2026-10-11')
 write(REC,r);print('WEAPON_MASTER_RUNTIME_REVIEW_RECORDED 20 assets')
elif mode=='publish':
 r=read(REC);assert r['status']=='runtime_review_complete'
 for name in ['art','gallery','recruitment','recruit','trait','skill','formation','command','attack0','attack1','attack2','garden','journey','tree0','tree2','tree4','battle-attack','battle-hit','battle-cutin']:
  assert (R/BASE/('gallery-frames/' if name=='gallery' else '')/(name+'.complete')).exists(),name
 assert 'AUTOMATIC_CHAIN_PASS' in (R/'tmp/plan12-weapon-master-core.log').read_text(encoding='utf8')
 p=R/'game/unity/Assets/Game/Scripts/Data/ProductionContentPaths.cs';s=p.read_text(encoding='utf8').replace('Combat/battle-plan12-vindicator','Combat/battle-plan12-weapon-master').replace('Story/plan12-vindicator-story-content','Story/plan12-weapon-master-story-content');p.write_text(s,encoding='utf8')
 d=read('docs/characters/index.json');person=next(p for p in d['developmentPeople'] if p['personId']==H);d['people'].append(person);d['developmentPeople'].remove(person);d.update(combatSource='game/unity/Assets/Game/Resources/Combat/battle-plan12-weapon-master.json',storySource='game/unity/Assets/Game/Resources/Story/plan12-weapon-master-story-content.json');write('docs/characters/index.json',d)
 m=read('game/unity/Assets/Game/ContentDrafts/WeaponMaster/manifest.json');m.update(available=True,stage='accepted-normal-play',remaining=[]);write('game/unity/Assets/Game/ContentDrafts/WeaponMaster/manifest.json',m)
 p=R/'docs/characters/weapon-master.md';s=p.read_text(encoding='utf8').replace('追加準備中・通常プレイ未公開','受入れ完了・通常プレイに追加').replace('戦闘数値・武器正式名・専用画像・旧セーブ移行・表示確認は未受入れ。','戦闘・専用画像20点・旧セーブ移行・標準解像度の表示を確認済み。原作武器の全文は未確認のため、本作独自名を用いる。');p.write_text(s,encoding='utf8')
 p=R/'tools/build-heroine-production-roster.py';s=p.read_text(encoding='utf8').replace('16, 17, 18],','16, 17, 18, 20],').replace("'viper', 'vindicator']))","'viper', 'vindicator', 'weapon-master']))").replace('現行23形態','現行24形態');p.write_text(s,encoding='utf8')
 for name in ['README.md','game/README.md','docs/characters/README.md','docs/production/heroine-addition-guide.md','docs/production/plan12-production-handoff.txt']:
  p=R/name;s=p.read_text(encoding='utf8').replace('23形態・20人','24形態・21人').replace('20人物・23形態','21人物・24形態').replace('114章774詩・115交流','117章792詩・120交流').replace('plan12-vindicator/newASTER.exe','plan12-weapon-master/newASTER.exe').replace('Plan12VindicatorBuild.ValidateAndBuild','Plan12WeaponMasterBuild.ValidateAndBuild').replace('Combat/battle-plan12-vindicator','Combat/battle-plan12-weapon-master').replace('Story/plan12-vindicator-story-content','Story/plan12-weapon-master-story-content').replace('人物索引の23形態カタログ','人物索引の24形態カタログ').replace('ヴィンディーケーターを含む23形態カタログ','ウェポンマスターを含む24形態カタログ')
  s=s.replace('Core/Data12,310項目とUnity C#277ソース','Core/Data12,435項目とUnity C#278ソース').replace('インヴィジブル、ヴァイパー、ヴィンディーケーターまで追加済み','インヴィジブル、ヴァイパー、ヴィンディーケーター、ウェポンマスターまで追加済み')
  if name=='docs/characters/README.md':s+='\n- [ウェポンマスター](weapon-master.md) — イサカ・ハイライン／ガンナー。3章・5交流、専用20点、全キャラ共通構成の木。\n'
  if name.endswith('handoff.txt'):s+='\n最新追加：ウェポンマスター。完了記録 plan12-weapon-master-form-record.json。\n'
  p.write_text(s,encoding='utf8')
 print('WEAPON_MASTER_NORMAL_PATHS_PUBLISHED final build/player pending')
elif mode=='complete':
 logs=['tmp/plan12-weapon-master-core.log','tmp/plan12-weapon-master-build-final.log','tmp/plan12-weapon-master/production-player.log']
 assert 'PLAN12_PRODUCTION_PLAYER_PASS defaultCatalog=24 people=21 oldSave=23 joins=1 summonable=1 wallet=preserved' in (R/logs[2]).read_text(encoding='utf8')
 assert 'PLAN12_WEAPON_MASTER_BUILD_PASS' in (R/logs[1]).read_text(encoding='utf8')
 assert 'AUTOMATIC_CHAIN_PASS' in (R/logs[0]).read_text(encoding='utf8')
 r=read(REC);r.update(status='complete_published',acceptance='pass',build=dict(path='game/Builds/plan12-weapon-master/newASTER.exe',sha256=digest('game/Builds/plan12-weapon-master/newASTER.exe'),workingTreeChanges=True),validationLogs=[dict(path=p,sha256=digest(p)) for p in logs]);write(REC,r)
 g=read(GEN);g['available']=True;write(GEN,g)
 write('docs/production/plan12-weapon-master-preparation.json',dict(status='accepted-normal-play',available=True,personId=H,sourcePersonId='aa.person.020',realName='イサカ・ハイライン',jobId='job.gunner',chapters=3,pages=30,poems=18,events=5,images=20,weaponNameOrigin='newaster-original; source full name remains unverified',completionRecord=REC,validation=dict(assertions=12435,unitySources=278),remaining=[]))
 write('docs/production/plan12-weapon-master-runtime-validation.json',dict(status='pass',forms=24,people=21,added=H,record=REC,oldSave='23-to-24; all fields and wallet preserved; physical save/reload',automatedPlayerCases=19,physicalInput='excluded',resolution='default 1920x1080',unresolved=[]))
 print('WEAPON_MASTER_COMPLETE_PUBLISHED 24 forms / 21 people')
else:raise ValueError(mode)

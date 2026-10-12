"""Record manually reviewed Player frames, then publish the accepted form.

The review command is only run after the operator has inspected the listed frames.
It records evidence identity; it does not infer visual approval from file existence.
"""
from pathlib import Path
import hashlib,json,sys
ROOT=Path(__file__).resolve().parents[1]
def read(p):return json.loads((ROOT/p).read_text(encoding='utf-8-sig'))
def write(p,v):(ROOT/p).write_text(json.dumps(v,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def digest(p):return hashlib.sha256((ROOT/p).read_bytes()).hexdigest()
def capture(p):
    from PIL import Image
    assert Image.open(ROOT/p).size==(1920,1080),p
    return dict(path=p,sha256=digest(p))
GEN='game/art-source/invisible-v1/generation.json'
RECORD='docs/production/plan12-invisible-form-record.json'
BASE='tmp/plan12-invisible/player/'
REFINED='tmp/plan12-invisible/refined/'
if sys.argv[1]=='review':
    g=read(GEN);images={}
    for a in g['assets']:
        k=a['key']
        if k=='standing':paths=[REFINED+'invisible-expression.normal.png']
        elif k=='portrait':paths=[BASE+'recruit.png',REFINED+'trait.png']
        elif k.startswith('expression-'):paths=[REFINED+'invisible-expression.'+k[11:]+'.png']
        elif k in ('attack','hit','cutin'):paths=[BASE+'battle-'+k+'.png']
        elif k.startswith('sd-'):paths=[(REFINED if k=='sd-work' else BASE)+'invisible-sd.'+k[3:]+'.png']
        elif k.startswith('event-'):paths=[BASE+'invisible-cg.'+k.split('-')[1]+'.png',BASE+'gallery-frames/invisible-cg.'+k.split('-')[1]+'.png']
        else:paths=[BASE+'tree0.png',BASE+'tree2.png',BASE+'tree4.png']
        if k in ('sd-sit','sd-work','sd-look'):paths.append(REFINED+'invisible-sd.furniture.png')
        files=[capture(p) for p in paths]
        assert digest(a['path'])==a['sha256']
        a.update(runtimeReview='approved',captureFiles=files)
        images[k]=dict(status='approved',source=a['path'],sha256=a['sha256'],resourcePath=a['resourcePath'],purposeReview='approved',runtimeReview=dict(status='approved',reviewedSha256=a['sha256'],captureFiles=files))
    write(GEN,g)
    layout='game/unity/Assets/Game/Resources/UI/WeaponTrees/invisible.json';tree=read(layout)
    components=[layout]+['game/unity/Assets/Game/Resources/UI/WeaponTrees/'+p+'.png' for p in set(['common-tree',tree['foliageAsset'],tree['flowerAsset']]+[d['asset'] for d in tree['decorations']])]
    record=dict(schemaVersion=1,personId='heroine.invisible',formId='heroine.invisible',jobId='job.healer',status='runtime_review_complete',profile='docs/characters/invisible.md',images=images,treeComponents=[dict(path=p,sha256=digest(p),status='approved') for p in sorted(components)],resolution='default launch 1920x1080',method='manual actual Player frame review; automated behavior separately recorded; physical input excluded',unresolved=[],updatedAt='2026-10-11')
    record['renderers']=[dict(path=p,sha256=digest(p),status='approved') for p in ['game/unity/Assets/Game/Scripts/Presentation/AdvExperience.cs','game/unity/Assets/Game/Scripts/Presentation/GardenArtComposition.cs','game/unity/Assets/Game/Scripts/Presentation/LayeredWeaponTree.cs','game/unity/Assets/Game/Scripts/Data/Plan12FirstBatchHomeArt.cs']]
    write(RECORD,record)
    print('INVISIBLE_RUNTIME_REVIEW_RECORDED 21 assets')
elif sys.argv[1]=='publish':
    r=read(RECORD);assert r['status']=='runtime_review_complete'
    paths=ROOT/'game/unity/Assets/Game/Scripts/Data/ProductionContentPaths.cs';s=paths.read_text(encoding='utf-8').replace('Combat/battle-plan12-first-batch','Combat/battle-plan12-invisible').replace('Story/plan12-first-batch-story-content','Story/plan12-invisible-story-content');paths.write_text(s,encoding='utf-8')
    index=read('docs/characters/index.json');person=next(p for p in index['developmentPeople'] if p['personId']=='heroine.invisible');index['people'].append({k:person[k] for k in ('personId','name','profile','formIds')});index['developmentPeople']=[p for p in index['developmentPeople'] if p!=person];index.update(combatSource='game/unity/Assets/Game/Resources/Combat/battle-plan12-invisible.json',storySource='game/unity/Assets/Game/Resources/Story/plan12-invisible-story-content.json');write('docs/characters/index.json',index)
    m=read('game/unity/Assets/Game/ContentDrafts/Invisible/manifest.json');m.update(available=True,stage='accepted-normal-play');write('game/unity/Assets/Game/ContentDrafts/Invisible/manifest.json',m)
    profile=ROOT/'docs/characters/invisible.md';profile.write_text(profile.read_text(encoding='utf-8').replace('制作中・未公開','受入れ完了・通常プレイに追加'),encoding='utf-8')
    prep=read('docs/production/plan12-invisible-preparation.json');prep.update(status='accepted-normal-play',available=True,remaining=[],completionRecord=RECORD);write('docs/production/plan12-invisible-preparation.json',prep)
    roster=ROOT/'tools/build-heroine-production-roster.py';s=roster.read_text(encoding='utf-8').replace('11, 45],','11, 45, 16],').replace("'anathema', 'shiva']))","'anathema', 'shiva', 'invisible']))").replace('現行20形態','現行21形態');roster.write_text(s,encoding='utf-8')
    for name in ['README.md','game/README.md','docs/characters/README.md','docs/production/heroine-addition-guide.md']:
        p=ROOT/name;s=p.read_text(encoding='utf-8').replace('20形態・17人','21形態・18人').replace('17人物・20形態','18人物・21形態').replace('105章720詩・100交流','108章738詩・105交流')
        if name in ['README.md','game/README.md']:
            s=s.replace('plan12-first-batch/newASTER.exe','plan12-invisible/newASTER.exe').replace('Plan12FirstBatchBuild.ValidateAndBuild','Plan12InvisibleBuild.ValidateAndBuild').replace('Combat/battle-plan12-first-batch','Combat/battle-plan12-invisible').replace('Story/plan12-first-batch-story-content','Story/plan12-invisible-story-content').replace('シヴァまで追加済み','シヴァ、インヴィジブルまで追加済み').replace('Core/Data12,023項目とUnity C#274ソース','Core/Data12,047項目とUnity C#275ソース')
            s+='\nインヴィジブルの追加・画像・戦闘・保存の受入記録は [完了記録]('+('docs/' if name=='README.md' else '../docs/')+'production/plan12-invisible-form-record.json)を参照。\n'
        elif name=='docs/characters/README.md':s+='\n- [インヴィジブル](invisible.md) — 品田ゆにこ／ヒーラー。3章・5交流と専用画像を接続済み。\n'
        else:s=s.replace('第1ロット5人を含む20形態カタログ','インヴィジブルを含む21形態カタログ');s+='\n2026-10-11追記：インヴィジブルの通常形態を完了。机用姿勢は実際の机との合成で手と接地位置を確認する。新規画像は Texture2D として取り込み、回復対象への強化もスキル説明に含める。専用20画像と共通木＋専用装飾の合格記録は `plan12-invisible-form-record.json`、現行正本は人物索引の21形態カタログ。\n'
        p.write_text(s,encoding='utf-8')
    p=ROOT/'docs/production/plan12-production-handoff.txt';s=p.read_text(encoding='utf-8').replace('現行製品：17人物・20形態。第1ロット5人は通常プレイへ公開済み。','現行製品：18人物・21形態。第1ロット5人とインヴィジブルは通常プレイへ公開済み。').replace('次の制作入力：plan12-invisible-preparation.json。インヴィジブルは資料確認中・未公開。','最新完了記録：plan12-invisible-form-record.json。次の追加人物は未指定。');p.write_text(s,encoding='utf-8')
    print('INVISIBLE_NORMAL_PATHS_PUBLISHED build/player acceptance still required')
elif sys.argv[1]=='complete':
    assert 'PLAN12_PRODUCTION_PLAYER_PASS defaultCatalog=21 people=18 oldSave=20 joins=1 summonable=1 wallet=preserved' in (ROOT/'tmp/plan12-invisible/production-player.log').read_text(encoding='utf-8')
    assert 'PLAN12_INVISIBLE_BUILD_PASS' in (ROOT/'tmp/plan12-invisible-build-final.log').read_text(encoding='utf-8')
    assert 'AUTOMATIC_CHAIN_PASS' in (ROOT/'tmp/plan12-invisible-core-final.log').read_text(encoding='utf-8')
    r=read(RECORD);r.update(status='complete_published',acceptance='pass',build=dict(path='game/Builds/plan12-invisible/newASTER.exe',sha256=digest('game/Builds/plan12-invisible/newASTER.exe'),workingTreeChanges=True),validationLogs=[dict(path=p,sha256=digest(p)) for p in ['tmp/plan12-invisible-core-final.log','tmp/plan12-invisible-build-final.log','tmp/plan12-invisible/production-player.log']]);write(RECORD,r)
    write('docs/production/plan12-invisible-runtime-validation.json',dict(status='pass',forms=21,people=18,added='heroine.invisible',record=RECORD,oldSave='20-to-21; all fields and wallet preserved; physical save/reload',automatedPlayerCases=19,physicalInput='excluded',resolution='default 1920x1080',unresolved=[]))
    print('INVISIBLE_COMPLETE_PUBLISHED 21 forms / 18 people')

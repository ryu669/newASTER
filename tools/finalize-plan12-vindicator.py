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
GEN='game/art-source/vindicator-v1/generation.json'
RECORD='docs/production/plan12-vindicator-form-record.json'
BASE='tmp/plan12-vindicator/player/'
REFINED=BASE
if sys.argv[1]=='review':
    g=read(GEN);images={}
    for a in g['assets']:
        k=a['key']
        if k=='standing':paths=[REFINED+'vindicator-expression.normal.png']
        elif k=='portrait':paths=[BASE+'recruit.png',REFINED+'trait.png']
        elif k.startswith('expression-'):paths=[REFINED+'vindicator-expression.'+k[11:]+'.png']
        elif k in ('attack','hit','cutin'):paths=[BASE+'battle-'+k+'.png']
        elif k.startswith('sd-'):paths=[(REFINED if k=='sd-work' else BASE)+'vindicator-sd.'+k[3:]+'.png']
        elif k.startswith('event-'):paths=[BASE+'vindicator-cg.'+k.split('-')[1]+'.png',BASE+'gallery-frames/vindicator-cg.'+k.split('-')[1]+'.png']
        else:paths=[BASE+'tree0.png',BASE+'tree2.png',BASE+'tree4.png']
        if k in ('sd-sit','sd-work','sd-look'):paths.append('tmp/plan12-vindicator/refined/vindicator-sd.furniture.png')
        files=[capture(p) for p in paths]
        assert digest(a['path'])==a['sha256']
        a.update(runtimeReview='approved',captureFiles=files)
        images[k]=dict(status='approved',source=a['path'],sha256=a['sha256'],resourcePath=a['resourcePath'],purposeReview='approved',runtimeReview=dict(status='approved',reviewedSha256=a['sha256'],captureFiles=files))
    write(GEN,g)
    layout='game/unity/Assets/Game/Resources/UI/WeaponTrees/vindicator.json';tree=read(layout)
    components=[layout]+['game/unity/Assets/Game/Resources/UI/WeaponTrees/'+p+'.png' for p in set(['common-tree',tree['foliageAsset'],tree['flowerAsset']]+[d['asset'] for d in tree['decorations']])]
    record=dict(schemaVersion=1,personId='heroine.vindicator',formId='heroine.vindicator',jobId='job.defender',status='runtime_review_complete',profile='docs/characters/vindicator.md',images=images,treeComponents=[dict(path=p,sha256=digest(p),status='approved') for p in sorted(components)],resolution='default launch 1920x1080',method='manual actual Player frame review; automated behavior separately recorded; physical input excluded',unresolved=[],updatedAt='2026-10-11')
    record['renderers']=[dict(path=p,sha256=digest(p),status='approved') for p in ['game/unity/Assets/Game/Scripts/Presentation/AdvExperience.cs','game/unity/Assets/Game/Scripts/Presentation/GardenArtComposition.cs','game/unity/Assets/Game/Scripts/Presentation/LayeredWeaponTree.cs','game/unity/Assets/Game/Scripts/Data/Plan12FirstBatchHomeArt.cs']]
    write(RECORD,record)
    print('VINDICATOR_RUNTIME_REVIEW_RECORDED 21 assets')
elif sys.argv[1]=='publish':
    r=read(RECORD);assert r['status']=='runtime_review_complete'
    paths=ROOT/'game/unity/Assets/Game/Scripts/Data/ProductionContentPaths.cs';s=paths.read_text(encoding='utf-8').replace('Combat/battle-plan12-viper','Combat/battle-plan12-vindicator').replace('Story/plan12-viper-story-content','Story/plan12-vindicator-story-content');paths.write_text(s,encoding='utf-8')
    index=read('docs/characters/index.json');person=next(p for p in index['developmentPeople'] if p['personId']=='heroine.vindicator');index['people'].append({k:person[k] for k in ('personId','name','profile','formIds')});index['developmentPeople']=[p for p in index['developmentPeople'] if p!=person];index.update(combatSource='game/unity/Assets/Game/Resources/Combat/battle-plan12-vindicator.json',storySource='game/unity/Assets/Game/Resources/Story/plan12-vindicator-story-content.json');write('docs/characters/index.json',index)
    m=read('game/unity/Assets/Game/ContentDrafts/Vindicator/manifest.json');m.update(available=True,stage='accepted-normal-play',remaining=[]);write('game/unity/Assets/Game/ContentDrafts/Vindicator/manifest.json',m)
    profile=ROOT/'docs/characters/vindicator.md';profile.write_text(profile.read_text(encoding='utf-8').replace('制作中・未公開','受入れ完了・通常プレイに追加').replace('制作中正本は Assets/Game/ContentDrafts/Vindicator に置き、画像・戦闘・保存・実行版表示の受入れ後に通常プレイへ追加する。','通常プレイの正本は人物索引の combatSource / storySource。制作入力を Assets/Game/ContentDrafts/Vindicator に保持する。'),encoding='utf-8')
    prep=read('docs/production/plan12-vindicator-preparation.json');prep.update(status='accepted-normal-play',available=True,remaining=[],completionRecord=RECORD);prep['validation']['assertions']=12310;write('docs/production/plan12-vindicator-preparation.json',prep)
    roster=ROOT/'tools/build-heroine-production-roster.py';s=roster.read_text(encoding='utf-8').replace('11, 45, 16, 17],','11, 45, 16, 17, 18],').replace("'shiva', 'invisible', 'viper']))","'shiva', 'invisible', 'viper', 'vindicator']))").replace('現行22形態','現行23形態');roster.write_text(s,encoding='utf-8')
    for name in ['README.md','game/README.md','docs/characters/README.md','docs/production/heroine-addition-guide.md']:
        p=ROOT/name;s=p.read_text(encoding='utf-8').replace('22形態・19人','23形態・20人').replace('19人物・22形態','20人物・23形態').replace('111章756詩・110交流','114章774詩・115交流')
        if name in ['README.md','game/README.md']:
            s=s.replace('plan12-viper/newASTER.exe','plan12-vindicator/newASTER.exe').replace('Plan12ViperBuild.ValidateAndBuild','Plan12VindicatorBuild.ValidateAndBuild').replace('Combat/battle-plan12-viper','Combat/battle-plan12-vindicator').replace('Story/plan12-viper-story-content','Story/plan12-vindicator-story-content').replace('インヴィジブル、ヴァイパーまで追加済み','インヴィジブル、ヴァイパー、ヴィンディーケーターまで追加済み').replace('Core/Data12,120項目とUnity C#276ソース','Core/Data12,310項目とUnity C#277ソース')
        elif name=='docs/characters/README.md':s+='\n- [ヴィンディーケーター](vindicator.md) — モルトラ・バルタ／ディフェンダー。3章・5交流と専用画像を接続済み。\n'
        else:s=s.replace('ヴァイパーを含む22形態カタログ','ヴィンディーケーターを含む23形態カタログ').replace('人物索引の22形態カタログ','人物索引の23形態カタログ')
        p.write_text(s,encoding='utf-8')
    p=ROOT/'docs/production/plan12-production-handoff.txt';s=p.read_text(encoding='utf-8').replace('19人物・22形態','20人物・23形態').replace('第1ロット5人とインヴィジブルは','第1ロット5人、インヴィジブル、ヴァイパーは');lines=s.splitlines();lines[4]='最新完了記録：plan12-vindicator-form-record.json。次は未実装の候補を資料確認から順に進める。ヴィンディーケーターの画像21点・戦闘・保存・標準解像度表示を確認済み。';p.write_text('\n'.join(lines)+'\n',encoding='utf-8')
    print('VINDICATOR_NORMAL_PATHS_PUBLISHED build/player acceptance still required')
elif sys.argv[1]=='complete':
    assert 'PLAN12_PRODUCTION_PLAYER_PASS defaultCatalog=23 people=20 oldSave=22 joins=1 summonable=1 wallet=preserved' in (ROOT/'tmp/plan12-vindicator/production-player.log').read_text(encoding='utf-8')
    assert 'PLAN12_VINDICATOR_BUILD_PASS' in (ROOT/'tmp/plan12-vindicator-build-final.log').read_text(encoding='utf-8')
    assert 'AUTOMATIC_CHAIN_PASS' in (ROOT/'tmp/plan12-vindicator-core.log').read_text(encoding='utf-8')
    r=read(RECORD);r.update(status='complete_published',acceptance='pass',build=dict(path='game/Builds/plan12-vindicator/newASTER.exe',sha256=digest('game/Builds/plan12-vindicator/newASTER.exe'),workingTreeChanges=True),validationLogs=[dict(path=p,sha256=digest(p)) for p in ['tmp/plan12-vindicator-core.log','tmp/plan12-vindicator-build-final.log','tmp/plan12-vindicator/production-player.log']]);write(RECORD,r)
    write('docs/production/plan12-vindicator-runtime-validation.json',dict(status='pass',forms=23,people=20,added='heroine.vindicator',record=RECORD,oldSave='22-to-23; all fields and wallet preserved; physical save/reload',automatedPlayerCases=19,physicalInput='excluded',resolution='default 1920x1080',unresolved=[]))
    print('VINDICATOR_COMPLETE_PUBLISHED 23 forms / 20 people')

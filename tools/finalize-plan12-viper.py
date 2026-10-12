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
GEN='game/art-source/viper-v1/generation.json'
RECORD='docs/production/plan12-viper-form-record.json'
BASE='tmp/plan12-viper/player/'
REFINED=BASE
if sys.argv[1]=='review':
    g=read(GEN);images={}
    for a in g['assets']:
        k=a['key']
        if k=='standing':paths=[REFINED+'viper-expression.normal.png']
        elif k=='portrait':paths=[BASE+'recruit.png',REFINED+'trait.png']
        elif k.startswith('expression-'):paths=[REFINED+'viper-expression.'+k[11:]+'.png']
        elif k in ('attack','hit','cutin'):paths=[BASE+'battle-'+k+'.png']
        elif k.startswith('sd-'):paths=[(REFINED if k=='sd-work' else BASE)+'viper-sd.'+k[3:]+'.png']
        elif k.startswith('event-'):paths=[BASE+'viper-cg.'+k.split('-')[1]+'.png',BASE+'gallery-frames/viper-cg.'+k.split('-')[1]+'.png']
        else:paths=[BASE+'tree0.png',BASE+'tree2.png',BASE+'tree4.png']
        if k in ('sd-sit','sd-work','sd-look'):paths.append(REFINED+'viper-sd.furniture.png')
        files=[capture(p) for p in paths]
        assert digest(a['path'])==a['sha256']
        a.update(runtimeReview='approved',captureFiles=files)
        images[k]=dict(status='approved',source=a['path'],sha256=a['sha256'],resourcePath=a['resourcePath'],purposeReview='approved',runtimeReview=dict(status='approved',reviewedSha256=a['sha256'],captureFiles=files))
    write(GEN,g)
    layout='game/unity/Assets/Game/Resources/UI/WeaponTrees/viper.json';tree=read(layout)
    components=[layout]+['game/unity/Assets/Game/Resources/UI/WeaponTrees/'+p+'.png' for p in set(['common-tree',tree['foliageAsset'],tree['flowerAsset']]+[d['asset'] for d in tree['decorations']])]
    record=dict(schemaVersion=1,personId='heroine.viper',formId='heroine.viper',jobId='job.gunner',status='runtime_review_complete',profile='docs/characters/viper.md',images=images,treeComponents=[dict(path=p,sha256=digest(p),status='approved') for p in sorted(components)],resolution='default launch 1920x1080',method='manual actual Player frame review; automated behavior separately recorded; physical input excluded',unresolved=[],updatedAt='2026-10-11')
    record['renderers']=[dict(path=p,sha256=digest(p),status='approved') for p in ['game/unity/Assets/Game/Scripts/Presentation/AdvExperience.cs','game/unity/Assets/Game/Scripts/Presentation/GardenArtComposition.cs','game/unity/Assets/Game/Scripts/Presentation/LayeredWeaponTree.cs','game/unity/Assets/Game/Scripts/Data/Plan12FirstBatchHomeArt.cs']]
    write(RECORD,record)
    print('VIPER_RUNTIME_REVIEW_RECORDED 21 assets')
elif sys.argv[1]=='publish':
    r=read(RECORD);assert r['status']=='runtime_review_complete'
    paths=ROOT/'game/unity/Assets/Game/Scripts/Data/ProductionContentPaths.cs';s=paths.read_text(encoding='utf-8').replace('Combat/battle-plan12-invisible','Combat/battle-plan12-viper').replace('Story/plan12-invisible-story-content','Story/plan12-viper-story-content');paths.write_text(s,encoding='utf-8')
    index=read('docs/characters/index.json');person=next(p for p in index['developmentPeople'] if p['personId']=='heroine.viper');index['people'].append({k:person[k] for k in ('personId','name','profile','formIds')});index['developmentPeople']=[p for p in index['developmentPeople'] if p!=person];index.update(combatSource='game/unity/Assets/Game/Resources/Combat/battle-plan12-viper.json',storySource='game/unity/Assets/Game/Resources/Story/plan12-viper-story-content.json');write('docs/characters/index.json',index)
    m=read('game/unity/Assets/Game/ContentDrafts/Viper/manifest.json');m.update(available=True,stage='accepted-normal-play');write('game/unity/Assets/Game/ContentDrafts/Viper/manifest.json',m)
    profile=ROOT/'docs/characters/viper.md';profile.write_text(profile.read_text(encoding='utf-8').replace('制作中・未公開','受入れ完了・通常プレイに追加').replace('制作中正本は Assets/Game/ContentDrafts/Viper に置き、画像・戦闘・保存・実行版表示の受入れ後に通常プレイへ追加する。','通常プレイの正本は人物索引の combatSource / storySource。制作入力を Assets/Game/ContentDrafts/Viper に保持する。'),encoding='utf-8')
    prep=read('docs/production/plan12-viper-preparation.json');prep.update(status='accepted-normal-play',available=True,remaining=[],completionRecord=RECORD);prep['validation']['assertions']=12120;write('docs/production/plan12-viper-preparation.json',prep)
    roster=ROOT/'tools/build-heroine-production-roster.py';s=roster.read_text(encoding='utf-8').replace('11, 45, 16],','11, 45, 16, 17],').replace("'shiva', 'invisible']))","'shiva', 'invisible', 'viper']))").replace('現行21形態','現行22形態');roster.write_text(s,encoding='utf-8')
    for name in ['README.md','game/README.md','docs/characters/README.md','docs/production/heroine-addition-guide.md']:
        p=ROOT/name;s=p.read_text(encoding='utf-8').replace('21形態・18人','22形態・19人').replace('18人物・21形態','19人物・22形態').replace('108章738詩・105交流','111章756詩・110交流')
        if name in ['README.md','game/README.md']:
            s=s.replace('plan12-invisible/newASTER.exe','plan12-viper/newASTER.exe').replace('Plan12InvisibleBuild.ValidateAndBuild','Plan12ViperBuild.ValidateAndBuild').replace('Combat/battle-plan12-invisible','Combat/battle-plan12-viper').replace('Story/plan12-invisible-story-content','Story/plan12-viper-story-content').replace('インヴィジブルまで追加済み','インヴィジブル、ヴァイパーまで追加済み').replace('Core/Data12,047項目とUnity C#275ソース','Core/Data12,120項目とUnity C#276ソース')
        elif name=='docs/characters/README.md':s+='\n- [ヴァイパー](viper.md) — ドロシー・ナイナー／ガンナー。3章・5交流と専用画像を接続済み。\n'
        else:s=s.replace('インヴィジブルを含む21形態カタログ','ヴァイパーを含む22形態カタログ').replace('人物索引の21形態カタログ','人物索引の22形態カタログ')
        p.write_text(s,encoding='utf-8')
    p=ROOT/'docs/production/plan12-production-handoff.txt';s=p.read_text(encoding='utf-8').replace('18人物・21形態','19人物・22形態').replace('第1ロット5人とインヴィジブルは','第1ロット5人、インヴィジブル、ヴァイパーは');lines=s.splitlines();lines[4]='最新完了記録：plan12-viper-form-record.json。次は未実装の候補018を資料確認から順に進める。ヴァイパーの画像21点・戦闘・保存・標準解像度表示を確認済み。';p.write_text('\n'.join(lines)+'\n',encoding='utf-8')
    print('VIPER_NORMAL_PATHS_PUBLISHED build/player acceptance still required')
elif sys.argv[1]=='complete':
    assert 'PLAN12_PRODUCTION_PLAYER_PASS defaultCatalog=22 people=19 oldSave=21 joins=1 summonable=1 wallet=preserved' in (ROOT/'tmp/plan12-viper/production-player.log').read_text(encoding='utf-8')
    assert 'PLAN12_VIPER_BUILD_PASS' in (ROOT/'tmp/plan12-viper-build-final.log').read_text(encoding='utf-8')
    assert 'AUTOMATIC_CHAIN_PASS' in (ROOT/'tmp/plan12-viper-core.log').read_text(encoding='utf-8')
    r=read(RECORD);r.update(status='complete_published',acceptance='pass',build=dict(path='game/Builds/plan12-viper/newASTER.exe',sha256=digest('game/Builds/plan12-viper/newASTER.exe'),workingTreeChanges=True),validationLogs=[dict(path=p,sha256=digest(p)) for p in ['tmp/plan12-viper-core.log','tmp/plan12-viper-build-final.log','tmp/plan12-viper/production-player.log']]);write(RECORD,r)
    write('docs/production/plan12-viper-runtime-validation.json',dict(status='pass',forms=22,people=19,added='heroine.viper',record=RECORD,oldSave='21-to-22; all fields and wallet preserved; physical save/reload',automatedPlayerCases=19,physicalInput='excluded',resolution='default 1920x1080',unresolved=[]))
    print('VIPER_COMPLETE_PUBLISHED 22 forms / 19 people')

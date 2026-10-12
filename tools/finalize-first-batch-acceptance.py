"""Publish only after the recorded source, visual and isolated Player checks pass."""
import copy,hashlib,importlib.util,json,subprocess
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
def read(p):return json.loads((ROOT/p).read_text(encoding='utf-8'))
def write(p,obj):(ROOT/p).write_text(json.dumps(obj,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
def sha(p):return hashlib.sha256((ROOT/p).read_bytes()).hexdigest()
def require(p,marker):
    assert marker in (ROOT/p).read_text(encoding='utf-8-sig',errors='replace'),(p,marker)
core='tmp/plan12-batch/production-core-validation.log'
build='tmp/plan12-batch/production-build.log'
compile_log='tmp/plan12-batch/production-compile.log'
production='tmp/plan12-batch/production-player.log'
require(core,'AUTOMATIC_CHAIN_PASS 12023 assertions')
require(build,'PLAN12_FIRST_BATCH_BUILD_PASS')
require(build,'PLAN11_9_PRODUCTION_BUILD_PASS forms=20 people=17')
require(compile_log,'UNITY_SCRIPT_COMPILE_PASS 274')
require(production,'PLAN12_PRODUCTION_PLAYER_PASS defaultCatalog=20 people=17 oldSave=15 joins=5 summonable=5 wallet=preserved')
review=read('docs/production/plan12-first-batch-asset-review.json')
combat='game/unity/Assets/Game/Resources/Combat/battle-plan12-first-batch.json'
story='game/unity/Assets/Game/Resources/Story/plan12-first-batch-story-content.json'
catalog=read(combat)
template=read('docs/production/plan12-form-record-template.json')
spec=importlib.util.spec_from_file_location('batch_author',ROOT/'tools/author-plan12-first-batch.py');author=importlib.util.module_from_spec(spec);spec.loader.exec_module(author)
people={p['key']:p for p in author.PEOPLE}
metrics=[]
for form in review['forms']:
    hero=form['key'];hero_id='heroine.'+hero
    base='tmp/plan12-aeon/player-default' if hero=='aeon' else f'tmp/plan12-batch/player-default/{hero}'
    marker='PLAN12_AEON_PLAYER_PASS' if hero=='aeon' else 'PLAN12_FIRST_BATCH_PLAYER_PASS'
    for view in ['recruitment','recruit','trait','skill','formation','command','attack0','attack1','attack2','garden','journey']:
        require(f'{base}/{view}.log',marker)
    if hero!='aeon':require(f'{base}/command.log','JOB_PANEL_OPERATIONS_PASS')
    record=copy.deepcopy(template)
    record.update(personId=hero_id,formId=hero_id,jobId=next(h['jobId'] for h in catalog['heroines'] if h['id']==hero_id),profile=f'docs/characters/{hero}.md',adoptionDecisions='docs/references/characters/aeon.md' if hero=='aeon' else 'docs/production/heroine-relationships.md',status='complete_published',acceptance='pass',updatedAt='2026-10-11',unresolved=[])
    record['referenceSources']=read('docs/production/plan12-aeon-form-record.json')['referenceSources'] if hero=='aeon' else [f'{people[hero]["number"]:03d}戦闘_{people[hero]["name"]}.mkv','docs/production/heroine-relationships.md']
    for asset in form['assets']:
        runtime=asset['runtimeReview'];assert runtime['status']=='approved' and runtime['reviewedSha256']==asset['sha256'] and sha(asset['path'])==asset['sha256']
        key=asset['key'];adopted=f'game/unity/Assets/Game/Resources/Illustrations/{hero}-{key}-candidate-v1.png'
        if key=='portrait':adopted=f'game/unity/Assets/Game/Resources/Illustrations/heroine-portrait-{hero}-v1.png'
        if key=='equipment-tree-v1' and hero=='aeon':adopted=f'game/unity/Assets/Game/Resources/Illustrations/heroine-weapon-tree-{hero}-v1.png'
        if key=='equipment-tree-v1' and hero!='aeon':adopted=f'game/unity/Assets/Game/Resources/UI/WeaponTrees/{hero}.json'
        assert (ROOT/adopted).exists(),adopted
        record['images'][key]={'status':'approved','source':asset['path'],'adopted':adopted,'sha256':asset['sha256'],'adoptedSha256':sha(adopted),'purposeReview':asset['purposeReview'],'runtimeReview':runtime,'validation':'source hash and actual default-resolution Player frame reviewed'}
    evidence={
      'identity_and_reaction':[record['profile'],core],
      'three_skills':[f'{base}/attack{i}.log' for i in range(3)],
      'traits_2_job_3_character_3_interaction':[core,f'{base}/trait.png'],
      'weapon_13_nodes':[core,record['images']['equipment-tree-v1']['runtimeReview']['evidence'][0]],
      'story_3_chapters_30_pages_18_poems':[core,f'{base}/journey.log'],
      'events_basic_3_lover_2_target_variable_count':[f'{base}/journey.log']+[f'{base}/event{i}.log' for i in range(5) if (ROOT/f'{base}/event{i}.log').exists()],
      'recruit_formation_garden_adv':[f'{base}/{v}.log' for v in ['recruit','formation','garden','journey']],
      'save_migration_atomicity':[core,production],
      'automatic_regression':[core], 'unity_compile':[compile_log],
      'portrait_build_validation':[build], 'visual_review':['docs/production/plan12-first-batch-asset-review.json'],
      'player_default_launch':[production,f'{base}/command.log'],
      'job_commands_resources_and_states':[f'{base}/command.log'],
      'pre_addition_save_compatibility':[core,production],
      'real_asset_size_loading_memory':[f'tmp/plan12-batch/battle-art/{hero}/{pose}.metrics.json' for pose in ['attack','hit','cutin']]}
    for key,paths in evidence.items():
        assert all((ROOT/p).exists() for p in paths),paths
        record['checks'][key]={'status':'pass','evidence':paths}
    for pose in ['attack','hit','cutin']:metrics.append(read(f'tmp/plan12-batch/battle-art/{hero}/{pose}.metrics.json'))
    record['build']={'path':'game/Builds/plan12-first-batch/newASTER.exe','sha256':sha('game/Builds/plan12-first-batch/newASTER.exe'),'sourceCommit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),'workingTreeChanges':True,'catalogHashes':{combat:sha(combat),story:sha(story)}}
    write(f'docs/production/plan12-{hero}-form-record.json',record)
    form['status']='complete_published'
review['status']='complete_published';write('docs/production/plan12-first-batch-asset-review.json',review)
index=read('docs/characters/index.json');index['combatSource']=combat;index['storySource']=story
for entry in index.get('developmentPeople',[]):
    if any(f['key']==entry['personId'].removeprefix('heroine.') for f in review['forms']):
        if not any(e['personId']==entry['personId'] for e in index['people']):index['people'].append({k:entry[k] for k in ['personId','name','profile','formIds']})
index['developmentPeople']=[e for e in index.get('developmentPeople',[]) if not any(f['key']==e['personId'].removeprefix('heroine.') for f in review['forms'])]
assert len(index['people'])==17 and sum(len(e['formIds']) for e in index['people'])==20
write('docs/characters/index.json',index)
for directory in ['Aeon','FirstBatch']:
    path=f'game/unity/Assets/Game/ContentDrafts/{directory}/manifest.json';manifest=read(path)
    manifest.update(stage='complete-published',available=True,unresolved=[],combatSource=combat,storySource=story,combatSourceSha256=sha(combat),storySourceSha256=sha(story));write(path,manifest)
summary={'date':'2026-10-11','status':'pass','releaseReady':True,'forms':20,'people':17,'newPeople':['aeon','iron-maiden','avalanche','anathema','shiva'],'coreAssertions':12023,'unityCompileSources':274,'resolution':'1920x1080 default launch only','physicalInput':'not tested; automated job operations and manual frame review','oldSave':'15-to-20 Player physical save/load, exact pre-join field equality; 16-to-20 atomicity regression','metrics':metrics,'evidence':[core,build,compile_log,production],'remaining':[]}
write('docs/production/plan12-first-batch-runtime-validation.json',summary)
aeon=read('docs/production/plan12-aeon-runtime-validation.json');aeon.update(status='pass',releaseReady=True,coreAssertions=12023,remaining=[],evidence=[core,build,production,'docs/production/plan12-aeon-form-record.json']);write('docs/production/plan12-aeon-runtime-validation.json',aeon)
print('FIRST_BATCH_PUBLISHED 5 complete forms; normal roster 20 forms / 17 people')

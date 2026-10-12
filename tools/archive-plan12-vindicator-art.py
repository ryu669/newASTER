"""Copy reviewed generated originals unchanged and retain prompt/hash provenance."""
import hashlib,json,re,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
SRC=ROOT/'game/art-source/vindicator-v1'
GENERATED=Path('C:/Users/nishi/.codex/generated_images/01a122d9-2314-7463-b91b-94d1a4d927cb')
manifest=json.loads((SRC/'generation.json').read_text(encoding='utf8'))
rows=json.loads((ROOT/'tmp/vindicator-recovered-art.json').read_text(encoding='utf8'))
for p in (ROOT/'tmp').glob('vindicator-*-result.json'):rows.append(json.loads(p.read_text(encoding='utf8')))
base='Adult Vindicator / Moltra Balta, long black hair, icy blue eyes, gold star crown, black/gold plate armor, white wings, crimson cape, black/gold red-gem greatsword; same standing reference. Transparent background. '
for key,name,prompt in [
 ('attack','80b316be-eed0-46fb-97f8-0301c2aefbab','Full-body sword swing toward LEFT, thin blue arc; all wings boots sword inside canvas.'),
 ('sd-idle','4505deb5-2d86-4ff1-a9f1-59fe4b67b35b','2.5-head chibi full-body calm idle, small greatsword down.'),
 ('sd-sit','8763c163-1a4b-4b89-86c8-d99d555193ab','2.5-head chibi sitting on invisible chair, knees bent 90 degrees, lower legs hanging, feet together, hands on lap; no chair drawn.'),
 ('sd-work','01327f3e-c47c-4275-aa0f-fb0932486ae2','2.5-head chibi upright behind invisible desk, two hands forward at waist palms down; no desk/weapon/object.'),
 ('sd-look','575f5c68-f0fb-4e93-9097-4ec321777bb9','2.5-head chibi looking up LEFT, one hand shading brow, other relaxed, no weapon.')]:
 rows.append(dict(key=key,outputHint='exec-'+name+'.png',prompt=base+prompt,promptRecovery='Recovered semantic prompt from execution record; exact original unavailable.'))
approved={a['key']:a for a in manifest['assets']}
for row in rows:
 key=row['key']
 if key in approved:continue
 original=GENERATED/re.search(r'exec-[\w-]+\.png',row['outputHint']).group(0)
 assert original.exists(),original
 target=SRC/(key+'.png');assert not target.exists(),target
 shutil.copyfile(original,target)
 prompt=SRC/(key+'-prompt.txt');prompt.write_text(row['prompt']+'\n',encoding='utf8')
 approved[key]=dict(key=key,path=target.relative_to(ROOT).as_posix(),generatedPath=str(original),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),promptFile=prompt.relative_to(ROOT).as_posix(),purposeReview='approved',runtimeReview='pending',reviewNote='Original manually inspected for identity, intended pose, framing and no second person; Player review pending.')
 if 'promptRecovery' in row:approved[key]['promptRecovery']=row['promptRecovery']
expected=['standing','portrait','expression-joy','expression-puzzled','expression-determined','attack','hit','cutin','tree-ornament']+['sd-'+p for p in ['idle','walk','talk','react','sit','work','look']]+['event-'+str(i)+'-cg' for i in range(5)]
manifest['assets']=list(approved.values());manifest['remainingAssets']=[k for k in expected if k not in approved]
(SRC/'generation.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('VINDICATOR_ORIGINALS_ARCHIVED',len(approved),'remaining',manifest['remainingAssets'])

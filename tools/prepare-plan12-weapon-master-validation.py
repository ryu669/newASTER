"""Create dedicated acceptance entry points without copying old review approvals."""
from pathlib import Path
import json
R=Path(__file__).resolve().parents[1]
def convert(s):return s.replace('VINDICATOR','WEAPON_MASTER').replace('Vindicator','WeaponMaster').replace('vindicator','weapon-master')
for source,dest in [('tools/validate-plan12-vindicator-player.ps1','tools/validate-plan12-weapon-master-player.ps1'),('game/unity/Assets/Game/Editor/Plan12VindicatorBuild.cs','game/unity/Assets/Game/Editor/Plan12WeaponMasterBuild.cs')]:
 s=convert((R/source).read_text(encoding='utf8')).replace('Length!=23','Length!=24').replace('forms=23 people=20','forms=24 people=21').replace('Defender independent','Gunner independent')
 (R/dest).write_text(s,encoding='utf8')
s=convert((R/'tools/adopt-plan12-vindicator-art.py').read_text(encoding='utf8'))
s=s.replace('dict(r=.53,g=.67,b=.78,a=1)','dict(r=.60,g=.78,b=.24,a=1)').replace('dict(r=.65,g=.85,b=1,a=1)','dict(r=.78,g=.94,b=.35,a=1)')
(R/'tools/adopt-plan12-weapon-master-art.py').write_text(s,encoding='utf8')
p=R/'game/unity/Assets/Game/Scripts/Presentation/Plan12ProductionAcceptance.cs';s=p.read_text(encoding='utf8')
if 'bool weaponMaster=' not in s:
 s=s.replace('bool vindicator=', 'bool weaponMaster=combatDefinitions.HeroineIds.Contains("heroine.weapon-master");\n            bool vindicator=')
 s=s.replace('var additions=vindicator?', 'var additions=weaponMaster?new[]{"heroine.weapon-master"}:vindicator?')
 s=s.replace('(vindicator?23:', '(weaponMaster?24:vindicator?23:')
 s=s.replace('Resources.Load<TextAsset>(vindicator?"Combat/', 'Resources.Load<TextAsset>(weaponMaster?"Combat/battle-plan12-vindicator":vindicator?"Combat/')
 s=s.replace('Resources.Load<TextAsset>(vindicator?"Story/', 'Resources.Load<TextAsset>(weaponMaster?"Story/plan12-vindicator-story-content":vindicator?"Story/')
 p.write_text(s,encoding='utf8')
p=R/'game/unity/Assets/Game/Editor/Plan119BuildValidation.cs';s=p.read_text(encoding='utf8')
row='        if(ProductionContentPaths.Combat=="Combat/battle-plan12-weapon-master"){Plan12AssetReviewGate.ValidateRelease();Plan12InvisibleBuild.ValidateRelease();Plan12ViperBuild.ValidateRelease();Plan12VindicatorBuild.ValidateRelease();Plan12WeaponMasterBuild.ValidateRelease();}\n'
if row not in s:s=s.replace('        if(ProductionContentPaths.Combat=="Combat/battle-plan12-vindicator")',row+'        if(ProductionContentPaths.Combat=="Combat/battle-plan12-vindicator")');p.write_text(s,encoding='utf8')
p=R/'docs/characters/index.json';d=json.loads(p.read_text(encoding='utf-8-sig'))
if not any(x['personId']=='heroine.weapon-master' for x in d['people']+d['developmentPeople']):
 d['developmentPeople'].append(dict(personId='heroine.weapon-master',name='ウェポンマスター',profile='docs/characters/weapon-master.md',formIds=['heroine.weapon-master']))
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('WEAPON_MASTER_VALIDATION_PREPARED normal remains 23')

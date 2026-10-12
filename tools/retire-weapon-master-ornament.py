import json,re
from pathlib import Path
R=Path(__file__).resolve().parents[1]
p=R/'game/art-source/weapon-master-v1/generation.json';g=json.loads(p.read_text(encoding='utf8'))
retired=[a for a in g['assets'] if a['key']=='tree-ornament']
for a in retired:a.update(runtimeReview='rejected',reason='吊り下げ装飾が不自然なため削除。全キャラ共通の木・葉・花・丸い枠を使用。')
g.setdefault('retiredAssets',[]).extend(retired);g['assets']=[a for a in g['assets'] if a['key']!='tree-ornament'];g['remainingAssets']=0
p.write_text(json.dumps(g,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
for name in ['tools/finalize-plan12-weapon-master.py','tools/adopt-plan12-weapon-master-art.py']:
 p=R/name;s=p.read_text(encoding='utf8').replace("len(g['assets'])==21","len(g['assets'])==20").replace('21 originals','20 originals').replace('21 assets','20 assets').replace('専用画像21点','専用画像20点').replace('専用21点、吊り下げ装飾の木','専用20点、全キャラ共通構成の木').replace('images=21,weaponNameOrigin','images=20,weaponNameOrigin').replace('21 unchanged reviewed','20 unchanged reviewed')
 s=s.replace("for piece in tree['decorations']:piece['asset']='weapon-master-ornament-v1';piece['size']=.105","tree['decorations']=[]")
 p.write_text(s,encoding='utf8')
p=R/'game/unity/Assets/Game/Editor/Plan12WeaponMasterBuild.cs';s=p.read_text(encoding='utf8').replace('Length<5','Length<4').replace('21 source/runtime','20 source/runtime').replace('assets.Length!=21','assets.Length!=20').replace('twenty dedicated character images and a dedicated tree ornament','twenty dedicated character images and the shared wooden tree').replace('images=21 nodes','images=20 nodes');p.write_text(s,encoding='utf8')
p=R/'game/unity/Assets/Game/ContentDrafts/WeaponMaster/manifest.json';m=json.loads(p.read_text(encoding='utf8'));m['requiredAssetsPerForm']=20;m['remaining']=[x.replace('dedicated-art-21','dedicated-art-20') for x in m['remaining']];p.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
p=R/'game/unity/Assets/Game/Resources/UI/WeaponTrees/weapon-master.json';m=json.loads(p.read_text(encoding='utf8'));m['decorations']=[];p.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
p=R/'game/unity/Assets/Game/Scripts/Data/ProductionAssetAcceptance.cs';s=p.read_text(encoding='utf8');s=re.sub(r'^.*UI/WeaponTrees/weapon-master-ornament-v1.*\n','',s,flags=re.M);p.write_text(s,encoding='utf8')
p=R/'docs/characters/weapon-master.md';s=p.read_text(encoding='utf8').replace('専用画像21点','専用画像20点');s+='\n装備の木は全キャラ共通の実木・薄い葉・ノード周囲の花・丸い枠を使用する。吊り下げ装飾は不採用。\n';p.write_text(s,encoding='utf8')
print('WEAPON_MASTER_ORNAMENT_RETIRED assets=20')

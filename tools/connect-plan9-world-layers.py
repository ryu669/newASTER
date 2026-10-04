"""Bind shared world middle/front layers without replacing each enemy's own backdrop."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1];ART=ROOT/'game/unity/Assets/Game/Resources/Illustrations'
GROUPS=[('forest',['formal','red-crystal-tyrant']),('crystal',['memory-crystal-dragon','sky-tower-machine']),('water',['crystal-rose-princess','silver-sea-whale','heaven-tree-orochi']),('stars',['reenactment-yimir','emerald-star-astal']),('oasis',['amber-king-serpent','white-divine-dragon-mother']),('spring',['black-smoke-citadel','dead-king-megadeath']),('snow',['final-flame-ice-phoenix','newborn-asteria'])]
records=[]
for world,(style,enemies)in enumerate(GROUPS,1):
 for enemy in enemies:
  file=ART/('battle-formal.json'if enemy=='formal'else 'battle-'+enemy+'-candidate-v1.json');pack=json.loads(file.read_text(encoding='utf-8-sig'))
  pack['middleResourcePath']='Illustrations/world-'+style+'-mid-candidate-v1';pack['foregroundResourcePath']='Illustrations/'+('forest-front-candidate-v1'if style=='forest'else 'world-'+style+'-front-candidate-v1')
  for path in [pack['backgroundResourcePath'],pack['middleResourcePath'],pack['foregroundResourcePath']]:assert (ROOT/'game/unity/Assets/Game/Resources'/(path+'.png')).exists(),path
  file.write_text(json.dumps(pack,ensure_ascii=False,indent=2)+'\n',encoding='utf-8');records.append({'world':'W0'+str(world),'colossus':'green-return-dragon'if enemy=='formal'else enemy,'ownFar':pack['backgroundResourcePath'],'worldMiddle':pack['middleResourcePath'],'worldFront':pack['foregroundResourcePath'],'integrationVariant':enemy=='newborn-asteria'})
(ROOT/'docs/production/plan9-world-environment-bindings.json').write_text(json.dumps({'schemaVersion':1,'worlds':7,'battlefields':15,'bindings':records},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('PLAN9_WORLD_LAYERS_CONNECTED 7 worlds / 15 own battlefields')

"""Make QA sheets from Player screenshots; never modify game art."""
import json
from pathlib import Path
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[1];BASE=R/'tmp/plan12-common-tree/player'
entries=json.loads((R/'game/unity/Assets/Game/Resources/UI/heroine-weapon-trees.json').read_text(encoding='utf8'))['entries']
for start in range(0,len(entries),3):
 sheet=Image.new('RGB',(1920,1500),'#20252b');draw=ImageDraw.Draw(sheet)
 for row,e in enumerate(entries[start:start+3]):
  for col,stage in enumerate((0,2,4)):
   name=e['heroineId'][8:]+'-tree-stage-'+str(stage)
   im=Image.open(BASE/(name+'.png')).convert('RGB').crop((690,105,1880,1035));im.thumbnail((640,470))
   sheet.paste(im,(col*640,row*500+25));draw.text((col*640+10,row*500+5),name,fill='white')
 sheet.save(BASE/('review-'+str(start//3+1)+'.jpg'))
print('COMMON_TREE_REVIEW_SHEETS 8 / 72 actual Player frames')

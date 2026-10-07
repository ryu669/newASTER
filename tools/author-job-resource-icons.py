"""Original geometric job emblems, extending the code-native sanctuary icon system.
Every PNG has a different silhouette. No source-game pixels or ImageGen pixels edited.
"""
from pathlib import Path
from PIL import Image,ImageDraw
import math,json,hashlib,uuid,re
ROOT=Path(__file__).resolve().parents[1];OUT=ROOT/'game/unity/Assets/Game/Resources/UI/Jobs';OUT.mkdir(parents=True,exist_ok=True)
rows=[]
for job,color in [('fighter','#eaaf4c'),('berserker','#df6468'),('defender','#66cbe8'),('blaster','#b79aff'),('gunner','#80baa3'),('artist','#e8a1d7'),('healer','#72d594'),('panzer','#a7bbd4'),('alchemist','#ec9970'),('chaser','#68bce3'),('sniper','#ed8d98'),('gambler','#dcb56e'),('general','#7da8ed')]:
 im=Image.new('RGBA',(192,192));d=ImageDraw.Draw(im);ink='#162c3b';gold='#d9c493';w=9
 def line(p,c=color,width=w):d.line(p,fill=c,width=width,joint='curve')
 def poly(p,c=color):d.polygon(p,fill=c)
 def ring(box,c=color,width=w):d.ellipse(box,outline=c,width=width)
 if job=='fighter':
  for x,y in [(54,69),(127,69),(90,131)]:d.ellipse((x-26,y-26,x+26,y+26),fill=color);ring((x-17,y-17,x+17,y+17),gold,4)
 elif job=='berserker':
  poly([(26,34),(52,151),(78,84),(96,161),(118,84),(148,153),(167,35),(129,66),(97,45),(66,64)]);line([(55,40),(74,76)],gold,5)
 elif job=='defender':
  poly([(36,29),(96,15),(157,29),(151,113),(96,175),(42,113)]);poly([(52,43),(96,32),(140,43),(134,108),(96,151),(58,108)],ink);line([(96,50),(96,129)]);line([(67,88),(125,88)])
 elif job=='blaster':
  poly([(96,18),(144,72),(125,139),(96,174),(67,139),(48,72)]);line([(96,18),(96,174)],gold,4);line([(49,72),(96,92),(144,72)],gold,5)
 elif job=='gunner':
  for x,y in [(35,35),(96,64)]:
   d.rounded_rectangle((x,y,x+55,y+96),radius=9,fill=color);poly([(x+9,y),(x+27,y-20),(x+46,y)],gold)
   for j in range(3):line([(x+10,y+27+j*23),(x+45,y+27+j*23)],ink,5)
 elif job=='artist':
  line([(71,137),(71,47),(136,24),(136,112)],color,13);line([(72,65),(136,42)],color,10);d.ellipse((29,120,72,158),fill=color);d.ellipse((94,102,137,140),fill=color)
 elif job=='healer':
  poly([(96,157),(34,91),(33,51),(55,27),(79,27),(96,46),(112,27),(136,27),(159,51),(158,91)]);line([(56,85),(81,85),(90,64),(102,113),(114,85),(136,85)],ink,7)
 elif job=='panzer':
  poly([(49,25),(144,25),(174,96),(145,164),(48,164),(19,96)]);poly([(60,46),(132,46),(153,96),(130,142),(62,142),(42,96)],ink);poly([(69,58),(124,58),(141,96),(123,130),(70,130),(53,96)]);line([(59,94),(135,94)],gold,5)
 elif job=='alchemist':
  line([(78,37),(78,78),(49,133),(54,157),(137,157),(142,133),(114,78),(114,37)]);line([(71,35),(121,35)],gold,7);line([(58,127),(133,127)],gold,8)
  for a in range(5):
   ang=a*math.pi*2/5-math.pi/2;x=96+73*math.cos(ang);y=88+73*math.sin(ang);d.ellipse((x-11,y-11,x+11,y+11),fill=['#df7663','#ddc260','#9b79db','#e0dcc1','#82bfde'][a])
 elif job=='chaser':
  for i in range(12):
   a=i*math.pi/6;p=[(96+r*math.cos(a+b),96+r*math.sin(a+b)) for r,b in [(67,-.13),(86,-.13),(86,.13),(67,.13)]];poly(p)
  ring((36,36,156,156),color,20);ring((74,74,118,118),gold,9)
 elif job=='sniper':
  ring((38,38,154,154),color,10)
  for p in [[(96,14),(96,64)],[(96,128),(96,178)],[(14,96),(64,96)],[(128,96),(178,96)]]:line(p)
  d.ellipse((87,87,105,105),fill=gold)
 elif job=='gambler':
  d.rounded_rectangle((23,37,162,153),radius=15,fill=color);d.rectangle((37,53,147,124),fill=ink)
  for x in [44,79,114]:line([(x,66),(x+22,66),(x+7,109)],gold,7)
  line([(163,65),(178,43),(178,101)],color,8);d.ellipse((169,32,187,50),fill=gold)
 elif job=='general':
  line([(42,172),(42,25)],gold,10);poly([(48,29),(159,29),(139,64),(159,99),(48,99)]);poly([(83,51),(103,39),(123,51),(103,74)],gold);line([(29,173),(62,173)],color,9)
 path=OUT/(job+'.png');im.save(path)
 meta=Path(str(path)+'.meta')
 if not meta.exists():meta.write_text(re.sub(r'guid: [a-f0-9]+','guid: '+uuid.uuid4().hex,(ROOT/'game/unity/Assets/Game/Resources/Illustrations/r-standing-candidate-v1.png.meta').read_text(encoding='utf8')),encoding='utf8')
 rows.append(dict(job=job,path=str(path),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),width=192,height=192,origin='newaster-original-vector-geometry'))
canvas=Image.new('RGB',(960,576),'#132532')
for i,row in enumerate(rows):
 im=Image.open(row['path']);canvas.paste(im,((i%5)*192,(i//5)*192),im)
(ROOT/'tmp/plan10-next-four').mkdir(exist_ok=True,parents=True);canvas.save(ROOT/'tmp/plan10-next-four/job-icons.png')
(ROOT/'docs/production/job-resource-images.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('13 original resource PNGs, distinct geometry and digests.')

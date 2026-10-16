"""QA contact sheets from unchanged Player captures; never edits game assets."""
from pathlib import Path
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[1];B=R/'tmp/plan12-estreya/player'
groups={
 'expressions':['estreya-expression.'+v+'.png' for v in ['normal','joy','puzzled','determined']]+['estreya-sd.idle.png','estreya-sd.walk.png'],
 'poses':['estreya-sd.'+v+'.png' for v in ['talk','react','sit','work','look']]+['estreya-battle.png'],
 'events':['estreya-cg.'+str(i)+'.png' for i in range(5)],
 'gallery':['gallery-frames/estreya-cg.'+str(i)+'.png' for i in range(5)],
 'ui':['recruitment.png','recruit.png','formation.png','garden.png','journey.png','skill.png'],
 'actions':['command.png','attack0.png','attack1.png','attack2.png','battle-attack.png','battle-hit.png'],
 'tree':['tree0.png','tree2.png','tree4.png','trait.png','battle-cutin.png'],
}
for key,names in groups.items():
 if not all((B/n).exists() for n in names):continue
 w,h=960,565;s=Image.new('RGB',(w*2,h*((len(names)+1)//2)),'#20252b');draw=ImageDraw.Draw(s)
 for i,n in enumerate(names):
  im=Image.open(B/n);assert im.size==(1920,1080);im.thumbnail((w,540));s.paste(im,((i%2)*w,(i//2)*h+25));draw.text(((i%2)*w+8,(i//2)*h+6),n,fill='white')
 s.save(B/(key+'-review.jpg'))
 print(key)

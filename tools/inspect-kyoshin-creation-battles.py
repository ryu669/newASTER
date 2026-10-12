"""Extract visible battle evidence without copying source media into game assets."""
from pathlib import Path
import subprocess,json
import imageio_ffmpeg
from PIL import Image,ImageDraw
root=Path('D:/M02_gamemovie/巨神と誓女/思い出す');out=Path(__file__).resolve().parents[1]/'tmp/kyoshin-creation-battles';out.mkdir(parents=True,exist_ok=True)
ff=imageio_ffmpeg.get_ffmpeg_exe();records=[]
for key,duration in [('ノイア',60.40),('ユミル',70.34)]:
 source=root/f'巨神と誓女_{key}戦.mp4';times=list(range(0,int(duration),3))
 for t in times:
  p=out/f'{key}-{t:03}.png'
  if not p.exists():subprocess.run([ff,'-v','error','-ss',str(t),'-i',str(source),'-frames:v','1','-update','1','-y',str(p)],check=True,capture_output=True)
 for start in range(0,len(times),9):
  sheet=Image.new('RGB',(1800,990),'#222');draw=ImageDraw.Draw(sheet)
  for i,t in enumerate(times[start:start+9]):
   im=Image.open(out/f'{key}-{t:03}.png');im.thumbnail((600,305));x=i%3*600;y=i//3*330;sheet.paste(im,(x,y));draw.text((x+5,y+310),f'{t}s',fill='white')
  sheet.save(out/f'{key}-sheet-{start//9}.jpg',quality=95)
 records.append(dict(file=source.name,durationSeconds=duration,sampledSeconds=times))
(out/'sampling.json').write_text(json.dumps(records,ensure_ascii=False,indent=2),encoding='utf8')

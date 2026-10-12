from pathlib import Path
import subprocess
import imageio_ffmpeg
from PIL import Image,ImageDraw
out=Path('tmp/aa-after');out.mkdir(exist_ok=True);ff=imageio_ffmpeg.get_ffmpeg_exe()
times=list(range(0,525,5))
for t in times:
 p=out/f'{t:03}.png'
 if not p.exists():subprocess.run([ff,'-v','error','-ss',str(t),'-i','D:/M02_gamemovie/ANGELICAASTER/ストーリー/天使のその後.mkv','-frames:v','1','-update','1','-y',str(p)],check=True,capture_output=True)
for start in range(0,len(times),9):
 sheet=Image.new('RGB',(1920,1152),'#222');draw=ImageDraw.Draw(sheet)
 for i,t in enumerate(times[start:start+9]):
  im=Image.open(out/f'{t:03}.png');im.thumbnail((640,360));x=i%3*640;y=i//3*384;sheet.paste(im,(x,y));draw.text((x+5,y+362),f'{t//60}:{t%60:02} ({t}s)',fill='white')
 sheet.save(out/f'sheet-{start//9:02}.jpg',quality=95)

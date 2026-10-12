"""Extract timestamped evidence; original video stays local."""
import subprocess,json
from pathlib import Path
import imageio_ffmpeg
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[1];out=R/'tmp/aa-epilogue';out.mkdir(parents=True,exist_ok=True)
source=Path('D:/M02_gamemovie/ANGELICAASTER/ストーリー/エピローグ.mkv')
times=sorted(set(range(0,255,5))|set(range(12,24))|set(range(24,71,2)));ff=imageio_ffmpeg.get_ffmpeg_exe()
for t in times:
 if not (out/f'{t:03}.png').exists():subprocess.run([ff,'-v','error','-ss',str(t),'-i',str(source),'-frames:v','1','-update','1','-y',str(out/f'{t:03}.png')],check=True)
for start in range(0,len(times),9):
 sheet=Image.new('RGB',(1920,1152),'#222222');draw=ImageDraw.Draw(sheet)
 for index,t in enumerate(times[start:start+9]):
  im=Image.open(out/f'{t:03}.png');im.thumbnail((640,360));x=(index%3)*640;y=(index//3)*384
  sheet.paste(im,(x,y));draw.text((x+8,y+362),f'{t//60:02}:{t%60:02} ({t}s)',fill='white')
 sheet.save(out/f'review-{start//9+1}.jpg',quality=94)
(out/'sampling.json').write_text(json.dumps(dict(source=str(source),durationSeconds=255.4,sampledSeconds=times,scope='visible text frames; audio not transcribed'),ensure_ascii=False,indent=2),encoding='utf8')
print('AA_EPILOGUE_FRAMES_READY',len(times))

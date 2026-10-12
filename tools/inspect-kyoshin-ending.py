"""Timestamped ED and heroine 081 frames for comparison with AA epilogue."""
from pathlib import Path
import subprocess,json
import imageio_ffmpeg
from PIL import Image,ImageDraw
R=Path(__file__).resolve().parents[1];out=R/'tmp/kyoshin-ending';out.mkdir(parents=True,exist_ok=True);ff=imageio_ffmpeg.get_ffmpeg_exe();rows=[]
for key,source,duration in [('ed','D:/M02_gamemovie/巨神と誓女/巨神と誓女_データ/巨神と誓女_ED.mp4',150.28),('081','D:/M02_gamemovie/巨神と誓女/巨神と誓女_誓女データ/巨神と誓女_誓女081.mp4',211.41)]:
 times=sorted(set(range(0,int(duration),5)) | (set([12,18,22,28,32,38,42,48,52,58,62,68,72,78,82,88,92,98,102,108,112,118]) if key=='ed' else set()))
 for t in times:
  p=out/f'{key}-{t:03}.png'
  if not p.exists():subprocess.run([ff,'-v','error','-ss',str(t),'-i',source,'-frames:v','1','-update','1','-y',str(p)],check=True,capture_output=True)
 for start in range(0,len(times),9):
  sheet=Image.new('RGB',(1920,900),'#222');draw=ImageDraw.Draw(sheet)
  for i,t in enumerate(times[start:start+9]):
   im=Image.open(out/f'{key}-{t:03}.png');im.thumbnail((640,280));x=i%3*640;y=i//3*300;sheet.paste(im,(x,y));draw.text((x+5,y+282),f'{key} {t//60:02}:{t%60:02} ({t}s)',fill='white')
  sheet.save(out/f'{key}-review-{start//9+1}.jpg',quality=95)
 rows.append(dict(source=source,durationSeconds=duration,sampledSeconds=times))
(out/'sampling.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf8')
print('KYOSHIN_ENDING_FRAMES_READY',sum(len(r['sampledSeconds']) for r in rows))

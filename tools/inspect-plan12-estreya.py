import subprocess,re,json
from pathlib import Path
import imageio_ffmpeg
root=Path(__file__).resolve().parents[1];out=root/'tmp/plan12-estreya/reference';out.mkdir(parents=True,exist_ok=True);ff=imageio_ffmpeg.get_ffmpeg_exe();rows=[]
for kind,folder,filename,times in [('profile','天使','026_エストレヤ.mkv',[0,120,180,420,630,780,840,880]),('battle','天使戦闘','026戦闘_エストレヤ.mkv',[0,5,10,15,20,25,30,40,50,60,80,100])]:
 p=Path('D:/M02_gamemovie/ANGELICAASTER')/folder/filename
 info=subprocess.run([ff,'-hide_banner','-i',str(p)],capture_output=True,encoding='utf8',errors='replace').stderr;m=re.search(r'Duration: (\d+):(\d+):(\d+\.\d+)',info);duration=int(m[1])*3600+int(m[2])*60+float(m[3]);times=[t for t in times if t<duration]
 for t in times:subprocess.run([ff,'-v','error','-ss',str(t),'-i',str(p),'-frames:v','1','-update','1','-y',str(out/f'{kind}-{t:04}.png')],check=True)
 rows.append(dict(path=str(p),durationSeconds=duration,sampledSeconds=times))
(out/'sampling.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf8')
print('ESTREYA_REFERENCE_SAMPLED')

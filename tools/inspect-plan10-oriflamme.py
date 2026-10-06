"""Read-only local reference sampling. Source video pixels stay out of shipped assets."""
import subprocess,re,json
from pathlib import Path
from PIL import Image,ImageDraw
FF=Path(r'C:/Users/nishi/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe')
OUT=Path('tmp/plan10-oriflamme/reference');OUT.mkdir(parents=True,exist_ok=True)
rows=[]
for key,folder,name in [('oriflamme-character','天使','032_オリフラム.mkv'),('oriflamme-battle','天使戦闘','032戦闘_オリフラム.mkv')]:
    path=Path(r'D:/M02_gamemovie/ANGELICAASTER')/folder/name
    info=subprocess.run([str(FF),'-hide_banner','-i',str(path)],capture_output=True,text=True,encoding='utf-8',errors='replace').stderr
    m=re.search(r'Duration: (\d+):(\d+):(\d+\.\d+)',info);duration=int(m[1])*3600+int(m[2])*60+float(m[3])
    times=[0,5,15,30,60,90,120,150,180,210] if 'battle' in key else [0,15,60,180,360,540,720,900,1080,1260,1440,1620]
    times=[s for s in times if s<duration]
    for second in times:
        subprocess.run([str(FF),'-v','error','-ss',str(second),'-i',str(path),'-frames:v','1','-update','1','-y',str(OUT/f'{key}-{second:04}.png')],check=True)
    sheet=Image.new('RGB',(1280,((len(times)+2)//3)*260),'#10131c');draw=ImageDraw.Draw(sheet)
    for i,second in enumerate(times):
        image=Image.open(OUT/f'{key}-{second:04}.png');image.thumbnail((420,236));x=i%3*426;y=i//3*260
        sheet.paste(image,(x,y+22));draw.text((x+4,y+3),f'{key} {second}s',fill='white')
    sheet.save(OUT/f'{key}-contact.jpg')
    rows.append(dict(key=key,path=str(path),durationSeconds=duration,sampledSeconds=times))
(OUT/'sampling.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(rows,ensure_ascii=False))

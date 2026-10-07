"""Read-only reference observations for sequential Plan 10 forms. No source frames ship."""
import subprocess,re,json
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[1]
FF=Path(r'C:/Users/nishi/.cache/codex-runtimes/codex-primary-runtime/dependencies/python/Lib/site-packages/imageio_ffmpeg/binaries/ffmpeg-win-x86_64-v7.1.exe')
OUT=ROOT/'tmp/plan10-next-four/reference';OUT.mkdir(parents=True,exist_ok=True)
rows=[]
for key,number,name in [('slayer-swim','056','スレイヤー水着'),('arcane','012','アルケイン'),('arcane-academy','013','アルケイン学園'),('shangrila','050','シャングリラ')]:
 for kind,folder in [('character','天使'),('battle','天使戦闘')]:
  path=Path(r'D:/M02_gamemovie/ANGELICAASTER')/folder/(number+('戦闘' if kind=='battle' else '')+'_'+name+'.mkv')
  info=subprocess.run([str(FF),'-hide_banner','-i',str(path)],capture_output=True,text=True,encoding='utf8',errors='replace').stderr
  m=re.search(r'Duration: (\d+):(\d+):(\d+\.\d+)',info);duration=int(m[1])*3600+int(m[2])*60+float(m[3])
  times=[0,5,15,30,60,120,180] if kind=='battle' else [0,15,60,180,360,540,720,900,1080]
  times=[t for t in times if t<duration];prefix=key+'-'+kind
  for second in times:subprocess.run([str(FF),'-v','error','-ss',str(second),'-i',str(path),'-frames:v','1','-update','1','-y',str(OUT/f'{prefix}-{second:04}.png')],check=True)
  sheet=Image.new('RGB',(1280,((len(times)+2)//3)*260),'#10131c');d=ImageDraw.Draw(sheet)
  for i,second in enumerate(times):
   im=Image.open(OUT/f'{prefix}-{second:04}.png');im.thumbnail((420,236));x=i%3*426;y=i//3*260;sheet.paste(im,(x,y+22));d.text((x+4,y+3),f'{prefix} {second}s',fill='white')
  sheet.save(OUT/f'{prefix}-contact.jpg');rows.append(dict(key=key,kind=kind,path=str(path),durationSeconds=duration,sampledSeconds=times))
(OUT/'sampling.json').write_text(json.dumps(rows,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('Eight supplied videos sampled; source pixels remain local-only.')

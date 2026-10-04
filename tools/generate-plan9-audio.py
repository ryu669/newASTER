"""Original deterministic Plan9 compositions; no external samples or melodies."""
import hashlib,json,wave,uuid
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'game/unity/Assets/Game/Resources/Audio'
RATE=44100
TRACKS={
 'title':(.5,[74,77,81,79,76,72,74,69,72,76,79,81,77,74,72,69],[50,46,53,48]),
 'battle':(.375,[62,69,65,74,69,77,74,81,79,74,77,69,72,65,69,62],[38,41,43,36]),
 'garden':(.625,[72,76,79,76,74,77,81,79,76,72,74,69,72,67,69,72],[48,53,45,50]),
 'adv':(.75,[69,72,76,74,72,69,67,65,67,72,74,76,72,69,67,64],[45,41,48,43])}

def hz(m):return 440*2**((m-69)/12)
def env(p,length,decay=3):return np.minimum(p/.012,1)*np.minimum((length-p)/.025,1)*np.exp(-decay*p)
def tone(t,f):return np.sin(2*np.pi*f*t)+.18*np.sin(4*np.pi*f*t)+.045*np.sin(6*np.pi*f*t)
def write(name,signal):
 signal=np.clip(signal,-.65,.65);signal[0]=0;signal[-1]=0
 pcm=np.round(signal*32767).astype('<i2');path=DEST/(name+'.wav')
 with wave.open(str(path),'wb')as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(RATE);w.writeframes(pcm.tobytes())
 meta=path.with_suffix('.wav.meta')
 if not meta.exists():
  template=(DEST/'candidate-bgm-v2.wav.meta').read_text();lines=template.splitlines();lines=[('guid: '+uuid.uuid4().hex)if l.startswith('guid: ')else l for l in lines];meta.write_text('\n'.join(lines)+'\n')
 return {'resourcePath':'Audio/'+name,'file':str(path.relative_to(ROOT)).replace('\\','/'),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'durationSeconds':len(signal)/RATE,'channels':2,'sampleRate':RATE,'peak':float(np.max(np.abs(signal))),'source':'tools/generate-plan9-audio.py','status':'candidate-awaiting-functional-and-limited-listening-review','externalSamples':False}

def main():
 DEST.mkdir(parents=True,exist_ok=True);records=[]
 for name,(beat,melody,roots)in TRACKS.items():
  duration=64*beat;t=np.arange(round(RATE*duration))/RATE;p=t%beat;steps=(t/beat).astype(int)
  f=np.array([hz(melody[s%16])for s in range(64)])[steps];lead=tone(p,f)*env(p,beat,4)*.06
  pad=np.zeros_like(t)
  for bar,root in enumerate(roots*4):
   local=t-bar*4*beat;mask=(local>=0)&(local<4*beat);phase=local[mask];envelope=np.sin(np.pi*phase/(4*beat))**2
   for interval in [0,7,12]:pad[mask]+=.018*tone(phase,hz(root+interval))*envelope
  bass=tone(p,np.array([hz(roots[(s//4)%4]-12)for s in range(64)])[steps])*env(p,beat,5)*(.018 if name!='battle'else .04)
  percussion=np.zeros_like(t)
  if name=='battle':percussion=.018*np.sin(2*np.pi*(80*p-60*p*p))*np.exp(-p*25)
  left=lead+pad+bass+percussion;right=lead*.9+pad*1.05+bass+percussion
  signal=np.stack((left,right),axis=1);fade=np.minimum(t/.012,1)*np.minimum((duration-t)/.012,1);signal*=fade[:,None]
  records.append(write('plan9-bgm-'+name+'-candidate-v1',signal))
 for name,notes in {'confirm':[76,81],'cancel':[72,67],'page':[67,74,79],'unlock':[72,76,79,84]}.items():
  duration=.18*len(notes)+.16;t=np.arange(round(RATE*duration))/RATE;signal=np.zeros_like(t)
  for i,m in enumerate(notes):
   local=t-i*.18;mask=(local>=0)&(local<.28);p=local[mask];signal[mask]+=.14*tone(p,hz(m))*env(p,.28,12)
  records.append(write('plan9-se-'+name+'-candidate-v1',np.stack((signal,signal),axis=1)))
 output=ROOT/'docs/production/plan9-audio-source-validation.json';output.write_text(json.dumps({'schemaVersion':1,'provenance':'newaster-original-procedural-composition','seed':'deterministic-no-random','tracks':records,'listeningSeconds':0,'performanceMeasured':False},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
 print('PLAN9_ORIGINAL_AUDIO_GENERATED 4 scene BGM and 4 UI SE; no listening performed')
if __name__=='__main__':main()

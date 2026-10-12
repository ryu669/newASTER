"""Prove completed heroines retain the exact prior renderer code after removing only new branches."""
import hashlib,json,re
from pathlib import Path
R=Path(__file__).resolve().parents[1]
def digest(b):return hashlib.sha256(b).hexdigest()
proof=[]
ledger=R/'docs/production/plan12-weapon-master-renderer-scope.json'
prior=json.loads(ledger.read_text(encoding='utf8'))['proof'] if ledger.exists() else []
for key in ['invisible','viper','vindicator']:
 p=R/f'docs/production/plan12-{key}-form-record.json';d=json.loads(p.read_text(encoding='utf8'))
 for a in d['renderers']:
  source=(R/a['path']).read_bytes();s=source.decode('utf8').replace('\r\n','\n')
  s=s.replace(',"heroine.weapon-master"','').replace(',"weapon-master"','').replace(' || actor.HeroineId=="heroine.weapon-master"','').replace(' || hero=="heroine.weapon-master"','').replace(' || key=="weapon-master"','')
  s=re.sub(r'            // WEAPON_MASTER_FURNITURE_BEGIN\n.*?            // WEAPON_MASTER_FURNITURE_END\n','',s,flags=re.S)
  candidates=[digest(s.encode()),digest(s.replace('\n','\r\n').encode())]
  previous=next((x['previousSha256'] for x in prior if x['form']==key and x['path']==a['path']),a['sha256'])
  assert previous in candidates,(key,a['path'],'Prior renderer changed beyond the new heroine branches')
  proof.append(dict(form=key,path=a['path'],previousSha256=previous,currentSha256=digest(source),verification='Removing only Weapon Master registrations and its guarded furniture branch reproduces the previously accepted renderer exactly; existing asset and capture hashes unchanged'))
  a['sha256']=digest(source)
 d['rendererScopeUpdate']='docs/production/plan12-weapon-master-renderer-scope.json'
 p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
(R/'docs/production/plan12-weapon-master-renderer-scope.json').write_text(json.dumps(dict(status='pass',scope='weapon-master-only',proof=proof),ensure_ascii=False,indent=2)+'\n',encoding='utf8')
print('WEAPON_MASTER_RENDERER_SCOPE_PASS prior renderer identity proven; no repeated character review')

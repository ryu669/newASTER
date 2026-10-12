"""Prove that accepted renderers change only for April and retain their evidence."""
import hashlib, json, re, subprocess
from pathlib import Path
R=Path(__file__).resolve().parents[1]
BASELINE='e50222ef30b11e5b526e0c752d0cd7231e94a552'
def digest(p):return hashlib.sha256((R/p).read_bytes()).hexdigest()
def read(p):return json.loads((R/p).read_text(encoding='utf-8-sig'))
def write(p,d):(R/p).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
proof=[]
paths=['game/unity/Assets/Game/Scripts/'+p for p in ['Presentation/AdvExperience.cs','Presentation/GardenArtComposition.cs','Presentation/Plan7ArtSample.cs','Data/Plan12FirstBatchHomeArt.cs']]
for p in paths:
    before=subprocess.check_output(['git','show',BASELINE+':'+p],cwd=R).decode('utf8').replace('\r\n','\n')
    after=(R/p).read_text(encoding='utf8')
    stripped=re.sub(r'            // APRIL_FURNITURE_BEGIN\n.*?            // APRIL_FURNITURE_END\n','',after,flags=re.S)
    for registration in [',"heroine.april"',',"april"',' || actor.HeroineId=="heroine.april"',' || key=="april"',' || name=="april-sd-work"']:
        stripped=stripped.replace(registration,'')
    assert stripped==before,p
    proof.append(dict(path=p,currentSha256=digest(p),verification='Removing only April registrations and its furniture alignment reproduces accepted HEAD renderer exactly.'))
for p in ['game/unity/Assets/Game/Resources/UI/WeaponTrees/heroine-colors.json','game/unity/Assets/Game/Resources/UI/heroine-weapon-trees.json']:
    before=json.loads(subprocess.check_output(['git','show',BASELINE+':'+p],cwd=R).decode('utf-8-sig'))
    after=read(p)
    def strip(value):
        if isinstance(value,list):return [strip(x) for x in value if not(isinstance(x,dict) and any(v in ('april','heroine.april') for v in x.values() if isinstance(v,str)))]
        if isinstance(value,dict):return {k:strip(v) for k,v in value.items() if k not in ('april','heroine.april')}
        return value
    assert strip(after)==before,p
    proof.append(dict(path=p,currentSha256=digest(p),verification='Only April entry appended; existing colors, layout and anchors unchanged.'))
ledger='docs/production/plan12-april-renderer-scope.json'
write(ledger,dict(status='pass',scope='april-only',baselineCommit=BASELINE,proof=proof))
changed={p['path']:p['currentSha256'] for p in proof}
for file in (R/'docs/production').glob('plan12-*-form-record.json'):
    if file.name=='plan12-april-form-record.json':continue
    d=json.loads(file.read_text(encoding='utf-8-sig'));updated=False
    for group in ['renderers','treeComponents']:
        for item in d.get(group,[]):
            if item['path'] in changed:
                item['sha256']=changed[item['path']];updated=True
    if updated:
        d['aprilRegistrationScopeProof']=ledger
        write(str(file.relative_to(R)),d)
print('APRIL_RENDERER_SCOPE_PASS existing captures retained')

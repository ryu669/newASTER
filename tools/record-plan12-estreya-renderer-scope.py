"""Prove that accepted renderers change only for Estreya and retain their evidence."""
import hashlib, json, re, subprocess
from pathlib import Path
R=Path(__file__).resolve().parents[1]
BASELINE='c250125'
def digest(p):return hashlib.sha256((R/p).read_bytes()).hexdigest()
def read(p):return json.loads((R/p).read_text(encoding='utf-8-sig'))
def write(p,d):(R/p).write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf8')
proof=[]
paths=['game/unity/Assets/Game/Scripts/'+p for p in ['Presentation/AdvExperience.cs','Presentation/GardenArtComposition.cs','Presentation/Plan7ArtSample.cs','Data/Plan12FirstBatchHomeArt.cs']]
for p in paths:
    before=subprocess.check_output(['git','show',BASELINE+':'+p],cwd=R).decode('utf8').replace('\r\n','\n')
    after=(R/p).read_text(encoding='utf8')
    stripped=re.sub(r'            // ESTREYA_FURNITURE_BEGIN\n.*?            // ESTREYA_FURNITURE_END\n','',after,flags=re.S)
    for registration in [',"heroine.estreya"',',"estreya"',' || actor.HeroineId=="heroine.estreya"',' || key=="estreya"',' || name=="estreya-sd-work"']:
        stripped=stripped.replace(registration,'')
    assert stripped==before,p
    proof.append(dict(path=p,currentSha256=digest(p),verification='Removing only Estreya registrations and its furniture alignment reproduces accepted HEAD renderer exactly.'))
for p in ['game/unity/Assets/Game/Resources/UI/WeaponTrees/heroine-colors.json','game/unity/Assets/Game/Resources/UI/heroine-weapon-trees.json']:
    before=json.loads(subprocess.check_output(['git','show',BASELINE+':'+p],cwd=R).decode('utf-8-sig'))
    after=read(p)
    def strip(value):
        if isinstance(value,list):return [strip(x) for x in value if not(isinstance(x,dict) and any(v in ('estreya','heroine.estreya') for v in x.values() if isinstance(v,str)))]
        if isinstance(value,dict):return {k:strip(v) for k,v in value.items() if k not in ('estreya','heroine.estreya')}
        return value
    assert strip(after)==before,p
    proof.append(dict(path=p,currentSha256=digest(p),verification='Only Estreya entry appended; existing colors, layout and anchors unchanged.'))
ledger='docs/production/plan12-estreya-renderer-scope.json'
write(ledger,dict(status='pass',scope='estreya-only',baselineCommit=BASELINE,proof=proof))
changed={p['path']:p['currentSha256'] for p in proof}
for file in (R/'docs/production').glob('plan12-*-form-record.json'):
    if file.name=='plan12-estreya-form-record.json':continue
    d=json.loads(file.read_text(encoding='utf-8-sig'));updated=False
    for group in ['renderers','treeComponents']:
        for item in d.get(group,[]):
            if item['path'] in changed:
                item['sha256']=changed[item['path']];updated=True
    if updated:
        d['estreyaRegistrationScopeProof']=ledger
        write(str(file.relative_to(R)),d)
print('ESTREYA_RENDERER_SCOPE_PASS existing captures retained')

"""Record individually bound production assets; no pixel/audio edits."""
import json, hashlib, pathlib, uuid
root=pathlib.Path(__file__).resolve().parents[1]
resources=root/'game/unity/Assets/Game/Resources'
home=json.loads((root/'tmp/plan9-home-catalog.json').read_text(encoding='utf-8-sig'))
paths={a['resourcePath'] for a in home['assets']}
battle_evidence={}
def collect(v):
    if isinstance(v,dict):
        for k,x in v.items():
            if k.lower().endswith('resourcepath') and isinstance(x,str) and x: paths.add(x)
            else: collect(x)
    elif isinstance(v,list):
        for x in v: collect(x)
for p in resources.joinpath('Illustrations').glob('battle-*.json'):
    if p.name=='battle-preview.json': continue
    before=set(paths)
    data=json.loads(p.read_text(encoding='utf-8-sig'))
    collect(data)
    evidence='docs/production/plan9-'+p.stem.removeprefix('battle-').removesuffix('-candidate-v1')+'-art-validation.json'
    for path in paths:
        if path not in p.read_text(encoding='utf-8-sig'): continue
        battle_evidence[path]=[evidence] if (root/evidence).exists() else ['docs/production/plan7-validation.md']
heroes=['slayer','iconoclast','undermine','echidna','excalipan']
for hero in heroes:
    for pose in ['idle','sit','work','look']: paths.add(f'Illustrations/{hero}-sd-{pose}-candidate-v1')
    paths.add(f'Illustrations/{hero}-portrait-candidate-v1')
for name in ['bgm-title','bgm-battle','bgm-garden','bgm-adv','se-confirm','se-cancel','se-page','se-unlock']: paths.add(f'Audio/plan9-{name}-candidate-v1')
for name in ['hit','shield','heal','break','victory']: paths.add(f'Audio/candidate-{name}-v2')
rows=[]
for path in sorted(paths):
    matches=[resources/(path+ext) for ext in ['.png','.wav'] if (resources/(path+ext)).is_file()]
    if len(matches)!=1: raise ValueError(f'Asset file missing/ambiguous: {path}')
    p=matches[0]
    if p.suffix=='.wav': evidence=['docs/production/plan9-audio-source-validation.json'] if 'plan9-' in path else ['docs/production/plan7-audio-v2.md']
    elif any(hero in path for hero in heroes):
        hero=next(h for h in heroes if h in path)
        evidence=[f'docs/production/plan9-{hero}-asset-validation.json',f'docs/production/plan9-{hero}-expression-render-validation.json',f'docs/production/plan9-{hero}-cg-validation.json']
    elif '/world-' in path or any(x in path for x in ['garden-planter','garden-obelisk','garden-birdbath','garden-telescope','garden-well','garden-lantern','garden-brazier']): evidence=['docs/production/plan9-garden-art-source-validation.json','docs/production/plan9-garden-runtime-validation.json']
    else: evidence=['docs/production/plan9-garden-runtime-validation.json'] if '/garden-' in path or '/forest-' in path else [f'docs/production/{q.name}' for q in sorted((root/'docs/production').glob('plan9-*-art-validation.json')) if any(part in path for part in q.stem.removeprefix('plan9-').removesuffix('-art-validation').split())]
    if not evidence and path in battle_evidence: evidence=battle_evidence[path]
    if '/green-' in path: evidence=['docs/production/plan7-validation.md']
    evidence=[e for e in evidence if (root/e).exists()]
    if not evidence and path in battle_evidence: evidence=battle_evidence[path]
    if not evidence: raise ValueError(f'Missing review evidence: {path}')
    rows.append(dict(resourcePath=path,file=p.relative_to(root).as_posix(),sha256=hashlib.sha256(p.read_bytes()).hexdigest(),bytes=p.stat().st_size,adoption='initial-five-distribution-candidate',origin='newaster-original-procedural-audio' if p.suffix=='.wav' else 'newaster-imagegen-original',evidence=evidence))
manifest=dict(schemaVersion=1,version='plan9-initial-five-rc1-2026-10-05',scope='individually bound initial-five assets; excludes rejected unused alternatives',physicalInputCount=0,listeningSeconds=0,performanceMeasured=False,assets=rows)
(root/'docs/production/plan9-asset-adoption.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
code='using System;\nusing System.Collections.Generic;\nusing NewAster.Core;\nnamespace NewAster.Data\n{\n    public static class ProductionAssetAcceptance\n    {\n        public const string Version="'+manifest['version']+'";\n        public static readonly IReadOnlyDictionary<string,string> Hashes=new System.Collections.ObjectModel.ReadOnlyDictionary<string,string>(new Dictionary<string,string>{\n'
code+=''.join('            {"'+r['resourcePath']+'","'+r['sha256']+'"},\n' for r in rows)
code+='        });\n        public static void Adopt(HomeExperienceCatalog home)\n        {\n            foreach(var asset in home.assets){if(string.IsNullOrWhiteSpace(asset.resourcePath) || !Hashes.ContainsKey(asset.resourcePath))throw new ArgumentException("Unaccepted production asset: "+asset.id);asset.placeholder=false;}\n        }\n    }\n}\n'
p=root/'game/unity/Assets/Game/Scripts/Data/ProductionAssetAcceptance.cs';p.write_text(code,encoding='utf-8')
meta=pathlib.Path(str(p)+'.meta')
if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
print('PLAN9_ASSET_ADOPTION_RECORDED',len(rows))

"""Audit original story sources. Write a runtime resource only when the full scope exists."""
import argparse,hashlib,json,re
from pathlib import Path

parser=argparse.ArgumentParser()
parser.add_argument('--write-full',action='store_true')
args=parser.parse_args()
root=Path(__file__).resolve().parents[1]
sources=root/'game/story-source/plan9'
resource=root/'game/unity/Assets/Game/Resources'
world=(root/'game/unity/Assets/Game/Scripts/Data/WorldCatalog.cs').read_text(encoding='utf-8-sig')
colossi=list(dict.fromkeys(re.findall(r'"(colossus\.[a-z-]+)"',world)))
heroes=['heroine.slayer','heroine.iconoclast','heroine.undermine','heroine.echidna','heroine.excalipan']
owners=set(colossi+heroes)
chapters=[];events=[];provenance=[]
for path in sorted(sources.glob('*.json')):
    data=json.loads(path.read_text(encoding='utf-8-sig'))
    provenance.append(dict(path=path.relative_to(root).as_posix(),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    for index,original in enumerate(data.get('chapters',[]),1):
        c=dict(original);owner=c.get('ownerId',data.get('ownerId'))
        assert owner in owners,f'Unknown story owner: {owner}'
        c['ownerId']=owner;c.setdefault('id',owner+('.collection.chapter.' if owner in colossi else '.poem-chapter.')+str(index))
        c.setdefault('backgroundResourcePath',data.get('backgroundResourcePath'))
        per=8 if owner in colossi else 6
        assert len(c['poems'])==per,f'Wrong poem count: {c["id"]}'
        normalized=[]
        for i,value in enumerate(c['poems']):
            p=dict(value) if isinstance(value,dict) else dict(text=value[0],body=value[1])
            p.setdefault('id',owner+'.collection.poem.'+str((index-1)*per+i+1).zfill(2))
            assert p['text'] and p['text'] in p['body'],f'Poem not quoted: {p["id"]}'
            if owner in heroes:assert p.get('sourcePoemId') and p.get('reason'),f'Unexplained correspondence: {p["id"]}'
            normalized.append(p)
        c['poems']=normalized
        for key in ['title','introduction','conclusion','backgroundResourcePath']:assert c.get(key),f'Missing {key}: {c["id"]}'
        assert (resource/(c['backgroundResourcePath']+'.png')).is_file(),f'Missing chapter background: {c["id"]}'
        chapters.append(c)
    events.extend(data.get('events',[]))
poems=[p for c in chapters for p in c['poems']]
for values,key in [(chapters,'id'),(poems,'id'),(poems,'text'),(poems,'body'),(events,'id')]:
    assert len({v[key] for v in values})==len(values),f'Duplicate authored {key}'
for c in chapters:
    assert all('【オリジナル試遊本文】' not in c[k] and '【動作検証用' not in c[k] for k in ['introduction','conclusion'])
for p in poems:
    if p.get('sourcePoemId'):assert any(s['id']==p['sourcePoemId'] for s in poems),f'Missing authored source: {p["id"]}'
expected={owner+('.collection.chapter.' if owner in colossi else '.poem-chapter.')+str(i) for owner in owners for i in range(1,4)}
missing=sorted(expected-{c['id'] for c in chapters})
full=not missing and len(poems)==450 and len(events)==25
output=dict(schemaVersion=1,contentVersion='production-story-2026-10-04',provenance='newaster-original',chapters=chapters,events=events)
draft=root/'tmp/plan9-story-draft.json'
draft.parent.mkdir(parents=True,exist_ok=True)
draft.write_text(json.dumps(output,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
report=dict(schemaVersion=1,status='full-source-awaiting-core-validation' if full else 'partial-source-not-connected',chapters=len(chapters),poems=len(poems),events=len(events),required=dict(chapters=60,poems=450,events=25),missingChapterIds=missing,sources=provenance)
(root/'docs/production/plan9-story-source-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
if args.write_full:
    assert full,'Refusing runtime resource until all chapters, poems and events are authored.'
    target=resource/'Story/plan9-story-content.json';target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(draft.read_text(encoding='utf-8'),encoding='utf-8')
print(f'PLAN9_STORY_SOURCE_AUDIT chapters={len(chapters)}/60 poems={len(poems)}/450 events={len(events)}/25 full={full}')

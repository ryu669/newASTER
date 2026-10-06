"""Import ID-tagged editorial text without changing event unlocks or artwork."""
import argparse, json, re
from pathlib import Path

p = argparse.ArgumentParser()
p.add_argument('source', type=Path)
a = p.parse_args()
root = Path(__file__).resolve().parents[1]
text = a.source.read_text(encoding='utf-8-sig')
records = {}
for block in re.split(r'^={20,}\s*$', text, flags=re.M)[1:]:
    match = re.fullmatch(r'\s*([^\n]+)\nID: ([^\n]+)\n人物ID: ([^\n]+)\n\s*([\s\S]+?)\s*', block)
    if not match:
        raise ValueError('Invalid recollection section')
    title, identity, owner, body = match.groups()
    lines = [line.strip() for line in body.splitlines() if line.strip()]
    assert identity not in records, identity
    records[identity] = (title.strip(), owner.strip(), lines)
updates = []
for path in sorted((root/'game/story-source/plan9').glob('*-events.json')):
    data = json.loads(path.read_text(encoding='utf-8-sig'))
    for event in data['events']:
        title, owner, lines = records.pop(event['id'])
        assert owner == event['ownerId']
        assert len(lines) == len(event['expressions']), event['id']
        event['title'], event['paragraphs'] = title, lines
    updates.append((path, data))
assert not records, 'Unmatched event IDs'
assert sum(len(d['events']) for _, d in updates) == 25
for path, data in updates:
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print('RECOLLECTION_IMPORT_PASS 25 events; unlocks, IDs, expressions and artwork retained')

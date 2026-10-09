"""Check revised prose, save-facing identity, and authoring regeneration."""
import json
import re
import runpy
import subprocess
import argparse
from pathlib import Path
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
path = ROOT / 'game/unity/Assets/Game/Resources/Story/plan10-shangrila-story-content.json'
story = json.loads(path.read_text(encoding='utf-8-sig'))
parser = argparse.ArgumentParser()
parser.add_argument('--base-ref', default='81b071f', help='Story baseline before character prose revision')
args = parser.parse_args()
original = json.loads(subprocess.check_output(['git', 'show', args.base_ref + ':' + path.relative_to(ROOT).as_posix()], cwd=ROOT).decode('utf-8-sig'))

def strings(value):
    if isinstance(value, str):
        yield value
    elif isinstance(value, list):
        for item in value:
            yield from strings(item)
    elif isinstance(value, dict):
        for item in value.values():
            yield from strings(item)

def check_dialogue(catalog):
    for kind in ['chapters', 'events']:
        for record in catalog[kind]:
            owner = record['ownerId']
            if not owner.startswith('heroine.'):
                continue
            for value in strings(record):
                for quote in re.findall(r'[「『]([^」』]*)[」』]', value):
                    assert 'あなた' not in quote and 'きみ' not in quote, (record['id'], quote)

check_dialogue(story)
for kind in ['chapters', 'events']:
    before = {r['id']: r for r in original[kind]}
    after = {r['id']: r for r in story[kind]}
    assert before.keys() == after.keys(), f'{kind}: changed saved IDs'
    for rid, record in after.items():
        old = before[rid]
        for key in ['ownerId', 'affectionRequired', 'establishesLover', 'expressions', 'cgResourcePath', 'backgroundResourcePath']:
            assert record.get(key) == old.get(key), (rid, key)
        if record['ownerId'].startswith('colossus.'):
            assert record == old, f'Unrelated colossus prose changed: {rid}'
        if kind == 'chapters':
            assert [(p['id'], p.get('sourcePoemId')) for p in record['poems']] == [(p['id'], p.get('sourcePoemId')) for p in old['poems']]
            assert all(p['text'] in p['body'] for p in record['poems']), rid
        else:
            assert len(record['paragraphs']) == len(old['paragraphs']), rid

# Capture generator output in memory to verify that regeneration cannot restore old prose.
generated = []
def capture(target, data, *args, **kwargs):
    if target.suffix == '.json':
        generated.append(json.loads(data))
    return len(data)
with patch.object(Path, 'write_text', capture):
    for script in sorted((ROOT / 'tools').glob('author-plan10-*-story.py')):
        runpy.run_path(str(script))
assert len(generated) == 9, len(generated)
for catalog in generated:
    check_dialogue(catalog)
for person in {r['ownerId'] for r in story['events']}:
    events = [r for r in story['events'] if r['ownerId'] == person]
    assert len(events) == 5 and any('指揮官' in text for e in events for text in e['paragraphs']), person
print('STORY_CHARACTER_CONSISTENCY_PASS 15 forms / 45 heroine chapters / 75 events / stable IDs / 9 generators')

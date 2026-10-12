"""Validate implemented-person authoring documentation against runtime catalogs."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
index = json.loads((ROOT / 'docs/characters/index.json').read_text(encoding='utf-8'))
combat = json.loads((ROOT / index['combatSource']).read_text(encoding='utf-8-sig'))
story = json.loads((ROOT / index['storySource']).read_text(encoding='utf-8-sig'))
expected = {}
for hero in combat['heroines']:
    expected.setdefault(hero.get('personId', hero['id']), set()).add(hero['id'])
actual = {}
for person in index['people']:
    pid = person['personId']
    assert pid not in actual, f'Duplicate person: {pid}'
    actual[pid] = set(person['formIds'])
    assert len(actual[pid]) == len(person['formIds']), f'Duplicate form: {pid}'
    path = ROOT / person['profile']
    text = path.read_text(encoding='utf-8')
    for heading in ['概要・性格', '口調', '葛藤・関係の進め方', '避ける描写', '参考資料・採用判断']:
        assert '## ' + heading in text, f'{pid}: missing {heading}'
    for form in person['formIds']:
        assert f'`{form}`' in text, f'{pid}: missing form {form}'
        for kind in ['chapters', 'events']:
            records = [r for r in story[kind] if r.get('ownerId') == form]
            assert records, f'{form}: no runtime {kind}'
            for record in records:
                assert f'`{record["id"]}`' in text, f'{form}: missing {record["id"]}'
    for target in re.findall(r'\]\(([^)]+)\)', text):
        if '://' not in target:
            assert (path.parent / target.split('#')[0]).exists(), f'{pid}: broken link {target}'
assert actual == expected, f'Person/form coverage differs: {actual.keys()} / {expected.keys()}'
development_ids = set()
for person in index.get('developmentPeople', []):
    pid = person['personId']
    assert pid not in actual and pid not in development_ids, f'Duplicate development person: {pid}'
    development_ids.add(pid)
    manifest = json.loads((ROOT / person['manifest']).read_text(encoding='utf-8'))
    assert person['available'] is False and manifest['available'] is False, f'Premature release: {pid}'
    draft_combat = json.loads((ROOT / person['combatSource']).read_text(encoding='utf-8-sig'))
    draft_story = json.loads((ROOT / person['storySource']).read_text(encoding='utf-8-sig'))
    text = (ROOT / person['profile']).read_text(encoding='utf-8')
    for heading in ['概要・性格', '口調', '葛藤・関係の進め方', '避ける描写', '参考資料・採用判断']:
        assert '## ' + heading in text, f'{pid}: missing {heading}'
    for form in person['formIds']:
        assert form not in {h['id'] for h in combat['heroines']}, f'Draft form already live: {form}'
        assert any(h['id'] == form and h['personId'] == pid for h in draft_combat['heroines'])
        for kind in ['chapters', 'events']:
            records = [r for r in draft_story[kind] if r['ownerId'] == form]
            assert records and all('`' + r['id'] + '`' in text for r in records), f'Missing development story references: {form}'
print(f'CHARACTER_DOCS_PASS people={len(actual)} forms={sum(map(len, actual.values()))}')
print(f'DEVELOPMENT_CHARACTER_DOCS_PASS people={len(development_ids)}; not included in live counts')

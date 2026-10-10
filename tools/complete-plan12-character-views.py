"""Record a visually approved, scoped capture without reopening completed views."""
import hashlib, json, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
folder = (ROOT / sys.argv[1]).resolve()
capture = json.loads((folder / 'checked.json').read_text(encoding='utf-8'))
assert capture['captureChecks'] == 'passed'
path = ROOT / 'docs/production/plan12-character-review-status.json'
status = json.loads(path.read_text(encoding='utf-8'))
heroes = {r['heroineId'] for r in capture['records']}
assert len(heroes) == 1
hero = next(iter(heroes))
row = next(r for r in status['characters'] if r['heroineId'] == hero)
views = {r['view'] for r in capture['records']}
for record in capture['records']:
    evidence = ROOT / record['path']
    assert hashlib.sha256(evidence.read_bytes()).hexdigest() == record['sha256']
    row['evidence'] = [r for r in row['evidence'] if r['view'] != record['view']]
    row['evidence'].append(dict(view=record['view'], path=record['path'], sha256=record['sha256']))
row['completedViews'] = sorted(set(row['completedViews']) | views)
row['remaining'] = [v for v in row['remaining'] if v not in views]
if all('expression.' + e in views for e in ['normal', 'joy', 'puzzled', 'determined']):
    row['remaining'] = [v for v in row['remaining'] if v != 'expression.alignment']
row['status'] = 'complete' if not row['remaining'] else 'open'
path.write_text(json.dumps(status, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print(hero + ': ' + row['status'] + '; remaining=' + ','.join(row['remaining']))

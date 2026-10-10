"""Persist per-character decisions; completed views are not scheduled again."""
import json, hashlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
status_path = ROOT / 'docs/production/plan12-character-review-status.json'
if status_path.exists():
    raise SystemExit('Existing per-character decisions preserved; update only the changed character and views.')
baseline = 'tmp/plan12-art-sweep/f0ea496e59904195a249380b95f00ffe/default'
correction = 'tmp/plan12-art-sweep/edef550744b74d959151fc6a28e9dba7/default'
audit = json.loads((ROOT / 'docs/production/plan12-runtime-art-audit.json').read_text(encoding='utf-8'))
content = json.loads((ROOT / 'docs/production/plan12-event-cg-content-review.json').read_text(encoding='utf-8'))
rows = []
for hero in sorted({r['heroineId'] for r in audit['rows']}):
    key = hero.removeprefix('heroine.')
    missing = [r['role'] for r in audit['missingDedicatedRoles'] if r['heroineId'] == hero]
    cg = [r['view'] for r in content['rows'] if r['heroineId'] == hero and r['classification'] != 'no_male_character_observed']
    if key == 'arcane':
        cg = []
    expression_open = key not in ['slayer', 'iconoclast', 'undermine', 'echidna', 'excalipan']
    remaining = missing + cg + (['expression.alignment'] if expression_open else [])
    completed = ['detail', 'tree', 'battle']
    completed += ['cg.' + str(n) for n in range(5) if 'cg.' + str(n) not in cg]
    completed += ['sd.' + p for p in ['idle', 'walk', 'talk', 'react', 'sit', 'work', 'look'] if 'sd.' + p not in missing]
    if not expression_open:
        completed += ['expression.' + e for e in ['normal', 'joy', 'puzzled', 'determined']]
    evidence = []
    for view in completed:
        folder = correction if (key == 'iconoclast' and view.startswith('expression.')) or (key == 'arcane' and view.startswith('cg.')) else baseline
        path = ROOT / folder / (key + '-' + view + '.png')
        if path.exists():
            evidence.append(dict(view=view, path=path.relative_to(ROOT).as_posix(), sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    rows.append(dict(heroineId=hero, status='complete' if not remaining else 'open', completedViews=completed, remaining=remaining, evidence=evidence))
result = dict(policy='キャラごとに完了を判定する。完了済み項目は再確認しない。修正した素材または表示に影響する項目のみ再開し、未完了項目だけを指定して確認する。', resolution='default launch only', characters=rows)
status_path.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('PLAN12_CHARACTER_REVIEW_RECORDED forms=' + str(len(rows)))

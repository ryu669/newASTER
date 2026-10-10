"""Record reviewed corrections without reopening other characters."""
import hashlib, json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
def read(path): return json.loads((ROOT / path).read_text(encoding='utf-8'))
def write(path, data): (ROOT / path).write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
folder = 'tmp/plan12-art-sweep/9a15642965ef4f76822f989c30e5b9d0/default'
status = read('docs/production/plan12-character-review-status.json')
academy = next(r for r in status['characters'] if r['heroineId'] == 'heroine.arcane-academy')
academy['evidence'] = [r for r in academy['evidence'] if r['view'] not in ['sd.sit', 'sd.work', 'sd.look']]
for pose in ['sit', 'work', 'look']:
    path = f'{folder}/arcane-academy-sd.{pose}.png'
    academy['evidence'].append(dict(view='sd.' + pose, path=path, sha256=hashlib.sha256((ROOT / path).read_bytes()).hexdigest()))
write('docs/production/plan12-character-review-status.json', status)
content = read('docs/production/plan12-event-cg-content-review.json')
for row in content['rows']:
    if row['heroineId'] == 'heroine.arcane':
        path = 'tmp/plan12-art-sweep/edef550744b74d959151fc6a28e9dba7/default/arcane-' + row['view'] + '.png'
        row.update(classification='no_male_character_observed', evidence=path, sha256=hashlib.sha256((ROOT / path).read_bytes()).hexdigest())
content['maleCharacterNg'] = sum(r['classification'] == 'male_character_ng' for r in content['rows'])
write('docs/production/plan12-event-cg-content-review.json', content)
for name, capture in [('arcane-event-cg-corrections', 'tmp/plan12-art-sweep/edef550744b74d959151fc6a28e9dba7/default'), ('iconoclast-expression-corrections', 'tmp/plan12-art-sweep/edef550744b74d959151fc6a28e9dba7/default'), ('arcane-academy-furniture-sd', folder)]:
    path = 'game/art-source/plan12/' + name + '-adoption.json'
    record = read(path)
    record.update(runtimeCapture=capture, visualReview='passed at default 1280x720; completed views frozen')
    write(path, record)
print('PLAN12_CHANGED_VIEWS_FINALIZED characters=3; other decisions preserved')

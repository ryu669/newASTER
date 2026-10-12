"""Validate staged Aeon content and ensure that rebuilding cannot alter live content."""
import contextlib
import hashlib
import io
import json
from pathlib import Path
import re
import runpy
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[1]
DRAFT = ROOT / 'game/unity/Assets/Game/ContentDrafts/Aeon'
def read(path): return json.loads(path.read_text(encoding='utf-8-sig'))

index = read(ROOT / 'docs/characters/index.json')
manifest = read(DRAFT / 'manifest.json')
combat, story = read(DRAFT / 'combat.json'), read(DRAFT / 'story.json')
hero = 'heroine.aeon'
assert manifest['available'] is False
assert manifest['stage'] == 'data-and-story-implemented-art-pending'
for kind, path_key, hash_key in [('combat','combatSource','combatSourceSha256'),
                                 ('story','storySource','storySourceSha256')]:
    path = ROOT / index[path_key]
    assert manifest[path_key] == index[path_key]
    assert manifest[hash_key] == hashlib.sha256(path.read_bytes()).hexdigest(), f'Stale {kind} baseline'
    original = read(path)
    extended = combat if kind == 'combat' else story
    for key, value in original.items():
        if isinstance(value, list):
            assert extended[key][:len(value)] == value, f'Changed existing {kind}/{key}'
        else:
            assert extended[key] == value, f'Changed existing {kind}/{key}'
assert len(combat['heroines']) == 16
chapters = [c for c in story['chapters'] if c['ownerId'] == hero]
events = [e for e in story['events'] if e['ownerId'] == hero]
assert len(chapters) == 3 and sum(len(c['pages']) for c in chapters) == 30
assert sum(len(c['poems']) for c in chapters) == 18 and len(events) == 5
assert all('あなた' not in quote for record in chapters + events
           for line in record.get('pages', record.get('paragraphs', []))
           for quote in re.findall('「([^」]*)」', line))
profile = ROOT / 'docs/characters/aeon.md'
text = profile.read_text(encoding='utf-8')
assert all('`'+r['id']+'`' in text for r in chapters + events)
assert all('## '+h in text for h in ['概要・性格','口調','葛藤・関係の進め方','避ける描写','参考資料・採用判断'])
for target in re.findall(r'\]\(([^)]+)\)', text):
    assert (profile.parent / target).exists(), target
captured = {}
def capture(path, value, *args, **kwargs):
    captured[path] = value
    return len(value)
with patch.object(Path, 'write_text', capture), contextlib.redirect_stdout(io.StringIO()):
    runpy.run_path(str(ROOT / 'tools/author-plan12-aeon.py'), run_name='__main__')
assert captured
for path, value in captured.items():
    assert path.read_text(encoding='utf-8') == value, f'Stale generated content: {path}'
report = dict(status='pass', personId=hero, liveForms=15, developmentForms=1,
              checks=['live baseline preserved','repeatable authoring','30 pages / 18 poems / 5 events',
                      'profile and source links','commander address in dialogue'],
              releaseReady=False, unverified=manifest['unresolved'])
runtime_report = ROOT / 'docs/production/plan12-aeon-runtime-validation.json'
if runtime_report.exists():
    evidence = read(runtime_report)
    for filename, digest in evidence['checkedFileSha256'].items():
        assert hashlib.sha256((ROOT / filename).read_bytes()).hexdigest() == digest, f'Stale runtime evidence: {filename}'
    report['coreAssertions'] = evidence['coreAssertions']
    report['unityCompile'] = evidence['unityCompile']
    report['unityJsonUtilityRoundtrip'] = evidence['unityJsonUtilityRoundtrip']
(ROOT / 'docs/production/plan12-aeon-data-validation.json').write_text(
    json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
print('AEON_AUTHORING_PASS / baseline preservation / deterministic rebuild / profile and story coverage; art pending')

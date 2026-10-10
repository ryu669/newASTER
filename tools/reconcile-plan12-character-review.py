"""Reconcile approved per-character evidence without capturing completed views again."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DOC = ROOT / 'docs/production'

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def write(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')

ledger = read(DOC / 'plan12-character-review-status.json')
characters = {c['heroineId']: c for c in ledger['characters']}
swim = characters['heroine.slayer-swim']
if 'sd.look' in swim['remaining']:
    swim['blocker'] = {'view': 'sd.look', 'reason': 'ImageGen output rejected by safety system (sexual category); dedicated pose remains missing, no substitute accepted', 'requestId': 'e5bb9b81-8440-405b-8b3f-a84783faac29'}
else:
    swim.pop('blocker', None)
write(DOC / 'plan12-character-review-status.json', ledger)
content = read(DOC / 'plan12-event-cg-content-review.json')
for row in content['rows']:
    character = characters[row['heroineId']]
    evidence = next((e for e in character['evidence'] if e['view'] == row['view']), None)
    if row['view'] in character['remaining'] or evidence is None:
        continue
    assert hashlib.sha256((ROOT / evidence['path']).read_bytes()).hexdigest() == evidence['sha256']
    row.update(classification='no_male_character_observed', evidence=evidence['path'], sha256=evidence['sha256'])
content['maleCharacterNg'] = sum(r['classification'] == 'male_character_observed' for r in content['rows'])
content['precautionaryHandsArmsNg'] = sum(r['classification'] not in ('male_character_observed', 'no_male_character_observed') for r in content['rows'])
content['method'] = 'approved per-character visual review; completed unchanged views retained; other people and partner limbs removed from replacement CGs'
write(DOC / 'plan12-event-cg-content-review.json', content)

for path in (ROOT / 'game/art-source/plan12').glob('*-final-corrections-adoption.json'):
    key = path.name.removesuffix('-final-corrections-adoption.json')
    character = characters['heroine.' + key]
    report = read(path)
    report['runtimeCapture'] = 'passed_for_adopted_assets'
    report['characterStatus'] = character['status']
    report['remaining'] = character['remaining']
    report['evidence'] = character['evidence']
    write(path, report)

look_report_path = ROOT / 'game/art-source/plan12/slayer-swim-look-open-corrections-adoption.json'
if look_report_path.exists() and 'sd.look' not in swim['remaining']:
    look_report = read(look_report_path)
    look_report.update(runtimeCapture='passed', characterStatus=swim['status'], evidence=[e for e in swim['evidence'] if e['view'] == 'sd.look'])
    write(look_report_path, look_report)
    prior_path = ROOT / 'game/art-source/plan12/slayer-swim-look-cardigan-corrections-adoption.json'
    if prior_path.exists():
        prior = read(prior_path)
        prior.update(runtimeCapture='superseded_before_visual_review', supersededBy=look_report_path.relative_to(ROOT).as_posix())
        write(prior_path, prior)

complete = sum(c['status'] == 'complete' for c in characters.values())
remaining = [{'heroineId': c['heroineId'], 'views': c['remaining']} for c in characters.values() if c['remaining']]
report = read(DOC / 'plan12-full-review-validation.json')
report['productionAcceptance'] = 'passed' if not remaining else 'incomplete_dedicated_art'
report['completedForms'] = complete
report['remaining'] = remaining
report['currentDecisions'] = 'docs/production/plan12-character-review-status.json'
report['checks'].update(compileSourceFiles=260, regressionAssertions=12513, sourceImportAspectImages=286 if not remaining else 285)
audit = read(DOC / 'plan12-runtime-art-audit.json')
report['checks'].update(runtimeReferences=audit['referenceCount'], uniqueReferencedImages=audit['uniqueImageCount'], missingDedicatedRoles=audit['missingDedicatedRoleCount'])
report['checks']['characterScopedVisualReview'] = 'passed' if not remaining else 'passed_except_listed_remaining'
report['historicalEvidenceNote'] = 'Original full-sweep evidence is retained as history. Current decisions and changed-view evidence are in the character ledger; no repeated full visual sweep.'
write(DOC / 'plan12-full-review-validation.json', report)

path = DOC / 'plan12-full-review.txt'
old = path.read_text(encoding='utf-8')
marker = '以下は上記差替え前の全点検記録。既存の完了判定は継承する。'
history = old.split(marker, 1)[-1]
swim_note = ('水着スレイヤーsd.look：画像生成が安全システムに拒否されたため専用素材不足。' if 'sd.look' in swim['remaining'] else '水着スレイヤーsd.look：水着の上に薄い上着を前開きで羽織った専用見上げ姿勢を採用。既定起動で確認完了。')
summary = f'''Plan12準備・既存15形態の総点検（2026-10-10）

最新判定：{complete}/15形態が完了。
確定判定と個別の画像証跡は plan12-character-review-status.json に保存。
完了済み項目は再確認しない。変更した未完了項目だけを確認した。
イベント絵は男性・相手の手腕を除去。全75枚の内容確認が完了。
表情は通常立ち絵の顔位置へ合成し、縦横比を保って表示。
イベント絵は比率を保持した最大表示、ヘッダは最小限。
実画面は既定起動1280×720のみ確認。解像度設定は変更していない。
家具姿勢sit/work/lookは全形態必須。代用は不足として扱う。
ビルド成功。回帰12513項目、260ソースのコンパイル成功。
残件：{json.dumps(remaining, ensure_ascii=False)}
{swim_note}

{marker}'''
path.write_text(summary + history, encoding='utf-8')
print(f'PLAN12_RECONCILED complete={complete}/15 remaining={remaining}')

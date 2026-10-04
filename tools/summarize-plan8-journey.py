"""Bind successful isolated player journeys to their saved progression and telemetry."""
import argparse
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parent.parent
parser = argparse.ArgumentParser()
parser.add_argument('run_ids', nargs='+')
args = parser.parse_args()

def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()

reports = []
for run_id in args.run_ids:
    assert run_id.startswith('story-') and len(run_id) == 38 and all(c in '0123456789abcdef' for c in run_id[6:])
    output = root / 'tmp/plan8-story-player' / run_id
    directory = root / 'tmp/plan8-runs' / run_id
    result = read(output / 'result.json')
    save_path = directory / 'formal-campaign-v1.json'
    save = read(save_path)
    journey = read(directory / 'journey.json')
    assert result['passed'] and result['journey'] and result['telemetry'] and result['normalSaveUnchanged']
    assert read(output / 'normal-save-before.json') == read(output / 'normal-save-after.json')
    assert all(case['passed'] and case['saveSha256'] == sha(save_path) for case in result['cases'])
    assert save['world']['highestBattleLevel'] == 50
    history = [{'level': r['battle']['level'], 'seed': r['battle']['seed'], 'reason': r['reason']} for r in save['collection']['receipts']]
    highest = 1
    for receipt in save['collection']['receipts']:
        battle = receipt['battle']
        assert receipt['reason'] == 0 and battle['level'] <= highest
        highest = min(50, max(highest, battle['level'] + 1))
        assert battle['colossusVersion'] == 'colossus-plan8-2026-10-04'
    assert highest == 50 and len(history) >= 65
    assert [h['level'] for h in save['growth']['heroines']] == [30] * 5
    assert len(save['home']['weaponEquipment']) == 5 and len(save['home']['furniturePlacements']) == 3
    assert len(save['home']['readLineKeys']) == 77 and len(save['home']['loverHeroineIds']) == 0
    assert journey['performanceMeasured'] is False and journey['humanTimingMeasured'] is False
    assert journey['points'][-1]['battles'] == len(history)
    summaries = []
    for case in result['cases']:
        summary_path = output / (case['case'] + '-telemetry-summary.json')
        summary = read(summary_path)
        event_path = root / 'tmp/plan8-runs' / summary['runId'] / 'events.jsonl'
        events = [json.loads(line) for line in event_path.read_text(encoding='utf-8-sig').splitlines()]
        assert len(events) == summary['events'] and len({e['eventId'] for e in events}) == len(events)
        previous_elapsed = previous_active = 0
        for event in events:
            assert event['runId'] == summary['runId'] and event['buildHash'] == result['assemblySha256']
            assert event['resourceHash'] == result['resourceSha256'] and len(event['definitionHash']) == 64
            assert event['elapsedSeconds'] >= previous_elapsed and event['activeSeconds'] >= previous_active
            assert event['activeSeconds'] <= event['elapsedSeconds'] + 0.000001
            previous_elapsed, previous_active = event['elapsedSeconds'], event['activeSeconds']
        if case['case'] == 'new':
            assert {e['category'] for e in events} == {'audio', 'battle', 'collection', 'economy', 'garden', 'navigation', 'reading', 'save', 'timing'}
            assert any(e['category'] == 'save' and e['action'] == 'failed' for e in events)
            battle_seeds = {r['battle']['battleId']: r['battle']['seed'] for r in save['collection']['receipts']}
            for event in events:
                if event['battleId'] in battle_seeds:
                    assert event['seed'] == battle_seeds[event['battleId']]
            assert any(e['action'] == 'diagnostic-input-accepted' for e in events)
        summaries.append({'case': case['case'], **summary, 'eventsSha256': sha(event_path), 'summarySha256': sha(summary_path)})
    if result['exchange']:
        assert save['growth']['totalKinderDraws'] == 100 and save['growth']['kinderPoints'] == 0
        assert {'journey.exchange', 'journey.ticket'} <= {r['transactionId'] for r in save['growth']['receipts']}
        assert all(t['count'] == 0 for t in save['growth']['tickets'])
    else:
        assert len(history) == 65 and save['growth']['totalKinderDraws'] == 10 and save['growth']['kinderPoints'] == 10
        assert {case['case'] for case in result['cases']} == {'new', 'resume', 'progress', 'conditions', 'garden', 'events'}
    reports.append({'result': result, 'resultSha256': sha(output / 'result.json'), 'saveSha256': sha(save_path),
                    'journey': journey, 'battleHistory': history, 'telemetry': summaries,
                    'screenshots': [{'case': case['case'], 'sha256': sha(output / (case['case'] + '.png'))} for case in result['cases']]})

assert {r['result']['height'] for r in reports if not r['result']['exchange']} == {720, 1080}
assert sum(r['result']['exchange'] for r in reports) == 1
assert len({r['result']['assemblySha256'] for r in reports}) == 1
assert len({r['result']['resourceSha256'] for r in reports}) == 1
report = {'schemaVersion': 1, 'scope': '8-6/8-7 earned progression, physical save and diagnostic UI/telemetry; not human playtest or performance',
          'passed': True, 'runs': reports, 'performanceMeasured': False, 'humanTimingMeasured': False,
          'activeSecondsInjected': 60, 'nativeMouseTested': False, 'nativeAltTabTested': False, 'listeningTested': False}
path = root / 'docs/production/plan8-journey-validation.json'
path.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
print('PLAN8_JOURNEY_REPORT_PASS', len(reports), 'runs', sum(len(r['result']['cases']) for r in reports), 'processes')

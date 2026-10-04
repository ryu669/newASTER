"""Check stored evidence consistency without treating pending experience gates as passed."""
import hashlib
import json
from pathlib import Path

root = Path(__file__).resolve().parent.parent
documents = root / 'docs/production'

def read(name):
    return json.loads((documents / name).read_text(encoding='utf-8-sig'))

gate = read('plan8-completion-gate.json')
assert gate['schemaVersion'] == 1 and gate['requirementsChanged'] is False
ids = [condition['id'] for condition in gate['conditions']]
assert len(ids) == len(set(ids))
assert all(condition['result'] in ('passed', 'failed', 'not-run') for condition in gate['conditions'])
assert all((documents / condition['evidence']).is_file() for condition in gate['conditions'])
assert any(condition['result'] == 'not-run' for condition in gate['conditions'])
assert not gate['overallComplete'] and not gate['technicalGateComplete'] and not gate['experienceGateComplete']
deferred = read('plan8-performance-deferred.json')
assert deferred['benchmarkPlayerLaunched'] is False and deferred['otherAppsClosed'] is False
assert deferred['finalRecheck']['clear'] is False and deferred['finalRecheck']['benchmarkPlayerLaunched'] is False
performance = read('plan8-performance-validation.json')
assert performance['sameRunTelemetryJoinPassed']
assert not performance['coldOsCacheMeasured'] and not performance['otherAppsClosed']
assert any(run['result'] == 'failed' and run['height'] == 1080 for run in performance['battleRuns'])
assert all(run['environment']['clear'] and run['normalSaveUnchanged'] and run['sameRunTelemetryJoinPassed'] for run in performance['battleRuns'])
assert all(not attempt['clear'] and not attempt['benchmarkLaunchedOnThisAttempt'] for attempt in performance['deferredAttempts'])
assert performance['frameGoalPassed'] == any(run['height'] == 1080 and run['result'] == 'passed' and run['buildHash'] == performance['currentBuildHash'] for run in performance['battleRuns'])
assert performance['readinessReusePolicy']['passed'] and performance['readinessReusePolicy']['automaticRetries'] == 0
assert performance['readinessReusePolicy']['sourceHashNormalization'] == 'UTF-8 without BOM; LF newlines'
assert hashlib.sha256((root / 'tools/check-plan8-measurement-readiness.ps1').read_text(encoding='utf-8-sig').encode('utf-8')).hexdigest().upper() == performance['readinessReusePolicy']['sourceSha256']
journey = read('plan8-journey-validation.json')
regression = read('plan8-regression-validation.json')
assert journey['passed'] and regression['passed'] and not journey['performanceMeasured'] and not regression['performanceMeasured']
assert all(run['result']['assemblySha256'] == regression['assemblySha256'] for run in journey['runs'])
assert all(run['result']['resourceSha256'] == regression['resourceSha256'] for run in journey['runs'])
garden_sha = next(screenshot['sha256'] for run in journey['runs'] if run['result']['height'] == 720 and not run['result']['exchange'] for screenshot in run['screenshots'] if screenshot['case'] == 'garden')
assert hashlib.sha256((documents / 'plan8-journey-garden-720.png').read_bytes()).hexdigest().upper() == garden_sha
assert sum(len(run['result']['cases']) for run in journey['runs']) == 14 and len(regression['cases']) == 23
data = root / 'game/Builds/playable/newASTER_Data'
for field, path in [('assemblySha256', data / 'Managed/Assembly-CSharp.dll'), ('resourceSha256', data / 'resources.assets')]:
    assert hashlib.sha256(path.read_bytes()).hexdigest().upper() == regression[field]
economy = read('plan8-economy-measurements.json')
assert economy['statisticalDraws'] == 200000 and economy['heroineRateBasisPoints'] == 300
assert economy['budget']['fiveHeroNectarTo120'] == 77350 and economy['budget']['fiveHeroCrystalsTo120'] == 400
assert economy['humanTimingMeasured'] is False and economy['performanceMeasured'] is False
assert performance['currentBuildHash'] == regression['assemblySha256']
assert performance['currentResourceHash'] == regression['resourceSha256']
print('PLAN8_EVIDENCE_CONSISTENT 37 functional processes; overallComplete=False; 1080p frameGoalPassed=' + str(performance['frameGoalPassed']) + '; remaining performance/human acceptance pending')
